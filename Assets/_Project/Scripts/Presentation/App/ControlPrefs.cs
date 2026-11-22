using UnityEngine;

namespace ApoGirl.Presentation.App
{
    /// <summary>가상 패드를 언제 보일지.</summary>
    public enum PadMode
    {
        Auto = 0,   // 휴대폰에서만
        On = 1,
        Off = 2,
    }

    /// <summary>조작 설정 (PlayerPrefs에 저장): 탐험 자동 진행, 가상 패드 표시.</summary>
    public static class ControlPrefs
    {
        private const string AutoExploreKey = "ApoGirl.AutoExplore";
        private const string PadModeKey = "ApoGirl.PadMode";

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

        public static PadMode Pad
        {
            get => (PadMode)Mathf.Clamp(PlayerPrefs.GetInt(PadModeKey, 0), 0, 2);
            set
            {
                PlayerPrefs.SetInt(PadModeKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>지금 탐험에서 가상 패드를 보이는가.</summary>
        public static bool PadVisible => Pad == PadMode.On || (Pad == PadMode.Auto && Application.isMobilePlatform);
    }
}
