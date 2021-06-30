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
        #region Grey

        protected virtual ReplacedCardWon DetermineReplacedCardWon()
        {
            var replace = Game.CardJustWon != null && !WannaHave(Game.CardJustWon);
            return new ReplacedCardWon(Game) { Initiator = Faction, Passed = !replace };
        }

        protected GreyRemovedCardFromAuction DetermineGreyRemovedCardFromAuction()
        {
            TreacheryCard toBeRemoved = null;
            if (TreacheryCards.Any(c => c.Type == TreacheryCardType.ProjectileAndPoison)) toBeRemoved = Game.CardsOnAuction.Items.FirstOrDefault(c => c.Type == TreacheryCardType.ShieldAndAntidote);
            if (toBeRemoved == null && !TreacheryCards.Any(c => c.Type == TreacheryCardType.ShieldAndAntidote)) toBeRemoved = Game.CardsOnAuction.Items.FirstOrDefault(c => c.Type == TreacheryCardType.ProjectileAndPoison);

            if (toBeRemoved == null && TreacheryCards.Any(c => c.IsProjectileWeapon)) toBeRemoved = Game.CardsOnAuction.Items.FirstOrDefault(c => c.IsProjectileDefense);
            if (toBeRemoved == null && TreacheryCards.Any(c => c.IsPoisonWeapon)) toBeRemoved = Game.CardsOnAuction.Items.FirstOrDefault(c => c.IsPoisonDefense);
            if (toBeRemoved == null && !TreacheryCards.Any(c => c.IsProjectileDefense)) toBeRemoved = Game.CardsOnAuction.Items.FirstOrDefault(c => c.IsProjectileWeapon);
            if (toBeRemoved == null && !TreacheryCards.Any(c => c.IsPoisonDefense)) toBeRemoved = Game.CardsOnAuction.Items.FirstOrDefault(c => c.IsPoisonWeapon);

            if (toBeRemoved == null)
            {
                if (TreacheryCards.Count >= 3 || ResourcesIncludingAllyAndRedContribution < 5)
                {
                    //Remove the best card from auction
                    toBeRemoved = Game.CardsOnAuction.Items.OrderBy(c => CardQuality(c)).FirstOrDefault();
                }
                else
                {
                    //Remove the worst card from auction
                    toBeRemoved = Game.CardsOnAuction.Items.OrderBy(c => CardQuality(c)).FirstOrDefault();
                }
            }

            if (toBeRemoved == null) Game.CardsOnAuction.Items.FirstOrDefault();

            bool putOnTop = CardQuality(toBeRemoved) <= 2 && Ally == Faction.None || CardQuality(toBeRemoved) >= 4 && Ally != Faction.None;

            return new GreyRemovedCardFromAuction(Game) { Initiator = Faction, Card = toBeRemoved, PutOnTop = putOnTop };
        }

        protected GreySwappedCardOnBid DetermineGreySwappedCardOnBid()
        {
            var card = TreacheryCards.FirstOrDefault(c => c.Type == TreacheryCardType.Useless);

            if (card == null) card = TreacheryCards.FirstOrDefault(c =>
                c.Type == TreacheryCardType.Clairvoyance ||
                c.Type == TreacheryCardType.Thumper ||
                c.Type == TreacheryCardType.Harvester ||
                c.Type == TreacheryCardType.Mercenary ||
                c.Type == TreacheryCardType.StormSpell ||
                c.Type == TreacheryCardType.Metheor);

            if (card == null && TreacheryCards.Count(c => c.Type == TreacheryCardType.Shield) > 1) card = TreacheryCards.FirstOrDefault(c => c.Type == TreacheryCardType.Shield);

            if (card == null && TreacheryCards.Count(c => c.Type == TreacheryCardType.Antidote) > 1) card = TreacheryCards.FirstOrDefault(c => c.Type == TreacheryCardType.Antidote);

            if (card != null)
            {
                return new GreySwappedCardOnBid(Game) { Initiator = Faction, Passed = false, Card = card };
            }
            else
            {
                return new GreySwappedCardOnBid(Game) { Initiator = Faction, Passed = true };
            }
        }

        protected GreySelectedStartingCard DetermineGreySelectedStartingCard()
        {
            var cards = Game.StartingTreacheryCards.Items;

            var card = cards.FirstOrDefault(c => c.Type == TreacheryCardType.ProjectileAndPoison);
            if (card == null) card = cards.FirstOrDefault(c => c.Type == TreacheryCardType.ShieldAndAntidote);
            if (card == null) card = cards.FirstOrDefault(c => c.Type == TreacheryCardType.Laser);

            if (card == null && cards.Any(c => c.IsPoisonWeapon) && !cards.Any(c => c.IsPoisonDefense))
            {
                card = cards.FirstOrDefault(c => c.IsPoisonWeapon);
            }

            if (card == null && cards.Any(c => c.IsProjectileWeapon) && !cards.Any(c => c.IsProjectileDefense))
            {
                card = cards.FirstOrDefault(c => c.IsProjectileWeapon);
            }

            if (card == null) card = cards.FirstOrDefault(c => c.Type == TreacheryCardType.WeirdingWay);
            if (card == null) card = cards.FirstOrDefault(c => c.Type == TreacheryCardType.Chemistry);
            if (card == null) card = cards.FirstOrDefault(c => c.IsProjectileWeapon);
            if (card == null) card = cards.FirstOrDefault(c => c.IsPoisonWeapon);
            if (card == null) card = cards.FirstOrDefault(c => c.IsProjectileDefense);
            if (card == null) card = cards.FirstOrDefault(c => c.IsPoisonDefense);

            if (card == null) card = cards.FirstOrDefault(c => c.Type != TreacheryCardType.Useless);

            if (card == null) card = cards.FirstOrDefault();

            return new GreySelectedStartingCard(Game) { Initiator = Faction, Card = card };
        }

        protected PerformHmsMovement DeterminePerformHmsMovement()
        {
            var currentLocation = Game.Map.HiddenMobileStronghold.AttachedToLocation;

            var richestAdjacentSpiceLocation = PerformHmsMovement.ValidLocations(Game).Where(l => l != currentLocation && ResourcesIn(l) > 0).OrderByDescending(l => ResourcesIn(l)).FirstOrDefault();
            if (richestAdjacentSpiceLocation != null)
            {
                return new PerformHmsMovement(Game) { Initiator = Faction, Passed = false, Target = richestAdjacentSpiceLocation };
            }

            var reachableFromCurrentLocation = Game.Map.FindNeighbours(currentLocation, Game.HmsMovesLeft, false, Faction, Game.SectorInStorm, Game.ForcesOnPlanet);

            var richestReachableSpiceLocation = reachableFromCurrentLocation.Where(l => l != currentLocation && ResourcesIn(l) > 0).OrderByDescending(l => ResourcesIn(l)).FirstOrDefault();
            if (richestReachableSpiceLocation != null)
            {
                var nextStepTowardsSpice = PerformHmsMovement.ValidLocations(Game).Where(l => WithinDistance(l, richestReachableSpiceLocation, 1)).FirstOrDefault();

                if (nextStepTowardsSpice != null)
                {
                    return new PerformHmsMovement(Game) { Initiator = Faction, Passed = false, Target = nextStepTowardsSpice };
                }
                else
                {
                    nextStepTowardsSpice = PerformHmsMovement.ValidLocations(Game).Where(l => WithinDistance(l, richestReachableSpiceLocation, 2)).FirstOrDefault();
                    return new PerformHmsMovement(Game) { Initiator = Faction, Passed = false, Target = nextStepTowardsSpice };
                }
            }

            //If there is nowhere to go, move toward Imperial Basin or Polar Sink
            Location alternativeLocation = null;
            if (Game.ForcesOnPlanet[Game.Map.Arrakeen].Sum(b => b.TotalAmountOfForces) == 0 || Game.ForcesOnPlanet[Game.Map.Carthag].Sum(b => b.TotalAmountOfForces) == 0)
            {
                alternativeLocation = Game.Map.ImperialBasin.MiddleLocation;
            }
            else
            {
                alternativeLocation = Game.Map.PolarSink;
            }

            if (alternativeLocation != currentLocation)
            {
                var nextStepTowardsAlternative = PerformHmsMovement.ValidLocations(Game).FirstOrDefault(l => l == alternativeLocation);
                if (nextStepTowardsAlternative != null)
                {
                    return new PerformHmsMovement(Game) { Initiator = Faction, Passed = false, Target = nextStepTowardsAlternative };
                }

                nextStepTowardsAlternative = PerformHmsMovement.ValidLocations(Game).Where(l => WithinDistance(l, alternativeLocation, 1)).FirstOrDefault();
                if (nextStepTowardsAlternative != null)
                {
                    return new PerformHmsMovement(Game) { Initiator = Faction, Passed = false, Target = nextStepTowardsAlternative };
                }

                nextStepTowardsAlternative = PerformHmsMovement.ValidLocations(Game).Where(l => WithinDistance(l, alternativeLocation, 2)).FirstOrDefault();
                if (nextStepTowardsAlternative != null)
                {
                    return new PerformHmsMovement(Game) { Initiator = Faction, Passed = false, Target = nextStepTowardsAlternative };
                }
            }

            return new PerformHmsMovement(Game) { Initiator = Faction, Passed = true };
        }

        protected PerformHmsPlacement DeterminePerformHmsPlacement()
        {
            if (Faction == Faction.Grey)
            {
                if (Game.ForcesOnPlanet[Game.Map.Arrakeen].Sum(b => b.TotalAmountOfForces) == 0 || Game.ForcesOnPlanet[Game.Map.Carthag].Sum(b => b.TotalAmountOfForces) == 0)
                {
                    return new PerformHmsPlacement(Game) { Initiator = Faction, Target = PerformHmsPlacement.ValidLocations(Game, this).First(l => l.Territory == Game.Map.ImperialBasin) };
                }
                else
                {
                    return new PerformHmsPlacement(Game) { Initiator = Faction, Target = Game.Map.PolarSink };
                }
            }
            else
            {
                return new PerformHmsPlacement(Game) { Initiator = Faction, Target = Game.Map.ShieldWall.Locations.First() };
            }
        }

        #endregion Grey

        #region Yellow

        protected TakeLosses DetermineTakeLosses()
        {
            int normalForces = Math.Min(TakeLosses.LossesToTake(Game), TakeLosses.ValidForceAmounts(Game, this).Max());
            int specialForces = TakeLosses.LossesToTake(Game) - normalForces;
            return new TakeLosses(Game) { Initiator = Faction, ForceAmount = normalForces, SpecialForceAmount = specialForces };
        }

        protected PerformYellowSetup DeterminePerformYellowSetup()
        {
            var forceLocations = new Dictionary<Location, Battalion>();
            forceLocations.Add(Game.Map.FalseWallSouth.MiddleLocation, new Battalion() { Faction = Faction, AmountOfForces = 3 + D(1, 4), AmountOfSpecialForces = SpecialForcesInReserve > 0 ? 1 : 0 });
            //forceLocations.Add(Game.Map.SietchTabr, new Battalion() { Faction = Faction, AmountOfForces = Math.Min(10 - forceLocations.Sum(kvp => kvp.Value.TotalAmountOfForces), 1 + D(1, 4)), AmountOfSpecialForces = 0 });
            int forcesLeft = 10 - forceLocations.Sum(kvp => kvp.Value.TotalAmountOfForces);

            if (forcesLeft > 0)
            {
                var amountOfSpecialForces = SpecialForcesInReserve > 1 ? 1 : 0;
                forceLocations.Add(Game.Map.FalseWallWest.MiddleLocation, new Battalion() { Faction = Faction, AmountOfForces = forcesLeft - amountOfSpecialForces, AmountOfSpecialForces = amountOfSpecialForces });
            }

            return new PerformYellowSetup(Game) { Initiator = Faction, ForceLocations = forceLocations };
        }

        protected YellowSentMonster DetermineYellowSentMonster()
        {
            var target = YellowSentMonster.ValidTargets(Game).OrderByDescending(t => TotalMaxDialOfOpponents(t)).FirstOrDefault();
            return new YellowSentMonster(Game) { Initiator = Faction, Territory = target };
        }

        protected YellowRidesMonster DetermineYellowRidesMonster()
        {
            Location target = null;
            var validLocations = YellowRidesMonster.ValidTargets(Game, this).ToList();
            var battalionsToMove = ForcesOnPlanet.Where(forcesAtLocation => YellowRidesMonster.ValidSources(Game).Contains(forcesAtLocation.Key.Territory));
            var nrOfForces = battalionsToMove.Sum(forcesAtLocation => forcesAtLocation.Value.TotalAmountOfForces);

            if (validLocations.Contains(Game.Map.TueksSietch) && VacantAndSafeFromStorm(Game.Map.TueksSietch)) target = Game.Map.TueksSietch;
            if (target == null && validLocations.Contains(Game.Map.Carthag) && VacantAndSafeFromStorm(Game.Map.Carthag)) target = Game.Map.Carthag;
            if (target == null && validLocations.Contains(Game.Map.Arrakeen) && VacantAndSafeFromStorm(Game.Map.Arrakeen)) target = Game.Map.Arrakeen;
            if (target == null && validLocations.Contains(Game.Map.HabbanyaSietch) && VacantAndSafeFromStorm(Game.Map.HabbanyaSietch)) target = Game.Map.HabbanyaSietch;

            if (target == null && Game.LatestSpiceCardA != null && validLocations.Contains(Game.LatestSpiceCardA.Location) && Game.ResourcesOnPlanet.ContainsKey(Game.LatestSpiceCardA.Location) && VacantAndSafeFromStorm(Game.LatestSpiceCardA.Location)) target = Game.LatestSpiceCardA.Location;
            if (target == null && Game.LatestSpiceCardB != null && validLocations.Contains(Game.LatestSpiceCardB.Location) && Game.ResourcesOnPlanet.ContainsKey(Game.LatestSpiceCardB.Location) && VacantAndSafeFromStorm(Game.LatestSpiceCardB.Location)) target = Game.LatestSpiceCardB.Location;
            if (target == null) target = Game.ResourcesOnPlanet.OrderByDescending(r => r.Value).FirstOrDefault(l => validLocations.Contains(l.Key) && VacantAndSafeFromStorm(l.Key)).Key;

            if (target == null && Game.LatestSpiceCardA != null && validLocations.Contains(Game.LatestSpiceCardA.Location) && Game.ResourcesOnPlanet.ContainsKey(Game.LatestSpiceCardA.Location) && TotalMaxDialOfOpponents(Game.LatestSpiceCardA.Location.Territory) + 3 < nrOfForces) target = Game.LatestSpiceCardA.Location;
            if (target == null && Game.LatestSpiceCardB != null && validLocations.Contains(Game.LatestSpiceCardB.Location) && Game.ResourcesOnPlanet.ContainsKey(Game.LatestSpiceCardB.Location) && TotalMaxDialOfOpponents(Game.LatestSpiceCardB.Location.Territory) + 3 < nrOfForces) target = Game.LatestSpiceCardB.Location;
            if (target == null) target = Game.ResourcesOnPlanet.OrderByDescending(r => r.Value).FirstOrDefault(l => validLocations.Contains(l.Key) && TotalMaxDialOfOpponents(l.Key.Territory) + 3 < nrOfForces).Key;

            if (target == null) target = Game.Map.PolarSink;

            return new YellowRidesMonster(Game) { Initiator = Faction, Passed = false, ForceLocations = new Dictionary<Location, Battalion>(battalionsToMove), To = target };
        }

        #endregion Yellow

        #region Blue

        private GameEvent DetermineBluePlacement()
        {
            Location target;
            if (AnyForcesIn(Game.Map.Arrakeen) == 0) target = Game.Map.Carthag;
            else if (AnyForcesIn(Game.Map.Carthag) == 0) target = Game.Map.Arrakeen;
            else if (AnyForcesIn(Game.Map.HabbanyaSietch) == 0) target = Game.Map.HabbanyaSietch;
            else target = Game.Map.Carthag;

            return new PerformBluePlacement(Game) { Initiator = Faction, Target = target };
        }

        private BlueFlip DetermineBlueFlip()
        {
            var territory = BlueFlip.GetTerritory(Game);

            if (IWantToBeFightersIn(territory))
            {
                return new BlueFlip(Game) { Initiator = Faction, AsAdvisors = false };
            }
            else
            {
                return new BlueFlip(Game) { Initiator = Faction, AsAdvisors = true };
            }
        }

        private bool IWantToBeFightersIn(Territory territory)
        {
            if (!territory.IsStronghold) return false;

            var opponent = GetOpponentThatOccupies(territory);

            if (opponent != null)
            {
                var potentialWinningOpponents = Game.Players.Where(p => p != this && p != AlliedPlayer && Game.MeetsNormalVictoryCondition(p, true) && Game.CountChallengedStongholds(p) < 2);
                var amountICanReinforce = MaxReinforcedDialTo(this, territory);
                var maxDial = MaxDial(this, territory, opponent.Faction);

                if (WinWasPredictedByMeThisTurn(opponent.Faction) || GetDialNeeded(territory, opponent, false) <= maxDial || potentialWinningOpponents.Contains(opponent) && GetDialNeeded(territory, opponent, false) <= amountICanReinforce + maxDial)
                {
                    return true;
                }
            }

            return false;
        }

        private BlueBattleAnnouncement DetermineBlueBattleAnnouncement()
        {
            if (Game.CurrentMainPhase == MainPhase.Resurrection && NrOfBattlesToFight < 2)
            {
                var territory = BlueBattleAnnouncement.ValidTerritories(Game, this).OrderBy(t => GetDialNeeded(t, GetOpponentThatOccupies(t), false)).FirstOrDefault(t => IWantToAnnounceBattleIn(t));

                if (territory != null)
                {
                    return new BlueBattleAnnouncement(Game) { Initiator = Faction, Territory = territory };
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        private bool IWantToAnnounceBattleIn(Territory territory)
        {
            var opponent = GetOpponentThatOccupies(territory);

            if (opponent != null)
            {
                var dialNeeded = GetDialNeeded(territory, opponent, true);
                var forcesIn = SpecialForcesIn(territory);

                if (territory.IsStronghold && WinWasPredictedByMeThisTurn(opponent.Faction) || dialNeeded <= forcesIn + MaxReinforcedDialTo(this, territory))
                {
                    LogInfo("IWantToAnnounceBattleIn {0}, {1} <= {2}", territory, dialNeeded, forcesIn);
                    return true;
                }

                LogInfo("IDontWantToAnnounceBattleIn {0}, {1} > {2}", territory, dialNeeded, forcesIn);
            }

            return false;
        }

        private BlueAccompanies DetermineBlueAccompanies()
        {
            var target = BlueAccompanies.ValidTargets(Game, this).FirstOrDefault(l => l.IsStronghold || !Game.Applicable(Rule.BlueAccompaniesToShipmentLocation));

            bool shippingOpponentCanWin = false;
            if (target != null && target != Game.Map.PolarSink)
            {
                var opponent = GetOpponentThatOccupies(target);
                var potentialWinningOpponents = Game.Players.Where(p => p != this && p != AlliedPlayer && Game.MeetsNormalVictoryCondition(p, true) && Game.CountChallengedStongholds(p) < 2);
                shippingOpponentCanWin = potentialWinningOpponents.Contains(opponent);
            }

            if (target != null && !shippingOpponentCanWin && ForcesInReserve > 3 && (AnyForcesIn(target) > 0 || ForcesOnPlanet.Count() < 4 && ForcesOnPlanet.Count(kvp => IsStronghold(kvp.Key)) < 3) && (target != Game.Map.PolarSink || AnyForcesIn(target) < 8))
            {
                return new BlueAccompanies(Game) { Initiator = Faction, Location = target, Accompanies = true };
            }
            else
            {
                return new BlueAccompanies(Game) { Initiator = Faction, Location = null, Accompanies = false };
            }
        }

        private BluePrediction DetermineBluePrediction()
        {
            var factionId = D(1, BluePrediction.ValidTargets(Game, this).Count()) - 1;
            var faction = BluePrediction.ValidTargets(Game, this).ElementAt(factionId);

            int turn;
            if (faction == Faction.Black)
            {
                if (D(1, 2) == 1) turn = D(1, Math.Min(3, Game.MaximumNumberOfTurns));
                else turn = D(1, Game.MaximumNumberOfTurns);
            }
            else
            {
                if (D(1, 2) == 1) turn = D(1, Math.Min(5, Game.MaximumNumberOfTurns));
                else turn = 2 + D(1, Game.MaximumNumberOfTurns - 2);
            }

            return new BluePrediction(Game) { Initiator = Faction, ToWin = faction, Turn = turn };
        }

        private VoicePlan voicePlan = null;
        protected virtual Voice DetermineVoice()
        {
            LogInfo("DetermineVoice()");

            voicePlan = null;

            if (Game.CurrentBattle.Initiator == Faction || Game.CurrentBattle.Target == Faction)
            {
                voicePlan = BestVoice(Game.CurrentBattle, this, Game.CurrentBattle.OpponentOf(Faction));
                LogInfo(voicePlan.voice.GetMessage().ToString());
                return voicePlan.voice;
            }

            return null;
        }

        private VoicePlan BestVoice(BattleInitiated battle, Player player, Player opponent)
        {
            var result = new VoicePlan
            {
                battle = battle,
                playerHeroWillCertainlySurvive = false,
                opponentHeroWillCertainlyBeZero = false
            };

            if (WinWasPredictedByMeThisTurn(opponent.Faction))
            {
                result.weaponToUse = null;
                result.defenseToUse = null;
                result.voice = new Voice(Game) { Initiator = Faction, Must = true, Type = TreacheryCardType.Laser };
                result.opponentHeroWillCertainlyBeZero = true;
                result.playerHeroWillCertainlySurvive = true;
            }

            var knownOpponentDefenses = KnownOpponentDefenses(opponent);
            var knownOpponentWeapons = KnownOpponentWeapons(opponent);
            int nrOfUnknownOpponentCards = NrOfUnknownOpponentCards(opponent);

            var weapons = Weapons(null).Where(w => w.Type != TreacheryCardType.Useless && w.Type != TreacheryCardType.ArtilleryStrike && w.Type != TreacheryCardType.PoisonTooth);
            result.weaponToUse = weapons.FirstOrDefault(w => w.Type == TreacheryCardType.ProjectileAndPoison); //use poisonblade if available
            if (result.weaponToUse == null) result.weaponToUse = weapons.FirstOrDefault(w => w.Type == TreacheryCardType.Laser); //use lasgun if available
            if (result.weaponToUse == null) result.weaponToUse = weapons.FirstOrDefault(w => Game.KnownCards(this).Contains(w)); //use a known weapon if available
            if (result.weaponToUse == null) result.weaponToUse = weapons.FirstOrDefault(); //use any weapon

            TreacheryCardType type = TreacheryCardType.None;
            bool must = false;

            bool opponentMightHaveDefenses = !(knownOpponentDefenses.Any() && nrOfUnknownOpponentCards == 0);

            var cardsPlayerHasOrMightHave = CardsPlayerHasOrMightHave(opponent);

            if (opponentMightHaveDefenses && result.weaponToUse != null)
            {
                result.opponentHeroWillCertainlyBeZero = true;

                //opponent might have defenses and player has a weapon. Use voice to disable the corresponding defense, if possible by forcing use of the wrong defense and otherwise by forcing not to use the correct defense.
                if (result.weaponToUse.Type == TreacheryCardType.ProjectileAndPoison)
                {
                    var uselessDefense = knownOpponentDefenses.FirstOrDefault(d => d.Type != TreacheryCardType.ShieldAndAntidote);
                    if (uselessDefense != null)
                    {
                        must = true;
                        type = uselessDefense.Type;
                    }
                    else if (cardsPlayerHasOrMightHave.Any(c => c.Type == TreacheryCardType.ShieldAndAntidote))
                    {
                        must = false;
                        type = TreacheryCardType.ShieldAndAntidote;
                    }
                }

                if (type == TreacheryCardType.None && result.weaponToUse.IsLaser)
                {
                    var uselessDefense = knownOpponentDefenses.FirstOrDefault(d => d.IsPoisonDefense && !d.IsShield);
                    if (uselessDefense != null)
                    {
                        must = true;
                        type = uselessDefense.Type;
                    }
                    else if (cardsPlayerHasOrMightHave.Any(c => c.IsShield))
                    {
                        must = false;
                        type = TreacheryCardType.Shield;
                    }
                }

                if (type == TreacheryCardType.None && result.weaponToUse.IsProjectileWeapon)
                {
                    var uselessDefense = knownOpponentDefenses.FirstOrDefault(d => d.IsPoisonDefense && !d.IsProjectileDefense);
                    if (uselessDefense != null)
                    {
                        must = true;
                        type = uselessDefense.Type;
                    }
                    else
                    {
                        must = false;

                        if (!knownOpponentDefenses.Any(d => d.IsProjectileDefense && d.Type != TreacheryCardType.WeirdingWay)
                            && knownOpponentDefenses.Any(d => d.Type == TreacheryCardType.WeirdingWay)
                            && knownOpponentWeapons.Any(d => d.IsWeapon)
                            && cardsPlayerHasOrMightHave.Any(c => c.Type == TreacheryCardType.WeirdingWay))
                        {
                            type = TreacheryCardType.WeirdingWay;
                        }
                        else if (!Game.Applicable(Rule.BlueVoiceMustNameSpecialCards) && cardsPlayerHasOrMightHave.Any(c => c.IsProjectileDefense))
                        {
                            type = TreacheryCardType.ProjectileDefense;
                        }
                        else if (cardsPlayerHasOrMightHave.Any(c => c.Type == TreacheryCardType.ShieldAndAntidote))
                        {
                            type = TreacheryCardType.ShieldAndAntidote;
                        }
                        else if (cardsPlayerHasOrMightHave.Any(c => c.Type == TreacheryCardType.Shield))
                        {
                            type = TreacheryCardType.Shield;
                        }
                    }
                }

                if (type == TreacheryCardType.None && result.weaponToUse.IsPoisonWeapon)
                {
                    var uselessDefense = knownOpponentDefenses.FirstOrDefault(d => d.IsProjectileDefense && !d.IsProjectileDefense);
                    if (uselessDefense != null)
                    {
                        must = true;
                        type = uselessDefense.Type;
                    }
                    else if (!Game.Applicable(Rule.BlueVoiceMustNameSpecialCards) && cardsPlayerHasOrMightHave.Any(c => c.IsPoisonDefense))
                    {
                        must = false;
                        type = TreacheryCardType.PoisonDefense;
                    }
                    else if (cardsPlayerHasOrMightHave.Any(c => c.Type == TreacheryCardType.ShieldAndAntidote))
                    {
                        must = false;
                        type = TreacheryCardType.ShieldAndAntidote;
                    }
                    else if (cardsPlayerHasOrMightHave.Any(c => c.Type == TreacheryCardType.Chemistry))
                    {
                        must = false;
                        type = TreacheryCardType.Chemistry;
                    }
                    else if (cardsPlayerHasOrMightHave.Any(c => c.Type == TreacheryCardType.Antidote))
                    {
                        must = false;
                        type = TreacheryCardType.Antidote;
                    }
                }
            }

            if (type == TreacheryCardType.None)
            {
                bool opponentMightHaveWeapons = !(knownOpponentWeapons.Any() && nrOfUnknownOpponentCards == 0);

                //I have no weapon or the opponent has no defense. Focus on disabling their weapon.
                DetermineBestDefense(opponent, null, out result.defenseToUse);

                if (opponentMightHaveWeapons && result.defenseToUse != null)
                {
                    //opponent might have weapons and player has a defense. Use voice to disable the corresponding weapon, if possible by forcing use of the wrong weapon and otherwise by forcing not to use the correct weapon.
                    if (result.defenseToUse.Type == TreacheryCardType.ShieldAndAntidote)
                    {
                        var uselessWeapon = knownOpponentWeapons.FirstOrDefault(w => w.Type != TreacheryCardType.Laser && w.Type != TreacheryCardType.PoisonTooth);
                        if (uselessWeapon != null)
                        {
                            must = true;
                            type = uselessWeapon.Type;
                            result.playerHeroWillCertainlySurvive = true;
                        }
                        else if (knownOpponentWeapons.Any(w => w.Type == TreacheryCardType.Laser))
                        {
                            must = false;
                            type = TreacheryCardType.Laser;
                        }
                        else if (knownOpponentWeapons.Any(w => w.Type == TreacheryCardType.PoisonTooth))
                        {
                            must = false;
                            type = TreacheryCardType.PoisonTooth;
                        }
                    }

                    if (type == TreacheryCardType.None && result.defenseToUse.IsProjectileDefense)
                    {
                        var uselessWeapon = knownOpponentWeapons.FirstOrDefault(w => w.IsProjectileWeapon && !w.IsPoisonWeapon);
                        if (uselessWeapon != null)
                        {
                            must = true;
                            type = uselessWeapon.Type;
                            result.playerHeroWillCertainlySurvive = true;
                        }
                        else if (knownOpponentWeapons.Any(w => w.Type == TreacheryCardType.Laser) && nrOfUnknownOpponentCards == 0)
                        {
                            must = false;
                            type = TreacheryCardType.Laser;
                            result.playerHeroWillCertainlySurvive = true;
                        }
                        else if (knownOpponentWeapons.Any(w => w.Type == TreacheryCardType.PoisonTooth) && nrOfUnknownOpponentCards == 0)
                        {
                            must = false;
                            type = TreacheryCardType.PoisonTooth;
                            result.playerHeroWillCertainlySurvive = true;
                        }
                        else
                        {
                            if (!knownOpponentWeapons.Any(d => d.IsPoisonWeapon && d.Type != TreacheryCardType.Chemistry)
                                && knownOpponentWeapons.Any(d => d.Type == TreacheryCardType.Chemistry)
                                && knownOpponentDefenses.Any(d => d.IsDefense)
                                && cardsPlayerHasOrMightHave.Any(c => c.Type == TreacheryCardType.Chemistry))
                            {
                                must = false;
                                type = TreacheryCardType.Chemistry;
                            }
                            else if (cardsPlayerHasOrMightHave.Any(c => c.IsPoisonWeapon))
                            {
                                must = false;
                                type = TreacheryCardType.Poison;
                            }
                        }
                    }

                    if (type == TreacheryCardType.None && result.defenseToUse.IsPoisonDefense)
                    {
                        var uselessWeapon = knownOpponentWeapons.FirstOrDefault(w => !w.IsProjectileWeapon && w.IsPoisonWeapon);
                        if (uselessWeapon != null)
                        {
                            must = true;
                            type = uselessWeapon.Type;
                            result.playerHeroWillCertainlySurvive = true;
                        }
                        else if (knownOpponentWeapons.Any(w => w.Type == TreacheryCardType.Laser) && nrOfUnknownOpponentCards == 0)
                        {
                            must = false;
                            type = TreacheryCardType.Laser;
                            result.playerHeroWillCertainlySurvive = true;
                        }
                        else if (knownOpponentWeapons.Any(w => w.Type == TreacheryCardType.PoisonTooth) && nrOfUnknownOpponentCards == 0)
                        {
                            must = false;
                            type = TreacheryCardType.PoisonTooth;
                            result.playerHeroWillCertainlySurvive = true;
                        }
                        else if (cardsPlayerHasOrMightHave.Any(c => c.IsProjectileWeapon))
                        {
                            must = false;
                            type = TreacheryCardType.Projectile;
                        }
                    }
                }
            }

            if (type == TreacheryCardType.None && opponent.TreacheryCards.Any(c => Game.KnownCards(this).Contains(c) && c.Type == TreacheryCardType.Mercenary))
            {
                type = TreacheryCardType.Mercenary;
                must = true;
            }

            if (type == TreacheryCardType.None)
            {
                must = false;
                if (D(1, 2) > 1)
                {
                    if (cardsPlayerHasOrMightHave.Any(c => c.IsProjectileWeapon))
                    {
                        type = TreacheryCardType.Projectile;
                    }
                    else
                    {
                        type = TreacheryCardType.Poison;
                    }
                }
                else
                {
                    if (cardsPlayerHasOrMightHave.Any(c => c.IsPoisonWeapon))
                    {
                        type = TreacheryCardType.Poison;
                    }
                    else
                    {
                        type = TreacheryCardType.Projectile;
                    }
                }
            }

            var voice = new Voice(Game) { Initiator = player.Faction, Must = must, Type = type };
            result.voice = voice;

            return result;
        }

        #endregion Blue

        #region Green

        protected virtual Prescience DeterminePrescience()
        {
            if (Game.CurrentBattle.Initiator == Faction || Game.CurrentBattle.Target == Faction)
            {
                var opponent = Game.CurrentBattle.OpponentOf(Faction);
                return BestPrescience(opponent, MaxDial(this, Game.CurrentBattle.Territory, opponent.Faction));
            }

            return null;
        }

        protected Prescience BestPrescience(Player opponent, float maxForceStrengthInBattle)
        {
            var myDefenses = Battle.ValidDefenses(Game, this, null).Where(c => Game.KnownCards(this).Contains(c));
            var myWeapons = Battle.ValidWeapons(Game, this, null).Where(c => Game.KnownCards(this).Contains(c));

            var knownOpponentDefenses = Battle.ValidDefenses(Game, opponent, null).Where(c => Game.KnownCards(this).Contains(c));
            var knownOpponentWeapons = Battle.ValidWeapons(Game, opponent, null).Where(c => Game.KnownCards(this).Contains(c));
            //int nrOfUnknownOpponentCards = opponent.TreacheryCards.Count(c => !Game.KnownCards(this).Contains(c));

            var cardsOpponentHasOrMightHave = CardsPlayerHasOrMightHave(opponent);
            bool weaponIsCertain = CountDifferentWeaponTypes(cardsOpponentHasOrMightHave) <= 1;
            bool defenseIsCertain = CountDifferentDefenseTypes(cardsOpponentHasOrMightHave) <= 1;

            bool iHaveShieldSnooper = myDefenses.Any(d => d.Type == TreacheryCardType.ShieldAndAntidote);
            bool iHavePoisonBlade = myWeapons.Any(d => d.Type == TreacheryCardType.ProjectileAndPoison);

            PrescienceAspect aspect;
            if (!weaponIsCertain && myDefenses.Any(d => d.IsProjectileDefense) && myDefenses.Any(d => d.IsPoisonDefense) && !iHaveShieldSnooper)
            {
                //I dont have shield snooper and I have choice between shield and snooper, therefore ask for the weapon used
                aspect = PrescienceAspect.Weapon;
            }
            else if (!defenseIsCertain && myWeapons.Any(d => d.IsProjectileWeapon) && myWeapons.Any(d => d.IsPoisonWeapon) && !iHavePoisonBlade)
            {
                //I dont have poison blade and I have choice between poison weapon and projectile weapon, therefore ask for the defense used
                aspect = PrescienceAspect.Defense;
            }
            else if (!weaponIsCertain && myDefenses.Any() && !iHaveShieldSnooper)
            {
                aspect = PrescienceAspect.Weapon;
            }
            else if (!defenseIsCertain && myWeapons.Any() && !iHavePoisonBlade)
            {
                aspect = PrescienceAspect.Defense;
            }
            else if (maxForceStrengthInBattle > 2)
            {
                aspect = PrescienceAspect.Dial;
            }
            else
            {
                aspect = PrescienceAspect.Leader;
            }

            return new Prescience(Game) { Initiator = Faction, Aspect = aspect };
        }

        #endregion Green

        #region Purple

        protected SetIncreasedRevivalLimits DetermineSetIncreasedRevivalLimits()
        {
            var targets = SetIncreasedRevivalLimits.ValidTargets(Game, this).ToArray();
            if (Game.FactionsWithIncreasedRevivalLimits.Length != targets.Length)
            {
                return new SetIncreasedRevivalLimits(Game) { Initiator = Faction, Factions = targets };
            }
            else
            {
                return null;
            }
        }

        protected virtual FaceDanced DetermineFaceDanced()
        {
            if (FaceDanced.MayCallFaceDancer(Game, this))
            {
                var facedancer = FaceDancers.FirstOrDefault(f => Game.WinnerHero.IsFaceDancer(f));
                var facedancedHeroIsLivingLeader = facedancer is Leader && Game.IsAlive(facedancer);

                if ((FaceDanced.MaximumNumberOfForces(Game, this) > 0 || facedancedHeroIsLivingLeader) && Game.BattleWinner != Ally)
                {
                    var forcesFromPlanet = new Dictionary<Location, Battalion>();

                    int toPlace = FaceDanced.MaximumNumberOfForces(Game, this);

                    var biggest = BiggestBattalionThreatenedByStormWithoutSpice;
                    if (biggest.Key != null)
                    {
                        var toTake = biggest.Value.Take(toPlace, false);
                        forcesFromPlanet.Add(biggest.Key, toTake);
                        toPlace -= toTake.TotalAmountOfForces;
                    }

                    int fromReserves = Math.Min(ForcesInReserve, toPlace);

                    var targetLocation = FaceDanced.ValidTargetLocations(Game).FirstOrDefault(l => Game.ResourcesOnPlanet.ContainsKey(l));
                    if (targetLocation == null) targetLocation = FaceDanced.ValidTargetLocations(Game).FirstOrDefault();

                    var targetLocations = new Dictionary<Location, Battalion>
                    {
                        { targetLocation, new Battalion() { AmountOfForces = forcesFromPlanet.Sum(kvp => kvp.Value.AmountOfForces) + fromReserves, AmountOfSpecialForces = forcesFromPlanet.Sum(kvp => kvp.Value.AmountOfSpecialForces) } }
                    };

                    var result = new FaceDanced(Game) { Initiator = Faction, FaceDancerCalled = true, ForceLocations = forcesFromPlanet, ForcesFromReserve = fromReserves, TargetForceLocations = targetLocations };
                    LogInfo(result.GetMessage().ToString());
                    return result;
                }
            }

            return new FaceDanced(Game) { Initiator = Faction, FaceDancerCalled = false };
        }

        protected virtual FaceDancerReplaced DetermineFaceDancerReplaced()
        {
            var replacable = FaceDancers.Where(f => !RevealedDancers.Contains(f)).OrderBy(f => f.Value);
            var toReplace = replacable.FirstOrDefault(f => Leaders.Contains(f) || (Ally != Faction.None && AlliedPlayer.Leaders.Contains(f)));
            if (toReplace == null) toReplace = replacable.FirstOrDefault(f => f is Leader && !Game.LeaderState[f].Alive);

            if (toReplace != null)
            {
                return new FaceDancerReplaced(Game) { Initiator = Faction, Passed = false, SelectedDancer = toReplace };
            }
            else
            {
                return new FaceDancerReplaced(Game) { Initiator = Faction, Passed = true };
            }
        }

        private readonly Dictionary<IHero, int> priceSetEarlier = new Dictionary<IHero, int>();

        protected virtual RequestPurpleRevival DetermineRequestPurpleRevival()
        {
            if (Game.CurrentPurpleRevivalRequest != null || Game.AllowedEarlyRevivals.Keys.Any(h => h.Faction == Faction)) return null;

            var toRevive = RequestPurpleRevival.ValidTargets(Game, this).Where(l => SafeLeaders.Contains(l)).OrderByDescending(l => l.Value).FirstOrDefault();

            if (toRevive == null)
            {
                var knownOpponentTraitors = Opponents.SelectMany(p => p.RevealedTraitors);
                toRevive = RequestPurpleRevival.ValidTargets(Game, this).Where(l => !knownOpponentTraitors.Contains(l)).OrderByDescending(l => l.Value).FirstOrDefault();
            }

            if (toRevive != null && Battle.ValidBattleHeroes(Game, this).Count() <= 3)
            {
                toRevive = RequestPurpleRevival.ValidTargets(Game, this).OrderByDescending(l => l.Value).FirstOrDefault();
            }

            if (toRevive != null)
            {
                return new RequestPurpleRevival(Game) { Initiator = Faction, Hero = toRevive };
            }

            return null;
        }

        protected virtual AcceptOrCancelPurpleRevival DetermineAcceptOrCancelPurpleRevival()
        {
            if (Game.CurrentPurpleRevivalRequest != null)
            {
                var hero = Game.CurrentPurpleRevivalRequest.Hero;

                int price;
                if (priceSetEarlier.ContainsKey(hero))
                {
                    price = priceSetEarlier[hero];
                }
                else
                {
                    price = 2 + D(2, hero.Value);
                    if (Game.CurrentPurpleRevivalRequest.Initiator == Ally)
                    {
                        price = 0;
                    }
                    else if (FaceDancers.Contains(hero) && !RevealedDancers.Contains(hero) || (Ally != Faction.None && AlliedPlayer.Traitors.Contains(hero)))
                    {
                        price = D(1, hero.Value);
                    }

                    priceSetEarlier.Add(hero, price);
                }

                return new AcceptOrCancelPurpleRevival(Game) { Initiator = Faction, Hero = hero, Price = price, Cancel = false };
            }

            return null;
        }

        #endregion Purple
    }

    public class VoicePlan
    {
        public BattleInitiated battle;
        public Voice voice = null;
        public TreacheryCard weaponToUse = null;
        public TreacheryCard defenseToUse = null;
        public bool playerHeroWillCertainlySurvive = false;
        public bool opponentHeroWillCertainlyBeZero = false;
    }
}