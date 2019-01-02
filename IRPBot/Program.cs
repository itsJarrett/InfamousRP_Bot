using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MySql.Data.MySqlClient;

namespace IRPBot
{
    public enum WhitelistServer
    {
        Production,
        Development
    }
    
    internal class Program
    {
        public List<ulong> waitingProduction = new List<ulong>();
        public List<ulong> waitingDevelopment = new List<ulong>();
        
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            if (!Settings.isConfigured())
            {
                Settings.saveSettings();
                throw new Exception("Settings not configured.");
            }

            Settings.loadSettings();

            var client = new DiscordSocketClient();
            client.Log += Log;

            string token = Settings.getSettings().DiscordBotToken;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            client.GuildMemberUpdated += ClientOnGuildMemberUpdated;
            client.MessageReceived += ClientOnMessageReceived;

            await client.SetGameAsync("InfamousRoleplay.com");
            await Task.Delay(-1);
        }

        public bool IsSteamHexInDB(string steamHex)
        {
            using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM whitelisted WHERE steamHex=@steamHex", conn);
                    cmd.Parameters.AddWithValue("@steamHex", steamHex);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        return reader.HasRows;
            }
            return false;
        }
        
        public bool IsDiscordIdInDB(ulong discordId)
        {
            using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM whitelisted WHERE discordId=@discordId", conn);
                cmd.Parameters.AddWithValue("@discordId", discordId);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        return reader.HasRows;
            }
            return false;
        }
        
        public bool CheckWhitelistStaus(string steamHex, WhitelistServer serverType)
        {
            using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
            {
                conn.OpenAsync();
                var cmd = new MySqlCommand("SELECT * FROM whitelisted WHERE steamHex=@steamHex", conn);
                cmd.Parameters.AddWithValue("@steamHex", steamHex);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        if (serverType.Equals(WhitelistServer.Production) && reader.GetBoolean(3))
                        {
                            return true;
                        }
                        if (serverType.Equals(WhitelistServer.Development) && reader.GetBoolean(4))
                        {
                            return true;
                        }
                    }
            }
            return false;
        }
        
        public async Task UpdateSteamHex(SocketUser socketUser, string steamHex)
        {
            if (IsDiscordIdInDB(socketUser.Id))
            {
                using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
                {
                    await conn.OpenAsync();
                    var cmd = new MySqlCommand("UPDATE whitelisted SET steamHex=@steamHex WHERE discordId=@discordId", conn);
                    cmd.Parameters.AddWithValue("@steamHex", steamHex);
                    cmd.Parameters.AddWithValue("@discordId", socketUser.Id);
                    cmd.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task UpdateWhitelistStatus(SocketUser socketUser, string steamHex, WhitelistServer server, bool status)
        {
            if (IsSteamHexInDB(steamHex))
            {
                switch (server)
                {
                    case WhitelistServer.Production:
                    {
                        using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
                        {
                            await conn.OpenAsync();
                            var cmd = new MySqlCommand("UPDATE whitelisted SET userName=@userName, steamHex=@steamHex, production=@production WHERE steamHex=@steamHex", conn);
                            cmd.Parameters.AddWithValue("@discordId", socketUser.Id);
                            cmd.Parameters.AddWithValue("@userName", socketUser.Username);
                            cmd.Parameters.AddWithValue("@steamHex", steamHex);
                            cmd.Parameters.AddWithValue("@production", status);
                            cmd.ExecuteNonQueryAsync();
                        }

                        break;
                    }
                    case WhitelistServer.Development:
                    {
                        using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
                        {
                            await conn.OpenAsync();
                            var cmd = new MySqlCommand("UPDATE whitelisted SET userName=@userName, steamHex=@steamHex, development=@development WHERE steamHex=@steamHex", conn);
                            cmd.Parameters.AddWithValue("@discordId", socketUser.Id);
                            cmd.Parameters.AddWithValue("@userName", socketUser.Username);
                            cmd.Parameters.AddWithValue("@steamHex", steamHex);
                            cmd.Parameters.AddWithValue("@development", status);
                            cmd.ExecuteNonQueryAsync();
                        }

                        break;
                    }
                }
            }
            else
            {
                switch (server)
                {
                    case WhitelistServer.Production:
                    {
                        using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
                        {
                            await conn.OpenAsync();
                            var cmd = new MySqlCommand("INSERT INTO whitelisted (discordId, userName, steamHex, production, development) VALUES (@discordId, @userName, @steamHex, @production, @development)", conn);
                            cmd.Parameters.AddWithValue("@discordId", socketUser.Id);
                            cmd.Parameters.AddWithValue("@userName", socketUser.Username);
                            cmd.Parameters.AddWithValue("@steamHex", steamHex);
                            cmd.Parameters.AddWithValue("@production", status);
                            cmd.Parameters.AddWithValue("@development", false);
                            cmd.ExecuteNonQueryAsync();
                        }

                        break;
                    }
                    case WhitelistServer.Development:
                    {
                        using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
                        {
                            await conn.OpenAsync();
                            var cmd = new MySqlCommand("INSERT INTO whitelisted (discordId, userName, steamHex, production, development) VALUES (@discordId, @userName, @steamHex, @production, @development)", conn);
                            cmd.Parameters.AddWithValue("@discordId", socketUser.Id);
                            cmd.Parameters.AddWithValue("@userName", socketUser.Username);
                            cmd.Parameters.AddWithValue("@steamHex", steamHex);
                            cmd.Parameters.AddWithValue("@production", false);
                            cmd.Parameters.AddWithValue("@development", status);
                            cmd.ExecuteNonQueryAsync();
                        }

                        break;
                    }
                }
            }
        }
        
         public async Task RemoveWhitelistStatus(SocketUser socketUser, WhitelistServer server)
        {
            switch (server)
            {
                case WhitelistServer.Production:
                {
                    using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
                    {
                        await conn.OpenAsync();
                        var cmd = new MySqlCommand("UPDATE whitelisted SET userName=@userName, production=@production WHERE discordId=@discordId", conn);
                        cmd.Parameters.AddWithValue("@discordId", socketUser.Id);
                        cmd.Parameters.AddWithValue("@userName", socketUser.Username);
                        cmd.Parameters.AddWithValue("@production", false);
                        cmd.ExecuteNonQueryAsync();
                    }

                    break;
                }
                case WhitelistServer.Development:
                {
                    using (var conn = new MySqlConnection(Settings.getSettings().MySQLString))
                    {
                        await conn.OpenAsync();
                        var cmd = new MySqlCommand("UPDATE whitelisted SET userName=@userName, development=@development WHERE discordId=@discordId", conn);
                        cmd.Parameters.AddWithValue("@discordId", socketUser.Id);
                        cmd.Parameters.AddWithValue("@userName", socketUser.Username);
                        cmd.Parameters.AddWithValue("@development", false);
                        cmd.ExecuteNonQueryAsync();
                    }

                    break;
                }
            }
        }

        private Task ClientOnGuildMemberUpdated(SocketGuildUser beforeGuildUser, SocketGuildUser afterGuildUser)
        {
            if (beforeGuildUser.IsBot || beforeGuildUser.IsWebhook) return Task.CompletedTask;
            
            string whitelistedRoleName = Settings.getSettings().WhitelistedRoleName;
            
            if (beforeGuildUser.IsBot || beforeGuildUser.IsWebhook) return Task.CompletedTask;
            List<string> beforeUserRoles = beforeGuildUser.Roles.Select(role => role.Name).ToList();
            List<string> afterUserRoles = afterGuildUser.Roles.Select(role => role.Name).ToList();

            if (afterUserRoles.Contains(whitelistedRoleName) && !beforeUserRoles.Contains(whitelistedRoleName) && 
                Settings.getSettings().ProductionGuildServerNames.Contains(beforeGuildUser.Guild.Name) &&
                !waitingProduction.Contains(beforeGuildUser.Id))
            {
                Console.WriteLine("ADDING USER TO WHITELIST FOR PRODUCTION");
                beforeGuildUser.SendMessageAsync("Hello, I am the Infamous Roleplay Discord Bot!\n " +
                "It appears you have recently been whitelisted for the server, so I would like to get you on there as soon as possible!\n" +
                "You can grab your Steam Hex from: http://vacbanned.com/\n" +
                "Please send me your Steam Hex formatted like so: ```110000105f9d1e1```\n" +
                "Once you send it to me, I will let you know that I've updated your whitelist status.");
                waitingProduction.Add(beforeGuildUser.Id);
            } else if (Settings.getSettings().ProductionGuildServerNames.Contains(beforeGuildUser.Guild.Name) && 
                       !afterUserRoles.Contains("Whitelisted") && beforeUserRoles.Contains("Whitelisted"))
            {
                Console.WriteLine("REMOVING USER FROM WHITELIST FOR PRODUCTION");
                if (waitingProduction.Contains(beforeGuildUser.Id))
                {
                    waitingProduction.Remove(beforeGuildUser.Id);
                }
                RemoveWhitelistStatus(beforeGuildUser, WhitelistServer.Production);
            }
            
            if (afterUserRoles.Contains(whitelistedRoleName) && !beforeUserRoles.Contains(whitelistedRoleName) && 
                Settings.getSettings().DevelopmentGuildServerNames.Contains(beforeGuildUser.Guild.Name) &&
                !waitingDevelopment.Contains(beforeGuildUser.Id))
            {
                Console.WriteLine("ADDING USER TO WHITELIST FOR DEVELOPMENT");
                beforeGuildUser.SendMessageAsync("Hello, I am the Infamous Roleplay Discord Bot!\n " +
                "It appears you have recently been whitelisted for the development server, so I would like to get you on there as soon as possible!\n" +
                "You can grab your Steam Hex from: http://vacbanned.com/\n" +
                "Please send me your Steam Hex formatted like so: ```110000105f9d1e1```\n" +
                "Once you send it to me, I will let you know that I've updated your whitelist status.");
                waitingDevelopment.Add(beforeGuildUser.Id);
            } else if (Settings.getSettings().DevelopmentGuildServerNames.Contains(beforeGuildUser.Guild.Name) && 
                       !afterUserRoles.Contains("Whitelisted") && beforeUserRoles.Contains("Whitelisted"))
            {
                Console.WriteLine("REMOVING USER FROM WHITELIST FOR DEVELOPMENT");
                if (waitingDevelopment.Contains(beforeGuildUser.Id))
                {
                    waitingDevelopment.Remove(beforeGuildUser.Id);
                }
                RemoveWhitelistStatus(beforeGuildUser, WhitelistServer.Development);
            }
            return Task.CompletedTask;
        }
        
        private Task ClientOnMessageReceived(SocketMessage socketMessage)
        {
            ulong userId = socketMessage.Author.Id;
            Regex hexPattern = new Regex("^([0-9]{8})([0-9a-fA-F]+)");
            if (socketMessage.Channel.GetType() != typeof(SocketDMChannel)) { return Task.CompletedTask; }
            if (waitingProduction.Contains(userId) || waitingDevelopment.Contains(userId))
            {
                if (!hexPattern.Match(socketMessage.Content).Success)
                {
                    socketMessage.Author.SendMessageAsync(
                        "That doesnt look right. Please send me your Steam Hex formatted like so: ```110000105f9d1e1```");
                    return Task.CompletedTask;
                }
                if (waitingProduction.Contains(userId))
                {
                    UpdateWhitelistStatus(socketMessage.Author, socketMessage.Content, WhitelistServer.Production, true);
                    socketMessage.Author.SendMessageAsync("Your whitelist status has been updated.");
                    waitingProduction.Remove(userId);
                    return Task.CompletedTask;
                }
                if (waitingDevelopment.Contains(userId))
                {
                    UpdateWhitelistStatus(socketMessage.Author, socketMessage.Content, WhitelistServer.Development, true);
                    socketMessage.Author.SendMessageAsync("Your whitelist status has been updated for development.");
                    waitingDevelopment.Remove(userId);
                    return Task.CompletedTask;
                }
            }

            if (IsDiscordIdInDB(socketMessage.Author.Id) && hexPattern.Match(socketMessage.Content).Success)
            {
                if (IsSteamHexInDB(socketMessage.Content))
                {
                    socketMessage.Author.SendMessageAsync("That Steam Hex is already whitelisted.");
                    return Task.CompletedTask;
                }
                UpdateSteamHex(socketMessage.Author, socketMessage.Content);
                socketMessage.Author.SendMessageAsync("Your Steam Hex has been updated.");
                return Task.CompletedTask;
            }
            if (IsDiscordIdInDB(socketMessage.Author.Id))
            {
                socketMessage.Author.SendMessageAsync(
                    "I have no need to talk with you. If you wish to update your Steam Hex, please send it like so: ```110000105f9d1e1```");
            }
            else
            {
                socketMessage.Author.SendMessageAsync("I have no need to talk to you.");
            }
            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}