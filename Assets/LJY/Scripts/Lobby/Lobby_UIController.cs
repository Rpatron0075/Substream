using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class Lobby_UIController : MonoBehaviour
{
    private bool _isAnimating = false;
    private Button _foldButton;
    private VisualElement _hiddenButtonContainer;

    [SerializeField] private List<string> _popupPannelButtons;
    private Button _exitPannelButton;
    private VisualElement _pannel;

    private Button _popupLineWindowButton;
    private VisualElement _lineWindow;

    [SerializeField] private List<string> _mainContentButtons;
    private Dictionary<string, string> _mainContentScenes = new Dictionary<string, string>();
    private VisualElement _loadingScreen;
    private ProgressBar _loadingProgressBar;
    private SceneLoader sceneLoader;

    void Start()
    {
        VisualElement root = this.gameObject.GetComponent<UIDocument>().rootVisualElement;

        PannelInit(root);
        PopupLineWindowInit(root);
        HiddenContainerInit(root);
        ExitPannelButtonInit(root);
        MainContentInit(root);
        sceneLoader = GameObject.FindGameObjectWithTag("SceneLoader").gameObject.GetComponent<SceneLoader>();
    }

    private void PannelInit(VisualElement root)
    {
        foreach (string name in _popupPannelButtons)
        {
            Button button = root.Q<Button>(name);
            button.RegisterCallback<ClickEvent>(OnPopupWindow);
        }
        _pannel = root.Q<VisualElement>("Window_Container");
        _pannel.style.display = DisplayStyle.None;
    }

    private void PopupLineWindowInit(VisualElement root)
    {
        _popupLineWindowButton = root.Q<Button>("LD_Button");
        _popupLineWindowButton.RegisterCallback<ClickEvent>(OnPopupLine);

        _lineWindow = root.Q<VisualElement>("Line_Window");
        _lineWindow.style.display = DisplayStyle.None;
    }

    private void HiddenContainerInit(VisualElement root)
    {
        _hiddenButtonContainer = root.Q<VisualElement>("Hidden_Button_Container");
        _hiddenButtonContainer.RemoveFromClassList("hidden_button-container_unfold");
        _hiddenButtonContainer.style.display = DisplayStyle.None;
        _hiddenButtonContainer.RegisterCallback<TransitionEndEvent>(OnTransitionEndEvents);

        _foldButton = root.Q<Button>("Fold_Button");
        _foldButton.RegisterCallback<ClickEvent>(OnFoldingButton);
    }

    private void ExitPannelButtonInit(VisualElement root)
    {
        _exitPannelButton = root.Q<Button>("Exit_Pannel_Button");
        _exitPannelButton.RegisterCallback<ClickEvent>(OnExitPannel);
    }

    private void MainContentInit(VisualElement root)
    {
        // ���� ������ ������ �̵��ϴ� ��ư ���� �۾�
        foreach (string name in _mainContentButtons)
        {
            Button button = root.Q<Button>(name);
            button.RegisterCallback<ClickEvent>(OnLoadingScreen);
        }

        // ����� ���� �������� ��ư�� �� ���� ���� �۾�
        List<string> SceneNameList =
            _mainContentButtons.Select(
            button => button.Contains("-") 
            ? button.Substring(0, button.IndexOf("-")) + "Scene" : button + "Scene").ToList();

        foreach (string name in _mainContentButtons)
        {
            _mainContentScenes.Add(name, SceneNameList[_mainContentButtons.IndexOf(name)]);
        }

        _loadingScreen = root.Q<VisualElement>("Loading_Screen");
        _loadingProgressBar = root.Q<ProgressBar>("Loading_Progress_Bar");
        VisualElement gauge = _loadingProgressBar.Q<VisualElement>("unity-progressbar-value");
        if (gauge != null)
        {
            gauge.style.backgroundColor = Color.cyan;
        }
        _loadingScreen.style.display = DisplayStyle.None;

    }

    private void OnFoldingButton(ClickEvent evt)
    {
        if (_isAnimating) return; 
        _isAnimating = true;

        if (_hiddenButtonContainer.ClassListContains("hidden_button-container_unfold"))
        {
            _hiddenButtonContainer.RemoveFromClassList("hidden_button-container_unfold");
        }
        else
        {
            _hiddenButtonContainer.style.display = DisplayStyle.Flex;
            _hiddenButtonContainer.AddToClassList("hidden_button-container_unfold");
        }
    }

    private void OnTransitionEndEvents(TransitionEndEvent evt)
    {
        if (!_hiddenButtonContainer.ClassListContains("hidden_button-container_unfold"))
        {
            _hiddenButtonContainer.style.display = DisplayStyle.None;
        }

        if (!_lineWindow.ClassListContains("line_window-popup"))
            _lineWindow.style.display = DisplayStyle.None;

        _isAnimating = false;
    }

    private void OnPopupWindow(ClickEvent evt)
    {
        _pannel.style.display = DisplayStyle.Flex;
    }

    private void OnExitPannel(ClickEvent evt)
    {
        _pannel.style.display = DisplayStyle.None;
    }

    private void OnPopupLine(ClickEvent evt)
    {
        _lineWindow.style.display = DisplayStyle.Flex;
        _lineWindow.AddToClassList("line_window-popup");

        Invoke("OnPopdownLine", 5f);
    }

    private void OnPopdownLine()
    {
        _lineWindow.RemoveFromClassList("line_window-popup");
    }

    // Ŭ���� ��ư�� �̸�(key)�� �̿��� �ش� �� �̸��� ��ųʸ����� ������ �� �ε� ó�� ����
    private void OnLoadingScreen(ClickEvent evt)
    {
        Button clickedButton = evt.currentTarget as Button;
        if (clickedButton != null)
        {
            if (_mainContentScenes.TryGetValue(clickedButton.name, out string sceneName))
            {
                // �ε� ȭ�� ���̵� �ƿ��� ���α׷����� ������Ʈ�� ������ �ڷ�ƾ ����
                StartCoroutine(LoadSceneWithFade(sceneName));
            }
        }
    }

    // �ε� ȭ�� �� ���α׷����ٸ� ���̵� �ƿ���Ű�� �� �ε� ���൵�� ������Ʈ�ϴ� �ڷ�ƾ
    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        _loadingScreen.style.display = DisplayStyle.Flex;
        _loadingProgressBar.value = 0;
        _loadingScreen.style.opacity = 0;
        _loadingProgressBar.style.opacity = 0;

        float fadeDuration = 1f;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = (timer / fadeDuration);
            _loadingScreen.style.opacity = alpha;
            _loadingProgressBar.style.opacity = alpha;
            yield return null;
        }

        sceneLoader.StartLoadingScene(sceneName);

        while (sceneLoader.GetLoadingProgress() < 1f)
        {
            _loadingProgressBar.value = sceneLoader.GetLoadingProgress();
            yield return null;
        }
    }
}
