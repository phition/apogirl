using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 계획 보드의 겉모습: 요일 머리글(토 하늘색·일 붉은색)과 낮·밤 색.
    /// 낮 전용 = 상아색, 밤 전용 = 짙은 남색, 둘 다 되는 행동 = 사선으로 반씩 (색은 Theme.uss 변수).
    /// </summary>
    internal static class PlanLook
    {
        private static readonly CustomStyleProperty<Color> SplitColor = new CustomStyleProperty<Color>("--split-color");

        /// <summary>그 주 day(1~7)의 요일 이름을 쓰고 토·일이면 색 클래스를 붙인다.</summary>
        public static void Weekday(Label label, int day, BalanceConfig balance)
        {
            var weekday = CalendarRules.Weekday(day, balance);
            label.text = Loc.T("weekday." + weekday);
            label.EnableInClassList("weekday-sat", weekday == 5);
            label.EnableInClassList("weekday-sun", weekday == 6);
        }

        /// <summary>행동 카드에 낮·밤 색을 입힌다. 둘 다 되는 카드는 오른쪽 아래 반을 남색 삼각형으로 덮는다 (맨 뒤 자식).</summary>
        public static void Phase(VisualElement card, SlotTag tag)
        {
            card.EnableInClassList("phase-day", tag == SlotTag.DayOnly);
            card.EnableInClassList("phase-night", tag == SlotTag.NightOnly);
            card.EnableInClassList("phase-both", tag == SlotTag.Both);
            if (tag != SlotTag.Both)
            {
                return;
            }

            var split = new VisualElement { name = "phase-split", pickingMode = PickingMode.Ignore };
            split.AddToClassList("phase-split");
            split.generateVisualContent += DrawSplit;
            card.Insert(0, split);
        }

        /// <summary>카드 아이콘을 배경 대신 자식으로 (사선 삼각형 위에 그려지게).</summary>
        public static void Icon(VisualElement card, Texture2D icon, float size)
        {
            card.EnableInClassList("with-icon", icon != null);
            if (icon == null)
            {
                return;
            }

            var image = new VisualElement { name = "card-icon", pickingMode = PickingMode.Ignore };
            image.AddToClassList("card-icon");
            image.style.width = size;
            image.style.height = size;
            image.style.marginTop = -size / 2f;
            image.style.backgroundImage = new StyleBackground(icon);
            card.Add(image);
        }

        private static void DrawSplit(MeshGenerationContext context)
        {
            var element = context.visualElement;
            var rect = element.contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            if (!element.customStyle.TryGetValue(SplitColor, out var color))
            {
                color = new Color(0.11f, 0.15f, 0.27f);
            }

            // 오른쪽 위 → 왼쪽 아래 사선: 아래 오른쪽 삼각형이 밤
            var painter = context.painter2D;
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
