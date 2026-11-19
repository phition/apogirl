using System.IO;
using System.Linq;
using System.Text;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using NUnit.Framework;
using UnityEngine;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>
    /// M4-4: 실제 원본으로 1막(13주)을 세 전략(교육 위주 / 탐험 위주 / 혼합)으로 헤드리스 진행해
    /// 자원 곡선·쓰러짐·습격·이벤트 도달을 기록한다 (Temp/ApoGirlAct1Balance.txt).
    /// </summary>
    public sealed class Act1BalanceTests
    {
        private enum Strategy
        {
            Study,
            Expedition,
            Mixed,
        }

        private static InMemoryDefinitionDatabase Db()
        {
            var root = Path.GetDirectoryName(Application.dataPath) ?? "";
            var content = ContentJson.LoadFolder(Path.Combine(root, ContentJson.ContentFolder));
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), content.Actions, content.ExpeditionModels, content.Items, content.LootTables,
                null, content.Enemies, content.Events, content.Npcs,
                new DefinitionExtras { Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef() });
        }

        /// <summary>한 주 계획 (해금된 탐험 중 가장 좋은 곳을 고른다).</summary>
        private static void Plan(GameState state, IDefinitionDatabase db, Strategy strategy)
        {
            string Pick(params string[] ids) => ids.FirstOrDefault(id => db.TryGetAction(id, out var a) && ApoGirl.Core.Planning.PlanEditing.IsUnlocked(state, db, a)) ?? "";
            var dayExpedition = Pick("ACT_EXP_DAY_STORE", "ACT_EXP_DAY_SCHOOL");
            var slots = state.Plan.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].Locked)
                {
                    continue;
                }

                var day = i % 2 == 0;
                var dayIndex = i / 2;
                string id;
                switch (strategy)
                {
                    case Strategy.Study:
                        id = day ? (dayIndex % 2 == 0 ? "ACT_EDU_HUMANITIES" : "ACT_EDU_ENGINEERING") : "ACT_REST_NIGHT";
                        break;
                    case Strategy.Expedition:
                        id = day ? (dayIndex % 2 == 0 ? dayExpedition : "ACT_REST_DAY") : "ACT_REST_NIGHT";
                        break;
                    default:
                        id = day
                            ? dayIndex == 1 || dayIndex == 4 ? dayExpedition : dayIndex == 3 ? Pick("ACT_SURV_RESTORE_POWER", "ACT_SURV_MAKE_CRUTCH", "ACT_SURV_BARRICADE") : "ACT_EDU_HUMANITIES"
                            : dayIndex == 5 ? Pick("ACT_NIGHT_TALK") : "ACT_REST_NIGHT";
                        break;
                }

                if (string.IsNullOrEmpty(id) || !db.TryGetAction(id, out var action) || !ApoGirl.Core.Planning.PlanEditing.IsUnlocked(state, db, action)
                    || !ApoGirl.Core.Planning.PlanEditing.IsAllowedIn(action, i))
                {
                    id = day ? "ACT_REST_DAY" : "ACT_REST_NIGHT";
                }

                slots[i].ActionId = id;
            }
        }

        private static string Run(Strategy strategy, int seed, out GameState state)
        {
            var db = Db();
            state = NewGameFactory.Create(db.Balance, seed);
            EffectHelpers.ApplyPrologueCommon(state, db);
            state.Plan.Slots[0].ActionId = db.Balance.TutorialActionId;
            state.Plan.Slots[0].Locked = true;
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new HeadlessExpeditionRunner(db));
            var log = new StringBuilder();
            for (var week = 1; week <= 13 && state.Status == GameStatus.Playing; week++)
            {
                if (week > 1)
                {
                    Plan(state, db, strategy);
                }

                runner.PrepareWeek();
                while (!runner.IsWeekComplete && !runner.IsGameOver)
                {
                    runner.ExecuteNextSlot();
                }

                if (runner.IsGameOver)
                {
                    break;
                }

                runner.FinishWeek();
                var b = state.Base;
                log.AppendLine($"  w{week,2} food {b.GetResource(ResourceType.Food),3} water {b.GetResource(ResourceType.Water),3} med {b.GetResource(ResourceType.Medicine),2} mat {b.GetResource(ResourceType.Materials),2} " +
                               $"hp {state.Player.Vitals.Hp,3} stress {state.Player.Vitals.Stress,3} debt {state.Player.SleepDebt} def {b.Defense,2} threat {b.Threat,2} " +
                               $"exp {state.Exploration.DayExplorations + state.Exploration.NightExplorations} downs {state.Exploration.Downs} raids {state.Exploration.Raids}/{state.Exploration.RaidsRepelled}");
            }

            log.Insert(0, $"[{strategy} seed {seed}] status {state.Status} week {state.Calendar.Week} events {state.Events.Records.Count} review {state.Events.Records.ContainsKey("EVT_A1_REVIEW")}\n");
            return log.ToString();
        }

        [Test]
        public void Act1_ThreeStrategies_ReportAndReachReview()
        {
            var report = new StringBuilder();
            foreach (var strategy in new[] { Strategy.Study, Strategy.Expedition, Strategy.Mixed })
            {
                foreach (var seed in new[] { 11, 22, 33 })
                {
                    report.Append(Run(strategy, seed, out var state));
                    if (strategy != Strategy.Study)
                    {
                        Assert.AreEqual(GameStatus.Playing, state.Status, $"{strategy}/{seed}: 1막을 버텨야 함");
                        Assert.IsTrue(state.Events.Records.ContainsKey("EVT_A1_REVIEW"), $"{strategy}/{seed}: 1막 평가 도달");
                        Assert.IsTrue(state.Events.Records.ContainsKey("EVT_A1_FINALE"), $"{strategy}/{seed}: 막 마무리 습격");
                    }
                }
            }

            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/ApoGirlAct1Balance.txt", report.ToString());
            Debug.Log(report.ToString());
        }
    }

    internal static class EffectHelpers
    {
        public static void ApplyPrologueCommon(GameState state, IDefinitionDatabase db)
        {
            ApoGirl.Core.Effects.EffectApplier.Apply(state, db.Extras.Prologue.Common, db.Balance, new ApoGirl.Core.Effects.StateDelta());
        }
    }
}
