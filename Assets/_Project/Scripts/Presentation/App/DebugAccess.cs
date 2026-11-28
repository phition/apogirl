using UnityEngine;

namespace ApoGirl.Presentation.App
{
    /// <summary>
    /// 디버그 기능을 쓸 수 있는가: 에디터와 개발 빌드(메뉴 "Windows Test Build (Development)")에서만.
    /// 일반 테스트 빌드("Windows Test Build")에서는 허브 F1 디버그 패널·밸런스 표, 탐험 F2~F9가 꺼진다.
    /// </summary>
    public static class DebugAccess
    {
        public static bool Enabled => Application.isEditor || Debug.isDebugBuild;

        /// <summary>"선택지 결과 미리 보기"를 쓰는가: 디버그를 쓸 수 있고 F1 패널에서 켰을 때만.</summary>
        public static bool ChoicePreview => Enabled && ControlPrefs.ChoicePreview;
    }
}
