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

        /// <summary>눈 조각 (깜빡임·시선): 전신 그림마다 Eyes/{blink|gaze_left|gaze_right}_{그림}.png, 자리는 Rect (캔버스 좌표).</summary>
        [Serializable]
        public sealed class EyesConfig
        {
            public string Folder = "Standing/Eyes";
            public float CanvasWidth = 680f;
            public float CanvasHeight = 1024f;
            public float[] Rect = { 318f, 72f, 104f, 56f };
            public float BlinkMin = 2.5f;
            public float BlinkMax = 6f;
            public float BlinkTime = 0.13f;
            public float GazeMin = 9f;
            public float GazeMax = 18f;
            public float GazeTime = 1.3f;
        }

        /// <summary>
        /// 영상 묶음 하나: 전신 그림 Picture가 골라지고 (Face가 비었거나 지금 표정과 같을 때) Idle을 반복하다가
        /// MoveMin~MoveMax초마다 Moves 중 하나를 한 번 (힉스필드 3·4차 문서).
        /// </summary>
        [Serializable]
        public sealed class MotionSet
        {
            public string Picture = "";
            public string Face = "";
            public string Idle = "";
            public string[] Moves = Array.Empty<string>();
            public float MoveMin = 12f;
            public float MoveMax = 24f;
        }

        /// <summary>
        /// 영상에서 뽑은 동작 프레임 (Motion/{동작}/m_##.png, 전신 그림과 같은 자리·비율).
        /// Sets 순서대로 처음 맞는 묶음을 쓴다 (표정을 정한 묶음을 표정 없는 묶음보다 앞에 둔다).
        /// </summary>
        [Serializable]
        public sealed class MotionConfig
        {
            public string Folder = "Standing/Motion";
            public float Fps = 12f;
            public MotionSet[] Sets = Array.Empty<MotionSet>();
        }

        public string Folder = "Standing";
        public EyesConfig Eyes = new EyesConfig();
        public MotionConfig Motion = new MotionConfig();

        /// <summary>
        /// "Layers" = 같은 캔버스 조각을 겹친다. "Whole" = 한 가지씩 바뀐 전신 그림 중 하나를 고른다
        /// (WholePriority 순서, 지금: mark 상태 → outfit 옷 → react 잠깐 표정 → face 표정 → hair 머리).
        /// </summary>
        public string Mode = "Layers";
        public string[] WholePriority = Array.Empty<string>();

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

        /// <summary>
        /// 그림을 빌려 쓰는 아이템 (제작 C10): 아이템 ID → 그림 이름 조각 (ITEM_WEAPON_CLUB → bat).
        /// 탐험·단면도 주인공 그림의 옷·무기·가방 토큰이 이 표를 먼저 본다 (LookToken).
        /// </summary>
        public ItemKey[] ItemLooks = Array.Empty<ItemKey>();

        /// <summary>가방 색 (탐험 그림과 같은 순서: 갈색·파랑·분홍·빨강·노랑·초록). 판의 시드로 하나를 고른다.</summary>
        public string[] BagColors = Array.Empty<string>();

        /// <summary>조각 그림이 없을 때 대신 쓰는 표정 초상 (Portraits/char_seojin_{Portrait}).</summary>
        public FaceToPortrait[] PortraitFallback = Array.Empty<FaceToPortrait>();

        private static StandingConfig _loaded;

        /// <summary>방어구가 없을 때 입는 계절 교복 키 (SeasonOutfit, 없으면 빈 문자열). 탐험·단면도 주인공 그림도 이 키를 쓴다.</summary>
        public string SeasonOutfitKey(int week)
        {
            var season = CalendarRules.GetSeason(week).ToString();
            foreach (var entry in SeasonOutfit)
            {
                if (entry.Season == season) return entry.Key;
            }

            return "";
        }

        /// <summary>주인공 픽셀 그림의 옷 이름: 방어구가 있으면 그것, 없으면 계절 교복, 그것도 없으면 uniform (동복).</summary>
        public static string PlayerOutfitToken(string armorId, int week)
        {
            var token = LookToken(armorId, "ITEM_ARMOR_", "");
            if (token != "")
            {
                return token;
            }

            var key = Load().SeasonOutfitKey(week);
            return key != "" ? key : "uniform";
        }

        /// <summary>
        /// 아이템 ID에서 그림 이름 조각: ItemLooks 표에 있으면 그것(빌린 그림), 없으면 접두어를 뗀 이름
        /// (ITEM_WEAPON_BAT → bat). 접두어가 맞지 않거나 비면 fallback.
        /// </summary>
        public static string LookToken(string itemId, string itemPrefix, string fallback)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return fallback;
            }

            foreach (var entry in Load().ItemLooks)
            {
                if (entry.Item == itemId) return entry.Key;
            }

            return itemId.StartsWith(itemPrefix, StringComparison.Ordinal) ? itemId.Substring(itemPrefix.Length).ToLowerInvariant() : fallback;
        }

        /// <summary>전신 그림과 지금 표정에 맞는 영상 묶음 (없으면 null: 눈 깜빡임·시선만).</summary>
        public MotionSet PickMotion(string picture, string face)
        {
            foreach (var set in Motion.Sets)
            {
                if (set.Picture == picture && (string.IsNullOrEmpty(set.Face) || set.Face == face)) return set;
            }

            return null;
        }

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
                look.Outfit = config.SeasonOutfitKey(state.Calendar.Week);
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

        // ── 살아 있는 입상: 눈 깜빡임·시선, 동작 프레임 ──
        private readonly VisualElement _eyes = new VisualElement { pickingMode = PickingMode.Ignore };
        private readonly Dictionary<string, List<Texture2D>> _clips = new Dictionary<string, List<Texture2D>>();
        private readonly System.Random _random = new System.Random();
        private string _picture = "";
        private float _nextBlink;
        private float _nextGaze;
        private float _eyesUntil;
        private bool _eyesShown;
        private float _nextMove;
        private string _clip;
        private float _clipStart;
        private StandingConfig.MotionSet _set;

        public StandingView(VisualElement host, Func<GameState> state, Func<BalanceConfig> balance)
        {
            _host = host;
            _state = state;
            _balance = balance;
            _eyes.AddToClassList("standing-eyes");
            _eyes.style.position = Position.Absolute;
            _eyes.style.display = DisplayStyle.None;
            _host.RegisterCallback<GeometryChangedEvent>(_ => PlaceEyes());
            var now = Now;
            _nextBlink = now + Range(_config.Eyes.BlinkMin, _config.Eyes.BlinkMax);
            _nextGaze = now + Range(_config.Eyes.GazeMin, _config.Eyes.GazeMax);
            _host.schedule.Execute(Tick).Every(40);
        }

        /// <summary>지금 보이는 전신 그림 이름 (stand_ 뒤, 디버그·테스트용).</summary>
        public string Picture => _picture;

        /// <summary>지금 재생 중인 동작 (없으면 null).</summary>
        public string PlayingClip => _clip;

        /// <summary>디버그: 동작 하나를 바로 재생.</summary>
        public void PlayMove(string clip)
        {
            if (Clip(clip) != null)
            {
                _clip = clip;
                _clipStart = Now;
            }
        }

        /// <summary>디버그: 눈 조각 하나를 잠깐 보인다 (blink, gaze_left, gaze_right).</summary>
        public void ShowEyesNow(string kind, float seconds) => ShowEyes(kind, seconds, Now);

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

            if (_config.Mode == "Whole")
            {
                // 한 장 고르기: 우선순위에서 처음으로 그림이 있는 것, 없으면 기본 입상
                var picture = WholePicture(look, out var pictureName);
                _picture = picture != null ? pictureName : "base";
                StoryArt.SetContain(_host, picture ?? basePiece);
                HideEyes();
                _host.Add(_eyes);
                PlaceEyes();
                UseMotion(_config.PickMotion(_picture, look.Face));
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

        private Texture2D WholePicture(StandingLook look, out string pictureName)
        {
            pictureName = null;
            foreach (var kind in _config.WholePriority)
            {
                IEnumerable<string> names;
                switch (kind)
                {
                    case "react": names = string.IsNullOrEmpty(_reactFace) ? Array.Empty<string>() : new[] { "face_" + _reactFace }; break;
                    case "mark": names = look.Pieces("mark"); break;
                    case "outfit": names = look.Pieces("outfit"); break;
                    case "face": names = look.Pieces("face"); break;
                    case "hair": names = look.Pieces("hair"); break;
                    default: names = Array.Empty<string>(); break;
                }

                foreach (var name in names)
                {
                    var texture = Piece(name);
                    if (texture != null)
                    {
                        pictureName = name;
                        return texture;
                    }
                }
            }

            return null;
        }

        // ── 살아 있는 입상 ──

        private static float Now => Time.realtimeSinceStartup;

        private float Range(float min, float max) => min + (float)_random.NextDouble() * Math.Max(0f, max - min);

        private bool HasMotion => _config.Mode == "Whole" && _set != null && Clip(_set.Idle) != null;

        /// <summary>영상 묶음이 바뀌면 처음부터 다시 재생하고, 이제 안 쓰는 동작 프레임은 메모리에서 놓는다.</summary>
        private void UseMotion(StandingConfig.MotionSet set)
        {
            if (set == _set)
            {
                return;
            }

            _set = set;
            _clip = null;
            _nextMove = Now + (set != null ? Range(set.MoveMin, set.MoveMax) : 0f);
            var keep = new HashSet<string>();
            if (set != null)
            {
                keep.Add(set.Idle);
                foreach (var move in set.Moves) keep.Add(move);
            }

            var drop = new List<string>();
            foreach (var name in _clips.Keys)
            {
                if (!keep.Contains(name)) drop.Add(name);
            }

            foreach (var name in drop)
            {
                foreach (var frame in _clips[name]) Resources.UnloadAsset(frame);
                _clips.Remove(name);
            }
        }

        private List<Texture2D> Clip(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (!_clips.TryGetValue(name, out var frames))
            {
                frames = new List<Texture2D>();
                for (var i = 0; i < 500; i++)
                {
                    var frame = StoryArt.Load(_config.Motion.Folder + "/" + name + "/m_" + i.ToString("00"));
                    if (frame == null) break;
                    frames.Add(frame);
                }

                _clips[name] = frames;
            }

            return frames.Count > 0 ? frames : null;
        }

        private void Tick()
        {
            if (_config.Mode != "Whole" || _host.panel == null || _host.ClassListContains("fallback"))
            {
                return;
            }

            var now = Now;
            if (HasMotion)
            {
                HideEyes();
                TickMotion(now);
                return;
            }

            TickEyes(now);
        }

        private void TickMotion(float now)
        {
            var motion = _set;
            if (_clip == null)
            {
                _clip = motion.Idle;
                _clipStart = now;
            }

            if (_clip == motion.Idle && now >= _nextMove && motion.Moves.Length > 0)
            {
                var move = motion.Moves[_random.Next(motion.Moves.Length)];
                if (Clip(move) != null)
                {
                    _clip = move;
                    _clipStart = now;
                }

                _nextMove = now + Range(motion.MoveMin, motion.MoveMax);
            }

            var frames = Clip(_clip) ?? Clip(motion.Idle);
            var index = (int)((now - _clipStart) * Math.Max(1f, _config.Motion.Fps));
            if (_clip != motion.Idle && index >= frames.Count)
            {
                // 한 번 하는 동작이 끝나면 숨쉬기로
                _clip = motion.Idle;
                _clipStart = now;
                frames = Clip(_clip);
                index = 0;
            }

            StoryArt.SetContain(_host, frames[index % frames.Count]);
        }

        private void TickEyes(float now)
        {
            var eyes = _config.Eyes;
            if (_eyesShown && now >= _eyesUntil)
            {
                HideEyes();
            }

            if (now >= _nextBlink)
            {
                _nextBlink = now + Range(eyes.BlinkMin, eyes.BlinkMax);
                if (!_eyesShown) ShowEyes("blink", eyes.BlinkTime, now);
            }
            else if (now >= _nextGaze && !_eyesShown)
            {
                _nextGaze = now + Range(eyes.GazeMin, eyes.GazeMax);
                ShowEyes(_random.Next(2) == 0 ? "gaze_left" : "gaze_right", eyes.GazeTime, now);
                // 곁눈질이 끝난 뒤에 깜빡이게
                _nextBlink = Math.Max(_nextBlink, now + eyes.GazeTime + 0.3f);
            }
        }

        private void ShowEyes(string kind, float seconds, float now)
        {
            var texture = StoryArt.Load(_config.Eyes.Folder + "/" + kind + "_" + _picture);
            if (texture == null)
            {
                return;
            }

            _eyes.style.backgroundImage = new StyleBackground(texture);
            _eyes.style.display = DisplayStyle.Flex;
            _eyesShown = true;
            _eyesUntil = now + seconds;
            PlaceEyes();
        }

        private void HideEyes()
        {
            _eyes.style.display = DisplayStyle.None;
            _eyesShown = false;
        }

        /// <summary>눈 조각 자리: 입상 그림은 칸 안에 비율을 지켜 가운데 맞춤(contain)으로 그려진다.</summary>
        private void PlaceEyes()
        {
            var eyes = _config.Eyes;
            var width = _host.resolvedStyle.width;
            var height = _host.resolvedStyle.height;
            if (float.IsNaN(width) || width <= 0 || eyes.Rect == null || eyes.Rect.Length < 4)
            {
                return;
            }

            var scale = Math.Min(width / eyes.CanvasWidth, height / eyes.CanvasHeight);
            var left = (width - eyes.CanvasWidth * scale) / 2f;
            var top = (height - eyes.CanvasHeight * scale) / 2f;
            _eyes.style.left = left + eyes.Rect[0] * scale;
            _eyes.style.top = top + eyes.Rect[1] * scale;
            _eyes.style.width = eyes.Rect[2] * scale;
            _eyes.style.height = eyes.Rect[3] * scale;
        }

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
