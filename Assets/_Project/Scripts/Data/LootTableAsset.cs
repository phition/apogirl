using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>전리품 테이블 에셋.</summary>
    [CreateAssetMenu(menuName = "ApoGirl/Loot Table", fileName = "LOOT_New")]
    public sealed class LootTableAsset : DefinitionAsset
    {
        [SerializeField] private LootTableDef _definition = new LootTableDef();

        public LootTableDef ToDefinition()
        {
            _definition.Id = Id;
            return _definition;
        }

#if UNITY_EDITOR
        /// <summary>에디터 도구용: 정의 내용 지정.</summary>
        public void EditorSetDefinition(LootTableDef definition) => _definition = definition;
#endif
    }
}
