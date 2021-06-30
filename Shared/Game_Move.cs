﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public partial class Game
    {
        public Location LastShippedOrMovedTo { get; private set; }

        public bool ShipsTechTokenIncome;
        public List<PlacementEvent> RecentMoves = new List<PlacementEvent>();
        private List<Faction> FactionsWithOrnithoptersAtStartOfMovement;
        private bool BeginningOfShipmentAndMovePhase;

        private void EnterShipmentAndMovePhase()
        {
            FactionsWithOrnithoptersAtStartOfMovement = Players.Where(p => OccupiesArrakeenOrCarthag(p)).Select(p => p.Faction).ToList();
            CurrentMainPhase = MainPhase.ShipmentAndMove;
            CurrentReport = new Report(MainPhase.ShipmentAndMove);
            RecentMoves.Clear();
            ReceiveGraveyardTechIncome();
            BeginningOfShipmentAndMovePhase = true;

            if (Version >= 46)
            {
                FactionsWithIncreasedRevivalLimits = new Faction[] { };
                AllowedEarlyRevivals.Clear();
            }

            ShipsTechTokenIncome = false;
            Allow(FactionAdvantage.BlueAnnouncesBattle);
            Allow(FactionAdvantage.RedLetAllyReviveExtraForces);
            Allow(FactionAdvantage.PurpleReceiveRevive);
            HasActedOrPassed.Clear();
            LastShippedOrMovedTo = null;

            ShipmentAndMoveSequence.Start(this, false);

            if (ShipmentAndMoveSequence.CurrentFaction == Faction.Orange && OrangeDeterminesMoveMoment)
            {
                ShipmentAndMoveSequence.NextPlayer(this, false);
            }

            Enter(IsPlaying(Faction.Orange) && OrangeDeterminesMoveMoment, Phase.OrangeShip, Phase.NonOrangeShip);
        }

        public bool OccupiesArrakeenOrCarthag(Player p)
        {
            return p.Occupies(Map.Arrakeen) || p.Occupies(Map.Carthag);
        }

        private void ReceiveGraveyardTechIncome()
        {
            if (RevivalTechTokenIncome)
            {
                var techTokenOwner = Players.FirstOrDefault(p => p.TechTokens.Contains(TechToken.Graveyard));
                if (techTokenOwner != null)
                {
                    var amount = techTokenOwner.TechTokens.Count;
                    techTokenOwner.Resources += amount;
                    CurrentReport.Add(techTokenOwner.Faction, "{0} receive {1} from {2}.", techTokenOwner.Faction, amount, TechToken.Graveyard);
                }
            }
        }

        public void HandleEvent(OrangeDelay e)
        {
            BeginningOfShipmentAndMovePhase = false;
            CurrentReport.Add(e.GetMessage());
            Enter(EveryoneButOneActedOrPassed, Phase.ShipmentAndMoveConcluded, Phase.NonOrangeShip);
        }

        private bool BGMayAccompany;
        private List<Territory> ChosenDestinationsWithAllies = new List<Territory>();

        public void HandleEvent(Shipment s)
        {
            BeginningOfShipmentAndMovePhase = false;
            StormLossesToTake.Clear();
            ChosenDestinationsWithAllies.Clear();

            BGMayAccompany = false;
            var initiator = GetPlayer(s.Initiator);

            MessagePart orangeIncome = new MessagePart("");
            ProcessOrangeIncome(s, initiator, ref orangeIncome);
            CurrentReport.Add(s.GetVerboseMessage(orangeIncome));

            if (!s.Passed)
            {
                if (ContainsConflictingAlly(initiator, s.To))
                {
                    ChosenDestinationsWithAllies.Add(s.To.Territory);
                }

                LastShippedOrMovedTo = s.To;
                bool mustBeAdvisors = (initiator.Is(Faction.Blue) && initiator.SpecialForcesIn(s.To) > 0);

                if (s.IsSiteToSite)
                {
                    PerformSiteToSiteShipment(s, initiator);
                }
                else if (s.IsBackToReserves)
                {
                    PerformRetreatShipment(s, initiator);
                }
                else
                {
                    PerformNormalShipment(s, initiator);
                }

                if (s.Initiator != Faction.Yellow)
                {
                    RecentMilestones.Add(Milestone.Shipment);
                }

                if (Version >= 89 || mustBeAdvisors) initiator.FlipForces(s.To, mustBeAdvisors);
                PayForShipment(s, initiator);
                DetermineNextShipmentAndMoveSubPhase(DetermineIntrusionCaused(s), BGMayAccompany);
                FlipBeneGesseritWhenAlone();
            }
            else
            {
                DetermineNextShipmentAndMoveSubPhase(false, BGMayAccompany);
            }
        }

        public bool ContainsConflictingAlly(Player initiator, Location to)
        {
            if (initiator.Ally == Faction.None || to == Map.PolarSink || to == null) return false;

            var ally = initiator.AlliedPlayer;

            if (initiator.Ally == Faction.Blue && Applicable(Rule.AdvisorsDontConflictWithAlly)) {

                return ally.ForcesIn(to.Territory) > 0;
            }
            else
            {
                return ally.AnyForcesIn(to.Territory) > 0;
            }
        }

        private void PayForShipment(Shipment s, Player initiator)
        {
            var costToInitiator = s.DetermineCostToInitiator();
            initiator.Resources -= costToInitiator;

            if (s.UsingKarma(this))
            {
                var karmaCard = s.GetKarmaCard(this, initiator);
                Discard(karmaCard);
                RecentMilestones.Add(Milestone.Karma);
            }

            if (s.AllyContributionAmount > 0)
            {
                GetPlayer(initiator.Ally).Resources -= s.AllyContributionAmount;
                if (Version >= 76) DecreasePermittedUseOfAllySpice(initiator.Faction, s.AllyContributionAmount);
            }
        }

        private void ProcessOrangeIncome(Shipment s, Player initiator, ref MessagePart orangeIncome)
        {
            var guild = GetPlayer(Faction.Orange);

            int guildProfits;
            if (guild != null && initiator != guild && !s.UsingKarma(this) && (guildProfits = s.DetermineOrangeProfits(this)) > 0)
            {
                if (!Prevented(FactionAdvantage.OrangeReceiveShipment))
                {
                    guild.Resources += guildProfits;

                    if (guildProfits > 0)
                    {
                        orangeIncome = new MessagePart(" {0} receive {1}.", Faction.Orange, guildProfits);
                    }
                }
                else
                {
                    orangeIncome = new MessagePart(" {0} prevents {1} from receiving {2}.", TreacheryCardType.Karma, Faction.Orange, Concept.Resource);
                    if (!Applicable(Rule.FullPhaseKarma)) Allow(FactionAdvantage.OrangeReceiveShipment);
                }
            }
        }

        private void PerformRetreatShipment(Shipment s, Player initiator)
        {
            initiator.ShipForces(s.To, s.ForceAmount);
        }

        private void PerformSiteToSiteShipment(Shipment s, Player initiator)
        {
            initiator.MoveForces(s.From, s.To, s.ForceAmount);
            initiator.MoveSpecialForces(s.From, s.To, s.SpecialForceAmount);
        }

        private void PerformNormalShipment(Shipment s, Player initiator)
        {
            BGMayAccompany = BlueMayAccompany(s);
            initiator.ShipForces(s.To, s.ForceAmount);
            initiator.ShipSpecialForces(s.To, s.SpecialForceAmount);

            /*if (initiator.Faction != Faction.Blue && initiator.SpecialForcesIn(s.To) > 0)
            {
                initiator.FlipForces(s.To, true);
            }*/

            if (initiator.Is(Faction.Yellow) && IsInStorm(s.To))
            {
                int killCount;
                if (!Prevented(FactionAdvantage.YellowProtectedFromStorm) && Applicable(Rule.YellowStormLosses))
                {
                    killCount = 0;
                    StormLossesToTake.Add(new Tuple<Location, int>(s.To, LossesToTake(s.ForceAmount, s.SpecialForceAmount)));
                }
                else
                {
                    killCount = initiator.KillForces(s.To, s.ForceAmount, s.SpecialForceAmount, false);
                }

                if (killCount > 0)
                {
                    CurrentReport.Add(initiator.Faction, "{0} {1} {2} are killed by the storm.", killCount, initiator.Faction, initiator.Force);
                }
            }

            if (initiator.Faction != Faction.Yellow && initiator.Faction != Faction.Orange && !s.IsSiteToSite)
            {
                ShipsTechTokenIncome = true;
            }
        }

        private bool BlueMayAccompany(Shipment s)
        {
            return (s.ForceAmount > 0 || s.SpecialForceAmount > 0) && s.Initiator != Faction.Yellow && s.Initiator != Faction.Blue;
        }

        public bool MayShipAsGuild(Player p)
        {
            return ((!Prevented(FactionAdvantage.OrangeSpecialShipments) && p.Is(Faction.Orange)) || p.Ally == Faction.Orange && OrangeAllyMayShipAsGuild);
        }

        public bool MayShipWithDiscount(Player p)
        {
            return (p.Is(Faction.Orange) && !Prevented(FactionAdvantage.OrangeShipmentsDiscount)) ||
                   (p.Ally == Faction.Orange && OrangeAllyMayShipAsGuild && !Prevented(FactionAdvantage.OrangeShipmentsDiscountAlly));
        }

        public bool BlueMustBeFighterIn(Location l)
        {
            if (l == null) return true;

            var benegesserit = GetPlayer(Faction.Blue);
            return benegesserit.Occupies(l.Territory) || !IsOccupied(l.Territory) || !Applicable(Rule.BlueAdvisors);
        }

        public void HandleEvent(BlueAccompanies c)
        {
            var benegesserit = GetPlayer(c.Initiator);
            if (c.Accompanies && benegesserit.ForcesInReserve > 0)
            {
                if (BlueMustBeFighterIn(c.Location))
                {
                    benegesserit.ShipForces(c.Location, 1);
                    CurrentReport.Add(c.Initiator, "{0} ship a fighter to {1}.", c.Initiator, c.Location);
                }
                else
                {
                    benegesserit.ShipAdvisors(c.Location, 1);
                    CurrentReport.Add(c.Initiator, "{0} ship an advisor to {1}.", c.Initiator, c.Location);
                }
            }
            else
            {

                CurrentReport.Add(c.Initiator, "{0} don't accompany this shipment.", c.Initiator);
            }

            DetermineNextShipmentAndMoveSubPhase(false, BGMayAccompany);
        }

        public void HandleEvent(Move m)
        {
            RecentMoves.Add(m);

            StormLossesToTake.Clear();
            var initiator = GetPlayer(m.Initiator);
            HasActedOrPassed.Add(m.Initiator);

            if (CurrentPhase == Phase.NonOrangeMove)
            {
                ShipmentAndMoveSequence.NextPlayer(this, false);

                if (ShipmentAndMoveSequence.CurrentFaction == Faction.Orange && OrangeDeterminesMoveMoment)
                {
                    ShipmentAndMoveSequence.NextPlayer(this, false);
                }
            }

            bool intrusionCaused = false;
            if (!m.Passed)
            {
                if (ContainsConflictingAlly(initiator, m.To))
                {
                    ChosenDestinationsWithAllies.Add(m.To.Territory);
                }

                PerformMoveFromLocations(initiator, m.ForceLocations, m.To, m.Initiator != Faction.Blue || m.AsAdvisors, false);
                intrusionCaused = DetermineIntrusionCaused(m);
            }
            else
            {
                CurrentReport.Add(m.Initiator, "{0} pass movement.", m.Initiator);
            }

            DetermineNextShipmentAndMoveSubPhase(intrusionCaused, false);
            CheckIfForcesShouldBeDestroyedByAllyPresence(initiator);

            if (Version >= 87)
            {
                FlipBeneGesseritWhenAlone();
            }

            if (!Applicable(Rule.FullPhaseKarma)) Allow(FactionAdvantage.YellowExtraMove);
            if (!Applicable(Rule.FullPhaseKarma)) Allow(FactionAdvantage.GreyCyborgExtraMove);
        }

        private bool DetermineIntrusionCaused(PlacementEvent m)
        {
            return DetermineIntrusionCaused(m.Initiator, m.ForceLocations.Values.Sum(b => b.AmountOfForces) + m.ForceLocations.Values.Sum(b => b.AmountOfSpecialForces), m.To.Territory);
        }

        private bool DetermineIntrusionCaused(Shipment s)
        {
            return DetermineIntrusionCaused(s.Initiator, s.ForceAmount + s.SpecialForceAmount, s.To.Territory);
        }

        private bool DetermineIntrusionCaused(Faction initiator, int nrOfForces, Territory territory)
        {
            var bgPlayer = GetPlayer(Faction.Blue);

            return
                (Version <= 72 || territory != Map.PolarSink.Territory) &&
                Applicable(Rule.BlueAdvisors) &&
                bgPlayer != null &&
                initiator != bgPlayer.Ally &&
                initiator != Faction.Blue &&
                nrOfForces > 0 &&
                bgPlayer.ForcesIn(territory) > 0;
        }

        public Phase PausedPhase { get; set; }
        public void HandleEvent(Caravan e)
        {
            RecentMoves.Add(e);

            StormLossesToTake.Clear();
            var initiator = GetPlayer(e.Initiator);
            var card = initiator.TreacheryCards.FirstOrDefault(c => c.Type == TreacheryCardType.Caravan);

            DiscardTreacheryCard(initiator, TreacheryCardType.Caravan);
            PerformMoveFromLocations(initiator, e.ForceLocations, e.To, e.Initiator != Faction.Blue || e.AsAdvisors, true);

            if (ContainsConflictingAlly(initiator, e.To))
            {
                ChosenDestinationsWithAllies.Add(e.To.Territory);
            }

            if (DetermineIntrusionCaused(e))
            {
                PausedPhase = CurrentPhase;
                Enter(Phase.BlueIntrudedByCaravan);
            }

            if (!Applicable(Rule.FullPhaseKarma)) Allow(FactionAdvantage.YellowExtraMove);
            if (!Applicable(Rule.FullPhaseKarma)) Allow(FactionAdvantage.GreyCyborgExtraMove);
        }

        private void PerformMoveFromLocations(Player initiator, Dictionary<Location, Battalion> forceLocations, Location to, bool asAdvisors, bool byCaravan)
        {
            LastShippedOrMovedTo = to;
            bool wasOccupiedBeforeMove = IsOccupied(to.Territory);
            var fromTerritory = forceLocations.Keys.First().Territory;

            int totalNumberOfForces = 0;
            int totalNumberOfSpecialForces = 0;
            foreach (var fl in forceLocations)
            {
                PerformMoveFromLocation(initiator, fl.Key, fl.Value, to, ref totalNumberOfForces, ref totalNumberOfSpecialForces);
            }

            if (initiator.Is(Faction.Blue))
            {
                initiator.FlipForces(to, wasOccupiedBeforeMove && asAdvisors);
            }

            RecentMilestones.Add(Milestone.Move);
            LogMove(initiator, fromTerritory, to, totalNumberOfForces, totalNumberOfSpecialForces, wasOccupiedBeforeMove && asAdvisors, byCaravan);
            FlipBeneGesseritWhenAlone();
        }

        private void PerformMoveFromLocation(Player initiator, Location from, Battalion battalion, Location to, ref int totalNumberOfForces, ref int totalNumberOfSpecialForces)
        {
            bool mustMoveThroughStorm = MustMoveThroughStorm(initiator, from, to, battalion);
            if (IsInStorm(to) || mustMoveThroughStorm)
            {
                int killCount;
                if (initiator.Is(Faction.Yellow) && !Prevented(FactionAdvantage.YellowProtectedFromStorm) && Applicable(Rule.YellowStormLosses))
                {
                    killCount = 0;
                    initiator.MoveForces(from, to, battalion.AmountOfForces);
                    initiator.MoveSpecialForces(from, to, battalion.AmountOfSpecialForces);
                    StormLossesToTake.Add(new Tuple<Location, int>(to, LossesToTake(battalion)));
                }
                else
                {
                    killCount = battalion.AmountOfForces + battalion.AmountOfSpecialForces;
                    initiator.KillForces(from, battalion.AmountOfForces, battalion.AmountOfSpecialForces, false);
                }

                if (killCount > 0)
                {
                    CurrentReport.Add(initiator.Faction, "{0} {1} {2} are killed by the storm while travelling.", killCount, initiator.Faction, initiator.Force);
                }
            }
            else
            {
                initiator.MoveForces(from, to, battalion.AmountOfForces);
                initiator.MoveSpecialForces(from, to, battalion.AmountOfSpecialForces);
                totalNumberOfForces += battalion.AmountOfForces;
                totalNumberOfSpecialForces += battalion.AmountOfSpecialForces;
            }
        }

        public bool MustMoveThroughStorm(Player initiator, Location from, Location to, Battalion moved)
        {
            if (from == null || to == null) return false;

            var max = DetermineMaximumMoveDistance(initiator, moved);
            var targetsAvoidingStorm = Map.FindNeighbours(from, max, false, initiator.Faction, SectorInStorm, ForcesOnPlanet);
            var targetsIgnoringStorm = Map.FindNeighbours(from, max, true, initiator.Faction, SectorInStorm, ForcesOnPlanet);
            return !targetsAvoidingStorm.Contains(to) && targetsIgnoringStorm.Contains(to);
        }

        private void LogMove(Player initiator, Territory from, Location to, int forceAmount, int specialForceAmount, bool asAdvisors, bool byCaravan)
        {
            if (initiator.Is(Faction.Blue))
            {
                CurrentReport.Add(initiator.Faction, "{5}{0} move {1} forces from {2} to {3} as {4}.",
                    initiator.Faction, forceAmount + specialForceAmount, from, to, asAdvisors ? (object)initiator.SpecialForce : initiator.Force, CaravanMessage(byCaravan));
            }
            else if (specialForceAmount > 0)
            {
                CurrentReport.Add(initiator.Faction, "{7}{0} move {1} {6} and {2} {5} from {3} to {4}.",
                    initiator.Faction, forceAmount, specialForceAmount, from, to, initiator.SpecialForce, initiator.Force, CaravanMessage(byCaravan));
            }
            else
            {
                CurrentReport.Add(initiator.Faction, "{5}{0} move {1} {2} from {3} to {4}.",
                    initiator.Faction, forceAmount, initiator.Force, from, to, CaravanMessage(byCaravan));
            }
        }

        private MessagePart CaravanMessage(bool byCaravan)
        {
            if (byCaravan)
            {
                return new MessagePart("By {0}, ", TreacheryCardType.Caravan);
            }
            else
            {
                return new MessagePart("");
            }
        }

        public void HandleEvent(BlueFlip e)
        {
            var initiator = GetPlayer(e.Initiator);

            if (Version < 77)
            {
                initiator.FlipForces(LastShippedOrMovedTo, e.AsAdvisors);
            }
            else
            {
                initiator.FlipForces(LastShippedOrMovedTo.Territory, e.AsAdvisors);
            }

            CurrentReport.Add(e.GetMessage());
            DetermineNextShipmentAndMoveSubPhase(false, BGMayAccompany);
        }

        public void HandleEvent(BlueBattleAnnouncement e)
        {
            var initiator = GetPlayer(e.Initiator);
            initiator.FlipForces(e.Territory, false);
            CurrentReport.Add(e.GetMessage());
        }

        private void DetermineNextShipmentAndMoveSubPhase(bool intrusionCaused, bool bgMayAccompany)
        {
            if (StormLossesToTake.Count > 0)
            {
                PhaseBeforeStormLoss = CurrentPhase;
                intrusionCausedBeforeStormLoss = intrusionCaused;
                bgMayAccompanyBeforeStormLoss = bgMayAccompany;
                Enter(Phase.StormLosses);
            }
            else
            {
                switch (CurrentPhase)
                {
                    case Phase.YellowRidingMonsterA:
                        Enter(intrusionCaused, Phase.BlueIntrudedByYellowRidingMonsterA, EndWormRide);
                        break;

                    case Phase.YellowRidingMonsterB:
                        Enter(intrusionCaused, Phase.BlueIntrudedByYellowRidingMonsterB, EndWormRide);
                        break;

                    case Phase.OrangeShip:
                        Enter(intrusionCaused, Phase.BlueIntrudedByOrangeShip, IsPlaying(Faction.Blue) && bgMayAccompany, Phase.BlueAccompaniesOrange, Phase.OrangeMove);
                        break;

                    case Phase.NonOrangeShip:
                        Enter(intrusionCaused, Phase.BlueIntrudedByNonOrangeShip, IsPlaying(Faction.Blue) && bgMayAccompany, Phase.BlueAccompaniesNonOrange, Phase.NonOrangeMove);
                        break;

                    case Phase.BlueIntrudedByOrangeShip:
                        Enter(IsPlaying(Faction.Blue) && bgMayAccompany, Phase.BlueAccompaniesOrange, Phase.OrangeMove);
                        break;

                    case Phase.BlueIntrudedByNonOrangeShip:
                        Enter(IsPlaying(Faction.Blue) && bgMayAccompany, Phase.BlueAccompaniesNonOrange, Phase.NonOrangeMove);
                        break;

                    case Phase.BlueAccompaniesOrange: Enter(Phase.OrangeMove); break;
                    case Phase.BlueAccompaniesNonOrange: Enter(Phase.NonOrangeMove); break;

                    case Phase.OrangeMove:
                        Enter(intrusionCaused, Phase.BlueIntrudedByOrangeMove, DetermineNextSubPhaseAfterOrangeMove);
                        break;

                    case Phase.NonOrangeMove:
                        Enter(intrusionCaused, Phase.BlueIntrudedByNonOrangeMove, DetermineNextSubPhaseAfterNonOrangeMove);
                        break;

                    case Phase.BlueIntrudedByOrangeMove: DetermineNextSubPhaseAfterOrangeMove(); break;
                    case Phase.BlueIntrudedByCaravan: Enter(PausedPhase); break;
                    case Phase.BlueIntrudedByNonOrangeMove: DetermineNextSubPhaseAfterNonOrangeMove(); break;
                    case Phase.BlueIntrudedByYellowRidingMonsterA:
                        {
                            Enter(Phase.YellowRidingMonsterA);
                            EndWormRide();
                            break;
                        }
                    case Phase.BlueIntrudedByYellowRidingMonsterB:
                        {
                            Enter(Phase.YellowRidingMonsterB);
                            EndWormRide();
                            break;
                        }
                }
            }
        }

        private void CheckIfForcesShouldBeDestroyedByAllyPresence(Player p)
        {
            if (p.Ally != Faction.None)
            {
                if (Version >= 86)
                {
                    //Forces that must be destroyed because moves ended where allies are
                    foreach (var t in ChosenDestinationsWithAllies)
                    {
                        if (p.AnyForcesIn(t) > 0)
                        {
                            CurrentReport.Add(p.Faction, "All {0} forces in {1} were killed due to ally presence.", p.Faction, t);
                            p.KillAllForces(t, false);
                        }
                    }
                }

                if (HasActedOrPassed.Contains(p.Ally))
                {
                    //Forces that must me destroyed if both the player and his ally have moved

                    var playerTerritories = Applicable(Rule.AdvisorsDontConflictWithAlly) ? p.OccupiedTerritories : p.TerritoriesWithForces;
                    var allyTerritories = Applicable(Rule.AdvisorsDontConflictWithAlly) ? GetPlayer(p.Ally).OccupiedTerritories : GetPlayer(p.Ally).TerritoriesWithForces;

                    foreach (var t in playerTerritories.Intersect(allyTerritories).ToList())
                    {
                        if (t != Map.PolarSink.Territory)
                        {
                            CurrentReport.Add(p.Faction, "All {0} forces in {1} were killed due to ally presence.", p.Faction, t);
                            p.KillAllForces(t, false);
                        }
                    }
                }
            }
        }

        public bool ThreatenedByAllyPresence(Player p, Territory t)
        {
            if (t == Map.PolarSink.Territory) return false;

            var ally = GetPlayer(p.Ally);
            if (ally == null) return false;

            if (Applicable(Rule.AdvisorsDontConflictWithAlly))
            {

                if (p.Is(Faction.Blue) && !p.Occupies(t))
                {
                    return false;
                }
                else if (p.Ally == Faction.Blue && !ally.Occupies(t))
                {
                    return false;
                }
                else
                {
                    return ally.AnyForcesIn(t) > 0;
                }
            }
            else
            {
                return ally.AnyForcesIn(t) > 0;
            }
        }

        private void DetermineNextSubPhaseAfterNonOrangeMove()
        {
            Enter(
                EveryoneActedOrPassed, ConcludeShipmentAndMove,
                IsPlaying(Faction.Orange) && !HasActedOrPassed.Any(p => p == Faction.Orange) && OrangeDeterminesMoveMoment, Phase.OrangeShip,
                Phase.NonOrangeShip);
        }

        private bool OrangeDeterminesMoveMoment
        {
            get
            {
                return Applicable(Rule.OrangeDetermineShipment);
            }
        }

        private void DetermineNextSubPhaseAfterOrangeMove()
        {
            Enter(!EveryoneActedOrPassed, Phase.NonOrangeShip, ConcludeShipmentAndMove);
        }

        private void ConcludeShipmentAndMove()
        {
            Enter(Phase.ShipmentAndMoveConcluded);
            ReceiveShipsTechIncome();
        }

        private void ReceiveShipsTechIncome()
        {
            if (ShipsTechTokenIncome)
            {
                var techTokenOwner = Players.FirstOrDefault(p => p.TechTokens.Contains(TechToken.Ships));
                if (techTokenOwner != null)
                {
                    var amount = techTokenOwner.TechTokens.Count;
                    techTokenOwner.Resources += amount;
                    CurrentReport.Add(techTokenOwner.Faction, "{0} receive {1} from {2}.", techTokenOwner.Faction, amount, TechToken.Ships);
                }
            }
        }
    }
}