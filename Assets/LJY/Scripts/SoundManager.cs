using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// �������� ���Ǵ� ���� Ʈ���� ��������
/// <para>>> int ������ ĳ���� �ʿ�</para>
/// <para>>> ���� Ʈ�� �߰��� �ʿ��� ��� SoundManager���� ���� ����</para>
/// </summary>
public enum SoundTrack
{
    b_hover = 0,
    b_clicked = 1,
    backGroundSound = 2,
}

public class SoundManager : MonoBehaviour
{
    [SerializeField] private bool _buttonEffect = false;

    private Dictionary<string, AudioClip> sounds = new Dictionary<string, AudioClip>();
    private List<AudioSource> _audioSource = new List<AudioSource>();

    private void Start()
    {
        GameObject container = GameObject.Find("AudioSourceContainer").gameObject;
        if (_buttonEffect)
        { _audioSource.Add(container.transform.Find("MainContentButtons").GetComponent<AudioSource>()); }
    }

    /// <summary>
    /// UI Element���� �����ϴ� ��ư�� ���� �̺�Ʈ ���� �Լ�
    /// </summary>
    /// <typeparam name="T">>> UI Element���� �����ϴ� �̺�Ʈ Ŭ����</typeparam>
    /// <param name="button">>> UI Element���� �����ϴ� ��ư ������Ʈ</param>
    /// <param name="clip">>> ����� Ŭ��</param>
    public void SetButtonSoundEvent<T>(Button button, AudioClip clip) where T : EventBase
    {
        if (typeof(T) == typeof(PointerEnterEvent))
        {
            button.RegisterCallback<PointerEnterEvent>(PlayHoverSound);
            if (!sounds.ContainsKey(clip.name))
                sounds.Add(clip.name, clip);
        }
        else if (typeof(T) == typeof(ClickEvent))
        {
            button.RegisterCallback<ClickEvent>(PlaySelectSound);
            if (!sounds.ContainsKey(clip.name))
                sounds.Add(clip.name, clip);
        }
    }

    private void PlayHoverSound(PointerEnterEvent evt)
    {
        AudioSource audioSource = GetAudioSource("MainContentButtons");
        if (_audioSource != null)
        {
            audioSource.PlayOneShot(sounds["hover"]);
        }
    }

    private void PlaySelectSound(ClickEvent evt)
    {
        AudioSource audioSource = GetAudioSource("MainContentButtons");
        if (_audioSource != null)
        {
            audioSource.PlayOneShot(sounds["select"]);
        }
    }

    private AudioSource GetAudioSource(string sourceName)
    {
        return _audioSource.Find(src => src.gameObject.name == sourceName);
    }
}
