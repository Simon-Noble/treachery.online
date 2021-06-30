﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public class YellowRidesMonster : PlacementEvent
    {
        public YellowRidesMonster(Game game) : base(game)
        {
        }

        public YellowRidesMonster()
        {
        }

        public override string Validate()
        {
            if (Passed) return "";

            if (To == null) return "Target location not selected.";
            if (!ValidTargets(Game, Player).Contains(To)) return "Invalid target location.";

            var forceAmount = ForceLocations.Values.Sum(b => b.AmountOfForces);
            var specialForceAmount = ForceLocations.Values.Sum(b => b.AmountOfSpecialForces);

            if (forceAmount == 0 && specialForceAmount == 0) return "No forces selected.";

            bool tooManyForces = ForceLocations.Any(bl => bl.Value.AmountOfForces > Player.ForcesIn(bl.Key));
            if (tooManyForces) return "Invalid amount of forces.";

            bool tooManySpecialForces = ForceLocations.Any(bl => bl.Value.AmountOfSpecialForces > Player.SpecialForcesIn(bl.Key));
            if (tooManySpecialForces) return "Invalid amount of special forces.";

            return "";
        }

        protected override void ExecuteConcreteEvent()
        {
            Game.HandleEvent(this);
        }

        public override Message GetMessage()
        {
            if (Passed)
            {
                return new Message(Initiator, "{0} pass a ride on {1}.", Initiator, Concept.Monster);
            }
            else
            {
                var from = ForceLocations.Keys.First().Territory;
                return new Message(Initiator, "{0} ride {1} from {2} to {3}.", Initiator, Concept.Monster, from, To);
            }
        }

        public static IEnumerable<Territory> ValidSources(Game g)
        {
            var fremen = g.GetPlayer(Faction.Yellow);
            if (fremen != null)
            {
                var firstMonsterLocationWithFremenForces = g.Monsters.FirstOrDefault(t => fremen.AnyForcesIn(t) > 0);

                if (firstMonsterLocationWithFremenForces != null)
                {
                    return new Territory[] { firstMonsterLocationWithFremenForces };
                }
            }

            return new Territory[] { };
        }

        public static IEnumerable<Location> ValidTargets(Game g, Player p)
        {
            bool mayMoveIntoStorm = p.Faction == Faction.Yellow && g.Applicable(Rule.YellowMayMoveIntoStorm) && g.Applicable(Rule.YellowStormLosses);

            IEnumerable<Location> locationsInRange = locationsInRange = g.Map.Locations.Where(l =>
                (mayMoveIntoStorm || l.Sector != g.SectorInStorm) &&
                (l != g.Map.HiddenMobileStronghold || p.Is(Faction.Grey)) &&
                (!l.Territory.IsStronghold || g.NrOfOccupantsExcludingPlayer(l, p) < 2));

            return RemoveLocationsBlockedByAlly(g, p, locationsInRange);
        }

        private static IEnumerable<Location> RemoveLocationsBlockedByAlly(Game g, Player p, IEnumerable<Location> locations)
        {
            var ally = g.GetPlayer(p.Ally);
            if (ally == null)
            {
                return locations;
            }
            else
            {
                return locations.Where(l => l == g.Map.PolarSink || !ally.Occupies(l));
            }
        }
    }
}