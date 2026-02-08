using UnityEngine;

public class BlackMarketSpawner : MonoBehaviour
{
    BlackMarketManager bmManager;

    private void Start()
    {
        bmManager = BlackMarketManager.Instance;
    }

    /// <summary>
    /// 블랙마켓 노드를 클릭하면 해당 함수가 작동될 수 있도록 해야 함
    /// </summary>
    public void StartShop()
    {
        // 상점 데이터 초기화
        bmManager.Initialize(500, 1000);

        // 슬롯 개수만큼 아이템 생성
        int totalSlots = bmManager.TotalSlotCount;
        for (int i = 0; i < totalSlots; i++) {
            // 현재 등급에 맞는 희귀도 결정
            ItemRarity rarity = bmManager.GetRandomRarity();

            // 해당 희귀도의 아이템 로드 및 UI 표시
            // ItemData item = bmManageritemDatabase.GetRandomItem(rarity);
            // uiSlot[i].SetItem(item);
        }
    }
}