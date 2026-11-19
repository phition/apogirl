using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;

namespace ApoGirl.Core.Expedition
{
    /// <summary>
    /// 탐험 중 조우 (문서 6.3.5-(5)) 공통 규칙. 실시간 탐험(ExpeditionDirector)과 헤드리스 모델이 같이 쓴다.
    /// </summary>
    public static class EncounterRules
    {
        /// <summary>이 시간대 변형에서 출정 플래그 조건이 맞는 조우.</summary>
        public static IEnumerable<EncounterDef> Active(LocationDef location, ExpeditionTime time, ICollection<string> flags)
        {
            if (location == null)
            {
                yield break;
            }

            var variant = time == ExpeditionTime.Day ? location.DayVariant : location.NightVariant;
            if (variant == null)
            {
                yield break;
            }

            foreach (var encounter in variant.Encounters)
            {
                if (IsActive(encounter, flags))
                {
                    yield return encounter;
                }
            }
        }

        public static bool IsActive(EncounterDef encounter, ICollection<string> flags)
        {
            flags = flags ?? new HashSet<string>();
            foreach (var flag in encounter.FlagsAll)
            {
                if (!flags.Contains(flag)) return false;
            }

            foreach (var flag in encounter.FlagsNone)
            {
                if (flags.Contains(flag)) return false;
            }

            return true;
        }

        /// <summary>선택 기록 플래그 "ENC_{조우}_{선택지}".</summary>
        public static string ChoiceFlag(EncounterDef encounter, EncounterChoiceDef choice) => "ENC_" + encounter.Id + "_" + choice.Id;

        /// <summary>조우 선택 결과를 탐험 결과에 더한다 (플래그·단서·스트레스·선택 기록).</summary>
        public static void Apply(ExpeditionResult result, EncounterDef encounter, EncounterChoiceDef choice)
        {
            result.Flags.AddRange(choice.SetFlags);
            result.Clues.AddRange(choice.AddClues);
            result.StressDelta += choice.StressDelta;
            result.Flags.Add(ChoiceFlag(encounter, choice));
        }
    }

    /// <summary>헤드리스 탐험에서 조우 선택지를 고르는 방법 (M7 봇은 전략별로 바꿔 끼운다).</summary>
    public interface IEncounterChooser
    {
        /// <summary>고른 선택지의 인덱스.</summary>
        int Choose(EncounterDef encounter, ExpeditionRequest request, IRandomService random);
    }

    /// <summary>기본값: 선택지를 고르게 무작위로.</summary>
    public sealed class RandomEncounterChooser : IEncounterChooser
    {
        public int Choose(EncounterDef encounter, ExpeditionRequest request, IRandomService random)
        {
            return random.Range(0, encounter.Choices.Count - 1);
        }
    }
}
