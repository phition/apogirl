using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>기획 C.2 ⑤: 함께 엔딩 조건 "관계 높은 동료 2명 이상" (CloseCompanions, Key = 기준 호감).</summary>
    public sealed class CloseCompanionsTests
    {
        [Test]
        public void CountsJoinedAtOrAboveKey()
        {
            var state = new GameState();
            state.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined, Affinity = 60 };
            state.Npcs["NPC_NURSE"] = new NpcState { Status = NpcStatus.Joined, Affinity = 59 };
            state.Npcs["NPC_SURVIVOR_A"] = new NpcState { Status = NpcStatus.Left, Affinity = 90 };
            state.Npcs["NPC_BYSTANDER"] = new NpcState { Status = NpcStatus.Joined, Affinity = 85 };

            Assert.AreEqual(2, ConditionEvaluator.ValueOf(state, ValueKind.CloseCompanions, "60"), "떠난 사람·기준 미만은 세지 않는다");
            Assert.AreEqual(1, ConditionEvaluator.ValueOf(state, ValueKind.CloseCompanions, "80"));
        }
    }
}
