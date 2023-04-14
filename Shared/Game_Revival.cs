﻿/*
 * Copyright 2020-2023 Ronald Ossendrijver. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public partial class Game
    {
        #region BeginningOfRevival

        internal bool RevivalTechTokenIncome { get; set; }

        public List<Faction> FactionsThatTookFreeRevival { get; private set; } = new List<Faction>();

        internal bool PurpleStartedRevivalWithLowThreshold { get; set; }

        
        #endregion

        #region Revival

        public List<Faction> FactionsThatRevivedSpecialForcesThisTurn { get; } = new();

        internal void PrepareSkillAssignmentToRevivedLeader(Player player, Leader leader)
        {
            if (CurrentPhase != Phase.AssigningSkill)
            {
                PhaseBeforeSkillAssignment = CurrentPhase;
            }

            player.MostRecentlyRevivedLeader = leader;
            SkillDeck.Shuffle();
            Stone(Milestone.Shuffled);
            player.SkillsToChooseFrom.Add(SkillDeck.Draw());
            player.SkillsToChooseFrom.Add(SkillDeck.Draw());
            Enter(Phase.AssigningSkill);
        }

        internal void Revive(Player initiator, IHero h)
        {
            LeaderState[h].Revive();
            LeaderState[h].CurrentTerritory = null;

            var currentOwner = OwnerOf(h);
            if (currentOwner != null && h is Leader l && (Version >= 154 || initiator.Faction == Faction.Purple && h.Faction != Faction.Purple))
            {
                currentOwner.Leaders.Remove(l);
                initiator.Leaders.Add(l);
            }
        }

        public int FreeRevivals(Player player, bool usesRedSecretAlly)
        {
            if (FactionsThatTookFreeRevival.Contains(player.Faction) || FreeRevivalPrevented(player.Faction))
            {
                return 0;
            }
            else
            {
                int nrOfFreeRevivals = 0;

                switch (player.Faction)
                {
                    case Faction.Yellow: nrOfFreeRevivals = 3; break;
                    case Faction.Green: nrOfFreeRevivals = 2; break;
                    case Faction.Black: nrOfFreeRevivals = 2; break;
                    case Faction.Red: nrOfFreeRevivals = 1; break;
                    case Faction.Orange: nrOfFreeRevivals = 1; break;
                    case Faction.Blue: nrOfFreeRevivals = 1; break;
                    case Faction.Grey: nrOfFreeRevivals = 1; break;
                    case Faction.Purple: nrOfFreeRevivals = 2; break;

                    case Faction.Brown: nrOfFreeRevivals = 0; break;
                    case Faction.White: nrOfFreeRevivals = 2; break;
                    case Faction.Pink: nrOfFreeRevivals = 2; break;
                    case Faction.Cyan: nrOfFreeRevivals = 2; break;
                }

                if (CurrentRecruitsPlayed != null)
                {
                    nrOfFreeRevivals *= 2;
                }

                if (player.Ally == Faction.Yellow && player.Ally == Faction.Yellow && YellowAllowsThreeFreeRevivals)
                {
                    nrOfFreeRevivals = 3;
                }

                if (CurrentYellowNexus != null && CurrentYellowNexus.Player == player)
                {
                    nrOfFreeRevivals = 3;
                }

                if (usesRedSecretAlly)
                {
                    nrOfFreeRevivals += 3;
                }

                if (GetsExtraCharityAndFreeRevivalDueToLowThreshold(player))
                {
                    nrOfFreeRevivals += 1;
                }

                return nrOfFreeRevivals;
            }
        }

        private bool GetsExtraCharityAndFreeRevivalDueToLowThreshold(Player player) =>
            !(player.Is(Faction.Red) || player.Is(Faction.Brown)) && player.HasLowThreshold() ||
            player.Is(Faction.Red) && player.HasLowThreshold(World.Red) ||
            player.Is(Faction.Brown) && player.HasLowThreshold() && OccupierOf(World.Brown) == null;

        

        #endregion

        #region RevivalEvents

        public Faction[] FactionsWithIncreasedRevivalLimits { get; internal set; } = Array.Empty<Faction>();

        public void HandleEvent(SetIncreasedRevivalLimits e)
        {
            FactionsWithIncreasedRevivalLimits = e.Factions;
            Log(e);
        }

        public int GetRevivalLimit(Game g, Player p)
        {
            if (p.Is(Faction.Purple) || (p.Is(Faction.Brown) && !g.Prevented(FactionAdvantage.BrownRevival)))
            {
                return 100;
            }
            else if (CurrentRecruitsPlayed != null)
            {
                return 7;
            }
            else if (FactionsWithIncreasedRevivalLimits.Contains(p.Faction))
            {
                return 5;
            }
            else
            {
                return 3;
            }
        }

        public void HandleEvent(RaiseDeadPlayed r)
        {
            Stone(Milestone.RaiseDead);
            Log(r);
            var player = GetPlayer(r.Initiator);
            Discard(player, TreacheryCardType.RaiseDead);

            var purple = GetPlayer(Faction.Purple);
            if (purple != null)
            {
                purple.Resources += 1;
                Log(Faction.Purple, " get ", Payment.Of(1), " for revival by ", TreacheryCardType.RaiseDead);
            }

            if (r.Hero != null)
            {
                if (r.Initiator != r.Hero.Faction && r.Hero is Leader)
                {
                    Revive(player, r.Hero as Leader);
                }
                else
                {
                    Revive(player, r.Hero);
                }

                if (r.AssignSkill)
                {
                    PrepareSkillAssignmentToRevivedLeader(r.Player, r.Hero as Leader);
                }
            }
            else
            {
                player.ReviveForces(r.AmountOfForces);
                player.ReviveSpecialForces(r.AmountOfSpecialForces);

                if (r.AmountOfSpecialForces > 0)
                {
                    FactionsThatRevivedSpecialForcesThisTurn.Add(r.Initiator);
                }
            }

            if (r.Location != null && r.Initiator == Faction.Yellow)
            {
                player.ShipSpecialForces(r.Location, 1);
                Log(r.Initiator, " place ", FactionSpecialForce.Yellow, " in ", r.Location);
            }
        }

        public List<RequestPurpleRevival> CurrentRevivalRequests { get; private set; } = new();

        public Dictionary<IHero, int> EarlyRevivalsOffers { get; private set; } = new();

        public bool IsAllowedEarlyRevival(IHero h) => EarlyRevivalsOffers.ContainsKey(h) && EarlyRevivalsOffers[h] < int.MaxValue;


        private bool FreeRevivalPrevented(Faction f)
        {
            return CurrentFreeRevivalPrevention != null && CurrentFreeRevivalPrevention.Target == f;
        }

        public BrownFreeRevivalPrevention CurrentFreeRevivalPrevention { get; internal set; } = null;

        public bool PreventedFromReviving(Faction f) => CurrentKarmaRevivalPrevention != null && CurrentKarmaRevivalPrevention.Target == f;

        internal KarmaRevivalPrevention CurrentKarmaRevivalPrevention = null;

        public int AmbassadorsPlacedThisTurn { get; internal set; } = 0;

        public Ambassador AmbassadorIn(Territory t) => AmbassadorsOnPlanet.TryGetValue(t, out Ambassador value) ? value : Ambassador.None;

        #endregion

        #region EndOfRevival

        

        #endregion
    }
}
