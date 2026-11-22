using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>물림 판정 결과 (탐험 슬롯 연출용).</summary>
    public sealed class BiteResult
    {
        /// <summary>응급 처치에 성공해 감염이 휴면했는가.</summary>
        public bool Treated;

        public int InfectionGain;
    }

    /// <summary>
    /// 감염 규칙 (문서 3.1.1 휴면열, 5.3.1). 주인공 감염도는 숫자를 숨기고 증상으로만 알린다.
    /// 선생님은 재감염 이후 진행도가 쌓이고, 단계마다 멘토 기능이 약해진다.
    /// </summary>
    public static class InfectionRules
    {
        public const string FlagBitten = "BITTEN";
        public const string FlagTeacherReinfected = "TEACHER_REINFECTED";

        /// <summary>완치 (4막 히든 루트). 진행이 멈추고 병세 단계는 0이 된다.</summary>
        public const string FlagTeacherCured = "TEACHER_CURED";

        /// <summary>증상 단계 경계 (감염도). 0 = 증상 없음, 1 미열, 2 기침, 3 손떨림.</summary>
        public static readonly int[] SymptomThresholds = { 25, 50, 75 };

        public static int SymptomLevel(GameState state)
        {
            var infection = state.Player.Vitals.Infection;
            var level = 0;
            foreach (var threshold in SymptomThresholds)
            {
                if (infection >= threshold) level++;
            }

            return level;
        }

        /// <summary>쓰러졌거나 물림 부상이 있으면 물림 판정. 물리지 않았으면 null. 쓰러지지 않았으면 난수를 쓰지 않는다.</summary>
        public static BiteResult RollBite(
            GameState state, IDefinitionDatabase db, IRandomService random, bool night, bool down, bool biteInjury, StateDelta delta)
        {
            var balance = db.Balance;
            var bitten = biteInjury;
            if (!bitten && down)
            {
                var chance = night ? balance.BiteChanceOnDownNight : balance.BiteChanceOnDownDay;
                bitten = chance > 0f && random.NextDouble() < chance;
            }

            if (!bitten)
            {
                return null;
            }

            var medicine = state.Player.ProficiencyLevel(ProficiencyType.Medicine);
            var firstAid = balance.FirstAidBaseChance + medicine * balance.FirstAidPerMedicineLevel;
            var result = new BiteResult { Treated = random.NextDouble() < firstAid };
            result.InfectionGain = result.Treated ? balance.BiteInfectionTreated : balance.BiteInfection;
            // 청결이 낮으면 상처가 더 쉽게 덧난다
            result.InfectionGain = (int)Math.Round(result.InfectionGain * (1f + balance.BiteDirtyBonus * InjuryRules.Dirt(state, balance)), MidpointRounding.AwayFromZero);
            EffectApplier.ChangeVital(state, VitalType.Infection, result.InfectionGain, balance, delta);
            if (state.Flags.Add(FlagBitten))
            {
                delta.FlagsSet.Add(FlagBitten);
            }

            return result;
        }

        /// <summary>주간 정산: 감염이 있으면 오른다. 최대치에 이르면 true (감염 배드 엔딩).</summary>
        public static bool WeeklyPlayer(GameState state, IDefinitionDatabase db, StateDelta delta)
        {
            var balance = db.Balance;
            if (state.Player.Vitals.Infection <= 0)
            {
                return false;
            }

            var growth = balance.InfectionWeeklyGrowth
                         - state.Player.ProficiencyLevel(ProficiencyType.Medicine) * balance.InfectionGrowthReducePerMedicineLevel
                         - TraitRules.Spread(TraitRules.Sum(state, db, TraitModifierKind.InfectionResist), state.Calendar.Week);
            growth = Math.Max(balance.InfectionGrowthMin, growth);
            growth = DifficultyRules.Scale(growth, DifficultyRules.Get(state, balance).Infection);
            EffectApplier.ChangeVital(state, VitalType.Infection, growth, balance, delta);
            return state.Player.Vitals.Infection >= balance.VitalMax;
        }

        /// <summary>주간 정산: 재감염된 선생님의 병세 진행 (한파 가산, 의학 숙련·돌봄 보정 감산).</summary>
        public static void WeeklyTeacher(GameState state, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            var teacher = state.Teacher;
            if (!teacher.Reinfected || !teacher.Alive || state.Flags.Contains(FlagTeacherCured))
            {
                return;
            }

            var growth = balance.TeacherInfectionWeekly
                         - state.Player.ProficiencyLevel(ProficiencyType.Medicine) * balance.TeacherInfectionReducePerMedicineLevel
                         - TraitRules.Spread(TraitRules.Sum(state, db, TraitModifierKind.TeacherCare), state.Calendar.Week)
                         + (state.Base.Weather == WeatherType.Cold ? balance.TeacherInfectionColdBonus : 0);
            ChangeTeacherInfection(state, DifficultyRules.Scale(Math.Max(balance.TeacherInfectionMinWeekly, growth), DifficultyRules.Get(state, balance).Infection), balance);
        }

        public static void ChangeTeacherInfection(GameState state, int amount, BalanceConfig balance)
        {
            var teacher = state.Teacher;
            if (!teacher.Reinfected)
            {
                return;
            }

            var max = balance?.VitalMax ?? 100;
            teacher.InfectionProgress = Math.Min(Math.Max(teacher.InfectionProgress + amount, 0), max);
        }

        /// <summary>병세 단계 0~4 (재감염 전에는 0). balance가 없으면 기본 단계 크기 25.</summary>
        public static int TeacherStage(GameState state, BalanceConfig balance)
        {
            if (!state.Teacher.Reinfected || state.Flags.Contains(FlagTeacherCured))
            {
                return 0;
            }

            var size = Math.Max(1, balance?.TeacherStageSize ?? 25);
            return state.Teacher.InfectionProgress / size;
        }
    }

    /// <summary>주간 날씨 (문서 6.6): 계절표 가중치로 굴린다.</summary>
    public static class WeatherRules
    {
        public static WeatherType Roll(int week, BalanceConfig balance, IRandomService random)
        {
            var season = CalendarRules.GetSeason(week);
            WeatherOdds odds = null;
            foreach (var entry in balance.WeatherBySeason ?? Array.Empty<WeatherOdds>())
            {
                if (entry.Season == season) odds = entry;
            }

            if (odds == null)
            {
                return WeatherType.Clear;
            }

            var total = Math.Max(0, odds.Clear) + Math.Max(0, odds.Rain) + Math.Max(0, odds.Heat) + Math.Max(0, odds.Cold);
            if (total <= 0)
            {
                return WeatherType.Clear;
            }

            var pick = random.Range(0, total - 1);
            if ((pick -= Math.Max(0, odds.Clear)) < 0) return WeatherType.Clear;
            if ((pick -= Math.Max(0, odds.Rain)) < 0) return WeatherType.Rain;
            if ((pick -= Math.Max(0, odds.Heat)) < 0) return WeatherType.Heat;
            return WeatherType.Cold;
        }
    }

    /// <summary>정찰 정보 (문서 6.3.8): 플래그 SCOUT_{지역}이 있으면 출정 준비 화면에 지역 정보가 보이고, 출정하면 소모된다.</summary>
    public static class ScoutRules
    {
        public const string Prefix = "SCOUT_";

        public static string FlagFor(string locationId) => Prefix + (locationId ?? "");

        public static bool IsScouted(GameState state, string locationId) => state.Flags.Contains(FlagFor(locationId));
    }

    /// <summary>
    /// 동료 역할 (문서 6.4, 역할 재기획 Docs/ROLE_ROOMS_PROPOSAL.md): 합류한 NPC에게만, 데이터에 있는 역할만 맡길 수 있다.
    /// 역할마다 방 묶음이 있어 열린 방이 있어야 맡길 수 있고(기본 역할은 늘), 열린 방이 늘수록 효과가 커진다.
    /// 한 역할의 정원(BalanceConfig.RoleCapacity)을 넘으면 늦게 붙은 사람은 효과가 없다.
    /// </summary>
    public static class RoleRules
    {
        public static RoleDef Find(IDefinitionDatabase db, string roleId) =>
            string.IsNullOrEmpty(roleId) ? null : db.Extras.Roles.Find(r => r.Id == roleId);

        public static bool IsRoomOpen(GameState state, RoleRoomDef room)
        {
            if (room == null)
            {
                return false;
            }

            if (room.Always || (!string.IsNullOrEmpty(room.Flag) && state.Flags.Contains(room.Flag)))
            {
                return true;
            }

            return !string.IsNullOrEmpty(room.Facility) && state.Base.Facilities.TryGetValue(room.Facility, out var level) && level > 0;
        }

        /// <summary>묶음에서 열린 방 수. 방이 없는 역할은 1로 친다.</summary>
        public static int OpenRoomCount(GameState state, RoleDef role)
        {
            if (role == null)
            {
                return 0;
            }

            return role.Rooms.Count == 0 ? 1 : role.Rooms.Count(r => IsRoomOpen(state, r));
        }

        /// <summary>역할 단추에 나오는가: 기본 역할이거나 묶음에 열린 방이 있다.</summary>
        public static bool IsAvailable(GameState state, RoleDef role) => role != null && (role.Basic || OpenRoomCount(state, role) > 0);

        /// <summary>열린 방 수 배율: 1개 ×1, 하나 늘 때마다 + RoleExtraRoomBonus. 기본 역할은 방이 닫혀 있어도 ×1.</summary>
        public static float RoomScale(GameState state, RoleDef role, BalanceConfig balance)
        {
            var open = OpenRoomCount(state, role);
            if (open <= 0)
            {
                return role != null && role.Basic ? 1f : 0f;
            }

            return 1f + (open - 1) * balance.RoleExtraRoomBonus;
        }

        public static bool HasAptitude(BalanceConfig balance, string npcId, string roleId)
        {
            if (string.IsNullOrEmpty(roleId) || balance.RoleAptitudes == null)
            {
                return false;
            }

            foreach (var aptitude in balance.RoleAptitudes)
            {
                if (aptitude.NpcId == npcId && aptitude.Roles != null && Array.IndexOf(aptitude.Roles, roleId) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>그 역할을 맡은 동료 (먼저 받은 순, 같으면 ID 순).</summary>
        public static List<string> Holders(GameState state, string roleId) =>
            state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined && n.Value.Role == roleId)
                .OrderBy(n => n.Value.RoleSinceWeek).ThenBy(n => n.Key, StringComparer.Ordinal)
                .Select(n => n.Key).ToList();

        /// <summary>정원 안에 들어 역할 효과를 받는가 (다침·자리 비움은 따로 본다).</summary>
        public static bool IsActive(GameState state, string npcId, BalanceConfig balance)
        {
            if (!state.Npcs.TryGetValue(npcId ?? "", out var npc) || string.IsNullOrEmpty(npc.Role))
            {
                return false;
            }

            var index = Holders(state, npc.Role).IndexOf(npcId);
            return index >= 0 && (balance.RoleCapacity <= 0 || index < balance.RoleCapacity);
        }

        /// <summary>
        /// 역할 효과 배율 (정원·열린 방·적성). 관계 단계·스트레스 배율은 TraitRules에서 곱한다.
        /// 맡을 수 없는 상태면 0.
        /// </summary>
        public static float EffectScale(GameState state, IDefinitionDatabase db, string npcId, RoleDef role)
        {
            var balance = db.Balance;
            if (role == null || !IsActive(state, npcId, balance))
            {
                return 0f;
            }

            return RoomScale(state, role, balance) * (HasAptitude(balance, npcId, role.Id) ? balance.RoleAptitudeMultiplier : 1f);
        }

        /// <summary>
        /// 동료가 설 방 ID: 묶음에서 열린 방을 차례로 (같은 역할 둘째 사람은 다음 열린 방, 모자라면 마지막 열린 방).
        /// 열린 방이 없으면 null (거처).
        /// </summary>
        public static string RoomFor(GameState state, IDefinitionDatabase db, string npcId)
        {
            if (!state.Npcs.TryGetValue(npcId ?? "", out var npc))
            {
                return null;
            }

            var role = Find(db, npc.Role);
            if (role == null)
            {
                return null;
            }

            var open = role.Rooms.Where(r => IsRoomOpen(state, r)).ToList();
            if (open.Count == 0)
            {
                return null;
            }

            var index = Math.Max(0, Holders(state, npc.Role).IndexOf(npcId));
            return open[Math.Min(index, open.Count - 1)].Id;
        }

        /// <summary>지금 이 동료에게 맡길 수 있는 역할 (단추 순환 순서). 정원이 찬 역할은 빠진다 (이미 맡은 역할은 남는다).</summary>
        public static List<RoleDef> Choices(GameState state, IDefinitionDatabase db, string npcId)
        {
            state.Npcs.TryGetValue(npcId ?? "", out var npc);
            var capacity = db.Balance.RoleCapacity;
            return db.Extras.Roles.Where(r => IsAvailable(state, r)
                                              && (npc?.Role == r.Id || capacity <= 0 || Holders(state, r.Id).Count < capacity)).ToList();
        }

        /// <summary>계획 단계에서 역할을 바꾼다. 맡을 수 없는 역할(방이 닫힘·정원이 참)은 거절한다.</summary>
        public static bool Assign(GameState state, IDefinitionDatabase db, string npcId, string roleId)
        {
            if (!state.Npcs.TryGetValue(npcId ?? "", out var npc) || npc.Status != NpcStatus.Joined)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(roleId) && !Choices(state, db, npcId).Exists(r => r.Id == roleId))
            {
                return false;
            }

            if (npc.Role != (roleId ?? ""))
            {
                npc.Role = roleId ?? "";
                npc.RoleSinceWeek = state.Calendar.Week;
            }

            return true;
        }
    }
}
