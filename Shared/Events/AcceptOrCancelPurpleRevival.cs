﻿/*
 * Copyright 2020-2023 Ronald Ossendrijver. All rights reserved.
 */

using Newtonsoft.Json;
using System.Linq;

namespace Treachery.Shared
{
    public class AcceptOrCancelPurpleRevival : GameEvent
    {
        public bool Cancel;

        public int Price;

        public int _heroId;

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

        [JsonIgnore]
        private bool IsDenial => Price == int.MaxValue;

        public AcceptOrCancelPurpleRevival(Game g) : base(g) { }

        public AcceptOrCancelPurpleRevival()
        {
        }

        public override Message Validate()
        {
            return null;
        }

        protected override void ExecuteConcreteEvent()
        {
            Log();

            if (Hero == null)
            {
                foreach (var r in Game.CurrentRevivalRequests)
                {
                    Game.EarlyRevivalsOffers.Add(r.Hero, int.MaxValue);
                }

                Game.CurrentRevivalRequests.Clear();
            }
            else
            {
                Game.EarlyRevivalsOffers.Remove(Hero);

                if (!Cancel)
                {
                    Game.EarlyRevivalsOffers.Add(Hero, Price);
                }

                var requestToRemove = Game.CurrentRevivalRequests.FirstOrDefault(r => r.Hero == Hero);
                if (requestToRemove != null)
                {
                    Game.CurrentRevivalRequests.Remove(requestToRemove);
                }
            }
        }

        public override Message GetMessage()
        {
            if (Hero == null)
            {
                return Message.Express(Initiator, " deny all outstanding requests for early revival");
            }
            else
            {
                if (!Cancel)
                {
                    if (IsDenial)
                    {
                        return Message.Express(Initiator, " deny ", Hero.Faction, " early revival of a leader");
                    }
                    else
                    {
                        return Message.Express(Initiator, " offer ", Hero.Faction, " revival of a leader for ", Payment.Of(Price));
                    }
                }
                else
                {
                    return Message.Express(Initiator, " cancel their revival offer to ", Hero.Faction);
                }
            }
        }
    }
}
