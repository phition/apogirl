using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Data
{
    /// <summary>밸런스 수치 에셋 (Assets/_Project/Data/Balance).</summary>
    [CreateAssetMenu(menuName = "ApoGirl/Balance Config", fileName = "BalanceConfig")]
    public sealed class BalanceConfigAsset : ScriptableObject
    {
        [SerializeField] private BalanceConfig _config = new BalanceConfig();

        public BalanceConfig Config => _config;
    }
}
