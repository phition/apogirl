using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation.Bots;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Planning
{
    /// <summary>
    /// 선생님 메모 한 줄. Key는 UI 문구 키, Args는 UI가 이름으로 바꾸는 토큰
    /// ("loc:LOC_X" 지역, "npc:NPC_X" 사람, "act:ACT_X" 행동, "day:3" 요일, "res:Food" 자원, 그 밖은 그대로).
    /// </summary>
    public sealed class TeacherNote
    {
        public int Slot = -1;
        public string Key = "";
        public string[] Args = Array.Empty<string>();

        public TeacherNote()
        {
        }

        public TeacherNote(int slot, string key, params string[] args)
        {
            Slot = slot;
            Key = key;
            Args = args ?? Array.Empty<string>();
        }
    }

    /// <summary>선생님 추천 결과: 칸마다 선생님이 넣었는지·까닭, 고정 칸, 메모.</summary>
    public sealed class TeacherResult
    {
        public readonly bool[] Placed = new bool[WeekPlan.SlotCount];
        public readonly bool[] Pinned = new bool[WeekPlan.SlotCount];
        public readonly TeacherNote[] Reasons = new TeacherNote[WeekPlan.SlotCount];
        public readonly List<TeacherNote> Notes = new List<TeacherNote>();
    }

    /// <summary>이번 주 이야기가 기다리는 지역 하나.</summary>
    public sealed class StoryHint
    {
        public string LocationId = "";

        /// <summary>그 이벤트를 부르는 탐험 행동 (낮·밤이 정해진 경우). 대화 조우는 비어 있다 (그 지역 아무 탐험).</summary>
        public string ActionId = "";
        public string SourceId = "";
        public int Priority;
        public bool IsEncounter;
    }

    /// <summary>고정 시간표 (압정). "칸=행동" 문자열 목록으로 주고받는다.</summary>
    public static class TeacherPins
    {
        /// <summary>같이 할 사람이 좋아하는 동아리.</summary>
        public const string ClubToken = "@CLUB";

        public static Dictionary<int, string> Parse(IEnumerable<string> entries)
        {
            var pins = new Dictionary<int, string>();
            foreach (var entry in entries ?? Enumerable.Empty<string>())
            {
                var parts = (entry ?? "").Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }

                var slot = BotPlanner.SlotIndex(parts[0].Trim());
                var id = parts[1].Trim();
                if (slot >= 0 && slot < WeekPlan.SlotCount && id.Length > 0)
                {
                    pins[slot] = id;
                }
            }

            return pins;
        }

        public static string[] Format(IDictionary<int, string> pins)
        {
            return pins.OrderBy(p => p.Key).Select(p => Key(p.Key) + "=" + p.Value).ToArray();
        }

        /// <summary>칸 번호 → "D1"~"N7".</summary>
        public static string Key(int slot) => (CalendarRules.PhaseOf(slot) == DayPhase.Day ? "D" : "N") + CalendarRules.DayOf(slot);

        /// <summary>칸에 꽂을 압정 값: 동아리면 "@CLUB"(짝에 따라 활동이 바뀜), 그 밖은 행동 ID.</summary>
        public static string TokenFor(IDefinitionDatabase db, string actionId) =>
            db.TryGetAction(actionId ?? "", out var action) && action.IsClub ? ClubToken : actionId;

        /// <summary>압정 값을 이번 주 행동으로. 동아리는 같이 할 사람이 좋아하는 활동(없으면 열린 첫 동아리), 열린 것이 없으면 null.</summary>
        public static string Resolve(GameState state, IDefinitionDatabase db, string token, int slot)
        {
            if (token != ClubToken)
            {
                return token;
            }

            var partner = ClubRules.Partner(state);
            var liked = partner != null ? ClubRules.LikedActivity(db.Balance, partner) : "";
            var clubs = db.Actions.Where(a => a.IsClub && PlanEditing.IsUnlocked(state, db, a) && PlanEditing.IsAllowedIn(a, slot)).ToList();
            return (clubs.FirstOrDefault(a => a.ClubActivity == liked) ?? clubs.FirstOrDefault())?.Id;
        }
    }

    /// <summary>
    /// 이번 주 이야기가 기다리는 지역 (TEACHER_PLAN_PROPOSAL 4.2 ②): 아직 안 본 이야기 이벤트가 그 지역 탐험만 남겨 두고
    /// 조건이 맞거나, 그 지역에 만날 사람(대화 조우)이 기다린다. 상태를 바꾸지 않는다.
    /// </summary>
    public static class StoryHints
    {
        public static List<StoryHint> PendingLocations(GameState state, IDefinitionDatabase db)
        {
            var hints = new List<StoryHint>();
            var balance = db.Balance;
            var runs = db.Actions.Where(a => a.IsExpedition && !string.IsNullOrEmpty(a.LocationId) && PlanEditing.IsUnlocked(state, db, a)).ToList();

            foreach (var ev in db.Events)
            {
                if (ev.IsRandom || ev.Condition == null || ev.Condition.ActionIds.Count == 0 || !EventScheduler.IsAvailable(state, ev))
                {
                    continue;
                }

                foreach (var run in runs)
                {
                    if (!ev.Condition.ActionIds.Contains(run.Id) || hints.Any(h => h.SourceId == ev.Id && h.LocationId == run.LocationId))
                    {
                        continue;
                    }

                    var phase = run.ExpeditionTime == ExpeditionTime.Night ? DayPhase.Night : DayPhase.Day;
                    var met = false;
                    for (var day = 1; day <= CalendarState.DaysPerWeek && !met; day++)
                    {
                        var context = new EventContext { Phase = phase, Day = day, ActionId = run.Id, ActionCategory = run.Category };
                        try
                        {
                            met = ConditionEvaluator.IsMet(state, ev.Condition, context, balance);
                        }
                        catch (ArgumentException)
                        {
                            met = false;
                        }
                    }

                    if (met)
                    {
                        hints.Add(new StoryHint { LocationId = run.LocationId, ActionId = run.Id, SourceId = ev.Id, Priority = ev.Priority });
                    }
                }
            }

            foreach (var locationId in runs.Select(r => r.LocationId).Distinct())
            {
                if (!db.TryGetLocation(locationId, out var location))
                {
                    continue;
                }

                foreach (var encounter in location.DayVariant.Encounters.Concat(location.NightVariant.Encounters))
                {
                    if (string.IsNullOrEmpty(encounter.DialogueNode) || encounter.Collectible
                        || !encounter.FlagsAll.All(state.Flags.Contains) || encounter.FlagsNone.Any(state.Flags.Contains))
                    {
                        continue;
                    }

                    hints.Add(new StoryHint { LocationId = locationId, SourceId = encounter.Id, IsEncounter = true });
                    break;
                }
            }

            return hints.OrderByDescending(h => h.Priority).ThenBy(h => h.IsEncounter).ThenBy(h => h.LocationId, StringComparer.Ordinal).ToList();
        }
    }

    /// <summary>
    /// 선생님 추천 편성 (Docs/TEACHER_PLAN_PROPOSAL.md 4절). 빈 칸과 keep에 들지 않은 칸을 채운다:
    /// ① 고정 시간표 → ② 이야기 지역 탐험 → ③ 탐험 1회 보장 → ④ 식량·물 부족 탐험 → ⑤ 휴식 → ⑥ 일상 → ⑦ 거점 개발·수리 → ⑧ 수업 → ⑨ 다듬기.
    /// 모든 칸은 PlanEditing.SetSlot을 거쳐 들어간다. 난수를 쓰지 않아 같은 상태면 같은 추천이 나온다.
    /// </summary>
    public static class TeacherPlanner
    {
        /// <summary>선생님이 추천해 줄 수 있는가: 합류했고 자리를 비우지 않았다.</summary>
        public static bool Available(GameState state, BalanceConfig balance)
        {
            return GiveRules.Recipients(state, balance).Contains(GiveRules.TeacherId)
                   && (balance.TeacherAbsentFlags == null || !balance.TeacherAbsentFlags.Any(state.Flags.Contains));
        }

        public static bool TeacherIll(GameState state, BalanceConfig balance) =>
            balance.TeacherIllFlags != null && balance.TeacherIllFlags.Any(state.Flags.Contains);

        /// <param name="pins">고정 시간표 ("칸=행동"). null이면 BalanceConfig.TeacherDefaultPins.</param>
        /// <param name="keep">손대지 않을 칸 (플레이어가 넣은 칸).</param>
        public static TeacherResult Recommend(GameState state, IDefinitionDatabase db, IEnumerable<string> pins = null, ICollection<int> keep = null)
        {
            var planner = new Run(state, db, keep ?? new HashSet<int>());
            planner.Plan(TeacherPins.Parse(pins ?? db.Balance.TeacherDefaultPins));
            return planner.Result;
        }

        private sealed class Run
        {
            private readonly GameState _state;
            private readonly IDefinitionDatabase _db;
            private readonly BalanceConfig _balance;
            private readonly ICollection<int> _keep;
            private readonly List<PlannedSlot> _slots;
            private readonly HashSet<int> _guaranteed = new HashSet<int>();

            public readonly TeacherResult Result = new TeacherResult();

            public Run(GameState state, IDefinitionDatabase db, ICollection<int> keep)
            {
                _state = state;
                _db = db;
                _balance = db.Balance;
                _keep = keep;
                _slots = state.Plan.Slots;
            }

            public void Plan(Dictionary<int, string> pins)
            {
                // 0. 선생님이 채울 칸을 비운다 (잠긴 칸·플레이어 칸은 둔다)
                PlanEditing.ClearAll(_state, _db, _keep);
                MarkAuto(null);

                PlacePins(pins);
                var food = BaseForecast.Food(_state, _db);
                var water = BaseForecast.Water(_state, _db);
                PlaceStory();
                PlaceShortage(food, water);
                PlaceMedicine();
                EnsureExpedition(food, water);
                PlaceRests();
                PlaceDaily();
                PlaceDevelop(food, water);
                PlaceClasses();
                Tidy();
                FillRest();
                Result.Notes.Sort((a, b) => a.Slot.CompareTo(b.Slot));
                var order = new List<TeacherNote>(Result.Notes);
                Result.Notes.Clear();
                Result.Notes.AddRange(order.Where(n => n.Key != "teacher.why.rest" && n.Key != "teacher.why.rest_after" && n.Key != "teacher.why.pin")
                    .Take(Math.Max(1, _balance.TeacherNoteLines)));
            }

            // ── ① 고정 시간표 ──

            private void PlacePins(Dictionary<int, string> pins)
            {
                foreach (var pin in pins.OrderBy(p => p.Key))
                {
                    var id = TeacherPins.Resolve(_state, _db, pin.Value, pin.Key);
                    if (id != null && Usable(id, pin.Key) && Place(pin.Key, id, new TeacherNote(pin.Key, "teacher.why.pin")))
                    {
                        Result.Pinned[pin.Key] = true;
                        _guaranteed.Add(pin.Key);
                    }
                }
            }

            // ── ②~④ 탐험 ──

            private void PlaceStory()
            {
                foreach (var hint in StoryHints.PendingLocations(_state, _db))
                {
                    if (ExpeditionCount() >= _balance.TeacherMaxExpeditions || PlannedLocations().Contains(hint.LocationId))
                    {
                        continue;
                    }

                    var key = hint.IsEncounter ? "teacher.why.story_person" : "teacher.why.story";
                    if (PlaceExpedition(hint.LocationId, 0, key, hint.ActionId, "loc:" + hint.LocationId))
                    {
                        return;   // 이야기 지역은 한 주에 하나 (나머지는 다음 주)
                    }
                }
            }

            private void PlaceShortage(SupplyForecast food, SupplyForecast water)
            {
                foreach (var (resource, forecast) in new[] { (ResourceType.Food, food), (ResourceType.Water, water) })
                {
                    if (forecast == null || (forecast.ShortDay <= 0 && forecast.End >= forecast.NextWeekNeed * Math.Max(1f, _balance.TeacherSupplyBufferWeeks)))
                    {
                        continue;
                    }

                    var best = BestLocation(resource);
                    if (best == null)
                    {
                        continue;
                    }

                    // 그 자원을 넉넉히 주는 지역(가장 좋은 곳의 절반 이상)에 이미 간다면 그만큼 덜 넣는다.
                    // 며칠 안에 바닥나면(TeacherCriticalShortDay) 두 번.
                    var bestYield = Yield(best, resource);
                    var critical = forecast.ShortDay > 0 && forecast.ShortDay <= _balance.TeacherCriticalShortDay;
                    var underAWeek = _balance.TeacherTwoWhenUnderAWeek && forecast.End < forecast.NextWeekNeed;
                    var wanted = critical || underAWeek ? 2 : 1;
                    var have = PlannedExpeditions().Count(i => Yield(_db.GetAction(_slots[i].ActionId).LocationId, resource) >= bestYield * 0.5f);
                    var key = forecast.ShortDay > 0 ? "teacher.why.short" : "teacher.why.tight";
                    var places = Locations().Where(l => Yield(l, resource) >= bestYield * 0.5f)
                        .OrderByDescending(l => Yield(l, resource)).ThenBy(l => l, StringComparer.Ordinal).ToList();
                    while (have < wanted && ExpeditionCount() < _balance.TeacherMaxExpeditions)
                    {
                        // 가 본 적 없는 곳부터 (같은 곳을 두 번 가면 상자가 빈다)
                        var planned = PlannedLocations();
                        var placedOne = places.OrderBy(l => planned.Contains(l) ? 1 : 0)
                            .Any(l => PlaceExpedition(l, forecast.ShortDay, key, null, "res:" + resource, "day:" + Math.Max(1, forecast.ShortDay)));
                        if (!placedOne)
                        {
                            break;
                        }

                        have++;
                    }
                }
            }

            /// <summary>감염·부상인데 처치할 의약품이 모자라면 의약품이 많은 곳으로 한 번.</summary>
            private void PlaceMedicine()
            {
                var need = 0;
                if (_state.Player.Vitals.Infection >= _balance.TeacherInfectionTreat)
                {
                    need += Cost("ACT_TREAT_INFECTION", ResourceType.Medicine);
                }

                if (InjuryRules.IsInjured(_state, _balance))
                {
                    need += Cost("ACT_TREAT_WOUND", ResourceType.Medicine);
                }

                if (need <= 0 || _state.Base.GetResource(ResourceType.Medicine) >= need || ExpeditionCount() >= _balance.TeacherMaxExpeditions)
                {
                    return;
                }

                var best = BestLocation(ResourceType.Medicine);
                if (best == null || PlannedLocations().Any(l => Yield(l, ResourceType.Medicine) >= Yield(best, ResourceType.Medicine) * 0.5f))
                {
                    return;
                }

                PlaceExpedition(best, 0, "teacher.why.medicine", null, "loc:" + best);
            }

            private int Cost(string actionId, ResourceType resource) =>
                _db.TryGetAction(actionId, out var action) ? action.ResourceCosts.Where(c => c.Resource == resource).Sum(c => SpecialRules.Cost(_state, _balance, c)) : 0;

            private void EnsureExpedition(SupplyForecast food, SupplyForecast water)
            {
                if (ExpeditionCount() >= _balance.TeacherMinExpeditions)
                {
                    return;
                }

                var foodWeight = food != null && food.End < food.NextWeekNeed ? 2f : 1f;
                var waterWeight = water != null && water.End < water.NextWeekNeed ? 2f : 1f;
                var best = Locations()
                    .Select(l => (Location: l, Score: Yield(l, ResourceType.Food) * foodWeight + Yield(l, ResourceType.Water) * waterWeight + TotalYield(l) * 0.5f))
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Location, StringComparer.Ordinal)
                    .Select(x => x.Location);
                foreach (var location in best)
                {
                    if (PlaceExpedition(location, 0, "teacher.why.explore", null, "loc:" + location))
                    {
                        return;
                    }
                }
            }

            /// <summary>지역 탐험 한 번: 낮 탐험을 먼저, 주 앞쪽부터, 다음 칸(강제 휴식)이 비어 있고 다른 탐험과 하루 띄운 자리.</summary>
            private bool PlaceExpedition(string locationId, int beforeDay, string key, string onlyAction, params string[] args)
            {
                var runs = _db.Actions.Where(a => a.IsExpedition && a.LocationId == locationId && PlanEditing.IsUnlocked(_state, _db, a)
                                                  && (string.IsNullOrEmpty(onlyAction) || a.Id == onlyAction))
                    .OrderBy(a => a.ExpeditionTime == ExpeditionTime.Day ? 0 : 1).ToList();
                var plannedDay = PlannedExpeditions().Any(i => CalendarRules.PhaseOf(i) == DayPhase.Day);
                var plannedNight = PlannedExpeditions().Any(i => CalendarRules.PhaseOf(i) == DayPhase.Night);
                foreach (var run in runs)
                {
                    // 낮·밤 탐험을 한 주에 섞지 않는다 (강행군 경고)
                    if ((run.ExpeditionTime == ExpeditionTime.Day && plannedNight) || (run.ExpeditionTime == ExpeditionTime.Night && plannedDay))
                    {
                        continue;
                    }

                    if (BotPlanner.Exhausted(_state, _db, run))
                    {
                        continue;
                    }

                    var candidates = new List<(int Slot, int Score)>();
                    for (var day = 1; day <= CalendarState.DaysPerWeek; day++)
                    {
                        var slot = CalendarRules.SlotIndex(day, run.ExpeditionTime == ExpeditionTime.Night ? DayPhase.Night : DayPhase.Day);
                        if (!Free(slot) || !Usable(run.Id, slot))
                        {
                            continue;
                        }

                        // 다음 칸은 강제 휴식이 된다: 비어 있거나 이미 휴식이어야 한다
                        if (slot + 1 < _slots.Count && !Free(slot + 1) && !IsRest(slot + 1))
                        {
                            continue;
                        }

                        var score = day * 2;
                        if (beforeDay > 0 && day >= beforeDay)
                        {
                            score += 40;   // 바닥나는 날 전이 좋다
                        }

                        if (PlannedExpeditions().Any(i => Math.Abs(CalendarRules.DayOf(i) - day) <= 1))
                        {
                            score += 20;   // 다른 탐험과 하루 띄운다
                        }

                        candidates.Add((slot, score));
                    }

                    foreach (var (slot, _) in candidates.OrderBy(c => c.Score).ThenBy(c => c.Slot))
                    {
                        if (Place(slot, run.Id, new TeacherNote(slot, key, args)))
                        {
                            _guaranteed.Add(slot);
                            return true;
                        }
                    }
                }

                return false;
            }

            // ── ⑤ 휴식 ──

            private void PlaceRests()
            {
                var guard = 0;
                while (CountWhere(i => IsRest(i)) < _balance.TeacherMinRests && guard++ < WeekPlan.SlotCount)
                {
                    // 아직 수업이 안 들어가 예측이 가볍다: 자리로 고른다 (주 뒤쪽·밤, 쉬는 칸끼리 붙이지 않는다). 피로가 넘치면 ⑨가 더 쉬게 한다.
                    var best = -1;
                    var bestScore = float.MinValue;
                    for (var i = 0; i < _slots.Count; i++)
                    {
                        if (!Free(i))
                        {
                            continue;
                        }

                        var score = CalendarRules.DayOf(i) * 3f + (CalendarRules.PhaseOf(i) == DayPhase.Night ? 6f : 0f)
                                    - ((i > 0 && IsRest(i - 1)) || (i + 1 < _slots.Count && IsRest(i + 1)) ? 12f : 0f);
                        if (score > bestScore)
                        {
                            best = i;
                            bestScore = score;
                        }
                    }

                    if (best < 0 || !Place(best, PlanPreparer.RestIdFor(best, _balance), new TeacherNote(best, "teacher.why.rest")))
                    {
                        break;
                    }

                    _guaranteed.Add(best);
                }

                foreach (var i in Enumerable.Range(0, _slots.Count).Where(IsRest))
                {
                    _guaranteed.Add(i);
                }
            }

            // ── ⑥ 일상 ──

            private void PlaceDaily()
            {
                var count = CountWhere(i => !Result.Pinned[i] && Category(i) == ActionCategory.Daily);
                if (count >= _balance.TeacherMinDaily)
                {
                    return;
                }

                var candidates = new List<(string Id, TeacherNote Note)>();
                if (TeacherIll(_state, _balance))
                {
                    candidates.Add((_balance.TeacherCareActionId, new TeacherNote(-1, "teacher.why.daily_care")));
                }

                var lonely = _state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined && !CatRules.IsCat(n.Key) && n.Value.Affinity < _balance.TeacherTalkAffinity)
                    .OrderBy(n => n.Value.Affinity).ThenBy(n => n.Key, StringComparer.Ordinal).Select(n => n.Key).FirstOrDefault();
                if (lonely != null)
                {
                    candidates.Add(("ACT_NIGHT_TALK", new TeacherNote(-1, "teacher.why.daily_talk", "npc:" + lonely)));
                }

                if (_state.Player.Vitals.Stress >= _balance.TeacherRooftopStress)
                {
                    candidates.Add(("ACT_DAILY_ROOFTOP", new TeacherNote(-1, "teacher.why.daily_rooftop")));
                }

                foreach (var id in _balance.TeacherDailyFallback ?? Array.Empty<string>())
                {
                    candidates.Add((id, new TeacherNote(-1, "teacher.why.daily", "act:" + id)));
                }

                // 주 뒤쪽 밤부터 (주 앞은 탐험·개발이 먼저 차지)
                var order = new[] { 5, 4, 2, 1, 3, 6, 7 };
                foreach (var (id, note) in candidates)
                {
                    foreach (var day in order)
                    {
                        foreach (var phase in new[] { DayPhase.Night, DayPhase.Day })
                        {
                            var slot = CalendarRules.SlotIndex(day, phase);
                            if (Free(slot) && Usable(id, slot) && Place(slot, id, new TeacherNote(slot, note.Key, note.Args)))
                            {
                                _guaranteed.Add(slot);
                                if (++count >= _balance.TeacherMinDaily)
                                {
                                    return;
                                }

                                goto NextCandidate;
                            }
                        }
                    }

                    NextCandidate: ;
                }
            }

            // ── ⑦ 거점 개발·수리 ──

            private void PlaceDevelop(SupplyForecast food, SupplyForecast water)
            {
                var candidates = new List<(string Id, TeacherNote Note)>();
                if (InjuryRules.IsInjured(_state, _balance))
                {
                    candidates.Add(("ACT_TREAT_WOUND", new TeacherNote(-1, "teacher.why.treat")));
                }

                if (_state.Player.Vitals.Infection >= _balance.TeacherInfectionTreat)
                {
                    candidates.Add(("ACT_TREAT_INFECTION", new TeacherNote(-1, "teacher.why.treat_infection")));
                }

                var raid = BaseForecast.Raid(_state, _db);
                foreach (var advice in BaseAdvice.Pick(_state, _db, raid, food, water, 99))
                {
                    if (!string.IsNullOrEmpty(advice.ActionId))
                    {
                        candidates.Add((advice.ActionId, new TeacherNote(-1, "advice." + advice.Id)));
                    }
                }

                var foodTight = food != null && food.End < food.NextWeekNeed;
                foreach (var id in _balance.TeacherDevelopFallback ?? Array.Empty<string>())
                {
                    if (!_db.TryGetAction(id, out var action))
                    {
                        continue;
                    }

                    if (action.IsCraft && !BotCraft.Wanted(_state, _db, action))
                    {
                        continue;
                    }

                    if (id == "ACT_SURV_CLEAN" && _state.Player.Vitals.Cleanliness >= _balance.TeacherCleanBelow)
                    {
                        continue;
                    }

                    if (id == "ACT_SURV_GARDEN" && !foodTight)
                    {
                        continue;
                    }

                    candidates.Add((id, new TeacherNote(-1, "teacher.why.develop", "act:" + id)));
                }

                // 빗물이 많이 모였으면 정수를 더 (한 번에 PurifyWater만큼, 최대 TeacherMaxPurify번)
                var purify = _balance.TeacherPurifyActionId;
                var raw = _state.Base.GetResource(ResourceType.RawWater);
                var purifyWanted = Math.Min(_balance.TeacherMaxPurify, (raw + _balance.TeacherPurifyBatch - 1) / Math.Max(1, _balance.TeacherPurifyBatch));
                if (water != null && water.End >= water.NextWeekNeed * 2)
                {
                    purifyWanted = Math.Min(purifyWanted, 1);
                }

                var placed = 0;
                var used = new Dictionary<string, int>();
                foreach (var (id, note) in candidates.Concat(Enumerable.Repeat((purify, new TeacherNote(-1, "teacher.why.purify_more")), Math.Max(0, purifyWanted - 1))))
                {
                    used.TryGetValue(id, out var times);
                    var allowed = id == purify ? purifyWanted : 1;
                    if (placed >= _balance.TeacherMaxDevelop || times >= allowed || _slots.Count(s => s.ActionId == id) >= allowed)
                    {
                        continue;
                    }

                    used[id] = times + 1;

                    // 낮 칸부터, 주 앞쪽부터 (고칠 것은 빨리)
                    var slots = Enumerable.Range(0, _slots.Count).OrderBy(i => CalendarRules.PhaseOf(i) == DayPhase.Day ? 0 : 1).ThenBy(i => i);
                    foreach (var slot in slots)
                    {
                        if (Free(slot) && Usable(id, slot) && Place(slot, id, new TeacherNote(slot, note.Key, note.Args)))
                        {
                            placed++;
                            break;
                        }
                    }
                }
            }

            // ── ⑧ 수업 ──

            private void PlaceClasses()
            {
                var lowest = Enumerable.Range(0, WeekForecast.StatCount).Select(s => (StatType)s)
                    .OrderBy(s => _state.Player.GetStat(s)).First();
                var teacherHere = Available(_state, _balance) && !TeacherIll(_state, _balance);
                var classes = _db.Actions.Where(a => a.Category == ActionCategory.Education).ToList();
                for (var slot = 0; slot < _slots.Count; slot++)
                {
                    if (!Free(slot))
                    {
                        continue;
                    }

                    var best = classes
                        .Where(a => Usable(a.Id, slot) && CountWhere(i => _slots[i].ActionId == a.Id) < _balance.TeacherMaxSameClass)
                        .Select(a => (Action: a, Score: ClassScore(a, lowest, teacherHere)))
                        .OrderByDescending(x => x.Score)
                        .ThenBy(x => x.Action.Id, StringComparer.Ordinal)
                        .FirstOrDefault();
                    if (best.Action == null)
                    {
                        continue;
                    }

                    var top = ActionPreviewer.Preview(_state, _db, best.Action.Id).ExpectedGains.Where(g => g.Amount > 0)
                        .OrderByDescending(g => g.Amount).Select(g => "stat:" + g.Stat).FirstOrDefault() ?? "";
                    var key = teacherHere && best.Action.Teaching == TeachingMode.TeacherDirect ? "teacher.why.class_teach" : "teacher.why.class";
                    Place(slot, best.Action.Id, new TeacherNote(slot, key, "act:" + best.Action.Id, top));
                }
            }

            private float ClassScore(ActionDef action, StatType lowest, bool teacherHere)
            {
                var preview = ActionPreviewer.Preview(_state, _db, action.Id);
                var gain = preview.ExpectedGains.Sum(g => Math.Max(0, g.Amount)) * preview.SuccessChance;
                var score = gain / (1f + Math.Max(0, preview.StressCost) + Math.Max(0, preview.FatigueCost) / 5f);
                if (teacherHere && action.Teaching == TeachingMode.TeacherDirect)
                {
                    score += _balance.TeacherClassTeachBonus;
                }

                if (preview.ExpectedGains.Any(g => g.Stat == lowest && g.Amount > 0))
                {
                    score += _balance.TeacherClassLowStatBonus;
                }

                if (EchoRules.IsFavorite(_state, _balance, action))
                {
                    score += _balance.TeacherClassFavoriteBonus;
                }

                return score;
            }

            // ── ⑨ 다듬기 ──

            private void Tidy()
            {
                for (var pass = 0; pass < _balance.TeacherTidyPasses; pass++)
                {
                    var forecast = WeekForecast.Project(_state, _db, ProjectionIds());
                    var over = -1;
                    var stressOver = false;
                    for (var i = 0; i < forecast.Slots.Length; i++)
                    {
                        var s = forecast.Slots[i];
                        if (s.Fatigue >= _balance.TeacherFatigueLimit || s.SleepDebt >= _balance.TeacherSleepDebtLimit || s.Stress >= _balance.TeacherStressLimit)
                        {
                            over = i;
                            stressOver |= s.Stress >= _balance.TeacherStressLimit;
                        }
                    }

                    // 다음 주로 넘길 피로·스트레스도 줄인다 (주마다 쌓이지 않게)
                    if (forecast.Last.Fatigue > _balance.TeacherEndFatigue || forecast.Last.Stress > _balance.TeacherEndStress)
                    {
                        over = forecast.Slots.Length - 1;
                        stressOver |= forecast.Last.Stress > _balance.TeacherEndStress;
                    }

                    if (over < 0)
                    {
                        return;
                    }

                    // 넘는 칸까지 선생님이 넣은 무거운 칸 하나 (보장한 칸은 빼고)
                    var target = Enumerable.Range(0, over + 1)
                        .Where(i => Result.Placed[i] && !_guaranteed.Contains(i) && !_slots[i].Locked && _db.TryGetAction(_slots[i].ActionId ?? "", out var a) && !a.IsRest)
                        .OrderByDescending(i => Weight(i))
                        .ThenByDescending(i => i)
                        .DefaultIfEmpty(-1)
                        .First();
                    if (target < 0)
                    {
                        return;
                    }

                    var light = stressOver && Usable("ACT_EDU_MUSIC", target) && _slots[target].ActionId != "ACT_EDU_MUSIC" ? "ACT_EDU_MUSIC" : PlanPreparer.RestIdFor(target, _balance);
                    PlanEditing.ClearSlot(_state, _db, target);
                    Result.Placed[target] = false;
                    Place(target, light, new TeacherNote(target, "teacher.why.tidy", "day:" + CalendarRules.DayOf(target)));
                    _guaranteed.Add(target);
                }
            }

            private int Weight(int slot)
            {
                return _db.TryGetAction(_slots[slot].ActionId ?? "", out var action) ? Math.Max(0, action.FatigueCost) + Math.Max(0, action.StressCost) * 2 : 0;
            }

            private void FillRest()
            {
                for (var i = 0; i < _slots.Count; i++)
                {
                    if (Free(i))
                    {
                        Place(i, PlanPreparer.RestIdFor(i, _balance), new TeacherNote(i, "teacher.why.rest"));
                    }
                }
            }

            // ── 도움 ──

            /// <summary>선생님이 채울 수 있는 칸: 잠기지 않았고, 플레이어 칸이 아니고, 비어 있다.</summary>
            private bool Free(int slot) =>
                slot >= 0 && slot < _slots.Count && !_slots[slot].Locked && !_keep.Contains(slot) && string.IsNullOrEmpty(_slots[slot].ActionId);

            private bool Usable(string id, int slot)
            {
                if (!_db.TryGetAction(id ?? "", out var action) || !PlanEditing.IsUnlocked(_state, _db, action) || !PlanEditing.IsAllowedIn(action, slot)
                    || BotPlanner.Exhausted(_state, _db, action))
                {
                    return false;
                }

                if (action.ResourceCosts.Any(cost => _state.Base.GetResource(cost.Resource) < SpecialRules.Cost(_state, _balance, cost)))
                {
                    return false;
                }

                return !action.IsCraft || (CraftRules.CheckStart(_state, _db, action) == CraftBlock.None && BotPlanner.CraftFits(_state, _db, action, slot));
            }

            private bool Place(int slot, string id, TeacherNote note)
            {
                if (!Free(slot))
                {
                    return false;
                }

                var before = PlanEditing.Snapshot(_state);
                if (PlanEditing.SetSlot(_state, _db, slot, id) != PlanEditResult.Ok)
                {
                    return false;
                }

                Result.Placed[slot] = true;
                Result.Reasons[slot] = note;
                Result.Notes.Add(note);
                MarkAuto(before);
                return true;
            }

            /// <summary>편집 뒤 저절로 찬 칸 (탐험 뒤 휴식, 제작 중)도 선생님 칸으로 적는다.</summary>
            private void MarkAuto(string[] before)
            {
                if (before == null)
                {
                    return;
                }

                for (var i = 0; i < _slots.Count; i++)
                {
                    if (string.IsNullOrEmpty(before[i]) && !string.IsNullOrEmpty(_slots[i].ActionId) && !Result.Placed[i] && !_keep.Contains(i))
                    {
                        Result.Placed[i] = true;
                        Result.Reasons[i] = new TeacherNote(i, _slots[i].CraftLocked ? "teacher.why.craft" : "teacher.why.rest_after");
                        if (IsRest(i))
                        {
                            _guaranteed.Add(i);
                        }
                    }
                }
            }

            private bool IsRest(int slot) => _db.TryGetAction(_slots[slot].ActionId ?? "", out var action) && action.IsRest;

            private ActionCategory? Category(int slot) => _db.TryGetAction(_slots[slot].ActionId ?? "", out var action) ? action.Category : (ActionCategory?)null;

            private bool InPlan(string id) => _slots.Any(s => s.ActionId == id);

            private int CountWhere(Func<int, bool> predicate) => Enumerable.Range(0, _slots.Count).Count(predicate);

            private IEnumerable<int> PlannedExpeditions() =>
                Enumerable.Range(0, _slots.Count).Where(i => _db.TryGetAction(_slots[i].ActionId ?? "", out var a) && a.IsExpedition);

            private int ExpeditionCount() => PlannedExpeditions().Count();

            private HashSet<string> PlannedLocations() =>
                new HashSet<string>(PlannedExpeditions().Select(i => _db.GetAction(_slots[i].ActionId).LocationId));

            /// <summary>흐름 예측용: 빈 칸은 그대로(휴식으로 본다).</summary>
            private string[] ProjectionIds() => PlanEditing.Snapshot(_state);

            /// <summary>열린 지역 탐험의 지역들 (튜토리얼 제외).</summary>
            private IEnumerable<string> Locations() =>
                _db.Actions.Where(a => a.IsExpedition && !string.IsNullOrEmpty(a.LocationId) && a.LocationId != "LOC_TUTORIAL"
                                       && PlanEditing.IsUnlocked(_state, _db, a) && !BotPlanner.Exhausted(_state, _db, a))
                    .Select(a => a.LocationId).Distinct();

            private string BestLocation(ResourceType resource) =>
                Locations().Select(l => (Location: l, Yield: Yield(l, resource)))
                    .Where(x => x.Yield > 0f)
                    .OrderByDescending(x => x.Yield)
                    .ThenBy(x => x.Location, StringComparer.Ordinal)
                    .Select(x => x.Location)
                    .FirstOrDefault();

            /// <summary>그 지역 낮 탐험(없으면 밤)의 자원 기대량 ((최소+최대)/2).</summary>
            private float Yield(string locationId, ResourceType resource)
            {
                if (!_db.TryGetExpeditionModel(locationId, out var model))
                {
                    return 0f;
                }

                var yield = model.Day.Resources.Count > 0 ? model.Day : model.Night;
                return yield.Resources.Where(r => r.Resource == resource).Sum(r => (r.Min + r.Max) / 2f);
            }

            private float TotalYield(string locationId)
            {
                if (!_db.TryGetExpeditionModel(locationId, out var model))
                {
                    return 0f;
                }

                var yield = model.Day.Resources.Count > 0 ? model.Day : model.Night;
                return yield.Resources.Sum(r => (r.Min + r.Max) / 2f);
            }
        }
    }
}
