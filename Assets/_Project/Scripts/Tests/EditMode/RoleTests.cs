using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>동료 역할: 정수 보정의 소수점 누적(요리·간호), 습격 방어 반올림, 새 역할(분위기 메이커·정찰·정비).</summary>
    public sealed class RoleTests
    {
        private static InMemoryDefinitionDatabase Db()
        {
            var extras = new DefinitionExtras
            {
                Roles =
                {
                    new RoleDef { Id = "COOK", Modifiers = { new TraitModifier { Kind = TraitModifierKind.FoodSaving, Value = 1 } } },
                    new RoleDef { Id = "MEDIC", Modifiers = { new TraitModifier { Kind = TraitModifierKind.InfectionResist, Value = 1 } } },
                    new RoleDef { Id = "MOOD", Modifiers = { new TraitModifier { Kind = TraitModifierKind.RestRecovery, Value = 0.1f } } },
                    new RoleDef
                    {
                        Id = "SCOUT",
                        Modifiers =
                        {
                            new TraitModifier { Kind = TraitModifierKind.ExpeditionStress, Value = -0.1f },
                            new TraitModifier { Kind = TraitModifierKind.SearchSpeed, Value = 0.1f },
                        },
                    },
                    new RoleDef { Id = "MECHANIC", Modifiers = { new TraitModifier { Kind = TraitModifierKind.CheckBonus, Key = "Engineering", Value = 0.08f } } },
                },
            };
            return new InMemoryDefinitionDatabase(new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, extras: extras);
        }

        private static GameState WithCompanion(InMemoryDefinitionDatabase db, string role, int affinity)
        {
            var state = TestContent.NewGame(db);
            state.Difficulty = ""; // 난이도 배율 없이 (감염 증가량을 그대로 비교)
            state.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined, Role = role, Affinity = affinity };
            return state;
        }

        [Test]
        public void Spread_KeepsFractionsOverTime()
        {
            int Total(float value, int steps)
            {
                var sum = 0;
                for (var i = 1; i <= steps; i++)
                {
                    sum += TraitRules.Spread(value, i);
                }

                return sum;
            }

            Assert.AreEqual(2, Total(0.5f, 4), "×0.5: 하루건너 1");
            Assert.AreEqual(5, Total(1.25f, 4));
            Assert.AreEqual(6, Total(1.5f, 4));
            Assert.AreEqual(4, Total(1f, 4));
            Assert.AreEqual(0, Total(0f, 4));
            Assert.AreEqual(0, Total(-1f, 4));
        }

        [Test]
        public void RoundInt_RoundsHalfUp()
        {
            Assert.AreEqual(8, TraitRules.RoundInt(7.5f));
            Assert.AreEqual(7, TraitRules.RoundInt(6.5f), "은행가 반올림이면 6");
            Assert.AreEqual(3, TraitRules.RoundInt(3f));
        }

        [Test]
        public void Cook_WithNegativeBond_StillSavesHalf()
        {
            var db = Db();
            var state = WithCompanion(db, "COOK", -5);
            var saving = TraitRules.Sum(state, db, TraitModifierKind.FoodSaving);
            Assert.AreEqual(0.5f, saving, 1e-5, "호감 음수 ×0.5");
            var total = 0;
            for (var step = 1; step <= 6; step++)
            {
                total += TraitRules.Spread(saving, step);
            }

            Assert.AreEqual(3, total, "예전에는 (int)0.5 = 0이라 하나도 아끼지 못했다");
        }

        [Test]
        public void Medic_HighBond_AveragesOneAndHalfPerWeek()
        {
            var db = Db();
            int Growth(string role, int week)
            {
                var state = WithCompanion(db, role, 90);
                state.Calendar.Week = week;
                state.Player.Vitals.Infection = 10;
                InfectionRules.WeeklyPlayer(state, db, new StateDelta());
                return state.Player.Vitals.Infection;
            }

            var reduced = 0;
            for (var week = 1; week <= 4; week++)
            {
                reduced += Growth("", week) - Growth("MEDIC", week);
            }

            Assert.AreEqual(6, reduced, "×1.5 → 4주에 6 (1,2,1,2)");
        }

        [Test]
        public void NewRoles_ApplyTheirModifiers()
        {
            var db = Db();
            Assert.AreEqual(0.1f, TraitRules.Sum(WithCompanion(db, "MOOD", 10), db, TraitModifierKind.RestRecovery), 1e-5);
            Assert.AreEqual(0.15f, TraitRules.Sum(WithCompanion(db, "MOOD", 90), db, TraitModifierKind.RestRecovery), 1e-5, "호감 80↑ ×1.5");

            var scout = WithCompanion(db, "SCOUT", 10);
            Assert.AreEqual(-0.1f, TraitRules.Sum(scout, db, TraitModifierKind.ExpeditionStress), 1e-5);
            Assert.AreEqual(0.1f, TraitRules.ExplorationModifiers(scout, db)["SearchSpeed"], 1e-5);

            var mechanic = WithCompanion(db, "MECHANIC", 10);
            Assert.AreEqual(0.08f, TraitRules.Sum(mechanic, db, TraitModifierKind.CheckBonus, "Engineering"), 1e-5);
            Assert.AreEqual(0f, TraitRules.Sum(mechanic, db, TraitModifierKind.CheckBonus, "Medicine"), 1e-5, "공학만");

            mechanic.Npcs["NPC_DOHYEON"].InjuredWeeks = 1;
            Assert.AreEqual(0f, TraitRules.Sum(mechanic, db, TraitModifierKind.CheckBonus, "Engineering"), 1e-5, "다치면 역할 효과 없음");
        }
    }
}
