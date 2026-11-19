using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>모든 정의 에셋을 묶어 시뮬레이션용 DB를 만든다.</summary>
    [CreateAssetMenu(menuName = "ApoGirl/Definition Database", fileName = "DefinitionDatabase")]
    public sealed class DefinitionDatabaseAsset : ScriptableObject
    {
        [SerializeField] private BalanceConfigAsset _balance;
        [SerializeField] private List<ActionDefinitionAsset> _actions = new List<ActionDefinitionAsset>();
        [SerializeField] private List<ExpeditionModelAsset> _expeditionModels = new List<ExpeditionModelAsset>();
        [SerializeField] private List<ItemDefinitionAsset> _items = new List<ItemDefinitionAsset>();
        [SerializeField] private List<LootTableAsset> _lootTables = new List<LootTableAsset>();
        [SerializeField] private List<LocationAsset> _locations = new List<LocationAsset>();
        [SerializeField] private List<EnemyAsset> _enemies = new List<EnemyAsset>();

        /// <summary>빈 참조는 건너뛰고, 빈 ID·중복 ID는 예외로 알린다.</summary>
        public IDefinitionDatabase Build()
        {
            var balance = _balance != null ? _balance.Config : new BalanceConfig();
            return new InMemoryDefinitionDatabase(
                balance,
                _actions.Where(a => a != null).Select(a => a.ToDefinition()),
                _expeditionModels.Where(m => m != null).Select(m => m.ToDefinition()),
                _items.Where(i => i != null).Select(i => i.ToDefinition()),
                _lootTables.Where(t => t != null).Select(t => t.ToDefinition()),
                _locations.Where(l => l != null).Select(l => l.ToDefinition()),
                _enemies.Where(e => e != null).Select(e => e.ToDefinition()));
        }

#if UNITY_EDITOR
        /// <summary>에디터 도구용: 묶을 에셋 지정.</summary>
        public void EditorSetContents(BalanceConfigAsset balance, List<ActionDefinitionAsset> actions, List<ExpeditionModelAsset> expeditionModels)
        {
            _balance = balance;
            _actions = actions;
            _expeditionModels = expeditionModels;
        }

        /// <summary>에디터 도구용: 탐험 모드 정의 지정.</summary>
        public void EditorSetExplorationContents(List<ItemDefinitionAsset> items, List<LootTableAsset> lootTables, List<LocationAsset> locations)
        {
            _items = items;
            _lootTables = lootTables;
            _locations = locations;
        }

        /// <summary>에디터 도구용: 좀비 유형 지정.</summary>
        public void EditorSetEnemies(List<EnemyAsset> enemies)
        {
            _enemies = enemies;
        }
#endif
    }
}
