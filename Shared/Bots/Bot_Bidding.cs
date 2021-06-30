﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public partial class Player
    {
        protected virtual Bid DetermineBid()
        {
            LogInfo("DetermineBid()");

            var otherPlayers = Game.Players.Where(p => p != this && p.MayBidOnCards);
            var biddingPlayers = otherPlayers.Where(p => p.TreacheryCards.Count() < p.MaximumNumberOfCards);
            int maxBidOfOtherPlayer = !biddingPlayers.Any() ? 0 : biddingPlayers.Max(p => p.Resources);

            int currentBid = Game.CurrentBid == null ? 0 : Game.CurrentBid.TotalAmount;
            bool currentBidIsFromAlly = Game.CurrentBid != null && Game.CurrentBid.Initiator == Ally;
            bool isKnownCard = Game.HasBiddingPrescience(this) || Game.KnownCards(this).Contains(Game.CardsOnAuction.Top);
            bool thisCardIsUseless = isKnownCard && !MayUseUselessAsKarma && Game.CardsOnAuction.Top.Type == TreacheryCardType.Useless;
            bool thisCardIsCrappy = isKnownCard && !WannaHave(Game.CardsOnAuction.Top);
            bool thisCardIsPerfect = isKnownCard && CardQuality(Game.CardsOnAuction.Top) == 5;
            int resourcesToKeep = thisCardIsPerfect ? Param.Bidding_ResourcesToKeepWhenCardIsPerfect : Param.Bidding_ResourcesToKeepWhenCardIsntPerfect;
            int resourcesAvailable = Math.Max(0, ResourcesIncludingAllyAndRedContribution - resourcesToKeep);
            bool couldUseKarmaForBid = Ally != Faction.Red && (SpecialKarmaPowerUsed || !Param.Karma_SaveCardToUseSpecialKarmaAbility);
            var karmaCardToUseForBidding = couldUseKarmaForBid && (currentBid > 4 || resourcesAvailable <= currentBid) ? TreacheryCards.FirstOrDefault(c => c.Type == TreacheryCardType.Karma || MayUseUselessAsKarma && c.Type == TreacheryCardType.Useless) : null;
            int karmaWorth = (karmaCardToUseForBidding == null ? 0 : 8);
            int maximumIWillSpend = D(1, resourcesAvailable + karmaWorth);

            LogInfo("currentBidIsFromAlly: {0}, thisCardIsUseless: {1}, thisCardIsCrappy: {2}, resourcesAvailable:, {3}, karmaWorth: {4}, maximumIWillSpend: {5}, karmaCardToUseForBidding: {6}.",
                currentBidIsFromAlly, thisCardIsUseless, thisCardIsCrappy, resourcesAvailable, karmaWorth, maximumIWillSpend, karmaCardToUseForBidding);

            if (currentBidIsFromAlly || thisCardIsUseless && currentBid > 0 || thisCardIsCrappy && D(1, 1 + 2 * currentBid) > 1)
            {
                LogInfo("currentBidIsFromAlly or thisCardIsUseless or thisCardIsCrappy");
                return PassedBid();
            }
            else if (currentBid == 0 && ResourcesIncludingAllyAndRedContribution > 0)
            {
                LogInfo("always bid at least 1 if possible");
                return CreateBidUsingAllyAndRedSpice(1, 0, null);
            }
            else if (Ally != Faction.Red && currentBid > Param.Bidding_PassingTreshold + maximumIWillSpend)
            {
                LogInfo("Ally != Faction.Red && currentBid ({0}) > Param.Bidding_PassingTreshold ({1}) + maximumIWillSpend (D(1,{2}+{3}) => {4})",
                    currentBid,
                    Param.Bidding_PassingTreshold,
                    resourcesAvailable,
                    karmaWorth,
                    maximumIWillSpend);

                return PassedBid();
            }
            else if (currentBid + 2 == maxBidOfOtherPlayer && (karmaCardToUseForBidding != null || currentBid + 2 <= resourcesAvailable))
            {
                LogInfo("currentBid ({0}) + 2 == maxBidOfOtherPlayer ({1}) && (karmaCardToUseForBidding ({2}) != null || currentBid + 2 <= resourcesAvailable ({3}))",
                    currentBid,
                    maxBidOfOtherPlayer,
                    karmaCardToUseForBidding,
                    resourcesAvailable
                    );

                return CreateBidUsingAllyAndRedSpice(maxBidOfOtherPlayer, resourcesToKeep, karmaCardToUseForBidding);
            }
            else if (currentBid + 1 <= resourcesAvailable || karmaCardToUseForBidding != null)
            {
                LogInfo("currentBid ({0}) + 1 <= resourcesAvailable ({1}) || karmaCardToUseForBidding ({2}) != null",
                    currentBid,
                    resourcesAvailable,
                    karmaCardToUseForBidding);

                return CreateBidUsingAllyAndRedSpice(currentBid + 1, resourcesToKeep, karmaCardToUseForBidding);
            }
            else
            {
                LogInfo("Not enough spice available for bid");
                return PassedBid();
            }
        }

        private Bid PassedBid()
        {
            return new Bid(Game) { Initiator = Faction, Passed = true };
        }

        protected virtual Bid CreateBidUsingAllyAndRedSpice(int amount, int spiceToKeep, TreacheryCard karmaCard)
        {
            if (karmaCard == null)
            {
                int spiceLeftToPay = amount;
                int redContribution = Math.Min(spiceLeftToPay, Game.SpiceForBidsRedCanPay(Faction));
                spiceLeftToPay -= redContribution;

                int allyContribution = Math.Min(spiceLeftToPay, Game.GetPermittedUseOfAllySpice(Faction));
                spiceLeftToPay -= allyContribution;

                return new Bid(Game) { Initiator = Faction, Amount = spiceLeftToPay, AllyContributionAmount = allyContribution, RedContributionAmount = redContribution, KarmaBid = false, KarmaCard = null, Passed = false };
            }
            else
            {
                return new Bid(Game) { Initiator = Faction, Amount = amount, KarmaBid = false, KarmaCard = karmaCard, Passed = false };
            }
        }

        protected bool WannaHave(TreacheryCard c)
        {
            return
                c.IsLaser ||
                c.Type == TreacheryCardType.ProjectileAndPoison ||
                c.Type == TreacheryCardType.ShieldAndAntidote ||
                c.Type == TreacheryCardType.Chemistry ||
                c.Type == TreacheryCardType.WeirdingWay ||
                c.Type == TreacheryCardType.ArtilleryStrike ||
                c.Type == TreacheryCardType.PoisonTooth ||
                c.Type == TreacheryCardType.Amal || 
                c.IsProjectileWeapon && !TreacheryCards.Any(c => c.IsProjectileWeapon) ||
                c.IsPoisonWeapon && !TreacheryCards.Any(c => c.IsPoisonWeapon && c.Type != TreacheryCardType.Chemistry) ||
                c.IsProjectileDefense && !TreacheryCards.Any(c => c.IsProjectileDefense && c.Type != TreacheryCardType.WeirdingWay) ||
                c.IsPoisonDefense && !TreacheryCards.Any(c => c.IsPoisonDefense) ||
                c.Type == TreacheryCardType.Karma && (Faction == Faction.Black || TreacheryCards.Count() < MaximumNumberOfCards - 1) ||
                c.Type == TreacheryCardType.Mercenary && Leaders.Count(l => Game.IsAlive(l)) <= 1 ||
                c.Type == TreacheryCardType.RaiseDead && ForcesKilled > 7;
        }
    }
}