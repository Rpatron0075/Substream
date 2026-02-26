using UnityEngine;
using UnityEngine.UIElements;
using Audio.Controller;

namespace UI.BlackMarket
{
    /// <summary>
    /// 나가기 버튼, 새로고침 버튼, 아이템 버튼 클릭 및 호버링 연결
    /// </summary>
    public class BlackMarketBtnController : MonoBehaviour
    {
        [Header("메인 기능")]
        [SerializeField] private string _exitBtnName;
        [SerializeField] private string _refreshBtnName;
        [SerializeField] private string _settingBtnName;

        [Header("자세히 보기 기능")]
        [SerializeField] private string _buyBtnName;
        [SerializeField] private string _cancelBtnName;

        [Header("저축 기능")]
        [SerializeField] private string _savingsBtnName;
        [SerializeField] private string _depositBtnName;
        [SerializeField] private string _withdrawBtnName;
        [SerializeField] private string _closeSavingsBtnName;

        // ------------------------------------
        private Button _exitBtn;
        private Button _refreshBtn;
        private Button _settingBtn;

        private Button _buyBtn;
        private Button _cancelBtn;

        private Button _savingsBtn;
        private Button _depositBtn;
        private Button _withdrawBtn;
        private Button _closeSavingsBtn;

        // ------------------------------------

        public void OnDisable()
        {
            if (_exitBtn != null) { _exitBtn.clicked -= OnClickExit; }
            if (_refreshBtn != null) { _refreshBtn.clicked -= OnRefreshSlots; }
            if (_settingBtn != null) { _settingBtn.clicked -= OnPopUpSettingPanel; }

            if (_buyBtn != null) { _buyBtn.clicked -= OnClickBuy; }
            if (_cancelBtn != null) { _cancelBtn.clicked -= OnClickCancelPopup; }

            if (_savingsBtn != null) { _savingsBtn.clicked -= OnClickOpenSavings; }
            if (_depositBtn != null) { _depositBtn.clicked -= OnClickDeposit; _depositBtn = null; }
            if (_withdrawBtn != null) { _withdrawBtn.clicked -= OnClickWithdraw; _withdrawBtn = null; }
            if (_closeSavingsBtn != null) { _closeSavingsBtn.clicked -= OnClickCloseSavings; _closeSavingsBtn = null; }
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
            _savingsBtn = root.Q<Button>(_savingsBtnName);

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

            if (_savingsBtn != null) {
                _savingsBtn.clicked += OnClickOpenSavings;
            }
            else Debug.LogWarning($"Button '{_savingsBtn}' 를 찾을 수 없습니다");
        }

        /// <summary>
        /// 동적으로 생성된 구매 팝업의 버튼 이벤트를 연결
        /// </summary>
        public void ConnectPopupBtnEvt(VisualElement root)
        {
            _buyBtn = root.Q<Button>(_buyBtnName);
            _cancelBtn = root.Q<Button>(_cancelBtnName);

            if (_buyBtn != null) _buyBtn.clicked += OnClickBuy;
            else Debug.LogWarning($"Popup Button '{_buyBtnName}' 를 찾을 수 없습니다");

            if (_cancelBtn != null) _cancelBtn.clicked += OnClickCancelPopup;
            else Debug.LogWarning($"Popup Button '{_cancelBtnName}' 를 찾을 수 없습니다");
        }

        public void ConnectSavingsBtnEvt(VisualElement root)
        {
            // 저축 팝업 버튼
            _depositBtn = root.Q<Button>(_depositBtnName);
            _withdrawBtn = root.Q<Button>(_withdrawBtnName);
            _closeSavingsBtn = root.Q<Button>(_closeSavingsBtnName);


            if (_depositBtn != null) _depositBtn.clicked += OnClickDeposit;
            if (_withdrawBtn != null) _withdrawBtn.clicked += OnClickWithdraw;
            if (_closeSavingsBtn != null) _closeSavingsBtn.clicked += OnClickCloseSavings;
        }

        // ---- 이벤트 콜백 ----
        private void OnClickExit() => BlackMarketManager.Instance.CloseBlackMarket();
        private void OnRefreshSlots() => BlackMarketManager.Instance.HandleRefreshRequest();
        private void OnPopUpSettingPanel() => BlackMarketManager.Instance.OpenSettingPanel();

        private void OnClickBuy() => BlackMarketManager.Instance.ConfirmPurchase();
        private void OnClickCancelPopup() => BlackMarketManager.Instance.ClosePurchasePopup();

        private void OnClickOpenSavings() => BlackMarketManager.Instance.OpenSavingsPopup();
        private void OnClickDeposit() => BlackMarketManager.Instance.RequestDeposit();
        private void OnClickWithdraw() => BlackMarketManager.Instance.RequestWithdraw();
        private void OnClickCloseSavings() => BlackMarketManager.Instance.CloseSavingsPopup();
    }
}