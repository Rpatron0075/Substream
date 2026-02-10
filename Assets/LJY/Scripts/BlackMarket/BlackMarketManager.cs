using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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
    public string LevelName;

    [Range(0, 100)] public int CommonWeight;
    [Range(0, 100)] public int RareWeight;
    [Range(0, 100)] public int UniqueWeight;
    [Range(0, 100)] public int LegendaryWeight;

    // 가중치 총합 계산
    public int GetTotalWeight() => CommonWeight + RareWeight + UniqueWeight + LegendaryWeight;
}

public class BlackMarketManager : MonoBehaviour
{
    public static BlackMarketManager Instance;

    [Header("Settings")]
    [SerializeField]
    [Tooltip("슬롯이 추가되는 저축액 기준")]
    private List<int> savingsThresholds = new List<int>();

    [SerializeField]
    [Tooltip("멤버십 등급별 필요 비용 구간")]
    private List<int> membershipThresholds = new List<int>();

    [SerializeField]
    [Tooltip("멤버십 등급별 등장 확률 (인덱스 0 = 0단계 확률)")]
    private List<RarityProbability> rarityProbabilities = new List<RarityProbability>();


    [Header("Base Settings")]
    [SerializeField] private int _baseSlotCount = 6;

    // 런타임 변수
    private int _additiveSlotCount = 0;
    private int _currentMembershipLevel = 0;
    private RarityProbability _currentProbability; // 현재 적용된 확률 테이블

    // 외부에서 접근할 최종 슬롯 개수
    public int TotalSlotCount => _baseSlotCount + _additiveSlotCount;

    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(this);
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

        Debug.Log($"[BlackMarket] 저축액: {savingsThresholds} -> 추가 슬롯: {_additiveSlotCount} / 멤버십 Lv: {_currentMembershipLevel}");
    }

    private void CalculateSlotCount(long savings)
    {
        _additiveSlotCount = 0;

        // 저축액이 기준치를 넘을 때마다 슬롯 추가
        foreach (int threshold in savingsThresholds) {
            if (savings >= threshold) {
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
