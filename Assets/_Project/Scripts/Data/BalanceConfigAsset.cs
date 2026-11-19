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

#if UNITY_EDITOR
        /// <summary>에디터 도구용: 밸런스 값 지정.</summary>
        public void EditorSetConfig(BalanceConfig config) => _config = config;
#endif
    }
}
