﻿/*
 * Copyright 2020-2023 Ronald Ossendrijver. All rights reserved.
 */

namespace Treachery.Shared
{
    public class Audited : GameEvent
    {
        #region Construction

        public Audited(Game game) : base(game)
        {
        }

        public Audited()
        {
        }

        #endregion Construction

        #region Validation

        public override Message Validate()
        {
            return null;
        }

        #endregion Validation

        #region Execution

        protected override void ExecuteConcreteEvent()
        {
            Log();
            Game.Stone(Milestone.Audited);

            foreach (var card in Game.AuditedCards)
            {
                Game.RegisterKnown(Player, card);
            }

            Game.Enter(Game.BattleWinner == Faction.None, Game.FinishBattle, Game.BlackMustDecideToCapture, Phase.CaptureDecision, Phase.BattleConclusion);
        }

        public override Message GetMessage()
        {
            return Message.Express(Faction.Brown, " finish their audit");
        }

        #endregion Execution
    }
}
