using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Text;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 거점 방의 말 (BASE_TAB_PROPOSAL P2): 시설 효과·쓰임·잠긴 방 힌트·맡은 사람.
    /// 거점 상황판 시설 칸과 단면도 방 패널이 같은 말을 쓴다.
    /// 효과는 시설 데이터(traits_facilities.json Modifiers)에서 종류별 문구 facility.mod.{Kind}로 만든다.
    /// </summary>
    internal static class RoomText
    {
        /// <summary>값이 비율(0.15 = 15%)인 보정 종류.</summary>
        private static readonly HashSet<TraitModifierKind> PercentKinds = new HashSet<TraitModifierKind>
        {
            TraitModifierKind.EducationGain, TraitModifierKind.CheckBonus, TraitModifierKind.GreatSuccessBonus,
            TraitModifierKind.RestRecovery, TraitModifierKind.ExpeditionStress,
        };

        /// <summary>그 시설이 거점에 주는 효과 (한 줄씩). 시설이 없거나 효과가 없으면 빈 목록.</summary>
        public static List<string> Effects(IDefinitionDatabase db, string facilityId)
        {
            var lines = new List<string>();
            var def = string.IsNullOrEmpty(facilityId) ? null : db.Extras.Facilities.Find(f => f.Id == facilityId);
            if (def == null)
            {
                return lines;
            }

            foreach (var modifier in def.Modifiers)
            {
                lines.Add(Effect(modifier));
            }

            return lines;
        }

        public static string Effect(TraitModifier modifier)
        {
            var value = PercentKinds.Contains(modifier.Kind) ? modifier.Value * 100f : modifier.Value;
            var number = Number(value);
            if (modifier.Kind == TraitModifierKind.CheckBonus)
            {
                return string.IsNullOrEmpty(modifier.Key)
                    ? Loc.F("facility.mod.CheckBonus.all", number)
                    : Loc.F("facility.mod.CheckBonus", number, Enum.TryParse<ProficiencyType>(modifier.Key, out var proficiency)
                        ? UiText.Proficiency(proficiency)
                        : modifier.Key);
            }

            return Loc.F("facility.mod." + modifier.Kind, number);
        }

        /// <summary>이 방의 쓰임 (효과가 없는 방의 상황판 한 줄).</summary>
        public static string Use(HubSectionConfig.RoomDef room) => Loc.T("hubroom." + room.Id + ".use");

        /// <summary>잠긴 방을 여는 방법.</summary>
        public static string Hint(HubSectionConfig.RoomDef room) => Loc.T("hubroom." + room.Id + ".hint");

        /// <summary>
        /// 이 방이 묶인 역할과 맡은 동료 한 줄 ("간호 자리: 서진 ★"). 역할 방이 아니면 null.
        /// 맡은 사람이 없으면 역할 판으로 안내한다.
        /// </summary>
        public static string People(GameState state, IDefinitionDatabase db, string roomId)
        {
            var role = db.Extras.Roles.Find(r => r.Rooms.Exists(room => room.Id == roomId));
            if (role == null)
            {
                return null;
            }

            var roleName = Loc.T("role." + role.Id);
            var holders = RoleRules.Holders(state, role.Id);
            if (holders.Count == 0)
            {
                return Loc.F("hub.room_people_none", roleName);
            }

            var names = holders.Select(id =>
                Loc.T("npc." + id) + (RoleRules.HasAptitude(db.Balance, id, role.Id) ? " " + Loc.T("hub.room_aptitude") : ""));
            return Loc.F("hub.room_people", roleName, string.Join(", ", names));
        }

        private static string Number(float value) =>
            Math.Abs(value - (float)Math.Round(value)) < 0.01f
                ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
