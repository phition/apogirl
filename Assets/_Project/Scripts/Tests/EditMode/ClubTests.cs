using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>방과후 동아리 (확장 7): 같이 할 사람, 좋아하는 활동, 개인 장면 깃발, 주 1회 계획 제한.</summary>
    public sealed class ClubTests
    {
        private static ActionDef Club(string activity) => new ActionDef
        {
            Id = "ACT_CLUB_" + activity, Category = ActionCategory.Daily, SlotTag = SlotTag.NightOnly, ClubActivity = activity,
        };

        private static InMemoryDefinitionDatabase Db() => new InMemoryDefinitionDatabase(
            new BalanceConfig(), TestContent.CreateActions().Concat(new[] { Club("SPORTS"), Club("STUDY") }), new[] { TestContent.CreateModel() },
            TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() });

        private static GameState People()
        {
            var state = new GameState();
            state.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined, Affinity = 30 };
            state.Npcs["NPC_NURSE"] = new NpcState { Status = NpcStatus.Joined, Affinity = 50 };
            state.Npcs[CatRules.CatId] = new NpcState { Status = NpcStatus.Joined, Affinity = 99 };
            return state;
        }

        [Test]
        public void Partner_IsChosenOrClosest_NeverCatOrAway()
        {
            var state = People();
            Assert.AreEqual("NPC_NURSE", ClubRules.Partner(state), "고르지 않으면 호감이 가장 높은 사람 (고양이 제외)");

            state.ClubPartner = "NPC_DOHYEON";
            Assert.AreEqual("NPC_DOHYEON", ClubRules.Partner(state));

            state.Npcs["NPC_DOHYEON"].Away = true;
            Assert.AreEqual("NPC_NURSE", ClubRules.Partner(state), "고른 사람이 밖에 나가 있으면 다른 사람");

            state.ClubPartner = CatRules.CatId;
            state.Npcs["NPC_NURSE"].Status = NpcStatus.Left;
            Assert.IsNull(ClubRules.Partner(state));
        }

        [Test]
        public void Apply_LikedActivityGivesMore_AndUnlocksSceneAfterRepeats()
        {
            var db = Db();
            var balance = db.Balance;
            var state = People();
            state.ClubPartner = "NPC_DOHYEON";   // 도현은 운동을 좋아한다
            Assert.IsTrue(ClubRules.Likes(balance, "NPC_DOHYEON", "SPORTS"));

            var study = ClubRules.Apply(state, db, Club("STUDY"), new StateDelta());
            Assert.IsFalse(study.Liked);
            Assert.AreEqual(30 + balance.ClubAffinity, state.Npcs["NPC_DOHYEON"].Affinity);
            Assert.IsTrue(state.Flags.Contains(ClubRules.WithFlag("NPC_DOHYEON")));

            ClubOutcome last = null;
            for (var i = 0; i < balance.ClubSceneCount; i++)
            {
                Assert.IsFalse(state.Flags.Contains(ClubRules.SceneFlag("NPC_DOHYEON")));
                last = ClubRules.Apply(state, db, Club("SPORTS"), new StateDelta());
                Assert.IsTrue(last.Liked);
            }

            Assert.IsTrue(last.SceneUnlocked);
            Assert.IsTrue(state.Flags.Contains(ClubRules.SceneFlag("NPC_DOHYEON")));
            Assert.AreEqual(balance.ClubSceneCount, ClubRules.Count(state, "NPC_DOHYEON", "SPORTS"));
            Assert.AreEqual(30 + balance.ClubAffinity + balance.ClubLikedAffinity * balance.ClubSceneCount, state.Npcs["NPC_DOHYEON"].Affinity);

            // 다른 사람과 하면 같이 한 사람 깃발이 바뀐다
            state.ClubPartner = "NPC_NURSE";
            ClubRules.Apply(state, db, Club("STUDY"), new StateDelta());
            Assert.IsFalse(state.Flags.Contains(ClubRules.WithFlag("NPC_DOHYEON")));
            Assert.IsTrue(state.Flags.Contains(ClubRules.WithFlag("NPC_NURSE")));
            Assert.IsNull(ClubRules.Apply(state, db, TestContent.CreateActions().First(), new StateDelta()), "동아리가 아닌 행동");
        }

        [Test]
        public void Plan_AllowsOneClubPerWeek()
        {
            var db = Db();
            var state = TestContent.NewGame(db);
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, 1, "ACT_CLUB_SPORTS"));
            Assert.AreEqual(PlanEditResult.WeeklyLimit, PlanEditing.SetSlot(state, db, 3, "ACT_CLUB_STUDY"));
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, 1, "ACT_CLUB_STUDY"), "같은 칸을 바꾸는 건 된다");
        }
    }
}
