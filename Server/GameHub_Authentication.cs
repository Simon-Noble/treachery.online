﻿using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Treachery.Shared;

namespace Treachery.Server;

public partial class GameHub
{
    public async Task<Result<LoginInfo>> RequestCreateUser(string userName, string hashedPassword, string email, string playerName)
    {
        var trimmedUsername = userName.Trim().ToLower();
        
        if (trimmedUsername.Trim().Length <= 3)
        {
            return Error<LoginInfo>(ErrorType.UserNameTooShort);
        }
        
        await using var db = GetDbContext();
        
        if (db.Users.Any(x => x.Name.Trim().ToLower().Equals(trimmedUsername)))
        {
            return Error<LoginInfo>(ErrorType.UserNameExists);
        }

        db.Add(new User
            { Name = trimmedUsername, HashedPassword = hashedPassword, Email = email, PlayerName = playerName });

        await db.SaveChangesAsync();
        
        var user = await db.Users.FirstOrDefaultAsync(x =>
            x.Name.Trim().ToLower().Equals(userName.Trim().ToLower()) && x.HashedPassword == hashedPassword);
        
        if (user == null)
            return Error<LoginInfo>(ErrorType.UserCreationFailed);
        
        MailMessage mailMessage = new()
        {
            From = new MailAddress("noreply@treachery.online"),
            Subject = "Welcome to treachery.online",
            IsBodyHtml = true,
            Body = $"""
                    Welcome to treachery.online, {trimmedUsername}!
                    {Environment.NewLine}{Environment.NewLine}
                    If you ever need to reset your password, a reset token will be sent to this e-mail address.
                    """
        };

        mailMessage.To.Add(new MailAddress(email));
        SendMail(mailMessage);
        
        var userToken = GenerateToken();
        UsersByUserToken.TryAdd(userToken, user);
        UserTokenInfo[userToken] = new TokenInfo();
        return Success(new LoginInfo { UserId = user.Id, Token = userToken, PlayerName = user.PlayerName, UserName = user.Name, Email = user.Email });
    }
    
    public async Task<Result<LoginInfo>> RequestLogin(int version, string userName, string hashedPassword)
    {
        if (Game.LatestVersion != version)
            return Error<LoginInfo>(ErrorType.InvalidGameVersion);
        
        await using var db = GetDbContext();
        
        var user = await db.Users.FirstOrDefaultAsync(x =>
            x.Name.Trim().ToLower().Equals(userName.Trim().ToLower()) && x.HashedPassword == hashedPassword);
        
        if (user == null)
            return Error<LoginInfo>(ErrorType.InvalidUserNameOrPassword);

        var userToken = GenerateToken();
        UsersByUserToken.TryAdd(userToken, user);
        UserTokenInfo[userToken] = new TokenInfo();
        return Success(new LoginInfo { UserId = user.Id, Token = userToken, PlayerName = user.PlayerName, UserName = user.Name, Email = user.Email });
    }
    
    public async Task<VoidResult> RequestPasswordReset(string email)
    {
        await using var db = GetDbContext();
        
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email.Trim().ToLower().Equals(email.Trim().ToLower()));
        
        if (user == null)
            return Error(ErrorType.UnknownEmailAddress);

        if ((DateTime.Now - user.PasswordResetTokenCreated).TotalMinutes < 10)
            return Error(ErrorType.ResetRequestTooSoon);

        var token = GenerateToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenCreated = DateTime.Now;

        await db.SaveChangesAsync();

        MailMessage mailMessage = new()
        {
            From = new MailAddress("noreply@treachery.online"),
            Subject = "Password Reset",
            IsBodyHtml = true,
            Body = $"""
                    You have requested a password reset for user: {user.Name}
                    {Environment.NewLine}{Environment.NewLine}
                    You can use this token to reset your password: {token}
                    """
        };

        mailMessage.To.Add(new MailAddress(user.Email));
        SendMail(mailMessage);
        
        return null;
    }
    
    public async Task<Result<LoginInfo>> RequestSetPassword(string userName, string passwordResetToken, string newHashedPassword)
    {
        await using var db = GetDbContext();

        var user = await db.Users.FirstOrDefaultAsync(x =>
            x.Name.Trim().ToLower().Equals(userName.Trim().ToLower()));
        
        if (user == null)
            return Error<LoginInfo>(ErrorType.UnknownUserName);

        if (string.IsNullOrEmpty(user.PasswordResetToken) || user.PasswordResetToken.Trim() != passwordResetToken)
            return Error<LoginInfo>(ErrorType.InvalidResetToken);
        
        if ((DateTime.Now - user.PasswordResetTokenCreated).TotalMinutes > 60)
            return Error<LoginInfo>(ErrorType.ResetTokenExpired);

        user.HashedPassword = newHashedPassword;
        user.PasswordResetToken = null;
        user.PasswordResetTokenCreated = default;

        await db.SaveChangesAsync();
        
        var userToken = GenerateToken();
        UsersByUserToken.TryAdd(userToken, user);
        return Success(new LoginInfo { UserId = user.Id, Token = userToken, PlayerName = user.PlayerName, UserName = user.Name, Email = user.Email });
    }
    
    public async Task<Result<LoginInfo>> GetLoginInfo(string userToken)
    {
        if (!UsersByUserToken.TryGetValue(userToken, out var user))
            return Error<LoginInfo>(ErrorType.UserNotFound);
            
        return await Task.FromResult(Success(new LoginInfo { UserId = user.Id, Token = userToken, PlayerName = user.PlayerName, UserName = user.Name, Email = user.Email }));
    }
    
    public async Task<Result<LoginInfo>> RequestUpdateUserInfo(string userToken, string hashedPassword, string playerName, string email)
    {
        if (!UsersByUserToken.TryGetValue(userToken, out var user))
            return Error<LoginInfo>(ErrorType.UserNotFound);
        
        user.PlayerName = playerName;
        user.Email = email;
        if (!string.IsNullOrEmpty(hashedPassword))
            user.HashedPassword = hashedPassword;
        
        await using var db = GetDbContext();
        db.Users.Update(user);
        await db.SaveChangesAsync();
/*
        var dbUser = await db.Users.FindAsync(user.Id); 
        
        if (dbUser == null)
            return Error<LoginInfo>(ErrorType.UserNotFound);

        if (!string.IsNullOrEmpty(hashedPassword))
            dbUser.HashedPassword = hashedPassword;
        
        dbUser.PlayerName = playerName;
        dbUser.Email = email;

        await db.SaveChangesAsync();
        */
            
        return await Task.FromResult(Success(new LoginInfo { UserId = user.Id, Token = userToken, PlayerName = user.PlayerName, UserName = user.Name, Email = user.Email }));
    }

}

