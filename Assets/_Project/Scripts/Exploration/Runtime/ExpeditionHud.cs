using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Exploration.Model;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 HUD (문서 10장, M2b-1 범위): 시계, 위치, 상태, 무게, 상호작용 안내·수색 진행, 배낭 격자, 컨테이너 내용.
    /// 활동성·각성·소음 게이지는 M2b-2에서 추가.
    /// </summary>
    public sealed class ExpeditionHud
    {
        private const float CellSize = 54f;

        private readonly ExpeditionDirector _director;
        private readonly ProgressBar _clockBar;
        private readonly Label _clockPhase;
        private readonly Label _clockTime;
        private readonly Label _location;
        private readonly Label _room;
        private readonly Label _vitals;
        private readonly Label _weight;
        private readonly Label _prompt;
        private readonly ProgressBar _searchBar;
        private readonly VisualElement _backpackPanel;
        private readonly VisualElement _grid;
        private readonly VisualElement _lootPanel;
        private readonly Label _lootTitle;
        private readonly VisualElement _lootList;
        private readonly Label _message;
        private readonly Label _debug;
        private readonly Label _activity;
        private readonly Label _noise;
        private readonly Label _hidden;
        private readonly ProgressBar _stirBar;
        private readonly VisualElement _worldLabels;
        private readonly VisualElement _root;
        private readonly Label _equip;
        private readonly Label _tutorial;
        private readonly VisualElement _encounterPanel;
        private readonly Label _encounterText;
        private readonly VisualElement _encounterChoices;
        private ExplorationTarget _shownEncounter;
        private readonly List<Label> _sleepLabels = new List<Label>();
        private PlacedItem _selected;
        private int _gridVersion = -1;
        private ExplorationTarget _shownContainer;
        private int _shownContentCount = -1;

        public ExpeditionHud(VisualElement root, ExpeditionDirector director)
        {
            _director = director;
            KoreanFont.Apply(root);
            _clockBar = root.Q<ProgressBar>("clock-bar");
            _clockPhase = root.Q<Label>("clock-phase");
            _clockTime = root.Q<Label>("clock-time");
            _location = root.Q<Label>("location-label");
            _room = root.Q<Label>("room-label");
            _vitals = root.Q<Label>("vitals-label");
            _weight = root.Q<Label>("weight-label");
            _prompt = root.Q<Label>("prompt-label");
            _searchBar = root.Q<ProgressBar>("search-bar");
            _backpackPanel = root.Q("backpack-panel");
            _grid = root.Q("backpack-grid");
            _lootPanel = root.Q("loot-panel");
            _lootTitle = root.Q<Label>("loot-title");
            _lootList = root.Q("loot-list");
            _message = root.Q<Label>("message");
            _debug = root.Q<Label>("debug-label");
            _activity = root.Q<Label>("activity-label");
            _noise = root.Q<Label>("noise-label");
            _hidden = root.Q<Label>("hidden-label");
            _stirBar = root.Q<ProgressBar>("stir-bar");
            _worldLabels = root.Q("world-labels");
            _root = root;
            Presentation.Audio.Sound.AttachUiClicks(root);
            _equip = root.Q<Label>("equip-label");
            _tutorial = root.Q<Label>("tutorial-label");
            _encounterPanel = root.Q("encounter-panel");
            _encounterText = root.Q<Label>("encounter-text");
            _encounterChoices = root.Q("encounter-choices");

            root.Q<Label>("backpack-title").text = Loc.T("exp.backpack");
            root.Q<Label>("controls-label").text = Loc.T("exp.controls");
            root.Q<Button>("loot-take-all").text = Loc.T("exp.take_all");
            root.Q<Button>("loot-take-all").clicked += () => _director.TakeAll();
            root.Q<Button>("loot-close").text = Loc.T("common.close");
            root.Q<Button>("loot-close").clicked += () => _director.CloseContainer();

            var time = _director.Request.TimeOfDay == ExpeditionTime.Day ? "exp.time_day" : "exp.time_night";
            _location.text = Loc.T("location." + _director.Location.Id) + " · " + Loc.T(time);
            _grid.style.width = _director.Backpack.Width * CellSize;
            _grid.style.height = _director.Backpack.Height * CellSize;
            Refresh();
        }

        public void ToggleBackpack()
        {
            _backpackPanel.ToggleInClassList("hidden");
        }

        public void ShowMessage(string key)
        {
            ShowText(Loc.T(key));
        }

        /// <summary>이미 조합한 문장을 잠시 보여준다.</summary>
        public void ShowText(string text, int milliseconds = 1600)
        {
            if (_holdMessage)
            {
                return;
            }

            _message.text = text;
            _message.RemoveFromClassList("hidden");
            var token = ++_messageToken;
            _message.schedule.Execute(() =>
            {
                if (token == _messageToken && !_holdMessage)
                {
                    _message.AddToClassList("hidden");
                }
            }).ExecuteLater(milliseconds);
        }

        /// <summary>문장을 런이 끝날 때까지 띄워 둔다 (생존자 대치 암전 중). 다른 메시지가 덮지 않는다.</summary>
        public void ShowHold(string text)
        {
            _holdMessage = true;
            _messageToken++;
            _message.text = text;
            _message.RemoveFromClassList("hidden");
        }

        private int _messageToken;
        private bool _holdMessage;

        /// <summary>단독 실행 종료 표시.</summary>
        public void ShowEnd(ExpeditionResult result)
        {
            var loot = string.Join("  ", result.ResourceLoot.Select(r => UiText.Resource(r.Resource) + " +" + r.Amount)
                .Concat(result.Loot.Select(i => Loc.Item(i.ItemId) + " ×" + i.Count)));
            var status = result.Hostile != HostileOutcome.None ? Loc.T("result.hostile_" + result.Hostile) : Loc.T("exp.status." + result.Status);
            _message.text = status + (loot.Length > 0 ? "\n" + loot : "");
            _message.RemoveFromClassList("hidden");
        }

        public void Refresh()
        {
            var clock = _director.Clock;
            _clockBar.value = clock.Normalized * 100f;
            _clockPhase.text = Loc.T("clock." + clock.Phase);
            var remaining = Mathf.CeilToInt(clock.Remaining);
            _clockTime.text = $"{remaining / 60}:{remaining % 60:00}";
            _clockBar.EnableInClassList("late", clock.Phase == ClockPhase.Dusk || clock.Phase == ClockPhase.Dawn);

            _room.text = _director.LocationLabel;
            _vitals.text = Loc.F("exp.vitals", _director.Hp);
            _vitals.EnableInClassList("danger", _director.Hp <= 30);
            _activity.text = Loc.F("exp.activity", Loc.T("activity." + _director.Activity.Level));
            _activity.EnableInClassList("danger", _director.Activity.Level >= ActivityLevel.Alert);
            var noiseBars = Mathf.Clamp(Mathf.CeilToInt(_director.NoiseLevel / 1.8f), 0, 4);
            _noise.text = Loc.T("exp.noise") + " " + new string('■', noiseBars) + new string('□', 4 - noiseBars)
                          + (_director.DarkHere ? "   " + Loc.T(_director.FlashlightOn ? "exp.light_on" : "exp.light_off") : "");
            _hidden.EnableInClassList("hidden", !_director.Hidden);
            _hidden.text = Loc.T("exp.hidden");

            var crowd = _director.Threat.NearestDormantCrowd(_director.Snapshot, 10f);
            _stirBar.EnableInClassList("hidden", crowd == null);
            if (crowd != null)
            {
                var balance = _director.Definitions.Balance;
                _stirBar.value = crowd.Stirring / balance.StirThreshold * 100f;
                _stirBar.title = Loc.T("exp.stirring");
            }
            _weight.text = Loc.F("exp.weight", _director.Backpack.TotalWeight.ToString("0.0"), _director.CarryCapacity.ToString("0.0"));
            _weight.EnableInClassList("danger", _director.IsOverweight);
            var debug = _director.TimeScale > 1f ? "×" + _director.TimeScale.ToString("0") : "";
            if (_director.Invincible) debug += " " + Loc.T("exp.debug_invincible");
            if (_director.FullVision) debug += " " + Loc.T("exp.debug_vision");
            if (_director.DebugOverlay) debug += " F3";
            _debug.text = debug.Trim();

            _equip.text = EquipmentText();
            var hint = _director.TutorialHintKey;
            _tutorial.EnableInClassList("hidden", hint == null);
            if (hint != null)
            {
                _tutorial.text = Loc.T(hint);
            }

            RefreshEncounter();
            RefreshPrompt();
            RefreshLoot();
            RefreshGrid();
            RefreshSleepLabels();
        }

        private string EquipmentText()
        {
            var weapon = _director.Weapon != null
                ? Loc.F("exp.equip_weapon", Loc.Item(_director.Weapon.Id), _director.WeaponDurabilityLeft)
                : Loc.T("exp.equip_bare");
            var tool = _director.ToolKind == ToolKind.None ? "" : "  ·  " + Loc.T("tool." + _director.ToolKind);
            var quick = "";
            for (var i = 0; i < _director.QuickSlots.Count; i++)
            {
                quick += $"  [{i + 1}] " + Loc.Item(_director.QuickSlots[i]);
            }

            return weapon + tool + quick;
        }

        private void RefreshEncounter()
        {
            var target = _director.OpenEncounter;
            // 그 자리 대화 중에는 대화 창이 대신한다
            _encounterPanel.EnableInClassList("hidden", target == null || _director.InFieldDialogue);
            if (_director.InFieldDialogue)
            {
                return;
            }

            if (target == null || target == _shownEncounter)
            {
                _shownEncounter = target;
                return;
            }

            _shownEncounter = target;
            _encounterText.text = Loc.T(target.Encounter.PromptKey);
            _encounterChoices.Clear();
            for (var i = 0; i < target.Encounter.Choices.Count; i++)
            {
                var index = i;
                var button = new Button(() => _director.ChooseEncounter(index)) { text = Loc.T(target.Encounter.Choices[i].TextKey) };
                button.AddToClassList("exp-encounter-choice");
                _encounterChoices.Add(button);
            }
        }

        private void RefreshPrompt()
        {
            var target = _director.Nearest;
            _searchBar.AddToClassList("hidden");
            if (_director.Hidden)
            {
                _prompt.text = Loc.T("exp.prompt_unhide");
                return;
            }

            if (_director.PeekTarget != null)
            {
                _prompt.text = Loc.T(_director.PeekTooDark ? "exp.prompt_peeking_dark" : "exp.prompt_peeking");
                return;
            }

            if (_director.ListenProgress > 0f && _director.ListenProgress < 1f)
            {
                _prompt.text = Loc.T("exp.prompt_listening");
                _searchBar.RemoveFromClassList("hidden");
                _searchBar.value = _director.ListenProgress * 100f;
                return;
            }

            if (target == null)
            {
                _prompt.text = "";
                return;
            }

            switch (target.Kind)
            {
                case TargetKind.Container:
                    var name = ContainerName(target.Container);
                    var canHide = target.Container.Kind == ContainerKind.Locker || target.Container.Kind == ContainerKind.Desk;
                    var hideHint = canHide ? "  ·  " + Loc.T("exp.prompt_hide") : "";
                    if (target.Searched)
                    {
                        _prompt.text = Loc.F(target.Contents.Count > 0 ? "exp.prompt_open" : "exp.prompt_empty", name) + hideHint;
                    }
                    else
                    {
                        _prompt.text = Loc.F("exp.prompt_search", name) + hideHint;
                        if (target.SearchProgress > 0f)
                        {
                            _searchBar.RemoveFromClassList("hidden");
                            _searchBar.value = target.SearchProgress / target.SearchSeconds * 100f;
                        }
                    }

                    break;
                case TargetKind.Connector when target.Connector.Collapsed:
                    _prompt.text = Loc.T("exp.prompt_stairs_collapsed");
                    break;
                case TargetKind.Passage:
                    _prompt.text = Loc.T("exp.prompt_enter_building");
                    break;
                case TargetKind.Exit when !target.Locked && target.LinkArea != null:
                    _prompt.text = Loc.F("exp.prompt_go_outside", target.LinkArea.Label);
                    break;
                case TargetKind.Connector:
                    _prompt.text = Loc.F(target.Connector.TargetFloor > target.Floor ? "exp.prompt_up" : "exp.prompt_down", target.Connector.TargetFloor + 1);
                    break;
                case TargetKind.Exit:
                    _prompt.text = Loc.T(!target.Locked ? "exp.prompt_exit"
                        : _director.ToolKind == ToolKind.Crowbar ? "exp.prompt_exit_force" : "exp.prompt_exit_locked");
                    break;
                case TargetKind.Door when target.RoomLocked:
                    _prompt.text = Loc.F("exp.prompt_door_shuttered", target.Room.Label) + "  ·  " + Loc.T("exp.prompt_door_senses");
                    break;
                case TargetKind.Door when target.Forceable && !target.Forced:
                    _prompt.text = Loc.F(_director.ToolKind == ToolKind.Crowbar ? "exp.prompt_door_force" : "exp.prompt_door_locked", target.Room.Label)
                                   + "  ·  " + Loc.T("exp.prompt_door_senses");
                    break;
                case TargetKind.Door:
                    _prompt.text = Loc.F(target.Room.Visited ? "exp.prompt_enter_again" : "exp.prompt_enter", target.Room.Label)
                                   + "  ·  " + Loc.T("exp.prompt_door_senses");
                    break;
                case TargetKind.RoomExit:
                    _prompt.text = Loc.T("exp.prompt_leave") + "  ·  " + Loc.T("exp.prompt_door_senses");
                    break;
                case TargetKind.Encounter:
                    _prompt.text = Loc.T("exp.prompt_encounter");
                    break;
            }
        }

        private void RefreshLoot()
        {
            var container = _director.OpenContainer;
            _lootPanel.EnableInClassList("hidden", container == null);
            if (container == null)
            {
                _shownContainer = null;
                return;
            }

            if (container == _shownContainer && container.Contents.Count == _shownContentCount)
            {
                return;
            }

            _shownContainer = container;
            _shownContentCount = container.Contents.Count;
            _lootTitle.text = ContainerName(container.Container);
            _lootList.Clear();
            if (container.Contents.Count == 0)
            {
                _lootList.Add(new Label(Loc.T("exp.container_empty")));
                return;
            }

            for (var i = 0; i < container.Contents.Count; i++)
            {
                var index = i;
                var item = _director.Definitions.GetItemOrNull(container.Contents[i].ItemId);
                var row = new Button(() => _director.TakeItem(index));
                row.AddToClassList("loot-row");
                Presentation.Art.StoryArt.SetIcon(row, Presentation.Art.StoryArt.ItemIcon(container.Contents[i].ItemId), 26f);
                row.text = Loc.Item(container.Contents[i].ItemId) + (item != null ? $"   {item.Width}×{item.Height} · {item.Weight:0.0}kg" : "");
                _lootList.Add(row);
            }
        }

        private void RefreshGrid()
        {
            var backpack = _director.Backpack;
            var version = backpack.Items.Aggregate(backpack.Items.Count, (hash, i) => hash * 31 + i.X * 7 + i.Y * 13 + i.ItemId.GetHashCode());
            if (version == _gridVersion)
            {
                return;
            }

            _gridVersion = version;
            _grid.Clear();
            for (var y = 0; y < backpack.Height; y++)
            {
                for (var x = 0; x < backpack.Width; x++)
                {
                    var cellX = x;
                    var cellY = y;
                    var cell = new VisualElement();
                    cell.AddToClassList("bp-cell");
                    Place(cell, x, y, 1, 1);
                    cell.RegisterCallback<PointerDownEvent>(_ => OnCellClicked(cellX, cellY));
                    _grid.Add(cell);
                }
            }

            foreach (var placed in backpack.Items)
            {
                var item = placed;
                var element = new Label(Loc.Item(placed.ItemId));
                element.AddToClassList("bp-item");

                // 아이템 그림 (M6-5): 칸을 그림으로 채우고 이름은 아래에 작게
                var icon = Presentation.Art.StoryArt.ItemIcon(placed.ItemId);
                if (icon != null)
                {
                    Presentation.Art.StoryArt.SetContain(element, icon);
                    element.AddToClassList("with-item-icon");
                }
                element.EnableInClassList("selected", placed == _selected);
                Place(element, placed.X, placed.Y, placed.Width, placed.Height);
                element.RegisterCallback<PointerDownEvent>(evt =>
                {
                    evt.StopPropagation();
                    if (evt.button == 1)
                    {
                        _director.DropItem(item);
                        _selected = null;
                    }
                    else
                    {
                        _selected = _selected == item ? null : item;
                    }

                    _gridVersion = -1;
                });
                _grid.Add(element);
            }
        }

        /// <summary>
        /// 잠든 좀비 머리 위 "Zzz". 군집 각성 게이지가 오를수록 주황 → 빨강으로 바뀌어 위험을 알린다.
        /// </summary>
        private void RefreshSleepLabels()
        {
            var camera = _director.ViewCamera;
            var panel = _root.panel;
            var used = 0;
            if (camera != null && panel != null && _director.ScreenFade < 0.5f)
            {
                var threshold = _director.Definitions.Balance.StirThreshold;
                var phase = (int)(Time.time / 0.5f);
                foreach (var unit in _director.Threat.Zombies)
                {
                    var dormant = unit.State == ZombieState.Dormant;
                    if ((!dormant && !_director.DebugOverlay) || !_director.IsVisibleToPlayer(unit))
                    {
                        continue;
                    }

                    var world = _director.HeadPosition(unit);
                    var viewport = camera.WorldToViewportPoint(world);
                    if (viewport.z < 0f || viewport.x < -0.05f || viewport.x > 1.05f || viewport.y < -0.05f || viewport.y > 1.05f)
                    {
                        continue;
                    }

                    var label = Acquire(used++);
                    var position = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, camera);
                    label.style.left = position.x - 14f;
                    label.style.top = position.y - 24f;
                    if (!dormant)
                    {
                        label.text = unit.State + " " + unit.Awareness.ToString("0.0");
                        label.style.color = new Color(0.75f, 0.75f, 1f);
                        continue;
                    }

                    label.text = SleepText(phase + unit.Id);

                    var stir = 0f;
                    if (unit.CrowdId >= 0 && unit.CrowdId < _director.Threat.Crowds.Count)
                    {
                        stir = _director.Threat.Crowds[unit.CrowdId].Stirring / threshold;
                    }

                    label.style.color = stir < 0.4f ? new Color(0.7f, 0.8f, 0.9f)
                        : stir < 0.75f ? new Color(0.95f, 0.7f, 0.3f)
                        : new Color(0.95f, 0.35f, 0.3f);
                }
            }

            for (var i = 0; i < _sleepLabels.Count; i++)
            {
                _sleepLabels[i].style.display = i < used ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private Label Acquire(int index)
        {
            while (_sleepLabels.Count <= index)
            {
                var label = new Label { pickingMode = PickingMode.Ignore };
                label.AddToClassList("zzz");
                _worldLabels.Add(label);
                _sleepLabels.Add(label);
            }

            return _sleepLabels[index];
        }

        private static string SleepText(int step)
        {
            switch (((step % 3) + 3) % 3)
            {
                case 0: return "z";
                case 1: return "zZ";
                default: return "zZz";
            }
        }

        private void OnCellClicked(int x, int y)
        {
            if (_selected != null && _director.Backpack.TryMove(_selected, x, y))
            {
                _selected = null;
            }

            _gridVersion = -1;
        }

        private static void Place(VisualElement element, int x, int y, int width, int height)
        {
            element.style.position = Position.Absolute;
            element.style.left = x * CellSize;
            element.style.top = y * CellSize;
            element.style.width = width * CellSize - 2f;
            element.style.height = height * CellSize - 2f;
        }

        /// <summary>상자 이름: 방별 그림(SpriteKey)에 맞는 이름이 있으면 그것 (침대·실험대 등), 없으면 종류 이름.</summary>
        private static string ContainerName(ContainerDef container)
        {
            if (!string.IsNullOrEmpty(container.SpriteKey))
            {
                var special = Loc.Lookup(Loc.UiTable, "container." + container.SpriteKey);
                if (!string.IsNullOrEmpty(special))
                {
                    return special;
                }
            }

            return Loc.T("container." + container.Kind);
        }
    }

    internal static class DefinitionLookupExtensions
    {
        public static Core.Definitions.ItemDef GetItemOrNull(this Core.Definitions.IDefinitionDatabase db, string id)
        {
            return db.TryGetItem(id, out var item) ? item : null;
        }
    }
}
