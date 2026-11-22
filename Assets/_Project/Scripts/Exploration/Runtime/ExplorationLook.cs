using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 화면의 '보이는 맛' (Resources/Art/exploration_look.json): 1단계 후처리 색 보정·블룸·비네트·톤매핑과 빛 기둥(볼류메트릭), 2단계 그림자·도트 빛.
    /// - 후처리는 탐험 카메라에만 켠다 (전역 Volume이지만 허브 카메라는 후처리를 쓰지 않는다).
    /// - 색 보정은 픽셀마다 색만 바꿔 도트 모양을 그대로 둔다. 블룸은 밝은 곳(손전등·창문)에만 걸리게 문턱을 높게.
    /// - 디버그 F9: 후처리·빛 기둥·그림자·도트 빛 켜고 끄기 (전후 비교, 기록에 영향 없음).
    /// </summary>
    public sealed class ExplorationLook
    {
        [Serializable]
        public sealed class Profile
        {
            /// <summary>Neutral · ACES · None</summary>
            public string Tonemap = "Neutral";
            public float Exposure;
            public float Contrast;
            public float Saturation;
            public string ColorFilter = "#FFFFFF";
            public string ShadowsTint = "#808080";
            public string HighlightsTint = "#808080";
            public float SplitBalance;
            public float BloomThreshold = 1f;
            public float BloomIntensity;
            public float BloomScatter = 0.6f;
            public string BloomTint = "#FFFFFF";
            public float VignetteIntensity;
            public float VignetteSmoothness = 0.4f;
            public string VignetteColor = "#000000";

            /// <summary>빛 기둥(볼류메트릭) 세기: 손전등 원뿔·햇빛 줄기. 0이면 끈다.</summary>
            public float FlashlightVolume;
            public float SunbeamVolume;
        }

        /// <summary>
        /// 2단계 그림자 (ShadowCaster2D): 손전등 빛을 사물·사람이 가려 뒤쪽 벽에 그림자가 진다. 그림자 모양은 스프라이트 외곽선.
        /// 낮 전체 조명·햇빛 줄기는 그림자를 만들지 않는다.
        /// </summary>
        [Serializable]
        public sealed class ShadowConfig
        {
            public bool Enabled = true;
            public float Intensity = 0.5f;
            public float Softness = 0.3f;

            /// <summary>이보다 낮은 그림(유닛)은 그림자를 만들지 않는다 (종이·작은 소품).</summary>
            public float MinCasterHeight = 0.8f;

            public bool Zombies = true;
            public bool People = true;
            public bool Containers = true;
            public bool Decor = true;
        }

        /// <summary>
        /// 2단계 도트 빛 (셰이더 ApoGirl/ExplorationPixelLit, 머티리얼 Resources/Art/exploration_pixel_lit):
        /// 빛을 그림 한 화소마다 한 번만 읽어(Snap) 빛 경계가 도트 격자를 따르고, 빛 세기를 Steps 단계로 끊는다(StepStrength만큼 섞기).
        /// </summary>
        [Serializable]
        public sealed class PixelLightConfig
        {
            public bool Enabled = true;
            public bool Snap = true;

            /// <summary>그림 한 화소가 화면 높이의 이 비율보다 크면 맞추지 않는다 (작은 그림을 크게 늘린 단색 벽·바닥이 큰 덩어리로 끊기지 않게).</summary>
            public float SnapMaxTexel = 0.01f;
            public float Steps = 6f;
            public float StepStrength = 0.7f;
        }

        [Serializable]
        public sealed class Config
        {
            public bool Enabled = true;
            public Profile Day = new Profile();
            public Profile Night = new Profile();
            public ShadowConfig Shadows = new ShadowConfig();
            public PixelLightConfig PixelLight = new PixelLightConfig();
        }

        private const string DefaultLitShader = "Universal Render Pipeline/2D/Sprite-Lit-Default";
        private static readonly int PixelLightOnId = Shader.PropertyToID("_ApoPixelLightOn");
        private static readonly int PixelSnapId = Shader.PropertyToID("_ApoPixelSnap");
        private static readonly int PixelSnapMaxTexelId = Shader.PropertyToID("_ApoPixelSnapMaxTexel");
        private static readonly int LightStepsId = Shader.PropertyToID("_ApoLightSteps");
        private static readonly int LightStepStrengthId = Shader.PropertyToID("_ApoLightStepStrength");
        private Material _pixelMaterial;

        private static Config _config;

        public static Config Settings
        {
            get
            {
                if (_config == null)
                {
                    var asset = Resources.Load<TextAsset>("Art/exploration_look");
                    _config = asset != null ? JsonUtility.FromJson<Config>(asset.text) ?? new Config() : new Config { Enabled = false };
                }

                return _config;
            }
        }

        /// <summary>탐험마다 설정을 다시 읽는다 (조정하며 바로 보기). 월드를 짓기 전에 부른다.</summary>
        public static void Reload() => _config = null;

        /// <summary>그림자를 드리울 물건 (설정의 종류가 켜져 있고 그림이 충분히 클 때).</summary>
        /// <param name="floorY">주면 바닥에 선 것만 (그림 아래 끝이 바닥 + 0.5 유닛보다 높으면 벽에 걸린 것이라 뺀다)</param>
        public static void AddCaster(GameObject go, bool kindEnabled, float? floorY = null)
        {
            var shadows = Settings.Shadows;
            if (go == null || !kindEnabled || shadows == null || !shadows.Enabled)
            {
                return;
            }

            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null || renderer.bounds.size.y < shadows.MinCasterHeight || go.GetComponent<ShadowCaster2D>() != null
                || (floorY.HasValue && renderer.bounds.min.y > floorY.Value + 0.5f))
            {
                return;
            }

            var caster = go.AddComponent<ShadowCaster2D>();
            caster.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
            caster.selfShadows = false;
            caster.useRendererSilhouette = true;   // 자기 그림은 그림자에 덮이지 않는다
        }

        /// <summary>그림자를 만드는 빛 (손전등).</summary>
        public void ApplyShadows(Light2D light)
        {
            if (light == null)
            {
                return;
            }

            var shadows = Settings.Shadows ?? new ShadowConfig();
            light.shadowsEnabled = On && shadows.Enabled;
            light.shadowIntensity = shadows.Intensity;
            light.shadowSoftness = shadows.Softness;
        }

        private Volume _volume;
        private Profile _profile = new Profile();

        /// <summary>F9로 끈 상태 (비교용).</summary>
        public bool On { get; private set; }

        public float FlashlightVolume => On ? _profile.FlashlightVolume : 0f;
        public float SunbeamVolume => On ? _profile.SunbeamVolume : 0f;

        public void Setup(Camera camera, Transform parent, bool day)
        {
            On = Settings.Enabled;
            _profile = (day ? Settings.Day : Settings.Night) ?? new Profile();
            camera.allowHDR = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var go = new GameObject("ExplorationLookVolume");
            go.transform.SetParent(parent, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;
            _volume.sharedProfile = Build(_profile);
            _volume.weight = On ? 1f : 0f;

            var pixel = Settings.PixelLight;
            _pixelMaterial = pixel != null && pixel.Enabled ? Resources.Load<Material>("Art/exploration_pixel_lit") : null;
            if (pixel != null)
            {
                Shader.SetGlobalFloat(PixelSnapId, pixel.Snap ? 1f : 0f);
                Shader.SetGlobalFloat(PixelSnapMaxTexelId, pixel.SnapMaxTexel);
                Shader.SetGlobalFloat(LightStepsId, Mathf.Max(0f, pixel.Steps));
                Shader.SetGlobalFloat(LightStepStrengthId, Mathf.Clamp01(pixel.StepStrength));
            }

            ApplyPixelGlobal();
        }

        /// <summary>도트 빛: 기본 조명 머티리얼을 쓰는 스프라이트를 도트 빛 머티리얼로 (다른 머티리얼은 그대로). 새로 생긴 그림을 위해 다시 불러도 된다.</summary>
        public void ApplyPixelLight(Transform root)
        {
            if (_pixelMaterial == null || root == null)
            {
                return;
            }

            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material != null && material.shader != null && material.shader.name == DefaultLitShader)
                {
                    renderer.sharedMaterial = _pixelMaterial;
                }
            }
        }

        private void ApplyPixelGlobal() => Shader.SetGlobalFloat(PixelLightOnId, On && _pixelMaterial != null ? 1f : 0f);

        public void Toggle()
        {
            On = !On;
            if (_volume != null)
            {
                _volume.weight = On ? 1f : 0f;
            }

            ApplyPixelGlobal();
        }

        public void Dispose()
        {
            if (_volume != null && _volume.sharedProfile != null)
            {
                UnityEngine.Object.Destroy(_volume.sharedProfile);
            }
        }

        private static VolumeProfile Build(Profile p)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(p.Tonemap == "ACES" ? TonemappingMode.ACES : p.Tonemap == "None" ? TonemappingMode.None : TonemappingMode.Neutral);

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(p.Exposure);
            color.contrast.Override(p.Contrast);
            color.saturation.Override(p.Saturation);
            color.colorFilter.Override(Hex(p.ColorFilter, Color.white));

            var split = profile.Add<SplitToning>(true);
            split.shadows.Override(Hex(p.ShadowsTint, Color.gray));
            split.highlights.Override(Hex(p.HighlightsTint, Color.gray));
            split.balance.Override(p.SplitBalance);

            if (p.BloomIntensity > 0f)
            {
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(p.BloomThreshold);
                bloom.intensity.Override(p.BloomIntensity);
                bloom.scatter.Override(p.BloomScatter);
                bloom.tint.Override(Hex(p.BloomTint, Color.white));
            }

            if (p.VignetteIntensity > 0f)
            {
                var vignette = profile.Add<Vignette>(true);
                vignette.intensity.Override(p.VignetteIntensity);
                vignette.smoothness.Override(p.VignetteSmoothness);
                vignette.color.Override(Hex(p.VignetteColor, Color.black));
            }

            return profile;
        }

        private static Color Hex(string hex, Color fallback) => ColorUtility.TryParseHtmlString(hex ?? "", out var c) ? c : fallback;
    }
}
