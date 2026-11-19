using ApoGirl.Core.Definitions;
using ApoGirl.Exploration.Model;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class PlayerViewRulesTests
    {
        private readonly BalanceConfig _balance = new BalanceConfig();

        [Test]
        public void OtherArea_NeverVisible()
        {
            Assert.IsFalse(PlayerViewRules.CanPlayerSee(0, 10f, 1, false, false, 1, 10f, _balance), "낮이라도 다른 층");
            Assert.IsFalse(PlayerViewRules.CanPlayerSee(0, 10f, 1, true, true, 1, 10f, _balance));
        }

        [Test]
        public void Day_WholeAreaVisible()
        {
            Assert.IsTrue(PlayerViewRules.CanPlayerSee(0, 10f, 1, false, false, 0, 70f, _balance));
        }

        [Test]
        public void Night_OnlyNearby_FlashlightExtendsForward()
        {
            Assert.IsTrue(PlayerViewRules.CanPlayerSee(0, 10f, 1, true, false, 0, 13f, _balance), "3유닛");
            Assert.IsFalse(PlayerViewRules.CanPlayerSee(0, 10f, 1, true, false, 0, 15f, _balance), "5유닛, 손전등 없음");
            Assert.IsTrue(PlayerViewRules.CanPlayerSee(0, 10f, 1, true, true, 0, 18f, _balance), "앞쪽 8유닛, 손전등");
            Assert.IsFalse(PlayerViewRules.CanPlayerSee(0, 10f, 1, true, true, 0, 2f, _balance), "뒤쪽 8유닛은 손전등으로도 안 보임");
            Assert.IsFalse(PlayerViewRules.CanPlayerSee(0, 10f, 1, true, true, 0, 21f, _balance), "손전등 거리 밖");
        }
    }
}
