using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>헤드리스 탐험 확률 모델 에셋 (지역 1곳).</summary>
    [CreateAssetMenu(menuName = "ApoGirl/Expedition Model", fileName = "LOC_New")]
    public sealed class ExpeditionModelAsset : DefinitionAsset
    {
        [SerializeField] private ExpeditionModelDef _definition = new ExpeditionModelDef();

        public ExpeditionModelDef ToDefinition()
        {
            _definition.Id = Id;
            return _definition;
        }

#if UNITY_EDITOR
        /// <summary>에디터 도구용: 정의 내용 지정.</summary>
        public void EditorSetDefinition(ExpeditionModelDef definition) => _definition = definition;
#endif
    }
}
