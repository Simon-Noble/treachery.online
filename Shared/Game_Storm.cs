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
        #region StormPhase

        public int HmsMovesLeft;
        public List<Faction> HasBattleWheel = new List<Faction>();
        private List<int> Dials = new List<int>();

        private void EnterStormPhase()
        {
            FactionsThatRevivedSpecialForcesThisTurn.Clear();
            StormLossesToTake.Clear();
            CurrentMainPhase = MainPhase.Storm;
            CurrentReport = new Report(MainPhase.Storm);
            CurrentTurn++;

            if (CurrentTurn == 1)
            {
                if (Version >= 88 && Players.Count() > 1)
                {
                    Dials.Clear();
                    HasActedOrPassed.Clear();
                    Enter(Phase.DiallingStorm);
                }
                else
                {
                    NextStormMoves = DetermineFirstStorm();
                    PositionFirstStorm();
                }
            }
            else
            {
                MoveHMSBeforeDiallingStorm();
            }
        }

        public void HandleEvent(StormDialled e)
        {
            Dials.Add(e.Amount);
            HasActedOrPassed.Add(e.Initiator);

            if (HasBattleWheel.All(f => HasActedOrPassed.Contains(f)))
            {
                CurrentReport.Add("Storm dial: {0} ({1}) + {2} ({3}) = {4}", Dials[0], HasActedOrPassed[0], Dials[1], HasActedOrPassed[1], Dials[0] + Dials[1]);
                NextStormMoves = Dials.Sum();

                if (CurrentTurn == 1)
                {
                    PositionFirstStorm();
                }
                else
                {
                    RevealStorm();
                }
            }
        }

        private void PositionFirstStorm()
        {
            SectorInStorm = NextStormMoves % Map.NUMBER_OF_SECTORS;
            CurrentReport.Add("The first storm moves {0} sectors.", SectorInStorm);
            PerformStorm();

            if (Applicable(Rule.GreyAndPurpleExpansionTechTokens))
            {
                AssignTechTokens();
            }

            if (Version < 88 || IsPlaying(Faction.Yellow) && Applicable(Rule.YellowSeesStorm))
            {
                if (IsPlaying(Faction.Yellow) && Applicable(Rule.YellowSeesStorm))
                {
                    NextStormMoves = DetermineLaterStormWithStormDeck();
                }
                else
                {
                    NextStormMoves = DetermineLaterStormWithDials();
                }
            }

            Enter(IsPlaying(Faction.Grey) || Applicable(Rule.HMSwithoutGrey), Phase.HmsPlacement, EnterSpiceBlowPhase);
        }

        private void MoveHMSBeforeDiallingStorm()
        {
            if (IsPlaying(Faction.Grey) && GetPlayer(Faction.Grey).AnyForcesIn(Map.HiddenMobileStronghold) > 0)
            {
                if (!Prevented(FactionAdvantage.GreyMovingHMS))
                {
                    if (Version >= 85 && Map.HiddenMobileStronghold.AttachedToLocation.Sector == SectorInStorm)
                    {
                        CurrentReport.Add("The storm prevents {0} from moving.", Map.HiddenMobileStronghold);
                        DetermineStorm();
                    }
                    else
                    {
                        HmsMovesLeft = 3;
                        Enter(Phase.HmsMovement);
                    }
                }
                else
                {
                    CurrentReport.Add("{0} prevents {1} moving {2}", TreacheryCardType.Karma, Faction.Grey, Map.HiddenMobileStronghold);
                    if (!Applicable(Rule.FullPhaseKarma)) Allow(FactionAdvantage.GreyMovingHMS);
                    DetermineStorm();
                }
            }
            else
            {
                DetermineStorm();
            }
        }

        private int DetermineFirstStorm()
        {
            return Random.Next(21) + Random.Next(21);
        }

        private int DetermineLaterStormWithDials()
        {
            return Random.Next(3) + Random.Next(3) + 1;
        }

        private int DetermineLaterStormWithStormDeck()
        {
            return Random.Next(6) + 1;
        }

        private void DetermineStorm()
        {
            if (IsPlaying(Faction.Yellow) && Applicable(Rule.YellowSeesStorm) || Version < 88)
            {
                //Storm dial has already been determined
                RevealStorm();
            }
            else
            {
                if (Version >= 88 && Players.Count() > 1)
                {
                    Dials.Clear();
                    HasActedOrPassed.Clear();
                    Enter(Phase.DiallingStorm);
                }
                else
                {
                    NextStormMoves = DetermineLaterStormWithDials();
                    RevealStorm();
                }
            }
        }

        private void RevealStorm()
        {
            HasActedOrPassed.Clear();
            CurrentReport.Add("The storm will move {0} sectors...", NextStormMoves);
            Enter(Phase.MetheorAndStormSpell);
        }

        public void HandleEvent(PerformHmsPlacement e)
        {
            Map.HiddenMobileStronghold.PointAt(e.Target);
            CurrentReport.Add(e.GetMessage());
            EnterSpiceBlowPhase();
            RecentMilestones.Add(Milestone.HmsMovement);
        }

        public void HandleEvent(PerformHmsMovement e)
        {
            var initiator = GetPlayer(e.Initiator);
            int collectionRate = initiator.AnyForcesIn(Map.HiddenMobileStronghold) * 2;
            CurrentReport.Add(e.GetMessage());

            var currentLocation = Map.HiddenMobileStronghold.AttachedToLocation;
            CollectSpiceFrom(e.Initiator, currentLocation, collectionRate);

            if (!e.Passed)
            {
                Map.HiddenMobileStronghold.PointAt(e.Target);
                CollectSpiceFrom(e.Initiator, e.Target, collectionRate);
                HmsMovesLeft--;
                RecentMilestones.Add(Milestone.HmsMovement);
            }

            if (e.Passed || HmsMovesLeft == 0)
            {
                DetermineStorm();
            }
        }

        private void CollectSpiceFrom(Faction faction, Location l, int maximumAmount)
        {
            if (ResourcesOnPlanet.ContainsKey(l))
            {
                int collected = Math.Min(ResourcesOnPlanet[l], maximumAmount);
                ChangeSpiceOnPlanet(l, -collected);
                var collector = GetPlayer(faction);
                collector.Resources += collected;
                CurrentReport.Add(faction, "{0} collect {1} {2} from {3}.", faction, collected, Concept.Resource, l);
            }
        }

        private void AssignTechTokens()
        {
            var techTokensToBeDealt = new List<TechToken>();

            var yellow = GetPlayer(Faction.Yellow);
            if (yellow != null)
            {
                yellow.TechTokens.Add(TechToken.Resources);
                CurrentReport.Add(Faction.Yellow, "{0} receive {1}", Faction.Yellow, TechToken.Resources);
            }
            else
            {
                techTokensToBeDealt.Add(TechToken.Resources);
            }

            var purple = GetPlayer(Faction.Purple);
            if (purple != null)
            {
                purple.TechTokens.Add(TechToken.Graveyard);
                CurrentReport.Add(Faction.Purple, "{0} receive {1}", Faction.Purple, TechToken.Graveyard);
            }
            else
            {
                techTokensToBeDealt.Add(TechToken.Graveyard);
            }

            var grey = GetPlayer(Faction.Grey);
            if (grey != null)
            {
                grey.TechTokens.Add(TechToken.Ships);
                CurrentReport.Add(Faction.Grey, "{0} receive {1}", Faction.Grey, TechToken.Ships);
            }
            else
            {
                techTokensToBeDealt.Add(TechToken.Ships);
            }

            var remainingTechTokens = new Deck<TechToken>(techTokensToBeDealt, Random);
            remainingTechTokens.Shuffle();
            RecentMilestones.Add(Milestone.Shuffled);
            TechTokenSequence.Start(this, false);

            while (!remainingTechTokens.IsEmpty && Players.Any(p => p.TechTokens.Count == 0))
            {
                if (TechTokenSequence.CurrentPlayer.TechTokens.Count == 0)
                {
                    var token = remainingTechTokens.Draw();
                    TechTokenSequence.CurrentPlayer.TechTokens.Add(token);
                    CurrentReport.Add(TechTokenSequence.CurrentPlayer.Faction, "{0} receive {1}", TechTokenSequence.CurrentPlayer.Faction, token);
                }

                TechTokenSequence.NextPlayer(this, false);
            }
        }

        public void HandleEvent(MetheorPlayed e)
        {
            var player = GetPlayer(e.Initiator);
            var card = player.Card(TreacheryCardType.Metheor);

            RecentMilestones.Add(Milestone.MetheorUsed);
            ShieldWallDestroyed = true;
            player.TreacheryCards.Remove(card);
            CurrentReport.Add(e.GetMessage());

            foreach (var p in Players)
            {
                foreach (var location in Map.ShieldWall.Locations.Where(l => p.AnyForcesIn(l) > 0))
                {
                    p.KillAllForces(location, false);
                }
            }
        }

        public void HandleEvent(StormSpellPlayed e)
        {
            DiscardTreacheryCard(GetPlayer(e.Initiator), TreacheryCardType.StormSpell);
            CurrentReport.Add(e.GetMessage());
            MoveStormAndDetermineNext(e.MoveAmount);
            EnterSpiceBlowPhase();
        }


        private void EnterNormalStormPhase()
        {
            CurrentReport.Add("The storm moves {0} sectors.", NextStormMoves);
            MoveStormAndDetermineNext(NextStormMoves);
            EnterSpiceBlowPhase();
        }

        private void MoveStormAndDetermineNext(int amount)
        {
            if (amount == 0)
            {
                PerformStorm();
            }

            for (int i = 0; i < amount; i++)
            {
                SectorInStorm = (SectorInStorm + 1) % Map.NUMBER_OF_SECTORS;
                PerformStorm();
            }

            if (Version < 88 || IsPlaying(Faction.Yellow) && Applicable(Rule.YellowSeesStorm))
            {
                NextStormMoves = DetermineLaterStormWithStormDeck();
            }

            RecentMilestones.Add(Milestone.Storm);
        }

        private void PerformStorm()
        {
            foreach (var l in ForcesOnPlanet.Where(f => f.Key.Sector == SectorInStorm && !IsProtectedFromStorm(f.Key)))
            {
                foreach (var battalion in l.Value)
                {
                    var player = GetPlayer(battalion.Faction);

                    int killCount = 0;
                    if (battalion.Is(Faction.Yellow) && Applicable(Rule.YellowStormLosses))
                    {
                        if (!Prevented(FactionAdvantage.YellowProtectedFromStorm))
                        {
                            StormLossesToTake.Add(new Tuple<Location, int>(l.Key, LossesToTake(battalion)));
                        }
                        else
                        {
                            killCount += player.AnyForcesIn(l.Key);
                            player.KillAllForces(l.Key, false);
                            if (!Applicable(Rule.FullPhaseKarma)) Allow(FactionAdvantage.YellowProtectedFromStorm);
                        }
                    }
                    else
                    {
                        killCount += player.AnyForcesIn(l.Key);
                        player.KillAllForces(l.Key, false);
                    }

                    if (killCount > 0)
                    {
                        CurrentReport.Add("The storm kills {0} {1} forces in {2}.", killCount, battalion.Faction, l.Key);
                    }
                }
            }

            foreach (var l in Map.Locations.Where(l => l.Sector == SectorInStorm))
            {
                int removed = RemoveResources(l);
                if (removed > 0)
                {
                    CurrentReport.Add("The storm destroys {0} spice in {1}.", removed, l);
                }
            }
        }

        private Phase PhaseBeforeStormLoss;
        bool intrusionCausedBeforeStormLoss;
        bool bgMayAccompanyBeforeStormLoss;

        public void HandleEvent(TakeLosses e)
        {
            var player = GetPlayer(e.Initiator);
            player.KillForces(Shared.TakeLosses.LossLocation(this), e.ForceAmount, e.SpecialForceAmount, false);
            StormLossesToTake.RemoveAt(0);

            if (PhaseBeforeStormLoss == Phase.BlowA)
            {
                CurrentReport.Add(e.GetMessage());
                Enter(StormLossesToTake.Count > 0, Phase.StormLosses, EnterSpiceBlowPhase);
            }
            else
            {
                CurrentReport.Add(e.GetMessage());
                Enter(PhaseBeforeStormLoss);
                DetermineNextShipmentAndMoveSubPhase(intrusionCausedBeforeStormLoss, bgMayAccompanyBeforeStormLoss);
            }
        }

        private int LossesToTake(Battalion battalion)
        {
            return LossesToTake(battalion.AmountOfForces, battalion.AmountOfSpecialForces);
        }

        private int LossesToTake(int AmountOfForces, int AmountOfSpecialForces)
        {
            if (Version >= 57)
            {
                return (int)Math.Ceiling(0.5 * (AmountOfForces + AmountOfSpecialForces));
            }
            else
            {
                return (int)Math.Ceiling(0.5 * (AmountOfForces + AmountOfSpecialForces * 2));
            }
        }

        public List<Tuple<Location, int>> StormLossesToTake = new List<Tuple<Location, int>>();

        public bool IsProtectedFromStorm(Location l)
        {
            if (!ShieldWallDestroyed)
            {
                return l.Territory.IsProtectedFromStorm;
            }
            else
            {
                return l.Territory.IsProtectedFromStorm && !(l == Map.Arrakeen || l.Territory == Map.ImperialBasin || l == Map.Carthag);
            }
        }
        #endregion StormPhase

    }
}