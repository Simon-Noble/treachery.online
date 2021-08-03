﻿/*
 * Copyright 2020-2021 Ronald Ossendrijver. All rights reserved.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Svg;
using Svg.Pathing;
using Svg.Transforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Treachery.Shared;
using System.Threading.Tasks;

namespace Treachery.Test
{

    [TestClass]
    public class Tests
    {
        private static List<Type> Written = new List<Type>();
        private static void WriteSavegameIfApplicable(Game g, Type t)
        {
            if (!Written.Contains(t))
            {
                var playerWithAction = g.Players.FirstOrDefault(p => g.GetApplicableEvents(p, false).Contains(t));
                if (playerWithAction != null)
                {
                    lock (Written) {

                        File.WriteAllText("" + (Written.Count + 100) + " " + t.Name + "-" + playerWithAction.Name.Replace('*', 'X') + ".special.json", GameState.GetStateAsString(g));
                        Written.Add(t);
                    }
                }
            }
        }

        private static List<string> WrittenCases = new List<string>();
        private static void WriteSavegameIfApplicable(Game g, Player playerWithAction, string c)
        {
            if (!WrittenCases.Contains(c))
            {
                lock (WrittenCases)
                {
                    var id = playerWithAction == null ? "x" : playerWithAction.Name.Replace('*', 'X');
                    File.WriteAllText(c + "-" + id + ".special.json", GameState.GetStateAsString(g));
                    WrittenCases.Add(c);
                }
            }
        }

        private static string TestSpecialCases(Game g, GameEvent e)
        {
            
            var p = g.GetPlayer(e.Initiator);
            
            WriteSavegameIfApplicable(g, typeof(Diplomacy));
            WriteSavegameIfApplicable(g, typeof(Bureaucracy));
            WriteSavegameIfApplicable(g, typeof(Planetology));

            p = g.Players.FirstOrDefault(p => p.ForcesInReserve < 0 || p.SpecialForcesInReserve < 0);
            if (p != null) return "Negative forces: " + p;

            p = g.Players.FirstOrDefault(p => p.Faction == Faction.White &&(p.SpecialForcesInReserve != 0 || p.SpecialForcesKilled != 0));
            if (p != null) return "Invalid forces: " + p;

            p = g.Players.FirstOrDefault(p => p.Resources < 0);
            if (p != null) return "Negative spice: " + p;

            if (g.Players.Any(p => p.Leaders.Count(l => g.IsInFrontOfShield(l)) > 1))
            {
                return "More than 1 leader in front of shield";
            }

            if (g.Players.Any(p => p.Leaders.Count(l => !g.CapturedLeaders.Keys.Contains(l) && g.Skilled(l)) + g.CapturedLeaders.Count(cl => cl.Value == p.Faction && g.Skilled(cl.Key)) > 1))
            {
                return "More than 1 skilled leader for 1 player (not counting leaders captured by hark)";
            }

            if (e is SkillAssigned sa && sa.Leader == null)
            {
                return "Assigning skill to null leader";
            }

            if (e is Battle b4 && b4.Hero != null && g.SkilledAs(b4.Hero, LeaderSkill.Bureaucrat))
            {
                WriteSavegameIfApplicable(g, b4.Player, "Bureaucrat in battle");
            }

            if (e is Battle b && b.Hero != null && b.Hero.Id == 1002 && b.Weapon != null && !b.Weapon.IsUseless && (g.SkilledAs(b.Hero, LeaderSkill.Swordmaster) || g.SkilledAs(b.Hero, LeaderSkill.MasterOfAssassins)) && b.Messiah)
            {
                WriteSavegameIfApplicable(g, b.Player, "10-Strength Jessica");
            }

            if (e is Battle b2 && b2.Weapon != null && g.SkilledAs(b2.Hero, LeaderSkill.Planetologist) && b2.Player.TreacheryCards.Any(c => !(c.IsWeapon || c.IsDefense || c.IsUseless)))
            {
                WriteSavegameIfApplicable(g, b2.Player, "Planetologist in battle");
            }

            if (e is Battle b3 && b3.BankerBonus > 0)
            {
                var graduate = g.Players.FirstOrDefault(p => p.Leaders.Any(l => g.SkilledAs(l, LeaderSkill.Banker)));
                WriteSavegameIfApplicable(g, graduate, "Banker");
            }

            if (g.RecentMilestones.Contains(Milestone.None))
            {
                WriteSavegameIfApplicable(g, null, "Retreat failed due to killed leader...");
            }

            p = g.Players.FirstOrDefault(p => p.TreacheryCards.Count > p.MaximumNumberOfCards);
            if (p != null && g.CurrentPhase != Phase.PerformingKarmaHandSwap) return "Too many cards: " + p;

            return "";
        }

        private static void ProfileGames()
        {
            foreach (var f in Directory.EnumerateFiles(".", "savegame*.json"))
            {
                var testcaseFilename = f + ".testcase";
                if (!File.Exists(testcaseFilename))
                {
                    Console.WriteLine("Profiling {0}...", f);

                    var fs = File.OpenText(f);
                    var state = GameState.Load(fs.ReadToEnd());
                    var game = new Game(state.Version);
                    var testcase = new Testcase();

                    foreach (var e in state.Events)
                    {
                        e.Game = game;
                        e.Execute(true, true);
                        testcase.Testvalues.Add(DetermineTestvalues(game));
                    }

                    SaveObject(testcase, testcaseFilename);
                }
            }
        }

        //[TestMethod]
        public void ImproveBots()
        {
            //Console.WriteLine("");

            //Expansion, advanced game:
            var rules = Game.RulesetDefinition[Ruleset.ExpansionAdvancedGame].ToList();
            rules.Add(Rule.FillWithBots);
            var factions = new List<Faction> { Faction.Grey, Faction.Green, Faction.Orange, Faction.Red, Faction.Blue, Faction.Yellow, Faction.Purple, Faction.Black };
            int nrOfPlayers = 8;
            int nrOfTurns = 8;
            rules.Add(Rule.BotsCannotAlly);
            var rulesAsArray = rules.ToArray();

            File.AppendAllLines("results.csv", new string[] { string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", //;{9};{10};{11};{12}
                                                        "Bidding_ResourcesToKeepWhenCardIsntPerfect",
                                                        //"Shipment_DialShortageToAccept",
                                                        //"Shipment_MaxEnemyForceStrengthFightingForSpice",
                                                        "Shipment_DialForExtraForcesToShip",
                                                        //"Bidding_PassingTreshold",
                                                        //"Battle_MimimumChanceToAssumeEnemyHeroSurvives",
                                                        "Battle_MimimumChanceToAssumeMyLeaderSurvives",
                                                        "Battle_DialShortageThresholdForThrowing",
                                                        "Wins",
                                                        "Spice",
                                                        "Points",
                                                        "ForcesOnPlanet",
                                                        "Faction") });


            foreach (Faction toTest in factions)
            {
                //int x0 = 6;
                for (int x0 = 2; x0 <= 10; x0 += 4) // 3
                {
                    //for (int x1 = 0; x1 <= 6; x1 += 3) //3
                    //int x1 = 2;
                    {
                        //for (int x2 = 0; x2 <= 4; x2 += 2) //3
                        {
                            //int x3 = 3;
                            for (int x3 = 2; x3 <= 12; x3 += 2) //6
                            {
                                //for (int x4 = 0; x4 <= 4; x4 += 2) //3
                                {
                                    //for (float x5 = 0.1f; x5 <= 1f; x5 += 0.4f) //3
                                    {
                                        for (float x6 = 0.1f; x6 <= 1f; x6 += 0.2f) //5
                                        {
                                            for (int x7 = 1; x7 <= 7; x7 += 2) //4
                                            {
                                                //8 * 600 = 4800 lines
                                                //52488 lines
                                                var p = BotParameters.GetDefaultParameters(toTest);
                                                p.Bidding_ResourcesToKeepWhenCardIsntPerfect = x0;
                                                //p.Shipment_DialShortageToAccept = x1;
                                                //p.Shipment_MaxEnemyForceStrengthFightingForSpice = x2;
                                                p.Shipment_DialForExtraForcesToShip = x3;
                                                //p.Bidding_PassingTreshold = x4;
                                                //p.Battle_MimimumChanceToAssumeEnemyHeroSurvives = x5;
                                                p.Battle_MimimumChanceToAssumeMyLeaderSurvives = x6;
                                                p.Battle_DialShortageThresholdForThrowing = x7;

                                                var pDict = new Dictionary<Faction, BotParameters>() { { toTest, p } };

                                                DetermineWins(24, rulesAsArray, factions, nrOfPlayers, nrOfTurns, pDict, toTest, out int wins, out int spice, out int points, out int forcesOnPlanet);

                                                File.AppendAllLines("results.csv", new string[] { string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", //;{9};{10};{11};{12}
                                                        x0, /*x1, x2,*/ x3, /*x4, x5,*/ x6, x7,
                                                        wins, spice, points, forcesOnPlanet, toTest) });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DetermineWins(int nrOfGames, Rule[] rules, List<Faction> factions, int nrOfPlayers, int nrOfTurns, Dictionary<Faction, BotParameters> p, Faction f,
            out int wins, out int spice, out int points, out int forcesOnPlanet)
        {
            int countWins = 0;
            int countSpice = 0;
            int countPoints = 0;
            int countForcesOnPlanet = 0;

            Parallel.For(0, nrOfGames,
                i =>
                {
                    var game = LetBotsPlay(rules, factions, nrOfPlayers, nrOfTurns, p, false, false);
                    var playerToCheck = game.Players.Single(p => p.Faction == f);
                    if (game.Winners.Contains(playerToCheck)) countWins++;
                    countSpice += playerToCheck.Resources;
                    countPoints += game.NumberOfVictoryPoints(playerToCheck, true);
                    countForcesOnPlanet += playerToCheck.ForcesOnPlanet.Sum(kvp => kvp.Value.TotalAmountOfForces);
                });

            wins = countWins;
            spice = countSpice;
            points = countPoints;
            forcesOnPlanet = countForcesOnPlanet;
        }

        [TestMethod]
        public void TestBots()
        {
            int nrOfGames = 5000;

            Console.WriteLine("Winner;Method;Turn;Events;Leaders killed;Forces killed;Owned cards;Owned Spice;Discarded");

            //Expansion, advanced game, all expansions, all factions:
            var rules = Game.RulesetDefinition[Ruleset.AllExpansionsAdvancedGame].ToList();
            rules.Add(Rule.FillWithBots);
            //rules.Add(Rule.BotsCannotAlly);
            //rules.Remove(Rule.BrownAndWhiteLeaderSkills);
            var factions = EstablishPlayers.AvailableFactions().ToList();
            int nrOfTurns = 7;
            int nrOfPlayers = factions.Count;

            //Expansion, advanced game, all expansions, free for all without guild and fremen:
            //var rules = Game.RulesetDefinition[Ruleset.AllExpansionsAdvancedGame].ToList();
            //rules.Add(Rule.FillWithBots);
            //rules.Add(Rule.BotsCannotAlly);
            //var factions = EstablishPlayers.AvailableFactions().Except(new Faction[] { Faction.Orange, Faction.Yellow }).ToList();
            //int nrOfTurns = 7; 
            //int nrOfPlayers = factions.Count;
            
            //Expansion, advanced game, 8 players:
            //var rules = Game.RulesetDefinition[Ruleset.ExpansionAdvancedGame].ToList();
            //rules.Add(Rule.FillWithBots);
            //rules.Add(Rule.ExtraKaramaCards);
            //rules.Add(Rule.AssistedNotekeeping);
            //var factions = EstablishPlayers.AvailableFactions().ToList();
            //int nrOfPlayers = factions.Count;

            //Game to find a specific situation to test
            //var rules = Game.RulesetDefinition[Ruleset.ExpansionAdvancedGame].ToList();
            //rules.Add(Rule.FillWithBots);
            //rules.Add(Rule.ExtraKaramaCards);
            //rules.Add(Rule.AssistedNotekeeping);
            //var factions = new List<Faction>() { Faction.Black, Faction.Green, Faction.Red, Faction.Brown };
            //int nrOfPlayers = factions.Count;

            //Expansion, advanced game, 6 players:
            //var rules = Game.RulesetDefinition[Ruleset.ExpansionAdvancedGame].ToList();
            //rules.Add(Rule.FillWithBots);
            //rules.Add(Rule.BotsCannotAlly);
            //var factions = new List<Faction>() { Faction.Black, Faction.Green, Faction.Yellow, Faction.Red, Faction.Grey, Faction.Blue };
            //int nrOfPlayers = factions.Count;

            //Expansion, advanced game, 8 players:
            //var rules = Game.RulesetDefinition[Ruleset.ExpansionAdvancedGame].ToList();
            //rules.Add(Rule.FillWithBots);
            //var factions = new List<Faction>() { Faction.Black, Faction.Green, Faction.Grey, Faction.Red, Faction.Purple, Faction.Blue };
            //int nrOfPlayers = factions.Count;

            //Classic, basic game:
            //var rules = Game.RulesetDefinition[Ruleset.ServerClassic].ToList();
            //rules.Add(Rule.FillWithBots);
            //var factions = new List<Faction>() { Faction.Black, Faction.Green, Faction.Yellow, Faction.Red, Faction.Orange, Faction.Blue };
            //int nrOfPlayers = factions.Count;

            //Server Classic, advanced game:
            //var rules = Game.RulesetDefinition[Ruleset.ServerClassic].ToList();
            //rules.Add(Rule.FillWithBots);
            //var factions = new List<Faction>() { Faction.Black, Faction.Green, Faction.Yellow, Faction.Red, Faction.Orange, Faction.Blue };
            //int nrOfPlayers = factions.Count;

            //All rules enables:
            //var rules = Enumerations.GetValuesExceptDefault(typeof(Rule), Rule.None).ToList();
            //var factions = Enumerations.GetValuesExceptDefault(typeof(Faction), Faction.None).ToList();
            //rules.Remove(Rule.CustomInitialForcesAndResources);
            //rules.Remove(rules.Add(Rule.BotsCannotAlly));
            //int nrOfPlayers = factions.Count;

            /*rules.Add(Rule.RedBot);
            rules.Add(Rule.OrangeBot);
            rules.Add(Rule.BlackBot);
            rules.Add(Rule.PurpleBot);
            rules.Add(Rule.BlueBot);
            rules.Add(Rule.GreenBot);
            rules.Add(Rule.YellowBot);
            rules.Add(Rule.GreyBot);*/

            //Can bots ally?
            //rules.Add(Rule.BotsCannotAlly);

            var rulesAsArray = rules.ToArray();
            var wincounter = new ObjectCounter<Faction>();
            
            Parallel.For(0, nrOfGames,
                   index => {
                       PlayGameAndRecordResults(factions, nrOfPlayers, nrOfTurns, rulesAsArray, wincounter);
                   });
            
            foreach (var f in wincounter.Counted)
            {
                Console.WriteLine("{0}: {1} ({2}%)", f, wincounter.CountOf(f), (100f * wincounter.CountOf(f) / nrOfGames));
            }
            
        }

        private static void PlayGameAndRecordResults(List<Faction> factions, int nrOfPlayers, int nrOfTurns, Rule[] rulesAsArray, ObjectCounter<Faction> wincounter)
        {
            var game = LetBotsPlay(rulesAsArray, factions, nrOfPlayers, nrOfTurns, null, false, true);

            Console.WriteLine("{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                string.Join(",", game.Winners),
                Skin.Current.Describe(game.WinMethod),
                game.CurrentTurn,
                game.History.Count,
                string.Join(",", game.LeaderState.Where(l => !l.Value.Alive).Select(l => l.Key)),
                string.Join(",", game.Players.Sum(p => p.ForcesKilled + p.SpecialForcesKilled)),
                string.Join(",", game.Players.SelectMany(p => p.TreacheryCards)),
                string.Join(",", game.Players.Sum(p => p.Resources)),
                string.Join(",", game.TreacheryDiscardPile.Items));

            foreach (var winner in game.Winners)
            {
                wincounter.Count(winner.Faction);
            }
        }

        private static Game LetBotsPlay(Rule[] rules, List<Faction> factions, int nrOfPlayers, int nrOfTurns, Dictionary<Faction, BotParameters> p, bool infoLogging, bool performTests)
        {
            var game = new Game
            {
                BotInfologging = infoLogging,
            };

            var start = new EstablishPlayers(game) { ApplicableRules = rules.ToArray(), FactionsInPlay = factions, MaximumTurns = nrOfTurns, MaximumNumberOfPlayers = nrOfPlayers, Players = Array.Empty<string>(), Seed = new Random().Next() };
            start.Execute(false, true);

            if (p != null)
            {
                foreach (var kvp in p)
                {
                    game.Players.Single(p => p.Faction == kvp.Key).Param = kvp.Value;
                }
            }

            while (game.CurrentPhase != Phase.GameEnded)
            {
                var evt = PerformBotEvent(game, performTests);

                if (performTests)
                {
                    if (evt == null)
                    {
                        File.WriteAllText("novalidbotevent.json", GameState.GetStateAsString(game));
                    }
                    Assert.IsNotNull(evt, "bots couldn't come up with a valid event");

                    var strangeCase = TestSpecialCases(game, evt);
                    if (strangeCase != "")
                    {
                        File.WriteAllText("strangecase.json", GameState.GetStateAsString(game));
                    }

                    Assert.AreEqual("", strangeCase);

                    if (game.History.Count == 5000)
                    {
                        File.WriteAllText("stuck.json", GameState.GetStateAsString(game));
                    }

                    Assert.AreNotEqual(5000, game.History.Count, "bots got stuck at 5000 events");
                }
                else if (game.History.Count == 5000)
                {
                    File.WriteAllText("stuck" + game.Seed + ".json", GameState.GetStateAsString(game));
                    break;
                }
                
            }

            return game;
        }



        private static GameEvent PerformBotEvent(Game game, bool performTests)
        {
            var bots = Deck<Player>.Randomize(game.Players.Where(p => p.IsBot));

            foreach (var bot in bots)
            {
                var evt = bot.DetermineHighPrioInPhaseAction(game.GetApplicableEvents(bot, true));

                if (evt != null)
                {
                    var executeResult = evt.Execute(performTests, true);
                    if (performTests && executeResult != "")
                    {
                        var cnt = game.History.Count;
                        if (cnt >= 3)
                        {
                            Console.WriteLine(game.History[cnt - 3].GetMessage());
                            Console.WriteLine(game.History[cnt - 2].GetMessage());
                            Console.WriteLine(game.History[cnt - 1].GetMessage());
                        }
                        Console.WriteLine(evt.GetMessage());

                        File.WriteAllText("invalid.json", GameState.GetStateAsString(game));
                    }
                    if (performTests) Assert.AreEqual("", executeResult);
                    return evt;
                }
            }

            foreach (var bot in bots)
            {
                var evt = bot.DetermineLowPrioInPhaseAction(game.GetApplicableEvents(bot, true));

                if (evt != null)
                {
                    var executeResult = evt.Execute(performTests, true);
                    if (performTests && executeResult != "")
                    {
                        var cnt = game.History.Count;
                        if (cnt >= 3)
                        {
                            Console.WriteLine(game.History[cnt - 3].GetMessage());
                            Console.WriteLine(game.History[cnt - 2].GetMessage());
                            Console.WriteLine(game.History[cnt - 1].GetMessage());
                        }
                        Console.WriteLine(evt.GetMessage());

                        File.WriteAllText("invalid.json", GameState.GetStateAsString(game));
                    }
                    if (performTests) Assert.AreEqual("", executeResult);
                    return evt;
                }
            }

            foreach (var p in game.Players.Where(p => p.IsBot))
            {
                var evt = p.DetermineEndPhaseAction(game.GetApplicableEvents(p, true));

                if (evt != null)
                {
                    var result = evt.Execute(performTests, true);
                    if (performTests)
                    {
                        if (result != "") File.WriteAllText("error.json", GameState.GetStateAsString(game));
                        Assert.AreEqual("", result);
                    }
                    return evt;
                }
            }

            return null;
        }

        [TestMethod]
        public void Regression()
        {
            ProfileGames();

            try
            {
                Console.WriteLine("Re-playing all savegame files in {0}...", Directory.GetCurrentDirectory());
                int gamesTested = 0;
                Parallel.ForEach(Directory.EnumerateFiles(".", "savegame*.json"), f =>
                {
                    gamesTested++;
                    var fs = File.OpenText(f);
                    var state = GameState.Load(fs.ReadToEnd());
                    Console.WriteLine("Checking {0} (version {1})...", f, state.Version);
                    var game = new Game(state.Version);

                    fs = File.OpenText(f + ".testcase");
                    var tc = LoadObject<Testcase>(fs.ReadToEnd());

                    int valueId = 0;
                    foreach (var e in state.Events)
                    {
                        e.Game = game;
                        var previousPhase = game.CurrentPhase;

                        var result = e.Execute(true, true);
                        if (result != "")
                        {
                            File.WriteAllText("invalid.json", GameState.GetStateAsString(game));
                        }
                        Assert.AreEqual("", result, f + ", " + e.GetType().Name + " (" + valueId + ", " + e.GetMessage() + ")");

                        var actualValues = DetermineTestvalues(game);
                        tc.Testvalues[valueId].Equals(actualValues);
                        if (!tc.Testvalues[valueId].Equals(actualValues))
                        {
                            File.WriteAllText("invalid.json", GameState.GetStateAsString(game));
                        }
                        Assert.AreEqual(tc.Testvalues[valueId], actualValues, f + ", " + previousPhase + " -> " + game.CurrentPhase + ", " + e.GetType().Name + " (" + valueId + ", " + e.GetMessage() + "): " + Testvalues.Difference);

                        var strangeCase = TestSpecialCases(game, e);
                        if (strangeCase != "")
                        {
                            File.WriteAllText("strangecase.json", GameState.GetStateAsString(game));
                        }
                        Assert.AreEqual("", strangeCase);

                        valueId++;
                    }
                });

                Assert.AreNotEqual(0, gamesTested);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private static Testvalues DetermineTestvalues(Game game)
        {
            var result = new Testvalues
            {
                currentPhase = game.CurrentPhase,
                forcesinArrakeen = game.ForcesOnPlanet[game.Map.Arrakeen].Sum(b => b.TotalAmountOfForces),
                forcesinCarthag = game.ForcesOnPlanet[game.Map.Carthag].Sum(b => b.TotalAmountOfForces),
                forcesinTabr = game.ForcesOnPlanet[game.Map.SietchTabr].Sum(b => b.TotalAmountOfForces),
                forcesinHabbanya = game.ForcesOnPlanet[game.Map.HabbanyaSietch].Sum(b => b.TotalAmountOfForces),
                forcesinTuek = game.ForcesOnPlanet[game.Map.TueksSietch].Sum(b => b.TotalAmountOfForces),
                nrofplayers = game.Players.Count,
                playervalues = new TestvaluesPerPlayer[game.Players.Count]
            };

            for (int i = 0; i < game.Players.Count; i++)
            {
                var p = game.Players[i];

                result.playervalues[i] = new TestvaluesPerPlayer()
                {
                    faction = p.Faction,
                    ally = p.Ally,
                    position = p.PositionAtTable,
                    resources = p.Resources,
                    bribes = p.Bribes,
                    forcesinreserve = p.ForcesInReserve,
                    specialforcesinreserve = p.SpecialForcesInReserve,
                    totaldeathcount = p.Leaders.Sum(l => game.LeaderState[l].DeathCounter),
                    cardcount = p.TreacheryCards.Count,
                    cardtypes = p.TreacheryCards.Sum(c => (int)c.Type),
                    traitors = p.Traitors.Sum(t => t.Id),
                    facedancers = p.FaceDancers.Sum(t => t.Id),
                    totalforcesonplanet = p.ForcesOnPlanet.Values.Sum(b => b.AmountOfForces),
                    totalspecialforcesonplanet = p.ForcesOnPlanet.Values.Sum(b => b.AmountOfSpecialForces),
                    forceskilled = p.ForcesKilled,
                    specialforceskilled = p.SpecialForcesKilled,
                    nroftechtokens = p.TechTokens.Count
                };
            }

            return result;
        }

        private static T LoadObject<T>(string data)
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.TypeNameHandling = TypeNameHandling.All;
            var textReader = new StringReader(data);
            var jsonReader = new JsonTextReader(textReader);
            return serializer.Deserialize<T>(jsonReader);
        }


        [TestMethod]
        public void SaveAndLoadSkin()
        {
            var leader = LeaderManager.LeaderLookup.Find(1008);
            var oldName = Skin.Current.GetPersonName(leader);
            var serializer = JsonSerializer.CreateDefault();
            serializer.Formatting = Formatting.Indented;
            var writer = new StringWriter();
            serializer.Serialize(writer, Skin.Dune1979);
            writer.Close();
            var skinData = writer.ToString();
            File.WriteAllText("skin.json", skinData);

            var textReader = new StringReader(File.ReadAllText("skin.json"));
            var jsonReader = new JsonTextReader(textReader);
            var skinToTest = serializer.Deserialize<Skin>(jsonReader);
            Assert.AreEqual(oldName, skinToTest.GetPersonName(leader));
        }

        //[TestMethod]
        public static void SaveBuiltInSkins()
        {
            SaveObject(Skin.Dune1979, "e:\\Skin.Dune.treachery.online.json");
        }

        public static void SVG()
        {
            var accumulatedTransforms = new List<SvgTransformCollection>();

            var doc = SvgDocument.Open("e:\\map.svg");
            foreach (var group1 in doc.Children)
            {
                if (group1.ID == "Dune")
                {
                    if (group1.Transforms != null)
                    {
                        accumulatedTransforms.Add(group1.Transforms);
                    }

                    foreach (var child in group1.Children)
                    {
                        if (child.ID == "Areas")
                        {
                            if (child.Transforms != null)
                            {
                                accumulatedTransforms.Add(child.Transforms);
                            }

                            foreach (var areaOrSubgroup in child.Children)
                            {
                                Console.WriteLine(areaOrSubgroup.GetType());

                                if (areaOrSubgroup is SvgGroup)
                                {
                                    if (areaOrSubgroup.Transforms != null)
                                    {
                                        accumulatedTransforms.Add(areaOrSubgroup.Transforms);
                                    }

                                    foreach (var area in areaOrSubgroup.Children)
                                    {
                                        if (area is SvgPath)
                                        {
                                            ProcessPath(accumulatedTransforms, area);
                                        }
                                    }
                                }
                                else if (areaOrSubgroup is SvgPath)
                                {
                                    ProcessPath(accumulatedTransforms, areaOrSubgroup);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ProcessPath(List<SvgTransformCollection> accumulatedTransforms, SvgElement area)
        {
            var path = area as SvgPath;
            //Console.WriteLine(area.ID);

            var shape = new List<Segment>();

            foreach (var elt in path.PathData)
            {
                if (elt is SvgClosePathSegment)
                {
                    shape.Add(new Close());
                }
                else if (elt is SvgCubicCurveSegment)
                {
                    var x = elt as SvgCubicCurveSegment;
                    shape.Add(new BezierTo(Transform(x.Start, accumulatedTransforms), Transform(x.End, accumulatedTransforms), Transform(x.FirstControlPoint, accumulatedTransforms), Transform(x.SecondControlPoint, accumulatedTransforms)));
                }
                else if (elt is SvgLineSegment)
                {
                    var x = elt as SvgLineSegment;
                    shape.Add(new LineTo(Transform(x.Start, accumulatedTransforms), Transform(x.End, accumulatedTransforms)));
                }
                else if (elt is SvgMoveToSegment)
                {
                    var x = elt as SvgMoveToSegment;
                    shape.Add(new MoveTo(Transform(x.Start, accumulatedTransforms), Transform(x.End, accumulatedTransforms)));
                }
            }

            Console.WriteLine("private static readonly Segment[] {1} = {{ {0} }};", string.Join(", ", shape), area.ID);

            var xml = area.GetXML();
            int end = xml.IndexOf("z\"") + 1;
            var newPath = string.Join(' ', shape.Select(s => s.ToSvgString()));
            //Console.WriteLine( "{0}{1}{2}", xml.Substring(0, 9), newPath, xml.Substring(end, xml.Length - end));
        }

        public static void TranslatePoints()
        {
            Game g = new Game();
            foreach (var l in g.Map.Locations)
            {
                Console.WriteLine("{0};{1};{2};{3};{4};{5};{6}",
                    l.Territory.Id,
                    l.Sector,
                    l.Name,
                    (int)(l.Center.X / 7.32),
                    (int)(l.Center.Y / 7.32),
                    l.SpiceLocation != null ? (int)(l.SpiceLocation.X / 7.32) : -1,
                    l.SpiceLocation != null ? (int)(l.SpiceLocation.Y / 7.32) : -1);
            }
        }

        private static PointF Transform(PointF p, IEnumerable<SvgTransformCollection> transforms)
        {
            var thePoints = new PointF[] { p };

            foreach (var tc in transforms)
            {
                foreach (var t in tc)
                {
                    t.Matrix.TransformPoints(thePoints);
                }
            }

            return new PointF((int)(7.362344583f * thePoints[0].X), (int)(7.349840256f * thePoints[0].Y));
        }

        private static void SaveObject(object toSave, string filename)
        {
            var serializer = JsonSerializer.CreateDefault();
            serializer.Formatting = Formatting.Indented;
            var writer = new StringWriter();
            serializer.Serialize(writer, toSave);
            writer.Close();
            var skinData = writer.ToString();
            File.WriteAllText(filename, skinData);
        }

        private static int GetRandomId()
        {
            return (new Random()).Next();
        }

        [TestMethod]
        public void TestShuffleMethod()
        {
            int[] counters = new int[100];

            for (int run = 0; run < 100000; run++)
            {
                var deck = new Deck<int>(Enumerable.Range(0, 100), new Random());
                deck.Shuffle();
                counters[deck.Draw()]++;
            }

            Console.WriteLine("Average times any item is on top: {0}", counters.Average());
            Console.WriteLine("Standard deviation in the times any item is on top: {0}", CalculateStandardDeviation(counters.Select(c => (double)c)));

            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine("Times {0} was on top: {1}", i, counters[i]);
            }
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            double standardDeviation = 0;

            if (values.Any())
            {
                // Compute the average.     
                double avg = values.Average();

                // Perform the Sum of (value-avg)_2_2.      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));

                // Put it all together.      
                standardDeviation = Math.Sqrt((sum) / (values.Count() - 1));
            }

            return standardDeviation;
        }

        [TestMethod]
        public void DetermineBias()
        {
            var joined = new List<string>()
            {
                "Ronald",
                "Rene",
                "Ren�",
                "X1",
                "X2",
                ""
            };

            var factions = new List<Faction> { Faction.Green, Faction.Black, Faction.Yellow, Faction.Red, Faction.Orange, Faction.Blue };

            var assignedFactions = new List<Tuple<string, Faction>>();
            var assignedPositions = new List<Tuple<Faction, int>>();
            for (int i = 0; i < 10000; i++)
            {
                int _playerID = GetRandomId();
                var game = new Game(_playerID);

                game.HandleEvent(new EstablishPlayers() { Players = joined, FactionsInPlay = factions, MaximumTurns = 10, ApplicableRules = Game.RulesetDefinition[Ruleset.AdvancedGame], Seed = _playerID });

                foreach (var p in game.Players)
                {
                    assignedFactions.Add(new Tuple<string, Faction>(p.Name, p.Faction));
                    assignedPositions.Add(new Tuple<Faction, int>(p.Faction, p.PositionAtTable));
                }
            }

            Console.WriteLine("{0}: {1} {2} {3} {4} {5} {6}", "Player".PadLeft(20), "Atr".PadLeft(5), "Hrk".PadLeft(5), "Fre".PadLeft(5), "Emp".PadLeft(5), "Gld".PadLeft(5), "Bgt".PadLeft(5));
            foreach (var playerName in joined)
            {
                var a = assignedFactions.Count(t => t.Item1 == playerName && t.Item2 == Faction.Green).ToString();
                var h = assignedFactions.Count(t => t.Item1 == playerName && t.Item2 == Faction.Black).ToString();
                var f = assignedFactions.Count(t => t.Item1 == playerName && t.Item2 == Faction.Yellow).ToString();
                var e = assignedFactions.Count(t => t.Item1 == playerName && t.Item2 == Faction.Red).ToString();
                var g = assignedFactions.Count(t => t.Item1 == playerName && t.Item2 == Faction.Orange).ToString();
                var b = assignedFactions.Count(t => t.Item1 == playerName && t.Item2 == Faction.Blue).ToString();
                Console.WriteLine("{0}: {1} {2} {3} {4} {5} {6}", playerName.PadLeft(20), a.PadLeft(5), h.PadLeft(5), f.PadLeft(5), e.PadLeft(5), g.PadLeft(5), b.PadLeft(5));
            }

            Console.WriteLine("{0}: {1} {2} {3} {4} {5} {6}", "Faction".PadLeft(20), "0".PadLeft(5), "1".PadLeft(5), "2".PadLeft(5), "3".PadLeft(5), "4".PadLeft(5), "5".PadLeft(5));
            foreach (var faction in factions)
            {
                var p0 = assignedPositions.Count(t => t.Item1 == faction && t.Item2 == 0).ToString();
                var p1 = assignedPositions.Count(t => t.Item1 == faction && t.Item2 == 1).ToString();
                var p2 = assignedPositions.Count(t => t.Item1 == faction && t.Item2 == 2).ToString();
                var p3 = assignedPositions.Count(t => t.Item1 == faction && t.Item2 == 3).ToString();
                var p4 = assignedPositions.Count(t => t.Item1 == faction && t.Item2 == 4).ToString();
                var p5 = assignedPositions.Count(t => t.Item1 == faction && t.Item2 == 5).ToString();
                Console.WriteLine("{0}: {1} {2} {3} {4} {5} {6}", faction.ToString().PadLeft(20), p0.PadLeft(5), p1.PadLeft(5), p2.PadLeft(5), p3.PadLeft(5), p4.PadLeft(5), p5.PadLeft(5));
            }
        }
    }
}
