using System;
using System.Collections.Generic;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;
using UnityEngine;

namespace ApoGirl.Presentation.Audio
{
    /// <summary>
    /// 방송반 휴대용 CD 플레이어 (기획 A, A-1 ① 늘 재생). 곡 목록은 Resources/Art/cd_map.json,
    /// 음원은 Resources/Audio/{Track} (없으면 조용히 넘어간다). 고른 곡은 GameState.CdTrack에 남고,
    /// 탐험을 떠나면 멈추고(Pause) 돌아오면 이어진다(Resume).
    /// </summary>
    public static class CdPlayer
    {
        public const string Channel = "cd_music";

        [Serializable]
        public sealed class CdEntry
        {
            public string Id = "";
            public string Location = "";
            public string Track = "";
            public string Color = "";
        }

        [Serializable]
        private sealed class CdMap
        {
            public int Slots = 8;
            public CdEntry[] Cds = Array.Empty<CdEntry>();
        }

        private static CdMap _map;

        /// <summary>타이틀 음악실 기록 (GameApp이 만든다, 없으면 기록하지 않는다).</summary>
        public static MusicRoom Room { get; set; }

        private static CdMap Map
        {
            get
            {
                if (_map == null)
                {
                    var asset = Resources.Load<TextAsset>("Art/cd_map");
                    _map = asset != null ? JsonUtility.FromJson<CdMap>(asset.text) ?? new CdMap() : new CdMap();
                }

                return _map;
            }
        }

        public static int Slots => Math.Max(Map.Slots, Map.Cds.Length);

        public static IReadOnlyList<CdEntry> All => Map.Cds;

        public static CdEntry Find(string id)
        {
            foreach (var entry in Map.Cds)
            {
                if (entry.Id == id) return entry;
            }

            return null;
        }

        public static bool HasClip(string id)
        {
            var entry = Find(id);
            return entry != null && Sound.Clip(entry.Track) != null;
        }

        public static bool IsPlaying(GameState state, string id) => state != null && !string.IsNullOrEmpty(id) && state.CdTrack == id;

        /// <summary>가진 CD를 고르고 튼다. 들은 곡은 음악실에 남는다.</summary>
        public static void Play(GameState state, string id)
        {
            if (state == null || !CdRules.Select(state, id))
            {
                return;
            }

            Room?.Record(id);
            Start(id);
        }

        /// <summary>판 밖(타이틀 음악실)에서 튼다. 상태를 바꾸지 않는다.</summary>
        public static void Preview(string id)
        {
            Start(id);
        }

        public static void Stop(GameState state)
        {
            if (state != null)
            {
                CdRules.Select(state, "");
            }

            Sound.StopLoop(Channel);
        }

        /// <summary>탐험을 떠날 때: 소리만 멈추고 고른 곡은 기억한다.</summary>
        public static void Pause() => Sound.StopLoop(Channel);

        /// <summary>허브로 돌아왔을 때·판을 불러왔을 때: 고른 곡이 있으면 다시 튼다.</summary>
        public static void Resume(GameState state)
        {
            if (state != null && !string.IsNullOrEmpty(state.CdTrack) && CdRules.Has(state, state.CdTrack))
            {
                Start(state.CdTrack);
            }
        }

        private static void Start(string id)
        {
            var entry = Find(id);
            if (entry != null)
            {
                Sound.Loop(Channel, entry.Track, SoundCategory.Music);
            }
        }
    }
}
