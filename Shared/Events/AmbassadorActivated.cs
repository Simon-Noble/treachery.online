﻿/*
 * Copyright 2020-2022 Ronald Ossendrijver. All rights reserved.
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public class AmbassadorActivated : GameEvent, ILocationEvent
    {
        public AmbassadorActivated(Game game) : base(game)
        {
        }

        public AmbassadorActivated()
        {
        }

        public bool Passed { get; set; }

        public Faction BlueSelectedFaction { get; set; }

        public string _brownCardIds;

        [JsonIgnore]
        public IEnumerable<TreacheryCard> BrownCards
        {
            get
            {
                return IdStringToObjects(_brownCardIds, TreacheryCardManager.Lookup);
            }
            set
            {
                _brownCardIds = ObjectsToIdString(value, TreacheryCardManager.Lookup);
            }
        }

        public bool PinkOfferAlliance { get; set; }

        public bool PinkGiveVidalToAlly { get; set; }

        public bool PinkTakeVidal { get; set; }

        public int _yellowOrOrangeToId;

        [JsonIgnore]
        public Location YellowOrOrangeTo { get { return Game.Map.LocationLookup.Find(_yellowOrOrangeToId); } set { _yellowOrOrangeToId = Game.Map.LocationLookup.GetId(value); } }

        [JsonIgnore]
        public Location To => YellowOrOrangeTo;

        public string _yellowForceLocations = "";

        [JsonIgnore]
        public Dictionary<Location, Battalion> YellowForceLocations
        {
            get
            {
                return PlacementEvent.ParseForceLocations(Game, Player.Faction, _yellowForceLocations);
            }
            set
            {
                _yellowForceLocations = PlacementEvent.ForceLocationsString(Game, value);
            }
        }

        public int _greyCardId;

        [JsonIgnore]
        public TreacheryCard GreyCard
        {
            get
            {
                return TreacheryCardManager.Get(_greyCardId);
            }
            set
            {
                _greyCardId = TreacheryCardManager.GetId(value);
            }
        }

        public int OrangeForceAmount { get; set; }

        public int PurpleAmountOfForces { get; set; }

        public int _purpleHeroId = -1;

        [JsonIgnore]
        public IHero PurpleHero
        {
            get
            {
                return LeaderManager.HeroLookup.Find(_purpleHeroId);
            }
            set
            {
                _purpleHeroId = LeaderManager.HeroLookup.GetId(value);
            }
        }

        public bool PurpleAssignSkill { get; set; }

        public override Message Validate()
        {
            if (!Passed)
            {
                var player = Player;
                var ambassador = GetFaction(Game);
                var victim = GetVictim(Game);
                var victimPlayer = Game.GetPlayer(victim);

                if (Initiator != Faction.Pink) return Message.Express("Your faction can't activate Ambassadors");

                if (ambassador == Faction.Blue)
                {
                    if (!GetValidBlueFactions(Game).Contains(BlueSelectedFaction)) return Message.Express("Invalid Ambassador selected");
                    ambassador = BlueSelectedFaction;
                }

                switch (ambassador)
                {
                    case Faction.Brown:
                        if (BrownCards.Any(c => !GetValidBrownCards(player).Contains(c))) return Message.Express("Invalid card selected");
                        break;

                    case Faction.Pink:
                        if (PinkOfferAlliance && !AllianceCanBeOffered(Game, player)) return Message.Express("You can't offer an alliance");
                        if (PinkTakeVidal && !VidalCanBeTaken(Game)) return Message.Express("You can't take ", Game.Vidal);
                        if (PinkGiveVidalToAlly && !VidalCanBeGivenTo(Game, victimPlayer)) return Message.Express("You can't give ", Game.Vidal, " to ", victim);
                        break;

                    case Faction.Yellow:
                        if (YellowForceLocations.Any(kvp => Game.IsInStorm(kvp.Key))) return Message.Express("Can't move from storm");
                        if (YellowForceLocations.Any(kvp => player.ForcesIn(kvp.Key) < kvp.Value.AmountOfForces)) return Message.Express("Invalid amount of ", Player.Force);
                        if (YellowForceLocations.Any(kvp => player.SpecialForcesIn(kvp.Key) < kvp.Value.AmountOfSpecialForces)) return Message.Express("Invalid amount of ", Player.SpecialForce);
                        if (!ValidYellowTargets(Game, player, YellowForceLocations).Contains(YellowOrOrangeTo)) return Message.Express("Invalid target location");
                        break;

                    case Faction.Grey:
                        if (!GetValidGreyCards(player).Any()) return Message.Express("You don't have a card to discard");
                        if (GreyCard != null && !GetValidGreyCards(player).Contains(GreyCard)) return Message.Express("Invalid card selected");
                        break;

                    case Faction.White:
                        if (player.Resources < 3) return Message.Express("You don't enough ", Concept.Resource, " to buy a card");
                        if (!player.HasRoomForCards) return Message.Express("You don't have room for an additional card");
                        break;

                    case Faction.Orange:
                        if (!ValidOrangeTargets(Game, player).Contains(YellowOrOrangeTo)) return Message.Express("Invalid target location");
                        if (OrangeForceAmount > ValidOrangeMaxForces(player)) return Message.Express("Invalid amount of forces");
                        break;

                    case Faction.Purple:
                        if (PurpleAmountOfForces < 0) return Message.Express("You can't revive a negative amount of forces");
                        if (PurpleAmountOfForces > ValidOrangeMaxForces(player)) return Message.Express("You can't revive that many");
                        if (PurpleAmountOfForces > 0 && PurpleHero != null) return Message.Express("You can't revive both forces and a leader");
                        if (PurpleHero != null && !ValidPurpleHeroes(Game, player).Contains(PurpleHero)) return Message.Express("Invalid leader");
                        if (PurpleAssignSkill && PurpleHero == null) return Message.Express("Select a leader to assign a skill to");
                        if (PurpleAssignSkill && !Revival.MayAssignSkill(Game, Player, PurpleHero)) return Message.Express("You can't assign a skill to this leader");
                        break;
                }
            }

            return null;
        }

        protected override void ExecuteConcreteEvent()
        {
            Game.HandleEvent(this);
        }

        public override Message GetMessage()
        {
            return Message.Express(Initiator, Passed ? " don't" : "", " activate an Ambassador");
        }

        public static Territory GetTerritory(Game g) => g.LastAmbassadorTrigger.To.Territory;

        public static Faction GetVictim(Game g) => g.LastAmbassadorTrigger.Initiator;

        public static Player GetVictimPlayer(Game g) => g.GetPlayer(GetVictim(g));

        public static Faction GetFaction(Game g) => g.AmbassadorIn(GetTerritory(g));

        public static bool AllianceCanBeOffered(Game g, Player p) => !p.HasAlly && !g.GetPlayer(GetVictim(g)).HasAlly;

        public static bool VidalCanBeTaken(Game g) => g.VidalIsAlive && !g.VidalIsCapturedOrGhola;

        public static bool VidalCanBeGivenTo(Game g, Player p) => true; // g.HasRoomForLeaders(p);

        public static bool VidalCanBeGivenTo(Game g, Faction f) => true; // g.HasRoomForLeaders(g.GetPlayer(f));

        public static IEnumerable<Faction> GetValidBlueFactions(Game g) => g.UnassignedAmbassadors.Items;

        public static IEnumerable<TreacheryCard> GetValidBrownCards(Player p) => p.TreacheryCards;

        public static IEnumerable<TreacheryCard> GetValidGreyCards(Player p) => p.TreacheryCards;

        public static IEnumerable<Territory> ValidYellowSources(Game g, Player p) => PlacementEvent.TerritoriesWithAnyForcesNotInStorm(g, p);

        public static IEnumerable<Location> ValidYellowTargets(Game g, Player p, Dictionary<Location, Battalion> forces)
        {
            if (forces.Sum(kvp => kvp.Value.TotalAmountOfForces) > 0)
            {
                return PlacementEvent.ValidTargets(g, p, forces);
            }
            else
            {
                return Array.Empty<Location>();
            }
        }

        public static IEnumerable<Location> ValidOrangeTargets(Game g, Player p) => Shipment.ValidShipmentLocations(g, p).Where(l => 
            !p.HasAlly ||
            p.AlliedPlayer.AnyForcesIn(l.Territory) == 0 ||
            p.Ally == Faction.Blue && g.Applicable(Rule.AdvisorsDontConflictWithAlly) && p.AlliedPlayer.ForcesIn(l.Territory) == 0 );

        public static int ValidOrangeMaxForces(Player p) => Math.Min(p.ForcesInReserve, 4);

        public static int ValidPurpleMaxAmount(Player p) => Math.Min(p.ForcesKilled, 4);

        public static IEnumerable<IHero> ValidPurpleHeroes(Game game, Player player) => game.KilledHeroes(player);
    }

}