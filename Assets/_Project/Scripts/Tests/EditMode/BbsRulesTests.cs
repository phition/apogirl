using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>PC통신 '이제누리': 모뎀 이야기를 들은 뒤에만 접속하고, 멎은 주에는 다시 켜지지 않는다.</summary>
    public sealed class BbsRulesTests
    {
        [Test]
        public void CanConnect_OnlyAfterTeacherTellsAboutModem()
        {
            var state = new GameState();
            Assert.IsFalse(BbsRules.CanConnect(state));
            state.Flags.Add(BbsRules.KnownFlag);
            Assert.IsTrue(BbsRules.CanConnect(state));
        }

        [Test]
        public void HasPower_NeedsFreePowerForTheComputer()
        {
            var balance = new ApoGirl.Core.Definitions.BalanceConfig();
            var state = new GameState();
            Assert.IsFalse(BbsRules.HasPower(state, balance), "발전기가 없으면 컴퓨터가 안 켜진다");
            state.Base.PowerRestored = true;
            state.Base.SetResource(ResourceType.Power, balance.BbsMinFreePower);
            Assert.IsTrue(BbsRules.HasPower(state, balance), "여유 전력이 필요한 만큼 있으면 켜진다");
            state.Base.SetResource(ResourceType.Power, balance.BbsMinFreePower - 1);
            Assert.IsFalse(BbsRules.HasPower(state, balance));
        }

        [Test]
        public void MarkDown_BlocksThisWeek_AndOpensNextWeek()
        {
            var state = new GameState();
            state.Flags.Add(BbsRules.KnownFlag);
            state.Calendar.Week = 20;
            BbsRules.MarkDown(state);
            Assert.IsTrue(BbsRules.IsDown(state));
            Assert.IsFalse(BbsRules.CanConnect(state));

            state.Calendar.Week = 21;
            Assert.IsFalse(BbsRules.IsDown(state), "다음 주에는 다시 켜진다");
            BbsRules.MarkDown(state);
            Assert.IsFalse(state.Flags.Contains(BbsRules.DownFlag(20)), "지난 주 깃발은 지운다");
            Assert.IsTrue(state.Flags.Contains(BbsRules.DownFlag(21)));
        }
    }
}
