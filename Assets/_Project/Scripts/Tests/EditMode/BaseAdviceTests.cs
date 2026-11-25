using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>거점 상황판 "다음 할 일" (BASE_TAB_PROPOSAL P4): 데이터 점검, 조건·잠긴 행동·이미 계획한 행동 거르기, 우선순위.</summary>
    public sealed class BaseAdviceTests
    {
        private static string Root => Path.GetDirectoryName(Application.dataPath) ?? "";

        private static ContentFile Content() => ContentJson.LoadFolder(Path.Combine(Root, ContentJson.ContentFolder));

        private static InMemoryDefinitionDatabase ContentDb()
        {
            var content = Content();
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), content.Actions, content.ExpeditionModels, content.Items, content.LootTables,
                null, content.Enemies, content.Events, content.Npcs,
                new DefinitionExtras
                {
                    Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef(),
                    Endings = content.Endings, Roles = content.Roles, StartBonuses = content.StartBonuses, Factions = content.Factions,
                    Advice = content.Advice,
                });
        }

        private static GameState NewState(IDefinitionDatabase db)
        {
            var state = NewGameFactory.Create(db.Balance, 1);
            state.Calendar.Week = 20;
            state.Flags.Add("UI_DEFENSE");
            state.Flags.Add("ACT1_DONE");
            state.Base.SetResource(ResourceType.Food, 200);
            state.Base.SetResource(ResourceType.Water, 200);
            return state;
        }

        private static List<string> Ids(GameState state, IDefinitionDatabase db, int max = 50) =>
            BaseAdvice.Pick(state, db, BaseForecast.Raid(state, db), BaseForecast.Food(state, db), BaseForecast.Water(state, db), max)
                .Select(a => a.Id).ToList();

        [Test]
        public void ShippedAdvice_HasTextAndKnownActionsAndTargets()
        {
            var content = Content();
            Assert.Greater(content.Advice.Count, 5);
            var ko = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Assets/_Project/Localization/Source/ko.json")));
            var keys = new HashSet<string>(ko.Descendants().OfType<JProperty>().Select(p => p.Name));
            var actions = new HashSet<string>(content.Actions.Select(a => a.Id));
            Assert.AreEqual(content.Advice.Count, content.Advice.Select(a => a.Id).Distinct().Count(), "Id가 겹치지 않는다");
            foreach (var advice in content.Advice)
            {
                Assert.IsTrue(keys.Contains("advice." + advice.Id), advice.Id + " 문구");
                if (!string.IsNullOrEmpty(advice.ActionId))
                {
                    Assert.IsTrue(actions.Contains(advice.ActionId), advice.Id + " 행동 " + advice.ActionId);
                }

                Assert.IsTrue(advice.Target == "" || advice.Target == "roles" || advice.Target == "board", advice.Id + " Target " + advice.Target);
            }
        }

        [Test]
        public void IdleCompanion_AsksForRole_UntilAssigned()
        {
            var db = ContentDb();
            var state = NewState(db);
            state.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined, Affinity = 20 };
            Assert.Contains("ADV_IDLE", Ids(state, db));

            state.Npcs["NPC_DOHYEON"].Role = "GUARD";
            CollectionAssert.DoesNotContain(Ids(state, db), "ADV_IDLE");
        }

        [Test]
        public void WaterShort_WithRawWater_SuggestsPurify_UnlessAlreadyPlanned()
        {
            var db = ContentDb();
            var state = NewState(db);
            state.Base.SetResource(ResourceType.Water, 0);
            state.Base.SetResource(ResourceType.RawWater, 10);
            var ids = Ids(state, db);
            Assert.Contains("ADV_WATER_SHORT_RAW", ids);
            Assert.Contains("ADV_WATER_SHORT", ids);
            Assert.Less(ids.IndexOf("ADV_WATER_SHORT_RAW"), ids.IndexOf("ADV_WATER_SHORT"), "우선순위가 높은 줄이 먼저");

            state.Plan.Slots[0].ActionId = "ACT_SURV_PURIFY_WATER";
            CollectionAssert.DoesNotContain(Ids(state, db), "ADV_WATER_SHORT_RAW");

            // 빗물이 없으면 정수 행동이 잠겨 권하지 않는다
            state.Plan.Slots[0].ActionId = "";
            state.Base.SetResource(ResourceType.RawWater, 0);
            CollectionAssert.DoesNotContain(Ids(state, db), "ADV_WATER_SHORT_RAW");
        }

        [Test]
        public void Pick_TakesAtMostMax_AndDoesNotChangeState()
        {
            var db = ContentDb();
            var state = NewState(db);
            state.Base.SetResource(ResourceType.Water, 0);
            state.Base.SetResource(ResourceType.Food, 0);
            state.Base.SetResource(ResourceType.RawWater, 10);
            state.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined, Affinity = 20 };
            var rng = state.Rng.State;
            Assert.Greater(Ids(state, db).Count, 2);
            Assert.AreEqual(2, Ids(state, db, 2).Count);
            Assert.AreEqual(rng, state.Rng.State, "난수를 쓰지 않는다");
        }
    }
}
