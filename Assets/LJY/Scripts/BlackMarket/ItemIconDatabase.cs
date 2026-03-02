using System.Collections.Generic;
using UnityEngine;

namespace Item
{
    [System.Serializable]
    public struct ItemIconData
    {
        public string Name;
        public int ID;
        public Sprite Icon;
    }

    [CreateAssetMenu(fileName = "ItemIconDatabase", menuName = "BlackMarket/Item Icon Database")]
    public class ItemIconDatabase : ScriptableObject
    {
        [SerializeField] [Tooltip("아이템 ID와 이미지를 매핑")]
        private List<ItemIconData> _iconList = new List<ItemIconData>();

        private Dictionary<int, Sprite> _iconDict;

        /// <summary>
        /// 게임 시작 시 리스트를 딕셔너리로 변환하여 세팅함
        /// </summary>
        public void Initialize()
        {
            _iconDict = new Dictionary<int, Sprite>();
            foreach (var data in _iconList) {
                if (!_iconDict.ContainsKey(data.ID)) {
                    _iconDict.Add(data.ID, data.Icon);
                }
                else {
                    Debug.LogWarning($"[ItemIconDatabase] 중복된 아이템 ID가 존재합니다 : {data.ID}");
                }
            }
        }

        /// <summary>
        /// ID를 기반으로 Sprite를 반환함
        /// </summary>
        public Sprite GetIcon(int id)
        {
            if (_iconDict == null) Initialize();
            if (_iconDict.TryGetValue(id, out Sprite icon)) {
                return icon;
            }

            Debug.LogWarning($"[ItemIconDatabase] ID '{id}'에 해당하는 이미지를 찾을 수 없습니다");
            return null;
        }
    }
}