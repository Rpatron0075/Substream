using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �� �ڷ�ƾ�� ���� ���� �� ���� ������ ��� �ڵ� Ŭ����
/// </summary>
public class CoroutineHandle
{
    public Coroutine RunningCoroutine { get; set; }
    public IEnumerator Enumerator { get; set; }
    public bool IsRunning { get; set; }
}

/// <summary>
/// �ڷ�ƾ�� �����ϰ� �����ϴ� �Ŵ��� Ŭ����
/// </summary>
public class CoroutineManager : MonoBehaviour
{
    // �� �ڷ�ƾ�� ���� Ű ������ ����
    private Dictionary<string, CoroutineHandle> coroutineHandles;

    private void Awake()
    {
        coroutineHandles = new Dictionary<string, CoroutineHandle>();
    }

    /// <summary>
    /// �ڷ�ƾ�� �����Ͽ� ������
    /// </summary>
    /// <param name="id">�ڷ�ƾ�� �ĺ��� ���� Ű</param>
    /// <param name="coroutine">������ IEnumerator</param>
    /// <returns>>> �ڷ�ƾ�� ���¸� ���� �ڵ�</returns>
    public CoroutineHandle StartManagedCoroutine(string id, IEnumerator coroutine)
    {
        List<string> keysToRemove = new List<string>();
        foreach (var kvp in coroutineHandles)
        {
            if (!kvp.Value.IsRunning)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (string key in keysToRemove)
        {
            coroutineHandles.Remove(key);
        }

        // �̹� ������ id�� �ڷ�ƾ�� ���� ���̶�� �����մϴ�.
        if (coroutineHandles.ContainsKey(id))
        {
            StopManagedCoroutine(id);
        }

        CoroutineHandle handle = new CoroutineHandle
        {
            Enumerator = coroutine,
            IsRunning = true
        };

        // ���� �ڷ�ƾ���� ���μ� ���� �� ���� ������Ʈ
        handle.RunningCoroutine = StartCoroutine(RunCoroutine(id, coroutine, handle));
        coroutineHandles[id] = handle;
        return handle;
    }

    // �ڷ�ƾ ���� �� �Ϸ�Ǹ� ���¸� ����
    private IEnumerator RunCoroutine(string id, IEnumerator coroutine, CoroutineHandle handle)
    {
        yield return coroutine;
        handle.IsRunning = false;
    }

    /// <summary>
    /// id�� ���� ���� �ڷ�ƾ�� �ߴ�
    /// </summary>
    public void StopManagedCoroutine(string id)
    {
        if (coroutineHandles.TryGetValue(id, out CoroutineHandle handle))
        {
            if (handle.IsRunning)
            {
                StopCoroutine(handle.RunningCoroutine);
                handle.IsRunning = false;
            }
            coroutineHandles.Remove(id);
        }
    }

    /// <summary>
    /// id�� �ڷ�ƾ�� ���� ������ ��ȯ
    /// </summary>
    public CoroutineHandle GetCoroutineHandle(string id)
    {
        if (coroutineHandles.TryGetValue(id, out CoroutineHandle handle))
        {
            return handle;
        }
        return null;
    }
}
