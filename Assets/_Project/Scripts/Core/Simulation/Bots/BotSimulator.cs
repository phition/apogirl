using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Endings;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Simulation.Bots
{
    /// <summary>주간 정산 직후의 상태 한 줄.</summary>
    public sealed class BotWeekSample
    {
        public int Week;
        public int Food;
        public int Water;
        public int RawWater;
        public int Cleanliness;
        public int Injured;
        public int Medicine;
        public int Fuel;
        public int Materials;
        public int Hp;
        public int Stress;
        public int Fatigue;
        public int Infection;
        public int SleepDebt;
        public int TeacherInfection;
        public int Threat;
        public int Defense;

        /// <summary>동료 (주간 정산 직후): 합류 수, 부상 중, 가벼운 상처, 적성 역할을 맡아 효과가 있는 수.</summary>
        public int Companions;
        public int CompanionsInjured;
        public int CompanionsMinor;
        public int CompanionsOnAptitude;
    }

    /// <summary>동료 탐험 한 번 (역할 재기획 P5 진단).</summary>
    public sealed class BotOutingSample
    {
        public int Week;
        public string NpcId = "";
        public int Resources;
        public bool Item;
        public OutingWound Wound;
    }

    /// <summary>탐험 한 번.</summary>
    public sealed class BotExpeditionSample
    {
        public int Week;
        public string ActionId = "";
        public string LocationId = "";
        public bool Night;
        public ExtractStatus Status;
        public int Food;
        public int Water;
        public int Medicine;
        public int Materials;
        public int Items;
        public bool Bitten;

        /// <summary>적대적 생존자에게 붙잡힌 결과.</summary>
        public HostileOutcome Hostile;
    }

    /// <summary>한 판의 결과.</summary>
    public sealed class BotRunResult
    {
        public string StrategyId = "";
        public int Seed;
        public BloodType BloodType;
        public string Personality = "";

        public GameStatus Status;
        public int EndWeek;
        public string EndCause = "";
        public string EndingId = "";
        public EndingCategory? EndingCategory;

        public int Raids;
        public int RaidsRepelled;
        public int Intrusions;
        public int IntrusionsLost;
        public int Companions;
        public int Events;

        // 선생님·완치 갈래 (M7-0)
        public bool TeacherReinfected;
        public bool TeacherAlive;
        public bool TeacherBloodKnown;
        public bool NeededBloodPack;
        public bool GotBloodPack;
        public bool CureAttempted;
        public bool TeacherCured;
        public string FinalChoice = "";

        /// <summary>기획 A: 끝났을 때 가진 CD 수.</summary>
        public int Cds;

        /// <summary>확장 2: 끝났을 때 고양이가 눌러앉아 있었다.</summary>
        public bool Cat;

        /// <summary>확장 7: 동아리를 한 횟수(동료 모두 합), 본 동아리 개인 장면 수.</summary>
        public int ClubSessions;
        public int ClubScenes;

        /// <summary>M7-2 진단: 판이 끝났을 때 조건을 만족한 (배드 제외) 엔딩.</summary>
        public readonly List<string> EligibleEndings = new List<string>();

        /// <summary>M7-2 진단: 끝났을 때 수치 ("Stat.Intellect", "Prof.Medicine", "Hidden.Humanity", "Companions", "DiaryCount", "Clues.CURE_" …).</summary>
        public readonly Dictionary<string, int> FinalValues = new Dictionary<string, int>();

        /// <summary>M7-2 진단: 끝났을 때 켜져 있던 갈래 플래그 (BotSimulator.TrackedFlags 중).</summary>
        public readonly HashSet<string> TrackedFlags = new HashSet<string>();

        /// <summary>진단: 끝났을 때의 이벤트 기록 (이벤트 ID → 본 횟수, 마지막으로 본 주). 1회성 이벤트면 그 주가 발생 주.</summary>
        public readonly Dictionary<string, (int Count, int LastWeek)> EventRecords = new Dictionary<string, (int Count, int LastWeek)>();

        public readonly List<BotWeekSample> Weeks = new List<BotWeekSample>();
        public readonly List<BotExpeditionSample> Expeditions = new List<BotExpeditionSample>();
        public readonly List<BotOutingSample> Outings = new List<BotOutingSample>();

        public bool Survived => Status == GameStatus.Rescued;
    }

    /// <summary>한 판을 프롤로그부터 구조(또는 사망)까지 화면 없이 돌린다.</summary>
    public static class BotSimulator
    {
        /// <summary>엔딩 판정에 쓰는 마지막 선택 플래그 (보고서 표시용).</summary>
        public static readonly string[] FinalChoiceFlags =
        {
            "TEACHER_LAST_LESSON", "TEACHER_ISOLATED", "TEACHER_EXPOSED", "CHOSE_TO_STAY", "TEACHER_LEFT_BEHIND",
        };

        /// <summary>보고서의 갈래 깔때기에 쓰는 플래그.</summary>
        public static readonly string[] TrackedFlagIds =
        {
            "RINGLEADER_MET", "BULLY_FORGAVE", "BULLY_SEPARATED", "BULLY_RESCUED",
            "RESEARCH_DATA", "CURE_ROUTE", "CARE_ROUTE", "DISTANCE_ROUTE", "CURE_HINTED", "TEACHER_BLOOD_KNOWN", "NEED_BLOOD_PACK", "BLOOD_PACK",
            "CURE_ATTEMPTED", "TEACHER_CURED", "FINAL_CHOICE", "NIGHT_RUNS", "POWER_RESTORED",
        };

        /// <summary>끝났을 때 기록할 수치 (이름, 종류, 키).</summary>
        public static IEnumerable<(string Name, ValueKind Kind, string Key)> TrackedValues()
        {
            foreach (StatType stat in Enum.GetValues(typeof(StatType))) yield return ("Stat." + stat, ValueKind.Stat, stat.ToString());
            foreach (ProficiencyType prof in Enum.GetValues(typeof(ProficiencyType))) yield return ("Prof." + prof, ValueKind.Proficiency, prof.ToString());
            foreach (HiddenType hidden in Enum.GetValues(typeof(HiddenType))) yield return ("Hidden." + hidden, ValueKind.Hidden, hidden.ToString());
            yield return ("StatMin", ValueKind.StatMin, "");
            yield return ("StatSpread", ValueKind.StatSpread, "");
            yield return ("Companions", ValueKind.Companions, "");
            yield return ("DiaryCount", ValueKind.DiaryCount, "");
            yield return ("TeacherAffinity", ValueKind.TeacherAffinity, "");
            yield return ("Clues.CURE_", ValueKind.ClueCount, "CURE_");
            yield return ("Clues.TRUTH_", ValueKind.ClueCount, "TRUTH_");
            yield return ("Tag.TAG_TEACHING", ValueKind.EndingTag, "TAG_TEACHING");
        }

        public static BotRunResult Run(IDefinitionDatabase db, BotConfigDef config, BotStrategyDef strategy, int seed)
        {
            // 봇의 판단 난수는 게임 난수와 따로 둔다 (같은 시드면 전략끼리 세계가 같게 시작)
            var botRandom = new SeededRandom(SeededRandom.CreateState(unchecked(seed * 7919 + StableHash(strategy.Id))));
            var profile = new ProfileState();
            var prologue = PrologueRules.BuildEffect(db.Extras.Prologue ?? new PrologueDef(), PickPrologue(db.Extras.Prologue, config, botRandom), profile);

            var state = NewGameFactory.Create(db.Balance, seed, profile);
            state.Difficulty = string.IsNullOrEmpty(config.Difficulty) ? Rules.DifficultyRules.Standard : config.Difficulty;
            EffectApplier.Apply(state, prologue, db.Balance, new StateDelta());
            TutorialWeekRules.Apply(state, db);

            var result = new BotRunResult
            {
                StrategyId = strategy.Id, Seed = seed, BloodType = profile.BloodType, Personality = profile.PersonalityType,
            };

            var expeditions = new HeadlessExpeditionRunner(db, new BotEncounterChooser(strategy.EncounterFirstChoiceWeight));
            var chooser = new BotEventChooser(config.CommonChoiceBias.Concat(strategy.ChoiceBias), botRandom);
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), expeditions, chooser);
            runner.SlotResolved += outcome => Record(result, outcome);

            var intro = db.Balance.IntroEventId;
            if (!string.IsNullOrEmpty(intro) && db.TryGetEvent(intro, out _))
            {
                runner.ForceEvent(intro);
            }

            while (state.Status == GameStatus.Playing && state.Calendar.Week <= config.MaxWeeks)
            {
                // 1주차는 창고·탈출로 모두 잠겨 있다 (잠긴 칸은 전략이 건드리지 않음)
                BotTrader.Trade(state, db);
                BotRoles.Assign(state, db, config);
                BotPlanner.Plan(state, db, config, strategy, botRandom);

                runner.PrepareWeek();
                while (!runner.IsWeekComplete && !runner.IsGameOver)
                {
                    runner.ExecuteNextSlot();
                }

                if (runner.IsGameOver)
                {
                    break;
                }

                var week = state.Calendar.Week;
                runner.FinishWeek();
                result.Weeks.Add(Sample(state, week, db));
            }

            Finish(result, state, db);
            return result;
        }

        private static void Record(BotRunResult result, SlotOutcome outcome)
        {
            foreach (var back in outcome.OutingReturns ?? new List<OutingReturn>())
            {
                result.Outings.Add(new BotOutingSample
                {
                    Week = outcome.Week, NpcId = back.NpcId, Resources = back.Resources.Sum(r => r.Amount),
                    Item = !string.IsNullOrEmpty(back.ItemId), Wound = back.Wound,
                });
            }

            var expedition = outcome.Expedition;
            var request = outcome.ExpeditionRequest;
            if (expedition == null || request == null)
            {
                return;
            }

            int Loot(ResourceType type) => expedition.ResourceLoot.Where(r => r.Resource == type).Sum(r => r.Amount);
            result.Expeditions.Add(new BotExpeditionSample
            {
                Week = outcome.Week,
                ActionId = outcome.ActionId,
                LocationId = request.LocationId,
                Night = request.TimeOfDay == ExpeditionTime.Night,
                Status = expedition.Status,
                Food = Loot(ResourceType.Food),
                Water = Loot(ResourceType.Water),
                Medicine = Loot(ResourceType.Medicine),
                Materials = Loot(ResourceType.Materials),
                Items = expedition.Loot.Sum(i => i.Count),
                Bitten = outcome.Bite != null,
                Hostile = expedition.Hostile,
            });
        }

        private static BotWeekSample Sample(GameState state, int week, IDefinitionDatabase db)
        {
            var b = state.Base;
            var v = state.Player.Vitals;
            var joined = state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined).ToList();
            return new BotWeekSample
            {
                Companions = joined.Count,
                CompanionsInjured = joined.Count(n => n.Value.InjuredWeeks > 0),
                CompanionsMinor = joined.Count(n => n.Value.MinorWoundWeeks > 0),
                CompanionsOnAptitude = joined.Count(n => RoleRules.HasAptitude(db.Balance, n.Key, n.Value.Role) && RoleRules.IsActive(state, n.Key, db.Balance)),
                Week = week,
                Food = b.GetResource(ResourceType.Food),
                Water = b.GetResource(ResourceType.Water),
                RawWater = b.GetResource(ResourceType.RawWater),
                Cleanliness = v.Cleanliness,
                Injured = state.Flags.Contains("INJURED") ? 1 : 0,
                Medicine = b.GetResource(ResourceType.Medicine),
                Fuel = b.GetResource(ResourceType.Fuel),
                Materials = b.GetResource(ResourceType.Materials),
                Hp = v.Hp,
                Stress = v.Stress,
                Fatigue = v.Fatigue,
                Infection = v.Infection,
                SleepDebt = state.Player.SleepDebt,
                TeacherInfection = state.Teacher.InfectionProgress,
                Threat = b.Threat,
                Defense = b.Defense,
            };
        }

        private static void Finish(BotRunResult result, GameState state, IDefinitionDatabase db)
        {
            var ending = EndingResolver.Resolve(state, db);
            result.Status = state.Status;
            result.EndWeek = state.Calendar.Week;
            result.EndCause = state.EndCause ?? "";
            result.EndingId = ending.Ending?.Id ?? "";
            result.EndingCategory = ending.Ending?.Category;
            result.Raids = state.Exploration.Raids;
            result.RaidsRepelled = state.Exploration.RaidsRepelled;
            result.Intrusions = state.Exploration.Intrusions;
            result.IntrusionsLost = state.Exploration.IntrusionsLost;
            result.Companions = state.Npcs.Count(n => n.Value.Status == NpcStatus.Joined && !CatRules.IsCat(n.Key));
            result.Cat = CatRules.IsHome(state);
            result.Events = state.Events.Records.Count;
            foreach (var record in state.Events.Records)
            {
                result.EventRecords[record.Key] = (record.Value.Count, record.Value.LastWeek);
            }

            var flags = state.Flags;
            result.TeacherReinfected = state.Teacher.Reinfected;
            result.TeacherAlive = state.Teacher.Alive;
            result.TeacherBloodKnown = flags.Contains("TEACHER_BLOOD_KNOWN");
            result.NeededBloodPack = flags.Contains("NEED_BLOOD_PACK");
            result.GotBloodPack = flags.Contains("BLOOD_PACK") || flags.Contains("ENC_BLOOD_FRIDGE_TAKE");
            result.CureAttempted = flags.Contains("CURE_ATTEMPTED");
            result.TeacherCured = flags.Contains(InfectionRules.FlagTeacherCured);
            result.FinalChoice = string.Join("+", FinalChoiceFlags.Where(flags.Contains));
            result.Cds = CdRules.Count(state);
            result.ClubSessions = state.Npcs.Values.Sum(n => n.ClubCounts.Values.Sum());
            result.ClubScenes = state.Flags.Count(f => f.StartsWith("CLUB_BOND_", StringComparison.Ordinal));

            var context = EventContext.FromCalendar(state);
            foreach (var candidate in db.Extras.Endings.Where(e => e.Category != Definitions.EndingCategory.Bad))
            {
                if (ConditionEvaluator.IsMet(state, candidate.Condition, context, db.Balance))
                {
                    result.EligibleEndings.Add(candidate.Id);
                }
            }

            foreach (var (name, kind, key) in TrackedValues())
            {
                try
                {
                    result.FinalValues[name] = ConditionEvaluator.ValueOf(state, kind, key);
                }
                catch (ArgumentException)
                {
                    // 지원하지 않는 키는 건너뛴다
                }
            }

            foreach (var flag in TrackedFlagIds.Where(flags.Contains))
            {
                result.TrackedFlags.Add(flag);
            }
        }

        /// <summary>프롤로그 선택: 묶음 문항은 묶음마다 하나, 나머지 문항은 선택지 하나를 무작위로.</summary>
        public static List<(string Question, string Option)> PickPrologue(PrologueDef prologue, BotConfigDef config, IRandomService random)
        {
            var picks = new List<(string, string)>();
            if (prologue == null)
            {
                return picks;
            }

            foreach (var question in prologue.Questions)
            {
                var group = config.PrologueGroups.FirstOrDefault(g => g.QuestionId == question.Id);
                if (group != null)
                {
                    foreach (var set in group.Sets.Where(s => s.Count > 0))
                    {
                        picks.Add((question.Id, set[random.Range(0, set.Count - 1)]));
                    }
                }
                else if (question.Options.Count > 0)
                {
                    picks.Add((question.Id, question.Options[random.Range(0, question.Options.Count - 1)].Id));
                }
            }

            return picks;
        }

        /// <summary>실행마다 같은 값 (string.GetHashCode는 실행마다 달라질 수 있다).</summary>
        public static int StableHash(string text)
        {
            unchecked
            {
                var hash = 17;
                foreach (var c in text ?? "")
                {
                    hash = hash * 31 + c;
                }

                return hash;
            }
        }
    }
}
