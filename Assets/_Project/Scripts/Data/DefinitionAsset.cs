using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>
    /// 모든 데이터 정의 ScriptableObject의 기반. Core는 SO를 모르고 <see cref="Id"/> 문자열로만 참조한다.
    /// </summary>
    public abstract class DefinitionAsset : ScriptableObject
    {
        [Tooltip("고유 ID. 접두사 규칙: ACT_, ITEM_, EVT_, NPC_, END_, LOC_, FAC_ 등. 세이브에 저장되므로 바꾸지 말 것.")]
        [SerializeField] private string _id = "";

        /// <summary>세이브·참조에 쓰이는 고유 문자열 ID.</summary>
        public string Id => _id;

#if UNITY_EDITOR
        /// <summary>에디터 도구용: 에셋 생성 시 ID 지정.</summary>
        public void EditorSetId(string id) => _id = id;
#endif
    }
}
