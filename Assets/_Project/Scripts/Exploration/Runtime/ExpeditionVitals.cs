using ApoGirl.Exploration.Model;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 화면의 주인공 상태판 (사용자 요청 2026-11-26: 글자·숫자 대신 한눈에 읽히게).
    /// 왼쪽 아래: 체력·스태미너·무게 게이지(아이콘 + 막대)와 소리 파동 모양의 소음 계기, 어두운 곳의 손전등 표시.
    /// 오른쪽 위 시계: 해·달 아이콘과 좀비 활동성 눈금. 모양은 Expedition.uss의 "상태판" 절.
    /// </summary>
    internal sealed class ExpeditionVitalsView
    {
        private readonly ExpeditionDirector _director;
        private readonly VisualElement _panel;
        private readonly Gauge _hp;
        private readonly Gauge _stamina;
        private readonly Gauge _weight;
        private readonly NoiseMeter _noise;
        private readonly Label _noiseWord;
        private readonly VisualElement _light;
        private readonly VisualElement _lightIcon;
        private readonly Label _lightWord;
        private readonly VisualElement _clockIcon;
        private readonly VisualElement _activity;
        private readonly VisualElement[] _pips = new VisualElement[4];
        private readonly Label _activityWord;
        private string _bagShown;
        private string _lightShown;
        private bool? _nightShown;

        /// <summary>status: 왼쪽 아래 상태 칸(.exp-status), clock: 오른쪽 위 시계 칸(.exp-clock), activityLabel: 예전 활동성 글줄 (자리를 물려받고 숨긴다).</summary>
        public ExpeditionVitalsView(ExpeditionDirector director, VisualElement status, VisualElement clockRow, Label activityLabel)
        {
            _director = director;

            _panel = new VisualElement { name = "exp-vitals", pickingMode = PickingMode.Ignore };
            _panel.AddToClassList("exp-vitals");
            var gauges = new VisualElement { pickingMode = PickingMode.Ignore };
            gauges.AddToClassList("exp-gauges");
            _hp = new Gauge("hp", StoryArt.Icon("vital.Hp"));
            _stamina = new Gauge("stamina", StoryArt.Icon("stat.Stamina"));
            _weight = new Gauge("weight", StoryArt.Icon("exp.bag"));
            gauges.Add(_hp.Root);
            gauges.Add(_stamina.Root);
            gauges.Add(_weight.Root);
            _panel.Add(gauges);

            // 소음 계기 + 손전등
            var side = new VisualElement { pickingMode = PickingMode.Ignore };
            side.AddToClassList("exp-senses");
            _noise = new NoiseMeter();
            _noise.AddToClassList("exp-noise-meter");
            _noiseWord = new Label(Loc.T("exp.noise")) { pickingMode = PickingMode.Ignore };
            _noiseWord.AddToClassList("exp-noise-word");
            side.Add(_noise);
            side.Add(_noiseWord);
            _light = new VisualElement { pickingMode = PickingMode.Ignore };
            _light.AddToClassList("exp-light");
            _lightIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            _lightIcon.AddToClassList("exp-light-icon");
            _lightWord = new Label { pickingMode = PickingMode.Ignore };
            _lightWord.AddToClassList("exp-light-word");
            _light.Add(_lightIcon);
            _light.Add(_lightWord);
            side.Add(_light);
            _panel.Add(side);
            status.Insert(0, _panel);

            // 시계: 해·달 아이콘
            _clockIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            _clockIcon.AddToClassList("exp-clock-icon");
            clockRow.Insert(0, _clockIcon);

            // 좀비 활동성: 아이콘 + 눈금 넷 + 낱말
            _activity = new VisualElement { pickingMode = PickingMode.Ignore };
            _activity.AddToClassList("exp-activity-row");
            var threat = new VisualElement { pickingMode = PickingMode.Ignore };
            threat.AddToClassList("exp-activity-icon");
            StoryArt.SetContain(threat, StoryArt.Icon("exp.threat"));
            _activity.Add(threat);
            for (var i = 0; i < _pips.Length; i++)
            {
                _pips[i] = new VisualElement { pickingMode = PickingMode.Ignore };
                _pips[i].AddToClassList("exp-activity-pip");
                _activity.Add(_pips[i]);
            }

            _activityWord = new Label { pickingMode = PickingMode.Ignore };
            _activityWord.AddToClassList("exp-activity-word");
            _activity.Add(_activityWord);
            var parent = activityLabel.parent;
            parent.Insert(parent.IndexOf(activityLabel) + 1, _activity);
            activityLabel.AddToClassList("hidden");
        }

        public void Refresh()
        {
            var balance = _director.Definitions.Balance;

            // 체력: 막대 + 숫자. 30 이하는 붉게 깜박인다
            var hpMax = Mathf.Max(1, balance.VitalMax);
            _hp.Set(_director.Hp / (float)hpMax, _director.Hp.ToString());
            _hp.Root.EnableInClassList("warn", _director.Hp <= 50 && _director.Hp > 30);
            _hp.Root.EnableInClassList("danger", _director.Hp <= 30);

            // 스태미너: 초록 = 지금, 붉은 빗금 = 피로·부상으로 깎인 최대치
            var max = Mathf.Max(1f, _director.StaminaMax);
            _stamina.Set(_director.Stamina / max, null, 1f - Mathf.Clamp01(_director.StaminaCap / max));
            _stamina.Root.EnableInClassList("warn", _director.Exhausted);

            // 무게: 가방 그림 + 막대, 넘치면 붉게
            var bag = _director.BackpackItem?.Id ?? "";
            if (bag != _bagShown)
            {
                _bagShown = bag;
                _weight.SetIcon(bag.Length > 0 ? StoryArt.ItemIcon(bag) ?? StoryArt.Icon("exp.bag") : StoryArt.Icon("exp.bag"));
            }

            var capacity = Mathf.Max(0.1f, _director.CarryCapacity);
            var carried = _director.Backpack.TotalWeight;
            _weight.Set(carried / capacity, carried.ToString("0.#") + "/" + _director.CarryCapacity.ToString("0.#"));
            _weight.Root.EnableInClassList("danger", _director.IsOverweight);

            // 소음: 파동 넷 (웅크림 1 · 걷기 2 · 문·수색·밀치기 3 · 달리기·쇠지렛대 4)
            var noise = _director.NoiseLevel;
            var level = noise >= 9f ? 4 : noise >= 5f ? 3 : noise >= 2.5f ? 2 : noise > 0.05f ? 1 : 0;
            _noise.Level = level;
            _noiseWord.EnableInClassList("loud", level >= 3);

            // 손전등: 어두운 곳에서만
            var dark = _director.DarkHere;
            _light.EnableInClassList("hidden", !dark);
            if (dark)
            {
                var on = _director.FlashlightOn;
                _light.EnableInClassList("on", on);
                _lightWord.text = Loc.T(on ? "exp.light_on" : "exp.light_off");
                var tool = LightToolId();
                if (tool != _lightShown)
                {
                    _lightShown = tool;
                    StoryArt.SetContain(_lightIcon, (tool.Length > 0 ? StoryArt.ItemIcon(tool) : null) ?? StoryArt.Icon("exp.light"));
                }
            }

            // 시계 아이콘: 낮에는 해, 황혼부터는 달
            var phase = _director.Clock.Phase;
            var night = phase == ClockPhase.Dusk || phase == ClockPhase.Evening || phase == ClockPhase.Midnight || phase == ClockPhase.Dawn;
            if (_nightShown != night)
            {
                _nightShown = night;
                StoryArt.SetContain(_clockIcon, StoryArt.Icon(night ? "exp.night" : "exp.day"));
            }

            // 좀비 활동성: 휴면 1칸 ~ 사냥 4칸
            var activity = (int)_director.Activity.Level;
            for (var i = 0; i < _pips.Length; i++)
            {
                _pips[i].EnableInClassList("on", i <= activity);
            }

            _activity.EnableInClassList("alert", _director.Activity.Level == ActivityLevel.Alert);
            _activity.EnableInClassList("hunting", _director.Activity.Level == ActivityLevel.Hunting);
            _activityWord.text = Loc.T("activity." + _director.Activity.Level);
        }

        private string LightToolId()
        {
            for (var i = 0; i < 2; i++)
            {
                var tool = _director.ToolAt(i);
                if (tool != null && tool.UsesPower)
                {
                    return tool.Id;
                }
            }

            return "";
        }

        /// <summary>아이콘 + 막대 + (숫자). 막대 오른쪽 끝의 붉은 칸은 깎인 최대치.</summary>
        private sealed class Gauge
        {
            public readonly VisualElement Root;
            private readonly VisualElement _icon;
            private readonly VisualElement _fill;
            private readonly VisualElement _lost;
            private readonly Label _value;
            private float _shown = -1f;
            private float _lostShown = -1f;
            private string _text;

            public Gauge(string kind, Texture2D icon)
            {
                Root = new VisualElement { pickingMode = PickingMode.Ignore };
                Root.AddToClassList("exp-gauge");
                Root.AddToClassList("exp-gauge-" + kind);
                _icon = new VisualElement { pickingMode = PickingMode.Ignore };
                _icon.AddToClassList("exp-gauge-icon");
                StoryArt.SetContain(_icon, icon);
                var track = new VisualElement { pickingMode = PickingMode.Ignore };
                track.AddToClassList("exp-gauge-track");
                _fill = new VisualElement { pickingMode = PickingMode.Ignore };
                _fill.AddToClassList("exp-gauge-fill");
                _lost = new VisualElement { pickingMode = PickingMode.Ignore };
                _lost.AddToClassList("exp-gauge-lost");
                track.Add(_fill);
                track.Add(_lost);
                // 눈금: 4등분 선 (막대가 얼마나 찼는지 어림하기 쉽게)
                for (var i = 1; i < 4; i++)
                {
                    var tick = new VisualElement { pickingMode = PickingMode.Ignore };
                    tick.AddToClassList("exp-gauge-tick");
                    tick.style.left = Length.Percent(i * 25f);
                    track.Add(tick);
                }

                _value = new Label { pickingMode = PickingMode.Ignore };
                _value.AddToClassList("exp-gauge-value");
                Root.Add(_icon);
                Root.Add(track);
                Root.Add(_value);
            }

            public void SetIcon(Texture2D icon) => StoryArt.SetContain(_icon, icon);

            public void Set(float ratio, string text, float lost = 0f)
            {
                ratio = Mathf.Clamp01(ratio);
                if (Mathf.Abs(ratio - _shown) > 0.002f)
                {
                    _shown = ratio;
                    _fill.style.width = Length.Percent(ratio * 100f);
                }

                lost = Mathf.Clamp01(lost);
                if (Mathf.Abs(lost - _lostShown) > 0.002f)
                {
                    _lostShown = lost;
                    _lost.style.width = Length.Percent(lost * 100f);
                    _lost.EnableInClassList("hidden", lost < 0.001f);
                }

                if (text != _text)
                {
                    _text = text;
                    _value.text = text ?? "";
                    _value.EnableInClassList("hidden", string.IsNullOrEmpty(text));
                }
            }
        }

        /// <summary>소음 계기: 왼쪽 점에서 오른쪽으로 퍼지는 파동 넷. 소리가 클수록 바깥 파동까지 켜지고 색이 초록 → 노랑 → 주황 → 빨강으로 바뀐다.</summary>
        private sealed class NoiseMeter : VisualElement
        {
            private static readonly Color Off = new Color(1f, 1f, 1f, 0.16f);
            private static readonly Color[] Tones =
            {
                new Color(0.56f, 0.82f, 0.54f), new Color(0.94f, 0.81f, 0.42f), new Color(0.89f, 0.59f, 0.24f), new Color(0.85f, 0.33f, 0.31f),
            };

            private int _level = -1;

            public NoiseMeter()
            {
                pickingMode = PickingMode.Ignore;
                generateVisualContent += Draw;
            }

            public int Level
            {
                set
                {
                    value = Mathf.Clamp(value, 0, 4);
                    if (value != _level)
                    {
                        _level = value;
                        MarkDirtyRepaint();
                    }
                }
            }

            private void Draw(MeshGenerationContext context)
            {
                var rect = contentRect;
                if (rect.width < 8f || rect.height < 8f)
                {
                    return;
                }

                var painter = context.painter2D;
                var origin = new Vector2(rect.xMin + 7f, rect.center.y);
                var tone = _level > 0 ? Tones[_level - 1] : Off;

                // 소리 나는 자리: 작은 점
                painter.fillColor = _level > 0 ? tone : new Color(1f, 1f, 1f, 0.4f);
                painter.BeginPath();
                painter.Arc(origin, 3.5f, 0f, 360f);
                painter.Fill();

                // 파동 넷
                // 가장 바깥 파동이 위아래로 넘치지 않게: 반지름 × sin(펼친 각) ≤ 높이의 절반
                var step = Mathf.Min((rect.width - 16f) / 4f, (rect.height * 0.5f - 3f) / Mathf.Sin(42f * Mathf.Deg2Rad) / 4f);
                painter.lineCap = LineCap.Round;
                for (var i = 0; i < 4; i++)
                {
                    painter.strokeColor = i < _level ? tone : Off;
                    painter.lineWidth = i < _level ? 4f : 3f;
                    painter.BeginPath();
                    painter.Arc(origin, step * (i + 1) + 2f, -42f, 42f);
                    painter.Stroke();
                }
            }
        }
    }
}
