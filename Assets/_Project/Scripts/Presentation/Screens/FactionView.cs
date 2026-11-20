using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 3겹 세력 화면 (Docs/UI_REDESIGN_PROPOSAL.md): 위에는 대표 얼굴·이름·소개와 평판 눈금(적대 ~ 동맹, 지금 자리 표시),
    /// 지금 단계의 근황과 영향. 아래 왼쪽은 거래 카드(주는 것 → 받는 것, 아이콘·수량·지금 가진 양, 막힌 이유),
    /// 오른쪽은 최근 일. 거래하면 화면을 닫지 않고 그 자리에서 새로 그린다.
    /// </summary>
    internal sealed class FactionView
    {
        private static readonly (FactionTier Tier, int From, int To)[] Bands =
        {
            (FactionTier.Hostile, FactionRules.MinStanding, FactionRules.HostileMax),
            (FactionTier.Wary, FactionRules.HostileMax, FactionRules.WaryMax),
            (FactionTier.Neutral, FactionRules.WaryMax, FactionRules.FriendlyMin),
            (FactionTier.Friendly, FactionRules.FriendlyMin, FactionRules.AlliedMin),
            (FactionTier.Allied, FactionRules.AlliedMin, FactionRules.MaxStanding),
        };

        private readonly GameSession _session;
        private readonly VisualElement _sheet;
        private readonly VisualElement _portrait;
        private readonly VisualElement _intro;
        private readonly ScrollView _trades;
        private readonly ScrollView _log;
        private string _factionId;

        public FactionView(VisualElement root, GameSession session)
        {
            _session = session;
            _sheet = root.Q("faction-sheet");
            _portrait = root.Q("fac-portrait");
            _intro = root.Q("fac-intro");
            _trades = root.Q<ScrollView>("fac-trades");
            _log = root.Q<ScrollView>("fac-log");
            var close = root.Q<Button>("fac-close");
            close.text = Loc.T("hud.close");
            close.clicked += Hide;
        }

        /// <summary>거래했을 때 (허브가 알림과 화면 갱신).</summary>
        public Action<string> Traded { get; set; }

        public bool IsOpen => !_sheet.ClassListContains("hidden");

        public void Show(string factionId)
        {
            if (!FactionRules.TryGetDef(_session.Definitions, factionId, out _) || !_session.State.Factions.ContainsKey(factionId))
            {
                return;
            }

            _factionId = factionId;
            Build();
            _sheet.RemoveFromClassList("hidden");
        }

        public void Hide() => _sheet.AddToClassList("hidden");

        private void Build()
        {
            var state = _session.State;
            FactionRules.TryGetDef(_session.Definitions, _factionId, out var def);
            var faction = state.Factions[_factionId];
            var tier = FactionRules.TierOf(faction.Standing);
            var gone = FactionRules.IsGone(state, def);

            // ── 위: 얼굴·이름·소개·평판 ──
            var portrait = StoryArt.Faction(_factionId);
            _portrait.Clear();
            StoryArt.SetCover(_portrait, portrait);
            _portrait.EnableInClassList("empty", portrait == null);
            if (portrait == null)
            {
                var initial = new Label(Loc.T("faction." + _factionId + ".name").Substring(0, 1));
                initial.AddToClassList("fac-initial");
                _portrait.Add(initial);
            }

            _intro.Clear();
            var title = new VisualElement();
            title.AddToClassList("fac-title-row");
            Add(title, Loc.T("faction." + _factionId + ".name"), "fac-name");
            var chip = Add(title, gone ? Loc.T("faction.tier.gone") : Loc.T("faction.tier." + tier), "fac-tier");
            chip.AddToClassList("tier-" + (gone ? "gone" : tier.ToString().ToLowerInvariant()));
            _intro.Add(title);
            Add(_intro, Loc.T("faction." + _factionId + ".desc"), "fac-desc");
            _intro.Add(Gauge(faction.Standing, gone));
            Add(_intro, Loc.T(gone ? "faction." + _factionId + ".status.gone" : "faction." + _factionId + ".status." + tier), "fac-status");

            var effects = gone ? new List<FactionEffectDef>()
                : def.Effects.Where(e => tier >= e.MinTier && tier <= e.MaxTier && !string.IsNullOrEmpty(e.TextKey)).ToList();
            foreach (var effect in effects)
            {
                Add(_intro, "· " + Loc.T(effect.TextKey), "fac-effect");
            }

            // ── 아래 왼쪽: 거래 ──
            _trades.Clear();
            Add(_trades, Loc.T("faction.trade.title"), "fac-section");
            if (def.Trades.Count == 0)
            {
                Add(_trades, Loc.T("faction.trade.none"), "fac-dim");
            }

            foreach (var offer in def.Trades)
            {
                _trades.Add(TradeCard(def, offer));
            }

            // ── 아래 오른쪽: 최근 일 ──
            _log.Clear();
            Add(_log, Loc.T("faction.card.log"), "fac-section");
            if (faction.Log.Count == 0)
            {
                Add(_log, Loc.T("faction.log.empty"), "fac-dim");
            }

            foreach (var entry in faction.Log.AsEnumerable().Reverse())
            {
                var text = string.IsNullOrEmpty(entry.TextKey) ? Loc.T("faction.log.drift") : Loc.T(entry.TextKey);
                var amount = entry.Standing == 0 ? "" : "  (" + UiText.Signed(entry.Standing) + ")";
                var row = new VisualElement();
                row.AddToClassList("fac-log-row");
                Add(row, Loc.F("faction.log.week", entry.Week), "fac-log-week");
                Add(row, text + amount, "fac-log-text");
                _log.Add(row);
            }

            KoreanFont.ApplyDeep(_sheet);
        }

        /// <summary>평판 눈금: 다섯 단계 띠 위에 지금 자리.</summary>
        private static VisualElement Gauge(int standing, bool gone)
        {
            var gauge = new VisualElement();
            gauge.AddToClassList("fac-gauge");
            var span = (float)(FactionRules.MaxStanding - FactionRules.MinStanding);
            foreach (var band in Bands)
            {
                var part = new VisualElement();
                part.AddToClassList("fac-band");
                part.AddToClassList("tier-" + band.Tier.ToString().ToLowerInvariant());
                part.style.width = Length.Percent(100f * (band.To - band.From) / span);
                var label = new Label(Loc.T("faction.tier." + band.Tier));
                label.AddToClassList("fac-band-label");
                part.Add(label);
                gauge.Add(part);
            }

            if (!gone)
            {
                var marker = new VisualElement();
                marker.AddToClassList("fac-marker");
                marker.style.left = Length.Percent(100f * (standing - FactionRules.MinStanding) / span);
                var value = new Label(UiText.Signed(standing));
                value.AddToClassList("fac-marker-value");
                marker.Add(value);
                gauge.Add(marker);
            }

            return gauge;
        }

        private VisualElement TradeCard(FactionDef def, TradeOfferDef offer)
        {
            var blocker = _session.TradeBlocker(def.Id, offer.Id);
            var card = new VisualElement();
            card.AddToClassList("fac-trade");
            card.EnableInClassList("blocked", blocker != null);

            var row = new VisualElement();
            row.AddToClassList("fac-trade-row");
            row.Add(Side(offer.Give, true));
            Add(row, "→", "fac-arrow");
            row.Add(Side(offer.Get, false));
            card.Add(row);

            var notes = new List<string>();
            if (offer.Delayed)
            {
                notes.Add(Loc.T("faction.trade.delayed"));
            }

            if (offer.StandingChange != 0)
            {
                notes.Add(Loc.F("faction.trade.standing", UiText.Signed(offer.StandingChange)));
            }

            notes.Add(Loc.F("faction.trade.min_tier", Loc.T("faction.tier." + offer.MinTier)));
            Add(card, string.Join("  ·  ", notes), "fac-trade-note");
            Add(card, blocker == null ? Loc.T("trade." + def.Id + "." + offer.Id) : Loc.T(blocker), blocker == null ? "fac-trade-desc" : "fac-trade-blocker");

            var button = new Button(() =>
            {
                if (_session.Trade(def.Id, offer.Id))
                {
                    Traded?.Invoke(Loc.T("trade." + def.Id + "." + offer.Id + ".log"));
                    Build();
                }
            }) { text = Loc.T("faction.trade.do") };
            button.AddToClassList("fac-trade-button");
            button.SetEnabled(blocker == null);
            card.Add(button);
            return card;
        }

        /// <summary>주는 것 / 받는 것: 자원 아이콘 + 수량 (주는 쪽은 지금 가진 양도).</summary>
        private VisualElement Side(List<ResourceAmount> amounts, bool give)
        {
            var side = new VisualElement();
            side.AddToClassList("fac-side");
            Add(side, Loc.T(give ? "faction.trade.give" : "faction.trade.get"), "fac-side-label");
            var chips = new VisualElement();
            chips.AddToClassList("fac-chips");
            foreach (var amount in amounts)
            {
                var have = _session.State.Base.GetResource(amount.Resource);
                var text = UiText.Resource(amount.Resource) + " " + amount.Amount + (give ? " / " + have : "");
                var chip = Add(chips, text, "fac-chip");
                chip.EnableInClassList("short", give && have < amount.Amount);
                StoryArt.SetIcon(chip, StoryArt.Icon("res." + amount.Resource), 22f);
            }

            side.Add(chips);
            return side;
        }

        private static Label Add(VisualElement parent, string text, string cssClass)
        {
            var label = new Label(text);
            label.AddToClassList(cssClass);
            parent.Add(label);
            return label;
        }
    }
}
