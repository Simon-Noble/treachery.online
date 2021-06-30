﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Collections.Generic;
using Treachery.Shared;


namespace Treachery.Client
{
    public static class Support
    {
        public static bool DURATION_LOGGING_ENABLED = false;

        private static DateTime LatestLoggedDateTime = DateTime.Now;
        public static void LogDuration(string method)
        {
            if (DURATION_LOGGING_ENABLED)
            {
                var cur = DateTime.Now;
                Console.WriteLine("{0}:{1}", method, cur.Subtract(LatestLoggedDateTime).Milliseconds);
                LatestLoggedDateTime = cur;
            }
        }

        public static string HTMLEncode(string toEncode)
        {
            return HttpUtility.HtmlEncode(toEncode);
        }

        public static string GetCardImage(TreacheryCardType type)
        {
            return Skin.Current.GetImageURL(
                TreacheryCardManager.GetCardsInAndOutsidePlay().FirstOrDefault(c => c.Type == type));
        }

        public static string GetCardTitle(TreacheryCardType t)
        {
            return string.Format("Use {0}?", Skin.Current.Describe(t));
        }

        public static string GetTreacheryCardHoverHTML(TreacheryCard c)
        {
            if (c == null)
            {
                return "";
            }
            else
            {
                return string.Format("<div><img src='{0}' width=300 class='img-fluid' title='{2}'/></div><div class='bg-dark text-white text-center' style='width:300px'>{1}</div>", Skin.Current.GetImageURL(c), Skin.Current.GetTreacheryCardDescription(c), c.Name);
            }
        }

        public static string GetTreacheryCardHoverHTMLSmall(TreacheryCard c)
        {
            if (c == null)
            {
                return "";
            }
            else
            {
                return string.Format("<div><img src='{0}' width=200 class='img-fluid' title='{2}'/></div><div class='bg-dark text-white text-center small' style='width:200px'>{1}</div>", Skin.Current.GetImageURL(c), Skin.Current.GetTreacheryCardDescription(c), c.Name);
            }
        }

        public static string GetResourceCardHoverHTML(ResourceCard c)
        {
            if (c == null)
            {
                return "";
            }
            else
            {
                return string.Format("<img src='{0}' width=300 class='img-fluid' title='{1}'/>", Skin.Current.GetImageURL(c), c.ToString());
            }
        }

        public static string GetResourceCardHoverHTMLSmall(ResourceCard c)
        {
            if (c == null)
            {
                return "";
            }
            else
            {
                return string.Format("<img src='{0}' width=200 class='img-fluid' title='{1}'/>", Skin.Current.GetImageURL(c), c.ToString());
            }
        }

        public static string GetHeroHoverHTML(IHero h)
        {
            if (h == null)
            {
                return "";
            }
            else
            {
                return string.Format("<img src='{0}' width=200 class='img-fluid' title='{1}'/>", Skin.Current.GetImageURL(h), h.Name);
            }
        }

        public static string GetLeaderHTML(Leader l)
        {
            if (l == null)
            {
                return "";
            }
            else
            {
                return string.Format("<img class='img-fluid' src='{0}' width=80 title='{1}'/>", Skin.Current.GetImageURL(l), l.Name);
            }
        }

        public static string GetTechTokenHTML(TechToken tt)
        {
            if (tt == TechToken.None)
            {
                return "";
            }
            else
            {
                return string.Format("<div><img class='img-fluid' src='{0}' title='{2}' width=200/><div class='bg-dark text-white text-center small' style='width:200px'>{1}</div></div>", Skin.Current.GetImageURL(tt), Skin.Current.GetTechTokenDescription(tt), Skin.Current.Describe(tt));
            }
        }

        public static string Color(Faction f)
        {
            return string.Format("background-color:{0}", Skin.Current.GetFactionColor(f));
        }

        public static string GetHeroHoverHTMLSmall(IHero h)
        {
            if (h == null)
            {
                return "";
            }
            else
            {
                return string.Format("<img src='{0}' width=150 class='img-fluid'/>", Skin.Current.GetImageURL(h));
            }
        }

        public static void Log(object o)
        {
            Console.WriteLine(o);
        }

        public static void Log(string msg, params object[] o)
        {
            Console.WriteLine(msg, o);
        }

        public static string GetHash(string input)
        {
            if (input == null || input.Length == 0)
            {
                return "";
            }

            using SHA256 sha256Hash = SHA256.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        public static bool VerifyHash(string input, string hash)
        {
            // Hash the input.
            var hashOfInput = GetHash(input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hash) == 0;
        }

        public static Skin LoadSkin(string skinData)
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.Formatting = Formatting.Indented;
            var textReader = new StringReader(skinData);
            var jsonReader = new JsonTextReader(textReader);
            return serializer.Deserialize<Skin>(jsonReader);
        }

        
    }
}