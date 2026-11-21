using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 공간 조명 (M6-5). 그림만 바꾼다 — 시야는 ExpeditionDirector.IsDarkIn(밤·황혼, 또는 방 안), 수색 속도는 밤·황혼(IsDark)만 따른다.
    /// - 낮 복도: 화면 앞쪽 창으로 드는 햇빛 줄기. 시계가 흐르면 해가 움직여 줄기의 기울기·색·밝기가 천천히 바뀐다
    ///   (아침 = 한쪽으로 길게 기운 옅은 빛 → 한낮 = 곧고 밝은 빛 → 늦은 오후 = 반대로 기운 금빛 → 황혼 = 붉은빛).
    ///   줄기는 가운데가 가장 밝고 가장자리·바닥 쪽으로 부드럽게 옅어지는 그림(쿠키)으로 그린다.
    /// - 낮 방 안: 전기가 끊겨 어둡고, 복도 쪽 창 아래로 빛이 조금 샌다.
    /// - 밤: 옅은 달빛 줄기. 손전등은 뜨거운 가운데 → 넓은 가장자리로 번지는 세 겹 빛 (ExpeditionDirector가 방향을 준다).
    /// 밝기 값은 BalanceConfig 조명 절 (DayCorridorAmbient·DayRoomAmbient·Sunbeam*·RoomWindowLightIntensity).
    /// </summary>
    internal sealed class ExplorationLighting
    {
        public enum Phase
        {
            Day,
            Dusk,
            Night,
            Dawn,
        }

        /// <summary>줄기 너비(유닛)와 해가 가장 기울었을 때의 각도(도).</summary>
        private const float BeamWidth = 2.7f;
        private const float MaxSunAngle = 36f;
        private const float DuskAngle = -40f;
        private const float MoonAngle = 16f;
        private const float DawnAngle = 38f;

        private static readonly Color MorningBeam = new Color(1f, 0.87f, 0.74f);
        private static readonly Color NoonBeam = new Color(1f, 0.97f, 0.88f);
        private static readonly Color GoldenBeam = new Color(1f, 0.78f, 0.5f);
        private static readonly Color DuskBeam = new Color(1f, 0.5f, 0.36f);
        private static readonly Color NightBeam = new Color(0.55f, 0.66f, 1f);
        private static readonly Color DawnBeam = new Color(0.92f, 0.8f, 0.92f);
        private static readonly Color RoomLeak = new Color(0.9f, 0.93f, 1f);

        private readonly Transform _world;
        private readonly BalanceConfig _balance;
        private readonly float _floorHeight;
        private readonly List<Light2D> _beams = new List<Light2D>();
        private readonly List<Light2D> _pools = new List<Light2D>();
        private readonly List<Light2D> _leaks = new List<Light2D>();
        private Phase _phase;

        // 지금 값 (목표 값으로 부드럽게 따라간다)
        private float _angle;
        private float _intensity;
        private Color _color;
        private bool _snapped;

        private static Sprite _beamCookie;
        private static Sprite _poolCookie;
        private static Sprite _leakCookie;

        public ExplorationLighting(Transform world, BalanceConfig balance, float floorHeight, Phase phase)
        {
            _world = world;
            _balance = balance;
            _floorHeight = floorHeight;
            _phase = phase;
        }

        /// <summary>복도 한 층: 일정 간격으로 햇빛 줄기 + 줄기가 닿는 바닥의 번진 빛.</summary>
        public void AddCorridor(ExplorationArea corridor)
        {
            // 층마다 한두 곳에만 빛이 든다 (자리는 층마다 다르고 늘 같다)
            var random = new System.Random(7919 * (corridor.Floor + 1) + Mathf.RoundToInt(corridor.Width * 13f));
            var count = random.Next(Mathf.Max(0, _balance.SunbeamsPerFloorMin), Mathf.Max(_balance.SunbeamsPerFloorMin, _balance.SunbeamsPerFloorMax) + 1);
            var top = _floorHeight - 0.18f;
            var span = Mathf.Max(1f, corridor.Width - 8f);
            for (var i = 0; i < count; i++)
            {
                // 복도를 count 구간으로 나눠 구간마다 하나씩 (서로 붙지 않게)
                var x = 4f + span * (i + 0.2f + (float)random.NextDouble() * 0.6f) / count;
                var beam = SpriteLight("Sunbeam", BeamCookie(), new Vector3(corridor.Origin.x + x, corridor.Origin.y + top, 0f));
                _beams.Add(beam);
                var pool = SpriteLight("SunPool", PoolCookie(), new Vector3(corridor.Origin.x + x, corridor.Origin.y + 0.1f, 0f));
                _pools.Add(pool);
            }

            AddDust(corridor);
            _snapped = false;
        }

        private static Material _dustMaterial;

        /// <summary>
        /// 떠다니는 먼지: 1픽셀짜리 알갱이가 복도에 천천히 떠다닌다. 조명을 받는 재질이라 햇빛 줄기·손전등 빛 안에서만 반짝이고
        /// 그늘에서는 거의 보이지 않는다.
        /// </summary>
        private void AddDust(ExplorationArea corridor)
        {
            if (_dustMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (shader == null)
                {
                    return;
                }

                _dustMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            var go = new GameObject("Dust");
            go.transform.SetParent(_world, false);
            go.transform.localPosition = new Vector3(corridor.Origin.x + corridor.Width * 0.5f, corridor.Origin.y + _floorHeight * 0.5f, 0f);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var count = Mathf.Clamp(Mathf.RoundToInt(corridor.Width * 1.6f), 20, 400);
            var main = particles.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 13f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(1f / ExplorationArt.PixelsPerUnit, 2f / ExplorationArt.PixelsPerUnit);
            main.startColor = new Color(1f, 0.98f, 0.92f, 0.5f);
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = particles.emission;
            emission.rateOverTime = count / 10f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(corridor.Width, _floorHeight - 0.6f, 0.01f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.07f, 0.07f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.02f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _dustMaterial;
            renderer.sortingOrder = -1;
            particles.Play();
        }

        /// <summary>방 (처음 들어갈 때 만든다): 복도 쪽 창마다 창 아래로 번지는 희미한 빛.</summary>
        public void AddRoom(ExplorationArea room)
        {
            if (room.Template == null)
            {
                return;
            }

            var top = BuildingView.WindowBottom + BuildingView.WindowHeight;
            foreach (var window in room.Template.Windows)
            {
                var leak = SpriteLight("WindowLeak", LeakCookie(), new Vector3(room.Origin.x + window.X + window.Width * 0.5f, room.Origin.y + top, 0f));
                leak.transform.localScale = new Vector3(window.Width + 1.6f, top + 0.1f, 1f);
                _leaks.Add(leak);
            }

            _snapped = false;
        }

        public void SetPhase(Phase phase)
        {
            _phase = phase;
        }

        /// <summary>
        /// 매 프레임. dayProgress = 낮 탐험 시계 (0 = 아침, 1 = 해 질 녘). 해의 위치에 맞춰 줄기를 천천히 돌리고 색·밝기를 바꾼다.
        /// 시간대가 바뀌면(황혼·새벽) 몇 초에 걸쳐 넘어간다.
        /// </summary>
        public void Tick(float dt, float dayProgress)
        {
            Target(dayProgress, out var angle, out var intensity, out var color);
            if (!_snapped || dt >= 1000f)
            {
                _angle = angle;
                _intensity = intensity;
                _color = color;
                _snapped = true;
            }
            else
            {
                var k = 1f - Mathf.Exp(-dt * 0.8f);
                _angle = Mathf.Lerp(_angle, angle, k);
                _intensity = Mathf.Lerp(_intensity, intensity, k);
                _color = Color.Lerp(_color, color, k);
            }

            // 줄기: 천장의 한 점을 축으로 기운다. 길이는 바닥까지 (기울수록 길다)
            var radians = _angle * Mathf.Deg2Rad;
            var length = (_floorHeight - 0.2f) / Mathf.Max(0.5f, Mathf.Cos(radians));
            var rotation = Quaternion.Euler(0f, 0f, _angle);
            var scale = new Vector3(BeamWidth, length, 1f);
            var beamIntensity = _balance.SunbeamIntensity * _intensity;
            foreach (var beam in _beams)
            {
                beam.transform.localRotation = rotation;
                beam.transform.localScale = scale;
                beam.color = _color;
                beam.intensity = beamIntensity;
            }

            // 바닥에 닿는 곳: 줄기 끝을 따라 옆으로 움직이는 넓고 옅은 빛 (벽·바닥에 되비친 빛)
            var reach = Mathf.Tan(radians) * (_floorHeight - 0.2f);
            for (var i = 0; i < _pools.Count; i++)
            {
                var pool = _pools[i];
                var anchor = _beams[i].transform.localPosition;
                pool.transform.localPosition = new Vector3(anchor.x + reach, anchor.y - (_floorHeight - 0.2f) + 0.35f, 0f);
                pool.transform.localScale = new Vector3(BeamWidth * 2.4f, 1.5f, 1f);
                pool.color = _color;
                pool.intensity = beamIntensity * 0.5f;
            }

            var leakColor = _phase == Phase.Day || _phase == Phase.Dawn ? Color.Lerp(RoomLeak, _color, 0.35f) : _color;
            var leakIntensity = _balance.RoomWindowLightIntensity * _intensity;
            foreach (var leak in _leaks)
            {
                leak.color = leakColor;
                leak.intensity = leakIntensity;
            }
        }

        /// <summary>보이는 공간의 전체 밝기 배율: 낮 방 안은 어둡고, 복도는 햇빛 줄기를 살리려고 조금 낮춘다.</summary>
        public float AmbientFactor(ExplorationArea area)
        {
            if (area == null)
            {
                return 1f;
            }

            switch (_phase)
            {
                case Phase.Day:
                    return area.IsOutdoor ? 1f : area.IsRoom ? _balance.DayRoomAmbient : _balance.DayCorridorAmbient;
                case Phase.Dawn:
                    return area.IsOutdoor ? 1.25f : area.IsRoom ? Mathf.Min(1f, _balance.DayRoomAmbient * 2f) : 1f;
                case Phase.Night:
                case Phase.Dusk:
                    return area.IsOutdoor ? 1.5f : 1f;   // 밖은 달빛·노을로 건물 안보다 조금 밝다
                default:
                    return 1f;
            }
        }

        /// <summary>낮에도 어두운 방 안이면 주인공 주변을 희미하게 밝힌다.</summary>
        public bool IsDimRoom(ExplorationArea area) => area != null && area.IsRoom && (_phase == Phase.Day || _phase == Phase.Dawn);

        private void Target(float dayProgress, out float angle, out float intensity, out Color color)
        {
            switch (_phase)
            {
                case Phase.Dusk:
                    angle = DuskAngle;
                    intensity = 0.45f;
                    color = DuskBeam;
                    return;
                case Phase.Night:
                    angle = MoonAngle;
                    intensity = 0.3f;
                    color = NightBeam;
                    return;
                case Phase.Dawn:
                    angle = DawnAngle;
                    intensity = 0.6f;
                    color = DawnBeam;
                    return;
            }

            // 낮: 해가 한쪽에서 반대쪽으로 넘어간다. 한낮에 가장 곧고 밝다.
            var t = Mathf.Clamp01(dayProgress);
            angle = Mathf.Lerp(MaxSunAngle, -MaxSunAngle, Mathf.SmoothStep(0f, 1f, t));
            intensity = 0.62f + 0.38f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 0.9f + 0.05f));
            color = t < 0.45f ? Color.Lerp(MorningBeam, NoonBeam, t / 0.45f)
                : t < 0.8f ? Color.Lerp(NoonBeam, GoldenBeam, (t - 0.45f) / 0.35f)
                : Color.Lerp(GoldenBeam, DuskBeam, (t - 0.8f) / 0.2f);
        }

        private Light2D SpriteLight(string name, Sprite cookie, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Sprite;
            light.lightCookieSprite = cookie;
            light.intensity = 0f;
            return light;
        }

        // ── 빛 그림 (쿠키): 코드로 한 번 만든다 ──

        /// <summary>
        /// 햇빛 줄기: 1x1 유닛, 축 = 위 가운데. 가로는 가운데가 밝고 가장자리로 부드럽게(종 모양), 세로는 창 쪽이 밝고 바닥으로 갈수록 옅어진다.
        /// 양 끝은 짧게 사라져 기울었을 때 모서리가 위·아래층으로 번지지 않는다.
        /// </summary>
        private static Sprite BeamCookie()
        {
            if (_beamCookie != null)
            {
                return _beamCookie;
            }

            const int w = 128;
            const int h = 128;
            var texture = NewTexture(w, h);
            var pixels = new Color[w * h];
            for (var y = 0; y < h; y++)
            {
                var v = y / (float)(h - 1);            // 0 = 바닥 끝, 1 = 창
                var along = Mathf.Lerp(0.72f, 1f, Mathf.Pow(v, 1.3f));
                var ends = Mathf.SmoothStep(0f, 1f, v / 0.07f) * Mathf.SmoothStep(0f, 1f, (1f - v) / 0.05f);
                var spread = Mathf.Lerp(1.25f, 0.8f, v); // 바닥 쪽으로 갈수록 조금 퍼진다
                for (var x = 0; x < w; x++)
                {
                    var u = (x / (float)(w - 1) - 0.5f) * 2f / spread;
                    var core = Mathf.Exp(-u * u * 3.6f);          // 밝은 속
                    var halo = Mathf.Exp(-u * u * 1.6f) * 0.35f;  // 넓고 옅은 가장자리
                    var edge = Mathf.SmoothStep(0f, 1f, (1f - Mathf.Abs(u)) / 0.25f);
                    var value = Mathf.Clamp01((core * 0.75f + halo) * along * ends * edge);
                    pixels[y * w + x] = new Color(value, value, value, value);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            _beamCookie = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), h, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            return _beamCookie;
        }

        /// <summary>바닥에 번진 빛·방 창 아래 빛에 쓰는 둥근 빛: 1x1 유닛, 가운데가 밝고 바깥으로 부드럽게.</summary>
        private static Sprite PoolCookie()
        {
            if (_poolCookie != null)
            {
                return _poolCookie;
            }

            const int size = 64;
            var texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = (x / (float)(size - 1) - 0.5f) * 2f;
                    var v = (y / (float)(size - 1) - 0.5f) * 2f;
                    var d = Mathf.Sqrt(u * u + v * v);
                    var value = Mathf.Clamp01(Mathf.Exp(-d * d * 3.2f) * Mathf.SmoothStep(0f, 1f, (1f - d) / 0.3f));
                    pixels[y * size + x] = new Color(value, value, value, value);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            _poolCookie = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            return _poolCookie;
        }

        /// <summary>방 창 아래로 새는 빛: 1x1 유닛, 축 = 위 가운데. 창 높이에서 가장 밝고 바닥·옆으로 옅어진다.</summary>
        private static Sprite LeakCookie()
        {
            if (_leakCookie != null)
            {
                return _leakCookie;
            }

            const int size = 64;
            var texture = NewTexture(size, size);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                var v = y / (float)(size - 1); // 0 = 바닥, 1 = 창 위
                var along = Mathf.Lerp(0.25f, 1f, Mathf.SmoothStep(0f, 1f, v)) * Mathf.SmoothStep(0f, 1f, (1f - v) / 0.12f) * Mathf.SmoothStep(0f, 1f, v / 0.1f);
                for (var x = 0; x < size; x++)
                {
                    var u = (x / (float)(size - 1) - 0.5f) * 2f;
                    var value = Mathf.Clamp01(Mathf.Exp(-u * u * 2.4f) * along * Mathf.SmoothStep(0f, 1f, (1f - Mathf.Abs(u)) / 0.25f));
                    pixels[y * size + x] = new Color(value, value, value, value);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            _leakCookie = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 1f), size, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            return _leakCookie;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
    }
}
