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
        public void HandleEvent(Donated e)
        {
            var initiator = GetPlayer(e.Initiator);
            var target = GetPlayer(e.Target);

            ExchangeResourcesInBribe(initiator, target, e.Resources);

            if (e.Card != null)
            {
                initiator.TreacheryCards.Remove(e.Card);
                RegisterKnown(initiator, e.Card);
                target.TreacheryCards.Add(e.Card);

                foreach (var p in Players.Where(p => p != initiator && p != target))
                {
                    UnregisterKnown(p, initiator.TreacheryCards);
                    UnregisterKnown(p, target.TreacheryCards);
                }
            }

            CurrentReport.Add(e.GetMessage());
            RecentMilestones.Add(Milestone.Bribe);
        }

        private void ExchangeResourcesInBribe(Player from, Player target, int amount)
        {
            from.Resources -= amount;

            if (BribesDuringMentat)
            {
                if (Version >= 89)
                {
                    if (from.Faction == Faction.Red && target.Faction == from.Ally)
                    {
                        target.Resources += amount;
                    }
                    else
                    {
                        target.Bribes += amount;
                    }
                }
                else if (Version >= 75)
                {
                    if (from.Faction == Faction.Red || target.Faction == from.Ally)
                    {
                        target.Resources += amount;
                    }
                    else
                    {
                        target.Bribes += amount;
                    }
                }
                else
                {
                    if (from.Faction == Faction.Red && target.Faction != from.Ally)
                    {
                        target.Resources += amount;
                    }
                    else
                    {
                        target.Bribes += amount;
                    }
                }
            }
            else
            {
                target.Resources += amount;
            }
        }

        private Phase phasePausedByClairvoyance;
        public ClairVoyancePlayed LatestClairvoyance;
        public ClairVoyanceQandA LatestClairvoyanceQandA;
        public BattleInitiated LatestClairvoyanceBattle;

        public void HandleEvent(ClairVoyancePlayed e)
        {
            var initiator = GetPlayer(e.Initiator);
            var card = initiator.Card(TreacheryCardType.Clairvoyance);

            if (card != null)
            {
                Discard(card);
                CurrentReport.Add(e.GetMessage());
                RecentMilestones.Add(Milestone.Clairvoyance);
            }

            if (Version >= 77 && e.Target != Faction.None)
            {
                LatestClairvoyance = e;
                LatestClairvoyanceQandA = null;
                LatestClairvoyanceBattle = CurrentBattle;
                phasePausedByClairvoyance = CurrentPhase;
                Enter(Phase.Clairvoyance);
            }
        }

        public void HandleEvent(ClairVoyanceAnswered e)
        {
            LatestClairvoyanceQandA = new ClairVoyanceQandA(LatestClairvoyance, e);
            CurrentReport.Add(e.GetMessage());
            Enter(phasePausedByClairvoyance);
        }

        public void HandleEvent(AmalPlayed e)
        {
            DiscardTreacheryCard(GetPlayer(e.Initiator), TreacheryCardType.Amal);
            CurrentReport.Add(e.GetMessage());
            foreach (var p in Players)
            {
                int resourcesPaid = (int)Math.Ceiling(0.5 * p.Resources);
                p.Resources -= resourcesPaid;
                CurrentReport.Add(p.Faction, "{0} lose {1}.", p.Faction, resourcesPaid);
            }
            RecentMilestones.Add(Milestone.Amal);
        }

        public int KarmaHmsMovesLeft { get; private set; } = 2;
        public void HandleEvent(KarmaHmsMovement e)
        {
            var initiator = GetPlayer(e.Initiator);
            int collectionRate = initiator.AnyForcesIn(Map.HiddenMobileStronghold) * 2;
            CurrentReport.Add(e.GetMessage());

            if (!initiator.SpecialKarmaPowerUsed)
            {
                DiscardTreacheryCard(initiator, TreacheryCardType.Karma);
                initiator.SpecialKarmaPowerUsed = true;
            }

            var currentLocation = Map.HiddenMobileStronghold.AttachedToLocation;
            CollectSpiceFrom(e.Initiator, currentLocation, collectionRate);

            if (!e.Passed)
            {
                Map.HiddenMobileStronghold.PointAt(e.Target);
                CollectSpiceFrom(e.Initiator, e.Target, collectionRate);
                KarmaHmsMovesLeft--;
                RecentMilestones.Add(Milestone.HmsMovement);
            }

            if (e.Passed)
            {
                KarmaHmsMovesLeft = 0;
            }
        }

        public void HandleEvent(KarmaFreeRevival e)
        {
            RecentMilestones.Add(Milestone.Revival);
            CurrentReport.Add(e.GetMessage());
            var initiator = GetPlayer(e.Initiator);

            DiscardTreacheryCard(initiator, TreacheryCardType.Karma);
            initiator.SpecialKarmaPowerUsed = true;

            if (e.Hero != null)
            {
                ReviveHero(e.Hero);
            }
            else
            {
                initiator.ReviveForces(e.AmountOfForces);
                initiator.ReviveSpecialForces(e.AmountOfSpecialForces);

                if (e.AmountOfSpecialForces > 0)
                {
                    FactionsThatRevivedSpecialForcesThisTurn.Add(e.Initiator);
                }
            }
        }

        public void HandleEvent(KarmaMonster e)
        {
            var initiator = GetPlayer(e.Initiator);
            DiscardTreacheryCard(initiator, TreacheryCardType.Karma);
            initiator.SpecialKarmaPowerUsed = true;
            CurrentReport.Add(e.GetMessage());
            RecentMilestones.Add(Milestone.Karma);
            NumberOfMonsters++;
            ProcessMonsterCard(e.Territory);

            if (CurrentPhase == Phase.BlowReport)
            {
                Enter(Phase.AllianceB);
            }
        }

        public bool GreenKarma { get; private set; } = false;
        public void HandleEvent(KarmaPrescience e)
        {
            var initiator = GetPlayer(e.Initiator);
            DiscardTreacheryCard(initiator, TreacheryCardType.Karma);
            initiator.SpecialKarmaPowerUsed = true;
            CurrentReport.Add(e.GetMessage());
            RecentMilestones.Add(Milestone.Karma);
            GreenKarma = true;
        }

        public void HandleEvent(Karma e)
        {
            Discard(e.Card);
            CurrentReport.Add(e.GetMessage());
            RecentMilestones.Add(Milestone.Karma);

            if (e.Prevented != FactionAdvantage.None)
            {
                Prevent(e.Initiator, e.Prevented);
            }

            if (Version >= 42)
            {
                RevokeBattlePlansIfNeeded(e);
            }

            if (Version >= 77 && e.Prevented == FactionAdvantage.BlueUsingVoice)
            {
                CurrentVoice = null;
            }

            if (Version >= 77 && e.Prevented == FactionAdvantage.GreenBattlePlanPrescience)
            {
                CurrentPrescience = null;
            }
        }

        public IList<Faction> SecretsRemainHidden = new List<Faction>();
        public void HandleEvent(HideSecrets e)
        {
            SecretsRemainHidden.Add(e.Initiator);
        }

        public bool YieldsSecrets(Player p)
        {
            return !SecretsRemainHidden.Contains(p.Faction);
        }
        public bool YieldsSecrets(Faction f)
        {
            return !SecretsRemainHidden.Contains(f);
        }

        private void RevokeBattlePlansIfNeeded(Karma e)
        {
            if (CurrentMainPhase == MainPhase.Battle)
            {
                if (e.Prevented == FactionAdvantage.YellowNotPayingForBattles ||
                    e.Prevented == FactionAdvantage.YellowSpecialForceBonus)
                {
                    RevokePlanIfNeeded(Faction.Yellow);
                }

                if (e.Prevented == FactionAdvantage.RedSpecialForceBonus)
                {
                    RevokePlanIfNeeded(Faction.Red);
                }

                if (e.Prevented == FactionAdvantage.GreySpecialForceBonus)
                {
                    RevokePlanIfNeeded(Faction.Grey);
                }

                if (e.Prevented == FactionAdvantage.GreenUseMessiah)
                {
                    RevokePlanIfNeeded(Faction.Green);
                }
            }
        }

        private void RevokePlanIfNeeded(Faction f)
        {
            if (AggressorBattleAction != null && AggressorBattleAction.Initiator == f)
            {
                AggressorBattleAction = null;
            }

            if (DefenderBattleAction != null && DefenderBattleAction.Initiator == f)
            {
                DefenderBattleAction = null;
            }
        }

        private readonly List<FactionAdvantage> PreventedAdvantages = new List<FactionAdvantage>();

        public void Prevent(Faction initiator, FactionAdvantage advantage)
        {
            if (!PreventedAdvantages.Contains(advantage))
            {
                PreventedAdvantages.Add(advantage);
                CurrentReport.Add(initiator, "Using {0}, {1} prevent {2}.", TreacheryCardType.Karma, initiator, advantage);
            }
        }

        public void Allow(FactionAdvantage advantage)
        {
            if (PreventedAdvantages.Contains(advantage))
            {
                PreventedAdvantages.Remove(advantage);
                CurrentReport.Add("{0} no longer prevents {1}.", TreacheryCardType.Karma, advantage);
            }
        }

        public bool Prevented(FactionAdvantage advantage)
        {
            return PreventedAdvantages.Contains(advantage);
        }

        public bool BribesDuringMentat
        {
            get
            {
                if (Version >= 41)
                {
                    return !Applicable(Rule.BribesAreImmediate);
                }
                else
                {
                    //In this version, the rule was "the other way around"
                    return Applicable(Rule.BribesAreImmediate);
                }
            }
        }

        public void HandleEvent(PlayerReplaced e)
        {
            GetPlayer(e.ToReplace).IsBot = true;
            CurrentReport.Add(e.GetMessage());
        }
    }
}