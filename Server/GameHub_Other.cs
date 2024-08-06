﻿using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Treachery.Shared;

namespace Treachery.Server;

public partial class GameHub
{
    public Result<ServerSettings> Connect()
    {
        //Do some maintenance work here, cleaning up old tokens
        
        var result = new ServerSettings
        {
            ScheduledMaintenance = MaintenanceDate,
            AdminName = configuration["GameAdminUsername"]
        };

        return Success(result);
    }
    
    public async Task<VoidResult> RequestRegisterHeartbeat(string userToken)
    {
        if (!UserTokenInfo.TryGetValue(userToken, out var tokenInfo))
            return Error("User not found");

        tokenInfo.Refreshed = DateTime.Now;
        return await Task.FromResult(Success());
    }

    public async Task<Result<string>> AdminUpdateMaintenance(string userToken, DateTime maintenanceDate)
    {
        if (!UsersByUserToken.TryGetValue(userToken, out var user) || user.Name != configuration["GameAdminUsername"])
            return Error<string>("Not authorized");

        MaintenanceDate = maintenanceDate;
        return await Task.FromResult(Success("Maintenance window updated"));
    }

    public async Task<Result<string>> AdminPersistState(string userToken)
    {
        if (!UsersByUserToken.TryGetValue(userToken, out var user) || user.Name != configuration["GameAdminUsername"])
            return Error<string>("Not authorized");
        
        var amount = 0;
        await using (var context = GetDbContext())
        {
            await context.PersistedGames.ExecuteDeleteAsync();
            
            foreach (var gameTokenAndManagedGame in GamesByGameToken)
            {
                var game = gameTokenAndManagedGame.Value.Game;

                var persisted = new PersistedGame
                {
                    Token = gameTokenAndManagedGame.Key,
                    CreatorUserId = gameTokenAndManagedGame.Value.CreatorUserId,
                    GameId = gameTokenAndManagedGame.Value.GameId,
                    GameState = GameState.GetStateAsString(game),
                    GameParticipation = JsonSerializer.Serialize(game.Participation),
                    HashedPassword = gameTokenAndManagedGame.Value.HashedPassword,
                    BotsArePaused = gameTokenAndManagedGame.Value.BotsArePaused,
                    ObserversRequirePassword = gameTokenAndManagedGame.Value.ObserversRequirePassword,
                    StatisticsSent = gameTokenAndManagedGame.Value.StatisticsSent
                };

                context.PersistedGames.Add(persisted);
                amount++;
            }
        }
            
        return Success($"Number of games persisted: {amount}");

    }

    public async Task<Result<string>> AdminRestoreState(string userToken)
    {
        if (!UsersByUserToken.TryGetValue(userToken, out var user) || user.Name != configuration["GameAdminUsername"])
            return Error<string>("Not authorized");
        
        var amount = 0;
        await using (var context = GetDbContext())
        {
            GamesByGameToken.Clear();
            
            foreach (var persistedGame in context.PersistedGames)
            {
                var token = persistedGame.Token;
                var gameState = GameState.Load(persistedGame.GameState);
                var participation = JsonSerializer.Deserialize<GameParticipation>(persistedGame.GameParticipation);
                if (Game.TryLoad(gameState, participation, false, true, out Game game) == null)
                {
                    var managedGame = new ManagedGame()
                    {
                        CreatorUserId = persistedGame.CreatorUserId,
                        GameId = persistedGame.GameId,
                        Game = game,
                        HashedPassword = persistedGame.HashedPassword,
                        BotsArePaused = persistedGame.BotsArePaused,
                        ObserversRequirePassword = persistedGame.ObserversRequirePassword,
                        StatisticsSent = persistedGame.StatisticsSent
                    };
                    GamesByGameToken.TryAdd(token, managedGame);
                }
                amount++;
            }
        }
            
        return Success($"Number of games restored: {amount}");
    }

    public async Task<Result<string>> AdminCloseGame(string userToken, string gameId)
    {
        if (!UsersByUserToken.TryGetValue(userToken, out var user) || user.Name != configuration["GameAdminUsername"])
            return Error<string>("Not authorized");

        if (GameTokensByGameId.TryGetValue(gameId, out var gameToken))
        {
            GameTokensByGameId.Remove(gameId, out _);

            if (GamesByGameToken.TryGetValue(gameToken, out var game))
            {
                GamesByGameToken.Remove(gameToken, out _);

                foreach (var userId in game.Game.Participation.Users.Keys)
                {
                    game.Game.RemoveUser(userId, true);
                    await Clients.Group(gameToken).HandleRemoveUser(userId, true);
                    await RemoveFromGroup(gameToken, userId);
                }
            }
        }
        
        return Success("Game removed");
    }
}