using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 양심의 무게 (사용자 결정 2026-11-27). 무엇이 무게를 올리고, 처음 올랐을 때 무엇이 일어나는가.
    /// - 탐험: 한 번에 좀비를 ConscienceFelledThreshold 이상 쓰러뜨리면 +ConscienceFelledAmount.
    ///   좀비를 치는 것은 몸을 지키는 일이지만, 거기 붙이는 정당성은 스스로 붙이는 것이라 무게가 남는다.
    /// - 탐험: 사람이 살던 방(ConscienceOwnedRooms)의 상자를 뒤지면 +ConscienceOwnedRoomAmount (탐험 한 번에 한 번).
    /// - 이벤트·행동: 사람을 외면한 선택지의 효과(GameEffect.Conscience). 덜어 내는 행동(선생님과의 대화·사연 읽기)은 음수.
    /// 처음으로 무게가 오르면 FirstFlag와 원인 깃발이 켜지고, 그날 밤 윤 선생님께 고해하는 장면(EVT_CONSCIENCE_CONFESS)이 열린다.
    /// 고해하면 무게가 하나 덜어진다 (튜토리얼, 이벤트 데이터의 효과).
    /// </summary>
    public static class ConscienceRules
    {
        /// <summary>처음으로 양심의 무게가 올랐다 (고해 장면의 조건).</summary>
        public const string FirstFlag = "CONSCIENCE_FIRST";

        /// <summary>고해 장면을 마쳤다.</summary>
        public const string ConfessedFlag = "CONSCIENCE_CONFESSED";

        /// <summary>처음 무게가 오른 까닭 (고해 대사가 갈린다). 처음 한 번만 켠다.</summary>
        public const string CauseFelled = "CONSCIENCE_CAUSE_FELLED";
        public const string CauseHome = "CONSCIENCE_CAUSE_HOME";
        public const string CauseChoice = "CONSCIENCE_CAUSE_CHOICE";

        /// <summary>탐험 결과를 반영한다: 쓰러뜨린 수 기록, 문턱을 넘기면 무게, 사람이 살던 방을 뒤졌으면 무게, 결과에 실린 그 밖의 변화.</summary>
        public static void ApplyExpedition(GameState state, ExpeditionResult result, BalanceConfig balance, StateDelta delta)
        {
            if (state == null || result == null || balance == null)
            {
                return;
            }

            var felled = Math.Max(0, result.ZombiesFelled);
            state.Exploration.ZombiesFelled += felled;
            if (balance.ConscienceFelledThreshold > 0 && felled >= balance.ConscienceFelledThreshold)
            {
                state.Exploration.HeavyFelledRuns++;
                EffectApplier.ChangeConscience(state, balance.ConscienceFelledAmount, balance, delta, CauseFelled);
            }

            if (result.SearchedOwnedRoom)
            {
                state.Exploration.OwnedRoomRuns++;
                EffectApplier.ChangeConscience(state, balance.ConscienceOwnedRoomAmount, balance, delta, CauseHome);
            }

            if (result.ConscienceDelta != 0)
            {
                EffectApplier.ChangeConscience(state, result.ConscienceDelta, balance, delta, CauseChoice);
            }
        }

        /// <summary>무게가 실제로 올랐을 때 (EffectApplier.ChangeConscience가 부른다): 처음이면 FirstFlag와 원인 깃발을 켠다.</summary>
        public static void NoteRise(GameState state, string cause, StateDelta delta)
        {
            if (state == null || !state.Flags.Add(FirstFlag))
            {
                return;
            }

            delta?.FlagsSet.Add(FirstFlag);
            var causeFlag = string.IsNullOrEmpty(cause) ? CauseChoice : cause;
            if (state.Flags.Add(causeFlag))
            {
                delta?.FlagsSet.Add(causeFlag);
            }
        }

        /// <summary>이 방 종류(RoomTemplateDef.KindId)가 사람이 살던 방인가.</summary>
        public static bool IsOwnedRoom(BalanceConfig balance, string roomKindId)
        {
            if (balance?.ConscienceOwnedRooms == null || string.IsNullOrEmpty(roomKindId))
            {
                return false;
            }

            return Array.IndexOf(balance.ConscienceOwnedRooms, roomKindId) >= 0;
        }
    }
}
