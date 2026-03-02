using System.Collections.Generic;
using UnityEngine;

namespace Item
{
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
        public string Info;
        public Sprite Image;
        public int OffsetX;
        public int OffsetY;
        public float Scale;
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
        private ItemIconDatabase _iconDB;

        private List<ItemData> _masterItemDB = new List<ItemData>();
        private List<CardData> _curCardPool = new List<CardData>();
        private List<OpartsData> _curOpartsPool = new List<OpartsData>();
        private List<int> _excludeItemIDs = new List<int>();

        public void Initialize(ItemIconDatabase iconDB)
        {
            _iconDB = iconDB;

            // >> 여기서 아이템을 파싱해야 한다 <<

            MapAllImagesToMasterDB();
        }

        private void MapAllImagesToMasterDB()
        {
            if (_iconDB == null) {
                Debug.LogError("[ItemDatabase] ItemIconDatabase가 연결되지 않았습니다");
                return;
            }

            foreach (var item in _masterItemDB) {
                item.Image = _iconDB.GetIcon(item.ID);
            }

            Debug.Log("[ItemDatabase] 모든 아이템에 이미지 매핑 완료");
        }

        /// <summary>
        /// 이번 블랙마켓 씬에 등장할 수 있는 전체 아이템 풀 세팅
        /// </summary>
        public void CreateItemPool(List<int> partyCharacterIDs, List<int> ownedOpartsIDs)
        {
            _curCardPool.Clear();
            _curOpartsPool.Clear();

            foreach (var item in _masterItemDB)
            {
                // 새로고침 시, 직전 아이템들은 제외됨
                if (_excludeItemIDs != null && _excludeItemIDs.Contains(item.ID)) { continue; }

                if (item is CardData card)
                {
                    // 파티 내 캐릭터의 ID와 일치하는 카드만 추가
                    if (partyCharacterIDs.Contains(card.OwnerCharacterID))
                    {
                        _curCardPool.Add(card);
                    }
                }
                else if (item is OpartsData oparts)
                {
                    // 상점 등장 가능한 오파츠이면서, 현재 보유 중이 아닌 경우만 추가
                    if (oparts.CanAppearShop && !ownedOpartsIDs.Contains(oparts.ID))
                    {
                        _curOpartsPool.Add(oparts);
                    }
                }
            }
        }

        public void SetExcludeItemIDs(List<int> ids)
        {
            _excludeItemIDs.Clear();
            if (ids != null)
            {
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
            if (type == typeof(CardData))
            {
                return ExtractItem(_curCardPool, rarity, type);
            }
            else if (type == typeof(OpartsData))
            {
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
            if (filteredItems.Count == 0)
            {
                if (pool.Count == 0)
                {
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
            int dummyID = 000;

            if (type == typeof(CardData))
            {
                return new CardData
                {
                    ID = dummyID,
                    Name = "품절된 카드",
                    Rarity = rarity,
                    Price = ranPrice,
                    Image = _iconDB != null ? _iconDB.GetIcon(dummyID) : null,
                    OffsetX = 0,
                    OffsetY = 0,
                    Scale = 1f,
                };
            }
            return new OpartsData
            {
                ID = dummyID,
                Name = "품절된 오파츠",
                Rarity = rarity,
                Price = ranPrice,
                Image = _iconDB != null ? _iconDB.GetIcon(dummyID) : null,
                OffsetX = 0,
                OffsetY = 0,
                Scale = 1f,
            };
        }
    }
}