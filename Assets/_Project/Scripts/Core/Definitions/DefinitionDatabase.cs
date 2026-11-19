using System;
using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    /// <summary>시뮬레이션이 정의 데이터를 조회하는 창구. ScriptableObject 구현은 ApoGirl.Data에 있다.</summary>
    public interface IDefinitionDatabase
    {
        BalanceConfig Balance { get; }

        bool TryGetAction(string id, out ActionDef action);

        bool TryGetExpeditionModel(string id, out ExpeditionModelDef model);
    }

    public static class DefinitionDatabaseExtensions
    {
        public static ActionDef GetAction(this IDefinitionDatabase db, string id)
        {
            if (!db.TryGetAction(id, out var action))
            {
                throw new KeyNotFoundException($"행동 정의를 찾을 수 없음: '{id}'");
            }

            return action;
        }

        public static ExpeditionModelDef GetExpeditionModel(this IDefinitionDatabase db, string id)
        {
            if (!db.TryGetExpeditionModel(id, out var model))
            {
                throw new KeyNotFoundException($"탐험 모델을 찾을 수 없음: '{id}'");
            }

            return model;
        }
    }

    /// <summary>메모리 기반 정의 DB. 빈 ID·중복 ID는 생성 시점에 거부한다.</summary>
    public sealed class InMemoryDefinitionDatabase : IDefinitionDatabase
    {
        private readonly Dictionary<string, ActionDef> _actions = new Dictionary<string, ActionDef>();
        private readonly Dictionary<string, ExpeditionModelDef> _expeditionModels = new Dictionary<string, ExpeditionModelDef>();

        public InMemoryDefinitionDatabase(
            BalanceConfig balance,
            IEnumerable<ActionDef> actions,
            IEnumerable<ExpeditionModelDef> expeditionModels = null)
        {
            Balance = balance ?? throw new ArgumentNullException(nameof(balance));
            foreach (var action in actions)
            {
                Register(_actions, action.Id, action);
            }

            if (expeditionModels != null)
            {
                foreach (var model in expeditionModels)
                {
                    Register(_expeditionModels, model.Id, model);
                }
            }
        }

        public BalanceConfig Balance { get; }

        public bool TryGetAction(string id, out ActionDef action)
        {
            return _actions.TryGetValue(id ?? "", out action);
        }

        public bool TryGetExpeditionModel(string id, out ExpeditionModelDef model)
        {
            return _expeditionModels.TryGetValue(id ?? "", out model);
        }

        private static void Register<T>(Dictionary<string, T> map, string id, T value)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("정의 ID가 비어 있음");
            }

            if (map.ContainsKey(id))
            {
                throw new ArgumentException($"중복된 정의 ID: '{id}'");
            }

            map.Add(id, value);
        }
    }
}
