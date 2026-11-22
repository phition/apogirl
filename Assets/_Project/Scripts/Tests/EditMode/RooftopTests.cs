using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Systems.Content;
using NUnit.Framework;
using UnityEngine;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>학교 4층 옥상: 옥상에 오르면 남는 플래그(헤드리스 확률 포함) → 귀환 뒤 거점 옥상 해금 이벤트, 살림 역할 방 묶음.</summary>
    public sealed class RooftopTests
    {
        private const string Flag = "REACHED_ROOFTOP";

        private static InMemoryDefinitionDatabase Db(float chance, bool rooftop = true)
        {
            var location = TestContent.CreateLocation();
            if (rooftop)
            {
                location.Layout.RooftopFloor = 0;
                location.Layout.RooftopFlag = Flag;
            }

            return new InMemoryDefinitionDatabase(
                new BalanceConfig { HeadlessRooftopChance = chance }, TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { location });
        }

        private static System.Collections.Generic.List<ExpeditionResult> Runs(InMemoryDefinitionDatabase db, bool alreadyReached = false)
        {
            var runner = new HeadlessExpeditionRunner(db);
            return Enumerable.Range(1, 60)
                .Select(seed =>
                {
                    var request = new ExpeditionRequest { LocationId = TestContent.Location, Week = 2, Seed = seed, TimeOfDay = ExpeditionTime.Day, Hp = 100 };
                    if (alreadyReached) request.Flags.Add(Flag);
                    return runner.Run(request);
                })
                .Where(r => r.Status == ExtractStatus.Extracted)
                .ToList();
        }

        [Test]
        public void Headless_ChanceOne_AlwaysLeavesRooftopFlag()
        {
            var runs = Runs(Db(1f));
            Assert.IsNotEmpty(runs);
            Assert.IsTrue(runs.All(r => r.Flags.Contains(Flag)));
        }

        [Test]
        public void Headless_NoRooftopOrAlreadyReached_NoFlag()
        {
            Assert.IsTrue(Runs(Db(1f, rooftop: false)).All(r => !r.Flags.Contains(Flag)), "옥상이 없는 지역");
            Assert.IsTrue(Runs(Db(1f), alreadyReached: true).All(r => !r.Flags.Contains(Flag)), "이미 오른 적이 있으면 다시 남기지 않는다");
            Assert.IsTrue(Runs(Db(0f)).All(r => !r.Flags.Contains(Flag)), "확률 0");
        }

        [Test]
        public void Headless_DefaultChance_SometimesButNotAlways()
        {
            var runs = Runs(Db(new BalanceConfig().HeadlessRooftopChance));
            var reached = runs.Count(r => r.Flags.Contains(Flag));
            Assert.Greater(reached, 0);
            Assert.Less(reached, runs.Count);
        }

        [Test]
        public void Content_EarlyRooftopEvent_OpensRooftopAfterSchoolRun()
        {
            var root = Path.GetDirectoryName(Application.dataPath) ?? "";
            var content = ContentJson.LoadFolder(Path.Combine(root, ContentJson.ContentFolder));
            var early = content.Events.Single(e => e.Id == "EVT_A1_ROOFTOP_EARLY");
            CollectionAssert.Contains(early.Condition.FlagsAll, Flag);
            CollectionAssert.Contains(early.Condition.ActionIds, "ACT_EXP_DAY_SCHOOL");
            Assert.AreEqual(0, early.Condition.WeekMin, "4주차 전에도 열린다");
            Assert.IsTrue(early.OnShow.Facilities.Any(f => f.FacilityId == "ROOFTOP" && f.Level == 1));

            var house = content.Roles.Single(r => r.Id == "HOUSE");
            Assert.IsFalse(house.Rooms.Any(r => r.Id == "STORAGE" || r.Always), "살림은 가사실이 열려야 맡긴다 (창고 제외)");
        }
    }
}
