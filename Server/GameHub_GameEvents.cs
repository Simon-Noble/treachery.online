﻿using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Treachery.Client;
using Treachery.Shared;
using Treachery.Shared.Model;

namespace Treachery.Server;

public partial class GameHub
{
    public async Task<VoidResult> RequestChangeSettings(string userToken, string gameId, ChangeSettings e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestEstablishPlayers(string userToken, string gameId, EstablishPlayers e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestEndPhase(string userToken, string gameId, EndPhase e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDonated(string userToken, string gameId, Donated e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestResourcesTransferred(string userToken, string gameId, ResourcesTransferred e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestFactionSelected(string userToken, string gameId, FactionSelected e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestFactionTradeOffered(string userToken, string gameId, FactionTradeOffered e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPerformSetup(string userToken, string gameId, PerformSetup e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestCardsDetermined(string userToken, string gameId, CardsDetermined e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPerformYellowSetup(string userToken, string gameId, PerformYellowSetup e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBluePrediction(string userToken, string gameId, BluePrediction e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestCharityClaimed(string userToken, string gameId, CharityClaimed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPerformBluePlacement(string userToken, string gameId, PerformBluePlacement e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestTraitorsSelected(string userToken, string gameId, TraitorsSelected e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestStormSpellPlayed(string userToken, string gameId, StormSpellPlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestTestingStationUsed(string userToken, string gameId, TestingStationUsed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestTakeLosses(string userToken, string gameId, TakeLosses e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestMetheorPlayed(string userToken, string gameId, MetheorPlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestYellowSentMonster(string userToken, string gameId, YellowSentMonster e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestYellowRidesMonster(string userToken, string gameId, YellowRidesMonster e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAllianceOffered(string userToken, string gameId, AllianceOffered e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAllianceBroken(string userToken, string gameId, AllianceBroken e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBid(string userToken, string gameId, Bid e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestRevival(string userToken, string gameId, Revival e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBlueBattleAnnouncement(string userToken, string gameId, BlueBattleAnnouncement e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestShipment(string userToken, string gameId, Shipment e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestOrangeDelay(string userToken, string gameId, OrangeDelay e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBlueAccompanies(string userToken, string gameId, BlueAccompanies e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBlueFlip(string userToken, string gameId, BlueFlip e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestMove(string userToken, string gameId, Move e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestCaravan(string userToken, string gameId, Caravan e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBattleInitiated(string userToken, string gameId, BattleInitiated e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBattle(string userToken, string gameId, Battle e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBattleRevision(string userToken, string gameId, BattleRevision e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestTreacheryCalled(string userToken, string gameId, TreacheryCalled e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBattleConcluded(string userToken, string gameId, BattleConcluded e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestClairvoyancePlayed(string userToken, string gameId, ClairVoyancePlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestClairvoyanceAnswered(string userToken, string gameId, ClairVoyanceAnswered e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestRaiseDeadPlayed(string userToken, string gameId, RaiseDeadPlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarma(string userToken, string gameId, Karma e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaFreeRevival(string userToken, string gameId, KarmaFreeRevival e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaShipmentPrevention(string userToken, string gameId, KarmaShipmentPrevention e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaRevivalPrevention(string userToken, string gameId, KarmaRevivalPrevention e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaHandSwapInitiated(string userToken, string gameId, KarmaHandSwapInitiated e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaHandSwap(string userToken, string gameId, KarmaHandSwap e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaMonster(string userToken, string gameId, KarmaMonster e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaWhiteBuy(string userToken, string gameId, KarmaWhiteBuy e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAllyPermission(string userToken, string gameId, AllyPermission e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestMulliganPerformed(string userToken, string gameId, MulliganPerformed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestFaceDancerRevealed(string userToken, string gameId, FaceDancerRevealed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestFaceDanced(string userToken, string gameId, FaceDanced e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestFaceDancerReplaced(string userToken, string gameId, FaceDancerReplaced e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestSetIncreasedRevivalLimits(string userToken, string gameId, SetIncreasedRevivalLimits e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestSetShipmentPermission(string userToken, string gameId, SetShipmentPermission e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestRequestPurpleRevival(string userToken, string gameId, RequestPurpleRevival e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAcceptOrCancelPurpleRevival(string userToken, string gameId, AcceptOrCancelPurpleRevival e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPerformHmsPlacement(string userToken, string gameId, PerformHmsPlacement e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPerformHmsMovement(string userToken, string gameId, PerformHmsMovement e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaHmsMovement(string userToken, string gameId, KarmaHmsMovement e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAmalPlayed(string userToken, string gameId, AmalPlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestGreyRemovedCardFromAuction(string userToken, string gameId, GreyRemovedCardFromAuction e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestGreySelectedStartingCard(string userToken, string gameId, GreySelectedStartingCard e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestGreySwappedCardOnBid(string userToken, string gameId, GreySwappedCardOnBid e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestHarvesterPlayed(string userToken, string gameId, HarvesterPlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPoisonToothCancelled(string userToken, string gameId, PoisonToothCancelled e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestReplacedCardWon(string userToken, string gameId, ReplacedCardWon e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestThumperPlayed(string userToken, string gameId, ThumperPlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestVoice(string userToken, string gameId, Voice e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPrescience(string userToken, string gameId, Prescience e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaPrescience(string userToken, string gameId, KarmaPrescience e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestRedBidSupport(string userToken, string gameId, RedBidSupport e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDealOffered(string userToken, string gameId, DealOffered e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDealAccepted(string userToken, string gameId, DealAccepted e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDiscoveryEntered(string userToken, string gameId, DiscoveryEntered e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestStormDialled(string userToken, string gameId, StormDialled e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestHideSecrets(string userToken, string gameId, HideSecrets e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPlayerReplaced(string userToken, string gameId, PlayerReplaced e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBrownDiscarded(string userToken, string gameId, BrownDiscarded e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestRedDiscarded(string userToken, string gameId, RedDiscarded e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBrownEconomics(string userToken, string gameId, BrownEconomics e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestCardTraded(string userToken, string gameId, CardTraded e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaBrownDiscard(string userToken, string gameId, KarmaBrownDiscard e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAuditCancelled(string userToken, string gameId, AuditCancelled e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAudited(string userToken, string gameId, Audited e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBrownMovePrevention(string userToken, string gameId, BrownMovePrevention e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBrownKarmaPrevention(string userToken, string gameId, BrownKarmaPrevention e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBrownExtraMove(string userToken, string gameId, BrownExtraMove e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBrownFreeRevivalPrevention(string userToken, string gameId, BrownFreeRevivalPrevention e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBrownRemoveForce(string userToken, string gameId, BrownRemoveForce e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestWhiteAnnouncesBlackMarket(string userToken, string gameId, WhiteAnnouncesBlackMarket e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBlackMarketBid(string userToken, string gameId, BlackMarketBid e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestWhiteAnnouncesAuction(string userToken, string gameId, WhiteAnnouncesAuction e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestWhiteSpecifiesAuction(string userToken, string gameId, WhiteSpecifiesAuction e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestWhiteKeepsUnsoldCard(string userToken, string gameId, WhiteKeepsUnsoldCard e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestWhiteRevealedNoField(string userToken, string gameId, WhiteRevealedNoField e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestWhiteGaveCard(string userToken, string gameId, WhiteGaveCard e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestCardGiven(string userToken, string gameId, CardGiven e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestRockWasMelted(string userToken, string gameId, RockWasMelted e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestResidualPlayed(string userToken, string gameId, ResidualPlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestFlightUsed(string userToken, string gameId, FlightUsed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestFlightDiscoveryUsed(string userToken, string gameId, FlightDiscoveryUsed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDistransUsed(string userToken, string gameId, DistransUsed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDiscardedTaken(string userToken, string gameId, DiscardedTaken e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDiscardedSearchedAnnounced(string userToken, string gameId, DiscardedSearchedAnnounced e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDiscardedSearched(string userToken, string gameId, DiscardedSearched e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestJuicePlayed(string userToken, string gameId, JuicePlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPortableAntidoteUsed(string userToken, string gameId, PortableAntidoteUsed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBureaucracy(string userToken, string gameId, Bureaucracy e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDiplomacy(string userToken, string gameId, Diplomacy e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestSkillAssigned(string userToken, string gameId, SkillAssigned e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestSwitchedSkilledLeader(string userToken, string gameId, SwitchedSkilledLeader e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestThought(string userToken, string gameId, Thought e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestThoughtAnswered(string userToken, string gameId, ThoughtAnswered e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestHmsAdvantageChosen(string userToken, string gameId, HMSAdvantageChosen e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestRetreat(string userToken, string gameId, Retreat e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPlanetology(string userToken, string gameId, Planetology e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestCaptured(string userToken, string gameId, Captured e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestNexusCardDrawn(string userToken, string gameId, NexusCardDrawn e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestTerrorPlanted(string userToken, string gameId, TerrorPlanted e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestTerrorRevealed(string userToken, string gameId, TerrorRevealed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDiscoveryRevealed(string userToken, string gameId, DiscoveryRevealed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAmbassadorPlaced(string userToken, string gameId, AmbassadorPlaced e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAmbassadorActivated(string userToken, string gameId, AmbassadorActivated e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestExtortionPrevented(string userToken, string gameId, ExtortionPrevented e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDiscarded(string userToken, string gameId, Discarded e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAllianceByTerror(string userToken, string gameId, AllianceByTerror e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestNexusVoted(string userToken, string gameId, NexusVoted e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestAllianceByAmbassador(string userToken, string gameId, AllianceByAmbassador e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestLoserConcluded(string userToken, string gameId, LoserConcluded e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestPerformCyanSetup(string userToken, string gameId, PerformCyanSetup e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDivideResources(string userToken, string gameId, DivideResources e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestDivideResourcesAccepted(string userToken, string gameId, DivideResourcesAccepted e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestBattleClaimed(string userToken, string gameId, BattleClaimed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestKarmaPinkDial(string userToken, string gameId, KarmaPinkDial e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestTraitorDiscarded(string userToken, string gameId, TraitorDiscarded e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestNexusPlayed(string userToken, string gameId, NexusPlayed e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestResourcesAudited(string userToken, string gameId, ResourcesAudited e) => await ProcessGameEvent(userToken, gameId, e);
    public async Task<VoidResult> RequestRecruitsPlayed(string userToken, string gameId, RecruitsPlayed e) => await ProcessGameEvent(userToken, gameId, e);
    
    public async Task<VoidResult> SetTimer(string userToken, string gameId, int value)
    {
        if (!AreValid(userToken, gameId, out var user, out var game, out var error))
            return error;

        if (!game.Game.IsHost(user.Id))
            return Error(ErrorType.NoHost);
        
        await Clients.Group(gameId).HandleSetTimer(value);
        return Success();
    }
    
    private async Task<VoidResult> ProcessGameEvent<TEvent>(string userToken, string gameId, TEvent e) where TEvent : GameEvent
    {
        Console.WriteLine($"Start processing event {e.GetType().Name}...");
        
        if (!AreValid(userToken, gameId, out var user, out var game, out var error))
            return error;
        
        e.Initialize(game.Game);
        e.Time = DateTimeOffset.Now;
        
        Console.WriteLine($"Finished processing event {e.GetType().Name}.");
        
        return await ValidateAndExecute(gameId, e, game, game.Game.IsHost(user.Id));
    }

    private async Task<VoidResult> ValidateAndExecute<TEvent>(string gameId, TEvent e, ManagedGame game, bool isHost)
        where TEvent : GameEvent
    {
        var validationResult = e.Execute(true, isHost);
        e.Time = DateTimeOffset.Now;
        
        if (validationResult != null)
        {
            return Error(ErrorType.InvalidGameEvent, validationResult.ToString());
        }

        if (game.Game.CurrentMainPhase is MainPhase.Ended && !game.StatisticsSent)
        {
            await SendMailAndStatistics(game);
            game.StatisticsSent = true;
        }

        await Clients.Group(gameId).HandleGameEvent(e, game.Game.History.Count);
        
        await SendAsyncPlayMessagesIfApplicable(game);
        
        var botDelay = DetermineBotDelay(game.Game.CurrentMainPhase, e);
        _ = Task.Delay(botDelay).ContinueWith(_ => PerformBotEvent(gameId, game));
        
        return Success();
    }

    private async Task SendAsyncPlayMessagesIfApplicable(ManagedGame game)
    {
        Console.WriteLine("Start SendAsyncPlayMessagesIfApplicable...");
        
        if (game.Game.Settings.AsyncPlay)
        {
            var whatHappened = game.Game.History.Where(e => e.Time > game.LastAsyncPlayMessageSent).ToList();
            if (whatHappened.Count == 0)
                return;

            var now = DateTimeOffset.Now;
            var elapsed = (int)now.Subtract(game.LastAsyncPlayMessageSent).TotalMinutes;
            if (elapsed < game.Game.Settings.AsyncPlayMessageIntervalMinutes)
            {
                Console.WriteLine("Scheduling SendAsyncPlayMessagesIfApplicable()...");
                _ = Task.Delay(60000 * (game.Game.Settings.AsyncPlayMessageIntervalMinutes - elapsed) + 1000)
                    .ContinueWith(_ => SendAsyncPlayMessagesIfApplicable(game));
                
                return;
            }
                
            var history = $"The following happened:{Environment.NewLine}{Environment.NewLine}";
            foreach (var evt in whatHappened)
            {
                history += "- " + evt + Environment.NewLine;
            }
            
            var turnInfo = Skin.Current.Format("Turn: {0}, phase: {1}", game.Game.CurrentTurn, game.Game.CurrentPhase);
            
            await using var context = GetDbContext();

            foreach (var userId in game.Game.Participation.SeatedPlayers.Keys)
            {
                var user = await context.Users.FindAsync(userId);
                var mail = user?.Email;
                if (mail == null) continue;

                var player = game.Game.GetPlayerByUserId(userId);
                if (player == null) continue;

                var status = GameStatus.DetermineStatus(game.Game, player, true) + Environment.NewLine +
                             Environment.NewLine;

                var asyncMessage = turnInfo + Environment.NewLine + Environment.NewLine + status +
                                   Environment.NewLine +
                                   Environment.NewLine + history;

                MailMessage mailMessage = new()
                {
                    From = new MailAddress("noreply@treachery.online"),
                    Subject = $"Update for {game.Game.Name}",
                    IsBodyHtml = true,
                    Body = asyncMessage
                };

                mailMessage.To.Add(new MailAddress(mail));
                SendMail(mailMessage);
            }
        }
        
        Console.WriteLine("Finished SendAsyncPlayMessagesIfApplicable...");
    }

    private async Task PerformBotEvent(string gameId, ManagedGame managedGame)
    {
        var game = managedGame.Game;
        if (!managedGame.BotsArePaused && game.CurrentPhase > Phase.AwaitingPlayers)
        {
            var bots = Deck<Player>.Randomize(game.Players.Where(p => p.IsBot));
            foreach (var bot in bots)
            {
                var events = game.GetApplicableEvents(bot, false);
                var evt = 
                    bot.DetermineHighestPrioInPhaseAction(events) ??
                    bot.DetermineHighPrioInPhaseAction(events) ??
                    bot.DetermineMiddlePrioInPhaseAction(events) ??
                    bot.DetermineLowPrioInPhaseAction(events);
                          
                if (evt != null)
                {
                    await ValidateAndExecute(gameId, evt, managedGame, false);
                    return;
                }
            }
        }
    }
    
    private static int DetermineBotDelay(MainPhase phase, GameEvent e)
    {
        if (phase is MainPhase.Resurrection or MainPhase.Charity || e is AllyPermission || e is DealOffered || e is SetShipmentPermission)
            return 300;
        
        if (e is Bid)
            return 800;
        
        if (phase is MainPhase.ShipmentAndMove or MainPhase.Battle)
            return 3200;
        
        return 1600;
    }

    private async Task SendMailAndStatistics(ManagedGame game)
    {
        var state = GameState.GetStateAsString(game.Game);
        SendEndOfGameMail(state, GameInfo.Extract(game, -1));
        await SendGameStatistics(game.Game);
    }
}

