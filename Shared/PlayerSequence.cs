﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public class PlayerSequence
    {
        public IList<Player> Players { get; set; }

        public int RoundStartedAt { get; set; }

        public int Current { get; set; }

        private int _direction;

        public PlayerSequence(IEnumerable<Player> players, int direction = 1)
        {
            Players = players.ToList();
            _direction = direction;
        }

        public void Start(Game game, bool ignorePlayersThatCantBid)
        {
            var startLooking = (int)Math.Ceiling((float)game.SectorInStorm * game.MaximumNumberOfPlayers / Map.NUMBER_OF_SECTORS) % game.MaximumNumberOfPlayers;
            Current = FindNearestPlayerPosition(game, startLooking, ignorePlayersThatCantBid);
            RoundStartedAt = Current;
        }

        public Player CurrentPlayer => Players.Where(p => p.PositionAtTable == Current).Single();

        public Player RoundStartedAtPlayer => Players.Where(p => p.PositionAtTable == RoundStartedAt).Single();

        public Faction CurrentFaction => CurrentPlayer.Faction;

        public void NextRound(Game game, bool ignorePlayersThatCantBid)
        {
            Current = FindNearestPlayerPosition(game, RoundStartedAt + _direction, ignorePlayersThatCantBid);
            RoundStartedAt = Current;
        }

        public void NextPlayer(Game game, bool ignorePlayersThatCantBid)
        {
            Current = FindNearestPlayerPosition(game, Current + _direction, ignorePlayersThatCantBid);
        }

        private int FindNearestPlayerPosition(Game game, int positionToStartLooking, bool ignorePlayersThatCantBid)
        {
            int position = positionToStartLooking % game.MaximumNumberOfPlayers;
            for (int i = 0; i < game.MaximumNumberOfPlayers; i++)
            {
                if (Players.Any(p => p.PositionAtTable == position && (!ignorePlayersThatCantBid || p.MayBidOnCards)))
                {
                    return position;
                }
                else
                {
                    position = (position + _direction) % game.MaximumNumberOfPlayers;
                }
            }

            return -1;
        }

        public override string ToString()
        {
            return string.Format(string.Join("->", Players.OrderBy(p => p.PositionAtTable).Select(p => string.Format("{0} ({1})", p.Name, p.PositionAtTable))) + ", Current: {0}", Current);
        }

        public IEnumerable<SequenceElement> GetPlayersInSequence(Game g)
        {
            var result = new List<SequenceElement>();
            for (int i = 0; i < g.MaximumNumberOfPlayers; i++)
            {
                int pos = (RoundStartedAt + i) % g.MaximumNumberOfPlayers;
                var playerAtPosition = Players.FirstOrDefault(p => p.PositionAtTable == (RoundStartedAt + _direction * i) % g.MaximumNumberOfPlayers);
                if (playerAtPosition != null)
                {
                    var elt = new SequenceElement() { Player = playerAtPosition, HasTurn = pos == Current };
                    result.Add(elt);
                }
            }
            return result;
        }
    }

    public class SequenceElement
    {
        public Player Player;
        public bool HasTurn;
    }
}
