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

        /// <summary>
        /// 이번 블랙마켓 씬에 등장할 수 있는 전체 아이템 풀 세팅
        /// </summary>
        public void CreateItemPool(List<int> partyCharacterIDs, List<int> ownedOpartsIDs)
        {
            _curCardPool.Clear();
            _curOpartsPool.Clear();

            foreach (var item in _masterItemDB) {
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
            if (type == typeof(CardData)) {
                return new CardData { 
                    ID = 000, 
                    Name = "품절된 카드", 
                    Rarity = rarity, 
                    Price = 0 };
            }
            return new OpartsData { 
                ID = 000, 
                Name = "품절된 오파츠", 
                Rarity = rarity, 
                Price = 0 
            };
        }
    }

    [RequireComponent(typeof(BlackMarketBtnController))]
    public class BlackMarketManager : MonoBehaviour
    {
        public static BlackMarketManager Instance;

        [Header("Debug 변수")]
        public int Savings = 500;
        public int MembershipFee = 1000;

        [Header("UI References")]
        [SerializeField] private UIDocument _blackMarketUID;
        [SerializeField] private VisualTreeAsset _blackMarketUXML;
        [SerializeField] private VisualTreeAsset _itemSlotUXML;
        [SerializeField] private VisualTreeAsset _settingPanelUXML;

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

        // -- 런타임 변수 --
        private int _additiveSlotCount = 0;
        private int _additiveRefreshingCount = 0;
        private int _currentMembershipLevel = 0;
        private RarityProbability _currentProbability;

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

            _btnController = GetComponent<BlackMarketBtnController>();

            _itemDatabase = new ItemDatabase();
            _itemsData = new List<ItemData>();

            _blackMarketCWController = GetComponent<CharacterWidgetController>();
        }

        #region EventFunc
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

            // 데이터 초기화
            Initialize(Savings, MembershipFee);

            // 아이템 슬롯 생성
            StartMarket();

            // 버튼 이벤트 연결
            if (_btnController != null) {
                _btnController.ConnectButtonEvt(_blackMarketRoot);
            }
        }

        /// <summary>
        /// 블랙마켓 UI 닫기
        /// </summary>
        public void CloseBlackMarket()
        {
            if (_blackMarketRoot != null) {
                _blackMarketRoot.RemoveFromHierarchy(); // UI 제거
                _blackMarketRoot = null;
            }
        }

        public void OpenSettingPanel()
        {
            if (_settingPanelUXML == null) {
                Debug.LogError("설정 패널 UXML 미할당");
                return;
            }

            if (_settingRoot != null) return;

            _settingRoot = _settingPanelUXML.Instantiate();
            _settingRoot.style.position = Position.Absolute;
            _settingRoot.style.top = 0;
            _settingRoot.style.bottom = 0;
            _settingRoot.style.left = 0;
            _settingRoot.style.right = 0;
            _settingRoot.style.flexGrow = 1;

            _blackMarketRoot.Add(_settingRoot); // 블랙마켓 메인 루트 자식으로 추가

            Button closeBtn = _settingRoot.Q<Button>("Btn_CloseSetting");
            if (closeBtn != null) {
                closeBtn.clicked += CloseSettingPanel;
            }
        }

        public void CloseSettingPanel()
        {
            if (_settingRoot != null)
            {
                _settingRoot.RemoveFromHierarchy(); // 화면에서 제거
                _settingRoot = null; // 메모리 참조 해제
            }
        }

        #endregion

        /// <summary>
        /// 블랙마켓 생성 절차
        /// </summary>
        /// <param name="currentSavings">현재 플레이어의 누적 저축액</param>
        /// <param name="currentMembershipFee">현재 판에서 지불한 멤버십 비용</param>
        public void Initialize(long currentSavings, int currentMembershipFee)
        {
            CalculateSlotCount(currentSavings);
            CalculateMembershipLevel(currentMembershipFee);

            Debug.Log(
                $"[BlackMarket]\n" +
                $"  저축액: {currentSavings}\n" +
                $"      추가 슬롯: {_additiveSlotCount}\n" +
                $"      추가 새로고침: {_additiveRefreshingCount}\n" +
                $"  멤버십 Lv: {_currentMembershipLevel}");

            _slotList.Clear();
            _slotContainer.Clear();

            for (int i = 0; i < TotalSlotCount; i++) {
                VisualElement slot = _itemSlotUXML.Instantiate();
                slot.name = slot.name + $"({i})";
                _slotList.Add(slot);
                _slotContainer.Add(slot);
            }
        }

        private void CalculateSlotCount(long savings)
        {
            _additiveSlotCount = 0;
            _additiveRefreshingCount = 0;

            // 저축액이 기준치를 넘을 때마다 별도의 효과 추가
            for (int i = 0; i < savingsThresholds.Count; i++) {
                if (savings >= savingsThresholds[i].value) {
                    _additiveSlotCount = savingsThresholds[i].additiveSlots;
                    _additiveRefreshingCount = savingsThresholds[i].additiveRefreshing;
                }
                else { break; }
            }

        }

        private void CalculateMembershipLevel(int fee)
        {
            _currentMembershipLevel = 0;

            // 멤버십 비용에 따라 레벨 결정
            for (int i = 0; i < membershipThresholds.Count; i++) {
                if (fee >= membershipThresholds[i]) {
                    _currentMembershipLevel = i;
                }
                else { break; }
            }

            // 정해진 멤버십 Level 을 넘지 않도록
            _currentMembershipLevel = Mathf.Clamp(_currentMembershipLevel, 0, rarityProbabilities.Count - 1);

            // 멤버십 Level에 따라 현재의 각 희귀도별 카드 및 오파츠 등장 확률이 다르게 설정되도록 함
            _currentProbability = rarityProbabilities[_currentMembershipLevel];
        }

        /// <summary>
        /// 블랙마켓 노드를 클릭하면 해당 함수가 작동될 수 있도록 해야 함
        /// </summary>
        public void StartMarket()
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
                _coinValue.text = "999,999,999,999,998";

                // 조건에 맞는 아이템 풀 사전 생성
                _itemDatabase.CreateItemPool(curPartyIDs, ownedOpartsIDs);
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
        /// System.Type을 활용하여 슬롯 개수에 맞춰 카드 3장, 오파츠 2개를 보장하는 타입 리스트 생성
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

        private void BindSlotUI()
        {
            for (int i = 0; i < TotalSlotCount; i++) {
                // 현재 인덱스의 슬롯 UI와 아이템 데이터 가져오기
                VisualElement slot = _slotList[i];
                ItemData itemData = _itemsData[i];

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
            }
        }

        /// <summary>
        /// 현재 멤버십 등급에 맞춰 랜덤한 희귀도를 반환하는 함수
        /// </summary>
        private ItemRarity GetRandomRarity()
        {
            int totalWeight = _currentProbability.GetTotalWeight();
            int randomValue = Random.Range(0, totalWeight);

            // 가중치 랜덤
            if (randomValue < _currentProbability.CommonWeight)
                return ItemRarity.Common;

            randomValue -= _currentProbability.CommonWeight;
            if (randomValue < _currentProbability.RareWeight)
                return ItemRarity.Rare;

            randomValue -= _currentProbability.RareWeight;
            if (randomValue < _currentProbability.UniqueWeight)
                return ItemRarity.Unique;

            return ItemRarity.Legendary;
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