using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Audio
{
    /// <summary>소리 분류 (분류별 음량은 sound_map.json의 Volumes).</summary>
    public enum SoundCategory
    {
        Ui,
        Sfx,
        Voice,
        Ambience,
    }

    /// <summary>
    /// 소리 관리자 (M6-5). 소리는 Resources/Audio/ 아래 (원본은 ArtInbox/, 변환 도구는 code_temp의 audio_import.py),
    /// 분류별 음량·이벤트 소리·대사 음성은 Resources/Audio/sound_map.json. 소리가 없으면 아무것도 하지 않는다.
    /// 탐험은 옆에서 보는 화면이라 3D 소리 대신 거리로 줄이고 좌우로 나눈다 (PlayNear).
    /// </summary>
    public static class Sound
    {
        [Serializable]
        private sealed class VolumeEntry
        {
            public string Category = "";
            public float Volume = 1f;
        }

        [Serializable]
        private sealed class EventEntry
        {
            public string Event = "";
            public string Clip = "";
        }

        [Serializable]
        private sealed class LineEntry
        {
            public string Speaker = "";
            public string Starts = "";
            public string Clip = "";
        }

        [Serializable]
        private sealed class SoundMap
        {
            public VolumeEntry[] Volumes = Array.Empty<VolumeEntry>();
            public EventEntry[] Events = Array.Empty<EventEntry>();
            public LineEntry[] Lines = Array.Empty<LineEntry>();
        }

        private const int PoolSize = 10;
        private const string MasterKey = "ApoGirl.Volume.Master";

        private static SoundMap _map;
        private static GameObject _host;
        private static AudioSource[] _pool;
        private static int _next;
        private static AudioSource _voice;
        private static readonly Dictionary<string, AudioSource> Loops = new Dictionary<string, AudioSource>();
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

        /// <summary>전체 음량 (0~1, PlayerPrefs에 저장).</summary>
        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set
            {
                PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
                AudioListener.volume = Mathf.Clamp01(value);
            }
        }

        private static SoundMap Map
        {
            get
            {
                if (_map == null)
                {
                    var asset = Resources.Load<TextAsset>("Audio/sound_map");
                    _map = asset != null ? JsonUtility.FromJson<SoundMap>(asset.text) ?? new SoundMap() : new SoundMap();
                }

                return _map;
            }
        }

        /// <summary>Resources/Audio/{path} (확장자 없이, 예: Sfx/ui_click). 없으면 null.</summary>
        public static AudioClip Clip(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (!Clips.TryGetValue(path, out var clip))
            {
                clip = Resources.Load<AudioClip>("Audio/" + path);
                Clips[path] = clip;
            }

            return clip;
        }

        public static float CategoryVolume(SoundCategory category)
        {
            var name = category.ToString();
            foreach (var entry in Map.Volumes)
            {
                if (entry.Category == name)
                {
                    return entry.Volume;
                }
            }

            return 1f;
        }

        /// <summary>한 번 울린다.</summary>
        public static void Play(string path, SoundCategory category = SoundCategory.Sfx, float volume = 1f, float pan = 0f)
        {
            var clip = Clip(path);
            if (clip == null || !EnsureHost())
            {
                return;
            }

            var source = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume * CategoryVolume(category));
            source.panStereo = Mathf.Clamp(pan, -1f, 1f);
            source.pitch = 1f;
            source.Play();
        }

        /// <summary>주인공에게서 dx만큼 떨어진 곳의 소리: 멀수록 작게, 왼쪽·오른쪽으로 나눠 들린다.</summary>
        public static void PlayNear(string path, float dx, float hearing = 12f, SoundCategory category = SoundCategory.Sfx, float volume = 1f)
        {
            var falloff = Mathf.Clamp01(1f - Mathf.Abs(dx) / Mathf.Max(0.1f, hearing));
            if (falloff < 0.05f)
            {
                return;
            }

            Play(path, category, volume * falloff, Mathf.Clamp(dx / 8f, -0.8f, 0.8f));
        }

        /// <summary>채널 하나에 반복 소리 (같은 소리가 이미 돌고 있으면 음량만 바꾼다).</summary>
        public static void Loop(string channel, string path, SoundCategory category = SoundCategory.Sfx, float volume = 1f)
        {
            var clip = Clip(path);
            if (clip == null || !EnsureHost())
            {
                return;
            }

            if (!Loops.TryGetValue(channel, out var source) || source == null)
            {
                source = NewSource();
                source.loop = true;
                Loops[channel] = source;
            }

            source.volume = Mathf.Clamp01(volume * CategoryVolume(category));
            if (source.clip != clip || !source.isPlaying)
            {
                source.clip = clip;
                source.Play();
            }
        }

        public static void StopLoop(string channel)
        {
            if (Loops.TryGetValue(channel, out var source) && source != null && source.isPlaying)
            {
                source.Stop();
            }
        }

        public static void StopAllLoops()
        {
            foreach (var source in Loops.Values)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
        }

        /// <summary>대사 음성 (하나만, 새 음성이 오면 앞 음성을 끊는다).</summary>
        public static void PlayVoice(string path)
        {
            var clip = Clip(path);
            if (clip == null || !EnsureHost())
            {
                return;
            }

            _voice.Stop();
            _voice.clip = clip;
            _voice.volume = CategoryVolume(SoundCategory.Voice);
            _voice.Play();
        }

        public static void StopVoice()
        {
            if (_voice != null)
            {
                _voice.Stop();
            }
        }

        /// <summary>이벤트가 시작될 때 (sound_map.json의 Events: 습격 경보 등).</summary>
        public static void OnEvent(string eventId)
        {
            foreach (var entry in Map.Events)
            {
                if (entry.Event == eventId)
                {
                    Play(entry.Clip, SoundCategory.Sfx);
                }
            }
        }

        /// <summary>대사 한 줄이 나올 때: 말한 사람·첫 마디가 맞으면 음성 (sound_map.json의 Lines).</summary>
        public static void OnLine(string speaker, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (var entry in Map.Lines)
            {
                if ((string.IsNullOrEmpty(entry.Speaker) || entry.Speaker == speaker) && text.StartsWith(entry.Starts, StringComparison.Ordinal))
                {
                    PlayVoice(entry.Clip);
                    return;
                }
            }
        }

        /// <summary>화면의 버튼을 누르면 딸깍 (클래스 no-click-sound인 버튼은 스스로 소리를 낸다).</summary>
        public static void AttachUiClicks(VisualElement root)
        {
            root?.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target is Button button && button.enabledInHierarchy && !button.ClassListContains("no-click-sound"))
                {
                    Play("Sfx/ui_click", SoundCategory.Ui);
                }
            }, TrickleDown.TrickleDown);
        }

        private static bool EnsureHost()
        {
            if (_host != null)
            {
                return true;
            }

            if (!Application.isPlaying)
            {
                return false;
            }

            _host = new GameObject("Sound");
            UnityEngine.Object.DontDestroyOnLoad(_host);
            _pool = new AudioSource[PoolSize];
            for (var i = 0; i < PoolSize; i++)
            {
                _pool[i] = NewSource();
            }

            _voice = NewSource();
            Loops.Clear();
            AudioListener.volume = MasterVolume;
            return true;
        }

        private static AudioSource NewSource()
        {
            var source = _host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
