using System;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Random
{
    /// <summary>시뮬레이션 난수. 테스트에서는 고정 값 구현으로 교체한다.</summary>
    public interface IRandomService
    {
        /// <summary>[0, 1)</summary>
        double NextDouble();

        /// <summary>[minInclusive, maxInclusive]</summary>
        int Range(int minInclusive, int maxInclusive);

        /// <summary>하위 시뮬레이션(탐험 등)에 넘길 새 시드.</summary>
        int NextSeed();
    }

    /// <summary>
    /// SplitMix64 기반 난수. 상태를 <see cref="RandomState"/>에 직접 기록하므로
    /// 세이브/로드 후에도 같은 난수열이 이어진다.
    /// </summary>
    public sealed class SeededRandom : IRandomService
    {
        private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
        private readonly RandomState _state;

        public SeededRandom(RandomState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public static RandomState CreateState(int seed)
        {
            return new RandomState
            {
                Seed = seed,
                State = unchecked((long)((ulong)(uint)seed * GoldenGamma)),
            };
        }

        public double NextDouble()
        {
            return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
        }

        public int Range(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentException($"잘못된 범위: [{minInclusive}, {maxInclusive}]");
            }

            var span = (ulong)((long)maxInclusive - minInclusive + 1);
            return (int)(minInclusive + (long)(NextUInt64() % span));
        }

        public int NextSeed()
        {
            return (int)(NextUInt64() & 0x7FFFFFFF);
        }

        private ulong NextUInt64()
        {
            unchecked
            {
                var s = (ulong)_state.State + GoldenGamma;
                _state.State = (long)s;
                var z = s;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
