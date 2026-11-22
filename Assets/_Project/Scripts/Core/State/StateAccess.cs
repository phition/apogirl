using System;

namespace ApoGirl.Core.State
{
    /// <summary>상태 읽기/쓰기 헬퍼. 범위 제한은 EffectApplier가 담당한다.</summary>
    public static class StateAccess
    {
        public static int GetStat(this PlayerState player, StatType type)
        {
            return player.Stats.TryGetValue(type, out var value) ? value : 0;
        }

        public static void SetStat(this PlayerState player, StatType type, int value)
        {
            player.Stats[type] = value;
        }

        /// <summary>숙련도 상태를 가져온다. 없으면 만들어 둔다.</summary>
        public static ProficiencyState GetProficiency(this PlayerState player, ProficiencyType type)
        {
            if (!player.Proficiency.TryGetValue(type, out var state))
            {
                state = new ProficiencyState();
                player.Proficiency[type] = state;
            }

            return state;
        }

        /// <summary>숙련도 레벨 (읽기 전용, 상태를 만들지 않음).</summary>
        public static int ProficiencyLevel(this PlayerState player, ProficiencyType type)
        {
            return player.Proficiency.TryGetValue(type, out var state) ? state.Level : 0;
        }

        public static int GetVital(this VitalState vitals, VitalType type)
        {
            switch (type)
            {
                case VitalType.Hp: return vitals.Hp;
                case VitalType.Fatigue: return vitals.Fatigue;
                case VitalType.Stress: return vitals.Stress;
                case VitalType.Infection: return vitals.Infection;
                case VitalType.Cleanliness: return vitals.Cleanliness;
                case VitalType.Satiety: return vitals.Satiety;
                case VitalType.Hydration: return vitals.Hydration;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static void SetVital(this VitalState vitals, VitalType type, int value)
        {
            switch (type)
            {
                case VitalType.Hp: vitals.Hp = value; break;
                case VitalType.Fatigue: vitals.Fatigue = value; break;
                case VitalType.Stress: vitals.Stress = value; break;
                case VitalType.Infection: vitals.Infection = value; break;
                case VitalType.Cleanliness: vitals.Cleanliness = value; break;
                case VitalType.Satiety: vitals.Satiety = value; break;
                case VitalType.Hydration: vitals.Hydration = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static int GetHidden(this HiddenState hidden, HiddenType type)
        {
            switch (type)
            {
                case HiddenType.TraumaRecovery: return hidden.TraumaRecovery;
                case HiddenType.SelfAwareness: return hidden.SelfAwareness;
                case HiddenType.Humanity: return hidden.Humanity;
                case HiddenType.Hope: return hidden.Hope;
                case HiddenType.Isolation: return hidden.Isolation;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static void SetHidden(this HiddenState hidden, HiddenType type, int value)
        {
            switch (type)
            {
                case HiddenType.TraumaRecovery: hidden.TraumaRecovery = value; break;
                case HiddenType.SelfAwareness: hidden.SelfAwareness = value; break;
                case HiddenType.Humanity: hidden.Humanity = value; break;
                case HiddenType.Hope: hidden.Hope = value; break;
                case HiddenType.Isolation: hidden.Isolation = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static int GetResource(this BaseState baseState, ResourceType type)
        {
            return baseState.Resources.TryGetValue(type, out var value) ? value : 0;
        }

        public static void SetResource(this BaseState baseState, ResourceType type, int value)
        {
            baseState.Resources[type] = value;
        }
    }
}
