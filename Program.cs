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

        private static CancellationTokenSource s_cts;
        private static TelegramBotClient s_bot;
        private static User s_me;

        static async Task Main(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith("--token="))
                {
                    s_token = arg.Substring("--token=".Length);
                }
            }

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
            s_bot.OnUpdate += OnUpdate;
            
            Thread.Sleep(-1);
        }

        static async Task OnError(Exception exception, HandleErrorSource source)
        {
            Console.WriteLine(exception);
            await Task.Delay(2000, s_cts.Token);
        }

        static async Task OnMessage(Message msg, UpdateType type)
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
                        return; // command was not targeted at me
                    }
                }

                await OnCommand(command, text[space..].TrimStart(), msg);
            }
            else
            {
                await OnTextMessage(msg);
            }
        }

        static async Task OnTextMessage(Message msg) // received a text message that is not a command
        {
            Console.WriteLine($"Received text '{msg.Text}' in {msg.Chat}");
            await OnCommand("/start", "", msg); // for now we redirect to command /start
        }

        static async Task OnCommand(string command, string args, Message msg)
        {
            Console.WriteLine($"Received command: {command} {args}");
            switch (command)
            {
                case "/start":
                    await s_bot.SendMessage(msg.Chat, """
                                                    <b><u>Bot menu</u></b>:
                                                    /photo [url]    - send a photo <i>(optionally from an <a href="https://picsum.photos/310/200.jpg">url</a>)</i>
                                                    /inline_buttons - send inline buttons
                                                    /keyboard       - send keyboard buttons
                                                    /remove         - remove keyboard buttons
                                                    /poll           - send a poll
                                                    /reaction       - send a reaction
                                                    """, parseMode: ParseMode.Html, linkPreviewOptions: true,
                        replyMarkup: new ReplyKeyboardRemove()); // also remove keyboard to clean-up things
                    break;
                case "/photo":
                    if (args.StartsWith("http"))
                        await s_bot.SendPhoto(msg.Chat, args, caption: "Source: " + args);
                    else
                    {
                        await s_bot.SendChatAction(msg.Chat, ChatAction.UploadPhoto);
                        await Task.Delay(2000); // simulate a long task
                        await using var fileStream = new FileStream("bot.gif", FileMode.Open, FileAccess.Read);
                        await s_bot.SendPhoto(msg.Chat, fileStream, caption: "Read https://telegrambots.github.io/book/");
                    }

                    break;
                case "/inline_buttons":
                    InlineKeyboardMarkup inlineMarkup = new InlineKeyboardMarkup()
                        .AddNewRow("1.1", "1.2", "1.3")
                        .AddNewRow()
                        .AddButton("WithCallbackData", "CallbackData")
                        .AddButton(InlineKeyboardButton.WithUrl("WithUrl",
                            "https://github.com/TelegramBots/Telegram.Bot"));
                    await s_bot.SendMessage(msg.Chat, "Inline buttons:", replyMarkup: inlineMarkup);
                    break;
                case "/keyboard":
                    ReplyKeyboardMarkup replyMarkup = new ReplyKeyboardMarkup()
                        .AddNewRow("1.1", "1.2", "1.3")
                        .AddNewRow().AddButton("2.1").AddButton("2.2");
                    await s_bot.SendMessage(msg.Chat, "Keyboard buttons:", replyMarkup: replyMarkup);
                    break;
                case "/remove":
                    await s_bot.SendMessage(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
                    break;
                case "/poll":
                    await s_bot.SendPoll(msg.Chat, "Question", ["Option 0", "Option 1", "Option 2"], isAnonymous: false,
                        allowsMultipleAnswers: true);
                    break;
                case "/reaction":
                    await s_bot.SetMessageReaction(msg.Chat, msg.Id, ["❤"], false);
                    break;
            }
        }

        static async Task OnUpdate(Update update)
        {
            switch (update)
            {
                case { CallbackQuery: { } callbackQuery }: await OnCallbackQuery(callbackQuery); break;
                case { PollAnswer: { } pollAnswer }: await OnPollAnswer(pollAnswer); break;
                default: Console.WriteLine($"Received unhandled update {update.Type}"); break;
            }
        }

        static async Task OnCallbackQuery(CallbackQuery callbackQuery)
        {
            await s_bot.AnswerCallbackQuery(callbackQuery.Id, $"You selected {callbackQuery.Data}");
            await s_bot.SendMessage(callbackQuery.Message!.Chat,
                $"Received callback from inline button {callbackQuery.Data}");
        }

        static async Task OnPollAnswer(PollAnswer pollAnswer)
        {
            if (pollAnswer.User != null)
            {
                await s_bot.SendMessage(pollAnswer.User.Id,
                    $"You voted for option(s) id [{string.Join(',', pollAnswer.OptionIds)}]");
            }
        }
    }
}