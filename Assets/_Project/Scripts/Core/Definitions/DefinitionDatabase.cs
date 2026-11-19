using System;
using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    /// <summary>시뮬레이션이 정의 데이터를 조회하는 창구. ScriptableObject 구현은 ApoGirl.Data에 있다.</summary>
    public interface IDefinitionDatabase
    {
        BalanceConfig Balance { get; }

        /// <summary>모든 행동 정의 (등록 순서).</summary>
        IReadOnlyList<ActionDef> Actions { get; }

        bool TryGetAction(string id, out ActionDef action);

        bool TryGetExpeditionModel(string id, out ExpeditionModelDef model);

        bool TryGetItem(string id, out ItemDef item);

        bool TryGetLootTable(string id, out LootTableDef table);

        bool TryGetLocation(string id, out LocationDef location);

        bool TryGetEnemy(string id, out EnemyDef enemy);
    }

    public static class DefinitionDatabaseExtensions
    {
        public static ActionDef GetAction(this IDefinitionDatabase db, string id)
        {
            if (!db.TryGetAction(id, out var action))
            {
                throw new KeyNotFoundException($"행동 정의를 찾을 수 없음: '{id}'");
            }

            return action;
        }

        public static ExpeditionModelDef GetExpeditionModel(this IDefinitionDatabase db, string id)
        {
            if (!db.TryGetExpeditionModel(id, out var model))
            {
                throw new KeyNotFoundException($"탐험 모델을 찾을 수 없음: '{id}'");
            }

            return model;
        }

        public static ItemDef GetItem(this IDefinitionDatabase db, string id)
        {
            if (!db.TryGetItem(id, out var item))
            {
                throw new KeyNotFoundException($"아이템 정의를 찾을 수 없음: '{id}'");
            }

            return item;
        }

        public static LocationDef GetLocation(this IDefinitionDatabase db, string id)
        {
            if (!db.TryGetLocation(id, out var location))
            {
                throw new KeyNotFoundException($"탐험 지역을 찾을 수 없음: '{id}'");
            }

            return location;
        }
    }

    /// <summary>메모리 기반 정의 DB. 빈 ID·중복 ID는 생성 시점에 거부한다.</summary>
    public sealed class InMemoryDefinitionDatabase : IDefinitionDatabase
    {
        private readonly Dictionary<string, ActionDef> _actions = new Dictionary<string, ActionDef>();
        private readonly Dictionary<string, ExpeditionModelDef> _expeditionModels = new Dictionary<string, ExpeditionModelDef>();
        private readonly List<ActionDef> _actionList = new List<ActionDef>();
        private readonly Dictionary<string, ItemDef> _items = new Dictionary<string, ItemDef>();
        private readonly Dictionary<string, LootTableDef> _lootTables = new Dictionary<string, LootTableDef>();
        private readonly Dictionary<string, LocationDef> _locations = new Dictionary<string, LocationDef>();
        private readonly Dictionary<string, EnemyDef> _enemies = new Dictionary<string, EnemyDef>();

        public InMemoryDefinitionDatabase(
            BalanceConfig balance,
            IEnumerable<ActionDef> actions,
            IEnumerable<ExpeditionModelDef> expeditionModels = null,
            IEnumerable<ItemDef> items = null,
            IEnumerable<LootTableDef> lootTables = null,
            IEnumerable<LocationDef> locations = null,
            IEnumerable<EnemyDef> enemies = null)
        {
            Balance = balance ?? throw new ArgumentNullException(nameof(balance));
            foreach (var action in actions)
            {
                Register(_actions, action.Id, action);
                _actionList.Add(action);
            }

            if (expeditionModels != null)
            {
                foreach (var model in expeditionModels)
                {
                    Register(_expeditionModels, model.Id, model);
                }
            }

            RegisterAll(_items, items, i => i.Id);
            RegisterAll(_lootTables, lootTables, t => t.Id);
            RegisterAll(_locations, locations, l => l.Id);
            RegisterAll(_enemies, enemies, e => e.Id);
        }

        public BalanceConfig Balance { get; }

        public IReadOnlyList<ActionDef> Actions => _actionList;

        public bool TryGetAction(string id, out ActionDef action)
        {
            return _actions.TryGetValue(id ?? "", out action);
        }

        public bool TryGetExpeditionModel(string id, out ExpeditionModelDef model)
        {
            return _expeditionModels.TryGetValue(id ?? "", out model);
        }

        public bool TryGetItem(string id, out ItemDef item) => _items.TryGetValue(id ?? "", out item);

        public bool TryGetLootTable(string id, out LootTableDef table) => _lootTables.TryGetValue(id ?? "", out table);

        public bool TryGetLocation(string id, out LocationDef location) => _locations.TryGetValue(id ?? "", out location);

        public bool TryGetEnemy(string id, out EnemyDef enemy) => _enemies.TryGetValue(id ?? "", out enemy);

        private static void RegisterAll<T>(Dictionary<string, T> map, IEnumerable<T> values, Func<T, string> id)
        {
            if (values == null)
            {
                return;
            }

            foreach (var value in values)
            {
                Register(map, id(value), value);
            }
        }

        private static void Register<T>(Dictionary<string, T> map, string id, T value)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("정의 ID가 비어 있음");
            }

            if (map.ContainsKey(id))
            {
                throw new ArgumentException($"중복된 정의 ID: '{id}'");
            }

            map.Add(id, value);
        }
    }
}
