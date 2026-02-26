using Audio.Controller;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.BlackMarket
{
    [System.Serializable]
    [Tooltip("멤버십 단계별 각 희귀도의 아이템 등장확률을 설정한다.")]
    struct RarityProbability
    {
        [Tooltip("멤버십 단계 이름")]
        public string Name;

        [Range(0, 100)] public int CommonWeight;
        [Range(0, 100)] public int RareWeight;
        [Range(0, 100)] public int UniqueWeight;
        [Range(0, 100)] public int LegendaryWeight;

        // 가중치 총합 계산
        public int GetTotalWeight() => CommonWeight + RareWeight + UniqueWeight + LegendaryWeight;
    }

    [System.Serializable]
    [Tooltip("저축 단계별 상한선 및 효과 설정")]
    struct Savings
    {
        public int value;
        [Range(0, 10)] public int additiveSlots;
        [Range(0, 10)] public int additiveRefreshing;
    }

    #region ItemData
    public enum ItemRarity
    {
        Common,
        Rare,
        Unique,
        Legendary
    }

    [System.Serializable]
    class ItemData
    {
        public int ID;
        public string Name;
        public ItemRarity Rarity;
        public int Price;
        public Sprite Image;
    }

    class CardData : ItemData
    {
        public int OwnerCharacterID;
    }

    class OpartsData : ItemData
    {
        public bool CanAppearShop;
    }
    #endregion

    class ItemDatabase
    {
        private List<ItemData> _masterItemDB = new List<ItemData>();

        private List<CardData> _curCardPool = new List<CardData>();
        private List<OpartsData> _curOpartsPool = new List<OpartsData>();

        private List<int> _excludeItemIDs = new List<int>();

        /// <summary>
        /// 이번 블랙마켓 씬에 등장할 수 있는 전체 아이템 풀 세팅
        /// </summary>
        public void CreateItemPool(List<int> partyCharacterIDs, List<int> ownedOpartsIDs)
        {
            _curCardPool.Clear();
            _curOpartsPool.Clear();

            foreach (var item in _masterItemDB) {
                // 새로고침 시, 직전 아이템들은 제외됨
                if (_excludeItemIDs != null && _excludeItemIDs.Contains(item.ID)) { continue; }

                if (item is CardData card) {
                    // 파티 내 캐릭터의 ID와 일치하는 카드만 추가
                    if (partyCharacterIDs.Contains(card.OwnerCharacterID)) {
                        _curCardPool.Add(card);
                    }
                }
                else if (item is OpartsData oparts) {
                    // 상점 등장 가능한 오파츠이면서, 현재 보유 중이 아닌 경우만 추가
                    if (oparts.CanAppearShop && !ownedOpartsIDs.Contains(oparts.ID)) {
                        _curOpartsPool.Add(oparts);
                    }
                }
            }
        }

        public void SetExcludeItemIDs(List<int> ids)
        {
            _excludeItemIDs.Clear();
            if (ids != null) {
                _excludeItemIDs.AddRange(ids);
            }
        }

        /// <summary>
        /// 요청한 타입에 맞춰 해당 풀에서 아이템 추출
        /// </summary>
        /// <param name="type">요청한 아이템 타입</param>
        /// <param name="rarity">희귀도</param>
        /// <returns>아이템 데이터</returns>
        public ItemData GetRandomItem(System.Type type, ItemRarity rarity)
        {
            if (type == typeof(CardData)) {
                return ExtractItem(_curCardPool, rarity, type);
            }
            else if (type == typeof(OpartsData)) {
                return ExtractItem(_curOpartsPool, rarity, type);
            }

            Debug.LogError($"알 수 없는 타입 요청 : {type}");
            return null;
        }

        private ItemData ExtractItem<T>(List<T> pool, ItemRarity rarity, System.Type type) where T : ItemData
        {
            // 해당 희귀도의 아이템 필터링
            List<T> filteredItems = pool.FindAll(item => item.Rarity == rarity);

            // 해당 희귀도의 아이템이 소진되었을 경우의 예외 처리
            if (filteredItems.Count == 0) {
                if (pool.Count == 0) {
                    Debug.LogWarning($"[{type.Name}]\n" +
                        $"  타입의 남은 아이템이 풀에 없습니다\n" +
                        $"  더미 아이템으로 대체됩니다\n");
                    return GetDummyItem(type, rarity); // 풀이 완전 고갈 시 더미 반환
                }

                // 희귀도 상관없이 남은 아이템 중 하나를 반환
                filteredItems = pool;
            }

            int ranIdx = Random.Range(0, filteredItems.Count);
            T selectedItem = filteredItems[ranIdx];

            // 상점 내 중복 등장 방지를 위해 풀에서 즉시 제거
            pool.Remove(selectedItem);

            return selectedItem;
        }

        private ItemData GetDummyItem(System.Type type, ItemRarity rarity)
        {
            int ranPrice = Random.Range(100, 80000);

            if (type == typeof(CardData)) {
                return new CardData { 
                    ID = 000, 
                    Name = "품절된 카드", 
                    Rarity = rarity, 
                    Price = ranPrice
                };
            }
            return new OpartsData { 
                ID = 000, 
                Name = "품절된 오파츠", 
                Rarity = rarity, 
                Price = ranPrice
            };
        }
    }

    [RequireComponent(typeof(BlackMarketBtnController))]
    public class BlackMarketManager : MonoBehaviour
    {
        public static BlackMarketManager Instance;
        
        public enum RefreshState
        {
            /// <summary>
            /// 저축 단계 미달
            /// </summary>
            Disabled,
            /// <summary>
            /// 활성화
            /// </summary>
            Active,
            /// <summary>
            /// 이미 사용함
            /// </summary>
            Locked, 
        }

        [Header("Debug 변수")]
        public long CurrentGold = 999999999999998;
        public int Savings = 500;
        public int MembershipFee = 1000;

        [Header("UI References")]
        [SerializeField] private UIDocument _blackMarketUID;
        [SerializeField] private VisualTreeAsset _blackMarketUXML;
        [SerializeField] private VisualTreeAsset _itemSlotUXML;
        [SerializeField] private VisualTreeAsset _settingPanelUXML;
        [SerializeField] private VisualTreeAsset _purchaseUXML;
        [SerializeField] private VisualTreeAsset _savingsUXML;

        [Header("Settings")]
        [SerializeField] [Tooltip("슬롯이 추가되는 저축액 기준")]
        private List<Savings> savingsThresholds = new List<Savings>();

        [SerializeField] [Tooltip("멤버십 등급별 필요 비용 구간")]
        private List<int> membershipThresholds = new List<int>();

        [SerializeField]
        [Tooltip("멤버십 등급별 등장 확률 (인덱스 0 = 0단계 확률)")]
        private List<RarityProbability> rarityProbabilities = new List<RarityProbability>();

        [Tooltip("저축 0단계 기준, 기본 지급되는 아이템 슬롯 개수")]
        [SerializeField] private int _baseSlotCount = 6;

        [Header("Local User Data")]
        [SerializeField] [Tooltip("로컬 상의 플레이어 데이터")]
        private LocalUserDataBase _localUserData;

        [Header("Audio References")]
        [SerializeField] private string BGM_BLACKMARKET = "BGM_BlackMarket";
        [SerializeField] private string SFX_DOORBELL = "SFX_Doorbell";
        [SerializeField] private string SFX_DOOR = "SFX_Door";
        [SerializeField] private string SFX_COIN = "SFX_Coin";
        [SerializeField] private string SFX_OPENSLOT = "SFX_OpenSlot";
        [SerializeField] private string VO_ENTER = "VO_Enter";
        [SerializeField] private string VO_BUY_1 = "VO_Buy_1";
        [SerializeField] private string VO_BUY_2 = "VO_Buy_2";
        [SerializeField] private string VO_DISABLED = "VO_Disabled";
        [SerializeField] private string VO_LOCKED = "VO_Locked";
        [SerializeField] private string VO_REFRESH = "VO_Refresh";
        [SerializeField] private string VO_EXIT = "VO_Exit";

        // -- 런타임 변수 --
        private int _additiveSlotCount = 0;
        private int _additiveRefreshingCount = 0;
        private int _usedRefreshingCount = 0;
        private int _curMembershipLevel = 0;
        private RarityProbability _curProbability;
        private RefreshState _curRefreshState;
        private List<int> _prevItemIDs;
        private int _selectedSlotIdx = -1;
        private ItemData _selectedItemData = null;

        /// <summary>
        /// 최종 슬롯 개수
        /// </summary>
        public int TotalSlotCount => _baseSlotCount + _additiveSlotCount;
        /// <summary>
        /// 최종 새로고침 횟수
        /// </summary>
        public int TotalRefreshingCount => _additiveRefreshingCount;

        // -- UID 변수 --
        private VisualElement _blackMarketRoot;
        private Label _coinValue;
        private VisualElement _slotContainer;
        private List<VisualElement> _slotList =new List<VisualElement>();
        private VisualElement _settingRoot;
        private Button _btnRefresh;
        private VisualElement _purchaseRoot;
        private VisualElement _savingsRoot;
        private Label _lblSavingsLevel;
        private Label _lblTotalSavings;
        private SliderInt _sldAmount;
        private Label _lblSelectedAmount;

        // -- 아이템 --
        private ItemDatabase _itemDatabase;
        private List<ItemData> _itemsData;

        // -- UI Controller --
        private CharacterWidgetController _blackMarketCWController;
        private BlackMarketBtnController _btnController;

        private void Awake()
        {
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(this);
            }

            _prevItemIDs = new List<int>();

            _itemDatabase = new ItemDatabase();
            _itemsData = new List<ItemData>();

            _btnController = GetComponent<BlackMarketBtnController>();
            _blackMarketCWController = GetComponent<CharacterWidgetController>();
        }

        // <----- 진입 및 퇴장 기능 ----->
        /// <summary>
        /// 블랙마켓 UI 열기
        /// </summary>
        public void OpenBlackMarket()
        {
            if (_blackMarketUID == null) {
                Debug.LogError("UIDocument 미할당");
                return;
            }
            if (_blackMarketUXML == null) {
                Debug.LogError("블랙마켓 UXML 미할당");
                return;
            }
            if (_itemSlotUXML == null) {
                Debug.LogError("아이템 슬롯 UXML 미할당");
                return;
            }

            // UI 생성 및 화면에 추가
            _blackMarketRoot = _blackMarketUXML.Instantiate();
            _blackMarketRoot.style.flexGrow = 1;
            _blackMarketUID.rootVisualElement.Add(_blackMarketRoot);

            _blackMarketCWController.Initialize(_blackMarketRoot);
            _coinValue = _blackMarketRoot.Q<VisualElement>("Coin_Panel").Q<Label>("Value");
            _slotContainer = _blackMarketRoot.Q<VisualElement>("Item_Grid_Container");
            _btnRefresh = _blackMarketRoot.Q<Button>("Btn_Refresh");

            CreateAndHidePanels();

            // 버튼 이벤트 연결
            if (_btnController != null) {
                _btnController.ConnectButtonEvt(_blackMarketRoot);
            }

            // 데이터 초기화
            Initialize(Savings, MembershipFee);

            // 아이템 슬롯 생성
            StartMarket();
        }

        /// <summary>
        /// 블랙마켓 UI 닫기
        /// </summary>
        public void CloseBlackMarket()
        {
            AudioController.Instance.PlaySFX(SFX_DOOR);
            AudioController.Instance.PlayVO(VO_EXIT);

            if (_blackMarketRoot != null) {
                _blackMarketRoot.RemoveFromHierarchy(); // UI 제거
                _blackMarketRoot = null;
            }
        }

        private void CreateAndHidePanels()
        {
            // --- 세팅 패널 ---
            if (_settingPanelUXML != null) {
                _settingRoot = _settingPanelUXML.Instantiate();
                SetAbsolutePosition(_settingRoot);
                _settingRoot.style.display = DisplayStyle.None;
                _blackMarketRoot.Add(_settingRoot);

                Button closeBtn = _settingRoot.Q<Button>("Btn_CloseSetting");
                if (closeBtn != null) closeBtn.clicked += CloseSettingPanel;

                if (SettingPanelController.Instance != null) {
                    SettingPanelController.Instance.ConnectSettingUI(_settingRoot);
                }
            }

            // --- 구매 팝업 패널 ---
            if (_purchaseUXML != null) {
                _purchaseRoot = _purchaseUXML.Instantiate();
                SetAbsolutePosition(_purchaseRoot);
                _purchaseRoot.style.display = DisplayStyle.None;
                _blackMarketRoot.Add(_purchaseRoot);

                if (_btnController != null) {
                    _btnController.ConnectPopupBtnEvt(_purchaseRoot);
                }
            }

            // --- 저축 팝업 패널 ---
            if (_savingsUXML != null) {
                _savingsRoot = _savingsUXML.Instantiate();
                SetAbsolutePosition(_savingsRoot);
                _savingsRoot.style.display = DisplayStyle.None;
                _blackMarketRoot.Add(_savingsRoot);

                _lblSavingsLevel = _savingsRoot.Q<Label>("Lbl_SavingsLevel");
                _lblTotalSavings = _savingsRoot.Q<Label>("Lbl_TotalSavings");
                _sldAmount = _savingsRoot.Q<SliderInt>("Sld_Amount");
                _lblSelectedAmount = _savingsRoot.Q<Label>("Lbl_SelectedAmount");

                if (_sldAmount != null && _lblSelectedAmount != null) {
                    _sldAmount.RegisterValueChangedCallback(evt => {
                        int snappedValue = Mathf.RoundToInt(evt.newValue / 100f) * 100;
                        if (snappedValue != evt.newValue) {
                            _sldAmount.SetValueWithoutNotify(snappedValue);
                        }
                        _lblSelectedAmount.text = $"{snappedValue:N0} G";
                    });
                }

                if (_btnController != null) {
                    _btnController.ConnectSavingsBtnEvt(_savingsRoot);
                }
            }
        }

        private void SetAbsolutePosition(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.top = 0;
            element.style.bottom = 0;
            element.style.left = 0;
            element.style.right = 0;
            element.style.flexGrow = 1;
        }

        // <----- 초기화 ----->
        /// <summary>
        /// 블랙마켓 생성 절차
        /// </summary>
        /// <param name="currentSavings">현재 플레이어의 누적 저축액</param>
        /// <param name="currentMembershipFee">현재 판에서 지불한 멤버십 비용</param>
        private void Initialize(long curSavings, int curMembershipFee)
        {
            CalculateSavingsEffect(curSavings);
            CalculateMembershipLevel(curMembershipFee);

            Debug.Log(
                $"[ BlackMarket ]\n" +
                $"  저축액 : {curSavings}\n" +
                $"      추가 슬롯 : {_additiveSlotCount}\n" +
                $"      추가 새로고침 : {_additiveRefreshingCount}\n" +
                $"  멤버십 Lv : {_curMembershipLevel}\n" +
                $"      일반 : {rarityProbabilities[_curMembershipLevel].CommonWeight}\n" +
                $"      희귀 : {rarityProbabilities[_curMembershipLevel].RareWeight}\n" +
                $"      영웅 : {rarityProbabilities[_curMembershipLevel].UniqueWeight}\n" +
                $"      전설 : {rarityProbabilities[_curMembershipLevel].LegendaryWeight}\n");

            _slotList.Clear();
            _slotContainer.Clear();

            for (int i = 0; i < TotalSlotCount; i++) {
                VisualElement slot = _itemSlotUXML.Instantiate();
                slot.name = slot.name + $"({i})";
                _slotList.Add(slot);
                _slotContainer.Add(slot);
            }
        }

        /// <summary>
        /// 블랙마켓 노드를 클릭하면 해당 함수가 작동될 수 있도록 해야 함
        /// </summary>
        public void StartMarket()
        {
            _itemDatabase.SetExcludeItemIDs(null);
            _usedRefreshingCount = 0;
            GenerateMarketItems();
            AudioController.Instance.PlayBGM(BGM_BLACKMARKET);
            AudioController.Instance.PlaySFX(SFX_DOOR);
            AudioController.Instance.PlaySFX(SFX_DOORBELL);
            AudioController.Instance.PlayVO(VO_ENTER);
        }

        // <----- 세팅 기능 ----->
        public void OpenSettingPanel()
        {
            if (_settingPanelUXML == null) {
                Debug.LogError("설정 패널 UXML 미할당");
                return;
            }
            _settingRoot.style.display = DisplayStyle.Flex;
        }

        public void CloseSettingPanel()
        {
            if (_settingRoot != null) {
                _settingRoot.style.display = DisplayStyle.None;
            }
        }

        // <----- 아이템 슬롯 기능 ----->
        private void CalculateSavingsEffect(long savings)
        {
            _additiveSlotCount = 0;
            _additiveRefreshingCount = 0;

            // 저축액이 기준치를 넘을 때마다 별도의 효과 추가
            for (int i = 0; i < savingsThresholds.Count; i++) {
                if (savings >= savingsThresholds[i].value) {
                    _additiveSlotCount = savingsThresholds[i].additiveSlots;
                    _additiveRefreshingCount = savingsThresholds[i].additiveRefreshing - _usedRefreshingCount;
                }
                else { break; }
            }

            _curRefreshState = (_additiveRefreshingCount > 0) ? RefreshState.Active : RefreshState.Disabled;
        }

        private void CalculateMembershipLevel(int fee)
        {
            _curMembershipLevel = 0;

            // 멤버십 비용에 따라 레벨 결정
            for (int i = 0; i < membershipThresholds.Count; i++) {
                if (fee >= membershipThresholds[i]) {
                    _curMembershipLevel = i;
                }
                else { break; }
            }

            // 정해진 멤버십 Level 을 넘지 않도록
            _curMembershipLevel = Mathf.Clamp(_curMembershipLevel, 0, rarityProbabilities.Count - 1);

            // 멤버십 Level에 따라 현재의 각 희귀도별 카드 및 오파츠 등장 확률이 다르게 설정되도록 함
            _curProbability = rarityProbabilities[_curMembershipLevel];
        }

        private void GenerateMarketItems()
        {
            var ddoManager = DontDestroyOnLoadManager.Instance;
            if (ddoManager != null && ddoManager.ResourceManager != null) {
                // int curUserID = ddoManager.ResourceManager.SelectUserID;
                // var curUserData = ddoManager.LocalUser(curUserID);

                // List<int> currentPartyIDs = curUserData.PartyCharacterIDs;
                // List<int> ownedOpartsIDs = curUserData.OwnedOpartsIDs;
                // _coinValue.text = curUserData.Gold.ToString("N0");

                // <---- 임시 더미 데이터 ---->
                List<int> curPartyIDs = new List<int> { 1, 2, 3 };
                List<int> ownedOpartsIDs = new List<int> { 101 };
                _coinValue.text = CurrentGold.ToString("N0");

                // 조건에 맞는 아이템 풀 사전 생성
                _itemDatabase.CreateItemPool(curPartyIDs, ownedOpartsIDs);
            }

            // 저축으로 슬롯이 늘어났다면 UI 새롭게 추가
            while (_slotList.Count < TotalSlotCount)
            {
                VisualElement slot = _itemSlotUXML.Instantiate();
                slot.name = $"ItemSlot_{_slotList.Count}";
                _slotList.Add(slot);
                _slotContainer.Add(slot);
            }
            // 인출로 슬롯이 줄어들었다면 잉여 UI 제거
            while (_slotList.Count > TotalSlotCount)
            {
                int lastIdx = _slotList.Count - 1;
                _slotContainer.Remove(_slotList[lastIdx]);
                _slotList.RemoveAt(lastIdx);
            }

            List<System.Type> curSlotTypes = GenerateSlotTypes(TotalSlotCount);

            _itemsData.Clear();

            // 슬롯 개수만큼 아이템 생성
            for (int i = 0; i < TotalSlotCount; i++) {
                ItemRarity rarity = GetRandomRarity(); // 현재 등급에 맞는 희귀도 결정
                System.Type requiredType = curSlotTypes[i];
                var itemData = _itemDatabase.GetRandomItem(requiredType, rarity);
                if (itemData == null) {
                    Debug.LogWarning($"아이템을 불러올 수 없습니다\n\n" +
                        $"[ 조건 ]\n" +
                        $"  아이템 타입 : {requiredType},\n" +
                        $"  희귀도 : {rarity}");
                    continue;
                }
                _itemsData.Add(itemData); // 해당 희귀도의 아이템 로드
            }

            BindSlotUI();
        }

        /// <summary>
        /// 카드 3장, 오파츠 2개를 보장함과 동시에 System.Type을 활용하여 요구한 슬롯 개수에 맞춰 랜덤하게 아이템 타입 리스트 생성
        /// </summary>
        private List<System.Type> GenerateSlotTypes(int totalSlotCount)
        {
            List<System.Type> slotTypes = new List<System.Type>();

            // 최소 수량 보장
            slotTypes.Add(typeof(CardData));
            slotTypes.Add(typeof(CardData));
            slotTypes.Add(typeof(CardData));
            slotTypes.Add(typeof(OpartsData));
            slotTypes.Add(typeof(OpartsData));

            // 나머지 랜덤 할당
            for (int i = 5; i < totalSlotCount; i++) {
                slotTypes.Add(Random.Range(0, 2) == 0 ? typeof(CardData) : typeof(OpartsData));
            }

            for (int i = 0; i < slotTypes.Count; i++) {
                int tempIdx = Random.Range(i, slotTypes.Count);
                System.Type temp = slotTypes[i];
                slotTypes[i] = slotTypes[tempIdx];
                slotTypes[tempIdx] = temp;
            }

            return slotTypes;
        }

        /// <summary>
        /// 현재 멤버십 등급에 맞춰 랜덤한 희귀도를 반환하는 함수
        /// </summary>
        private ItemRarity GetRandomRarity()
        {
            int totalWeight = _curProbability.GetTotalWeight();
            int randomValue = Random.Range(0, totalWeight);

            // 가중치 랜덤
            if (randomValue < _curProbability.CommonWeight)
                return ItemRarity.Common;

            randomValue -= _curProbability.CommonWeight;
            if (randomValue < _curProbability.RareWeight)
                return ItemRarity.Rare;

            randomValue -= _curProbability.RareWeight;
            if (randomValue < _curProbability.UniqueWeight)
                return ItemRarity.Unique;

            return ItemRarity.Legendary;
        }

        private void BindSlotUI()
        {
            for (int i = 0; i < TotalSlotCount; i++) {
                // 현재 인덱스의 슬롯 UI와 아이템 데이터 가져오기
                VisualElement slot = _slotList[i];
                ItemData itemData = _itemsData[i];

                slot.UnregisterCallback<ClickEvent, int>(OnSlotClicked);

                if (itemData == null) {
                    LockSlotUI(slot); // 이미 팔린 자리면 잠금 처리
                    continue;
                }
                slot.style.opacity = 1f;

                Label nameLabel = slot.Q<Label>("Lbl_ItemName");
                Label priceLabel = slot.Q<Label>("Lbl_ItemPrice");
                VisualElement iconElement = slot.Q<VisualElement>("Img_ItemIcon");
                VisualElement rarityBackground = slot.Q<VisualElement>("RarityBackground"); // 희귀도용 배경 테두리가 있다면

                // 텍스트 데이터 바인딩
                if (nameLabel != null) nameLabel.text = itemData.Name;
                if (priceLabel != null) priceLabel.text = itemData.Price.ToString("N0"); // 10^3 단위로 콤마 찍기

                // Sprite 이미지 바인딩
                if (iconElement != null && itemData.Image != null) {
                    iconElement.style.backgroundImage = new StyleBackground(itemData.Image);
                }

                // 희귀도(Rarity)에 따른 시각적 피드백
                if (rarityBackground != null) {
                    StyleColor borderColor = new StyleColor(GetRarityColor(itemData.Rarity));
                    rarityBackground.style.borderBottomColor = borderColor;
                    rarityBackground.style.borderLeftColor = borderColor;
                    rarityBackground.style.borderRightColor = borderColor;
                    rarityBackground.style.borderTopColor = borderColor;
                }

                slot.RegisterCallback<ClickEvent, int>(OnSlotClicked, i);
            }
        }

        /// <summary>
        /// 불특정 슬롯 클릭 시 호출
        /// </summary>
        private void OnSlotClicked(ClickEvent evt, int slotIdx)
        {
            if (_itemsData[slotIdx] == null) return; // 빈 데이터면 무시

            _selectedSlotIdx = slotIdx;
            _selectedItemData = _itemsData[slotIdx];

            OpenPurchasePopup(_selectedItemData);
        }

        // <----- 새로고침 기능 ----->
        /// <summary>
        /// 새로고침 로직
        /// </summary>
        public void HandleRefreshRequest()
        {
            switch (_curRefreshState) {
                case RefreshState.Disabled:
                    AudioController.Instance.PlayVO(VO_DISABLED);
                    Debug.Log("대화창 : 아직 저축 금액이 부족하신데요? 저와의 신뢰 관계를 더 쌓으셔야겠어요.");
                    break;

                case RefreshState.Locked:
                    AudioController.Instance.PlayVO(VO_LOCKED);
                    Debug.Log("대화창 : 사용 가능한 새로고침 횟수를 전부 소진하셨어요.");
                    break;

                case RefreshState.Active:
                    _additiveRefreshingCount--;
                    _usedRefreshingCount++;
                    if (_additiveRefreshingCount <= 0) {
                        _curRefreshState = RefreshState.Locked;
                    }

                    UpdateRefreshButtonUI();

                    _prevItemIDs.Clear();
                    foreach (var item in _itemsData) {
                        if (item== null) continue;
                        _prevItemIDs.Add(item.ID);
                    }
                    _itemDatabase.SetExcludeItemIDs(_prevItemIDs);

                    GenerateMarketItems();

                    AudioController.Instance.PlayVO(VO_REFRESH);
                    Debug.Log("대화창 : 새로운 상품이 준비되었습니다!");
                    break;
            }
        }

        private void UpdateRefreshButtonUI()
        {
            if (_btnRefresh == null) return;

            _btnRefresh.text = $"새로고침 x {_additiveRefreshingCount}";

            if (_curRefreshState == RefreshState.Active) {
                _btnRefresh.style.opacity = 1f;
            }
            else {
                _btnRefresh.style.opacity = 0.5f;
            }
        }

        // <----- 구매 기능 ----->
        /// <summary>
        /// 구매 팝업을 생성하고 데이터를 삽입
        /// </summary>
        private void OpenPurchasePopup(ItemData item)
        {
            if (_purchaseUXML == null) {
                Debug.LogError("구매 팝업 UXML이 할당되지 않았습니다");
                return;
            }

            _purchaseRoot.style.display = DisplayStyle.Flex;

            // 정보만 갈아끼우기
            Label popupName = _purchaseRoot.Q<Label>("Lbl_PopupName");
            Label popupPrice = _purchaseRoot.Q<Label>("Lbl_PopupPrice");
            VisualElement popupIcon = _purchaseRoot.Q<VisualElement>("Img_PopupIcon");
            VisualElement popupBackground = _purchaseRoot.Q<VisualElement>("Img_PopupIcon");

            if (popupName != null) popupName.text = item.Name;
            if (popupPrice != null) popupPrice.text = item.Price.ToString("N0");
            if (popupIcon != null && item.Image != null) {
                popupIcon.style.backgroundImage = new StyleBackground(item.Image);
            }
            if (popupBackground != null) {
                popupBackground.style.borderBottomColor = GetRarityColor(item.Rarity);
                popupBackground.style.borderLeftColor = GetRarityColor(item.Rarity);
                popupBackground.style.borderRightColor = GetRarityColor(item.Rarity);
                popupBackground.style.borderTopColor = GetRarityColor(item.Rarity);
            }

            AudioController.Instance.PlaySFX(SFX_OPENSLOT);
        }

        /// <summary>
        /// BtnController에서 취소 클릭 시
        /// </summary>
        public void ClosePurchasePopup()
        {
            if (_purchaseRoot != null) {
                _purchaseRoot.style.display = DisplayStyle.None;
            }
            _selectedSlotIdx = -1;
            _selectedItemData = null;
        }

        /// <summary>
        /// BtnController에서 구매 클릭 시
        /// </summary>
        public void ConfirmPurchase()
        {
            if (_selectedItemData == null) return;

            var ddoManager = DontDestroyOnLoadManager.Instance;
            if (ddoManager == null || ddoManager.ResourceManager == null) return;

            int curUserID = ddoManager.ResourceManager.SelectUserID;
           // var curUserData = ddoManager.LocalUser(curUserID);

            if (CurrentGold >= _selectedItemData.Price) {
                CurrentGold -= _selectedItemData.Price;
                // curUserData.Gold = currentGold;
                _coinValue.text = CurrentGold.ToString("N0");

                if (_selectedItemData is CardData card) {
                    Debug.Log($"카드 획득 처리: {card.Name}");
                    // curUserData.AddCard(card.ID);
                }
                else if (_selectedItemData is OpartsData oparts) {
                    Debug.Log($"오파츠 획득 처리: {oparts.Name}");
                    // curUserData.AddOparts(oparts.ID);
                }

                // 상품 슬롯 잠금 상태로 변경
                _itemsData[_selectedSlotIdx] = null;
                LockSlotUI(_slotList[_selectedSlotIdx]);

                AudioController.Instance.PlaySFX(SFX_COIN);
                if (Random.Range(0, 2) == 0) AudioController.Instance.PlayVO(VO_BUY_1);
                else AudioController.Instance.PlayVO(VO_BUY_2);

                Debug.Log("대화창 : 성공적으로 거래가 성사되었습니다.");
                ClosePurchasePopup();
            }
            else {
                Debug.Log("대화창 : 재화가 부족합니다.");
            }
        }

        private void LockSlotUI(VisualElement slot)
        {
            slot.style.opacity = 0.3f;
            slot.UnregisterCallback<ClickEvent, int>(OnSlotClicked);
        }

        // <----- 저축 기능 ----->
        /// <summary>
        /// NPC 클릭 시 저축 팝업 열기
        /// </summary>
        public void OpenSavingsPopup()
        {
            if (_savingsRoot == null) {
                Debug.LogError("저축 UI Root 미할당");
                return; 
            }

            _savingsRoot.style.display = DisplayStyle.Flex;
            UpdateSavingsPopupUI();
        }

        public void CloseSavingsPopup()
        {
            if (_savingsRoot != null) {
                _savingsRoot.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        ///슬라이더 값을 참조하여 입금 처리
        /// </summary>
        public void RequestDeposit()
        {
            if (_sldAmount == null) return;
            int amount = _sldAmount.value;

            if (CurrentGold >= amount) {
                CurrentGold -= amount;
                Savings += amount;

                RefreshSavingsState();
                Debug.Log($"대화창 : {amount}G 입금 완료!");
            }
            else {
                Debug.Log("대화창 : 소지하신 재화가 부족합니다.");
            }
        }

        /// <summary>
        /// 슬라이더 값을 참조하여 인출 처리
        /// </summary>
        public void RequestWithdraw()
        {
            if (_sldAmount == null) return;
            int amount = _sldAmount.value;

            if (Savings >= amount) {
                long expectedSavings = Savings - amount;
                int expectedAdditiveSlots = 0;
                int expectedTotalRefreshing = 0;

                for (int i = 0; i < savingsThresholds.Count; i++) {
                    if (expectedSavings >= savingsThresholds[i].value) {
                        expectedAdditiveSlots = savingsThresholds[i].additiveSlots;
                        expectedTotalRefreshing = savingsThresholds[i].additiveRefreshing;
                    }
                    else break;
                }

                int expectedTotalSlots = _baseSlotCount + expectedAdditiveSlots;
                int purchasedCount = 0;
                foreach (var item in _itemsData) {
                    if (item == null) purchasedCount++;
                }

                bool isRefreshingValid = expectedTotalRefreshing >= _usedRefreshingCount;
                bool isSlotValid = !(expectedTotalSlots < TotalSlotCount && purchasedCount > 0);

                // 인출로 인해 새로고침 개수가 줄어들 예정인데, 사용해버려서 교환 불가하다면 허용하지 않음
                // 인출로 인해 슬롯 개수가 줄어들 예정인데, 이미 하나라도 샀다면 허용하지 않음
                if (isRefreshingValid && isSlotValid) {
                    Savings -= amount;
                    CurrentGold += amount;

                    // 인출 후 슬롯 감소 즉시 반영
                    while (_slotList.Count > expectedTotalSlots) {
                        int lastIdx = _slotList.Count - 1;
                        if (_itemsData.Count > lastIdx) {
                            _itemsData.RemoveAt(lastIdx);
                        }
                        _slotContainer.Remove(_slotList[lastIdx]);
                        _slotList.RemoveAt(lastIdx);
                    }

                    RefreshSavingsState();
                    Debug.Log($"대화창 : {amount}G 인출 완료!");
                }
                else {
                    Debug.Log($"대화창 : 고객님, 이미 블랙마켓 뱅크에서 제공된 혜택을 사용하셔서 해당 금액은 인출이 불가합니다.");
                }
            }
            else {
                Debug.Log("대화창 : 인출할 저축액이 부족합니다.");
            }
        }

        /// <summary>
        /// 입출금 발생 시 데이터 및 UI 갱신
        /// </summary>
        private void RefreshSavingsState()
        {
            // 골드 UI 갱신
            if (_coinValue != null) {
                _coinValue.text = CurrentGold.ToString("N0");
            }

            // 내부 데이터 재계산
            CalculateSavingsEffect(Savings);

            // UI 갱신
            UpdateSavingsPopupUI();
            UpdateRefreshButtonUI();
        }

        private void UpdateSavingsPopupUI()
        {
            if (_savingsRoot == null) return;

            // 현재 누적 단계 계산 (CalculateSavingsEffect에서 세팅된 값 기반)
            int currentLevel = 0;
            for (int i = 0; i < savingsThresholds.Count; i++) {
                if (Savings >= savingsThresholds[i].value) {
                    currentLevel = i + 1;
                }
                else break;
            }

            if (_lblSavingsLevel != null) _lblSavingsLevel.text = $"Lv. {currentLevel}";
            if (_lblTotalSavings != null) _lblTotalSavings.text = $"{Savings.ToString("N0")} G";
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity) {
                case ItemRarity.Common : return Color.black;
                case ItemRarity.Rare : return Color.red;
                case ItemRarity.Unique : return Color.blue;
                case ItemRarity.Legendary : return Color.yellow;
                default :
                    Debug.LogError("아이템 희귀도 색깔 미지정");
                    return Color.black;
            }
        }
    }
}