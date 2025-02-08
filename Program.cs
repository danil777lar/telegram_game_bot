using System;
using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    public static class Program
    {
        private static string s_token = "";
        private static string s_game = "";

        private static CancellationTokenSource s_cts;
        private static TelegramBotClient s_bot;
        private static User s_me;

        private static async Task Main(string[] args)
        {
            ReadArgs(args);

            if (string.IsNullOrEmpty(s_token))
            {
                Console.WriteLine("Please provide a bot token as --token=...");
                return;
            }
            
            while (!NetworkInterface.GetIsNetworkAvailable())
            {
                Thread.Sleep(100);
            }

            s_cts = new CancellationTokenSource();
            s_bot = new TelegramBotClient(s_token, cancellationToken: s_cts.Token);

            s_me = await s_bot.GetMe();
            await s_bot.DeleteWebhook();
            await s_bot.DropPendingUpdates();

            s_bot.OnError += OnError;
            s_bot.OnMessage += OnMessage;
            
            
            Thread.Sleep(-1);
        }
        
        private static void ReadArgs(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith("--token="))
                {
                    s_token = arg.Substring("--token=".Length);
                }
                else if (arg.StartsWith("--game="))
                {
                    s_game = arg.Substring("--game=".Length);
                }
            }
        }

        static async Task OnError(Exception exception, HandleErrorSource source)
        {
            Console.WriteLine(exception);
            await Task.Delay(2000, s_cts.Token);
        }

        private static async Task OnMessage(Message msg, UpdateType type)
        {
            if (msg.Text is not { } text)
            {
                Console.WriteLine($"Received a message of type {msg.Type}");
            }
            else if (text.StartsWith('/'))
            {
                int space = text.IndexOf(' ');
                if (space < 0)
                {
                    space = text.Length;
                }
                string command = text[..space].ToLower();

                if (command.LastIndexOf('@') is > 0 and int at)
                {
                    if (command[(at + 1)..].Equals(s_me.Username, StringComparison.OrdinalIgnoreCase))
                    {
                        command = command[..at];
                    }
                    else
                    {
                        return;
                    }
                }

                await OnCommand(command, text[space..].TrimStart(), msg);
            }
            else
            {
                await OnTextMessage(msg);
            }
        }

        private static async Task OnTextMessage(Message msg)
        {
            Console.WriteLine($"Received text '{msg.Text}' in {msg.Chat}");
            await OnCommand("/start", "", msg);
        }

        private static async Task OnCommand(string command, string args, Message msg)
        {
            if (command == "/start")
            {
                await s_bot.SendMessage(msg.Chat, s_game);
            }
        }
    }
}