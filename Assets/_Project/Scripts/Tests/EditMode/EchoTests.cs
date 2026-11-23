using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>프롤로그의 메아리 (PROPOSAL_PROLOGUE_ECHOES): 성격 E·F, 좋아하는 과목, 희망과 의지·위기 이탈.</summary>
    public sealed class EchoTests
    {
        private const string Npc = "NPC_NURSE";

        private static GameState Make(BalanceConfig balance)
        {
            var state = NewGameFactory.Create(balance, 7);
            state.Calendar.Week = 10;
            state.Npcs[Npc] = new NpcState { Status = NpcStatus.Joined };
            return state;
        }

        [Test]
        public void TraitE_ReducesExpeditionStress_LikeT()
        {
            var db = M6Act4Tests.ShippedDb();
            var e = db.Extras.Traits.Find(t => t.Id == "PT_E");
            Assert.IsTrue(e.Modifiers.Any(m => m.Kind == TraitModifierKind.ExpeditionStress && m.Value < 0f), "E: 탐험 스트레스 감소");
        }

        [Test]
        public void TraitF_CompanionStressRise_ScalesWithAffinity()
        {
            var balance = new BalanceConfig();
            var state = Make(balance);
            state.Npcs[Npc].Affinity = 100;
            Assert.AreEqual(5, EchoRules.CompanionStressRise(state, balance, Npc, 5), "F가 없으면 그대로");

            state.Player.Traits.Add(balance.CompanionStressReliefTraitId);
            var total = 0;
            for (var day = 1; day <= 7; day++)
            {
                for (var week = 10; week < 20; week++)
                {
                    state.Calendar.Week = week;
                    state.Calendar.Day = day;
                    total += EchoRules.CompanionStressRise(state, balance, Npc, 5);
                }
            }

            // 호감 100 → ×(1 − 0.4) = 3, 70번
            Assert.AreEqual(5 * 70 * (1f - balance.CompanionStressReliefMax), total, 2f);
            Assert.AreEqual(-4, EchoRules.CompanionStressRise(state, balance, Npc, -4), "내릴 때는 그대로");

            state.Npcs[Npc].Affinity = -30;
            Assert.AreEqual(5, EchoRules.CompanionStressRise(state, balance, Npc, 5), "호감이 0 이하면 효과 없음");
        }

        [Test]
        public void FavoriteSubject_LessStress_MoreGain()
        {
            var db = M6Act4Tests.ShippedDb();
            var lesson = db.GetAction("ACT_EDU_HUMANITIES");

            GameState Run(string subject)
            {
                var state = Make(db.Balance);
                state.Player.Profile.FavoriteSubjectId = subject;
                ActionResolver.Resolve(state, lesson, db, new FixedRandom(0.3), new StateDelta());
                return state;
            }

            var plain = Run("MATH_SCI");
            var favorite = Run("KOREAN");
            Assert.AreEqual(plain.Player.Vitals.Stress - db.Balance.FavoriteSubjectStressRelief, favorite.Player.Vitals.Stress);
            var top = lesson.StatGains.OrderByDescending(g => g.Amount).First().Stat;
            Assert.AreEqual(plain.Player.GetStat(top) + db.Balance.FavoriteSubjectGainBonus, favorite.Player.GetStat(top), "가장 큰 상승치 +1");
            foreach (var gain in lesson.StatGains.Where(g => g.Stat != top))
            {
                Assert.AreEqual(plain.Player.GetStat(gain.Stat), favorite.Player.GetStat(gain.Stat), "다른 능력치는 그대로");
            }
        }

        [Test]
        public void FavoriteNone_FoundAtLevel()
        {
            var balance = new BalanceConfig();
            var state = Make(balance);
            state.Player.Profile.FavoriteSubjectId = balance.FavoriteNoneId;
            Assert.IsFalse(EchoRules.TryFindFavorite(state, balance, ProficiencyType.Medicine, new StateDelta()));

            state.Player.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = balance.FavoriteFoundLevel };
            Assert.IsTrue(EchoRules.TryFindFavorite(state, balance, ProficiencyType.Medicine, new StateDelta()));
            Assert.AreEqual("BIO", state.Player.Profile.FavoriteSubjectId);
            Assert.IsTrue(state.Flags.Contains(balance.FavoriteFoundFlag));

            state.Player.Proficiency[ProficiencyType.Humanities] = new ProficiencyState { Level = 5 };
            Assert.IsFalse(EchoRules.TryFindFavorite(state, balance, ProficiencyType.Humanities, new StateDelta()), "한 번 정해지면 그대로");
        }

        [Test]
        public void Hope_WillpowerGuardsLoss_AndHighHopeGrowsWillpower()
        {
            var balance = new BalanceConfig();
            var state = Make(balance);
            state.Player.SetStat(StatType.Willpower, 50);
            state.Player.Hidden.Hope = 50;
            EffectApplier.ChangeHidden(state, HiddenType.Hope, -10, balance, new StateDelta());
            Assert.AreEqual(42, state.Player.Hidden.Hope, "의지 50 → 20% 덜 깎임");
            EffectApplier.ChangeHidden(state, HiddenType.Hope, 5, balance, new StateDelta());
            Assert.AreEqual(47, state.Player.Hidden.Hope, "오를 때는 그대로");

            Assert.AreEqual(0, EchoRules.ApplyWeekly(state, balance, new StateDelta()));
            state.Player.Hidden.Hope = balance.HopeWillpowerThreshold;
            Assert.AreEqual(balance.HopeWillpowerWeekly, EchoRules.ApplyWeekly(state, balance, new StateDelta()));
            Assert.AreEqual(50 + balance.HopeWillpowerWeekly, state.Player.GetStat(StatType.Willpower));
        }

        [Test]
        public void Hope_SparesLeaveSometimes_Deterministic()
        {
            var balance = new BalanceConfig();
            var state = Make(balance);
            state.Player.Hidden.Hope = balance.CrisisHopeThreshold - 1;
            Assert.IsFalse(Enumerable.Range(1, 40).Any(w => { state.Calendar.Week = w; return EchoRules.HopeSparesLeave(state, balance, Npc); }));

            state.Player.Hidden.Hope = balance.CrisisHopeThreshold;
            var spared = Enumerable.Range(1, 400).Count(w => { state.Calendar.Week = w; return EchoRules.HopeSparesLeave(state, balance, Npc); });
            Assert.AreEqual(400 * balance.CrisisHopeSkipChance, spared, 40, "대략 확률만큼");
            state.Calendar.Week = 12;
            Assert.AreEqual(EchoRules.HopeSparesLeave(state, balance, Npc), EchoRules.HopeSparesLeave(state, balance, Npc), "같은 주·사람이면 같은 결과");
        }

        [Test]
        public void Hope_PersuadeBonus_MarksHopeHeld()
        {
            var db = M6Act4Tests.ShippedDb();
            var balance = db.Balance;
            var ev = db.Events.First(e => e.Id == "EVT_CRISIS_LEAVE_NURSE");
            var persuade = ev.Choices.First(c => c.Id == "PERSUADE");
            Assert.IsTrue(persuade.Check.UsesHope);

            var state = Make(balance);
            state.Player.Hidden.Hope = 0;
            var low = CheckResolver.ComputeChance(state, persuade.Check, balance);
            state.Player.Hidden.Hope = balance.CrisisHopeThreshold;
            var high = CheckResolver.ComputeChance(state, persuade.Check, balance);
            Assert.AreEqual(low + balance.CrisisHopePersuadeBonus, high, 1e-4f);

            // 희망이 없었으면 실패했을 굴림 → 통했고, 희망이 막았다
            var roll = (low + high) / 2f;
            var result = EventResolver.Choose(state, ev, "PERSUADE", new EventContext(), balance, new FixedRandom(roll), new StateDelta());
            Assert.AreNotEqual(CheckResult.Failure, result.Result);
            Assert.AreEqual(Npc, result.HopeHeldNpcId);
            Assert.IsTrue(state.Flags.Contains(EchoRules.HopeHeldFlag(balance, Npc)));

            // 희망 없이도 통했을 굴림 → 표시 없음
            state = Make(balance);
            state.Player.Hidden.Hope = balance.CrisisHopeThreshold;
            result = EventResolver.Choose(state, ev, "PERSUADE", new EventContext(), balance, new FixedRandom(0.01), new StateDelta());
            Assert.AreEqual("", result.HopeHeldNpcId);
        }
    }
}
