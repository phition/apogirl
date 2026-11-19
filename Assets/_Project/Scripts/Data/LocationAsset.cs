using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>실시간 탐험 지역 에셋 (건물 레이아웃 포함).</summary>
    [CreateAssetMenu(menuName = "ApoGirl/Location", fileName = "LOC_New")]
    public sealed class LocationAsset : DefinitionAsset
    {
        [SerializeField] private LocationDef _definition = new LocationDef();

        public LocationDef ToDefinition()
        {
            _definition.Id = Id;
            return _definition;
        }

#if UNITY_EDITOR
        /// <summary>에디터 도구용: 정의 내용 지정.</summary>
        public void EditorSetDefinition(LocationDef definition) => _definition = definition;
#endif
    }
}
