using UnityEngine;
using UnityEngine.UIElements;

public class CharacterWidgetController : MonoBehaviour
{
    private Button _characterButton;
    private VisualElement _characterImage;
    private VisualElement _lineWindow;
    private Label _lblSpeakerName;
    private Label _lblLine;

    private const string POPUP_CLASS = "line_window-popup";

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null) Initialize(uiDocument.rootVisualElement);
    }

    void OnDisable()
    {
        if (_characterButton != null) _characterButton.UnregisterCallback<ClickEvent>(OnPopupLine);
        if (_lineWindow != null) _lineWindow.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
    }

    public void Initialize(VisualElement root)
    {
        _characterButton = root.Q<Button>("LD_Button");
        _characterImage = root.Q<VisualElement>("LD_Character_image");

        _lineWindow = root.Q<VisualElement>("Line_Window");
        _lblSpeakerName = root.Q<Label>("Lbl_SpeakerName");
        _lblLine = root.Q<Label>("Lbl_LineText");

        if (_characterButton != null)
            _characterButton.RegisterCallback<ClickEvent>(OnPopupLine);

        if (_lineWindow != null) {
            _lineWindow.style.display = DisplayStyle.None;
            _lineWindow.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
        }
    }

    /// <summary>
    /// 캐릭터 이미지와 맞춤형 위치/크기를 적용
    /// </summary>
    /// <param name="sprite">변경할 캐릭터 스프라이트</param>
    /// <param name="offsetX">좌우 이동 %값</param>
    /// <param name="offsetY">상하 이동 %값</param>
    /// <param name="scale">확대/축소 배율</param>
    public void SetCharacterImage(Sprite sprite, float offsetX, float offsetY, float scale)
    {
        if (_characterImage == null) return;

        _characterImage.style.backgroundImage = new StyleBackground(sprite);
        _characterImage.style.translate = new StyleTranslate(new Translate(Length.Percent(offsetX), Length.Percent(offsetY), 0));
        _characterImage.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
    }

    public void ShowLine(string speaker, string lineTxt)
    {
        if (_lblSpeakerName != null && _lblLine != null) {
            _lblSpeakerName.text = speaker;
            _lblLine.text = lineTxt;
        }
        TriggerPopup();
    }

    private void OnPopupLine(ClickEvent evt)
    {
        // 여기는 캐릭터 건드리면 출력하는 대사 들어감
        TriggerPopup();
    }

    private void TriggerPopup()
    {
        if (_lineWindow == null) return;

        CancelInvoke(nameof(OnPopdownLine));

        _lineWindow.style.display = DisplayStyle.Flex;

        _lineWindow.schedule.Execute(() => {
            _lineWindow.AddToClassList(POPUP_CLASS);
        }).StartingIn(1);

        Invoke(nameof(OnPopdownLine), 5f);
    }

    private void OnPopdownLine()
    {
        if (_lineWindow != null)
            _lineWindow.RemoveFromClassList(POPUP_CLASS);
    }

    private void OnTransitionEnd(TransitionEndEvent evt)
    {
        if (_lineWindow != null && !_lineWindow.ClassListContains(POPUP_CLASS)) {
            _lineWindow.style.display = DisplayStyle.None;
        }
    }
}