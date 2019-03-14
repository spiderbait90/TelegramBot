using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using Telegram.Bot;
using Telegram.Bot.Args;


namespace TelegramBot_1
{
    class Program
    {
        static void Main()
        {
            var bot = new Bot();
            bot.Login();
            bot.StartingToReceiveMessages();
        }
        
    }
}
