﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public class Karma : GameEvent
    {
        public int _cardId;
        public FactionAdvantage Prevented;

        public Karma(Game game) : base(game)
        {
        }

        public Karma()
        {
        }

        [JsonIgnore]
        public TreacheryCard Card
        {
            get
            {
                return TreacheryCardManager.Get(_cardId);
            }
            set
            {
                _cardId = TreacheryCardManager.GetId(value);
            }
        }

        public override string Validate()
        {
            return "";
        }

        protected override void ExecuteConcreteEvent()
        {
            Game.HandleEvent(this);
        }

        public override Message GetMessage()
        {
            if (Card.Type == TreacheryCardType.Karma)
            {
                return new Message(Initiator, "{0} play a {1} card.", Initiator, TreacheryCardType.Karma);
            }
            else
            {
                return new Message(Initiator, "{0} play {1} as a {2} card.", Initiator, Card, TreacheryCardType.Karma);
            }
        }

        public static IEnumerable<FactionAdvantage> ValidFactionAdvantages(Game g, Player p)
        {
            var result = new List<FactionAdvantage>
            {
                FactionAdvantage.None
            };

            if (p.Faction != Faction.Green && g.IsPlaying(Faction.Green))
            {
                result.Add(FactionAdvantage.GreenBattlePlanPrescience);
                result.Add(FactionAdvantage.GreenBiddingPrescience);
                result.Add(FactionAdvantage.GreenSpiceBlowPrescience);
                if (g.Applicable(Rule.GreenMessiah)) result.Add(FactionAdvantage.GreenUseMessiah);
            }

            if (p.Faction != Faction.Black && g.IsPlaying(Faction.Black))
            {
                if (g.Applicable(Rule.BlackCapturesOrKillsLeaders)) result.Add(FactionAdvantage.BlackCaptureLeader);
                result.Add(FactionAdvantage.BlackFreeCard);
                result.Add(FactionAdvantage.BlackCallTraitorForAlly);
            }

            if (p.Faction != Faction.Yellow && g.IsPlaying(Faction.Yellow))
            {
                result.Add(FactionAdvantage.YellowControlsMonster);
                if (g.Applicable(Rule.YellowSeesStorm)) result.Add(FactionAdvantage.YellowStormPrescience);
                if (g.Applicable(Rule.YellowSpecialForces)) result.Add(FactionAdvantage.YellowSpecialForceBonus);
                result.Add(FactionAdvantage.YellowExtraMove);
                result.Add(FactionAdvantage.YellowProtectedFromMonster);
                if (g.Applicable(Rule.YellowStormLosses)) result.Add(FactionAdvantage.YellowProtectedFromStorm);
                if (g.Applicable(Rule.AdvancedCombat)) result.Add(FactionAdvantage.YellowNotPayingForBattles);
            }

            if (p.Faction != Faction.Red && g.IsPlaying(Faction.Red))
            {
                if (g.Applicable(Rule.RedSpecialForces)) result.Add(FactionAdvantage.RedSpecialForceBonus);
                result.Add(FactionAdvantage.RedReceiveBid);
                result.Add(FactionAdvantage.RedGiveSpiceToAlly);
                result.Add(FactionAdvantage.RedLetAllyReviveExtraForces);
            }

            if (p.Faction != Faction.Orange && g.IsPlaying(Faction.Orange))
            {
                if (g.Applicable(Rule.OrangeDetermineShipment)) result.Add(FactionAdvantage.OrangeDetermineMoveMoment);
                result.Add(FactionAdvantage.OrangeSpecialShipments);
                result.Add(FactionAdvantage.OrangeShipmentsDiscount);
                result.Add(FactionAdvantage.OrangeShipmentsDiscountAlly);
                result.Add(FactionAdvantage.OrangeReceiveShipment);
            }

            if (p.Faction != Faction.Blue && g.IsPlaying(Faction.Blue))
            {
                result.Add(FactionAdvantage.BlueAccompanies);
                result.Add(FactionAdvantage.BlueUsingVoice);
                if (g.Applicable(Rule.BlueWorthlessAsKarma)) result.Add(FactionAdvantage.BlueWorthlessAsKarma);
                if (g.Applicable(Rule.BlueAdvisors)) result.Add(FactionAdvantage.BlueAnnouncesBattle);
                if (g.Applicable(Rule.BlueAdvisors)) result.Add(FactionAdvantage.BlueNoFlipOnIntrusion);
                if (g.Applicable(Rule.BlueAutoCharity)) result.Add(FactionAdvantage.BlueCharity);
            }

            if (p.Faction != Faction.Grey && g.IsPlaying(Faction.Grey))
            {
                result.Add(FactionAdvantage.GreyMovingHMS);
                result.Add(FactionAdvantage.GreySpecialForceBonus);
                result.Add(FactionAdvantage.GreySelectingCardsOnAuction);
                result.Add(FactionAdvantage.GreyCyborgExtraMove);
                result.Add(FactionAdvantage.GreyReplacingSpecialForces);
                result.Add(FactionAdvantage.GreyAllyDiscardingCard);
                if (g.Applicable(Rule.GreyAndPurpleExpansionGreySwappingCardOnBid)) result.Add(FactionAdvantage.GreySwappingCard);
            }

            if (p.Faction != Faction.Purple && g.IsPlaying(Faction.Purple))
            {
                result.Add(FactionAdvantage.PurpleRevivalDiscount);
                result.Add(FactionAdvantage.PurpleRevivalDiscountAlly);
                result.Add(FactionAdvantage.PurpleReplacingFaceDancer);
                result.Add(FactionAdvantage.PurpleIncreasingRevivalLimits);
                result.Add(FactionAdvantage.PurpleReceiveRevive);
                result.Add(FactionAdvantage.PurpleEarlyLeaderRevive);
                if (g.Applicable(Rule.GreyAndPurpleExpansionPurpleGholas)) result.Add(FactionAdvantage.PurpleReviveGhola);
            }

            return result;
        }

        public static IEnumerable<TreacheryCard> ValidKarmaCards(Game g, Player p)
        {
            return
                p.TreacheryCards.Where(c => c.Type == TreacheryCardType.Karma ||
                (p.Is(Faction.Blue) && c.Type == TreacheryCardType.Useless && g.Applicable(Rule.BlueWorthlessAsKarma)));
        }

        public static IEnumerable<TreacheryCard> ValidKarmaCardsForShipment(Game g, Player p)
        {
            var result = ValidKarmaCards(g, p).ToList();

            if (g.GetPermittedUseOfAllyKarma(p.Faction) != null)
            {
                result.Add(g.GetPermittedUseOfAllyKarma(p.Faction));
            }

            return result;
        }

        public static bool CanKarma(Game g, Player p)
        {
            return
                p.TreacheryCards.Any(c => c.Type == TreacheryCardType.Karma ||
                (p.Is(Faction.Blue) && c.Type == TreacheryCardType.Useless && g.Applicable(Rule.BlueWorthlessAsKarma)));
        }

        public static bool CanKarmaShipment(Game g, Player p)
        {
            return ValidKarmaCardsForShipment(g, p).Any();
        }
    }
}