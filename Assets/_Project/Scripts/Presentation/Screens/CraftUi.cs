using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Rules;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>제작 문구 (Docs/CRAFTING_PROPOSAL.md 2.2절): 카드의 재료·칸 줄, 다음 단계, 결과 한 줄, 결산 일기.</summary>
    internal static class CraftUi
    {
        /// <summary>카드 둘째 줄: 재료(자원·앞 단계 물건) · 칸 수 · 지금 못 하는 까닭.</summary>
        public static string CardLine(ActionDef action, ActionPreview preview, GameSession session)
        {
            var parts = new List<string>();
            var materials = preview.Costs.Where(c => c.Amount > 0).Select(c => UiText.Resource(c.Resource) + " " + c.Amount)
                .Concat(action.Craft.ItemCosts.Select(c => Loc.Item(c.ItemId) + " " + c.Count))
                .ToList();
            if (materials.Count > 0)
            {
                parts.Add(string.Join(" · ", materials));
            }

            if (action.Craft.Work >= 2)
            {
                parts.Add(Loc.T("craft.work." + action.Craft.Work));
            }

            var block = Block(action, preview, session);
            if (block.Length > 0)
            {
                parts.Add(block);
            }

            return string.Join("  ·  ", parts);
        }

        private static string Block(ActionDef action, ActionPreview preview, GameSession session)
        {
            switch (preview.CraftBlock)
            {
                case CraftBlock.Items:
                    var missing = action.Craft.ItemCosts.FirstOrDefault(c => LoadoutRules.CountInStorage(session.State, c.ItemId) < c.Count);
                    return missing == null ? "" : Loc.F("craft.block.Items", Loc.Josa(Loc.Item(missing.ItemId), "이/가"));
                case CraftBlock.MaxOwned:
                    return Loc.F("craft.block.MaxOwned", LoadoutRules.CountInStorage(session.State, action.Craft.Product));
                case CraftBlock.NothingToMaintain:
                    return Loc.T("craft.block.NothingToMaintain");
                default:
                    return "";
            }
        }

        /// <summary>카드 아래 흐리게: 아직 열리지 않은 다음 단계 (없거나 이미 열렸으면 null).</summary>
        public static string NextLine(ActionDef action, GameSession session)
        {
            var next = action.Craft.Next;
            if (string.IsNullOrEmpty(next) || !session.Definitions.TryGetAction(next, out var nextAction) || session.IsUnlocked(nextAction))
            {
                return null;
            }

            return Loc.F("craft.next", Loc.Action(next));
        }

        /// <summary>만든 것의 이름: 물건이면 아이템, 손질이면 그 무기, 설비면 행동 짧은 이름.</summary>
        private static string Thing(CraftOutcome craft)
        {
            if (!string.IsNullOrEmpty(craft.ProductId)) return Loc.Item(craft.ProductId);
            if (!string.IsNullOrEmpty(craft.WeaponId)) return Loc.Item(craft.WeaponId);
            return Loc.Action(craft.RecipeId, "short");
        }

        /// <summary>실행 중 결과 한 줄에 붙일 말 (없으면 빈 문자열).</summary>
        public static string TickerLine(CraftOutcome craft)
        {
            if (craft == null || craft.Step == CraftStep.None)
            {
                return "";
            }

            return Special(craft, "result.craft.done.") ?? Loc.F("result.craft." + craft.Step, Thing(craft), craft.Remaining);
        }

        /// <summary>레시피 전용 완성 문구 (예: 작업대를 놓았다). 완성·잘 만듦에만, 없으면 null.</summary>
        private static string Special(CraftOutcome craft, string prefix)
        {
            if (craft.Step != CraftStep.Done && craft.Step != CraftStep.Fine)
            {
                return null;
            }

            return Loc.Lookup(Loc.UiTable, prefix + craft.RecipeId);
        }

        /// <summary>결산 일기 한 줄: 완성·잘 만듦·실패만 (착수·제작 중은 남기지 않는다). 없으면 null.</summary>
        public static string SettleLine(CraftOutcome craft)
        {
            if (craft == null)
            {
                return null;
            }

            var special = Special(craft, "settle.craft_done.");
            if (special != null)
            {
                return special;
            }

            switch (craft.Step)
            {
                case CraftStep.Done:
                case CraftStep.Maintained:
                    return Loc.F("settle.craft_done", Loc.Josa(Thing(craft), "을/를"));
                case CraftStep.Fine:
                    return Loc.F("settle.craft_fine", Loc.Josa(Thing(craft), "을/를"));
                case CraftStep.Failed:
                    return Loc.F("settle.craft_failed", Thing(craft));
                default:
                    return null;
            }
        }
    }
}
