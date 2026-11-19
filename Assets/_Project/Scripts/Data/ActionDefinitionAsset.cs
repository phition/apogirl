using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>행동 정의 에셋. Core의 <see cref="ActionDef"/>를 그대로 직렬화한다.</summary>
    [CreateAssetMenu(menuName = "ApoGirl/Action Definition", fileName = "ACT_New")]
    public sealed class ActionDefinitionAsset : DefinitionAsset
    {
        [SerializeField] private ActionDef _definition = new ActionDef();

        /// <summary>에셋 ID를 반영한 정의.</summary>
        public ActionDef ToDefinition()
        {
            _definition.Id = Id;
            return _definition;
        }
    }
}
