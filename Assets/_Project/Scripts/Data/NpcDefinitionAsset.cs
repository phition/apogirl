using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>NPC Definition 에셋. 원본은 Content JSON이며 가져오기 도구가 만든다 (직접 수정하면 다음 가져오기 때 덮어씀).</summary>
    [CreateAssetMenu(menuName = "ApoGirl/NPC Definition", fileName = "NPC_New")]
    public sealed class NpcDefinitionAsset : DefinitionAsset
    {
        [SerializeField] private NpcDef _definition = new NpcDef();

        public NpcDef ToDefinition()
        {
            _definition.Id = Id;
            return _definition;
        }

#if UNITY_EDITOR
        /// <summary>에디터 도구용: 정의 내용 지정.</summary>
        public void EditorSetDefinition(NpcDef definition) => _definition = definition;
#endif
    }
}
