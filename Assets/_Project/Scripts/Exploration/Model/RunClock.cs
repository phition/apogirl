using System;
using ApoGirl.Core.Expedition;

namespace ApoGirl.Exploration.Model
{
    /// <summary>시계 구간 (문서 6.3.2-(2) 활동성 표의 시간대).</summary>
    public enum ClockPhase
    {
        Morning,
        Afternoon,
        Dusk,
        Evening,
        Midnight,
        Dawn,
    }

    /// <summary>
    /// 탐험 시간 예산 (문서 6.3.5-(1)): 낮 = 일몰 시계, 밤 = 밤 시계. 실시간 초 단위.
    /// </summary>
    public sealed class RunClock
    {
        /// <summary>낮: 오전~정오 / 오후 / 황혼 경계 (정규화 시간).</summary>
        public const float DayAfternoonAt = 0.4f;

        public const float DayDuskAt = 0.8f;

        /// <summary>밤: 초저녁 / 한밤 / 새벽 경계.</summary>
        public const float NightMidnightAt = 0.3f;

        public const float NightDawnAt = 0.75f;

        public RunClock(float lengthSeconds, ExpeditionTime timeOfDay)
        {
            if (lengthSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthSeconds));
            }

            Length = lengthSeconds;
            TimeOfDay = timeOfDay;
        }

        public float Length { get; }

        public ExpeditionTime TimeOfDay { get; }

        public float Elapsed { get; private set; }

        public float Remaining => Math.Max(0f, Length - Elapsed);

        /// <summary>0 → 1</summary>
        public float Normalized => Math.Min(1f, Elapsed / Length);

        public bool IsOver => Elapsed >= Length;

        public ClockPhase Phase
        {
            get
            {
                var t = Normalized;
                if (TimeOfDay == ExpeditionTime.Day)
                {
                    return t < DayAfternoonAt ? ClockPhase.Morning : t < DayDuskAt ? ClockPhase.Afternoon : ClockPhase.Dusk;
                }

                return t < NightMidnightAt ? ClockPhase.Evening : t < NightDawnAt ? ClockPhase.Midnight : ClockPhase.Dawn;
            }
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds > 0f)
            {
                Elapsed = Math.Min(Length, Elapsed + deltaSeconds);
            }
        }
    }
}
