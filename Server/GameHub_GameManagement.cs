﻿using System.IO;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Treachery.Shared;

namespace Treachery.Server;

public partial class GameHub
{
    public async Task<Result<JoinInfo>> RequestCreateGame(string playerToken, string hashedPassword, string settings)
        => await CreateOrLoadGame(playerToken, hashedPassword, settings, null);
    
    public async Task<Result<JoinInfo>> RequestLoadGame(string playerToken, string hashedPassword, string settings, string stateData)
        => await CreateOrLoadGame(playerToken, hashedPassword, settings, stateData);
    
    private async Task<Result<JoinInfo>> CreateOrLoadGame(string playerToken, string hashedPassword, string settings, string stateData)
    {
        if (!usersByPlayerToken.TryGetValue(playerToken, out var player))
            return Error<JoinInfo>("Player not found");

        Game game;
        if (stateData != null)
        {
            var state = GameState.Load(stateData);
            var errorMessage = Game.TryLoad(state, false, false, out var loadedGame);

            if (errorMessage == null)
                game = loadedGame;
            else
                return Error<JoinInfo>(errorMessage.ToString());
        }
        else
        {
            game = new Game();
        }
        
        var managedGame = new ManagedGame
        {
            Game = game, //TODO apply settings here
            Players = [player],
            Hosts = [player],
            HashedPassword = hashedPassword
        };
        var gameToken = GenerateToken();
        var gameId = Guid.NewGuid().ToString();
        gamesByGameToken[gameToken] = managedGame; 
        gameTokensByGameId[gameId] = gameToken;

        await Groups.AddToGroupAsync(Context.ConnectionId, gameToken);
        await Clients.Group(gameToken).UpdateParticipation(managedGame.GetParticipation());
        
        return await Task.FromResult(Success(new JoinInfo { GameToken = gameToken, GameState = stateData ?? GameState.GetStateAsString(game) }));
        
    }
    
    public async Task<Result<JoinInfo>> RequestJoinGame(string playerToken, string gameId, string hashedPassword, Faction faction)
    {
        if (!usersByPlayerToken.TryGetValue(playerToken, out var player))
            return Error<JoinInfo>("Player not found");

        if (!gameTokensByGameId.TryGetValue(gameId, out var gameToken) || !gamesByGameToken.TryGetValue(gameToken, out var game))
            return Error<JoinInfo>("Game not found");
        
        if (game.HashedPassword != null && !game.HashedPassword.Equals(hashedPassword))
            return Error<JoinInfo>("Incorrect password");
        
        if (!game.HasRoomFor(faction))
            return Error<JoinInfo>("Seat is not available");
    
        game.Players.Add(player);
        await Groups.AddToGroupAsync(Context.ConnectionId, gameToken);
        await Clients.Group(gameToken).UpdateParticipation(game.GetParticipation());
        return await Task.FromResult(Success(new JoinInfo { GameToken = gameToken, GameState = GameState.GetStateAsString(game.Game) }));
    }
    
    public async Task<Result<JoinInfo>> RequestObserveGame(string playerToken, string gameId, string hashedPassword)
    {
        if (!usersByPlayerToken.TryGetValue(playerToken, out var player))
            return Error<JoinInfo>("Player not found");

        if (!gameTokensByGameId.TryGetValue(gameId, out var gameToken) || !gamesByGameToken.TryGetValue(gameToken, out var game))
            return Error<JoinInfo>("Game not found");
        
        /*
        if (game.HashedPassword != null && !game.HashedPassword.Equals(hashedPassword))
            return Error<string>("Incorrect password");
        */
        
        game.Observers.Add(player);
        await Groups.AddToGroupAsync(Context.ConnectionId, gameToken);
        await Clients.Group(gameToken).UpdateParticipation(game.GetParticipation());
        return await Task.FromResult(Success(new JoinInfo { GameToken = gameToken, GameState = GameState.GetStateAsString(game.Game) }));
    }

    public async Task<Result<JoinInfo>> RequestReconnectGame(string playerToken, string gameToken)
    {
        if (!AreValid<JoinInfo>(playerToken, gameToken, out _, out var game, out var error))
            return error;
        
        await Groups.AddToGroupAsync(Context.ConnectionId, gameToken);
        return Success(new JoinInfo { GameToken = gameToken, GameState = GameState.GetStateAsString(game.Game) } );
    }
    
    public async Task<Result<string>> RequestGameState(string playerToken, string gameToken)
    {
        if (!AreValid<string>(playerToken, gameToken, out _, out var game, out var error))
            return error;
        
        return await Task.FromResult(Success(GameState.GetStateAsString(game.Game)));
    }

    public async Task<VoidResult> RequestSetSkin(string playerToken, string gameToken, string skin)
    {
        if (!AreValid(playerToken, gameToken, out var player, out var game, out var error))
            return error;

        if (!game.IsHost(player))
            return Error("You are not the host");

        await Clients.Group(gameToken).HandleSetSkin(skin);
        return Success();
    }

    public async Task<VoidResult> RequestUndo(string playerToken, string gameToken, int untilEventNr)
    {
        if (!AreValid(playerToken, gameToken, out var playerId, out var game, out var error))
            return error;

        if (!game.IsHost(playerId))
            return Error("You are not the host");

        game.Game.Undo(untilEventNr);
        await Clients.Group(gameToken).HandleUndo(untilEventNr);
        return Success();
    }

    public async Task<VoidResult> RequestPauseBots(string playerToken, string gameToken)
    {
        if (!AreValid(playerToken, gameToken, out var playerId, out var game, out var error))
            return error;

        if (!game.IsHost(playerId))
            return Error("You are not the host");

        game.BotsArePaused = !game.BotsArePaused;
        return await Task.FromResult(Success());
    }
    
    private void SendEndOfGameMail(string content, GameInfo info)
    {
        var from = configuration["GameEndEmailFrom"];
        var to = configuration["GameEndEmailTo"];
        if (from == null || to == null)
            return;
        
        var ruleset = Game.DetermineApproximateRuleset(info.FactionsInPlay, info.Rules, info.ExpansionLevel);
        var subject = $"{info.GameName} ({info.Players.Length} Players, {info.NumberOfBots} Bots, Turn {info.CurrentTurn} - {ruleset})";
        var saveGameToAttach = new Attachment(GenerateStreamFromString(content), "savegame" + DateTime.Now.ToString("yyyyMMdd.HHmm") + ".json");
        
        MailMessage mailMessage = new()
        {
            From = new MailAddress(from),
            Subject = subject,
            IsBodyHtml = true,
            Body = "Game finished!",
            Priority = info.NumberOfBots < 0.5f * info.Players.Length ? MailPriority.Normal : MailPriority.Low
        };

        mailMessage.To.Add(new MailAddress(to));
        mailMessage.Attachments.Add(saveGameToAttach);

        SendMail(mailMessage);
    }
    
    private static async Task SendGameStatistics(Game game)
    {
        try
        {
            var statistics = GameStatistics.GetStatistics(game);
            var httpClient = new HttpClient();
            var data = GetStatisticsAsString(statistics);
            var json = new StringContent(data, Encoding.UTF8, "application/json");
            await httpClient.PostAsync("https://dune.games/.netlify/functions/plays-add", json);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error sending statistics: {0}", e.Message);
        }
    }

    private static string GetStatisticsAsString(GameStatistics g)
    {
        var serializer = JsonSerializer.CreateDefault();
        serializer.TypeNameHandling = TypeNameHandling.None;
        var writer = new StringWriter();
        serializer.Serialize(writer, g);
        writer.Close();
        return writer.ToString();
    }

}

