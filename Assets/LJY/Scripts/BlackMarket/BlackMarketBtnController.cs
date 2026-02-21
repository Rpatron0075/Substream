using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.BlackMarket
{
    /// <summary>
    /// 나가기 버튼, 새로고침 버튼, 아이템 버튼 클릭 및 호버링 연결
    /// </summary>
    public class BlackMarketBtnController : MonoBehaviour
    {
        [SerializeField] private string _exitBtnName;
        [SerializeField] private string _refreshBtnName;
        [SerializeField] private string _settingBtnName;

        private Button _exitBtn;
        private Button _refreshBtn;
        private Button _settingBtn;


        public void OnDisable()
        {
            if (_exitBtn != null) {
                _exitBtn.clicked -= OnClickExit;
            }

            if (_refreshBtn != null) {
                _refreshBtn.clicked -= OnRefreshSlots;
            }

            if (_settingBtn != null) {
                _settingBtn.clicked -= OnPopUpSettingPanel;
            }
        }

        /// <summary>
        /// UI가 생성된 후 Manager에 의해 호출되어 버튼을 연결
        /// </summary>
        /// <param name="root">블랙마켓 UI의 루트 요소</param>
        public void ConnectButtonEvt(VisualElement root)
        {
            _exitBtn = root.Q<Button>(_exitBtnName);
            _refreshBtn = root.Q<Button>(_refreshBtnName);
            _settingBtn = root.Q<Button>(_settingBtnName);

            if (_exitBtn != null) {
                _exitBtn.clicked += OnClickExit;
            }
            else Debug.LogWarning($"Button '{_exitBtnName}' 를 찾을 수 없습니다");

            if (_refreshBtn != null) {
                _refreshBtn.clicked += OnRefreshSlots;
            }
            else Debug.LogWarning($"Button '{_refreshBtnName}' 를 찾을 수 없습니다");

            if (_settingBtn != null) {
                _settingBtn.clicked += OnPopUpSettingPanel;
            }
            else Debug.LogWarning($"Button '{_settingBtnName}' 를 찾을 수 없습니다");
        }

        private void OnClickExit()
        {
            BlackMarketManager.Instance.CloseBlackMarket();
        }

        private void OnRefreshSlots()
        {
            BlackMarketManager.Instance.StartMarket();

            // 슬롯 UI 업데이트 진행
        }

        private void OnPopUpSettingPanel()
        {
            BlackMarketManager.Instance.OpenSettingPanel();
        }
    }
}