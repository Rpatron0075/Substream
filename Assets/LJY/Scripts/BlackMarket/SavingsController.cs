using System;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;

namespace BlackMarket
{
    public class SavingsController : MonoBehaviour
    {
        public event Action<int> OnDepositRequested;
        public event Action<int> OnWithdrawRequested;

        // -- UI 캐싱 --
        private VisualElement _savingsRoot;
        private Label _lblSavingsLevel;
        private Label _lblTotalSavings;
        private SliderInt _sldAmount;
        private Label _lblSelectedAmount;

        private Button _btnDeposit;
        private Button _btnWithdraw;
        private Button _btnClose;

        /// <summary>
        /// 매니저가 생성한 UI Root를 넘겨받아 초기화
        /// </summary>
        public void ConnectUI(VisualElement root)
        {
            _savingsRoot = root;

            // UI 요소 찾기
            _lblSavingsLevel = _savingsRoot.Q<Label>("Lbl_SavingsLevel");
            _lblTotalSavings = _savingsRoot.Q<Label>("Lbl_TotalSavings");
            _sldAmount = _savingsRoot.Q<SliderInt>("Sld_Amount");
            _lblSelectedAmount = _savingsRoot.Q<Label>("Lbl_SelectedAmount");

            _btnDeposit = _savingsRoot.Q<Button>("Btn_Deposit");
            _btnWithdraw = _savingsRoot.Q<Button>("Btn_Withdraw");
            _btnClose = _savingsRoot.Q<Button>("Btn_CloseSavings");

            // 슬라이더 이벤트 연결 (100단위 스냅 기능 유지)
            if (_sldAmount != null && _lblSelectedAmount != null) {
                _sldAmount.RegisterValueChangedCallback(evt => {
                    int snappedValue = Mathf.RoundToInt(evt.newValue / 100f) * 100;
                    if (snappedValue != evt.newValue) {
                        _sldAmount.SetValueWithoutNotify(snappedValue);
                    }
                    _lblSelectedAmount.text = $"{snappedValue:N0} G";
                });
            }

            // 버튼 이벤트 연결
            if (_btnDeposit != null) {
                _btnDeposit.clicked -= OnDepositClicked;
                _btnDeposit.clicked += OnDepositClicked;
            }
            if (_btnWithdraw != null) {
                _btnWithdraw.clicked -= OnWithdrawClicked;
                _btnWithdraw.clicked += OnWithdrawClicked;
            }
            if (_btnClose != null) {
                _btnClose.clicked -= ClosePopup;
                _btnClose.clicked += ClosePopup;
            }
        }

        public void OpenPopup(int curLevel, int totalSavings)
        {
            UpdateUI(curLevel, totalSavings);
            _savingsRoot?.ShowPopupFade();
        }

        public void ClosePopup()
        {
            _savingsRoot?.HidePopupFade();
        }

        /// <summary>
        /// 입출금 발생 시 UI(레벨, 총액) 갱신
        /// </summary>
        public void UpdateUI(int curLevel, int totalSavings)
        {
            if (_lblSavingsLevel != null) _lblSavingsLevel.text = $"Lv. {curLevel}";
            if (_lblTotalSavings != null) _lblTotalSavings.text = $"{totalSavings:N0} G";
        }

        /// <summary>
        /// 매니저에게 실제 입금 요청
        /// </summary>
        public void OnDepositClicked()
        {
            if (_sldAmount == null) return;
            OnDepositRequested?.Invoke(_sldAmount.value);
        }

        /// <summary>
        /// 매니저에게 실제 인출 요청
        /// </summary>
        public void OnWithdrawClicked()
        {
            if (_sldAmount == null) return;
            OnWithdrawRequested?.Invoke(_sldAmount.value);
        }
    }
}