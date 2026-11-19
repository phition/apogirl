using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;
using ApoGirl.Exploration.Model;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class VisibilityTests
    {
        [Test]
        public void Darkness_ShrinksSight_FlashlightExtendsIt()
        {
            var balance = new BalanceConfig();
            var sim = new ThreatSimulation(balance, new SeededRandom(SeededRandom.CreateState(1)));
            var zombie = sim.AddZombie(new EnemyDef { SightRange = 7f }, 0, 10f);
            zombie.Facing = 1;
            var mods = ActivityMods.Neutral;

            var player = new PlayerSnapshot { AreaId = 0, X = 15f };
            Assert.IsTrue(sim.CanSee(zombie, player, mods));

            player.Visibility = balance.DarknessVisibility;
            Assert.IsFalse(sim.CanSee(zombie, player, mods), "어둠: 7 × 0.6 = 4.2 < 5");

            player.X = 19f;
            player.Visibility = balance.FlashlightExposure;
            Assert.IsTrue(sim.CanSee(zombie, player, mods), "손전등: 7 × 1.5 = 10.5 ≥ 9");
        }
    }

    public sealed class PlayLogAnalyzerTests
    {
        private static PlayLogEntry Run(string status, int food, string time = "Day", bool debug = false)
        {
            var entry = new PlayLogEntry { LocationId = "LOC", TimeOfDay = time, Status = status, TimeSpent = 100, DebugUsed = debug };
            if (food > 0)
            {
                entry.Resources.Add(new ResourceAmount(ResourceType.Food, food));
            }

            return entry;
        }

        [Test]
        public void Summary_GroupsByTime_IgnoresDebugRuns()
        {
            var logs = new[]
            {
                Run("Extracted", 4), Run("Extracted", 6), Run("Extracted", 8), Run("Down", 0), Run("Extracted", 10),
                Run("Extracted", 99, debug: true),
                Run("Extracted", 3, "Night"),
            };

            var summaries = PlayLogAnalyzer.Summarize(logs);
            var day = summaries.Single(s => s.TimeOfDay == "Day");
            Assert.AreEqual(5, day.Runs, "디버그 판 제외");
            Assert.AreEqual(0.2f, day.DownRate, 1e-5f);

            var food = day.SuggestedResources.Single(r => r.Resource == ResourceType.Food);
            Assert.AreEqual(4, food.Min, "하위 25% (쓰러진 판 0 포함: 0,4,6,8,10)");
            Assert.AreEqual(8, food.Max);
            Assert.AreEqual(1, summaries.Single(s => s.TimeOfDay == "Night").Runs);
        }
    }
}
