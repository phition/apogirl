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
                root.style.unityFontDefinition = FontDefinition.FromSDFFont(_font);
            }
        }
    }
}
