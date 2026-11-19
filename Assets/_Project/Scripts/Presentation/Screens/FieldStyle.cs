using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 런타임 기본 테마의 입력 칸·드롭다운은 밝은 배경이라 어두운 화면에서 글자가 보이지 않는다.
    /// 한글 글꼴과 함께 어두운 배경·밝은 글자를 직접 지정한다.
    /// </summary>
    internal static class FieldStyle
    {
        private static readonly Color Background = new Color(0.2f, 0.215f, 0.235f);
        private static readonly Color Border = new Color(0.267f, 0.286f, 0.314f);
        private static readonly Color Text = new Color(0.894f, 0.882f, 0.855f);

        public static T Dark<T>(T field) where T : VisualElement
        {
            KoreanFont.ApplyDeep(field);
            field.Query(className: "unity-base-field__input").ForEach(Paint);
            field.Query(className: "unity-base-text-field__input").ForEach(Paint);
            field.Query(className: "unity-base-popup-field__input").ForEach(Paint);
            field.Query(className: "unity-text-element").ForEach(t => t.style.color = Text);
            return field;
        }

        private static void Paint(VisualElement input)
        {
            input.style.backgroundColor = Background;
            input.style.borderTopColor = Border;
            input.style.borderBottomColor = Border;
            input.style.borderLeftColor = Border;
            input.style.borderRightColor = Border;
            input.style.color = Text;
        }
    }
}
