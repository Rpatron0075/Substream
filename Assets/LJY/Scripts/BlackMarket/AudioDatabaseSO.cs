using System.Collections.Generic;
using UnityEngine;

namespace Audio.Data
{
    [CreateAssetMenu(fileName = "NewAudioDatabase", menuName = "Audio/AudioDB")]
    public class AudioDatabaseSO : ScriptableObject
    {
        [System.Serializable]
        public struct AudioData
        {
            public string ID;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [Tooltip("사용하는 모든 음성을 등록하세요")]
        public List<AudioData> audioList = new List<AudioData>();

        // -- 런타임 변수 --
        private Dictionary<string, AudioData> _audioDict;

        private void Initialize()
        {
            if (_audioDict != null) return;

            _audioDict = new Dictionary<string, AudioData>();
            foreach (var audio in audioList) {
                if (!_audioDict.ContainsKey(audio.ID)) {
                    _audioDict.Add(audio.ID, audio);
                }
            }
        }

        private AudioData Prepare(AudioSource source, string id)
        {
            AudioData data = default;
            if (source == null || string.IsNullOrEmpty(id)) return data;
            if (_audioDict == null) Initialize();

            if (_audioDict.TryGetValue(id, out data) == false) {
                Debug.LogWarning("[ VoiceDatabaseSO ] 등록되지 않은 음성입니다");
            }
            return data;
        }

        public void PlayLoop(AudioSource source, string id)
        {
            AudioData data = Prepare(source, id);
            if (data.clip == null) {
                Debug.LogWarning($"음성을 찾을 수 없음 : {id}");
                return;
            }
            source.clip = data.clip;
            source.loop = true;
            source.Play();
        }

        public void PlayOneShot(AudioSource source, string id)
        {
            AudioData data = Prepare(source, id);
            if (data.clip == null) {
                Debug.LogWarning($"음성을 찾을 수 없음 : {id}");
                return;
            }
            source.PlayOneShot(data.clip, data.volume);
        }

        public float GetAudioVolume(string id)
        {
            AudioData data = default;
            if (_audioDict == null) Initialize();
            if (_audioDict.TryGetValue(id, out data) == false) {
                Debug.LogWarning($"[ 오디오 데이터를 찾을 수 없음 ]\n" +
                    $"  ID : {id}");
                return 0f;
            }
            return data.volume;
        }
    }
}
