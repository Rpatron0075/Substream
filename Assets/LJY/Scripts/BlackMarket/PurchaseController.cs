using Audio.Controller;
using Item;
using Localization;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;

namespace BlackMarket
{
    public class PurchaseController : MonoBehaviour
    {
        public static PurchaseController Instance { get; private set; }

        [Header("Audio Settings")]
        [SerializeField] private string SFX_OPENSLOT = "SFX_OpenSlot";

        // -- UI 캐싱 --
        private VisualElement _purchaseRoot;
        private Label _popupName;
        private Label _popupInfo;
        private Label _popupPrice;
        private VisualElement _popupIcon;
        private VisualElement _popupBackground;

        private Button _btnBuy;
        private Button _btnCancel;

        // -- 상태 변수 --
        private ItemData _selectedItemData;
        private int _selectedSlotIdx = -1;

        private void Awake()
        {
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 매니저가 생성한 UI Root를 넘겨받아 초기화
        /// </summary>
        public void ConnectUI(VisualElement root)
        {
            _purchaseRoot = root;

            _popupName = _purchaseRoot.Q<Label>("Lbl_PopupName");
            _popupInfo = _purchaseRoot.Q<Label>("Lbl_PopupInfo");
            _popupPrice = _purchaseRoot.Q<Label>("Lbl_PopupPrice");
            _popupIcon = _purchaseRoot.Q<VisualElement>("Img_PopupIcon");
            _popupBackground = _purchaseRoot.Q<VisualElement>("Img_PopupIcon");

            // 자신이 담당하는 버튼 이벤트는 스스로 연결
            _btnBuy = _purchaseRoot.Q<Button>("Btn_Buy");
            _btnCancel = _purchaseRoot.Q<Button>("Btn_Cancel");

            if (_btnBuy != null) {
                _btnBuy.clicked -= OnBuyClicked;
                _btnBuy.clicked += OnBuyClicked;
            }
            if (_btnCancel != null) {
                _btnCancel.clicked -= ClosePopup;
                _btnCancel.clicked += ClosePopup;
            }
        }

        /// <summary>
        /// 매니저가 특정 슬롯을 클릭했을 때 호출하여 팝업
        /// </summary>
        public void OpenPopup(int slotIdx, ItemData item)
        {
            _selectedSlotIdx = slotIdx;
            _selectedItemData = item;

            if (_popupName != null) _popupName.text = LocalizationManager.GetText(item.Name);
            if (_popupInfo != null) _popupInfo.text = LocalizationManager.GetText(item.Info);
            if (_popupPrice != null) _popupPrice.text = item.Price.ToString("N0");

            if (_popupIcon != null) {
                _popupIcon.SetImage(item.Image, item.OffsetX, item.OffsetY, item.Scale);
                ItemSlotController.ApplyRarityBorderColor(_popupBackground, item.Rarity);
            }

            _purchaseRoot.ShowPopupFade();
            AudioController.Instance.PlaySFX(SFX_OPENSLOT);
        }

        public void ClosePopup()
        {
            _purchaseRoot?.HidePopupFade();
            _selectedSlotIdx = -1;
            _selectedItemData = null;
        }

        private void OnBuyClicked()
        {
            if (_selectedItemData == null) return;

            // 실제 재화 계산 및 아이템 추가는 Manager에게 위임
            BlackMarketManager.Instance.ProcessPurchase(_selectedSlotIdx, _selectedItemData);
        }

        /// <summary>
        /// 다국어 변경 시 이 컨트롤러가 팝업 번역을 스스로 갱신
        /// </summary>
        public void RefreshTranslation()
        {
            // 팝업이 열려있고 데이터가 있을 때만 갱신
            if (_purchaseRoot != null && _purchaseRoot.style.display == DisplayStyle.Flex && _selectedItemData != null) {
                if (_popupName != null) _popupName.text = LocalizationManager.GetText(_selectedItemData.Name);
                if (_popupInfo != null) _popupInfo.text = LocalizationManager.GetText(_selectedItemData.Info);
            }
        }
    }
}