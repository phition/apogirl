using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Presentation.App
{
    /// <summary>
    /// 선생님 추천의 고정 시간표 (압정, Docs/TEACHER_PLAN_PROPOSAL.md 결정 D2): 플레이어 설정이라 모든 판에 같이 쓴다.
    /// 값은 "N3=@CLUB|N6=ACT_NIGHT_DIARY"처럼 "|"로 잇는다. 저장한 적이 없으면 BalanceConfig.TeacherDefaultPins.
    /// </summary>
    public static class TeacherPinPrefs
    {
        private const string Key = "ApoGirl.TeacherPins";

        public static List<string> Load(BalanceConfig balance)
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                return (balance.TeacherDefaultPins ?? Array.Empty<string>()).ToList();
            }

            return PlayerPrefs.GetString(Key, "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static void Save(IEnumerable<string> pins)
        {
            PlayerPrefs.SetString(Key, string.Join("|", pins ?? Enumerable.Empty<string>()));
            PlayerPrefs.Save();
        }
    }
}
