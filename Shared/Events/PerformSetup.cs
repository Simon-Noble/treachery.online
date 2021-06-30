﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using System.Linq;

namespace Treachery.Shared
{
    public class PerformSetup : PlacementEvent
    {
        public PerformSetup(Game game) : base(game)
        {
        }

        public PerformSetup()
        {
        }

        public int Resources { get; set; }

        public override string Validate()
        {
            var faction = Game.NextFactionToPerformCustomSetup;
            var p = Game.GetPlayer(faction);
            int numberOfSpecialForces = ForceLocations.Values.Sum(b => b.AmountOfSpecialForces);
            if (numberOfSpecialForces > p.SpecialForcesInReserve) return Skin.Current.Format("Too many {0}", p.SpecialForce);

            int numberOfForces = ForceLocations.Values.Sum(b => b.AmountOfForces);
            if (numberOfForces > p.ForcesInReserve) return "Too many forces";

            return "";
        }

        protected override void ExecuteConcreteEvent()
        {
            Game.HandleEvent(this);
        }

        public override Message GetMessage()
        {
            return new Message(Initiator, "Initial positions and {0} ({1}) determined.", Concept.Resource, Resources);
        }
    }
}