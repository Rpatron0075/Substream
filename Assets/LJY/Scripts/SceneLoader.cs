using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private AsyncOperation asyncLoader = null;
    [HideInInspector] public CoroutineHandle coroutineHandle = null;
    [HideInInspector] public CoroutineManager coroutineManager;

    // UI 요소
    private VisualElement _loadingScreen;
    private ProgressBar _loadingProgressBar;

    // 페이드 효과 지속 시간
    private float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeUI();

        if (coroutineManager == null)
        {
            coroutineManager = GetComponent<CoroutineManager>();
        }

        if (coroutineManager == null)
        {
            Debug.LogError("CoroutineManager가 할당되지 않았습니다");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬 로드 후 새 UI 참조 재설정 및 페이드 인 효과 시작
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeUI();
        // 새 씬에 진입할 때는 로딩 UI가 보이는 상태(투명도 1)로 준비된 것으로 가정
        // 여기서 페이드 인 효과를 실행
        StartCoroutine(FadeInNewScene());
    }

    private void InitializeUI()
    {
        // Manager라는 GameObject에 있는 UIDocument를 할당
        UIDocument uiDocument = GameObject.Find("Manager")?.GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogWarning("새 씬에서 Manager GameObject의 UIDocument를 찾을 수 없습니다");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        _loadingScreen = root.Q<VisualElement>("Loading_Screen");
        _loadingProgressBar = root.Q<ProgressBar>("Loading_Progress_Bar");

        // 프로그래스바 게이지 색상 설정
        VisualElement gauge = _loadingProgressBar?.Q<VisualElement>("unity-progressbar-value");
        if (gauge != null)
        {
            gauge.style.backgroundColor = Color.cyan;
        }

        // 기본 상태 재설정
        if (_loadingScreen != null)
        {
            _loadingScreen.style.display = DisplayStyle.None;
            _loadingScreen.style.opacity = 0;
        }
        if (_loadingProgressBar != null)
        {
            _loadingProgressBar.value = 0;
            _loadingProgressBar.style.opacity = 0;
        }
    }

    /// <summary>
    /// 현재 씬에서 다음 씬으로 전환하기 전에 페이드 아웃 효과를 실행하고, 로딩 진행도를 갱신
    /// </summary>
    /// <param name="sceneName">전환할 씬 이름</param>
    public IEnumerator LoadSceneWithFade(string sceneName)
    {
        // 현재 씬에서 로딩 UI를 활성화하고, 페이드 아웃(투명도 0 -> 1)을 실행
        _loadingScreen.style.display = DisplayStyle.Flex;
        _loadingProgressBar.value = 0;
        _loadingScreen.style.opacity = 0;
        _loadingProgressBar.style.opacity = 0;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = timer / fadeDuration;
            _loadingScreen.style.opacity = alpha;
            _loadingProgressBar.style.opacity = alpha;
            yield return null;
        }

        // 로딩 UI가 완전히 불투명해지면 씬 로딩 시작
        StartLoadingScene(sceneName);

        // 로딩 진행도 업데이트 (비동기 진행도에 맞춰 프로그래스바 업데이트)
        while (_loadingProgressBar != null && GetLoadingProgress() < 1f)
        {
            _loadingProgressBar.value = GetLoadingProgress();
            yield return null;
        }
        _loadingProgressBar.value = 1f;

        // 현재 씬에서는 여기서 추가적인 페이드 아웃 처리 없이 씬 전환 후 OnSceneLoaded에서 페이드 인 효과가 실행
    }

    // 씬 로딩 시작 (코루틴 관리)
    private void StartLoadingScene(string name)
    {
        coroutineHandle = coroutineManager.StartManagedCoroutine(name, StartLoading(name));
    }

    private IEnumerator StartLoading(string name)
    {
        asyncLoader = SceneManager.LoadSceneAsync(name);
        if (asyncLoader == null)
            yield break;

        while (!asyncLoader.isDone)
        {
            yield return null;
        }
    }

    /// <summary>
    /// 새 씬 진입 후, 로딩 UI를 페이드 인하여 새 씬을 노출
    /// </summary>
    private IEnumerator FadeInNewScene()
    {
        if (_loadingScreen == null || _loadingProgressBar == null)
            yield break;

        _loadingScreen.style.display = DisplayStyle.Flex;
        _loadingScreen.style.opacity = 1;
        _loadingProgressBar.style.opacity = 1;

        float timer = fadeDuration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            float alpha = timer / fadeDuration;
            _loadingScreen.style.opacity = alpha;
            _loadingProgressBar.style.opacity = alpha;
            yield return null;
        }

        _loadingScreen.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// 로딩되는 씬의 진행도를 반환
    /// </summary>
    private float GetLoadingProgress()
    {
        return asyncLoader != null ? asyncLoader.progress : 0f;
    }
}
