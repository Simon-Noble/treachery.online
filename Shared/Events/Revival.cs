﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public class Revival : GameEvent
    {
        public int _heroId;

        public Revival(Game game) : base(game)
        {
        }

        public Revival()
        {
        }

        public int AmountOfForces { get; set; } = 0;

        public int AmountOfSpecialForces { get; set; } = 0;


        [JsonIgnore]
        public IHero Hero
        {
            get
            {
                return LeaderManager.HeroLookup.Find(_heroId);
            }
            set
            {
                _heroId = LeaderManager.HeroLookup.GetId(value);
            }
        }

        public override string Validate()
        {
            var p = Player;

            if (AmountOfForces < 0 || AmountOfSpecialForces < 0) return "You can't revive a negative amount of forces.";
            if (Game.Version >= 60 && AmountOfForces <= 0 && AmountOfSpecialForces <= 0 && Hero == null) return "Select forces or a leader to revive.";

            if (AmountOfForces > p.ForcesKilled) return "You can't revive that much.";
            if (AmountOfSpecialForces > p.SpecialForcesKilled) return "You can't revive that much.";

            int emperorRevivals = (p.Ally == Faction.Red) ? Game.RedWillPayForExtraRevival : 0;

            int limit = Game.GetRevivalLimit(p);

            if (AmountOfForces + AmountOfSpecialForces > limit + emperorRevivals) return "You can't revive that much.";
            if (Game.Version >= 32 && Initiator != Faction.Grey && AmountOfSpecialForces > 1) return Skin.Current.Format("You can't revive more than one {0} per turn.", p.SpecialForce);
            if (Game.Version >= 32 && AmountOfSpecialForces > 0 && Initiator != Faction.Grey && Game.FactionsThatRevivedSpecialForcesThisTurn.Contains(Initiator)) return Skin.Current.Format("You already revived one {0} this turn.", p.SpecialForce);

            var costOfRevival = DetermineCost(Game, p, Hero, AmountOfForces, AmountOfSpecialForces);
            if (costOfRevival.TotalCostForPlayer > p.Resources) return "You can't pay that much.";

            return "";
        }

        public static RevivalCost DetermineCost(Game g, Player initiator, IHero hero, int amountOfForces, int amountOfSpecialForces)
        {
            return new RevivalCost(g, initiator, hero, amountOfForces, amountOfSpecialForces);
        }

        protected override void ExecuteConcreteEvent()
        {
            Game.HandleEvent(this);
        }

        public override Message GetMessage()
        {
            return new Message(Initiator, "{0} perform revival.", Initiator);
        }

        public static IEnumerable<IHero> ValidRevivalHeroes(Game g, Player p)
        {
            var result = new List<IHero>();

            if (p.Faction != Faction.Purple)
            {
                result.AddRange(NormallyRevivableHeroes(g, p));
            }
            else if (p.Leaders.Count(l => g.LeaderState[l].Alive) < 5)
            {
                result.AddRange(UnrestrictedRevivableHeroes(g, p));
            }

            if (result.Count == 0)
            {
                return g.ValidFreeRevivalHeroes(p).Where(h => g.AllowedEarlyRevivals.ContainsKey(h));
            }

            if (p.Is(Faction.Purple) && g.Applicable(Rule.GreyAndPurpleExpansionPurpleGholas) && p.Leaders.Count(l => g.LeaderState[l].Alive) < 5)
            {
                result.AddRange(
                    g.LeaderState.Where(leaderAndState =>
                    leaderAndState.Key.Faction != Faction.Purple &&
                    leaderAndState.Key != LeaderManager.Messiah &&
                    !leaderAndState.Value.Alive).Select(kvp => kvp.Key));
            }

            return result;
        }

        public static IEnumerable<IHero> NormallyRevivableHeroes(Game g, Player p)
        {
            var result = new List<IHero>();

            if (p.Leaders.Count > 0)
            {
                int lowestDeathCount = p.Leaders.Min(l => g.LeaderState[l].DeathCounter);
                result.AddRange(p.Leaders.Where(l => g.LeaderState[l].DeathCounter == lowestDeathCount && !g.LeaderState[l].Alive));

                if (p.Is(Faction.Green) && !g.IsAlive(LeaderManager.Messiah))
                {
                    if (g.Version < 43 || g.LeaderState[LeaderManager.Messiah].DeathCounter == lowestDeathCount && !g.LeaderState[LeaderManager.Messiah].Alive)
                    {
                        result.Add(LeaderManager.Messiah);
                    }
                }
            }

            return result;
        }

        public static IEnumerable<IHero> UnrestrictedRevivableHeroes(Game g, Player p)
        {
            var result = new List<IHero>();

            result.AddRange(p.Leaders.Where(l => !g.LeaderState[l].Alive));

            if (p.Is(Faction.Green) && !g.IsAlive(LeaderManager.Messiah))
            {
                result.Add(LeaderManager.Messiah);
            }

            return result;
        }

        public static IEnumerable<int> ValidRevivalAmounts(Game g, Player p, bool specialForces)
        {
            int killedForces = specialForces ? p.SpecialForcesKilled : p.ForcesKilled;
            var amountPaidByEmperor = p.Ally == Faction.Red ? g.RedWillPayForExtraRevival : 0;

            int maxRevivals;
            if (!specialForces)
            {
                maxRevivals = Math.Min(g.GetRevivalLimit(p) + amountPaidByEmperor, killedForces);
            }
            else
            {
                maxRevivals = Math.Min(p.Is(Faction.Grey) ? g.GetRevivalLimit(p) + amountPaidByEmperor : (g.FactionsThatRevivedSpecialForcesThisTurn.Contains(p.Faction)? 0 : 1), killedForces);
            }

            return Enumerable.Range(0, maxRevivals + 1);
        }

        public static int ValidMaxRevivals(Game g, Player p, bool specialForces)
        {
            var amountPaidByEmperor = p.Ally == Faction.Red ? g.RedWillPayForExtraRevival : 0;

            if (!specialForces)
            {
                return Math.Min(g.GetRevivalLimit(p) + amountPaidByEmperor, p.ForcesKilled);
            }
            else
            {
                return Math.Min(p.Is(Faction.Grey) ? g.GetRevivalLimit(p) + amountPaidByEmperor : (g.FactionsThatRevivedSpecialForcesThisTurn.Contains(p.Faction) ? 0 : 1), p.SpecialForcesKilled);
            }
        }


        public static bool MayReviveWithDiscount(Game g, Player p)
        {
            return (p.Is(Faction.Purple) && !g.Prevented(FactionAdvantage.PurpleRevivalDiscount)) ||
                   (p.Ally == Faction.Purple && g.PurpleAllyMayReviveAsPurple && !g.Prevented(FactionAdvantage.PurpleRevivalDiscountAlly));
        }


        public static int GetPriceOfHeroRevival(Game g, Player initiator, IHero hero)
        {
            bool purpleDiscountPrevented = g.Prevented(FactionAdvantage.PurpleRevivalDiscount);

            if (hero == null) return 0;

            var price = hero.CostToRevive;

            if (g.Version < 102)
            {
                if (initiator.Is(Faction.Purple) && !purpleDiscountPrevented)
                {
                    price = (int)Math.Ceiling(0.5 * price);
                }
                
                if (!NormallyRevivableHeroes(g, initiator).Contains(hero) && g.AllowedEarlyRevivals.ContainsKey(hero))
                {
                    price = g.AllowedEarlyRevivals[hero];
                }
            }
            else
            {
                if (initiator.Is(Faction.Purple) && !purpleDiscountPrevented)
                {
                    price = (int)Math.Ceiling(0.5 * price);
                }
                else if (!NormallyRevivableHeroes(g, initiator).Contains(hero) && g.AllowedEarlyRevivals.ContainsKey(hero))
                {
                    price = g.AllowedEarlyRevivals[hero];
                }
            }

            return price;
        }

        public static int GetPriceOfForceRevival(Game g, Player initiator, int amountOfForces, int amountOfSpecialForces)
        {
            int nrOfFreeRevivals = g.FreeRevivals(initiator);
            int nrOfPaidSpecialForces = Math.Max(0, amountOfSpecialForces - nrOfFreeRevivals);
            int nrOfFreeRevivalsLeft = nrOfFreeRevivals - (amountOfSpecialForces - nrOfPaidSpecialForces);
            int nrOfPaidNormalForces = Math.Max(0, amountOfForces - nrOfFreeRevivalsLeft);
            int priceOfSpecialForces = initiator.Is(Faction.Grey) ? 3 : 2;
            int priceOfNormalForces = 2;

            var cost = nrOfPaidSpecialForces * priceOfSpecialForces + nrOfPaidNormalForces * priceOfNormalForces;

            if (MayReviveWithDiscount(g, initiator))
            {
                cost = (int)Math.Ceiling(0.5 * cost);
            }

            return cost;
        }
    }

    public class RevivalCost
    {
        public int TotalCostForPlayer;
        public int CostForForceRevivalForPlayer;
        public int CostForEmperor;
        public int CostToReviveHero;
        public bool CanBePaid;

        public RevivalCost(Game g, Player initiator, IHero hero, int amountOfForces, int amountOfSpecialForces)
        {
            int costForForceRevival = Revival.GetPriceOfForceRevival(g, initiator, amountOfForces, amountOfSpecialForces);
            var amountPaidForByEmperor = initiator.Ally == Faction.Red ? g.RedWillPayForExtraRevival : 0;
            var emperor = g.GetPlayer(Faction.Red);
            var emperorsSpice = emperor != null ? emperor.Resources : 0;

            if (g.Version <= 80)
            {
                CostForEmperor = Math.Min(costForForceRevival, Math.Min(amountPaidForByEmperor * 2, emperorsSpice));
            }
            else
            {
                CostForEmperor = DetermineCostForEmperor(initiator.Faction, costForForceRevival, amountOfForces, amountOfSpecialForces, emperorsSpice, amountPaidForByEmperor);
            }

            CostForForceRevivalForPlayer = costForForceRevival - CostForEmperor;
            CostToReviveHero = Revival.GetPriceOfHeroRevival(g, initiator, hero);
            TotalCostForPlayer = CostForForceRevivalForPlayer + CostToReviveHero;
            CanBePaid = initiator.Resources >= TotalCostForPlayer;
        }


        public int Total
        {
            get
            {
                return TotalCostForPlayer + CostForEmperor;
            }
        }

        public int TotalCostForForceRevival
        {
            get
            {
                return CostForForceRevivalForPlayer + CostForEmperor;
            }
        }

        public static int DetermineCostForEmperor(Faction initiator, int totalCostForForceRevival, int amountOfForces, int amountOfSpecialForces, int emperorsSpice, int amountPaidForByEmperor)
        {
            int priceOfSpecialForces = initiator == Faction.Grey ? 3 : 2;

            int specialForcesPaidByEmperor = 0;
            while (
                (specialForcesPaidByEmperor + 1) <= amountOfSpecialForces &&
                (specialForcesPaidByEmperor + 1) * priceOfSpecialForces <= emperorsSpice &&
                specialForcesPaidByEmperor + 1 <= amountPaidForByEmperor)
            {
                specialForcesPaidByEmperor++;
            }

            int forcesPaidByEmperor = 0;
            while (
                (forcesPaidByEmperor + 1) <= amountOfForces &&
                specialForcesPaidByEmperor * priceOfSpecialForces + (forcesPaidByEmperor + 1) * 2 <= emperorsSpice &&
                specialForcesPaidByEmperor + forcesPaidByEmperor + 1 <= amountPaidForByEmperor)
            {
                forcesPaidByEmperor++;
            }

            int costForEmperor = specialForcesPaidByEmperor * priceOfSpecialForces + forcesPaidByEmperor * 2;
            return Math.Min(totalCostForForceRevival, Math.Min(costForEmperor, emperorsSpice));
        }
    }
}
