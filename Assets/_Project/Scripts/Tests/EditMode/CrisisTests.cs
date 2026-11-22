using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>위기 때 남느냐 떠나느냐 (기획 C.2 ③): 위기 주 예고, 습격 때 대신 다치기.</summary>
    public sealed class CrisisTests
    {
        private static GameState State(params (string Id, int Affinity)[] npcs)
        {
            var state = new GameState();
            state.Base.Resources[ResourceType.Food] = 10;
            state.Base.Resources[ResourceType.Water] = 10;
            foreach (var (id, affinity) in npcs)
            {
                state.Npcs[id] = new NpcState { Status = NpcStatus.Joined, Affinity = affinity };
            }

            return state;
        }

        [Test]
        public void Warn_OnlyInCrisisWeek_LowestBelowZeroFirst()
        {
            var balance = new BalanceConfig();
            var state = State(("NPC_DOHYEON", -5), ("NPC_BYSTANDER", -12), ("NPC_NURSE", 30));
            Assert.IsEmpty(CrisisRules.WarnWeekly(state, balance, raidBreached: false), "물자가 있고 습격도 없으면 위기가 아니다");

            state.Base.Resources[ResourceType.Water] = 0;
            var warned = CrisisRules.WarnWeekly(state, balance, raidBreached: false);
            CollectionAssert.AreEqual(new[] { "NPC_BYSTANDER" }, warned, "한 주에 한 명, 호감이 가장 낮은 사람");
            Assert.IsTrue(state.Flags.Contains(CrisisRules.WarnFlag(balance, "NPC_BYSTANDER")));

            CollectionAssert.AreEqual(new[] { "NPC_DOHYEON" }, CrisisRules.WarnWeekly(state, balance, raidBreached: false), "이미 예고한 사람은 건너뛴다");
        }

        [Test]
        public void Warn_RaidBreach_IsCrisis_TeacherNeverLeaves()
        {
            var balance = new BalanceConfig();
            var state = State(("NPC_TEACHER", -50), ("NPC_NURSE", 10));
            Assert.IsEmpty(CrisisRules.WarnWeekly(state, balance, raidBreached: true), "선생님은 떠나지 않고, 호감 0 이상은 후보가 아니다");

            state.Npcs["NPC_NURSE"].Affinity = -1;
            CollectionAssert.AreEqual(new[] { "NPC_NURSE" }, CrisisRules.WarnWeekly(state, balance, raidBreached: true));
        }

        [Test]
        public void Cover_HighestAffinityUninjured_TakesTheHit()
        {
            var balance = new BalanceConfig { RaidCoverChance = 1f };
            var state = State(("NPC_DOHYEON", 70), ("NPC_SURVIVOR_A", 90), ("NPC_NURSE", 40));
            state.Npcs["NPC_SURVIVOR_A"].InjuredWeeks = 1;
            var random = new SeededRandom(SeededRandom.CreateState(3));

            Assert.AreEqual("NPC_DOHYEON", CrisisRules.RollCover(state, balance, random), "다친 사람은 못 막는다");
            Assert.AreEqual(balance.RaidCoverInjuryWeeks, state.Npcs["NPC_DOHYEON"].InjuredWeeks);
            Assert.AreEqual("", CrisisRules.RollCover(state, balance, random), "이제 막을 사람이 없다 (간호 40은 기준 미만)");
        }

        [Test]
        public void Cover_ChanceZero_NobodyCovers()
        {
            var balance = new BalanceConfig { RaidCoverChance = 0f };
            var state = State(("NPC_DOHYEON", 90));
            Assert.AreEqual("", CrisisRules.RollCover(state, balance, new SeededRandom(SeededRandom.CreateState(1))));
            Assert.AreEqual(0, state.Npcs["NPC_DOHYEON"].InjuredWeeks);
        }
    }
}
