using System;
using ApoGirl.Core.Definitions;

namespace ApoGirl.Exploration.Model
{
    /// <summary>
    /// 플레이어 화면에 좀비·NPC를 그릴지 판단 (좀비의 감지 판정과는 별개).
    /// - 다른 공간(다른 층 복도, 다른 방)에 있는 존재는 낮이든 밤이든 보이지 않는다.
    /// - 어둠 속에서는 가까운 거리만 보이고, 손전등을 켜면 비추는 방향으로 더 멀리 보인다.
    /// </summary>
    public static class PlayerViewRules
    {
        /// <param name="facing">플레이어가 바라보는 방향 (+1 오른쪽, -1 왼쪽).</param>
        public static bool CanPlayerSee(
            int playerArea, float playerX, int facing, bool dark, bool flashlightOn,
            int targetArea, float targetX, BalanceConfig balance)
        {
            if (playerArea != targetArea)
            {
                return false;
            }

            if (!dark)
            {
                return true;
            }

            var dx = targetX - playerX;
            var distance = Math.Abs(dx);
            if (distance <= balance.NightViewDistance)
            {
                return true;
            }

            return flashlightOn && Math.Sign(dx) == Math.Sign(facing) && distance <= balance.FlashlightViewDistance;
        }
    }
}
