using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.State;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace ApoGirl.Systems.Save
{
    /// <summary>세이브 버전이 맞지 않거나 마이그레이션 경로가 없을 때.</summary>
    public sealed class SaveVersionException : Exception
    {
        public SaveVersionException(string message) : base(message)
        {
        }
    }

    /// <summary>세이브 포맷 버전 n → n+1 변환.</summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }

        void Migrate(JObject root);
    }

    /// <summary>GameState ↔ JSON. enum은 문자열, 기존 컬렉션은 덮어쓰기(Replace)로 처리한다.</summary>
    public sealed class GameStateSerializer
    {
        private readonly List<ISaveMigration> _migrations;
        private readonly JsonSerializerSettings _settings;

        public GameStateSerializer(IEnumerable<ISaveMigration> migrations = null)
        {
            _migrations = migrations?.ToList() ?? new List<ISaveMigration>();
            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Converters = { new StringEnumConverter() },
            };
        }

        public string Serialize(GameState state)
        {
            return JsonConvert.SerializeObject(state, _settings);
        }

        public GameState Deserialize(string json)
        {
            var root = JObject.Parse(json);
            var version = root.Value<int?>(nameof(GameState.SaveVersion)) ?? 0;
            if (version > GameState.CurrentSaveVersion)
            {
                throw new SaveVersionException($"더 새로운 버전의 세이브: v{version} (현재 v{GameState.CurrentSaveVersion})");
            }

            while (version < GameState.CurrentSaveVersion)
            {
                var migration = _migrations.FirstOrDefault(m => m.FromVersion == version);
                if (migration == null)
                {
                    throw new SaveVersionException($"v{version} → v{version + 1} 마이그레이션이 없음");
                }

                migration.Migrate(root);
                version++;
                root[nameof(GameState.SaveVersion)] = version;
            }

            return root.ToObject<GameState>(JsonSerializer.Create(_settings));
        }
    }
}
