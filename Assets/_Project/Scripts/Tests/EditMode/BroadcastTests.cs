using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using NUnit.Framework;
using UnityEngine;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>방송반 송출 (확장 5): 해금 조건, 응답 굴림, 방송한 주의 습격 위험, 주 1회 계획 제한.</summary>
    public sealed class BroadcastTests
    {
        private static InMemoryDefinitionDatabase ContentDb()
        {
            var root = Path.GetDirectoryName(Application.dataPath) ?? "";
            var content = ContentJson.LoadFolder(Path.Combine(root, ContentJson.ContentFolder));
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), content.Actions, content.ExpeditionModels, content.Items, content.LootTables,
                null, content.Enemies, content.Events, content.Npcs,
                new DefinitionExtras
                {
                    Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef(),
                    Endings = content.Endings, Roles = content.Roles, StartBonuses = content.StartBonuses, Factions = content.Factions,
                });
        }

        private static GameState NewState(IDefinitionDatabase db)
        {
            var state = NewGameFactory.Create(db.Balance, 1);
            state.Calendar.Week = 30;
            FactionRules.EnsureInitialized(state, db);
            return state;
        }

        [Test]
        public void ShippedBroadcasts_UnlockAfterDjContact_SongNeedsCd()
        {
            var db = ContentDb();
            var state = NewState(db);
            var info = db.GetAction("ACT_BROADCAST_INFO");
            var song = db.GetAction("ACT_BROADCAST_SONG");
            state.Flags.Add("POWER_RESTORED");
            state.Base.PowerRestored = true;
            state.Base.Resources[ResourceType.Power] = 3;   // 자전거 발전기
            state.Base.Facilities["BROADCAST"] = 1;
            Assert.IsFalse(PlanEditing.IsUnlocked(state, db, info), "DJ와 이어지기 전");

            state.Flags.Add("DJ_CONTACT");
            Assert.IsTrue(PlanEditing.IsUnlocked(state, db, info));
            Assert.IsFalse(PlanEditing.IsUnlocked(state, db, song), "CD가 없으면 틀 곡이 없다");

            state.Flags.Add("CD_03");
            Assert.IsTrue(PlanEditing.IsUnlocked(state, db, song));

            state.Base.Resources[ResourceType.Power] = 2;
            state.Flags.Add("SEARCHED_HOMEEC");   // 냉장고가 1을 쓰면 여유 1
            Assert.IsFalse(PlanEditing.IsUnlocked(state, db, info), "여유 전력 2가 있어야 방송");
        }

        [Test]
        public void Apply_RollsReply_AndRaisesRaidRiskUntilTruceOrNextWeek()
        {
            var db = ContentDb();
            var balance = db.Balance;
            var state = NewState(db);
            var info = db.GetAction("ACT_BROADCAST_INFO");
            Assert.AreEqual(1f, BroadcastRules.RaidMultiplier(state, db, balance), "방송 전");

            var outcome = BroadcastRules.Apply(state, db, info, CheckResult.Success, new FixedRandom(balance.BroadcastReplyChance - 0.01));
            Assert.AreEqual("INFO", outcome.Program);
            Assert.IsTrue(outcome.Replied);
            Assert.IsTrue(state.Flags.Contains(BroadcastRules.FlagReply));
            Assert.IsTrue(outcome.Risky, "사거리 파는 처음에 경계 단계");
            Assert.AreEqual(balance.BroadcastRaidMultiplier, BroadcastRules.RaidMultiplier(state, db, balance), 1e-5);

            state.Factions["FAC_SAGEORI"].Standing = 0;   // 휴전
            Assert.AreEqual(1f, BroadcastRules.RaidMultiplier(state, db, balance), 1e-5);
            state.Factions["FAC_SAGEORI"].Standing = -40;

            var quiet = BroadcastRules.Apply(state, db, info, CheckResult.Success, new FixedRandom(0.99));
            Assert.IsFalse(quiet.Replied);
            Assert.IsFalse(state.Flags.Contains(BroadcastRules.FlagReply), "새 방송은 지난 응답을 지운다");

            BroadcastRules.StartWeek(state);
            Assert.AreEqual(1f, BroadcastRules.RaidMultiplier(state, db, balance), "다음 주에는 위험이 끝난다");

            Assert.IsNull(BroadcastRules.Apply(state, db, info, CheckResult.Failure, new FixedRandom()), "전력이 없어 못 내보냈다");
            Assert.IsNull(BroadcastRules.Apply(state, db, db.GetAction("ACT_REST_NIGHT"), CheckResult.Success, new FixedRandom()));
        }

        [Test]
        public void Plan_AllowsOneBroadcastPerWeek_ApartFromClub()
        {
            var db = ContentDb();
            var state = NewState(db);
            state.Flags.Add("POWER_RESTORED");
            state.Base.PowerRestored = true;
            state.Base.Resources[ResourceType.Power] = 3;   // 자전거 발전기
            state.Flags.Add("DJ_CONTACT");
            state.Base.Facilities["BROADCAST"] = 1;
            state.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined, Affinity = 30 };
            foreach (var slot in state.Plan.Slots)
            {
                slot.Locked = false;   // 1주차 창고 잠금은 이 시험과 상관없다
                slot.ActionId = "";
            }

            var nights = Enumerable.Range(0, state.Plan.Slots.Count).Where(i => CalendarRules.PhaseOf(i) == DayPhase.Night).ToList();
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, nights[1], "ACT_BROADCAST_INFO"));
            Assert.AreEqual(PlanEditResult.WeeklyLimit, PlanEditing.SetSlot(state, db, nights[2], "ACT_BROADCAST_LETTER"));
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, nights[2], "ACT_CLUB_STUDY"), "동아리는 다른 묶음");
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, nights[1], "ACT_BROADCAST_LETTER"), "같은 칸을 바꾸는 건 된다");
        }
    }
}
