using UnityEngine;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 타격 이펙트 한 번: 픽셀 그림 fx_hit_0..n이 있으면 그 프레임을 한 번 돌리고,
    /// 없으면 코드 그림(흰 십자 번쩍임이 커지며 사라짐). 끝나면 스스로 지운다.
    /// 조명에 묻히지 않게 빛을 받지 않는 머티리얼(Resources/Art/exploration_fx_unlit)로 그린다.
    /// </summary>
    public sealed class HitFx : MonoBehaviour
    {
        private const string Prefix = "fx_hit_";
        private const float FallbackSeconds = 0.14f;
        private const int SortingOrder = 14;

        private static Sprite _white;
        private static Material _unlit;
        private static bool _unlitLoaded;

        private SpriteRenderer[] _renderers;
        private Sprite[] _frames;
        private float _fps;
        private float _time;

        public void Play(bool flip, float framesPerSecond)
        {
            _fps = Mathf.Max(1f, framesPerSecond);
            var count = ExplorationArt.HasPixel(Prefix + "0") ? ExplorationArt.FrameCount(Prefix, 1) : 0;
            if (count > 0)
            {
                _frames = new Sprite[count];
                for (var i = 0; i < count; i++)
                {
                    _frames[i] = ExplorationArt.Get(Prefix + i);
                }

                var renderer = Add("Frame", _frames[0], Color.white);
                renderer.flipX = flip;
                _renderers = new[] { renderer };
                return;
            }

            // 코드 그림: 가는 막대 두 개를 엇갈려 놓은 번쩍임
            _renderers = new[]
            {
                Add("RayA", White, new Color(1f, 0.97f, 0.85f)),
                Add("RayB", White, new Color(1f, 0.9f, 0.6f)),
            };
            _renderers[0].transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            _renderers[1].transform.localRotation = Quaternion.Euler(0f, 0f, -55f);
            Step(0f);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (_frames != null)
            {
                var index = (int)(_time * _fps);
                if (index >= _frames.Length)
                {
                    Destroy(gameObject);
                    return;
                }

                _renderers[0].sprite = _frames[index];
                return;
            }

            if (_time >= FallbackSeconds)
            {
                Destroy(gameObject);
                return;
            }

            Step(_time / FallbackSeconds);
        }

        /// <summary>코드 그림: 진행도 t(0~1)에 따라 길어지고 가늘어지며 옅어진다.</summary>
        private void Step(float t)
        {
            var length = Mathf.Lerp(0.35f, 0.95f, t);
            var thickness = Mathf.Lerp(0.12f, 0.03f, t);
            var alpha = 1f - t * t;
            foreach (var renderer in _renderers)
            {
                renderer.transform.localScale = new Vector3(length, thickness, 1f);
                var color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }

        private SpriteRenderer Add(string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = SortingOrder;
            if (Unlit != null)
            {
                renderer.sharedMaterial = Unlit;
            }

            return renderer;
        }

        private static Material Unlit
        {
            get
            {
                if (!_unlitLoaded)
                {
                    _unlitLoaded = true;
                    _unlit = Resources.Load<Material>("Art/exploration_fx_unlit");
                }

                return _unlit;
            }
        }

        /// <summary>1유닛 크기 흰 사각형 (피벗 가운데).</summary>
        private static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    _white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                }

                return _white;
            }
        }
    }
}
