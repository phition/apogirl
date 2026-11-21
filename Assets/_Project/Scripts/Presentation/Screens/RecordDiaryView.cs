using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>생활기록부 다이어리 설정 (Resources/Art/record_diary.json). 그림 경로는 Resources/Art 아래.</summary>
    [Serializable]
    public sealed class RecordDiaryConfig
    {
        [Serializable]
        public sealed class VitalBand
        {
            public string Vital = "";

            /// <summary>값(스트레스는 한계 대비 %)이 이 이상이면 이 말. 큰 것부터.</summary>
            public float From;
            public string Key = "";

            /// <summary>글자 색: ok / warn / danger.</summary>
            public string Tone = "ok";
        }

        public string Page = "UI/ui_paper_diary_page";
        public string Polaroid = "UI/Paper/ui_paper_polaroid";

        /// <summary>폴라로이드 틀 안의 사진 자리 (왼·위·오른·아래, 0~1).</summary>
        public float[] PhotoRect = { 0.052f, 0.055f, 0.948f, 0.765f };
        public string Tape = "UI/Paper/ui_paper_tape";
        public string NoteBody = "UI/Paper/ui_paper_note_yellow";
        public string NoteStats = "UI/Paper/ui_paper_note_blue";
        public string Circle = "UI/Paper/ui_mark_circle_red";
        public string Underline = "UI/Paper/ui_mark_underline_red";

        /// <summary>능력치 칸: 이 값이면 칸이 다 찬다, 칸 수. 이번 주에 이만큼 오르면 빨간 동그라미.</summary>
        public int StatBoxes = 10;
        public int StatBoxMax = 250;
        public int DeltaMark = 3;

        /// <summary>사진으로 고를 이벤트 종류 순서 (앞일수록 중요).</summary>
        public string[] PhotoKinds = { "Story", "Calendar", "Night", "Random" };

        public VitalBand[] Vitals = Array.Empty<VitalBand>();

        private static RecordDiaryConfig _loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLoaded() => _loaded = null;

        public static RecordDiaryConfig Load()
        {
            if (_loaded == null)
            {
                var asset = Resources.Load<TextAsset>("Art/record_diary");
                _loaded = asset != null ? JsonUtility.FromJson<RecordDiaryConfig>(asset.text) ?? new RecordDiaryConfig() : new RecordDiaryConfig();
            }

            return _loaded;
        }
    }

    /// <summary>
    /// 생활기록부 서랍 (다이어리): 맨 위에 주차·날짜, 지난달 가장 중요했던 이벤트 컷을 폴라로이드 사진으로,
    /// 노란 쪽지 "오늘의 몸"(수치를 말로), 파란 쪽지 "나는 지금 이만큼"(능력치 칸 막대·이번 주 오른 것 빨간 동그라미),
    /// "모은 스티커"(숙련도), 맨 아래 "선생님의 한마디". 능력치·숙련에 마우스를 올리면 어디에 쓰이는지 힌트가 뜬다.
    /// </summary>
    internal sealed class RecordDiaryView
    {
        private static readonly VitalType[] BodyVitals =
        {
            VitalType.Hp, VitalType.Fatigue, VitalType.Stress, VitalType.Cleanliness, VitalType.Satiety, VitalType.Hydration,
        };

        private readonly GameSession _session;
        private readonly RecordDiaryConfig _config = RecordDiaryConfig.Load();
        private readonly VisualElement _drawer;
        private readonly VisualElement _panel;
        private readonly Label _hint;

        public RecordDiaryView(VisualElement root, GameSession session)
        {
            _session = session;
            _drawer = root.Q("drawer-left");
            _panel = root.Q("status-panel");
            if (_panel is ScrollView scroll)
            {
                scroll.mode = ScrollViewMode.Vertical;
                scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            }

            _drawer.AddToClassList("diary");
            var page = StoryArt.Load(_config.Page);
            if (page != null)
            {
                _drawer.style.backgroundImage = new StyleBackground(page);
                _drawer.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(Length.Percent(100), Length.Percent(100)));
            }

            _hint = new Label { pickingMode = PickingMode.Ignore };
            _hint.AddToClassList("diary-hint");
            _hint.AddToClassList("hidden");
            _drawer.Add(_hint);
        }

        public void Refresh()
        {
            var state = _session.State;
            if (state == null)
            {
                return;
            }

            _panel.Clear();
            HideHint();
            var balance = _session.Definitions.Balance;
            var calendar = state.Calendar;
            var date = CalendarRules.DateOf(calendar.Week, Math.Max(1, calendar.Day), balance);
            Add(_panel, Loc.F("diary.date", calendar.Week, UiText.Season(CalendarRules.GetSeason(calendar.Week)), date.Month, date.Day), "diary-date");

            // ── 위: 사진(지난달 한 장면) + 오늘의 몸 ──
            var top = new VisualElement();
            top.AddToClassList("diary-top");
            _panel.Add(top);
            var photo = BuildPhoto(state, balance);
            if (photo != null)
            {
                top.Add(photo);
            }

            top.Add(BuildBody(state, balance));

            // ── 나는 지금 이만큼 (능력치) ──
            if (Disclosure.Has(state, "UI_STATS"))
            {
                _panel.Add(BuildStats(state, balance));
            }

            // ── 모은 스티커 (숙련도) ──
            if (Disclosure.Has(state, "UI_PROFICIENCY"))
            {
                _panel.Add(BuildStickers(state));
            }

            // ── 선생님의 한마디 ──
            if (_session.HasMetTeacher)
            {
                var comment = TeacherNote(state);
                if (!string.IsNullOrEmpty(comment))
                {
                    var box = new VisualElement();
                    box.AddToClassList("diary-teacher");
                    Add(box, Loc.T("diary.teacher"), "diary-teacher-head");
                    Add(box, StoryText.Format(comment, state), "diary-teacher-text");
                    var underline = StoryArt.Load(_config.Underline);
                    if (underline != null)
                    {
                        var mark = new VisualElement { pickingMode = PickingMode.Ignore };
                        mark.AddToClassList("diary-underline");
                        mark.style.backgroundImage = new StyleBackground(underline);
                        box.Add(mark);
                    }

                    _panel.Add(box);
                }
            }

            KoreanFont.ApplyDeep(_drawer);
        }

        /// <summary>
        /// 생활기록부의 "행동 특성 및 종합 의견"을 선생님 말투로: 이번 주에 가장 많이 오른 능력치가 있으면 그것을,
        /// 없으면 지금 가장 높은 능력치를 두고 한 줄 (문구는 diary.teacher.grew.* / diary.teacher.best.*).
        /// </summary>
        private string TeacherNote(GameState state)
        {
            var delta = _session.LastWeekSummary?.Delta;
            if (delta != null && delta.Stats.Count > 0)
            {
                var grew = delta.Stats.OrderByDescending(p => p.Value).First();
                if (grew.Value >= _config.DeltaMark)
                {
                    var text = Loc.Lookup(Loc.UiTable, "diary.teacher.grew." + grew.Key);
                    if (!string.IsNullOrEmpty(text)) return text;
                }
            }

            var best = Enum.GetValues(typeof(StatType)).Cast<StatType>().OrderByDescending(s => state.Player.GetStat(s)).First();
            return Loc.Lookup(Loc.UiTable, "diary.teacher.best." + best) ?? Loc.T("diary.teacher.default");
        }

        // ── 사진 ──

        /// <summary>지난달(달력 기준)에 본 이벤트 중 그림이 있는 가장 중요한 것. 없으면 그 전에 본 것 중에서.</summary>
        private VisualElement BuildPhoto(GameState state, BalanceConfig balance)
        {
            var eventId = PickPhotoEvent(state, balance, out var month);
            var texture = string.IsNullOrEmpty(eventId) ? null : StoryArt.EventImage(eventId);
            if (texture == null)
            {
                return null;
            }

            var frame = new VisualElement();
            frame.AddToClassList("diary-polaroid");
            var frameArt = StoryArt.Load(_config.Polaroid);
            if (frameArt != null)
            {
                frame.style.backgroundImage = new StyleBackground(frameArt);
            }

            var r = _config.PhotoRect;
            var picture = new VisualElement { pickingMode = PickingMode.Ignore };
            picture.AddToClassList("diary-photo");
            picture.style.left = Length.Percent(r[0] * 100f);
            picture.style.top = Length.Percent(r[1] * 100f);
            picture.style.right = Length.Percent((1f - r[2]) * 100f);
            picture.style.bottom = Length.Percent((1f - r[3]) * 100f);
            StoryArt.SetCover(picture, texture);
            frame.Add(picture);
            var caption = Add(frame, Loc.F("diary.photo", month), "diary-photo-caption");
            caption.style.top = Length.Percent(r[3] * 100f + 2f);

            var tape = StoryArt.Load(_config.Tape);
            if (tape != null)
            {
                var strip = new VisualElement { pickingMode = PickingMode.Ignore };
                strip.AddToClassList("diary-tape");
                strip.style.backgroundImage = new StyleBackground(tape);
                frame.Add(strip);
            }

            return frame;
        }

        private string PickPhotoEvent(GameState state, BalanceConfig balance, out int month)
        {
            var week = state.Calendar.Week;
            var current = CalendarRules.DateOf(week, 1, balance).Month;
            month = current == 1 ? 12 : current - 1;
            var target = month;
            var weeks = new HashSet<int>(Enumerable.Range(1, Math.Max(0, week - 1)).Where(w => CalendarRules.DateOf(w, 1, balance).Month == target));

            var seen = new List<(EventDef Def, int Week)>();
            foreach (var ev in _session.Definitions.Events)
            {
                if (state.Events.Records.TryGetValue(ev.Id, out var record) && record.Count > 0 && record.LastWeek < week
                    && StoryArt.EventImage(ev.Id) != null)
                {
                    seen.Add((ev, record.LastWeek));
                }
            }

            int Rank(EventDef ev)
            {
                var index = Array.IndexOf(_config.PhotoKinds, ev.Kind.ToString());
                return index < 0 ? _config.PhotoKinds.Length : index;
            }

            var best = seen.Where(s => weeks.Contains(s.Week)).OrderBy(s => Rank(s.Def)).ThenByDescending(s => s.Def.Priority).ThenByDescending(s => s.Week)
                .FirstOrDefault();
            if (best.Def == null)
            {
                // 지난달에 그림 있는 일이 없으면 그 전에 본 것 중 가장 최근의 중요한 장면
                best = seen.OrderByDescending(s => s.Week).ThenBy(s => Rank(s.Def)).ThenByDescending(s => s.Def.Priority).FirstOrDefault();
                if (best.Def != null)
                {
                    month = CalendarRules.DateOf(best.Week, 1, balance).Month;
                }
            }

            return best.Def?.Id;
        }

        // ── 오늘의 몸 ──

        private VisualElement BuildBody(GameState state, BalanceConfig balance)
        {
            var note = Note(_config.NoteBody, "diary-note-body", Loc.T("diary.body"));
            var player = state.Player;
            var threshold = Math.Max(1, DerivedStats.StressThreshold(state, balance));
            foreach (var vital in BodyVitals)
            {
                if (vital == VitalType.Cleanliness && !Disclosure.Has(state, "UI_RESOURCES") && state.Calendar.Week < 2)
                {
                    continue;
                }

                var value = player.Vitals.GetVital(vital);
                string word;
                string tone;
                if (vital == VitalType.Satiety || vital == VitalType.Hydration)
                {
                    var band = UpkeepRules.Band(vital == VitalType.Satiety ? balance.SatietyBands : balance.HydrationBands, value);
                    word = band != null ? Loc.T("band." + band.Key) : "";
                    tone = value < -30 ? "danger" : value < 0 ? "warn" : "ok";
                }
                else
                {
                    var measured = vital == VitalType.Stress ? value * 100f / threshold : value;
                    var band = _config.Vitals.Where(b => b.Vital == vital.ToString() && measured >= b.From).OrderByDescending(b => b.From).FirstOrDefault();
                    word = band != null ? Loc.T("diary.vital." + vital + "." + band.Key) : "";
                    tone = band?.Tone ?? "ok";
                }

                var row = new VisualElement();
                row.AddToClassList("diary-body-row");
                Add(row, UiText.Vital(vital), "diary-body-name");
                var w = Add(row, word, "diary-body-word");
                w.AddToClassList("tone-" + tone);
                Add(row, (vital == VitalType.Satiety || vital == VitalType.Hydration) && value > 0 ? "+" + value : value.ToString(), "diary-body-value");
                note.Add(row);
            }

            if (InjuryRules.IsInjured(state, balance))
            {
                Add(note, Loc.T("status.injured"), "diary-alert");
            }

            var symptom = InfectionRules.SymptomLevel(state);
            if (symptom > 0)
            {
                Add(note, Loc.T("symptom." + symptom), "diary-alert");
            }

            if (Disclosure.Has(state, "UI_SLEEPDEBT"))
            {
                Add(note, Loc.T("status.sleep_debt") + " " + Dots(player.SleepDebt, balance.SleepDebtMax), "diary-dots");
                Add(note, Loc.T("status.conscience") + " " + Dots(player.Conscience, balance.ConscienceMax), "diary-dots");
            }

            return note;
        }

        // ── 나는 지금 이만큼 ──

        private VisualElement BuildStats(GameState state, BalanceConfig balance)
        {
            var note = Note(_config.NoteStats, "diary-note-stats", Loc.T("diary.stats"));
            var grid = new VisualElement();
            grid.AddToClassList("diary-stat-grid");
            note.Add(grid);
            var delta = _session.LastWeekSummary?.Delta;
            var circle = StoryArt.Load(_config.Circle);
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                var value = state.Player.GetStat(stat);
                var filled = Mathf.Clamp(Mathf.RoundToInt(value * (float)_config.StatBoxes / Math.Max(1, _config.StatBoxMax)), 0, _config.StatBoxes);
                var row = new VisualElement();
                row.AddToClassList("diary-stat-row");
                var name = Add(row, UiText.Stat(stat), "diary-stat-name");
                StoryArt.SetIcon(name, StoryArt.Icon("stat." + stat), 18f);
                Add(row, new string('■', filled) + new string('□', _config.StatBoxes - filled), "diary-stat-boxes");
                Add(row, value.ToString(), "diary-stat-value");
                var gained = 0;
                if (delta != null && delta.Stats.TryGetValue(stat, out var d))
                {
                    gained = d;
                }

                var mark = Add(row, gained > 0 ? "+" + gained : "", "diary-stat-delta");
                if (gained >= _config.DeltaMark && circle != null)
                {
                    mark.AddToClassList("circled");
                    mark.style.backgroundImage = new StyleBackground(circle);
                }

                Hover(row, Loc.T("stat." + stat + ".hint"));
                grid.Add(row);
            }

            Add(note, Loc.F("diary.derived", DerivedStats.CombatPower(state, balance), DerivedStats.StressThreshold(state, balance)), "diary-derived");
            return note;
        }

        // ── 모은 스티커 ──

        private VisualElement BuildStickers(GameState state)
        {
            var box = new VisualElement();
            box.AddToClassList("diary-stickers");
            Add(box, Loc.T("diary.profs"), "diary-section");
            var row = new VisualElement();
            row.AddToClassList("diary-sticker-row");
            box.Add(row);
            var index = 0;
            foreach (ProficiencyType proficiency in Enum.GetValues(typeof(ProficiencyType)))
            {
                var level = state.Player.ProficiencyLevel(proficiency);
                var sticker = new VisualElement();
                sticker.AddToClassList("diary-sticker");
                sticker.EnableInClassList("empty", level == 0);
                sticker.style.rotate = new StyleRotate(new Rotate(index++ % 2 == 0 ? -5f : 4f));
                var badge = new VisualElement { pickingMode = PickingMode.Ignore };
                badge.AddToClassList("diary-sticker-badge");
                var icon = StoryArt.Icon("prof." + proficiency);
                if (icon != null)
                {
                    badge.style.backgroundImage = new StyleBackground(icon);
                }

                sticker.Add(badge);
                Add(sticker, Loc.Lookup(Loc.UiTable, "prof." + proficiency + ".short") ?? UiText.Proficiency(proficiency).Split('·')[0], "diary-sticker-name");
                // 아직 못 배운 것은 흐린 스티커만 (별은 배운 단계만큼)
                Add(sticker, level == 0 ? "" : new string('★', level), "diary-sticker-level");
                Hover(sticker, Loc.T("prof." + proficiency + ".hint"));
                row.Add(sticker);
            }

            return box;
        }

        // ── 힌트 ──

        private void Hover(VisualElement target, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            target.RegisterCallback<PointerEnterEvent>(_ =>
            {
                _hint.text = text;
                _hint.RemoveFromClassList("hidden");
                var local = _drawer.WorldToLocal(new Vector2(target.worldBound.xMin, target.worldBound.yMax));
                var below = local.y + 4f;
                var height = _drawer.resolvedStyle.height;
                _hint.style.left = Mathf.Clamp(local.x, 8f, Math.Max(8f, _drawer.resolvedStyle.width - 330f));
                _hint.style.top = below + 90f > height ? local.y - target.worldBound.height - 94f : below;
            });
            target.RegisterCallback<PointerLeaveEvent>(_ => HideHint());
        }

        private void HideHint() => _hint.AddToClassList("hidden");

        // ── 작은 도구 ──

        private VisualElement Note(string art, string cssClass, string title)
        {
            var note = new VisualElement();
            note.AddToClassList("diary-note");
            note.AddToClassList(cssClass);
            var texture = StoryArt.Load(art);
            if (texture != null)
            {
                note.style.backgroundImage = new StyleBackground(texture);
            }

            Add(note, title, "diary-section");
            return note;
        }

        private static string Dots(int value, int max) => new string('●', Math.Max(0, Math.Min(value, max))) + new string('○', Math.Max(0, max - value));

        private static Label Add(VisualElement parent, string text, string cssClass)
        {
            var label = new Label(text);
            label.AddToClassList(cssClass);
            parent.Add(label);
            return label;
        }
    }
}
