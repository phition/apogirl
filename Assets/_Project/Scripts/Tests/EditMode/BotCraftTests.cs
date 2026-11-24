using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.Simulation.Bots;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>제작 P7: 봇의 CRAFT 묶음 (착수·더 좋은 것이 있으면 건너뛰기·여러 칸 제작과 탐험 겹침·밤 허수아비 챙기기).</summary>
    public sealed class BotCraftTests
    {
        private static BotConfigDef Config() => BotConfigJson.Load(ContentPipelineTests.ProjectPath(BotConfigJson.DefaultPath));

        private static GameState State(IDefinitionDatabase db, int materials)
        {
            var state = NewGameFactory.Create(db.Balance, 5);
            state.Calendar.Week = 6;
            state.Flags.Add("LOC_SCHOOL_DAY");
            state.Base.Resources[ResourceType.Materials] = materials;
            return state;
        }

        private static string At(GameState state, string slot) => state.Plan.Slots[BotPlanner.SlotIndex(slot)].ActionId;

        [Test]
        public void Mixed_BuildsWorkbenchFirst_ThenSkipsWhatItAlreadyHasBetter()
        {
            var db = M6Act4Tests.ShippedDb();
            var config = Config();
            var strategy = config.Strategies.First(s => s.Id == "MIXED");
            var state = State(db, 20);

            BotPlanner.Plan(state, db, config, strategy, new SeededRandom(SeededRandom.CreateState(1)));
            Assert.AreEqual("ACT_CRAFT_WORKBENCH", At(state, "D4"), "작업대가 먼저");

            state.Flags.Add("WORKBENCH");
            BotPlanner.Plan(state, db, config, strategy, new SeededRandom(SeededRandom.CreateState(1)));
            Assert.AreEqual("ACT_CRAFT_SAND_FILTER", At(state, "D4"));

            state.Flags.Add("SAND_FILTER");
            state.Storage.Add(new ItemStack { ItemId = "ITEM_BAG_RUCKSACK", Count = 1 });
            BotPlanner.Plan(state, db, config, strategy, new SeededRandom(SeededRandom.CreateState(1)));
            Assert.AreEqual("ACT_CRAFT_CLUB", At(state, "D4"), "더 큰 가방이 있으면 끈 주머니는 건너뛴다");
        }

        [Test]
        public void MultiWorkCraft_LocksNextSlot_UnlessAnExpeditionIsThere()
        {
            var db = M6Act4Tests.ShippedDb();
            var config = Config();
            var strategy = config.Strategies.First(s => s.Id == "MIXED");
            var state = State(db, 20);
            state.Flags.UnionWith(new[] { "WORKBENCH", "SAND_FILTER", "SEARCHED_HOMEEC" });
            state.Player.Proficiency[ProficiencyType.Survival] = new ProficiencyState { Level = 2, Xp = db.Balance.ProficiencyXpThresholds[1] };
            foreach (var owned in new[] { "ITEM_BAG_FRAME", "ITEM_WEAPON_CLUB_REBAR", "ITEM_ARMOR_PLATED" })
            {
                state.Storage.Add(new ItemStack { ItemId = owned, Count = 1 });
            }

            BotPlanner.Plan(state, db, config, strategy, new SeededRandom(SeededRandom.CreateState(1)));
            Assert.AreEqual("ACT_CRAFT_BOIL_STATION", At(state, "D4"), "밤 탐험이 없으면 2칸 제작");
            var next = state.Plan.Slots[BotPlanner.SlotIndex("N4")];
            Assert.IsTrue(next.CraftLocked);
            Assert.AreEqual(db.Balance.CraftContinueActionId, next.ActionId);

            state.Flags.Add("NIGHT_RUNS");
            BotPlanner.Plan(state, db, config, strategy, new SeededRandom(SeededRandom.CreateState(1)));
            Assert.IsTrue(db.GetAction(At(state, "N4")).IsExpedition, "밤 탐험 칸은 그대로");
            var d4 = db.GetAction(At(state, "D4"));
            Assert.IsFalse(d4.IsCraft && d4.Craft.Work > 1, "뒤 칸이 탐험이면 여러 칸 제작은 건너뛴다");
        }

        [Test]
        public void NightCarry_TakesTheLongestDecoy_DayClearsIt()
        {
            var db = M6Act4Tests.ShippedDb();
            var state = State(db, 0);
            state.Storage.Add(new ItemStack { ItemId = "ITEM_DECOY_DUMMY", Count = 2 });
            state.Storage.Add(new ItemStack { ItemId = "ITEM_DECOY_BELL", Count = 1 });

            BotCraft.ChooseCarry(state, db, true);
            CollectionAssert.AreEqual(new[] { "ITEM_DECOY_BELL" }, state.Loadout.Carry);
            BotCraft.ChooseCarry(state, db, true);
            Assert.AreEqual(1, state.Loadout.Carry.Count, "하나만");
            BotCraft.ChooseCarry(state, db, false);
            Assert.IsEmpty(state.Loadout.Carry);
        }
    }
}
