using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Xml.XPath;
using System.Linq;

namespace ThiccBot
{
    public class User
    {
        public string username;
        public int points;
        public bool isGirl;
    }

    public class Serialize
    {

        public static void Run()
        {
            List<User> users = new List<User>();

            for (int i = 0; i < Program.Users.Count; i++)
            {
                users.Add(Program.Users[i]);
            }

            using (StreamWriter file = File.CreateText(@"c:\thicc.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, users);
            }
        }
    }

    public class LoadData : ModuleBase
    {
        [Command("load"), Summary("loads saved scored")]
        public async Task Say()
        {
            if(Program.Users.Count == 0)
            {
                await ReplyAsync($"loading saved scores");
                List<User> Users = JsonConvert.DeserializeObject<List<User>>(File.ReadAllText(@"c:\thicc.json"));
                for (int i = 0; i < Users.Count; i++)
                {
                    Program.Users.Add(Users[i]);
                }
            }
            else
            {
                await ReplyAsync($"Score data already loaded");
            }
        }
    }

    public class Program
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _services;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            string token = "Mzk5OTI0MDg0MDg5MDI4NjEw.DTVAug.TkUNqufDaRV3O7rPUmqqlxSsMOg"; // Remember to keep this private!

            _services = new ServiceCollection()
                        .AddSingleton(_client)
                        .AddSingleton(_commands)
                        .BuildServiceProvider();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;

            await InstallCommandsAsync();
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleCommandAsync;
            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new SocketCommandContext(_client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await _commands.ExecuteAsync(context, argPos, _services);
             //if (!result.IsSuccess)
             //    await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static List<User> Users = new List<User>();

        public static bool UserExists(string UID)
        {
            bool exists = false;
            foreach (User u in Users)
            {
                if (u.username == UID)
                {
                    exists = true;
                }
            }

            return exists;
        }

        public static void CreateUser(string UID)
        {
            User newUser = new User();
            newUser.username = UID;
            newUser.points = 0;
            Users.Add(newUser);
            Serialize.Run();
        }

        public static void AddPoint(string UID)
        {
            foreach (User u in Users)
            {
                if (u.username == UID)
                {
                    u.points++;
                }
            }
            Serialize.Run();
        }

        public static void ChangeGender(string UID)
        {
            foreach (User u in Users)
            {
                if (u.username == UID)
                {
                    if (u.isGirl)
                    {
                        u.isGirl = false;
                    }
                    else
                    {
                        u.isGirl = true;
                    }
                }
            }
            Serialize.Run();
        }

        public static void RemovePoint(string UID)
        {
            foreach (User u in Users)
            {
                if (u.username == UID)
                {
                    u.points--;
                }
            }
            Serialize.Run();
        }

        public static User FindUser(string UID)
        {
            User u = new User();
            foreach (User x in Users)
            {
                if (x.username == UID)
                {
                    u = x;
                }
            }

            return u;
        }

    }

    public class SignUp : ModuleBase
    {
        [Command("addme"), Summary("adds a user to the thiccboi system")]
        [Alias("user", "whois")]
        public async Task UserInfo()
        {
            string UID = Context.User.Username + "#" + Context.User.Discriminator;

            if (Program.UserExists(UID))
            {
                User u = Program.FindUser(UID);
                await ReplyAsync($"**{u.username} is already registered and has [{u.points}] points**");
            }
            else
            {
                Program.CreateUser(UID);
                User u = Program.FindUser(UID);
                await ReplyAsync($"**{u.username} has successfully been registered and has [{u.points}] points**");
            }
        }
    }

    public class Grill : ModuleBase
    {
        [Command("gender"), Summary("adds a user to the thiccboi system")]
        [Alias("user", "whois")]
        public async Task UserInfo()
        {
            string UID = Context.User.Username + "#" + Context.User.Discriminator;

            User u = Program.FindUser(UID);
            bool gender = u.isGirl;

            if (gender)
            {
                await ReplyAsync($"**{u.username} prefers to be referred to as a ThiccBoi**");
                Program.ChangeGender(UID);
            }
            if (!gender)
            {
                await ReplyAsync($"**{u.username} prefers to be referred to as a ThiccGrill**");
                Program.ChangeGender(UID);
            }
            
        }
    }

    public class UserScore : ModuleBase
    {
        [Command("myscore"), Summary("returns context user's score")]
        [Alias("user", "whois")]
        public async Task getScore()
        {
            string UID = Context.User.Username + "#" + Context.User.Discriminator;

            if (Program.UserExists(UID))
            {
                User u = Program.FindUser(UID);
                await ReplyAsync($"**{u.username} has [{u.points}] points**");
            }
            else
            {
                Program.CreateUser(UID);
                User u = Program.FindUser(UID);
                await ReplyAsync($"**{u.username} has successfully been registered and has [{u.points}] points**");
            }
        }
    }

    public class SignUpOther : ModuleBase
    {
        [Command("adduser"), Summary("adds a user to the thiccboi system")]
        [Alias("user", "whois")]
        public async Task UserInfo([Summary("The (optional) user to get info for")] IUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            string UID = userInfo.Username + "#" + userInfo.Discriminator;

            if (Program.UserExists(UID))
            {
                User u = Program.FindUser(UID);
                await ReplyAsync($"**{u.username} is already registered and has [{u.points}] points**");
            }
            else
            {
                Program.CreateUser(UID);
                User u = Program.FindUser(UID);
                await ReplyAsync($"**{u.username} has successfully been registered and has [{u.points}] points**");
            }

        }
    }

    public class GivePoints : ModuleBase
    {
        [Command("THICC"), Summary("give a user 1 thiccboi point")]
        [Alias("tuser", "whois")]
        public async Task UserInfo([Summary("The (optional) user to get info for")] IUser tuser = null)
        {
            var userInfo = tuser ?? Context.Client.CurrentUser;
            string UID = userInfo.Username + "#" + userInfo.Discriminator;
            string ContextUser;
            string TargetUser;

            ContextUser = Context.User.Username + "#" + Context.User.Discriminator;
            TargetUser = userInfo.Username + "#" + userInfo.Discriminator;

            if (ContextUser != TargetUser)
            {
                if (Program.UserExists(UID))
                {
                    User u = Program.FindUser(UID);
                    Program.AddPoint(UID);
                    await ReplyAsync($"**{u.username} now has [{u.points}] points**");
                }
                else
                {
                    Program.CreateUser(UID);
                    User u = Program.FindUser(UID);
                    Program.AddPoint(UID);
                    await ReplyAsync($"**{u.username} has successfully been registered and has [{u.points}] points**");
                }
            }
            else
            {
                await ReplyAsync($"**You cannot gove yourself points bro, that's just not fair**");
            }
        }

    }

    public class TakePoints : ModuleBase
    {
        [Command("UNTHICC"), Summary("removes 1 thiccboi point from user")]
        [Alias("user", "whois")]
        public async Task UserInfo([Summary("The (optional) user to get info for")] IUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            string UID = userInfo.Username + "#" + userInfo.Discriminator;

            string ContextUser;
            string TargetUser;

            ContextUser = Context.User.Username + "#" + Context.User.Discriminator;
            TargetUser = userInfo.Username + "#" + userInfo.Discriminator;

            if (TargetUser != ContextUser)
            {
                if (Program.UserExists(UID))
                {
                    User u = Program.FindUser(UID);
                    Program.RemovePoint(UID);
                    await ReplyAsync($"**{u.username} now has [{u.points}] points**");
                }
                else
                {
                    Program.CreateUser(UID);
                    User u = Program.FindUser(UID);
                    Program.RemovePoint(UID);
                    await ReplyAsync($"**{u.username} has successfully been registered and has [{u.points}] points**");
                }
            }
            else
            {
                await ReplyAsync($"**You probably shouldnt do that**");
            }
        }
    }

    public class Score : ModuleBase
    {
        [Command("scoreboard"), Summary("shows a scoreboard")]
        public async Task Say()
        {
            List<User> SortedUsers = Program.Users.OrderByDescending(o => o.points).ToList();

            string full = "";
            for (int i = 0; i < SortedUsers.Count; i++)
            {
                string board = ($"\n**#{i + 1} {SortedUsers[i].username} [{SortedUsers[i].points}] \n**");
                full += board;
            }
            await ReplyAsync($"{full}");

        }
    }

    public class help : ModuleBase
    {
        [Command("thicchelp"), Summary("shows a scoreboard")]
        public async Task Say()
        {
            await ReplyAsync($"**Type !thicchelp for the help menu \nType !addme to sign up for the THICCBOI system \nType !adduser USERNAME to sign up a friend for the THICCBOI system \nType !THICC USERNAME to give a THICC point \nType !THICC USERNAME to give a THICC point \nType !UNTHICC USERNAME to remove a point \nType !myscore to see your point total \nType !scoreboard to view the points leaderboard \nType !cleanup to delete messages from the bot**");
        }
    }

    public class DatBoi : ModuleBase
    {
        [Command("WhoDatBoi"), Summary("shows who the thiccest boi it")]
        public async Task Say()
        {
            List<User> SortedUsers = Program.Users.OrderByDescending(o => o.points).ToList();
            string gender = "BOI";
            if (SortedUsers[0].isGirl)
            {
                gender = "Grill";
            }
            await ReplyAsync($"**{SortedUsers[0].username} is currently the THICCEST {gender}! [{SortedUsers[0].points}] Points!**");
        }
    }

    public class addtoall : ModuleBase
    {
        [Command("massthicc"), Summary("gives everyone a point")]
        public async Task Say()
        {
            foreach (User u in Program.Users)
            {
                Program.AddPoint(u.username);
            }

            await ReplyAsync($"**You get a THICCPOINT! And YOU get a THICCPOINT! EVERYONE GETS A THICCPOINT!**");
        }
    }

    public class Purge : ModuleBase
    {
        [Command("cleanup"), Summary("shows a scoreboard")]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ClearMessage()
        {
            var messagesToDelete = await Context.Channel.GetMessagesAsync(100).Flatten();
            foreach (IMessage i in messagesToDelete)
            {
                if (i.Author.Username == "ThiccBot")
                {
                    await i.DeleteAsync();
                }

                string x = i.Content.ToLower();

                if(x == "!cleanup")
                {
                    await i.DeleteAsync();
                }

                if (x == "!load")
                {
                    await i.DeleteAsync();
                }

                if (x == "!scoreboard")
                {
                    await i.DeleteAsync();
                }

                if (x.Contains("!thicc"))
                {
                    await i.DeleteAsync();
                }

                if (x.Contains("!adduser"))
                {
                    await i.DeleteAsync();
                }

                if (x.Contains("Unknown command"))
                {
                    await i.DeleteAsync();
                }

                if (x == "!addme")
                {
                    await i.DeleteAsync();
                }

                if (x == "!whodatboi")
                {
                    await i.DeleteAsync();
                }

                if (x == "!gender")
                {
                    await i.DeleteAsync();
                }

                await Task.Delay(500);
            }
        }
    }
}


