﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

namespace Treachery.Shared
{
    public class EndPhase : GameEvent
    {
        public EndPhase(Game game) : base(game)
        {
        }

        public EndPhase()
        {
        }

        public override string Validate()
        {
            return "";
        }

        protected override void ExecuteConcreteEvent()
        {
            Game.HandleEvent(this);
        }

        public override Message GetMessage()
        {
            return new Message(Initiator, "Phase ended by {0}.", Initiator);
        }
    }
}
