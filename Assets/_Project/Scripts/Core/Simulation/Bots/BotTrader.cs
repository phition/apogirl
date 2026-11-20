using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Simulation.Bots
{
    /// <summary>
    /// 봇의 세력 거래 (계획 직전, 세력마다 주 1회): 모자란 자원을 주는 거래가 있고, 내줄 것이 넉넉하면 한다.
    /// </summary>
    public static class BotTrader
    {
        /// <summary>이보다 적으면 그 자원이 모자라다고 본다.</summary>
        private static int ShortBelow(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Food: return 10;
                case ResourceType.Water: return 10;
                case ResourceType.Medicine: return 2;
                case ResourceType.Fuel: return 3;
                default: return 0;
            }
        }

        /// <summary>내줄 자원은 이만큼은 남겨 둔다.</summary>
        private static int Keep(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Food: return 8;
                case ResourceType.Water: return 8;
                case ResourceType.Medicine: return 1;
                case ResourceType.Fuel: return 1;
                default: return 0;
            }
        }

        public static int Trade(GameState state, IDefinitionDatabase db)
        {
            var trades = 0;
            var list = db?.Extras?.Factions;
            if (list == null)
            {
                return 0;
            }

            foreach (var def in list)
            {
                foreach (var offer in def.Trades)
                {
                    if (FactionRules.TradeBlocker(state, db, def.Id, offer.Id) != null || !Worth(state, offer))
                    {
                        continue;
                    }

                    if (FactionRules.Trade(state, db, def.Id, offer.Id, new StateDelta()))
                    {
                        trades++;
                        break;
                    }
                }
            }

            return trades;
        }

        private static bool Worth(GameState state, TradeOfferDef offer)
        {
            var needed = false;
            foreach (var get in offer.Get)
            {
                needed |= state.Base.GetResource(get.Resource) < ShortBelow(get.Resource);
            }

            if (!needed)
            {
                return false;
            }

            foreach (var give in offer.Give)
            {
                if (state.Base.GetResource(give.Resource) - give.Amount < Keep(give.Resource))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
