using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>미리 정한 값을 순서대로 돌려주는 난수 (큐가 비면 기본값).</summary>
    internal sealed class FixedRandom : IRandomService
    {
        private readonly Queue<double> _values;

        public FixedRandom(params double[] values)
        {
            _values = new Queue<double>(values);
        }

        public double Default { get; set; } = 0.5;

        public double NextDouble() => _values.Count > 0 ? _values.Dequeue() : Default;

        public int Range(int minInclusive, int maxInclusive) => minInclusive;

        public int NextSeed() => 42;
    }

    /// <summary>항상 정해진 결과를 돌려주는 탐험 실행기.</summary>
    internal sealed class StubExpeditionRunner : IExpeditionRunner
    {
        public Func<ExpeditionRequest, ExpeditionResult> Handler = _ => new ExpeditionResult { Status = ExtractStatus.Extracted };
        public readonly List<ExpeditionRequest> Requests = new List<ExpeditionRequest>();

        public ExpeditionResult Run(ExpeditionRequest request)
        {
            Requests.Add(request);
            return Handler(request);
        }
    }

    /// <summary>코드로 만든 테스트용 정의 (SO 없이 테스트).</summary>
    internal static class TestContent
    {
        public const string RestDay = "ACT_REST_DAY";
        public const string RestNight = "ACT_REST_NIGHT";
        public const string Humanities = "ACT_EDU_HUMANITIES";
        public const string MedicineStudy = "ACT_EDU_MEDICINE";
        public const string PurifyWater = "ACT_SURV_PURIFY_WATER";
        public const string Craft = "ACT_SURV_CRAFT";
        public const string DayExpedition = "ACT_EXP_DAY";
        public const string NightExpedition = "ACT_EXP_NIGHT";
        public const string Location = "LOC_TEST";

        public static InMemoryDefinitionDatabase CreateDatabase(Action<BalanceConfig> tweak = null)
        {
            var balance = new BalanceConfig();
            tweak?.Invoke(balance);
            return new InMemoryDefinitionDatabase(
                balance, CreateActions(), new[] { CreateModel() }, CreateItems(), new[] { CreateLootTable() }, new[] { CreateLocation() });
        }

        public static IEnumerable<ActionDef> CreateActions()
        {
            yield return Rest(RestDay, SlotTag.DayOnly, -10, -5);
            yield return Rest(RestNight, SlotTag.NightOnly, -15, -10);

            var humanities = new ActionDef
            {
                Id = Humanities,
                Category = ActionCategory.Education,
                FatigueCost = 5,
                StressCost = 2,
                HasCheck = true,
                Check = new CheckDef { BaseRate = 0.6f, StatWeights = { new StatWeight(StatType.Intellect, 0.004f) } },
                StatGains = { new StatAmount(StatType.Intellect, 3), new StatAmount(StatType.Empathy, 1) },
                HasProficiency = true,
                Proficiency = ProficiencyType.Humanities,
                ProficiencyXp = 1,
                Teaching = TeachingMode.TeacherDirect,
            };
            yield return humanities;

            yield return new ActionDef
            {
                Id = MedicineStudy,
                Category = ActionCategory.Education,
                FatigueCost = 5,
                StressCost = 3,
                HasCheck = true,
                Check = new CheckDef { BaseRate = 0.5f, StatWeights = { new StatWeight(StatType.Intellect, 0.004f) } },
                StatGains = { new StatAmount(StatType.Intellect, 2), new StatAmount(StatType.Craft, 1) },
                HasProficiency = true,
                Proficiency = ProficiencyType.Medicine,
                ProficiencyXp = 1,
                Teaching = TeachingMode.SelfStudyGuided,
            };

            yield return new ActionDef
            {
                Id = PurifyWater,
                Category = ActionCategory.Survival,
                SlotTag = SlotTag.DayOnly,
                FatigueCost = 5,
                OnSuccess = new GameEffect { Resources = { new ResourceAmount(ResourceType.Water, 4) } },
            };

            yield return new ActionDef
            {
                Id = Craft,
                Category = ActionCategory.Survival,
                ResourceCosts = { new ResourceAmount(ResourceType.Materials, 2) },
                FatigueCost = 5,
                StatGains = { new StatAmount(StatType.Craft, 2) },
            };

            yield return new ActionDef
            {
                Id = DayExpedition,
                Category = ActionCategory.Expedition,
                SlotTag = SlotTag.DayOnly,
                ExpeditionTime = ExpeditionTime.Day,
                LocationId = Location,
            };

            yield return new ActionDef
            {
                Id = NightExpedition,
                Category = ActionCategory.Expedition,
                SlotTag = SlotTag.NightOnly,
                ExpeditionTime = ExpeditionTime.Night,
                LocationId = Location,
            };
        }

        public static ExpeditionModelDef CreateModel()
        {
            return new ExpeditionModelDef
            {
                Id = Location,
                Day = new ExpeditionYield
                {
                    Resources = { new ResourceRange(ResourceType.Food, 6, 10), new ResourceRange(ResourceType.Water, 4, 8) },
                    BaseDownChance = 0.05f,
                    Fatigue = 15,
                    StressMin = 0,
                    StressMax = 12,
                    OwnedRoomChance = 0.5f,
                },
                Night = new ExpeditionYield
                {
                    Resources = { new ResourceRange(ResourceType.Food, 8, 12), new ResourceRange(ResourceType.Medicine, 0, 2) },
                    BaseDownChance = 0.1f,
                    Fatigue = 20,
                    StressMin = 15,
                    StressMax = 15,
                    DawnReturnChance = 0.2f,
                },
            };
        }

        public const string LootTable = "LOOT_TEST";

        public static IEnumerable<ItemDef> CreateItems()
        {
            yield return new ItemDef { Id = "ITEM_CANNED_FOOD", Weight = 0.4f, ResourceYield = { new ResourceAmount(ResourceType.Food, 2) } };
            yield return new ItemDef { Id = "ITEM_WATER_BOTTLE", Height = 2, Weight = 0.6f, ResourceYield = { new ResourceAmount(ResourceType.Water, 2) } };
            yield return new ItemDef { Id = "ITEM_SCRAP", Width = 2, Height = 2, Weight = 1.5f, ResourceYield = { new ResourceAmount(ResourceType.Materials, 2) } };
            yield return new ItemDef { Id = "ITEM_TEXTBOOK", Width = 2, Weight = 0.8f };
        }

        public static LootTableDef CreateLootTable()
        {
            return new LootTableDef
            {
                Id = LootTable,
                MinRolls = 1,
                MaxRolls = 3,
                EmptyChance = 0.1f,
                Entries = { new LootEntry("ITEM_CANNED_FOOD", 5), new LootEntry("ITEM_WATER_BOTTLE", 3), new LootEntry("ITEM_TEXTBOOK", 1) },
            };
        }

        public static LocationDef CreateLocation()
        {
            var classroom = new RoomTemplateDef
            {
                Id = "ROOM_CLASSROOM",
                Width = 12,
                Containers =
                {
                    new ContainerDef { Id = "DESK", Kind = ContainerKind.Desk, X = 4 },
                    new ContainerDef { Id = "LOCKER", Kind = ContainerKind.Locker, X = 8 },
                    new ContainerDef { Id = "CABINET", Kind = ContainerKind.Cabinet, X = 10 },
                },
            };

            return new LocationDef
            {
                Id = Location,
                DefaultLootTableId = LootTable,
                Layout = new BuildingLayoutDef
                {
                    Floors = 1,
                    CorridorLength = 30,
                    RoomTemplates = { classroom },
                    Doors =
                    {
                        new DoorDef { RoomId = "R1", TemplateId = "ROOM_CLASSROOM", Floor = 0, X = 8, Number = 1 },
                        new DoorDef { RoomId = "R2", TemplateId = "ROOM_CLASSROOM", Floor = 0, X = 16, Number = 2 },
                        new DoorDef { RoomId = "R3", TemplateId = "ROOM_CLASSROOM", Floor = 0, X = 24, Number = 3, LootTableId = "LOOT_EMPTY" },
                    },
                    Containers = { new ContainerDef { Id = "C1", Floor = 0, X = 5 } },
                    Exits = { new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 1 } },
                },
            };
        }

        public static GameState NewGame(InMemoryDefinitionDatabase db, int seed = 1234)
        {
            return ApoGirl.Core.Simulation.NewGameFactory.Create(db.Balance, seed);
        }

        private static ActionDef Rest(string id, SlotTag tag, int fatigue, int stress)
        {
            return new ActionDef
            {
                Id = id,
                Category = ActionCategory.Rest,
                SlotTag = tag,
                OnSuccess = new GameEffect
                {
                    Vitals = { new VitalAmount(VitalType.Fatigue, fatigue), new VitalAmount(VitalType.Stress, stress) },
                },
            };
        }
    }
}
