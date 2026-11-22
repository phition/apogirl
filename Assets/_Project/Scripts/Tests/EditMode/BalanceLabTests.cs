using System.IO;
using System.Linq;
using ApoGirl.Core.Balance;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
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
    }
}
