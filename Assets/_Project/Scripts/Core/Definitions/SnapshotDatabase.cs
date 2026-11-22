using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    /// <summary>
    /// 밸런스 표 봇 실행용 (Docs/BALANCE_LAB_PROPOSAL.md 6.1): 고칠 수 있는 정의(밸런스·탐험 모델)는 넘겨받은 사본을,
    /// 나머지는 원래 DB를 그대로 읽는다. 사본은 부르는 쪽이 만든다 (Core에는 직렬화가 없다).
    /// </summary>
    public sealed class SnapshotDatabase : IDefinitionDatabase
    {
        private readonly IDefinitionDatabase _inner;
        private readonly IReadOnlyDictionary<string, ExpeditionModelDef> _models;

        public SnapshotDatabase(IDefinitionDatabase inner, BalanceConfig balance, IReadOnlyDictionary<string, ExpeditionModelDef> models)
        {
            _inner = inner;
            Balance = balance;
            _models = models ?? new Dictionary<string, ExpeditionModelDef>();
        }

        public BalanceConfig Balance { get; }

        public IReadOnlyList<ActionDef> Actions => _inner.Actions;

        public bool TryGetAction(string id, out ActionDef action) => _inner.TryGetAction(id, out action);

        public bool TryGetExpeditionModel(string id, out ExpeditionModelDef model)
        {
            return id != null && _models.TryGetValue(id, out model) || _inner.TryGetExpeditionModel(id, out model);
        }

        public bool TryGetItem(string id, out ItemDef item) => _inner.TryGetItem(id, out item);

        public IEnumerable<ItemDef> Items => _inner.Items;

        public bool TryGetLootTable(string id, out LootTableDef table) => _inner.TryGetLootTable(id, out table);

        public bool TryGetLocation(string id, out LocationDef location) => _inner.TryGetLocation(id, out location);

        public bool TryGetEnemy(string id, out EnemyDef enemy) => _inner.TryGetEnemy(id, out enemy);

        public IReadOnlyList<EventDef> Events => _inner.Events;

        public bool TryGetEvent(string id, out EventDef ev) => _inner.TryGetEvent(id, out ev);

        public IReadOnlyList<NpcDef> Npcs => _inner.Npcs;

        public bool TryGetNpc(string id, out NpcDef npc) => _inner.TryGetNpc(id, out npc);

        public DefinitionExtras Extras => _inner.Extras;
    }
}
