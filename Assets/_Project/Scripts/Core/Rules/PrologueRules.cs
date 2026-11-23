using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>프롤로그 선택을 새 게임 효과와 프로필로 바꾼다 (화면 PrologueFlow와 봇이 같이 쓴다).</summary>
    public static class PrologueRules
    {
        public const string PersonalityQuestionId = "PERSONALITY";

        /// <summary>공통 효과 + 고른 선택지 효과를 합치고, 프로필 칸(혈액형·성적·과목·동아리·성격 유형)을 채운다.</summary>
        public static GameEffect BuildEffect(PrologueDef prologue, IEnumerable<(string Question, string Option)> picks, ProfileState profile)
        {
            var effect = new GameEffect().MergeFrom(prologue.Common);
            var personality = "";
            foreach (var (questionId, optionId) in picks)
            {
                var option = prologue.Questions.Find(q => q.Id == questionId)?.Options.Find(o => o.Id == optionId);
                if (option == null)
                {
                    continue;
                }

                effect.MergeFrom(option.Effect);
                if (questionId == PersonalityQuestionId)
                {
                    personality += optionId;
                }

                switch (option.ProfileField)
                {
                    case "BloodType":
                        profile.BloodType = Enum.TryParse<BloodType>(option.ProfileValue, out var blood) ? blood : BloodType.Unknown;
                        break;
                    case "GradeBandId":
                        profile.GradeBandId = option.ProfileValue;
                        break;
                    case "FavoriteSubjectId":
                        profile.FavoriteSubjectId = option.ProfileValue;
                        break;
                    case "ClubId":
                        profile.ClubId = option.ProfileValue;
                        break;
                }
            }

            profile.PersonalityType = personality;
            return effect;
        }
    }

    /// <summary>
    /// 프롤로그의 메아리 (기획 PROPOSAL_PROLOGUE_ECHOES): 성격 F의 동료 스트레스 완충, 좋아하는 과목, 희망과 의지·위기 이탈.
    /// 수치는 BalanceConfig "프롤로그의 메아리" 절.
    /// </summary>
    public static class EchoRules
    {
        // ── 성격 F ──

        /// <summary>
        /// 동료 스트레스가 amount만큼 오를 때 실제로 오를 양. F 특성이 있으면 그 동료 호감(0~100)에 비례해 줄인다.
        /// 소수점은 날짜(step)에 나눠 쌓아(TraitRules.Spread) 평균이 정확히 맞게 한다. 내릴 때(음수)는 그대로.
        /// </summary>
        public static int CompanionStressRise(GameState state, BalanceConfig balance, string npcId, int amount, int salt = 0)
        {
            if (amount <= 0 || string.IsNullOrEmpty(balance.CompanionStressReliefTraitId)
                || !state.Player.Traits.Contains(balance.CompanionStressReliefTraitId))
            {
                return amount;
            }

            var affinity = Math.Max(0, Math.Min(100, ConditionEvaluator.ValueOf(state, ValueKind.NpcAffinity, npcId)));
            var relief = balance.CompanionStressReliefMax * affinity / 100f;
            if (relief <= 0f)
            {
                return amount;
            }

            var step = state.Calendar.Week * 14 + state.Calendar.Day * 2 + (int)state.Calendar.Phase + salt;
            return TraitRules.Spread(amount * (1f - relief), Math.Max(1, step));
        }

        // ── 좋아하는 과목 ──

        /// <summary>이 교육 행동이 좋아하는 과목인가 (프롤로그 과목 → 숙련 표).</summary>
        public static bool IsFavorite(GameState state, BalanceConfig balance, ActionDef action)
        {
            if (action == null || action.Category != ActionCategory.Education || !action.HasProficiency)
            {
                return false;
            }

            var subject = state.Player.Profile.FavoriteSubjectId ?? "";
            var def = balance.FavoriteSubjects.Find(f => f.SubjectId == subject);
            return def != null && def.Proficiency == action.Proficiency;
        }

        /// <summary>
        /// "딱히 없어": 숙련이 FavoriteFoundLevel에 처음 닿으면 그 과목이 좋아하는 과목이 된다. 바뀌었으면 true.
        /// </summary>
        public static bool TryFindFavorite(GameState state, BalanceConfig balance, ProficiencyType type, StateDelta delta)
        {
            var profile = state.Player.Profile;
            if ((profile.FavoriteSubjectId ?? "") != balance.FavoriteNoneId || state.Player.ProficiencyLevel(type) < balance.FavoriteFoundLevel)
            {
                return false;
            }

            var def = balance.FavoriteSubjects.Find(f => f.Proficiency == type);
            if (def == null)
            {
                return false;
            }

            profile.FavoriteSubjectId = def.SubjectId;
            if (state.Flags.Add(balance.FavoriteFoundFlag))
            {
                delta.FlagsSet.Add(balance.FavoriteFoundFlag);
            }

            return true;
        }

        // ── 희망 ──

        /// <summary>희망이 깎일 때 의지가 받쳐 준다: 의지 1당 WillpowerHopeGuardPerPoint (상한 Max). 오를 때는 그대로.</summary>
        public static int GuardHope(GameState state, BalanceConfig balance, int amount)
        {
            if (amount >= 0)
            {
                return amount;
            }

            var guard = Math.Min(balance.WillpowerHopeGuardMax, state.Player.GetStat(StatType.Willpower) * balance.WillpowerHopeGuardPerPoint);
            return -(int)Math.Round(-amount * (1f - guard), MidpointRounding.AwayFromZero);
        }

        /// <summary>주간 결산: 희망이 높으면 의지가 자란다.</summary>
        public static int ApplyWeekly(GameState state, BalanceConfig balance, StateDelta delta)
        {
            if (balance.HopeWillpowerWeekly <= 0 || state.Player.Hidden.Hope < balance.HopeWillpowerThreshold)
            {
                return 0;
            }

            return EffectApplier.ChangeStat(state, StatType.Willpower, balance.HopeWillpowerWeekly, balance, delta);
        }

        /// <summary>희망이 위기 문턱 이상인가.</summary>
        public static bool HopeHigh(GameState state, BalanceConfig balance) => state.Player.Hidden.Hope >= balance.CrisisHopeThreshold;

        /// <summary>
        /// 위기 이탈 예고를 희망이 비켜 가게 하는가. 판 시드·주·NPC로 정해지는 따로 굴린 난수라 본 난수 흐름을 건드리지 않는다.
        /// </summary>
        public static bool HopeSparesLeave(GameState state, BalanceConfig balance, string npcId)
        {
            if (!HopeHigh(state, balance) || balance.CrisisHopeSkipChance <= 0f)
            {
                return false;
            }

            unchecked
            {
                var hash = (int)2166136261;
                foreach (var c in npcId ?? "")
                {
                    hash = (hash ^ c) * 16777619;
                }

                var seed = (state.Rng?.Seed ?? 0) ^ 0x4F9E ^ (state.Calendar.Week * 7919) ^ hash;
                return new SeededRandom(SeededRandom.CreateState(seed)).NextDouble() < balance.CrisisHopeSkipChance;
            }
        }

        /// <summary>설득 판정에 더할 희망 보너스.</summary>
        public static float PersuadeBonus(GameState state, CheckDef check, BalanceConfig balance) =>
            check != null && check.UsesHope && HopeHigh(state, balance) ? balance.CrisisHopePersuadeBonus : 0f;

        public static string HopeHeldFlag(BalanceConfig balance, string npcId) => (balance.HopeHeldFlagPrefix ?? "HOPE_HELD_") + npcId;
    }
}
