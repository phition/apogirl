using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 주인공 입상 조각 고르기 표 (Resources/Art/standing_map.json). 옷·머리·표정·상태·든 것을 무엇으로 고를지 데이터로 둔다.
    /// 조각 그림은 Resources/Art/{Folder}/stand_{조각}.png, 모두 같은 크기의 투명 캔버스라 그대로 겹친다.
    /// </summary>
    [Serializable]
    public sealed class StandingConfig
    {
        [Serializable]
        public sealed class ItemKey
        {
            public string Item = "";
            public string Key = "";
        }

        [Serializable]
        public sealed class SeasonKey
        {
            public string Season = "";
            public string Key = "";
        }

        [Serializable]
        public sealed class WeekKey
        {
            public int FromWeek;
            public string Key = "";
        }

        [Serializable]
        public sealed class FaceRules
        {
            /// <summary>스트레스가 한계의 이 비율을 넘으면 멍한 얼굴.</summary>
            public float BlankStressRatio = 0.9f;
            public int TiredSleepDebt = 3;
            public int TiredFatigue = 70;
            public int SadStress = 60;
            public int SmileStressBelow = 25;
        }

        [Serializable]
        public sealed class MarkRules
        {
            public int DirtyBelow = 50;
            public int ThinSatietyBelow = -50;
            public int MaskSymptom = 2;
        }

        [Serializable]
        public sealed class FaceToPortrait
        {
            public string Face = "";
            public string Portrait = "";
        }

        public string Folder = "Standing";

        /// <summary>겹치는 순서 (아래 → 위): base, outfit, hair, face, mark, bag, weapon, tool.</summary>
        public string[] Order = Array.Empty<string>();
        public ItemKey[] Armor = Array.Empty<ItemKey>();
        public SeasonKey[] SeasonOutfit = Array.Empty<SeasonKey>();
        public WeekKey[] Hair = Array.Empty<WeekKey>();
        public FaceRules Face = new FaceRules();
        public MarkRules Marks = new MarkRules();
        public ItemKey[] Bags = Array.Empty<ItemKey>();
        public ItemKey[] Weapons = Array.Empty<ItemKey>();
        public ItemKey[] Tools = Array.Empty<ItemKey>();

        /// <summary>가방 색 (탐험 그림과 같은 순서: 갈색·파랑·분홍·빨강·노랑·초록). 판의 시드로 하나를 고른다.</summary>
        public string[] BagColors = Array.Empty<string>();

        /// <summary>조각 그림이 없을 때 대신 쓰는 표정 초상 (Portraits/char_seojin_{Portrait}).</summary>
        public FaceToPortrait[] PortraitFallback = Array.Empty<FaceToPortrait>();

        private static StandingConfig _loaded;

        public static StandingConfig Load()
        {
            if (_loaded == null)
            {
                var asset = Resources.Load<TextAsset>("Art/standing_map");
                _loaded = asset != null ? JsonUtility.FromJson<StandingConfig>(asset.text) ?? new StandingConfig() : new StandingConfig();
            }

            return _loaded;
        }
    }

    /// <summary>지금 상태에 맞는 입상 조각 이름들.</summary>
    public sealed class StandingLook
    {
        public string Outfit = "";
        public string Hair = "";
        public string Face = "neutral";
        public readonly List<string> Marks = new List<string>();
        public string Bag = "";
        public int BagColor;
        public string Weapon = "";
        public string Tool = "";

        /// <summary>조각 이름 (stand_ 뒤): 겹치는 칸 하나에 대해, 그 칸이 비면 null.</summary>
        public IEnumerable<string> Pieces(string layer)
        {
            switch (layer)
            {
                case "base": yield return "base"; break;
                case "outfit": if (Outfit != "") yield return "outfit_" + Outfit; break;
                case "hair": if (Hair != "") yield return "hair_" + Hair; break;
                case "face": if (Face != "neutral") yield return "face_" + Face; break;
                case "mark": foreach (var mark in Marks) yield return "mark_" + mark; break;
                case "bag": if (Bag != "") yield return "item_" + Bag; break;
                case "weapon": if (Weapon != "") yield return "item_" + Weapon; break;
                case "tool": if (Tool != "") yield return "item_" + Tool; break;
            }
        }
    }

    /// <summary>입상 조각 고르기 (규칙은 표의 수치를 따른다).</summary>
    public static class StandingRules
    {
        public static StandingLook Choose(GameState state, BalanceConfig balance, StandingConfig config, bool hasWarnings)
        {
            var look = new StandingLook();
            var player = state.Player;
            var loadout = state.Loadout;

            // 옷: 방어구가 있으면 방어구, 없으면 계절 교복
            look.Outfit = Find(config.Armor, loadout.Armor) ?? "";
            if (look.Outfit == "")
            {
                var season = CalendarRules.GetSeason(state.Calendar.Week).ToString();
                foreach (var entry in config.SeasonOutfit)
                {
                    if (entry.Season == season) look.Outfit = entry.Key;
                }
            }

            // 머리: 주가 지날수록 길어진다
            foreach (var entry in config.Hair)
            {
                if (state.Calendar.Week >= entry.FromWeek) look.Hair = entry.Key;
            }

            // 표정: 멍함 → 지침 → 슬픔 → 걱정 → 미소 → 보통
            var face = config.Face;
            var stress = player.Vitals.GetVital(VitalType.Stress);
            var threshold = DerivedStats.StressThreshold(state, balance);
            if (threshold > 0 && stress >= threshold * face.BlankStressRatio) look.Face = "blank";
            else if (player.SleepDebt >= face.TiredSleepDebt || player.Vitals.GetVital(VitalType.Fatigue) >= face.TiredFatigue) look.Face = "tired";
            else if (stress >= face.SadStress) look.Face = "sad";
            else if (hasWarnings) look.Face = "worried";
            else if (stress < face.SmileStressBelow) look.Face = "smile";

            // 상태: 붕대·얼룩·마름·마스크 (상처는 붕대로만)
            var marks = config.Marks;
            if (InjuryRules.IsInjured(state, balance)) look.Marks.Add("bandage_arm");
            if (player.Vitals.GetVital(VitalType.Cleanliness) < marks.DirtyBelow) look.Marks.Add("dirty");
            if (player.Vitals.GetVital(VitalType.Satiety) < marks.ThinSatietyBelow) look.Marks.Add("thin");
            if (InfectionRules.SymptomLevel(state) >= marks.MaskSymptom) look.Marks.Add("mask");

            // 든 것: 가방(색은 판마다), 무기, 손전등
            look.Bag = Find(config.Bags, loadout.Backpack) ?? "";
            look.BagColor = config.BagColors.Length > 0 ? (state.Rng.Seed & 0x7fffffff) % config.BagColors.Length : 0;
            look.Weapon = Find(config.Weapons, loadout.MainWeapon) ?? "";
            look.Tool = Find(config.Tools, loadout.Tool) ?? "";
            return look;
        }

        private static string Find(StandingConfig.ItemKey[] table, string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            foreach (var entry in table)
            {
                if (entry.Item == itemId) return entry.Key;
            }

            return null;
        }
    }

    /// <summary>
    /// 1겹 왼쪽 아래의 주인공 입상. 조각 그림이 있으면 겹쳐 그리고, 아직 없으면 표정 초상으로 대신한다.
    /// 잠깐 다른 표정을 짓게 할 수 있다 (편성 중 놀람, 대성공 때 밝은 얼굴 등).
    /// </summary>
    internal sealed class StandingView
    {
        private readonly VisualElement _host;
        private readonly Func<GameState> _state;
        private readonly Func<BalanceConfig> _balance;
        private readonly StandingConfig _config = StandingConfig.Load();
        private string _reactFace;
        private IVisualElementScheduledItem _reactEnd;
        private bool _lastWarnings;

        public StandingView(VisualElement host, Func<GameState> state, Func<BalanceConfig> balance)
        {
            _host = host;
            _state = state;
            _balance = balance;
        }

        /// <summary>지금 고른 조각 (디버그·테스트용).</summary>
        public StandingLook Current { get; private set; }

        public void Refresh(bool hasWarnings)
        {
            _lastWarnings = hasWarnings;
            var state = _state();
            if (state == null)
            {
                return;
            }

            var look = StandingRules.Choose(state, _balance(), _config, hasWarnings);
            if (!string.IsNullOrEmpty(_reactFace))
            {
                look.Face = _reactFace;
            }

            Current = look;
            _host.Clear();
            var basePiece = Piece("base");
            _host.EnableInClassList("fallback", basePiece == null);
            if (basePiece == null)
            {
                // 조각 그림이 오기 전: 표정 초상으로
                StoryArt.SetContain(_host, Portrait(look.Face));
                return;
            }

            StoryArt.SetCover(_host, null);
            foreach (var layer in _config.Order)
            {
                foreach (var name in look.Pieces(layer))
                {
                    var texture = Piece(name);
                    if (texture == null)
                    {
                        continue;
                    }

                    var element = new VisualElement { pickingMode = PickingMode.Ignore };
                    element.AddToClassList("standing-layer");
                    StoryArt.SetContain(element, texture);
                    if (layer == "bag" && _config.BagColors.Length > 0 && ColorUtility.TryParseHtmlString(_config.BagColors[look.BagColor], out var tint))
                    {
                        element.style.unityBackgroundImageTintColor = tint;
                    }

                    _host.Add(element);
                }
            }
        }

        /// <summary>잠깐 다른 표정 (seconds 뒤 원래대로).</summary>
        public void React(string face, float seconds)
        {
            _reactFace = face;
            _reactEnd?.Pause();
            _reactEnd = _host.schedule.Execute(() =>
            {
                _reactFace = null;
                Refresh(_lastWarnings);
            });
            _reactEnd.ExecuteLater((long)(seconds * 1000));
            Refresh(_lastWarnings);
        }

        private Texture2D Piece(string name) => StoryArt.Load(_config.Folder + "/stand_" + name);

        private Texture2D Portrait(string face)
        {
            var portrait = face;
            foreach (var entry in _config.PortraitFallback)
            {
                if (entry.Face == face) portrait = entry.Portrait;
            }

            return StoryArt.Load("Portraits/char_seojin_" + portrait) ?? StoryArt.Load("Portraits/char_seojin_neutral");
        }
    }
}
