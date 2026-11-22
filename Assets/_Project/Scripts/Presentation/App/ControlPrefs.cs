using UnityEngine;

namespace ApoGirl.Presentation.App
{
    /// <summary>조작 설정 (PlayerPrefs에 저장): 탐험 자동 진행.</summary>
    public static class ControlPrefs
    {
        private const string AutoExploreKey = "ApoGirl.AutoExplore";

        /// <summary>탐험을 직접 하지 않고 헤드리스 확률 모델로 자동 진행한다.</summary>
        public static bool AutoExplore
        {
            get => PlayerPrefs.GetInt(AutoExploreKey, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(AutoExploreKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
