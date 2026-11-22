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
        [SerializeField] private List<EventDefinitionAsset> _events = new List<EventDefinitionAsset>();
        [SerializeField] private List<NpcDefinitionAsset> _npcs = new List<NpcDefinitionAsset>();
        [SerializeField] private StoryExtrasAsset _extras;

        /// <summary>빈 참조는 건너뛰고, 빈 ID·중복 ID는 예외로 알린다.</summary>
        public IDefinitionDatabase Build()
        {
            // 밸런스 표(Docs/BALANCE_LAB_PROPOSAL.md)가 실행 중에 값을 고치므로, 고칠 수 있는 정의는 사본으로 넘긴다.
            // 원본(에셋)을 그대로 넘기면 에디터에서 플레이를 멈춘 뒤에도 고친 값이 에셋에 남아 봇 메뉴 등에 섞인다.
            var balance = Copy(_balance != null ? _balance.Config : new BalanceConfig());
            return new InMemoryDefinitionDatabase(
                balance,
                _actions.Where(a => a != null).Select(a => a.ToDefinition()),
                _expeditionModels.Where(m => m != null).Select(m => Copy(m.ToDefinition())),
                _items.Where(i => i != null).Select(i => i.ToDefinition()),
                _lootTables.Where(t => t != null).Select(t => Copy(t.ToDefinition())),
                _locations.Where(l => l != null).Select(l => l.ToDefinition()),
                _enemies.Where(e => e != null).Select(e => e.ToDefinition()),
                _events.Where(e => e != null).Select(e => e.ToDefinition()),
                _npcs.Where(n => n != null).Select(n => n.ToDefinition()),
                _extras != null ? _extras.ToDefinition() : null);
        }

        /// <summary>유니티 직렬화와 같은 규칙으로 깊은 사본을 만든다.</summary>
        private static T Copy<T>(T source) where T : new()
        {
            var copy = new T();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), copy);
            return copy;
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

        /// <summary>에디터 도구용: 이벤트·NPC 지정 (콘텐츠 가져오기).</summary>
        public void EditorSetStoryContents(List<EventDefinitionAsset> events, List<NpcDefinitionAsset> npcs)
        {
            _events = events;
            _npcs = npcs;
        }

        /// <summary>에디터 도구용: M4 콘텐츠 (행동·아이템·전리품·탐험 모델·적·묶음).</summary>
        public void EditorSetContent(
            List<ActionDefinitionAsset> actions, List<ItemDefinitionAsset> items, List<LootTableAsset> lootTables,
            List<ExpeditionModelAsset> models, List<EnemyAsset> enemies, StoryExtrasAsset extras)
        {
            _actions = actions;
            _items = items;
            _lootTables = lootTables;
            _expeditionModels = models;
            _enemies = enemies;
            _extras = extras;
        }

        /// <summary>에디터 도구용: 탐험 지역 목록.</summary>
        public void EditorSetLocations(List<LocationAsset> locations)
        {
            _locations = locations;
        }

        public IReadOnlyList<LocationAsset> EditorLocations => _locations;
#endif
    }
}
