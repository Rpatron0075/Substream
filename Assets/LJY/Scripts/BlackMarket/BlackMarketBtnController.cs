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

        [Header("멤버십 기능")]
        [SerializeField] private string _membershipBtnName = "Btn_Membership";
        [SerializeField] private string _upgradeMembershipBtnName = "Btn_UpgradeMembership";
        [SerializeField] private string _closeMembershipBtnName = "Btn_CloseMembership";

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

        private Button _membershipBtn;
        private Button _upgradeMembershipBtn;
        private Button _closeMembershipBtn;

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

            if (_membershipBtn != null) { _membershipBtn.clicked -= OnClickOpenMembership; _membershipBtn = null; }
            if (_upgradeMembershipBtn != null) { _upgradeMembershipBtn.clicked -= OnClickUpgradeMembership; _upgradeMembershipBtn = null; }
            if (_closeMembershipBtn != null) { _closeMembershipBtn.clicked -= OnClickCloseMembership; _closeMembershipBtn = null; }
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
            _membershipBtn = root.Q<Button>(_membershipBtnName);

            if (_exitBtn != null) _exitBtn.clicked += OnClickExit;
            else Debug.LogWarning($"Button '{_exitBtnName}' 를 찾을 수 없습니다");

            if (_refreshBtn != null) _refreshBtn.clicked += OnRefreshSlots;
            else Debug.LogWarning($"Button '{_refreshBtnName}' 를 찾을 수 없습니다");

            if (_settingBtn != null) _settingBtn.clicked += OnPopUpSettingPanel;
            else Debug.LogWarning($"Button '{_settingBtnName}' 를 찾을 수 없습니다");

            if (_savingsBtn != null) _savingsBtn.clicked += OnClickOpenSavings;
            else Debug.LogWarning($"Button '{_savingsBtn}' 를 찾을 수 없습니다");

            if (_membershipBtn != null) _membershipBtn.clicked += OnClickOpenMembership;
            else Debug.LogWarning($"Button '{_membershipBtn}' 를 찾을 수 없습니다");
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

        public void ConnectMembershipBtnEvt(VisualElement root)
        {
            _upgradeMembershipBtn = root.Q<Button>(_upgradeMembershipBtnName);
            _closeMembershipBtn = root.Q<Button>(_closeMembershipBtnName);

            if (_upgradeMembershipBtn != null) _upgradeMembershipBtn.clicked += OnClickUpgradeMembership;
            if (_closeMembershipBtn != null) _closeMembershipBtn.clicked += OnClickCloseMembership;
        }

        /// <summary>
        /// 메인 화면의 멤버십 버튼 텍스트 갱신
        /// </summary>
        public void UpdateMembershipBtnText(int level)
        {
            if (_membershipBtn != null) {
                _membershipBtn.text = $"멤버십 Lv.{level}";
            }
        }

        /// <summary>
        /// 메인 화면의 저축 버튼 텍스트 갱신
        /// </summary>
        public void UpdateSavingsBtnText(int level)
        {
            if (_savingsBtn != null) {
                _savingsBtn.text = $"저축 Lv.{level}";
            }
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

        private void OnClickOpenMembership() => BlackMarketManager.Instance.OpenMembershipPopup();
        private void OnClickUpgradeMembership() => BlackMarketManager.Instance.UpgradeMembership();
        private void OnClickCloseMembership() => BlackMarketManager.Instance.CloseMembershipPopup();
    }
}