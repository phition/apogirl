using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>윤 선생님 호감 구간 (기획 C.1): 선생님 수업의 성공률·경험치·대성공, 신뢰를 호감으로 합치기.</summary>
    public sealed class MentorBondTests
    {
        private static ActionDef Taught() => new ActionDef { Id = "ACT_T", Teaching = TeachingMode.TeacherDirect };

        [Test]
        public void Bands_ApplyOnlyToTaughtActions()
        {
            var balance = new BalanceConfig();
            var state = NewGameFactory.Create(balance, 1);
            state.Teacher.Affinity = 10;
            Assert.AreEqual(0f, MentorBondRules.CheckBonus(state, Taught(), balance), "30 미만은 지금과 같다");

            state.Teacher.Affinity = 65;
            Assert.AreEqual(0.10f, MentorBondRules.CheckBonus(state, Taught(), balance), 1e-4);
            Assert.AreEqual(12, MentorBondRules.ScaleXp(state, Taught(), 10, balance), "×1.2");
            Assert.AreEqual(0f, MentorBondRules.GreatSuccessBonus(state, Taught(), balance));
            Assert.AreEqual(0f, MentorBondRules.CheckBonus(state, new ActionDef { Teaching = TeachingMode.None }, balance), "혼자 하는 행동은 없음");

            state.Teacher.Affinity = 90;
            Assert.AreEqual(0.15f, MentorBondRules.CheckBonus(state, Taught(), balance), 1e-4);
            Assert.AreEqual(0.05f, MentorBondRules.GreatSuccessBonus(state, Taught(), balance), 1e-4);

            state.Teacher.Alive = false;
            Assert.AreEqual(0f, MentorBondRules.CheckBonus(state, Taught(), balance), "선생님이 없으면 없음");
        }

        [Test]
        public void TeacherTrust_MergesIntoAffinity()
        {
            var balance = new BalanceConfig();
            var state = NewGameFactory.Create(balance, 1);
            var before = state.Teacher.Affinity;
            EffectApplier.Apply(state, new GameEffect { Npcs = { new NpcChange { NpcId = EffectApplier.TeacherNpcId, Affinity = 2, Trust = 1 } } }, balance, new StateDelta());
            Assert.AreEqual(before + 3, state.Teacher.Affinity);
        }
    }
}
