using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader
{
    private AsyncOperation asyncLoader = null;
    [HideInInspector] public CoroutineHandle coroutineHandle = null;
    [HideInInspector] public CoroutineManager coroutineManager = new CoroutineManager();

    /// <summary>
    /// �ٸ� �������� �񵿱� ��ȯ�� ������
    /// </summary>
    /// <param name="name">��ȯ�Ǵ� �� �̸�</param>
    public void StartLoadingScene(string name)
    {
        coroutineHandle = coroutineManager.StartManagedCoroutine(name, StartLoading(name));
    }

    private IEnumerator StartLoading(string name)
    {
        asyncLoader = SceneManager.LoadSceneAsync(name);
        if (asyncLoader == null) yield break;

        while (asyncLoader.isDone == false)
        {
            yield return null;
        }
    }

    /// <summary>
    /// �ε��Ǵ� ���� ���൵�� �δ��κ��� ����
    /// </summary>
    /// <returns>>> ���൵</returns>
    public float GetLoadingProgress()
    {
        return asyncLoader.progress;
    }
}
