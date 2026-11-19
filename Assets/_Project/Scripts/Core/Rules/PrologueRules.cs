using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
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
}
