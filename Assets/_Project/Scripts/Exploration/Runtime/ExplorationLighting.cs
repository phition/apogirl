using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 공간 조명 (M6-5): 낮에는 복도 앞쪽 창으로 햇빛 줄기가 비스듬히 들고, 전기가 끊긴 방 안은 어둡다
    /// (복도 쪽 창으로 빛이 조금 샌다). 그림만 바꾼다 — 시야·수색 규칙은 지금처럼 밤·황혼(IsDark)만 따른다.
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

        /// <summary>햇빛 줄기: 천장에서 바닥으로 비스듬히 (화면 앞쪽 창 → 뒤 벽·바닥).</summary>
        private const float BeamWidth = 1.5f;
        private const float BeamSlant = 1.8f;
        private const float BeamSpread = 1.35f;

        private static readonly Color DayBeam = new Color(1f, 0.88f, 0.66f);
        private static readonly Color DuskBeam = new Color(1f, 0.5f, 0.38f);
        private static readonly Color NightBeam = new Color(0.55f, 0.65f, 1f);
        private static readonly Color DawnBeam = new Color(0.9f, 0.8f, 0.95f);
        private static readonly Color RoomLeak = new Color(0.9f, 0.93f, 1f);

        private readonly Transform _world;
        private readonly BalanceConfig _balance;
        private readonly float _floorHeight;
        private readonly List<Light2D> _beams = new List<Light2D>();
        private readonly List<Light2D> _leaks = new List<Light2D>();
        private Phase _phase = Phase.Day;

        public ExplorationLighting(Transform world, BalanceConfig balance, float floorHeight, Phase phase)
        {
            _world = world;
            _balance = balance;
            _floorHeight = floorHeight;
            _phase = phase;
        }

        /// <summary>복도 한 층: 일정 간격으로 햇빛 줄기.</summary>
        public void AddCorridor(ExplorationArea corridor)
        {
            var spacing = Mathf.Max(2.5f, _balance.SunbeamSpacing);
            var top = _floorHeight - 0.2f;
            for (var x = spacing * 0.6f; x < corridor.Width - 1f; x += spacing)
            {
                var path = new[]
                {
                    new Vector3(x, top),
                    new Vector3(x + BeamWidth, top),
                    new Vector3(x + BeamWidth * BeamSpread - BeamSlant, 0.05f),
                    new Vector3(x - BeamSlant, 0.05f),
                };
                _beams.Add(Freeform("Sunbeam", corridor.Origin, path, 0.55f));
            }

            Apply();
        }

        /// <summary>방 (처음 들어갈 때 만든다): 복도 쪽 창마다 바닥으로 번지는 희미한 빛.</summary>
        public void AddRoom(ExplorationArea room)
        {
            if (room.Template == null)
            {
                return;
            }

            var top = BuildingView.WindowBottom + BuildingView.WindowHeight;
            foreach (var window in room.Template.Windows)
            {
                var path = new[]
                {
                    new Vector3(window.X, top),
                    new Vector3(window.X + window.Width, top),
                    new Vector3(window.X + window.Width + 0.9f, 0.05f),
                    new Vector3(window.X - 0.5f, 0.05f),
                };
                _leaks.Add(Freeform("WindowLeak", room.Origin, path, 0.8f));
            }

            Apply();
        }

        public void SetPhase(Phase phase)
        {
            if (phase == _phase)
            {
                return;
            }

            _phase = phase;
            Apply();
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
                    return area.IsRoom ? _balance.DayRoomAmbient : _balance.DayCorridorAmbient;
                case Phase.Dawn:
                    return area.IsRoom ? Mathf.Min(1f, _balance.DayRoomAmbient * 2f) : 1f;
                default:
                    return 1f;
            }
        }

        /// <summary>낮에도 어두운 방 안이면 주인공 주변을 희미하게 밝힌다.</summary>
        public bool IsDimRoom(ExplorationArea area) => area != null && area.IsRoom && (_phase == Phase.Day || _phase == Phase.Dawn);

        private void Apply()
        {
            Color beamColor;
            float beamScale;
            switch (_phase)
            {
                case Phase.Dusk:
                    beamColor = DuskBeam;
                    beamScale = 0.45f;
                    break;
                case Phase.Night:
                    beamColor = NightBeam;
                    beamScale = 0.3f;
                    break;
                case Phase.Dawn:
                    beamColor = DawnBeam;
                    beamScale = 0.6f;
                    break;
                default:
                    beamColor = DayBeam;
                    beamScale = 1f;
                    break;
            }

            foreach (var beam in _beams)
            {
                beam.color = beamColor;
                beam.intensity = _balance.SunbeamIntensity * beamScale;
            }

            foreach (var leak in _leaks)
            {
                leak.color = _phase == Phase.Day || _phase == Phase.Dawn ? RoomLeak : beamColor;
                leak.intensity = _balance.RoomWindowLightIntensity * beamScale;
            }
        }

        private Light2D Freeform(string name, Vector2 origin, Vector3[] path, float falloff)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_world, false);
            go.transform.localPosition = new Vector3(origin.x, origin.y, 0f);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Freeform;
            light.SetShapePath(path);
            light.shapeLightFalloffSize = falloff;
            return light;
        }
    }
}
