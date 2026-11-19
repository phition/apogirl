using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Expedition
{
    /// <summary>
    /// 확률 기반 탐험 모델 (문서 12장 M7: 실시간 탐험을 기대 수익/손실/쓰러짐 확률로 추상화).
    /// 요청의 Seed만으로 결과가 결정되므로 같은 요청은 항상 같은 결과를 낸다.
    /// </summary>
    public sealed class HeadlessExpeditionRunner : IExpeditionRunner
    {
        private readonly IDefinitionDatabase _db;

        public HeadlessExpeditionRunner(IDefinitionDatabase db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public ExpeditionResult Run(ExpeditionRequest request)
        {
            var model = _db.GetExpeditionModel(request.LocationId);
            var timeYield = request.TimeOfDay == ExpeditionTime.Day ? model.Day : model.Night;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed));

            request.Stats.TryGetValue(StatType.Agility, out var agility);
            var downChance = timeYield.BaseDownChance
                             - agility * model.AgilityDownReduction
                             + request.SleepDebt * model.SleepDebtDownIncrease
                             + (request.ConsecutiveRun ? model.ConsecutiveDownIncrease : 0f);
            downChance = Math.Min(Math.Max(downChance, 0f), 0.95f);

            var result = new ExpeditionResult
            {
                FatigueDelta = timeYield.Fatigue,
                StressDelta = random.Range(timeYield.StressMin, Math.Max(timeYield.StressMin, timeYield.StressMax)),
            };

            if (random.NextDouble() < downChance)
            {
                // 쓰러짐: 배낭 전부 분실, 중상, 스트레스 대폭 상승 (문서 6.3.6)
                result.Status = ExtractStatus.Down;
                result.HpDelta = -timeYield.HpLossOnDown;
                result.StressDelta += timeYield.StressOnDown;
                result.Injuries = InjuryFlags.Fracture;
                return result;
            }

            result.Status = ExtractStatus.Extracted;
            foreach (var range in timeYield.Resources)
            {
                var amount = random.Range(range.Min, Math.Max(range.Min, range.Max));
                if (amount > 0)
                {
                    result.ResourceLoot.Add(new ResourceAmount(range.Resource, amount));
                }
            }

            if (request.TimeOfDay == ExpeditionTime.Day && timeYield.ConscienceMax > 0)
            {
                result.ConscienceDelta = random.Range(0, timeYield.ConscienceMax);
            }

            if (request.TimeOfDay == ExpeditionTime.Night && random.NextDouble() < timeYield.DawnReturnChance)
            {
                result.SleepDebtDelta = 1;
            }

            return result;
        }
    }
}
