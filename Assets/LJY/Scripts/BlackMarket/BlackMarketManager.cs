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
    [Tooltip("멤버십  단계에 도달 시, 아이템의 희귀도율을 설정한다.")]
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
    enum ItemTpye
    {
        Card = 0,
        Oparts = 1, 
    }

    [System.Serializable]
    class ItemData
    {
        public string Name;
        public ItemTpye Type;
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
                Type = ItemTpye.Card,
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

        [Header("UI References")]
        [SerializeField] private UIDocument _blackMarketUID;
        [SerializeField] private VisualTreeAsset _blackMarketUXML;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("슬롯이 추가되는 저축액 기준")]
        private List<Savings> savingsThresholds = new List<Savings>();

        [SerializeField]
        [Tooltip("멤버십 등급별 필요 비용 구간")]
        private List<int> membershipThresholds = new List<int>();

        [SerializeField]
        [Tooltip("멤버십 등급별 등장 확률 (인덱스 0 = 0단계 확률)")]
        private List<RarityProbability> rarityProbabilities = new List<RarityProbability>();

        [SerializeField] private int _baseSlotCount = 6;

        // 런타임 변수
        private int _additiveSlotCount = 0;
        private int _currentMembershipLevel = 0;
        private RarityProbability _currentProbability; // 현재 적용된 확률 테이블
        [SerializeField] private VisualElement _blackMarketRoot;
        private Label _coinValue;

        /// <summary>
        /// 외부에서 접근할 최종 슬롯 개수
        /// </summary>
        public int TotalSlotCount => _baseSlotCount + _additiveSlotCount;

        private BlackMarketBtnController _btnController;

        // 아이템 관련 정보를 가져오는 DB
        private ItemDatabase _itemDatabase;
        private List<ItemData> _itemsData;

        // 상점 캐릭터 위젯 UI
        private CharacterWidgetController _blackMarketCWController;

        [SerializeField][Tooltip("로컬 상의 플레이어 데이터")] private LocalUserDataBase _localUserData;

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

        /// <summary>
        /// 블랙마켓 UI 열기
        /// </summary>
        public void OpenBlackMarket()
        {
            if (_blackMarketUID == null || _blackMarketUXML == null) {
                Debug.LogError("UIDocument 혹은 BlackMarket UXML 값이 할당되지 않았습니다.");
                return;
            }

            // UI 생성 및 화면에 추가
            _blackMarketRoot = _blackMarketUXML.Instantiate();
            _blackMarketRoot.style.flexGrow = 1; // 화면 채우기
            _blackMarketUID.rootVisualElement.Add(_blackMarketRoot);
            _blackMarketCWController.Initialize(_blackMarketRoot);
            _coinValue = _blackMarketRoot.Q<VisualElement>("Coin_Panel").Q<Label>("Value");

            // 데이터 초기화
            Initialize(500, 1000);

            // 아이템 슬롯 생성
            StartMarket();

            // 버튼 이벤트 연결
            if (_btnController != null) {
                _btnController.BindUI(_blackMarketRoot);
            }
        }

        /// <summary>
        /// 블랙마켓 생성 절차
        /// </summary>
        /// <param name="currentSavings">현재 플레이어의 누적 저축액</param>
        /// <param name="currentMembershipFee">현재 판에서 지불한 멤버십 비용</param>
        public void Initialize(long currentSavings, int currentMembershipFee)
        {
            CalculateSlotCount(currentSavings);
            CalculateMembershipLevel(currentMembershipFee);

            Debug.Log($"[BlackMarket] 저축액: {currentSavings} -> 추가 슬롯: {_additiveSlotCount} / 멤버십 Lv: {_currentMembershipLevel}");
        }

        private void CalculateSlotCount(long savings)
        {
            _additiveSlotCount = 0;

            // 저축액이 기준치를 넘을 때마다 슬롯 추가
            foreach (var threshold in savingsThresholds) {
                if (savings >= threshold.value) {
                    _additiveSlotCount++;
                }
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
                else {
                    break; // 더 높은 단계는 달성 불가하므로 루프 종료
                }
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
            //string coinTxt = onHand.ToString();
            _coinValue.text = "999,999,999,999,998";
            // ----------------------------------

            _itemsData.Clear();

            // 슬롯 개수만큼 아이템 생성
            int totalSlots = TotalSlotCount;
            for (int i = 0; i < totalSlots; i++) {
                // 현재 등급에 맞는 희귀도 결정
                ItemRarity rarity = GetRandomRarity();

                // 해당 희귀도의 아이템 로드
                _itemsData.Add(_itemDatabase.GetRandomItem(rarity));
            }
            // 정보를 ui로 옮기기 시작
        }

        /// <summary>
        /// 블랙마켓 UI 닫기
        /// </summary>
        public void CloseBlackMarket()
        {
            if (_blackMarketRoot != null)
            {
                _blackMarketRoot.RemoveFromHierarchy(); // UI 제거
                _blackMarketRoot = null;
            }
        }

        /// <summary>
        /// 현재 멤버십 등급에 맞춰 랜덤한 희귀도를 반환하는 함수
        /// </summary>
        public ItemRarity GetRandomRarity()
        {
            int totalWeight = _currentProbability.GetTotalWeight();
            int randomValue = Random.Range(0, totalWeight);

            // 범위 초과 시
            if (randomValue >= totalWeight) {
                randomValue = randomValue % totalWeight;
            }

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
    }
}