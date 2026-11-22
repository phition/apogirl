using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using ApoGirl.Systems.Save;
using NUnit.Framework;
using UnityEngine;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>CD 수집 (기획 A): 깃발·주운 주차, 헤드리스 확률(다른 결과를 흔들지 않음), 음악실 기록, 지역 배치.</summary>
    public sealed class CdTests
    {
        private static InMemoryDefinitionDatabase Db(float chance)
        {
            var location = TestContent.CreateLocation();
            location.DayVariant = location.DayVariant ?? new LocationVariantDef();
            location.DayVariant.Encounters.Add(new EncounterDef
            {
                Id = "CD_09", Collectible = true, FlagsNone = { "CD_09" },
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", SetFlags = { "CD_09" } },
                    new EncounterChoiceDef { Id = "LEAVE", Removes = false },
                },
            });
            return new InMemoryDefinitionDatabase(
                new BalanceConfig { HeadlessCdChance = chance }, TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { location });
        }

        private static ExpeditionResult Run(InMemoryDefinitionDatabase db, int seed, bool owned = false)
        {
            var request = new ExpeditionRequest { LocationId = TestContent.Location, Week = 3, Seed = seed, TimeOfDay = ExpeditionTime.Day, Hp = 100 };
            if (owned) request.Flags.Add("CD_09");
            return new HeadlessExpeditionRunner(db).Run(request);
        }

        [Test]
        public void IsCd_OnlyNumberedFlags()
        {
            Assert.IsTrue(CdRules.IsCd("CD_01"));
            Assert.IsFalse(CdRules.IsCd("CD_SHELF_FULL"));
            Assert.IsFalse(CdRules.IsCd("ENC_CD_01_TAKE"));
            Assert.IsFalse(CdRules.IsCd("CD_"));
        }

        [Test]
        public void Record_KeepsFirstWeek_SelectNeedsOwnership()
        {
            var state = new GameState();
            state.Calendar.Week = 5;
            state.Flags.Add("CD_02");
            CollectionAssert.AreEqual(new[] { "CD_02" }, CdRules.Record(state, new[] { "CD_02", "NOTICE_TAKEN" }));
            state.Calendar.Week = 9;
            Assert.IsEmpty(CdRules.Record(state, new[] { "CD_02" }), "이미 적은 CD는 다시 적지 않는다");
            Assert.AreEqual(5, state.CdWeeks["CD_02"]);

            Assert.IsFalse(CdRules.Select(state, "CD_03"), "없는 CD는 못 튼다");
            Assert.IsTrue(CdRules.Select(state, "CD_02"));
            Assert.AreEqual("CD_02", state.CdTrack);
            Assert.AreEqual(1, CdRules.Count(state));
        }

        [Test]
        public void Headless_Chance_PicksCd_WithoutShakingOtherResults()
        {
            var always = Db(1f);
            var never = Db(0f);
            var picked = 0;
            for (var seed = 1; seed <= 40; seed++)
            {
                var a = Run(always, seed);
                var b = Run(never, seed);
                Assert.AreEqual(b.Status, a.Status);
                CollectionAssert.AreEqual(b.ResourceLoot.Select(r => r.Resource + ":" + r.Amount), a.ResourceLoot.Select(r => r.Resource + ":" + r.Amount), "CD 확률은 따로 굴린다");
                Assert.IsFalse(b.Flags.Contains("CD_09"));
                if (a.Status == ExtractStatus.Extracted)
                {
                    Assert.IsTrue(a.Flags.Contains("CD_09"));
                    picked++;
                }
                else
                {
                    Assert.IsFalse(a.Flags.Contains("CD_09"), "쓰러지면 못 챙긴다");
                }

                Assert.IsFalse(Run(always, seed, owned: true).Flags.Contains("ENC_CD_09_TAKE"), "가진 CD는 다시 안 나온다");
            }

            Assert.Greater(picked, 0);
        }

        [Test]
        public void MusicRoom_PersistsHeardTracks()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlMusicRoom_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var room = new MusicRoom(directory);
                Assert.IsTrue(room.Record("CD_01"));
                Assert.IsFalse(room.Record("CD_01"));
                var reloaded = new MusicRoom(directory);
                Assert.IsTrue(reloaded.IsHeard("CD_01"));
                Assert.AreEqual(1, reloaded.Count);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Content_EightCds_ShelfEventNeedsAll()
        {
            var root = Path.GetDirectoryName(Application.dataPath) ?? "";
            var content = ContentJson.LoadFolder(Path.Combine(root, ContentJson.ContentFolder));
            var full = content.Events.Single(e => e.Id == "EVT_CD_SHELF_FULL");
            CollectionAssert.AreEquivalent(Enumerable.Range(1, 8).Select(i => "CD_0" + i), full.Condition.FlagsAll);
        }
    }
}
