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

    private VisualElement _loadingScreen;
    private ProgressBar _loadingProgressBar;

    private float fadeDuration = 1f; // ���̵� ��, �ƿ� ȿ�� ���� �ð�

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
            Debug.LogError("CoroutineManager�� �Ҵ���� �ʾҽ��ϴ�");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // �� �ε� �� �� UI ���� �缳�� �� ���̵� �� ȿ�� ����
    {
        InitializeUI(); // �� ���� ������ ���, �ε� UI�� ���̴� ����(���� 1)�� �غ�� ������ ����
        StartCoroutine(FadeInNewScene()); // ���⼭ ���̵� �� ȿ���� ����
    }

    private void InitializeUI()
    {
        UIDocument uiDocument = GameObject.Find("Manager")?.GetComponent<UIDocument>(); // Manager��� GameObject�� �ִ� UIDocument�� �Ҵ�
        if (uiDocument == null)
        {
            Debug.LogWarning("�� ������ Manager GameObject�� UIDocument�� ã�� �� �����ϴ�");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        _loadingScreen = root.Q<VisualElement>("Loading_Screen");
        _loadingProgressBar = root.Q<ProgressBar>("Loading_Progress_Bar");

        if (_loadingScreen != null) // �⺻ ���� �缳��
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
    /// ���� ������ ���� ������ ��ȯ�ϱ� ���� ���̵� �ƿ� ȿ���� �����ϰ�, �ε� ���൵�� ����
    /// </summary>
    /// <param name="sceneName">��ȯ�� �� �̸�</param>
    public IEnumerator LoadSceneWithFade(string sceneName)
    {
        // ���� ������ �ε� UI�� Ȱ��ȭ�ϰ�, ���̵� �ƿ�(���� 0 -> 1)�� ����
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

        // �ε� UI�� ������ ������������ �� �ε� ����
        StartLoadingScene(sceneName);

        // �ε� ���൵ ������Ʈ (�񵿱� ���൵�� ���� ���α׷����� ������Ʈ)
        while (_loadingProgressBar != null && GetLoadingProgress() < 0.9f)
        {
            _loadingProgressBar.value = _loadingProgressBar.highValue * GetLoadingProgress();
            yield return null;
        }
        _loadingProgressBar.value = 1f;

    }

    // �� �ε� ����
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
    /// �� �� ���� ��, �ε� UI�� ���̵� ���Ͽ� �� ���� ����
    /// </summary>
    private IEnumerator FadeInNewScene()
    {
        if (_loadingScreen == null || _loadingProgressBar == null)
            yield break;

        _loadingScreen.style.display = DisplayStyle.Flex;
        _loadingScreen.style.opacity = 1;
        _loadingProgressBar.style.opacity = 1;
        _loadingProgressBar.value = _loadingProgressBar.highValue;

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
    /// �ε��Ǵ� ���� ���൵�� ��ȯ
    /// </summary>
    private float GetLoadingProgress()
    {
        return asyncLoader != null ? asyncLoader.progress : 0f;
    }
}
