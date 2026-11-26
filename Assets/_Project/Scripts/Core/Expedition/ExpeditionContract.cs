using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Expedition
{
    // 허브(시뮬레이션 코어) ↔ 탐험 모드 데이터 계약 (문서 11.2.1). 순수 데이터로만 통신한다.

    public enum ExpeditionTime
    {
        Day,
        Night,
    }

    public enum ExtractStatus
    {
        Extracted,
        Stranded,
        Down,
    }

    [Flags]
    public enum InjuryFlags
    {
        None = 0,
        Bleeding = 1,
        Sprain = 2,
        Fracture = 4,
        Scratch = 8,
        Bite = 16,
    }

    public sealed class ExpeditionRequest
    {
        public string LocationId = "";
        public int Seed;
        public LoadoutState Loadout = new LoadoutState();
        public Dictionary<StatType, int> Stats = new Dictionary<StatType, int>();
        public Dictionary<ProficiencyType, int> ProficiencyLevels = new Dictionary<ProficiencyType, int>();
        public ExpeditionTime TimeOfDay;
        public int Week;
        public int DayOfWeek;

        /// <summary>런 시계 길이(초). 0이면 지역 기본값.</summary>
        public float RunLength;

        public int SleepDebt;
        public int Hp;
        public int Fatigue;
        public int Stress;

        /// <summary>수분 (0 아래면 갈증만큼 탐험 최대 스태미너가 준다).</summary>
        public int Hydration;

        /// <summary>선생님 무전 지원 (무전기 수리 후).</summary>
        public bool RadioSupport;

        /// <summary>탐험 직후 연속 출정 (판정 페널티·쓰러짐 확률 상승).</summary>
        public bool ConsecutiveRun;

        public string WeatherId = "";

        // ── M4 ──

        /// <summary>출정 시점의 플래그 (탐험 조우 조건).</summary>
        public HashSet<string> Flags = new HashSet<string>();

        /// <summary>깊은 곳의 일방 출구 위치를 안다 (유랑자 특기, 기획 B). 들어가면 출구 자리를 알려 주고, 헤드리스는 깊이 들어갈 확률이 오른다.</summary>
        public bool KnowsDeepExits;

        /// <summary>후문 진입 (출정 준비에서 고름): 쇠지렛대로 열어 둔 탈출로 끝에서 시작한다 (ExitRules.CanEnterRear). 헤드리스는 보지 않는다.</summary>
        public bool RearEntry;

        /// <summary>특성 배율 (키 = TraitModifierKind 이름: SearchSpeed, NoiseMultiplier, RunSpeed, StealthBonus).</summary>
        public Dictionary<string, float> Modifiers = new Dictionary<string, float>();

        public float Modifier(string key) => Modifiers.TryGetValue(key, out var value) ? value : 0f;

        /// <summary>주무기의 남은 내구도 (0 = 최대치 사용).</summary>
        public int WeaponDurability;

        /// <summary>찬 방어구·도구의 출발 내구(방어구·쇠지렛대)·전력(빛 도구), 키 = 아이템 ID (출정 충전 뒤 값). 없으면 최대치.</summary>
        public Dictionary<string, int> EquipDurability = new Dictionary<string, int>();

        /// <summary>출정 때 바닥난 빛 도구를 채우려고 쓴 거점 배터리 수 (귀환 때 뺀다).</summary>
        public int BaseBatteriesUsed;

        /// <summary>찬 방어구·도구의 출발 내구·전력 (기록이 없으면 최대치).</summary>
        public int StartDurability(ItemDef item) =>
            item == null ? 0 : EquipDurability.TryGetValue(item.Id, out var left) ? left : item.MaxDurability;

        // ── 적대적 생존자·상자 고갈 ──

        /// <summary>이번 출정에 나오는 적대적 생존자의 성향 (0 = 없음, 1 위협 / 2 강탈 / 3 강탈+구타). HostileRules가 정한다.</summary>
        public int HostileTemperament;

        /// <summary>생존자 수 (한 무리, 같은 성향).</summary>
        public int HostileCount;

        /// <summary>출정 시점의 노출도 (낮에 자주 나갈수록 생존자가 더 자주 나온다).</summary>
        public int DayExposure;

        /// <summary>난이도 배율: 피해·쓰러짐, 사람 위협 (생존자 등장).</summary>
        public float HarmMultiplier = 1f;
        public float HumanThreatMultiplier = 1f;

        /// <summary>판 시드 (지역마다 처음부터 빈 상자를 정한다).</summary>
        public int WorldSeed;

        /// <summary>지난 방문 뒤 물자가 다시 채워졌다 (들어가면 '누군가 머물다 간 흔적').</summary>
        public bool Restocked;

        /// <summary>이 지역에서 이미 뒤진 상자 키 (DepletingContainers 지역만). 키는 HostileRules.ContainerKey.</summary>
        public HashSet<string> LootedContainers = new HashSet<string>();
    }

    /// <summary>적대적 생존자에게 붙잡힌 결과.</summary>
    public enum HostileOutcome
    {
        None,

        /// <summary>1단계: 위협하고 쫓아낸다 (가진 것은 지킨다).</summary>
        Evicted,

        /// <summary>2단계: 배낭을 전부 빼앗고 쫓아낸다.</summary>
        Robbed,

        /// <summary>3단계: 빼앗고 구타한다 (체력 크게 감소, 트라우마).</summary>
        Beaten,
    }

    public sealed class ExpeditionResult
    {
        public List<ItemStack> Loot = new List<ItemStack>();

        /// <summary>거점 자원으로 바로 들어가는 획득량 (헤드리스 모델용, 실시간 모드에서는 Loot 정산 결과).</summary>
        public List<ResourceAmount> ResourceLoot = new List<ResourceAmount>();

        public InjuryFlags Injuries;
        public int HpDelta;
        public float StressDelta;
        public float FatigueDelta;

        /// <summary>새벽 귀환 등 런 안에서 생긴 추가 수면 부채 (밤 탐험 기본 +1은 허브가 처리).</summary>
        public int SleepDebtDelta;

        public int ConscienceDelta;
        public List<string> Flags = new List<string>();
        public List<string> Clues = new List<string>();
        public float TimeSpent;
        public ExtractStatus Status;

        // ── M4 ──

        /// <summary>탐험 중 쓴 소모품 (하나당 한 항목).</summary>
        public List<string> UsedItems = new List<string>();

        /// <summary>주무기 내구도 소모량 (가지고 나간 무기).</summary>
        public int WeaponWear;

        /// <summary>탐험 중 장비를 바꿨으면 끝날 때 찬 장비 (null = 바꾸지 않음). 무기·가방·도구·방어구.</summary>
        public LoadoutState FinalLoadout;

        /// <summary>탐험에서 주워 장착한 채 돌아온 아이템 (창고에 더한다).</summary>
        public List<string> EquippedLoot = new List<string>();

        /// <summary>주워 장착한 채 돌아온 무기의 내구도 소모.</summary>
        public int EquippedLootWear;

        /// <summary>가지고 나간 방어구·도구가 돌아올 때 남은 내구·전력 (키 = 아이템 ID). 부서져 잃은 것은 LostOwned에 든다.</summary>
        public Dictionary<string, int> OwnedDurability = new Dictionary<string, int>();

        /// <summary>주워 찬 채 돌아온 방어구·도구의 남은 내구·전력 (키 = 아이템 ID).</summary>
        public Dictionary<string, int> EquippedLootDurability = new Dictionary<string, int>();

        /// <summary>가지고 나간 장비 중 탐험에서 버리고 온 것 (창고에서 뺀다).</summary>
        public List<string> LostOwned = new List<string>();

        /// <summary>적대적 생존자에게 붙잡힌 결과 (없으면 None).</summary>
        public HostileOutcome Hostile;

        /// <summary>이번에 뒤진 상자 키 (DepletingContainers 지역이면 거점이 기록한다).</summary>
        public List<string> SearchedContainers = new List<string>();

        /// <summary>탐험 중 그 자리에서 치른 이벤트와 고른 선택지 (귀환 때 효과를 적용하고 본 것으로 기록).</summary>
        public List<FieldEventChoice> FieldEvents = new List<FieldEventChoice>();
    }

    /// <summary>탐험 중 그 자리에서 치른 이벤트 하나.</summary>
    public sealed class FieldEventChoice
    {
        public string EventId = "";
        public string ChoiceId = "";
    }

    /// <summary>탐험 실행기. M1은 헤드리스 확률 모델, M2b부터 실시간 탐험 씬이 같은 계약을 구현한다.</summary>
    public interface IExpeditionRunner
    {
        ExpeditionResult Run(ExpeditionRequest request);
    }

    public static class ExpeditionRequestFactory
    {
        public static ExpeditionRequest Create(GameState state, ActionDef action, int seed, bool consecutiveRun, IDefinitionDatabase db = null)
        {
            var player = state.Player;
            Rules.LoadoutRules.Sanitize(state);
            var request = new ExpeditionRequest
            {
                LocationId = action.LocationId,
                Seed = seed,
                Loadout = CloneLoadout(state.Loadout),
                WeaponDurability = WeaponDurability(state),
                Flags = new HashSet<string>(state.Flags),
                Modifiers = db != null ? Rules.TraitRules.ExplorationModifiers(state, db) : new Dictionary<string, float>(),
                Stats = new Dictionary<StatType, int>(player.Stats),
                TimeOfDay = action.ExpeditionTime,
                Week = state.Calendar.Week,
                DayOfWeek = state.Calendar.Day,
                SleepDebt = player.SleepDebt,
                Hp = player.Vitals.Hp,
                Fatigue = player.Vitals.Fatigue,
                Hydration = player.Vitals.Hydration,
                Stress = player.Vitals.Stress,
                ConsecutiveRun = consecutiveRun,
                WeatherId = state.Base.Weather.ToString(),
            };

            // 적대적 생존자 (낮만) · 상자 고갈 (교실동)
            LocationDef location = null;
            var hasLocation = db != null && db.TryGetLocation(action.LocationId, out location);
            request.DayExposure = state.Exploration.DayExposure;
            if (db != null)
            {
                var difficulty = Rules.DifficultyRules.Get(state, db.Balance);
                request.HarmMultiplier = difficulty.Harm;
                request.KnowsDeepExits = Rules.SpecialRules.Value(state, db.Balance, SpecialKind.DeepExitKnown) > 0f;
                request.HumanThreatMultiplier = difficulty.HumanThreat * Rules.FactionRules.HostileMultiplier(state, db);
            }

            HostileRules.Assign(request, hasLocation ? location : null, db?.Balance);
            request.WorldSeed = state.Rng.Seed;
            request.Restocked = state.Flags.Contains(ContainerRules.RestockFlag(action.LocationId));
            if (hasLocation && location.DepletingContainers)
            {
                var prefix = action.LocationId + "/";
                foreach (var key in state.Exploration.LootedContainers)
                {
                    if (key.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        request.LootedContainers.Add(key);
                    }
                }
            }

            foreach (var pair in player.Proficiency)
            {
                request.ProficiencyLevels[pair.Key] = pair.Value.Level;
            }

            FillDurability(request, state, db);
            return request;
        }

        /// <summary>출정 준비 화면에서 로드아웃을 바꾼 뒤 요청에 다시 반영한다.</summary>
        public static void RefreshLoadout(ExpeditionRequest request, GameState state, IDefinitionDatabase db = null)
        {
            Rules.LoadoutRules.Sanitize(state);
            request.Loadout = CloneLoadout(state.Loadout);
            request.WeaponDurability = WeaponDurability(state);
            FillDurability(request, state, db);
        }

        /// <summary>찬 방어구·도구의 출발 내구·전력. 전력이 바닥난 빛 도구는 거점 배터리 하나로 채운다 (배터리는 귀환 때 뺀다).</summary>
        private static void FillDurability(ExpeditionRequest request, GameState state, IDefinitionDatabase db)
        {
            request.EquipDurability.Clear();
            request.BaseBatteriesUsed = 0;
            if (db == null)
            {
                return;
            }

            var batteries = state.Base.GetResource(ResourceType.Battery);
            var loadout = state.Loadout;
            foreach (var id in new[] { loadout.Armor, loadout.Tool, loadout.Tool2 })
            {
                if (string.IsNullOrEmpty(id) || request.EquipDurability.ContainsKey(id) || !db.TryGetItem(id, out var item) || item.MaxDurability <= 0)
                {
                    continue;
                }

                var stack = state.Storage.Find(s => s.ItemId == id);
                var left = item.LeftOf(stack?.Durability ?? 0);
                if (item.UsesPower && left <= 0 && request.BaseBatteriesUsed < batteries)
                {
                    left = Rules.LoadoutRules.BatteryCharge(item, db.Balance);
                    request.BaseBatteriesUsed++;
                }

                request.EquipDurability[id] = left;
            }
        }

        private static int WeaponDurability(GameState state)
        {
            var stack = state.Storage.Find(s => s.ItemId == state.Loadout.MainWeapon);
            return stack?.Durability ?? 0;
        }

        private static LoadoutState CloneLoadout(LoadoutState source)
        {
            return new LoadoutState
            {
                Backpack = source.Backpack,
                MainWeapon = source.MainWeapon,
                Tool = source.Tool,
                Tool2 = source.Tool2,
                Armor = source.Armor,
                QuickSlots = new List<string>(), // 퀵슬롯은 없앴다
                Carry = new List<string>(source.Carry ?? new List<string>()),
            };
        }
    }
}
