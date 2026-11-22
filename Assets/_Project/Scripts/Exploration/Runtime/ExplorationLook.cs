using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 화면의 '보이는 맛' 1단계 (Resources/Art/exploration_look.json): 후처리 색 보정·블룸·비네트·톤매핑과 빛 기둥(볼류메트릭).
    /// - 후처리는 탐험 카메라에만 켠다 (전역 Volume이지만 허브 카메라는 후처리를 쓰지 않는다).
    /// - 색 보정은 픽셀마다 색만 바꿔 도트 모양을 그대로 둔다. 블룸은 밝은 곳(손전등·창문)에만 걸리게 문턱을 높게.
    /// - 디버그 F9: 후처리·빛 기둥 켜고 끄기 (전후 비교, 기록에 영향 없음).
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
            public float Intensity = 0.75f;
            public float Softness = 0.3f;

            /// <summary>이보다 낮은 그림(유닛)은 그림자를 만들지 않는다 (종이·작은 소품).</summary>
            public float MinCasterHeight = 0.8f;

            public bool Zombies = true;
            public bool People = true;
            public bool Containers = true;
            public bool Decor = true;
        }

        [Serializable]
        public sealed class Config
        {
            public bool Enabled = true;
            public Profile Day = new Profile();
            public Profile Night = new Profile();
            public ShadowConfig Shadows = new ShadowConfig();
        }

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
        }

        public void Toggle()
        {
            On = !On;
            if (_volume != null)
            {
                _volume.weight = On ? 1f : 0f;
            }
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
