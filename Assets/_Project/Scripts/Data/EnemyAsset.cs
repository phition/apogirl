using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>좀비 유형 에셋.</summary>
    [CreateAssetMenu(menuName = "ApoGirl/Enemy", fileName = "ENEMY_New")]
    public sealed class EnemyAsset : DefinitionAsset
    {
        [SerializeField] private EnemyDef _definition = new EnemyDef();

        public EnemyDef ToDefinition()
        {
            _definition.Id = Id;
            return _definition;
        }

#if UNITY_EDITOR
        /// <summary>에디터 도구용: 정의 내용 지정.</summary>
        public void EditorSetDefinition(EnemyDef definition) => _definition = definition;
#endif
    }
}
