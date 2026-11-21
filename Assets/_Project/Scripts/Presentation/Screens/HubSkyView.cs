using System;
using System.Collections.Generic;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>단면도 뒷배경 설정 (hub_section.json의 "Sky"). 그림은 Resources/Art/UI/HubSky 아래.</summary>
    [Serializable]
    public sealed class HubSkyConfig
    {
        [Serializable]
        public sealed class Palette
        {
            /// <summary>하늘 위·아래 색, 마을 그림에 곱할 색 (#RRGGBB).</summary>
            public string Top = "#7f9cb8";
            public string Bottom = "#c9d6dc";
            public string Town = "#ffffff";
        }

        public string Folder = "UI/HubSky/";
        public string Far = "town_far";
        public string Near = "town_near";
        public string Sun = "sun";
        public string Moon = "moon";
        public string[] Clouds = Array.Empty<string>();
        public string[] DarkClouds = Array.Empty<string>();

        /// <summary>먼 마을 띠의 밑변: 옥상 바닥에서 아래로 (그림 픽셀). 아래쪽은 건물에 가린다.</summary>
        public float FarSink = 24;

        /// <summary>해·달 자리 (단면도 왼쪽 위에서, 그림 픽셀).</summary>
        public float SunX = 60;
        public float SunY = 8;

        /// <summary>구름이 떠다니는 높이 범위와 빠르기 (그림 픽셀, 초당).</summary>
        public float CloudTop = -20;
        public float CloudBottom = 50;
        public float CloudSpeed = 6;
        public int CloudsClear = 3;
        public int CloudsRain = 7;
        public int RainDrops = 90;
        public int SnowFlakes = 60;
        public int Stars = 40;

        public Palette Day = new Palette();
        public Palette Night = new Palette { Top = "#070b18", Bottom = "#1c2438", Town = "#454c66" };
        public Palette Rain = new Palette { Top = "#56626e", Bottom = "#8a949a", Town = "#b4b9bd" };
        public Palette RainNight = new Palette { Top = "#05070d", Bottom = "#151a24", Town = "#3a4050" };
        public Palette Heat = new Palette { Top = "#d9a56a", Bottom = "#f1dcb0", Town = "#f2e0c8" };
        public Palette Cold = new Palette { Top = "#9fb2c4", Bottom = "#e4ebef", Town = "#dfe6ee" };
    }

    /// <summary>
    /// 허브 단면도 뒤 바깥 풍경: 하늘(시간·날씨 색), 먼 마을·가까운 마을 띠(PixelLab 픽셀 아트), 해·달·별, 떠가는 구름, 비·눈.
    /// 단면도와 같은 좌표(그림 픽셀 × Scale)로 단면도 하늘 겹(hub-sky) 안에 그린다. 비·눈은 건물 뒤라서 방 안에는 내리지 않는다.
    /// </summary>
    internal sealed class HubSkyView
    {
        private readonly VisualElement _layer;
        private readonly VisualElement _frame;
        private readonly HubSkyConfig _config;
        private readonly float _scale;
        private readonly List<Cloud> _clouds = new List<Cloud>();
        private readonly List<Drop> _drops = new List<Drop>();
        private readonly List<VisualElement> _stars = new List<VisualElement>();
        private readonly System.Random _random = new System.Random(7);
        private VisualElement _far;
        private VisualElement _near;
        private VisualElement _sun;
        private VisualElement _moon;
        private VisualElement _cloudLayer;
        private VisualElement _dropLayer;
        private Texture2D _gradient;
        private float _left;
        private float _right;
        private float _skyTop;
        private float _ground;
        private string _signature;

        public HubSkyView(VisualElement layer, VisualElement frame, HubSkyConfig config, float scale)
        {
            _layer = layer;
            _frame = frame;
            _config = config ?? new HubSkyConfig();
            _scale = scale;
            _layer.pickingMode = PickingMode.Ignore;
        }

        /// <summary>단면도 크기가 정해지면 다시 짓는다. roofFloor = 옥상 바닥 높이, ground = 땅 높이 (그림 픽셀).</summary>
        public void Build(float width, float roofFloor, float ground)
        {
            _layer.Clear();
            _clouds.Clear();
            _drops.Clear();
            _stars.Clear();
            _signature = null;
            _left = -1000f / _scale;
            _right = width + 1000f / _scale;
            _skyTop = -400f / _scale;
            _ground = ground;

            _stars.Capacity = _config.Stars;
            for (var i = 0; i < _config.Stars; i++)
            {
                var star = Place(_layer, Lerp(_left, _right, (float)_random.NextDouble()), Lerp(_skyTop, roofFloor - 30, (float)_random.NextDouble()), 1, 1);
                star.AddToClassList("hub-star");
                _stars.Add(star);
            }

            _sun = Sprite(_config.Sun, _config.SunX, _config.SunY, "hub-sun");
            _moon = Sprite(_config.Moon, _config.SunX, _config.SunY, "hub-moon");

            // 먼 마을: 옥상 바닥 위로 지평선이 보인다 (밑은 건물에 가림)
            var far = Load(_config.Far);
            if (far != null)
            {
                _far = Strip(far, roofFloor + _config.FarSink - far.height);
            }

            _cloudLayer = Place(_layer, _left, _skyTop, _right - _left, ground - _skyTop);
            _cloudLayer.pickingMode = PickingMode.Ignore;

            // 가까운 마을: 땅 위 (건물 양옆으로 보인다)
            var near = Load(_config.Near);
            if (near != null)
            {
                _near = Strip(near, ground - near.height);
            }

            _dropLayer = Place(_layer, _left, _skyTop, _right - _left, ground - _skyTop);
            _dropLayer.pickingMode = PickingMode.Ignore;
            _dropLayer.style.overflow = Overflow.Hidden;
        }

        /// <summary>시간·날씨를 반영한다 (바뀐 것이 있을 때만 다시 그린다).</summary>
        public void Apply(bool night, WeatherType weather, Season season)
        {
            var signature = night + "|" + weather + "|" + season;
            if (signature == _signature)
            {
                return;
            }

            _signature = signature;
            var rain = weather == WeatherType.Rain;
            var snow = weather == WeatherType.Cold && season == Season.Winter;
            var palette = rain ? (night ? _config.RainNight : _config.Rain)
                : night ? _config.Night
                : weather == WeatherType.Heat ? _config.Heat
                : weather == WeatherType.Cold ? _config.Cold
                : _config.Day;

            // 하늘: 위·아래 두 색 그러데이션을 틀 전체에 늘인다
            if (_gradient == null)
            {
                _gradient = new Texture2D(1, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            }

            _gradient.SetPixel(0, 1, Parse(palette.Top));
            _gradient.SetPixel(0, 0, Parse(palette.Bottom));
            _gradient.Apply();
            _frame.style.backgroundImage = new StyleBackground(_gradient);
            _frame.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(Length.Percent(100), Length.Percent(100)));

            var town = Parse(palette.Town);
            if (_far != null) _far.style.unityBackgroundImageTintColor = town * new Color(0.85f, 0.88f, 0.95f, 1f);
            if (_near != null) _near.style.unityBackgroundImageTintColor = town;

            Show(_sun, !night && !rain);
            if (_sun != null) _sun.EnableInClassList("heat", weather == WeatherType.Heat);
            Show(_moon, night && !rain);
            foreach (var star in _stars)
            {
                Show(star, night && !rain && _random.NextDouble() < 0.8);
            }

            // 구름: 맑으면 흰 구름 몇 개, 비면 먹구름 많이
            _cloudLayer.Clear();
            _clouds.Clear();
            var keys = rain && _config.DarkClouds.Length > 0 ? _config.DarkClouds : _config.Clouds;
            var count = rain ? _config.CloudsRain : weather == WeatherType.Heat ? 1 : _config.CloudsClear;
            for (var i = 0; i < count && keys.Length > 0; i++)
            {
                var texture = Load(keys[i % keys.Length]);
                if (texture == null)
                {
                    continue;
                }

                var y = Lerp(_config.CloudTop, _config.CloudBottom, (float)_random.NextDouble()) - _skyTop;
                var cloud = new Cloud { Element = Image(_cloudLayer, texture), Width = texture.width, X = Lerp(0, _right - _left, (float)_random.NextDouble()), Y = y };
                cloud.Speed = _config.CloudSpeed * Lerp(0.6f, 1.4f, (float)_random.NextDouble()) * (rain ? 1.8f : 1f);
                cloud.Element.style.top = cloud.Y * _scale;
                cloud.Element.style.opacity = night ? 0.45f : 0.95f;
                _clouds.Add(cloud);
            }

            // 비·눈
            _dropLayer.Clear();
            _drops.Clear();
            var drops = rain ? _config.RainDrops : snow ? _config.SnowFlakes : 0;
            var height = _ground - _skyTop;
            for (var i = 0; i < drops; i++)
            {
                var drop = new Drop
                {
                    Element = new VisualElement(),
                    X = Lerp(0, _right - _left, (float)_random.NextDouble()),
                    Y = Lerp(0, height, (float)_random.NextDouble()),
                    Speed = rain ? Lerp(220, 300, (float)_random.NextDouble()) : Lerp(14, 26, (float)_random.NextDouble()),
                    Sway = snow ? Lerp(0.5f, 1.5f, (float)_random.NextDouble()) : 0f,
                    Phase = (float)_random.NextDouble() * 6f,
                };
                drop.Element.AddToClassList(rain ? "hub-rain" : "hub-snow");
                drop.Element.pickingMode = PickingMode.Ignore;
                drop.Element.style.position = Position.Absolute;
                _dropLayer.Add(drop.Element);
                _drops.Add(drop);
            }
        }

        public void Tick(float dt)
        {
            var span = _right - _left;
            foreach (var cloud in _clouds)
            {
                cloud.X += cloud.Speed * dt;
                if (cloud.X > span)
                {
                    cloud.X = -cloud.Width;
                }

                cloud.Element.style.left = Mathf.Round(cloud.X) * _scale;
            }

            var height = _ground - _skyTop;
            foreach (var drop in _drops)
            {
                drop.Y += drop.Speed * dt;
                drop.Phase += dt;
                var x = drop.X + (drop.Sway > 0 ? Mathf.Sin(drop.Phase * 1.3f) * drop.Sway * 4f : -drop.Y * 0.12f);
                if (drop.Y > height)
                {
                    drop.Y -= height;
                    drop.X = Lerp(0, span, (float)_random.NextDouble());
                }

                drop.Element.style.left = x * _scale;
                drop.Element.style.top = drop.Y * _scale;
            }
        }

        // ── 도우미 ──

        private VisualElement Strip(Texture2D texture, float top)
        {
            var strip = Place(_layer, _left, top, _right - _left, texture.height);
            strip.AddToClassList("hub-town");
            strip.style.backgroundImage = new StyleBackground(texture);
            strip.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.Repeat, Repeat.NoRepeat));
            strip.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(new Length(texture.width * _scale), new Length(texture.height * _scale)));
            strip.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Bottom));
            return strip;
        }

        private VisualElement Sprite(string key, float x, float y, string cssClass)
        {
            var texture = Load(key);
            if (texture == null)
            {
                return null;
            }

            var element = Place(_layer, x, y, texture.width, texture.height);
            element.AddToClassList(cssClass);
            element.style.backgroundImage = new StyleBackground(texture);
            element.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
            return element;
        }

        private VisualElement Image(VisualElement parent, Texture2D texture)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.width = texture.width * _scale;
            element.style.height = texture.height * _scale;
            element.style.backgroundImage = new StyleBackground(texture);
            element.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
            parent.Add(element);
            return element;
        }

        private VisualElement Place(VisualElement parent, float x, float y, float width, float height)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.left = x * _scale;
            element.style.top = y * _scale;
            element.style.width = width * _scale;
            element.style.height = height * _scale;
            parent.Add(element);
            return element;
        }

        private Texture2D Load(string key) => string.IsNullOrEmpty(key) ? null : StoryArt.Load(_config.Folder + key);

        private static void Show(VisualElement element, bool show)
        {
            if (element != null)
            {
                element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static Color Parse(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private sealed class Cloud
        {
            public VisualElement Element;
            public float X;
            public float Y;
            public float Width;
            public float Speed;
        }

        private sealed class Drop
        {
            public VisualElement Element;
            public float X;
            public float Y;
            public float Speed;
            public float Sway;
            public float Phase;
        }
    }
}
