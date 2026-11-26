using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>습격 조정 (2026-11-26 사용자): 방어도 손실 3·10, 습격 확률 상한 30%, 밤 탐험 중 습격(주인공 전투력 없음·방어도는 주인공 몫만 빠짐).</summary>
    public sealed class RaidAbsenceTests
    {
        /// <summary>주인공 + 선생님·도현·박 선생님 = 넷. 고양이와 아직 합류하지 않은 사람은 세지 않는다.</summary>
        private static GameState FourAtBase(BalanceConfig balance)
        {
            var state = NewGameFactory.Create(balance, 5);
            state.Calendar.Week = 20;
            state.Npcs.Clear();
            state.Npcs[EffectApplier.TeacherNpcId] = new NpcState { Status = NpcStatus.Joined };
            state.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined };
            state.Npcs["NPC_NURSE"] = new NpcState { Status = NpcStatus.Joined };
            state.Npcs[CatRules.CatId] = new NpcState { Status = NpcStatus.Joined };
            state.Npcs["NPC_BYSTANDER"] = new NpcState { Status = NpcStatus.Met };
            state.Base.Defense = 40;
            state.Base.Traps = 0;
            state.Flags.Remove(DefenseRules.FlagAlarm);
            return state;
        }

        [Test]
        public void Defaults_DefenseLoss3And10_RaidCap30()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(3, balance.RaidRepelDefenseLoss);
            Assert.AreEqual(10, balance.RaidBreachDefenseLoss);
            Assert.AreEqual(0.30f, balance.RaidMaxChance, 1e-6f);
        }

        [Test]
        public void RaidChance_IsCappedAt30Percent()
        {
            var balance = new BalanceConfig { RaidBaseChance = 0.5f };
            var state = FourAtBase(balance);
            Assert.AreEqual(0.30f, RaidRules.Chance(state, balance), 1e-6f);
        }

        [Test]
        public void ExpeditionAbsence_DropsPlayerPower_AndOnlyHerShareOfDefense()
        {
            var balance = new BalanceConfig();
            var state = FourAtBase(balance);
            Assert.AreEqual(4, DefenseRules.HeadCount(state));
            Assert.AreEqual(0.75f, DefenseRules.AbsenceDefenseFactor(state, balance, RaidAbsence.Expedition), 1e-6f);

            var defense = (state.Base.Defense + SpecialRules.DefenseBonus(state, balance)) * balance.RaidDefenseWeight;
            var home = DefenseRules.Power(state, balance, 0, new FixedRandom(), false, RaidAbsence.None);
            var outside = DefenseRules.Power(state, balance, 0, new FixedRandom(), false, RaidAbsence.Expedition);
            Assert.AreEqual(balance.RaidBasePower + DerivedStats.CombatPower(state, balance) + (int)System.Math.Round(defense), home);
            Assert.AreEqual(balance.RaidBasePower + (int)System.Math.Round(defense * 0.75f), outside, "전투력 없이, 방어도 3/4");

            // 혼자면 방어도가 하나도 들지 않는다
            state.Npcs.Clear();
            Assert.AreEqual(0f, DefenseRules.AbsenceDefenseFactor(state, balance, RaidAbsence.Expedition), 1e-6f);
        }

        [Test]
        public void NightExpedition_RollsRaid_SparesPlayer_NoTripFlag()
        {
            var balance = new BalanceConfig();
            var state = FourAtBase(balance);
            state.Npcs.Remove(CatRules.CatId);   // 고양이 경고(피해 ×0.85) 없이 손실을 그대로 본다
            state.Base.Threat = 100;
            state.Base.Defense = 30;
            state.Base.Resources[ResourceType.Food] = 20;
            var hp = state.Player.Vitals.Hp;
            var expedition = new ActionDef { Id = "ACT_EXP_NIGHT_TEST", Category = ActionCategory.Expedition };

            var result = RaidRules.RollNight(state, expedition, balance, new FixedRandom(0.0), new StateDelta());
            Assert.IsNotNull(result, "밤 탐험 칸도 습격을 굴린다");
            Assert.IsTrue(result.Away);
            Assert.IsTrue(result.WhileExpedition);
            Assert.IsFalse(result.Repelled);
            Assert.AreEqual(hp, state.Player.Vitals.Hp, "주인공은 밖에 있어 다치지 않는다");
            Assert.AreEqual(20, state.Base.Defense, "뚫리면 방어도 −10");
            Assert.Less(state.Base.GetResource(ResourceType.Food), 20);
            Assert.IsFalse(state.Flags.Contains(RaidRules.FlagWhileAway), "수학여행 귀환 장면용 깃발은 켜지 않는다");
        }

        [Test]
        public void Repel_LosesThreeDefense()
        {
            var balance = new BalanceConfig();
            var state = FourAtBase(balance);
            var result = RaidRules.Resolve(state, 0, 0, false, balance, new FixedRandom(), new StateDelta());
            Assert.IsTrue(result.Repelled);
            Assert.AreEqual(37, state.Base.Defense);
        }
    }
}
