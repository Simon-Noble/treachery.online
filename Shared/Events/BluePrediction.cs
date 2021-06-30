﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public class BluePrediction : GameEvent
    {
        public BluePrediction(Game game) : base(game)
        {
        }

        public BluePrediction()
        {
        }

        public Faction ToWin { get; set; }
        public int Turn { get; set; }

        public override string Validate()
        {
            if (!Game.IsPlaying(ToWin)) return "Invalid target";
            if (Turn < 1 || Turn > Game.MaximumNumberOfTurns) return "Invalid turn";

            return "";
        }

        protected override void ExecuteConcreteEvent()
        {
            Game.HandleEvent(this);
        }

        public override Message GetMessage()
        {
            return new Message(Initiator, "{0} predict who will win and when.", Initiator);
        }

        public static IEnumerable<Faction> ValidTargets(Game g, Player p)
        {
            if (g.Version < 71 || g.Players.Count() <= 1)
            {
                return g.Players.Select(p => p.Faction);
            }
            else
            {
                return g.ValidTargets(p);
            }
        }

        public static IEnumerable<int> ValidTurns(Game g)
        {
            return Enumerable.Range(1, g.MaximumNumberOfTurns);
        }
    }
}