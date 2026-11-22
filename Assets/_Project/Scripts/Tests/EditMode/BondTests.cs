using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>관계 1단계 (기획 C.2 ①·C.4 ①): 단계 경계, 역할 효율, 이벤트로 오르는 호감 배율.</summary>
    public sealed class BondTests
    {
        [Test]
        public void Tier_Boundaries()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(0, BondRules.Tier(-1, balance));
            Assert.AreEqual(1, BondRules.Tier(0, balance));
            Assert.AreEqual(2, BondRules.Tier(20, balance));
            Assert.AreEqual(3, BondRules.Tier(50, balance));
            Assert.AreEqual(4, BondRules.Tier(80, balance));
        }

        [Test]
        public void RoleMultiplier_ByBond()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(0.5f, BondRules.RoleMultiplier(new NpcState { Affinity = -5 }, balance));
            Assert.AreEqual(1f, BondRules.RoleMultiplier(new NpcState { Affinity = 30 }, balance));
            Assert.AreEqual(1.25f, BondRules.RoleMultiplier(new NpcState { Affinity = 60 }, balance));
            Assert.AreEqual(1.5f, BondRules.RoleMultiplier(new NpcState { Affinity = 90 }, balance));
        }

        [Test]
        public void ContentGain_DoubledForCompanions_NotTeacherOrLoss()
        {
            var balance = new BalanceConfig();
            var state = NewGameFactory.Create(balance, 1);
            state.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined, Affinity = 10 };
            var effect = new GameEffect();
            effect.Npcs.Add(new NpcChange { NpcId = "NPC_DOHYEON", Affinity = 3 });
            effect.Npcs.Add(new NpcChange { NpcId = EffectApplier.TeacherNpcId, Affinity = 3 });
            var teacherBefore = state.Teacher.Affinity;
            EffectApplier.Apply(state, effect, balance, new StateDelta());
            Assert.AreEqual(16, state.Npcs["NPC_DOHYEON"].Affinity, "동료는 ×2");
            Assert.AreEqual(teacherBefore + 3, state.Teacher.Affinity, "선생님은 그대로");
            Assert.AreEqual(3, effect.Npcs[0].Affinity, "정의는 바뀌지 않는다");

            var loss = new GameEffect();
            loss.Npcs.Add(new NpcChange { NpcId = "NPC_DOHYEON", Affinity = -4 });
            EffectApplier.Apply(state, loss, balance, new StateDelta());
            Assert.AreEqual(12, state.Npcs["NPC_DOHYEON"].Affinity, "떨어질 때는 곱하지 않는다");
        }
    }
}
