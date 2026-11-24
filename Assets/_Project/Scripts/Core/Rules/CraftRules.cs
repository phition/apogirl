using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>제작 칸 하나의 결과 단계.</summary>
    public enum CraftStep
    {
        /// <summary>"제작 중" 칸인데 진행 중인 제작이 없다 (피로만 든다).</summary>
        None,

        /// <summary>착수했고 뒤 칸이 남았다.</summary>
        Started,

        /// <summary>"제작 중" 칸, 아직 남았다.</summary>
        Working,

        Done,

        /// <summary>대성공: 잘 만든 물건.</summary>
        Fine,

        /// <summary>완성 판정 실패: 자원 반·앞 단계 물건을 돌려받았다.</summary>
        Failed,

        /// <summary>착수할 재료가 모자랐다 (아무것도 내지 않았고 뒤 칸은 쉬었다).</summary>
        NoMaterials,

        /// <summary>무기 손질 완료.</summary>
        Maintained,
    }

    /// <summary>제작 칸의 결과 (결과 한 줄·결산 일기·말풍선용).</summary>
    public sealed class CraftOutcome
    {
        public string RecipeId = "";
        public CraftStep Step;

        /// <summary>완성한 물건과 개수 (설비·손질이면 비움).</summary>
        public string ProductId = "";
        public int Count;

        /// <summary>손질한 무기.</summary>
        public string WeaponId = "";

        /// <summary>이 칸 뒤에 남은 "제작 중" 칸.</summary>
        public int Remaining;

        public CheckResult Result = CheckResult.Success;

        public bool Finished => Step == CraftStep.Done || Step == CraftStep.Fine || Step == CraftStep.Maintained;
    }

    /// <summary>착수할 수 없는 까닭 (카드에 한 줄로).</summary>
    public enum CraftBlock
    {
        None,
        Resources,
        Items,
        MaxOwned,
        NothingToMaintain,
    }

    /// <summary>
    /// 제작 (Docs/CRAFTING_PROPOSAL.md 3.0절·4.2절, 결정 C1): 낮·밤 어느 칸에서나 착수하고, 뒤 Work − 1칸이 "제작 중"으로 잠긴다.
    /// 재료는 착수 칸에서 내고, 마지막 칸에서 한 번 판정한다. 진행은 주를 넘기지 않는다.
    /// </summary>
    public static class CraftRules
    {
        public const string CraftedFlagPrefix = "CRAFTED_";

        public static bool IsContinue(ActionDef action, BalanceConfig balance) =>
            action != null && action.Id == balance.CraftContinueActionId;

        /// <summary>제작 칸(착수 또는 "제작 중")인가.</summary>
        public static bool Handles(ActionDef action, BalanceConfig balance) => action != null && (action.IsCraft || IsContinue(action, balance));

        // ───────────── 계획 ─────────────

        /// <summary>착수 칸 뒤의 칸이 주 안에 있고, 제작이 미리 잠근 칸 말고 다른 잠금이 없는가.</summary>
        public static bool FitsPlan(GameState state, ActionDef action, int slotIndex)
        {
            if (action == null || !action.IsCraft)
            {
                return true;
            }

            var slots = state.Plan.Slots;
            for (var k = 1; k < action.Craft.Work; k++)
            {
                var index = slotIndex + k;
                if (index >= slots.Count || (slots[index].Locked && !slots[index].CraftLocked))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>계획의 "제작 중" 칸을 모두 풀고 비운다.</summary>
        public static void ReleaseLocks(WeekPlan plan)
        {
            foreach (var slot in plan.Slots)
            {
                if (slot.CraftLocked)
                {
                    slot.CraftLocked = false;
                    slot.Locked = false;
                    slot.ActionId = "";
                }
            }
        }

        /// <summary>
        /// 계획 편집 뒤: "제작 중" 칸을 모두 풀고, 앞에서부터 착수 카드마다 뒤 칸을 다시 잠근다.
        /// 뒤 칸에 자리가 없는 착수 카드는 비운다 (지난주 복사·프리셋에서 생길 수 있다).
        /// </summary>
        public static void Relock(GameState state, IDefinitionDatabase db)
        {
            var plan = state.Plan;
            ReleaseLocks(plan);
            var continueId = db.Balance.CraftContinueActionId;
            foreach (var slot in plan.Slots)
            {
                // 착수 카드 없이 떠도는 "제작 중" 칸 (지난주 계획 복사 등)은 비운다
                if (!slot.Locked && slot.ActionId == continueId)
                {
                    slot.ActionId = "";
                }
            }

            if (!db.TryGetAction(continueId, out _))
            {
                return;
            }

            for (var i = 0; i < plan.Slots.Count; i++)
            {
                var id = plan.Slots[i].ActionId;
                if (string.IsNullOrEmpty(id) || !db.TryGetAction(id, out var action) || !action.IsCraft || action.Craft.Work <= 1)
                {
                    continue;
                }

                if (!FitsPlan(state, action, i))
                {
                    plan.Slots[i].ActionId = "";
                    continue;
                }

                for (var k = 1; k < action.Craft.Work; k++)
                {
                    var slot = plan.Slots[i + k];
                    slot.ActionId = continueId;
                    slot.ForcedRest = false;
                    slot.Locked = true;
                    slot.CraftLocked = true;
                }

                i += action.Craft.Work - 1;
            }
        }

        // ───────────── 착수할 수 있나 ─────────────

        /// <summary>지금 창고·자원으로 착수할 수 있나 (해금은 따로 본다).</summary>
        public static CraftBlock CheckStart(GameState state, IDefinitionDatabase db, ActionDef action)
        {
            if (action == null || !action.IsCraft)
            {
                return CraftBlock.None;
            }

            var craft = action.Craft;
            foreach (var cost in action.ResourceCosts)
            {
                if (state.Base.GetResource(cost.Resource) < SpecialRules.Cost(state, db.Balance, cost))
                {
                    return CraftBlock.Resources;
                }
            }

            foreach (var cost in craft.ItemCosts)
            {
                if (LoadoutRules.CountInStorage(state, cost.ItemId) < Math.Max(1, cost.Count))
                {
                    return CraftBlock.Items;
                }
            }

            if (craft.MaxOwned > 0 && !string.IsNullOrEmpty(craft.Product)
                                   && LoadoutRules.CountInStorage(state, craft.Product) + Math.Max(1, craft.Count) > craft.MaxOwned)
            {
                return CraftBlock.MaxOwned;
            }

            if (craft.MaintainWeapon && WeaponToMaintain(state, db) == null)
            {
                return CraftBlock.NothingToMaintain;
            }

            return CraftBlock.None;
        }

        // ───────────── 실행 ─────────────

        /// <summary>제작 칸 하나를 실행한다 (착수 또는 "제작 중").</summary>
        public static CraftOutcome Execute(GameState state, IDefinitionDatabase db, ActionDef action, int slotIndex, IRandomService random, StateDelta delta)
        {
            return IsContinue(action, db.Balance)
                ? Continue(state, db, random, delta)
                : Start(state, db, action, slotIndex, random, delta);
        }

        private static CraftOutcome Start(GameState state, IDefinitionDatabase db, ActionDef recipe, int slotIndex, IRandomService random, StateDelta delta)
        {
            var balance = db.Balance;
            var craft = recipe.Craft;
            var outcome = new CraftOutcome { RecipeId = recipe.Id, Remaining = craft.Work - 1 };

            // 주 안에서는 하나씩: 혹시 남은 제작이 있으면 실패처럼 정리한다
            Abort(state, db, delta);

            ChargeVitals(state, balance, recipe.FatigueCost, recipe.StressCost, delta);
            if (CheckStart(state, db, recipe) != CraftBlock.None)
            {
                // 재료가 없다: 아무것도 내지 않고, 미리 잠근 뒤 칸은 쉬는 칸으로 바꾼다
                outcome.Step = CraftStep.NoMaterials;
                outcome.Result = CheckResult.Failure;
                outcome.Remaining = 0;
                FreeLockedAfter(state, balance, slotIndex, craft.Work - 1);
                return outcome;
            }

            // 밤 칸에서 착수해도 밤 작업이다 (정전이면 완성 판정 −, 사용자 결정 2026-11-24: 제작은 낮·밤 모두)
            var job = new CraftJob { RecipeId = recipe.Id, Remaining = craft.Work - 1, NightWorked = state.Calendar.Phase == DayPhase.Night };
            PayResources(state, db, recipe, job, delta);
            PayItems(state, db, craft, job, delta);
            state.Crafting.Active = job;

            if (job.Remaining > 0)
            {
                outcome.Step = CraftStep.Started;
                ApplyGains(state, db, recipe, CheckResult.Success, delta);
                return outcome;
            }

            return Finish(state, db, recipe, job, outcome, random, delta);
        }

        private static CraftOutcome Continue(GameState state, IDefinitionDatabase db, IRandomService random, StateDelta delta)
        {
            var balance = db.Balance;
            ChargeVitals(state, balance, balance.CraftContinueFatigue, balance.CraftContinueStress, delta);
            var job = state.Crafting.Active;
            if (job == null || !db.TryGetAction(job.RecipeId, out var recipe))
            {
                state.Crafting.Active = null;
                return new CraftOutcome { Step = CraftStep.None };
            }

            job.Remaining = Math.Max(0, job.Remaining - 1);
            if (state.Calendar.Phase == DayPhase.Night)
            {
                job.NightWorked = true;
            }

            var outcome = new CraftOutcome { RecipeId = recipe.Id, Remaining = job.Remaining };
            if (job.Remaining > 0)
            {
                outcome.Step = CraftStep.Working;
                ApplyGains(state, db, recipe, CheckResult.Success, delta);
                return outcome;
            }

            return Finish(state, db, recipe, job, outcome, random, delta);
        }

        /// <summary>마지막 칸: 판정하고 완성 또는 실패 (환불).</summary>
        private static CraftOutcome Finish(GameState state, IDefinitionDatabase db, ActionDef recipe, CraftJob job, CraftOutcome outcome, IRandomService random, StateDelta delta)
        {
            var balance = db.Balance;
            var result = CheckResult.Success;
            if (recipe.HasCheck)
            {
                result = CheckResolver.Roll(Chance(state, db, recipe, job.NightWorked), random, balance,
                    TraitRules.Sum(state, db, TraitModifierKind.GreatSuccessBonus) + MentorBondRules.GreatSuccessBonus(state, recipe, balance));
            }

            state.Crafting.Active = null;
            outcome.Result = result;
            ApplyGains(state, db, recipe, result, delta);
            if (result == CheckResult.Failure)
            {
                Refund(state, job, balance.CraftFailRefund, delta);
                outcome.Step = CraftStep.Failed;
                return outcome;
            }

            Complete(state, db, recipe, result == CheckResult.GreatSuccess, outcome, delta);
            return outcome;
        }

        /// <summary>작업대 Ⅱ가 있으면 모든 제작 판정에 더하는 값.</summary>
        public static float WorkshopBonus(GameState state, BalanceConfig balance) =>
            !string.IsNullOrEmpty(balance.WorkshopFlag) && state.Flags.Contains(balance.WorkshopFlag) ? balance.WorkshopCheckBonus : 0f;

        /// <summary>완성 판정 확률: 행동 판정 공식 + 작업대 Ⅱ − 정전 중 밤 작업.</summary>
        public static float Chance(GameState state, IDefinitionDatabase db, ActionDef recipe, bool nightWorked)
        {
            var balance = db.Balance;
            var chance = ActionResolver.CheckChance(state, recipe, db) + WorkshopBonus(state, balance);
            if (nightWorked && state.Flags.Contains(PowerRules.FlagBroken))
            {
                chance -= balance.BlackoutCraftPenalty;
            }

            return Math.Min(Math.Max(chance, balance.CheckMinChance), balance.CheckMaxChance);
        }

        private static void Complete(GameState state, IDefinitionDatabase db, ActionDef recipe, bool fine, CraftOutcome outcome, StateDelta delta)
        {
            var balance = db.Balance;
            var craft = recipe.Craft;
            if (craft.MaintainWeapon)
            {
                var stack = WeaponToMaintain(state, db);
                if (stack != null && db.TryGetItem(stack.ItemId, out var weapon))
                {
                    var current = stack.Durability <= 0 ? weapon.MaxDurability : stack.Durability;
                    var restore = (int)Math.Ceiling(weapon.MaxDurability * balance.WeaponMaintainRestore);
                    stack.Durability = Math.Min(weapon.MaxDurability, current + restore);
                    outcome.WeaponId = weapon.Id;
                }

                outcome.Step = CraftStep.Maintained;
            }
            else
            {
                outcome.Step = fine ? CraftStep.Fine : CraftStep.Done;
                if (!string.IsNullOrEmpty(craft.Product))
                {
                    var count = Math.Max(1, craft.Count);
                    var durability = 0;
                    var isWeapon = db.TryGetItem(craft.Product, out var item) && item.MaxDurability > 0;
                    if (isWeapon)
                    {
                        durability = fine ? (int)Math.Round(item.MaxDurability * balance.CraftFineDurability) : item.MaxDurability;
                    }

                    for (var i = 0; i < count; i++)
                    {
                        state.Storage.Add(new ItemStack { ItemId = craft.Product, Count = 1, Durability = durability });
                    }

                    delta.ItemsGained.Add(new ItemStack { ItemId = craft.Product, Count = count });
                    outcome.ProductId = craft.Product;
                    outcome.Count = count;
                    if (fine && !isWeapon && balance.CraftFineRefund > 0)
                    {
                        EffectApplier.ChangeResource(state, ResourceType.Materials, balance.CraftFineRefund, delta);
                    }
                }
                else if (fine && balance.CraftFineRefund > 0)
                {
                    EffectApplier.ChangeResource(state, ResourceType.Materials, balance.CraftFineRefund, delta);
                }
            }

            foreach (var flag in craft.SetFlags.Append(CraftedFlagPrefix + recipe.Id))
            {
                if (!string.IsNullOrEmpty(flag) && state.Flags.Add(flag))
                {
                    delta.FlagsSet.Add(flag);
                }
            }

            ScarecrowRules.OnCrafted(state, craft, balance);   // 교문 허수아비: 내구를 채운다
        }

        /// <summary>끝나지 못한 제작 (주가 끝났거나 새 제작이 끼어듦): 실패와 같이 돌려준다. 정리했으면 true.</summary>
        public static bool Abort(GameState state, IDefinitionDatabase db, StateDelta delta)
        {
            var job = state.Crafting?.Active;
            if (job == null)
            {
                return false;
            }

            state.Crafting.Active = null;
            Refund(state, job, db.Balance.CraftFailRefund, delta);
            return true;
        }

        // ───────────── 무기 손질 ─────────────

        /// <summary>손질할 무기 묶음: 찬 무기(그 ID 맨 앞 묶음)가 닳았으면 그것, 아니면 창고에서 가장 닳은 무기. 없으면 null.</summary>
        public static ItemStack WeaponToMaintain(GameState state, IDefinitionDatabase db)
        {
            var equipped = state.Loadout.MainWeapon;
            if (!string.IsNullOrEmpty(equipped))
            {
                var stack = state.Storage.Find(s => s.ItemId == equipped);
                if (stack != null && IsWorn(stack, db))
                {
                    return stack;
                }
            }

            ItemStack best = null;
            var bestRatio = 1f;
            foreach (var stack in state.Storage)
            {
                if (!IsWorn(stack, db) || !db.TryGetItem(stack.ItemId, out var def))
                {
                    continue;
                }

                var ratio = (float)stack.Durability / def.MaxDurability;
                if (ratio < bestRatio)
                {
                    best = stack;
                    bestRatio = ratio;
                }
            }

            return best;
        }

        private static bool IsWorn(ItemStack stack, IDefinitionDatabase db) =>
            stack.Count > 0 && stack.Durability > 0 && db.TryGetItem(stack.ItemId, out var def)
            && def.MaxDurability > 0 && stack.Durability < def.MaxDurability;

        // ───────────── 안쪽 ─────────────

        private static void ChargeVitals(GameState state, BalanceConfig balance, int fatigue, int stress, StateDelta delta)
        {
            EffectApplier.ChangeVital(state, VitalType.Fatigue, fatigue, balance, delta);
            EffectApplier.ChangeVital(state, VitalType.Stress, StatRules.ScaleStress(stress, state.Player.SleepDebt, balance), balance, delta);
        }

        /// <summary>칸마다 능력치 성장·숙련 경험 (교육과 같은 배율 규칙, 실패면 경험 없음).</summary>
        private static void ApplyGains(GameState state, IDefinitionDatabase db, ActionDef recipe, CheckResult result, StateDelta delta)
        {
            var balance = db.Balance;
            var multiplier = ActionResolver.GainMultiplier(state, recipe, result, 0f, balance) * ActionResolver.ExtraGainMultiplier(state, recipe, db);
            foreach (var gain in recipe.StatGains)
            {
                var amount = StatRules.ScaleGain(state.Player.GetStat(gain.Stat), gain.Amount, multiplier, balance);
                EffectApplier.ChangeStat(state, gain.Stat, amount, balance, delta);
            }

            if (recipe.HasProficiency && result != CheckResult.Failure)
            {
                var xp = recipe.ProficiencyXp + (result == CheckResult.GreatSuccess ? balance.GreatSuccessBonusXp : 0);
                ActionResolver.AddProficiencyXp(state, recipe.Proficiency, xp, balance, delta);
            }
        }

        private static void PayResources(GameState state, IDefinitionDatabase db, ActionDef recipe, CraftJob job, StateDelta delta)
        {
            foreach (var cost in recipe.ResourceCosts)
            {
                var amount = SpecialRules.Cost(state, db.Balance, cost);
                if (amount <= 0)
                {
                    continue;
                }

                EffectApplier.ChangeResource(state, cost.Resource, -amount, delta);
                job.PaidResources.TryGetValue(cost.Resource, out var paid);
                job.PaidResources[cost.Resource] = paid + amount;
            }
        }

        /// <summary>앞 단계 물건을 하나씩 뺀다: 내구가 가장 낮은 묶음부터 (돌려줄 때 그 내구 그대로).</summary>
        private static void PayItems(GameState state, IDefinitionDatabase db, CraftDef craft, CraftJob job, StateDelta delta)
        {
            foreach (var cost in craft.ItemCosts)
            {
                db.TryGetItem(cost.ItemId, out var def);
                var max = def?.MaxDurability ?? 0;
                for (var n = 0; n < Math.Max(1, cost.Count); n++)
                {
                    ItemStack pick = null;
                    foreach (var stack in state.Storage)
                    {
                        if (stack.ItemId != cost.ItemId || stack.Count <= 0)
                        {
                            continue;
                        }

                        if (pick == null || Effective(stack, max) < Effective(pick, max))
                        {
                            pick = stack;
                        }
                    }

                    if (pick == null)
                    {
                        break;
                    }

                    job.PaidItems.Add(new ItemStack { ItemId = pick.ItemId, Count = 1, Durability = pick.Durability });
                    pick.Count--;
                    if (pick.Count <= 0)
                    {
                        state.Storage.Remove(pick);
                    }
                }

                delta.ItemsLost.Add(new ItemStack { ItemId = cost.ItemId, Count = Math.Max(1, cost.Count) });
                ClearLoadoutIfGone(state, cost.ItemId);
            }
        }

        private static int Effective(ItemStack stack, int max) => max > 0 && stack.Durability <= 0 ? max : stack.Durability;

        private static void ClearLoadoutIfGone(GameState state, string itemId)
        {
            if (LoadoutRules.CountInStorage(state, itemId) > 0)
            {
                return;
            }

            var loadout = state.Loadout;
            if (loadout.Backpack == itemId) loadout.Backpack = "";
            if (loadout.MainWeapon == itemId) loadout.MainWeapon = "";
            if (loadout.Tool == itemId) loadout.Tool = "";
            if (loadout.Tool2 == itemId) loadout.Tool2 = "";
            if (loadout.Armor == itemId) loadout.Armor = "";
        }

        /// <summary>자원은 비율만큼(내림), 앞 단계 물건은 모두 돌려준다.</summary>
        private static void Refund(GameState state, CraftJob job, float resourceRate, StateDelta delta)
        {
            foreach (var paid in job.PaidResources.OrderBy(p => (int)p.Key))
            {
                var back = (int)Math.Floor(paid.Value * resourceRate);
                if (back > 0)
                {
                    EffectApplier.ChangeResource(state, paid.Key, back, delta);
                }
            }

            foreach (var item in job.PaidItems)
            {
                state.Storage.Add(new ItemStack { ItemId = item.ItemId, Count = item.Count, Durability = item.Durability });
                delta.ItemsGained.Add(new ItemStack { ItemId = item.ItemId, Count = item.Count });
            }
        }

        /// <summary>착수에 실패한 칸 뒤의 "제작 중" 칸을 쉬는 칸으로 바꾼다 (실행 중이라 잠금은 그대로).</summary>
        private static void FreeLockedAfter(GameState state, BalanceConfig balance, int slotIndex, int count)
        {
            var slots = state.Plan.Slots;
            for (var k = 1; k <= count && slotIndex + k < slots.Count; k++)
            {
                var slot = slots[slotIndex + k];
                if (slot.ActionId == balance.CraftContinueActionId)
                {
                    slot.ActionId = PlanPreparer.RestIdFor(slotIndex + k, balance);
                    slot.CraftLocked = false;
                }
            }
        }
    }
}
