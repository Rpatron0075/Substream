using UnityEngine;
using UnityEngine.UIElements;

public class CharacterWidgetController : MonoBehaviour
{
    private Button _characterButton;
    private VisualElement _lineWindow;

    private const string POPUP_CLASS = "line_window-popup";

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null) Initialize(uiDocument.rootVisualElement);
    }

    public void Initialize(VisualElement root)
    {
        _characterButton = root.Q<Button>("LD_Button");
        _lineWindow = root.Q<VisualElement>("Line_Window");

        if (_characterButton != null)
            _characterButton.RegisterCallback<ClickEvent>(OnPopupLine);

        if (_lineWindow != null) {
            _lineWindow.style.display = DisplayStyle.None;
            _lineWindow.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
        }
    }

    private void OnPopupLine(ClickEvent evt)
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

    void OnDisable()
    {
        if (_characterButton != null) _characterButton.UnregisterCallback<ClickEvent>(OnPopupLine);
        if (_lineWindow != null) _lineWindow.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
    }
}