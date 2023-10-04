using Film_bot.Broker;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var botClient = new TelegramBotClient("6573404549:AAEpIzNU6T9bDXxkxnNeKbSfZqX9kMEowtg");

        using CancellationTokenSource cts = new();

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        cts.Cancel();

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => HandleMessageAsync(botClient,update,cancellationToken),
                UpdateType.CallbackQuery => HandleCallBackQueryAsync(botClient,update,cancellationToken),
                _ => throw new Exception("Unknown update type")
            };

            try
            {
                await handler;
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }

    private static async Task HandleCallBackQueryAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private static async Task HandleMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {

        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        await SendFilmList(botClient,update, cancellationToken);
    }

    private static async Task SendFilmList(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        var searchReasult = await ApiBroker.GetSearchResult(messageText, 1);

        var textOfSearchResult = "";

        int count = 1;
        foreach(var item in searchReasult.Search)
        {
            textOfSearchResult += $"\n{count++}.{item.Title} -{item.Year}"; 
        }

        List<List<InlineKeyboardButton>> inlineKeyboardButtons = new List<List<InlineKeyboardButton>>();

        if(searchReasult.Search.Count <= 5)
        {
            List<InlineKeyboardButton> list = new List<InlineKeyboardButton>();
            for(int i = 0;i < searchReasult.Search.Count;i++)
            {
                InlineKeyboardButton inlineKeyboard = InlineKeyboardButton.WithCallbackData( $"{i+1}", searchReasult.Search[i].imdbID);
                list.Add(inlineKeyboard);
            }
            inlineKeyboardButtons.Add(list);
        }
        else



        {
            List<InlineKeyboardButton> list = new List<InlineKeyboardButton>();
            for (int i = 0; i < 5; i++)
            {
                InlineKeyboardButton inlineKeyboard = InlineKeyboardButton.WithCallbackData($"{i + 1}", searchReasult.Search[i].imdbID);
                list.Add(inlineKeyboard);
            }
            inlineKeyboardButtons.Add(list);
            list = new List<InlineKeyboardButton>();
            for (int i = 5; i < searchReasult.Search.Count - 5; i++)
            {
                InlineKeyboardButton inlineKeyboard = InlineKeyboardButton.WithCallbackData($"{i + 1}", searchReasult.Search[i].imdbID);
                list.Add(inlineKeyboard);
            }
            inlineKeyboardButtons.Add(list);
        }

        InlineKeyboardMarkup reply = new InlineKeyboardMarkup(inlineKeyboardButtons);

        await botClient.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: textOfSearchResult,
            replyMarkup: reply,
            cancellationToken:cancellationToken
            );
    }
}