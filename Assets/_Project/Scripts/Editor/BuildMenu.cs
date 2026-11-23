using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 테스트용 단독 실행 파일 (PC만). 메뉴 ApoGirl/Build/Windows Test Build.
    /// 출력: 프로젝트 폴더의 Builds/Windows/LastSemester_날짜시각/ (git에서 제외).
    /// </summary>
    public static class BuildMenu
    {
        public const string OutputRoot = "Builds/Windows";

        [MenuItem("ApoGirl/Build/Windows Test Build")]
        public static void BuildWindowsMenu()
        {
            var report = BuildWindows(false);
            if (report != null && report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(report.summary.outputPath);
            }
        }

        [MenuItem("ApoGirl/Build/Windows Test Build (Development)")]
        public static void BuildWindowsDevelopmentMenu() => BuildWindows(true);

        /// <summary>빌드하고 결과를 돌려준다. 실패하면 콘솔에 오류.</summary>
        public static BuildReport BuildWindows(bool development)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] 빌드할 씬이 없다 (Build Settings).");
                return null;
            }

            var folder = Path.Combine(OutputRoot, PlayerSettings.productName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + (development ? "_dev" : ""));
            var exe = Path.Combine(folder, PlayerSettings.productName + ".exe");
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = development ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None,
            };

            KeepRuntimeRenderingShaders();
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                File.WriteAllText(Path.Combine(folder, "읽어 주세요.txt"), Readme(development), new UTF8Encoding(true));
                Debug.Log($"[Build] 성공: {Path.GetFullPath(exe)} ({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0}초)");
            }
            else
            {
                Debug.LogError($"[Build] 실패: {summary.result}, 오류 {summary.totalErrors}개");
            }

            return report;
        }

        /// <summary>
        /// 빌드가 에디터와 똑같이 보이게: 탐험 후처리 볼륨(톤매핑·블룸·비네트…)과 2D 빛(손전등 쿠키·노멀·그림자)은
        /// 실행 중에 코드로 만들어서, URP의 "안 쓰는 셰이더 빼기"가 씬에서 못 보고 빌드에서 지운다
        /// (Player.log: "Shader 'Hidden/Universal Render Pipeline/Bloom' … stripped"). 두 가지 빼기를 끈다.
        /// </summary>
        public static void KeepRuntimeRenderingShaders()
        {
            var urp = GraphicsSettings.GetRenderPipelineSettings<URPShaderStrippingSetting>();
            if (urp == null || (!urp.stripUnusedPostProcessingVariants && !urp.strip2DUnusedVariants))
            {
                return;
            }

            urp.stripUnusedPostProcessingVariants = false;
            urp.strip2DUnusedVariants = false;
            Debug.Log("[Build] URP 셰이더 빼기: 후처리·2D 빛 변형을 남긴다 (실행 중에 만드는 볼륨·빛)");
        }

        private static string Readme(bool development)
        {
            var persistent = "%USERPROFILE%\\AppData\\LocalLow\\" + PlayerSettings.companyName + "\\" + PlayerSettings.productName;
            return string.Join("\r\n", new[]
            {
                $"〈마지막 학기 (가제)〉 테스트 빌드 {DateTime.Now:yyyy-MM-dd HH:mm}{(development ? " (개발 빌드)" : "")}",
                "",
                "실행: " + PlayerSettings.productName + ".exe (폴더째 옮겨야 한다. exe만 옮기면 실행되지 않는다)",
                "전체 화면 전환: Alt+Enter",
                "",
                "[허브]",
                development ? "  F1  디버그 패널 (지역·시설 해금, 이벤트 강제 발생, 52주차로, 엔딩 보기 등)" : "  (디버그 패널은 개발 빌드에서만 열린다)",
                "",
                "[탐험]",
                "  이동 A/D, 마우스 왼쪽 공격(없으면 밀치기)·오른쪽 수색(길게)/들어가기 (Q·E도 됨), 손전등은 마우스 높이로 위아래, 문 앞 W 살피기 · S 듣기, Space 숨기, F 손전등(도구 필요), Tab 주머니·배낭(우클릭 쓰기·장착, 아래 장비 칸에 놓아 장착)",
                development ? "  디버그: F2 x10 · F3 AI·소음 · F4 무적 · F5 시야 · F6 아이템 · F7 +1분 · F8 시계 끝 · F9 후처리" : "  (디버그 키는 개발 빌드에서만 된다)",
                "",
                "[저장·기록 위치]",
                "  " + persistent,
                "  세이브, 엔딩 갤러리(Gallery.json), 탐험 플레이 기록(PlayLogs\\expeditions.jsonl)이 여기에 남는다.",
                "  디버그 기능을 쓴 판의 탐험은 플레이 기록에서 빠진다. 기록 파일을 보내 주면 탐험 수치 보정(M7-4)에 쓴다.",
            });
        }
    }

    /// <summary>메뉴 밖(File ▸ Build)에서 빌드해도 같은 설정이 되게.</summary>
    public sealed class KeepRenderingShadersPreprocess : IPreprocessBuildWithReport
    {
        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report) => BuildMenu.KeepRuntimeRenderingShaders();
    }
}
