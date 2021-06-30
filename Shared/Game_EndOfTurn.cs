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

        #region SpiceCollectionPhase

        private void EnterSpiceCollectionPhase()
        {
            CurrentMainPhase = MainPhase.Contemplate;
            CurrentReport = new Report(MainPhase.Contemplate);
            CallHeroesHome();
            CollectResourcesFromTerritories();
            CollectResourcesFromStrongholds();
            EnterMentatPhase();
        }

        private void CollectResourcesFromStrongholds()
        {
            if (Applicable(Rule.IncreasedResourceFlow))
            {
                foreach (var playerInArrakeen in Players.Where(p => p.Controls(this, Map.Arrakeen, Applicable(Rule.ContestedStongholdsCountAsOccupied))))
                {
                    CurrentReport.Add(playerInArrakeen.Faction, "{0} earn 2 for {1}.", playerInArrakeen.Faction, Map.Arrakeen);
                    playerInArrakeen.Resources += 2;
                }

                foreach (var playerInCarthag in Players.Where(p => p.Controls(this, Map.Carthag, Applicable(Rule.ContestedStongholdsCountAsOccupied))))
                {
                    CurrentReport.Add(playerInCarthag.Faction, "{0} earn 2 for {1}.", playerInCarthag.Faction, Map.Carthag);
                    playerInCarthag.Resources += 2;
                }

                foreach (var playerInTueksSietch in Players.Where(p => p.Controls(this, Map.TueksSietch, Applicable(Rule.ContestedStongholdsCountAsOccupied))))
                {
                    CurrentReport.Add(playerInTueksSietch.Faction, "{0} earn 1 for {1}.", playerInTueksSietch.Faction, Map.TueksSietch);
                    playerInTueksSietch.Resources += 1;
                }
            }
        }

        private void CollectResourcesFromTerritories()
        {
            foreach (var l in ResourcesOnPlanet.Where(x => x.Value > 0).ToList())
            {
                foreach (var p in Players.Where(y => y.Occupies(l.Key)))
                {
                    int collectionRate = ResourceCollectionRate(p);
                    int forcesCollectingDefaultAmountOfSpice = p.Faction != Faction.Grey ? p.OccupyingForces(l.Key) : p.ForcesIn(l.Key);
                    int forcesCollecting3Spice = p.Is(Faction.Grey) ? p.SpecialForcesIn(l.Key) : 0;
                    int maximumSpiceThatCanBeCollected = forcesCollectingDefaultAmountOfSpice * collectionRate + forcesCollecting3Spice * 3;
                    int collectedAmount = Math.Min(l.Value, maximumSpiceThatCanBeCollected);
                    ChangeSpiceOnPlanet(l.Key, -collectedAmount);
                    CurrentReport.Add(p.Faction, "{0} collect {1} from {2}.", p.Faction, collectedAmount, l.Key);
                    p.Resources += collectedAmount;
                }
            }
        }

        public int ResourceCollectionRate(Player p)
        {
            return (p.Controls(this, Map.Arrakeen, Applicable(Rule.ContestedStongholdsCountAsOccupied)) || p.Controls(this, Map.Carthag, Applicable(Rule.ContestedStongholdsCountAsOccupied))) ? 3 : 2;
        }
        #endregion SpiceCollectionPhase

        #region MentatPhase
        public List<Player> Winners = new List<Player>();

        private void EnterMentatPhase()
        {
            AllowAllFactionAdvantages();
            CheckNormalWin();
            CheckBeneGesseritPrediction();
            CheckFinalTurnWin();
        }

        private void AllowAllFactionAdvantages()
        {
            foreach (var adv in Enumerations.GetValuesExceptDefault(typeof(FactionAdvantage), FactionAdvantage.None))
            {
                Allow(adv);
            }
        }

        private void AddBribesToPlayerResources()
        {
            foreach (var p in Players)
            {
                if (p.Bribes > 0)
                {
                    p.Resources += p.Bribes;
                    CurrentReport.Add(p.Faction, "{0} add {1} received as bribes to their reserves.", p.Faction, p.Bribes);
                    p.Bribes = 0;
                }
            }
        }

        public WinMethod WinMethod { get; set; }

        private void CheckFinalTurnWin()
        {
            if (Winners.Count == 0 && CurrentTurn == MaximumNumberOfTurns)
            {
                CheckSpecialWinConditions();
            }

            if (Winners.Count == 0 && CurrentTurn == MaximumNumberOfTurns)
            {
                CheckOtherWinConditions();
                CheckBeneGesseritPrediction();
            }

            if (Winners.Count > 0 || CurrentTurn == MaximumNumberOfTurns)
            {
                CurrentMainPhase = MainPhase.Ended;
                Enter(Phase.GameEnded);
                RecentMilestones.Add(Milestone.GameWon);
                CurrentReport.Add("The game has ended.");

                foreach (var w in Winners)
                {
                    CurrentReport.Add(w.Faction, "{0} win!", w.Faction);
                }
            }
            else
            {
                Enter(IsPlaying(Faction.Purple), Phase.ReplacingFaceDancer, Phase.TurnConcluded);
            }
        }

        public void HandleEvent(FaceDancerReplaced e)
        {
            if (!e.Passed)
            {
                var player = GetPlayer(e.Initiator);
                player.FaceDancers.Remove(e.SelectedDancer);
                TraitorDeck.PutOnTop(e.SelectedDancer);
                TraitorDeck.Shuffle();
                RecentMilestones.Add(Milestone.Shuffled);
                var leader = TraitorDeck.Draw();
                player.FaceDancers.Add(leader);
                if (!player.KnownNonTraitors.Contains(leader)) player.KnownNonTraitors.Add(leader);
            }

            CurrentReport.Add(e.GetMessage());
            Enter(Phase.TurnConcluded);
        }

        private void CheckBeneGesseritPrediction()
        {
            var benegesserit = GetPlayer(Faction.Blue);
            if (benegesserit != null && benegesserit.PredictedTurn == CurrentTurn && Winners.Any(w => w.Faction == benegesserit.PredictedFaction))
            {
                CurrentReport.Add(Faction.Blue, "{0} predicted {1} victory in turn {2}! They had everything planned...", Faction.Blue, benegesserit.PredictedFaction, benegesserit.PredictedTurn);
                WinMethod = WinMethod.Prediction;
                Winners.Clear();
                Winners.Add(benegesserit);
            }
        }

        public bool IsSpecialStronghold(Territory t)
        {
            return t == Map.ShieldWall && Applicable(Rule.SSW) && NumberOfMonsters >= 4;
        }

        private void CheckNormalWin()
        {
            CheckWinSequence.Start(this, false);

            for (int i = 0; i < Players.Count; i++)
            {
                var p = CheckWinSequence.CurrentPlayer;

                if (MeetsNormalVictoryCondition(p, Applicable(Rule.ContestedStongholdsCountAsOccupied)))
                {
                    WinMethod = WinMethod.Strongholds;

                    Winners.Add(p);

                    var ally = GetPlayer(p.Ally);
                    if (ally != null)
                    {
                        Winners.Add(ally);
                    }

                    LogNormalWin(p, ally);
                }

                if (Winners.Count > 0) break;

                CheckWinSequence.NextPlayer(this, false);
            }
        }

        private void LogNormalWin(Player p, Player ally)
        {
            if (Players.Any(p => !Winners.Contains(p) && MeetsNormalVictoryCondition(p, Applicable(Rule.ContestedStongholdsCountAsOccupied))))
            {
                if (ally == null)
                {
                    CurrentReport.Add(p.Faction, "{0} is the first player in front of the storm with enough victory points to win the game.", p.Faction);
                }
                else
                {
                    CurrentReport.Add(p.Faction, "{0} and {1} are the first players in front of the storm with enough victory points to win the game.", p.Faction, p.Ally);
                }
            }
            else
            {
                if (ally == null)
                {
                    CurrentReport.Add(p.Faction, "{0} have enough victory points to win the game.", p.Faction);
                }
                else
                {
                    CurrentReport.Add(p.Faction, "{0} and {1} have enough victory points to win the game.", p.Faction, p.Ally);
                }
            }
        }

        public bool MeetsNormalVictoryCondition(Player p, bool contestedStongholdsCountAsOccupied)
        {
            return NumberOfVictoryPoints(p, contestedStongholdsCountAsOccupied) >= TresholdForWin(p);
        }

        public int CountChallengedStongholds(Player p)
        {
            return NumberOfVictoryPoints(p, true) - NumberOfVictoryPoints(p, false);
        }

        public int TresholdForWin(Player p)
        {
            if (p.Ally != Faction.None)
            {
                return 4;
            }
            else
            {
                return (Players.Count <= 2) ? 4 : 3;
            }
        }

        public int NumberOfVictoryPoints(Player p, bool contestedStongholdsCountAsOccupied)
        {
            int techTokenPoint = p.TechTokens.Count == 3 ? 1 : 0;
            var ally = GetPlayer(p.Ally);

            if (ally != null)
            {
                return techTokenPoint + (Map.Locations.Where(l => l.Territory.IsStronghold || IsSpecialStronghold(l.Territory)).Count(l => p.Controls(this, l, contestedStongholdsCountAsOccupied) || ally.Controls(this, l, contestedStongholdsCountAsOccupied)));
            }
            else
            {
                return techTokenPoint + (Map.Locations.Where(l => l.Territory.IsStronghold || IsSpecialStronghold(l.Territory)).Count(l => p.Controls(this, l, contestedStongholdsCountAsOccupied)));
            }
        }

        private void CheckSpecialWinConditions()
        {
            var fremen = GetPlayer(Faction.Yellow);
            var guild = GetPlayer(Faction.Orange);

            if (fremen != null && YellowVictoryConditionMet)
            {
                CurrentReport.Add(Faction.Yellow, "{0} special victory conditions are met!", Faction.Yellow);
                WinMethod = WinMethod.YellowSpecial;
                Winners.Add(fremen);
                if (fremen.Ally != Faction.None) Winners.Add(GetPlayer(fremen.Ally));
            }
            else if (guild != null)
            {
                CurrentReport.Add(Faction.Orange, "{0} special victory conditions are met!", Faction.Orange);
                WinMethod = WinMethod.OrangeSpecial;
                Winners.Add(guild);
                if (guild.Ally != Faction.None) Winners.Add(GetPlayer(guild.Ally));
            }
            else if (Version >= 50 && fremen != null)
            {
                CurrentReport.Add(Faction.Yellow, "{0} win because {1} is not playing and no one else won.", Faction.Yellow, Faction.Orange);
                WinMethod = WinMethod.YellowSpecial;
                Winners.Add(fremen);
                if (fremen.Ally != Faction.None) Winners.Add(GetPlayer(fremen.Ally));
            }
        }

        public bool YellowVictoryConditionMet
        {
            get
            {

                bool sietchTabrOccupiedByOtherThanFremen = Players.Any(p => p.Faction != Faction.Yellow && p.Occupies(Map.SietchTabr));
                bool habbanyaSietchOccupiedByOtherThanFremen = Players.Any(p => p.Faction != Faction.Yellow && p.Occupies(Map.HabbanyaSietch));
                //bool tueksSietchOccupiedByAtreidesOrHarkonnen = Players.Any(p => (p.Is(Faction.Green) || p.Is(Faction.Black)) && p.Occupies(Map.TueksSietch));
                bool tueksSietchOccupiedByAtreidesOrHarkonnenOrEmperor = Players.Any(p => (p.Is(Faction.Green) || p.Is(Faction.Black) || p.Is(Faction.Red)) && p.Occupies(Map.TueksSietch));

                return CurrentTurn == MaximumNumberOfTurns && !sietchTabrOccupiedByOtherThanFremen && !habbanyaSietchOccupiedByOtherThanFremen && !tueksSietchOccupiedByAtreidesOrHarkonnenOrEmperor;
            }
        }

        private void CheckOtherWinConditions()
        {
            var fremen = GetPlayer(Faction.Yellow);

            if (Version < 50 && fremen != null)
            {
                CurrentReport.Add(Faction.Yellow, "{0} win because {1} is not playing and no one else won.", Faction.Yellow, Faction.Orange);
                WinMethod = WinMethod.OrangeSpecial;
                Winners.Add(fremen);
                if (fremen.Ally != Faction.None) Winners.Add(GetPlayer(fremen.Ally));
            }
            else
            {
                CurrentReport.Add(Faction.None, "Players with the most strongholds win.");
                WinMethod = WinMethod.Strongholds;
                var nrOfStrongholdsPerPlayer = new Dictionary<Player, int>();
                foreach (var p in Players)
                {
                    int techTokenPoint = p.TechTokens.Count == 3 ? 1 : 0;

                    nrOfStrongholdsPerPlayer.Add(p,
                        techTokenPoint +
                        (p.Controls(this, Map.Arrakeen, Applicable(Rule.ContestedStongholdsCountAsOccupied)) ? 1 : 0) +
                        (p.Controls(this, Map.Carthag, Applicable(Rule.ContestedStongholdsCountAsOccupied)) ? 1 : 0) +
                        (p.Controls(this, Map.SietchTabr, Applicable(Rule.ContestedStongholdsCountAsOccupied)) ? 1 : 0) +
                        (p.Controls(this, Map.HabbanyaSietch, Applicable(Rule.ContestedStongholdsCountAsOccupied)) ? 1 : 0) +
                        (p.Controls(this, Map.TueksSietch, Applicable(Rule.ContestedStongholdsCountAsOccupied)) ? 1 : 0) +
                        (IsSpecialStronghold(Map.ShieldWall) && p.Controls(this, Map.ShieldWall, Applicable(Rule.ContestedStongholdsCountAsOccupied)) ? 1 : 0));
                }

                int mostStrongholds = nrOfStrongholdsPerPlayer.Values.Max();
                Winners.AddRange(nrOfStrongholdsPerPlayer.Where(nr => nr.Value == mostStrongholds).Select(nr => nr.Key));
            }
        }

        private void CallHeroesHome()
        {
            foreach (var ls in LeaderState)
            {
                ls.Value.CurrentTerritory = null;
            }
        }

        #endregion MentatPhase

    }
}