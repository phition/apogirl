using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>탐험의 깊이 (기획 B): 깊은 곳 층 번호, 배치 점검, 헤드리스 깊은 곳 판정(CD는 깊이 들어간 판만, 다른 결과를 흔들지 않음).</summary>
    public sealed class DeepTests
    {
        /// <summary>겉 2층 + 깊은 곳 2층: 1층 방화문 ↔ 지하 1, 지하 1↔2 계단, 지하 2 비상구 → 2층.</summary>
        private static BuildingLayoutDef Layout(bool withExit = true)
        {
            var layout = TestContent.CreateLocation().Layout;   // 방·출구는 그대로, 층과 깊은 곳만 덧붙인다
            layout.Floors = 2;
            layout.DeepFloors = 2;
            layout.DeepCorridorLength = 20f;
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 0, X = 4f, TargetFloor = 1 });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 1, X = 2.6f, TargetFloor = 0 });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.DeepDoor, Floor = 0, X = 15f, TargetFloor = 2, TargetX = 3f });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.DeepDoor, Floor = 2, X = 3f, TargetFloor = 0, TargetX = 15f });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 2, X = 16f, TargetFloor = 3 });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 3, X = 14.6f, TargetFloor = 2 });
            if (withExit)
            {
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.OneWayExit, Floor = 3, X = 2f, TargetFloor = 1, TargetX = 20f });
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.OneWayBack, Floor = 1, X = 20f, TargetFloor = 3, TargetX = 2f });
            }

            return layout;
        }

        [Test]
        public void Layout_DeepFloorsFollowOuterFloors()
        {
            var layout = Layout();
            Assert.AreEqual(4, layout.TotalFloors);
            Assert.IsFalse(layout.IsDeep(1));
            Assert.IsTrue(layout.IsDeep(2));
            Assert.AreEqual(2, layout.DeepLevel(3), "층 번호 3 = 지하 2");
            Assert.AreEqual(20f, layout.CorridorLengthOf(3));
            Assert.AreEqual(30f, layout.CorridorLengthOf(0));
            Assert.AreEqual(1, DeepRules.EntryCount(layout));
        }

        [Test]
        public void Issues_GoodLayoutIsClean_MissingExitAndBrokenDoorAreReported()
        {
            CollectionAssert.IsEmpty(DeepRules.Issues(Layout()));

            var noExit = Layout(withExit: false);
            Assert.IsTrue(DeepRules.Issues(noExit).Any(i => i.Contains("일방 출구가 없음")));

            var oneSided = Layout();
            oneSided.Connectors.RemoveAll(c => c.Kind == ConnectorKind.DeepDoor && c.Floor == 2);
            var issues = DeepRules.Issues(oneSided);
            Assert.IsTrue(issues.Any(i => i.Contains("반대쪽")), "깊은 문은 양쪽에 있어야 한다");
            Assert.IsFalse(issues.Any(i => i.Contains("나갈 수 없음")), "일방 출구로는 나갈 수 있다");

            var flat = new BuildingLayoutDef { Floors = 1 };
            flat.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.DeepDoor, Floor = 0, X = 3f, TargetFloor = 0 });
            Assert.IsNotEmpty(DeepRules.Issues(flat), "깊은 곳이 없는 지역에 깊은 문");
        }

        private static InMemoryDefinitionDatabase Db(float deepChance, float bonus = 0f)
        {
            var location = TestContent.CreateLocation();
            var layout = Layout();
            layout.Containers.Add(new ContainerDef { Id = "DEEP_BOX", Floor = 3, X = 8f, LootTableId = TestContent.CreateLootTable().Id });
            location.Layout = layout;
            location.DayVariant.Encounters.Add(new EncounterDef
            {
                Id = "CD_09", Floor = 3, Collectible = true, FlagsNone = { "CD_09" },
                Choices = { new EncounterChoiceDef { Id = "TAKE", SetFlags = { "CD_09" } } },
            });
            return new InMemoryDefinitionDatabase(
                new BalanceConfig { HeadlessDeepChance = deepChance, HeadlessDeepSpecialBonus = bonus, HeadlessCdChance = 1f, HeadlessDeepItemChance = 1f },
                TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { location });
        }

        private static ExpeditionResult Run(InMemoryDefinitionDatabase db, int seed, bool knows = false)
        {
            var request = new ExpeditionRequest
            {
                LocationId = TestContent.Location, Week = 10, Seed = seed, TimeOfDay = ExpeditionTime.Day, Hp = 100, KnowsDeepExits = knows,
            };
            return new HeadlessExpeditionRunner(db).Run(request);
        }

        [Test]
        public void Headless_DeepCdOnlyWhenEnteringDeep_OtherResultsUnchanged()
        {
            var always = Db(1f);
            var never = Db(0f);
            var extracted = 0;
            for (var seed = 1; seed <= 40; seed++)
            {
                var a = Run(always, seed);
                var b = Run(never, seed);
                Assert.AreEqual(b.Status, a.Status);
                CollectionAssert.AreEqual(b.ResourceLoot.Select(r => r.Resource + ":" + r.Amount), a.ResourceLoot.Select(r => r.Resource + ":" + r.Amount));
                Assert.IsFalse(b.Flags.Contains("CD_09"), "깊이 들어가지 않으면 깊은 곳 CD는 없다");
                if (a.Status == ExtractStatus.Extracted)
                {
                    extracted++;
                    Assert.IsTrue(a.Flags.Contains("CD_09"));
                    Assert.Greater(a.StressDelta, b.StressDelta, "깊은 곳은 어둡고 길을 잃는다");
                    Assert.Greater(a.Loot.Count, b.Loot.Count, "깊은 곳 상자에서 하나");
                }
            }

            Assert.Greater(extracted, 0);
        }

        [Test]
        public void Headless_WandererSpecial_RaisesDeepChance()
        {
            var db = Db(0f, bonus: 1f);
            var withSpecial = Enumerable.Range(1, 30).Select(seed => Run(db, seed, knows: true)).Where(r => r.Status == ExtractStatus.Extracted).ToList();
            Assert.IsNotEmpty(withSpecial);
            Assert.IsTrue(withSpecial.All(r => r.Flags.Contains("CD_09")));
            Assert.IsFalse(Enumerable.Range(1, 30).Select(seed => Run(db, seed)).Any(r => r.Flags.Contains("CD_09")));
        }
    }
}
