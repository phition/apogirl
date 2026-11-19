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

        /// <summary>빈 참조는 건너뛰고, 빈 ID·중복 ID는 예외로 알린다.</summary>
        public IDefinitionDatabase Build()
        {
            var balance = _balance != null ? _balance.Config : new BalanceConfig();
            return new InMemoryDefinitionDatabase(
                balance,
                _actions.Where(a => a != null).Select(a => a.ToDefinition()),
                _expeditionModels.Where(m => m != null).Select(m => m.ToDefinition()));
        }
    }
}
