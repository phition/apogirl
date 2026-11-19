using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Text
{
    /// <summary>
    /// 그레이박스 임시 한글 폰트: OS 폰트(맑은 고딕)를 런타임 동적 폰트로 쓴다. 배포용이 아니다.
    /// 정식 폰트(Pretendard 등) 에셋이 생기면 Theme.uss의 -unity-font-definition으로 교체하고 이 클래스는 지운다.
    /// </summary>
    public static class KoreanFont
    {
        private static readonly string[] Families = { "Malgun Gothic", "맑은 고딕", "Noto Sans KR", "NanumGothic" };
        private static FontAsset _font;
        private static bool _tried;

        public static void Apply(VisualElement root)
        {
            if (!_tried)
            {
                _tried = true;
                foreach (var family in Families)
                {
                    try
                    {
                        _font = FontAsset.CreateFontAsset(family, "Regular");
                    }
                    catch (Exception)
                    {
                        _font = null;
                    }

                    if (_font != null)
                    {
                        break;
                    }
                }

                if (_font == null)
                {
                    Debug.LogWarning("한글 OS 폰트를 찾지 못함: 한글이 □로 보일 수 있음");
                }
            }

            if (_font != null)
            {
                var definition = FontDefinition.FromSDFFont(_font);
                root.style.unityFontDefinition = definition;

                // 드롭다운 목록 등은 패널 최상위에 붙으므로 패널 전체에도 적용한다
                if (root.panel?.visualTree != null)
                {
                    root.panel.visualTree.style.unityFontDefinition = definition;
                }
                else
                {
                    root.RegisterCallback<AttachToPanelEvent>(e => ((VisualElement)e.target).panel.visualTree.style.unityFontDefinition = definition);
                }
            }
        }

        /// <summary>입력 칸처럼 테마가 글꼴을 따로 지정하는 요소: 안쪽 글자 요소까지 직접 적용한다.</summary>
        public static void ApplyDeep(VisualElement element)
        {
            Apply(element);
            if (_font == null)
            {
                return;
            }

            var definition = FontDefinition.FromSDFFont(_font);
            element.Query<TextElement>().ForEach(t => t.style.unityFontDefinition = definition);
            element.Query(className: "unity-text-element").ForEach(t => t.style.unityFontDefinition = definition);
        }
    }
}
