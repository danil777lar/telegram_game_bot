using System;
using System.Net.NetworkInformation;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    public static class Program
    {
        private static string s_token = "";
        private static string s_game = "";
        
        private static UpdateType[] s_allowedUpdates =
        {
            UpdateType.Message, 
            UpdateType.PreCheckoutQuery
        };

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
            
            ReceiverOptions? receiverOptions = new ReceiverOptions { AllowedUpdates = s_allowedUpdates };
            s_bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, s_cts.Token);
            
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
        
        private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Handling update {update.Type.ToString()}");
            
            switch (update.Type)
            {
                case UpdateType.Message:
                    await OnMessage(update.Message, update.Type);
                    break;
                case UpdateType.PreCheckoutQuery:
                    await OnPreCheckoutQuery(update.PreCheckoutQuery, cancellationToken);
                    break;
            }
        }
        
        private static async Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception);
            await Task.Delay(2000, s_cts.Token);
        }

        private static async Task OnPreCheckoutQuery(PreCheckoutQuery query, CancellationToken cancellationToken)
        {
            await s_bot.SendMessage(
                chatId: query.From.Id,
                text: "Get pre checkout query");
                
            bool ok = true;
            try
            {
                string? errorMessage = ok ? null : "Error in payment data";
                    
                await s_bot.SendMessage(
                    chatId: query.From.Id,
                    text: "Answer pre checkout query");
                    
                await s_bot.AnswerPreCheckoutQuery(query.Id, errorMessage, cancellationToken);
            }
            catch (Exception ex) { }
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

                await OnCommandMessage(command, text[space..].TrimStart(), msg);
            }
            else
            {
                await OnTextMessage(msg);
            }
        }

        private static async Task OnTextMessage(Message msg)
        {
            Console.WriteLine($"Received text '{msg.Text}' in {msg.Chat}");
            await OnCommandMessage("/start", "", msg);
        }
        
        private static async Task OnCommandMessage(string command, string args, Message msg)
        {
            if (command == "/start")
            {
                await SendGameMessage(msg);
            }
        }

        private static async Task SendGameMessage(Message msg)
        {
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithWebApp("Play!", s_game)
            });

            await s_bot.SendMessage(
                chatId: msg.Chat.Id,
                text: "Hi! Press the button below to play the game.",
                replyMarkup: inlineKeyboard
            );
        }
    }
}