﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Treachery.Shared
{
    public static class TreacheryCardManager
    {
        //public static readonly TreacheryCard NONE = new TreacheryCard(TreacheryCard.NONE, -1, TreacheryCardType.None, Rule.None);

        private static readonly List<TreacheryCard> Items = new List<TreacheryCard>();
        public static IFetcher<TreacheryCard> Lookup = new TreacheryCardFetcher();

        static TreacheryCardManager()
        {
            Initialize();
        }

        public static void Initialize()
        {
            //Basic cards
            Items.Add(new TreacheryCard(0, 0, TreacheryCardType.Laser, Rule.None));
            Items.Add(new TreacheryCard(1, 1, TreacheryCardType.Projectile, Rule.None));
            Items.Add(new TreacheryCard(2, 2, TreacheryCardType.Projectile, Rule.None));
            Items.Add(new TreacheryCard(3, 3, TreacheryCardType.Projectile, Rule.None));
            Items.Add(new TreacheryCard(4, 4, TreacheryCardType.Projectile, Rule.None));
            Items.Add(new TreacheryCard(5, 5, TreacheryCardType.Poison, Rule.None));
            Items.Add(new TreacheryCard(6, 6, TreacheryCardType.Poison, Rule.None));
            Items.Add(new TreacheryCard(7, 7, TreacheryCardType.Poison, Rule.None));
            Items.Add(new TreacheryCard(8, 8, TreacheryCardType.Poison, Rule.None));
            Items.Add(new TreacheryCard(9, 9, TreacheryCardType.Shield, Rule.None));
            Items.Add(new TreacheryCard(10, 9, TreacheryCardType.Shield, Rule.None));
            Items.Add(new TreacheryCard(11, 9, TreacheryCardType.Shield, Rule.None));
            Items.Add(new TreacheryCard(12, 9, TreacheryCardType.Shield, Rule.None));
            Items.Add(new TreacheryCard(13, 13, TreacheryCardType.Antidote, Rule.None));
            Items.Add(new TreacheryCard(14, 13, TreacheryCardType.Antidote, Rule.None));
            Items.Add(new TreacheryCard(15, 13, TreacheryCardType.Antidote, Rule.None));
            Items.Add(new TreacheryCard(16, 13, TreacheryCardType.Antidote, Rule.None));
            Items.Add(new TreacheryCard(17, 17, TreacheryCardType.Mercenary, Rule.None));
            Items.Add(new TreacheryCard(18, 17, TreacheryCardType.Mercenary, Rule.None));
            Items.Add(new TreacheryCard(19, 19, TreacheryCardType.Mercenary, Rule.None));
            Items.Add(new TreacheryCard(20, 20, TreacheryCardType.RaiseDead, Rule.None));
            Items.Add(new TreacheryCard(21, 21, TreacheryCardType.Metheor, Rule.None));
            Items.Add(new TreacheryCard(22, 22, TreacheryCardType.Caravan, Rule.None));
            Items.Add(new TreacheryCard(23, 23, TreacheryCardType.Karma, Rule.None));
            Items.Add(new TreacheryCard(24, 23, TreacheryCardType.Karma, Rule.None));
            Items.Add(new TreacheryCard(25, 25, TreacheryCardType.Clairvoyance, Rule.None));
            Items.Add(new TreacheryCard(26, 25, TreacheryCardType.Clairvoyance, Rule.None));
            Items.Add(new TreacheryCard(27, 27, TreacheryCardType.StormSpell, Rule.None));
            Items.Add(new TreacheryCard(28, 28, TreacheryCardType.Useless, Rule.None));
            Items.Add(new TreacheryCard(29, 29, TreacheryCardType.Useless, Rule.None));
            Items.Add(new TreacheryCard(30, 30, TreacheryCardType.Useless, Rule.None));
            Items.Add(new TreacheryCard(31, 31, TreacheryCardType.Useless, Rule.None));
            Items.Add(new TreacheryCard(32, 32, TreacheryCardType.Useless, Rule.None));

            //Grey & Purple Expansion Treachery Cards
            Items.Add(new TreacheryCard(33, 33, TreacheryCardType.ProjectileAndPoison, Rule.GreyAndPurpleExpansionTreacheryCardsPBandSS));
            Items.Add(new TreacheryCard(34, 34, TreacheryCardType.Projectile, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(35, 35, TreacheryCardType.Poison, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(36, 36, TreacheryCardType.WeirdingWay, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(37, 37, TreacheryCardType.PoisonTooth, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(38, 38, TreacheryCardType.ShieldAndAntidote, Rule.GreyAndPurpleExpansionTreacheryCardsPBandSS));
            Items.Add(new TreacheryCard(39, 9, TreacheryCardType.Shield, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(40, 13, TreacheryCardType.Antidote, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(41, 39, TreacheryCardType.Chemistry, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(42, 40, TreacheryCardType.ArtilleryStrike, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(43, 41, TreacheryCardType.Harvester, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(44, 42, TreacheryCardType.Thumper, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(45, 43, TreacheryCardType.Amal, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
            Items.Add(new TreacheryCard(46, 44, TreacheryCardType.Useless, Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));

            //3 extra karma cards
            Items.Add(new TreacheryCard(100, 23, TreacheryCardType.Karma, Rule.ExtraKaramaCards));
            Items.Add(new TreacheryCard(101, 23, TreacheryCardType.Karma, Rule.ExtraKaramaCards));
            Items.Add(new TreacheryCard(102, 23, TreacheryCardType.Karma, Rule.ExtraKaramaCards));
        }

        public static Deck<TreacheryCard> CreateAndShuffleTreacheryDeck(Game g, Random random)
        {
            var result = new Deck<TreacheryCard>(GetCardsInPlay(g), random);
            result.Shuffle();
            return result;
        }

        public static IEnumerable<TreacheryCard> GetCardsInPlay(Game g)
        {
            return Items.Where(c => 
            c.Rule == Rule.None || 
            g.Applicable(c.Rule) ||

            //Amal used to be included in the basic set of expansion cards
            g.Version < 88 && c.Type == TreacheryCardType.Amal && g.Applicable(Rule.GreyAndPurpleExpansionTreacheryCardsExceptPBandSSandAmal));
        }

        public static IEnumerable<TreacheryCard> GetCardsInAndOutsidePlay()
        {
            return Items;
        }

        public static TreacheryCard Get(int id)
        {
            return Items.SingleOrDefault(i => i.Id == id);
        }

        public static int GetId(TreacheryCard value)
        {
            if (value != null)
            {
                return value.Id;
            }
            else
            {
                return -1;
            }
        }

        public class TreacheryCardFetcher : IFetcher<TreacheryCard>
        {
            public TreacheryCard Find(int id)
            {
                if (id < -1)
                {
                    return null;
                }
                else
                {
                    return Items.SingleOrDefault(t => t.Id == id);
                }
            }

            public int GetId(TreacheryCard obj)
            {
                if (obj == null)
                {
                    return -1;
                }
                else
                {
                    return obj.Id;
                }
            }
        }
    }
}