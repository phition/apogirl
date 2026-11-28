using UnityEngine;

namespace ApoGirl.Presentation.App
{
    /// <summary>조작 설정 (PlayerPrefs에 저장): 탐험 자동 진행, 디버그 "선택지 결과 미리 보기".</summary>
    public static class ControlPrefs
    {
        private const string AutoExploreKey = "ApoGirl.AutoExplore";
        private const string ChoicePreviewKey = "ApoGirl.Debug.ChoicePreview";

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

        /// <summary>디버그: 이벤트 선택지에 마우스를 올리면 고른 결과를 미리 보여 준다 (기본 꺼짐). 실제로 쓸지는 DebugAccess.ChoicePreview.</summary>
        public static bool ChoicePreview
        {
            get => PlayerPrefs.GetInt(ChoicePreviewKey, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(ChoicePreviewKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
