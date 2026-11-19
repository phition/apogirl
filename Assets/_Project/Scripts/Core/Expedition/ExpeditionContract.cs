using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Expedition
{
    // 허브(시뮬레이션 코어) ↔ 탐험 모드 데이터 계약 (문서 11.2.1). 순수 데이터로만 통신한다.

    public enum ExpeditionTime
    {
        Day,
        Night,
    }

    public enum ExtractStatus
    {
        Extracted,
        Stranded,
        Down,
    }

    [Flags]
    public enum InjuryFlags
    {
        None = 0,
        Bleeding = 1,
        Sprain = 2,
        Fracture = 4,
        Scratch = 8,
        Bite = 16,
    }

    public sealed class ExpeditionRequest
    {
        public string LocationId = "";
        public int Seed;
        public LoadoutState Loadout = new LoadoutState();
        public Dictionary<StatType, int> Stats = new Dictionary<StatType, int>();
        public Dictionary<ProficiencyType, int> ProficiencyLevels = new Dictionary<ProficiencyType, int>();
        public ExpeditionTime TimeOfDay;
        public int Week;
        public int DayOfWeek;

        /// <summary>런 시계 길이(초). 0이면 지역 기본값.</summary>
        public float RunLength;

        public int SleepDebt;
        public int Hp;
        public int Fatigue;
        public int Stress;

        /// <summary>선생님 무전 지원 (무전기 수리 후).</summary>
        public bool RadioSupport;

        /// <summary>탐험 직후 연속 출정 (판정 페널티·쓰러짐 확률 상승).</summary>
        public bool ConsecutiveRun;

        public string WeatherId = "";
    }

    public sealed class ExpeditionResult
    {
        public List<ItemStack> Loot = new List<ItemStack>();

        /// <summary>거점 자원으로 바로 들어가는 획득량 (헤드리스 모델용, 실시간 모드에서는 Loot 정산 결과).</summary>
        public List<ResourceAmount> ResourceLoot = new List<ResourceAmount>();

        public InjuryFlags Injuries;
        public int HpDelta;
        public float StressDelta;
        public float FatigueDelta;

        /// <summary>새벽 귀환 등 런 안에서 생긴 추가 수면 부채 (밤 탐험 기본 +1은 허브가 처리).</summary>
        public int SleepDebtDelta;

        public int ConscienceDelta;
        public List<string> Flags = new List<string>();
        public List<string> Clues = new List<string>();
        public float TimeSpent;
        public ExtractStatus Status;
    }

    /// <summary>탐험 실행기. M1은 헤드리스 확률 모델, M2b부터 실시간 탐험 씬이 같은 계약을 구현한다.</summary>
    public interface IExpeditionRunner
    {
        ExpeditionResult Run(ExpeditionRequest request);
    }

    public static class ExpeditionRequestFactory
    {
        public static ExpeditionRequest Create(GameState state, ActionDef action, int seed, bool consecutiveRun)
        {
            var player = state.Player;
            var request = new ExpeditionRequest
            {
                LocationId = action.LocationId,
                Seed = seed,
                Loadout = state.Loadout,
                Stats = new Dictionary<StatType, int>(player.Stats),
                TimeOfDay = action.ExpeditionTime,
                Week = state.Calendar.Week,
                DayOfWeek = state.Calendar.Day,
                SleepDebt = player.SleepDebt,
                Hp = player.Vitals.Hp,
                Fatigue = player.Vitals.Fatigue,
                Stress = player.Vitals.Stress,
                ConsecutiveRun = consecutiveRun,
            };

            foreach (var pair in player.Proficiency)
            {
                request.ProficiencyLevels[pair.Key] = pair.Value.Level;
            }

            return request;
        }
    }
}
