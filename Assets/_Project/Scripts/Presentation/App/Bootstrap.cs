using ApoGirl.Data;
using UnityEngine;

namespace ApoGirl.Presentation.App
{
    /// <summary>Boot 씬: 정의 DB로 세션을 만들고 타이틀로 넘어간다.</summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [Tooltip("시뮬레이션 정의 DB (Assets/_Project/Data/DefinitionDatabase.asset)")]
        [SerializeField] private DefinitionDatabaseAsset _database;

        private void Start()
        {
            GameApp.Initialize(_database);
            GameApp.GoTo(GameApp.TitleScene);
        }
    }
}
