using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

namespace UI.BlackMarket
{
    public enum ItemRarity
    {
        Common,
        Rare,
        Unique,
        Legendary
    }

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

    [System.Serializable]
    enum ItemType
    {
        Card = 0,
        Oparts = 1, 
    }

    [System.Serializable]
    class ItemData
    {
        public string Name;
        public ItemType Type;
        public ItemRarity Rarity;
        public int Price;
        public Sprite Image;
    }

    class ItemDatabase
    {
        public ItemData GetRandomItem(ItemRarity rarity)
        {
            int value = Random.Range(0, 100);

            // csv로 작성된 DB에서 해당하는 아이템 번호를 가져옴
            // 카드와 오파츠의 비율은 최소값을 보장하되 그 이상으로는 무작위에 의존한다.

            return new ItemData {
                Name = "dummy_Item",
                Type = ItemType.Card,
                Rarity = rarity,
                Price = 0,
                Image = null
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
            // <---- 보유 재화 업데이트 ---->
            // 유저 데이터를 스크립터블 오브젝트로 가져오는 로직이 완성되면 적용 가능

            //int onHand = _localUserData.LocalUserList.Gold;
            //string coinTxt = onHand.ToString("N0");
            _coinValue.text = "999,999,999,999,998";
            // ----------------------------------

            _itemsData.Clear();

            // 슬롯 개수만큼 아이템 생성
            int totalSlots = TotalSlotCount;
            for (int i = 0; i < totalSlots; i++) {
                ItemRarity rarity = GetRandomRarity(); // 현재 등급에 맞는 희귀도 결정 
                _itemsData.Add(_itemDatabase.GetRandomItem(rarity)); // 해당 희귀도의 아이템 로드
            }

            BindSlotUI();
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
        public ItemRarity GetRandomRarity()
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
        
        public Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common :
                    return Color.black;
                case ItemRarity.Rare :
                    return Color.red;
                case ItemRarity.Unique :
                    return Color.blue;
                case ItemRarity.Legendary :
                    return Color.yellow;
                default :
                    Debug.LogError("아이템 희귀도 색깔 미지정");
                    return Color.black;
            }
        }
    }

}