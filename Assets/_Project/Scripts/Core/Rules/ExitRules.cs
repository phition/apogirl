using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 탈출로(보조 출구): 쇠지렛대로 한 번 비집어 열면 깃발 EXIT_OPEN_{지역}이 남아 다음 탐험부터 열려 있다.
    /// 학교 밖 지역(바깥 길이 있는 곳)은 열어 둔 탈출로로 들어갈 수도 있다 (출정 준비의 정문·후문 진입, ExpeditionRequest.RearEntry).
    /// 계절 재해가 다시 막은 출구(폭설에 파묻힘)는 열려 있어도 막힌다 — 지도는 이 규칙 뒤에 DisasterRules.Apply를 얹는다.
    /// </summary>
    public static class ExitRules
    {
        public static string OpenFlag(string locationId) => "EXIT_OPEN_" + locationId;

        /// <summary>바깥 길(진입로·탈출로)이 있는 지역: 학교 밖 (교실동·튜토리얼 제외).</summary>
        public static bool HasApproach(string locationId) =>
            !string.IsNullOrEmpty(locationId) && !locationId.Contains("SCHOOL") && !locationId.Contains("TUTORIAL");

        public static bool IsOpened(ICollection<string> flags, string locationId) => flags != null && flags.Contains(OpenFlag(locationId));

        /// <summary>열어 둔 탈출로를 반영한 지역 (사본). 바뀔 것이 없으면 원본을 그대로.</summary>
        public static LocationDef Apply(LocationDef location, ICollection<string> flags)
        {
            var layout = location?.Layout;
            if (layout == null || !IsOpened(flags, location.Id) || !layout.Exits.Any(e => e.Kind == ExitKind.Alternate && e.StartsLocked))
            {
                return location;
            }

            var copy = location.Copy();
            copy.Layout = layout.CopyLinks();
            foreach (var exit in copy.Layout.Exits.Where(e => e.Kind == ExitKind.Alternate))
            {
                exit.StartsLocked = false;
            }

            return copy;
        }

        /// <summary>
        /// 후문으로 들어갈 수 있나: 바깥 길이 있는 지역에서, (열어 둔 탈출로·재해를 반영한) 지도의 보조 출구가 열려 있고 막히지 않았다.
        /// location은 ExitRules.Apply → DisasterRules.Apply를 거친 지도.
        /// </summary>
        public static bool CanEnterRear(LocationDef location)
        {
            return location?.Layout != null && HasApproach(location.Id)
                   && location.Layout.Exits.Any(e => e.Kind == ExitKind.Alternate && !e.StartsLocked && string.IsNullOrEmpty(e.BlockedBy));
        }

        /// <summary>이번 탐험의 지도: 열어 둔 탈출로 → 계절 재해.</summary>
        public static LocationDef RunMap(LocationDef plain, ICollection<string> flags, DisasterDef disaster) =>
            DisasterRules.Apply(Apply(plain, flags), disaster);
    }
}
