using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>아이템 정의 에셋 (크기·무게·소음·가치·자원 환산).</summary>
    [CreateAssetMenu(menuName = "ApoGirl/Item Definition", fileName = "ITEM_New")]
    public sealed class ItemDefinitionAsset : DefinitionAsset
    {
        [SerializeField] private ItemDef _definition = new ItemDef();

        public ItemDef ToDefinition()
        {
            _definition.Id = Id;
            return _definition;
        }

#if UNITY_EDITOR
        /// <summary>에디터 도구용: 정의 내용 지정.</summary>
        public void EditorSetDefinition(ItemDef definition) => _definition = definition;
#endif
    }
}
