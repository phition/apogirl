using System.IO;
using System.Linq;
using ApoGirl.Core.Balance;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using ApoGirl.Core.Definitions;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>밸런스 표 (Docs/BALANCE_LAB_PROPOSAL.md) 칸 모델: 고치기·범위·되돌리기·바뀐 값 목록, 절 이름 뽑기.</summary>
    public sealed class BalanceLabTests
    {
        [Test]
        public void FieldCell_EditsLiveBalance_AndReverts()
        {
            var db = M6Act4Tests.ShippedDb();
            var model = BalanceLabModel.For(db);
            var cell = model.Field("DailyFood");
            var original = db.Balance.DailyFood;

            Assert.That(cell.TrySet("5", out _), Is.True);
            Assert.That(db.Balance.DailyFood, Is.EqualTo(5));
            Assert.That(model.Changed.Select(c => c.Key), Is.EquivalentTo(new[] { "DailyFood" }));

            Assert.That(cell.TrySet("abc", out var error), Is.False);
            Assert.That(error, Is.Not.Null);
            Assert.That(db.Balance.DailyFood, Is.EqualTo(5));

            model.RevertAll();
            Assert.That(db.Balance.DailyFood, Is.EqualTo(original));
            Assert.That(model.Changed, Is.Empty);
        }

        [Test]
        public void Cell_ClampsToRange_AndScalesIntegersAtLeastOneStep()
        {
            var db = M6Act4Tests.ShippedDb();
            var model = BalanceLabModel.For(db);
            var chance = model.Field("RaidBaseChance");
            chance.Min = 0;
            chance.Max = 1;
            chance.TrySet("3", out _);
            Assert.That(db.Balance.RaidBaseChance, Is.EqualTo(1f));

            var food = model.Field("DailyFood");
            food.TrySet("2", out _);
            food.Scale(1.1);
            Assert.That(db.Balance.DailyFood, Is.EqualTo(3), "정수는 배율이 작아도 한 칸은 움직인다");
            model.RevertAll();
        }

        [Test]
        public void ModelCell_EditsExpeditionYield_AndAddsMissingResource()
        {
            var db = M6Act4Tests.ShippedDb();
            var model = BalanceLabModel.For(db);
            var id = model.ModelIds.First();
            db.TryGetExpeditionModel(id, out var expedition);
            var hadFuel = expedition.Night.Resources.Any(r => r.Resource == ResourceType.Fuel);

            var max = model.ModelCell(id, true, "Fuel.Max");
            max.TrySet("4", out _);
            Assert.That(expedition.Night.Resources.First(r => r.Resource == ResourceType.Fuel).Max, Is.EqualTo(4));

            var down = model.ModelCell(id, false, "BaseDownChance");
            down.TrySet("0.5", out _);
            Assert.That(expedition.Day.BaseDownChance, Is.EqualTo(0.5f));

            model.RevertAll();
            Assert.That(expedition.Night.Resources.Any(r => r.Resource == ResourceType.Fuel), Is.EqualTo(hadFuel), "없던 자원은 되돌리면 범위가 빠진다");
            Assert.That(expedition.Day.BaseDownChance, Is.EqualTo((float)down.Baseline));
        }

        [Test]
        public void Sections_CoverEveryBalanceField()
        {
            var sections = BalanceLabJson.ExtractSections(File.ReadAllText(BalanceLabJson.BalanceSourcePath));
            var listed = sections.Sections.SelectMany(s => s.Fields).ToList();
            var all = BalanceFields.All.Select(f => f.Name).ToList();
            Assert.That(listed, Is.EquivalentTo(all));
        }

        [Test]
        public void Layout_RowsNameRealFields()
        {
            var layout = BalanceLabJson.ParseLayout(File.ReadAllText("Assets/_Project/Resources/Balance/balance_lab_layout.json"));
            var missing = layout.Tabs.SelectMany(t => t.Rows).Where(r => BalanceFields.Find(r.Field) == null).Select(r => r.Field).ToList();
            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void Overrides_CollectOnlyDifferences_AndApplyRoundTrips()
        {
            var live = new BalanceConfig { DailyFood = 7, RaidBaseChance = 0.05f };
            live.Difficulties.First(d => d.Id == "STANDARD").FoodYield = 1.5f;
            var overrides = BalanceOverridesJson.Collect(live);
            Assert.That(overrides.Fields.Keys, Is.EquivalentTo(new[] { "DailyFood", "RaidBaseChance" }));
            Assert.That(overrides.Difficulties["STANDARD"].Keys, Is.EquivalentTo(new[] { "FoodYield" }));

            var text = BalanceOverridesJson.Write(overrides);
            var fresh = new BalanceConfig();
            var warnings = BalanceOverridesJson.Apply(fresh, BalanceOverridesJson.Parse(text));
            Assert.That(warnings, Is.Empty);
            Assert.That(fresh.DailyFood, Is.EqualTo(7));
            Assert.That(fresh.RaidBaseChance, Is.EqualTo(0.05f));
            Assert.That(fresh.Difficulties.First(d => d.Id == "STANDARD").FoodYield, Is.EqualTo(1.5f));

            var stale = BalanceOverridesJson.Parse("{ \"Fields\": { \"NoSuchField\": 3 } }");
            Assert.That(BalanceOverridesJson.Apply(new BalanceConfig(), stale), Has.Count.EqualTo(1));

            BalanceOverridesJson.ResetEditable(live);
            Assert.That(BalanceOverridesJson.Collect(live).Count, Is.Zero);
        }

        /// <summary>L4: 사본 DB는 뒤에 고친 값을 안 보고, 묶음 실행(병렬)은 메뉴처럼 한 판씩 돌린 것과 같다.</summary>
        [Test]
        public void BotBatch_MatchesSequentialRuns_AndSnapshotIgnoresLaterEdits()
        {
            var live = M6Act4Tests.ShippedDb();
            var config = BotConfigJson.Load(BotConfigJson.DefaultPath);
            var strategies = config.Strategies.Take(2).ToList();
            var modelId = BalanceLabModel.For(live).ModelIds.First();
            live.TryGetExpeditionModel(modelId, out var model);
            var snapshot = new SnapshotDatabase(live, Copy(live.Balance), new System.Collections.Generic.Dictionary<string, ExpeditionModelDef> { [modelId] = Copy(model) });

            // 사본을 만든 뒤 표를 고친다 → 사본은 그대로
            var food = live.Balance.DailyFood;
            var down = model.Day.BaseDownChance;
            live.Balance.DailyFood = food + 5;
            model.Day.BaseDownChance = 0.99f;
            Assert.That(snapshot.Balance.DailyFood, Is.EqualTo(food));
            snapshot.TryGetExpeditionModel(modelId, out var copied);
            Assert.That(copied.Day.BaseDownChance, Is.EqualTo(down));
            live.Balance.DailyFood = food;
            model.Day.BaseDownChance = down;

            var progress = new ApoGirl.Core.Simulation.Bots.BotBatchProgress();
            var batch = ApoGirl.Core.Simulation.Bots.BotBatch.Run(snapshot, config, strategies, 2, progress, parallelism: 4);
            Assert.That(progress.Done, Is.EqualTo(4));
            string Key(ApoGirl.Core.Simulation.Bots.BotRunResult r) => r.StrategyId + r.Seed + r.EndingId + r.EndWeek;
            System.Collections.Generic.List<ApoGirl.Core.Simulation.Bots.BotRunResult> OneByOne(IDefinitionDatabase db) =>
                strategies.SelectMany(s => Enumerable.Range(0, 2).Select(i => ApoGirl.Core.Simulation.Bots.BotSimulator.Run(db, config, s, config.BaseSeed + i))).ToList();
            Assert.That(batch.Select(Key), Is.EqualTo(OneByOne(snapshot).Select(Key)), "병렬로 돌려도 한 판씩 돌린 것과 같다");
            Assert.That(batch.Select(Key), Is.EqualTo(OneByOne(live).Select(Key)), "사본은 원래 DB와 같은 결과를 낸다");

            var table = ApoGirl.Core.Simulation.Bots.BotBatch.EndingTable(batch);
            Assert.That(table.Count, Is.EqualTo(ApoGirl.Core.Simulation.Bots.BotReport.EndingTargets.Length));
            Assert.That(table.Sum(r => r.Total), Is.LessThanOrEqualTo(1.001f), "엔딩 묶음 비율의 합은 100%를 넘지 않는다");

            var cancelled = new ApoGirl.Core.Simulation.Bots.BotBatchProgress();
            cancelled.Cancel();
            Assert.That(ApoGirl.Core.Simulation.Bots.BotBatch.Run(snapshot, config, strategies, 2, cancelled), Is.Null);
        }

        private static T Copy<T>(T source) where T : new()
        {
            var copy = new T();
            UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(source), copy);
            return copy;
        }

        [Test]
        public void Patcher_ChangesOnlyThatModelsNumbers_KeepingLineShape()
        {
            var text = File.ReadAllText(ContentJson.ContentFolder + "/Base/items_loot.json");
            var content = ContentJson.Parse(text);
            var store = content.ExpeditionModels.First(m => m.Id == "LOC_STORE");
            store.Day.Resources.First(r => r.Resource == ResourceType.Food).Max = 99;
            store.Day.Resources.Add(new ResourceRange(ResourceType.Battery, 1, 2));
            store.Night.BaseDownChance = 0.2f;
            store.Day.EncounterChance = 0.75f; // 원본에 없는 칸 → 덧붙인다

            var patched = ExpeditionModelPatcher.Patch(text, store, out var found);
            Assert.That(found, Is.True);

            var again = ContentJson.Parse(patched).ExpeditionModels.First(m => m.Id == "LOC_STORE");
            Assert.That(again.Day.Resources.First(r => r.Resource == ResourceType.Food).Max, Is.EqualTo(99));
            Assert.That(again.Day.Resources.Any(r => r.Resource == ResourceType.Battery), Is.True);
            Assert.That(again.Night.BaseDownChance, Is.EqualTo(0.2f));
            Assert.That(again.Day.EncounterChance, Is.EqualTo(0.75f));

            // 줄 수가 같고, LOC_STORE의 낮·밤 두 줄 말고는 글자가 같다
            var before = text.Split((char)10);
            var after = patched.Split((char)10);
            Assert.That(after.Length, Is.EqualTo(before.Length));
            Assert.That(Enumerable.Range(0, before.Length).Count(i => before[i] != after[i]), Is.EqualTo(2));

            var unchanged = ExpeditionModelPatcher.Patch(text, content.ExpeditionModels.First(m => m.Id == "LOC_PHARMACY"), out _);
            Assert.That(unchanged, Is.EqualTo(text), "값이 같으면 한 글자도 바꾸지 않는다");
        }
    }
}
