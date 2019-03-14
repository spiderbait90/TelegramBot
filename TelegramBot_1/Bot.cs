using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot_1
{
    internal class Bot
    {
        private static TelegramBotClient botClient;

        private const string username = "put here username of isntagram account";
        private const string password = "put here password of instagram account";

        private static IInstaApi api;
        private static IResult<InstaMediaList> userMedia;
        private static long chatId;
        private static string state;

        public Bot()
        {
            botClient = new TelegramBotClient("put here bot token");
        }

        internal async void Login()
        {
            api = InstaApiBuilder.CreateBuilder()
                .SetUser(new UserSessionData { UserName = username, Password = password })
                .Build();

            while (true)
            {
                var login = await api.LoginAsync();

                if (login.Succeeded)
                {
                    Console.WriteLine("Login Successful");
                    break;
                }

                Console.WriteLine("Login Unsuccessful");
                
            }
        }

        public void StartingToReceiveMessages()
        {
            botClient.OnMessage += BotClient_OnMessage;
            botClient.StartReceiving();
            Console.ReadLine();
            botClient.StopReceiving();
        }

        private static async void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            
            chatId = e.Message.Chat.Id;
            var input = e.Message.Text.ToLower();
            Console.WriteLine(input);
            var commands = new[]
            {
                "/getinfo",
                "/getfan"
            };

            if (state == null)
            {
                if (commands.Contains(input))
                {
                    state = input;
                    await botClient.SendTextMessageAsync(chatId, "Enter Username :");
                }
                else
                {
                    NoSuchCommand();
                }
            }
            else
            {
                if (state == commands[0])
                {
                    GetUserInfo(input);
                }
                if (state == commands[1])
                {
                    GetBiggestFan(input);
                }

                state = null;
            }
        }

        private static async void GetBiggestFan(string userName)
        {
            userMedia = await api.UserProcessor.GetUserMediaAsync(userName,
                PaginationParameters.MaxPagesToLoad(1));

            if (!userMedia.Succeeded)
            {
                await botClient.SendTextMessageAsync(chatId, $"{userMedia.Info.Message}");
                return;
            }

            if (userMedia.Value.Count == 0)
            {
                await botClient.SendTextMessageAsync(chatId, "This user has no posts");
                return;
            }

            var users = new Dictionary<string, int>();

            foreach (var m in userMedia.Value)
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);

                var likers = await api.MediaProcessor.GetMediaLikersAsync(m.InstaIdentifier);

                foreach (var l in likers.Value)
                {
                    if (!users.ContainsKey(l.UserName))
                        users[l.UserName] = 0;

                    users[l.UserName]++;
                }
            }

            var biggestFan = users.OrderByDescending(x => x.Value).First();
            var biggestFanInfo = await api.UserProcessor.GetUserAsync(biggestFan.Key);

            await botClient.SendPhotoAsync(chatId, biggestFanInfo.Value.ProfilePicture);
            await botClient.SendTextMessageAsync(chatId,
                $"Username : {biggestFan.Key}" +
                $"{Environment.NewLine}" +
                $"Likes for the last {userMedia.Value.Count} posts : {biggestFan.Value}");
        }

        private static async void NoSuchCommand()
        {
            await botClient.SendTextMessageAsync(chatId, $@"Valid Format Commands{Environment.NewLine}" +
                                                         $"----------------------{Environment.NewLine}" +
                                                         $"/getinfo{Environment.NewLine}" +
                                                         $"/getfan");
        }

        private static async void GetUserInfo(string input)
        {
            var userToSerch = input;

            var user = await api.UserProcessor.GetUserAsync(userToSerch);

            if (user.Succeeded)
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);

                userMedia = await api.UserProcessor.GetUserMediaAsync(userToSerch, PaginationParameters.MaxPagesToLoad(1));

                if (!userMedia.Succeeded)
                {
                    await botClient.SendTextMessageAsync(chatId, $"{userMedia.Info.Message}");
                    return;
                }

                var averageLikes = GetAverageLikes();

                await botClient.SendPhotoAsync(chatId, user.Value.ProfilePicture);

                await botClient.SendTextMessageAsync(chatId,
                    $"User : {user.Value.UserName}{Environment.NewLine}" +
                    $"Followers Count : {user.Value.FollowersCount}{Environment.NewLine}" +
                    $"Average likes for the last 6 posts : {averageLikes}");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "User Not Found");
            }
        }

        private static double GetAverageLikes()
        {
            var toTake = 6;

            if (userMedia.Value.Count == 0)
                return 0;

            if (userMedia.Value.Count < 6)
                toTake = userMedia.Value.Count;

            var averageLikes = userMedia.Value.Take(toTake)
                 .Select(x => x.LikesCount)
                 .Average();

            return Math.Round(averageLikes);
        }
    }
}
