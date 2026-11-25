using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>동료 특기 (기획 C.2 ②): 깃발 + 합류 + 호감 80 이상일 때만 켜지고, 효과는 비용·습격·방어·공물·결산 스트레스.</summary>
    public sealed class SpecialTests
    {
        private static GameState State(BalanceConfig balance, string npcId, int affinity, bool unlocked = true)
        {
            var state = new GameState();
            state.Npcs[npcId] = new NpcState { Status = NpcStatus.Joined, Affinity = affinity };
            if (unlocked)
            {
                state.Flags.Add(SpecialRules.Flag(balance, npcId));
            }

            return state;
        }

        [Test]
        public void Active_OnlyWithFlagJoinedAndAffinity()
        {
            var balance = new BalanceConfig();
            Assert.IsFalse(SpecialRules.IsActive(State(balance, "NPC_DOHYEON", 90, unlocked: false), balance, "NPC_DOHYEON"), "장면을 보지 않았으면 없다");
            Assert.IsTrue(SpecialRules.IsActive(State(balance, "NPC_DOHYEON", balance.SpecialMinAffinity), balance, "NPC_DOHYEON"));

            var cooled = State(balance, "NPC_DOHYEON", balance.SpecialMinAffinity - 1);
            Assert.IsTrue(SpecialRules.IsUnlocked(cooled, balance, "NPC_DOHYEON"));
            Assert.IsFalse(SpecialRules.IsActive(cooled, balance, "NPC_DOHYEON"), "호감이 떨어지면 잠시 꺼진다");

            var left = State(balance, "NPC_DOHYEON", 90);
            left.Npcs["NPC_DOHYEON"].Status = NpcStatus.Left;
            Assert.IsFalse(SpecialRules.IsActive(left, balance, "NPC_DOHYEON"), "떠난 동료의 특기는 없다");
        }

        [Test]
        public void Wanderer_Visitor_ActiveWithFlagOnly()
        {
            var balance = new BalanceConfig();
            var state = new GameState();
            Assert.IsFalse(SpecialRules.IsActive(state, balance, "NPC_WANDERER"), "방문 사다리를 끝내지 않았으면 없다");
            state.Flags.Add(SpecialRules.Flag(balance, "NPC_WANDERER"));
            Assert.IsTrue(SpecialRules.IsActive(state, balance, "NPC_WANDERER"), "합류·호감 없이 깃발만으로 켜진다");
            Assert.AreEqual(1f, SpecialRules.Value(state, balance, SpecialKind.DeepExitKnown));
        }

        [Test]
        public void Nurse_MedicineCostDown_OnlyForTwoOrMore()
        {
            var balance = new BalanceConfig();
            var state = State(balance, "NPC_NURSE", 85);
            Assert.AreEqual(1, SpecialRules.Cost(state, balance, new ResourceAmount(ResourceType.Medicine, 2)));
            Assert.AreEqual(1, SpecialRules.Cost(state, balance, new ResourceAmount(ResourceType.Medicine, 1)), "최소 1");
            Assert.AreEqual(4, SpecialRules.Cost(state, balance, new ResourceAmount(ResourceType.Materials, 4)), "의약품만");
            Assert.AreEqual(2, SpecialRules.Cost(new GameState(), balance, new ResourceAmount(ResourceType.Medicine, 2)), "특기 없으면 그대로");
        }

        [Test]
        public void Dohyeon_RaidBreachDamageCut()
        {
            var balance = new BalanceConfig();
            var scale = SpecialRules.RaidDamageScale(State(balance, "NPC_DOHYEON", 80), balance);
            Assert.AreEqual(0.8f, scale, 0.001f);
            Assert.AreEqual(8, SpecialRules.ScaleDamage(10, scale));
            Assert.AreEqual(1, SpecialRules.ScaleDamage(1, scale), "피해가 있으면 최소 1");
            Assert.AreEqual(1f, SpecialRules.RaidDamageScale(new GameState(), balance));
        }

        [Test]
        public void Senior_DefenseBonus_RaisesPowerByWeightedTen()
        {
            var balance = new BalanceConfig { RaidPowerVariance = 0, AlarmPower = 0 };
            var plain = new GameState();
            plain.Base.Defense = 20;
            var with = State(balance, "NPC_SURVIVOR_A", 80);
            with.Base.Defense = 20;
            var random = new ApoGirl.Core.Random.SeededRandom(ApoGirl.Core.Random.SeededRandom.CreateState(1));
            var gap = DefenseRules.Power(with, balance, 0, random, false) - DefenseRules.Power(plain, balance, 0, random, false);
            Assert.AreEqual((int)System.Math.Round(30 * balance.RaidDefenseWeight) - (int)System.Math.Round(20 * balance.RaidDefenseWeight), gap);
            Assert.AreEqual(10, SpecialRules.DefenseBonus(with, balance));
        }

        [Test]
        public void Ringleader_TributeOnlyForTargetFaction_BystanderWeeklyRelief()
        {
            var balance = new BalanceConfig();
            var state = State(balance, "NPC_RINGLEADER", 80);
            Assert.AreEqual(2f, SpecialRules.Value(state, balance, SpecialKind.TributeCut, "FAC_SAGEORI"));
            Assert.AreEqual(0f, SpecialRules.Value(state, balance, SpecialKind.TributeCut, "FAC_MART"));

            var calm = State(balance, "NPC_BYSTANDER", 80);
            Assert.AreEqual(3f, SpecialRules.Value(calm, balance, SpecialKind.WeeklyStressRelief));
        }
    }
}
