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
        public void Warn_BelowThreshold_EvenWithoutCrisis_LowestFirst()
        {
            var balance = new BalanceConfig();
            var state = State(("NPC_DOHYEON", balance.CrisisLeaveBelowAffinity - 1), ("NPC_BYSTANDER", -12), ("NPC_NURSE", 30));
            var warned = CrisisRules.WarnWeekly(state, balance, false);
            CollectionAssert.AreEqual(new[] { "NPC_BYSTANDER" }, warned, "물자가 있어도(위기가 아니어도) 한 주에 한 명, 호감이 가장 낮은 사람");
            Assert.IsTrue(state.Flags.Contains(CrisisRules.WarnFlag(balance, "NPC_BYSTANDER")));

            CollectionAssert.AreEqual(new[] { "NPC_DOHYEON" }, CrisisRules.WarnWeekly(state, balance, false), "이미 예고한 사람은 건너뛴다");
            Assert.IsEmpty(CrisisRules.WarnWeekly(state, balance, false), "기준 이상(간호 30)은 후보가 아니다");
        }

        [Test]
        public void Warn_MarksHardTimes_OnlyInCrisisWeek()
        {
            var balance = new BalanceConfig();
            var state = State(("NPC_DOHYEON", 0), ("NPC_BYSTANDER", -5));
            CrisisRules.WarnWeekly(state, balance, false);
            Assert.IsFalse(state.Flags.Contains(CrisisRules.HardTimesFlag(balance, "NPC_BYSTANDER")), "물자가 있고 습격도 없으면 평소 말");

            state.Flags.Add(CrisisRules.HardTimesFlag(balance, "NPC_DOHYEON"));   // 지난번 위기 때 남은 깃발
            state.Base.Resources[ResourceType.Food] = 0;
            CrisisRules.WarnWeekly(state, balance, false);
            Assert.IsTrue(state.Flags.Contains(CrisisRules.HardTimesFlag(balance, "NPC_DOHYEON")), "식량이 바닥난 주면 위기 때 말");

            var raid = State(("NPC_NURSE", 0));
            raid.Flags.Add(CrisisRules.HardTimesFlag(balance, "NPC_NURSE"));
            CrisisRules.WarnWeekly(raid, balance, true);
            Assert.IsTrue(raid.Flags.Contains(CrisisRules.HardTimesFlag(balance, "NPC_NURSE")), "습격에 뚫린 주도 위기");

            var calm = State(("NPC_NURSE", 0));
            calm.Flags.Add(CrisisRules.HardTimesFlag(balance, "NPC_NURSE"));
            CrisisRules.WarnWeekly(calm, balance, false);
            Assert.IsFalse(calm.Flags.Contains(CrisisRules.HardTimesFlag(balance, "NPC_NURSE")), "평소에 다시 예고되면 지난 위기 깃발은 지운다");
        }

        [Test]
        public void Warn_TeacherNeverLeaves_ThresholdIsStrict()
        {
            var balance = new BalanceConfig();
            var state = State(("NPC_TEACHER", -50), ("NPC_NURSE", balance.CrisisLeaveBelowAffinity));
            Assert.IsEmpty(CrisisRules.WarnWeekly(state, balance, false), "선생님은 떠나지 않고, 호감이 기준과 같으면 후보가 아니다");

            state.Npcs["NPC_NURSE"].Affinity = balance.CrisisLeaveBelowAffinity - 1;
            CollectionAssert.AreEqual(new[] { "NPC_NURSE" }, CrisisRules.WarnWeekly(state, balance, false));
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
