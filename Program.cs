using Telegram.Bot.Polling;
using Telegram.Bot;
using static Telegram.Bot.TelegramBotClient;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

class Player
{
    public enum ProfileStatus { Unregistered, AwaitingConfirmation, InGame };

    public long ChatId = -1;
    public string Name = "Unknown_Player";
    public ProfileStatus status = ProfileStatus.Unregistered;
}

class Program
{
    private static ITelegramBotClient _botClient;
    private static ReceiverOptions _receiverOptions;
    private static string _token = "7824999294:AAEq3Bsn6x_zj5Zp2jiJfY2JBt3cHNHOD8E";

    private static string adminPassword = "DavilkusIsSus";
    private static long? adminChatId = null;
    private static List<Player> players = new List<Player>();

    private static async Task HandleMessage(Message message, User sender, Chat chat)
    {
        if (chat.Id != adminChatId)
        {
            Player player = GetPlayerFromChatId(chat.Id);
            if (player == null)
            {
                player = new Player();
                player.ChatId = chat.Id;
                players.Add(player);
            }

            if (player.status == Player.ProfileStatus.InGame)
            {
                await _botClient.SendMessage(chat.Id, "Вы в игре");
            }
            else if (player.status == Player.ProfileStatus.Unregistered)
            {
                if (message.Text.ToLower() == "/start")
                {
                    await _botClient.SendMessage(chat.Id, "Приветствую. Добро пожаловать в \"Киллера\"!\n\n" +
                    "Пожалуйста, введите свое реальное имя и фамилию для участия в игре (Например: Кирилл Бутарев):");
                }
                else
                {
                    if (message.Text != adminPassword)
                    {
                        if (message.Text.Split(' ').Length != 2)
                        {
                            await _botClient.SendMessage(chat.Id, "Неверный формат имени. Попробуйте еще раз:");
                        }
                        else
                        {
                            await _botClient.SendMessage(chat.Id, "🕒 Ваша заявка на участие отправлена администратору");
                            player.Name = message.Text;
                            player.status = Player.ProfileStatus.AwaitingConfirmation;

                            if (adminChatId.HasValue)
                            {
                                var acceptRejectKeyboard = new InlineKeyboardMarkup(new[]
                                {
                            InlineKeyboardButton.WithCallbackData("Accept", "accept:" + chat.Id),
                            InlineKeyboardButton.WithCallbackData("Reject", "reject:" + chat.Id)
                        });

                                await _botClient.SendMessage(adminChatId.Value, $"Регистрация ожидает подтверждения\nИмя: " +
                                    $"{player.Name}\nID чата: {player.ChatId}",
                                    replyMarkup: acceptRejectKeyboard);
                            }
                        }
                    }
                    else
                    {
                        adminChatId = chat.Id;
                        await _botClient.SendMessage(chat.Id, "Права администратора получены.");
                    }
                }
            }
            else if (player.status == Player.ProfileStatus.AwaitingConfirmation)
            {
                await _botClient.SendMessage(chat.Id, "Пожалуйста, ожидайте подтверждения регистрации администратором.");
            }
        }
        else
        {
            await _botClient.SendMessage(chat.Id, "Вы администратор.");
        }
    }

    private static async Task HandleCallbackQuery(CallbackQuery callbackQuery, User sender, Chat chat)
    {
        if (chat.Id != adminChatId)
        {

        }
        else // ADMIN
        {
            var data = callbackQuery.Data.Split(':');
            var action = data[0];
            var playerId = long.Parse(data[1]);

            if (action == "accept")
            {
                Player player = GetPlayerFromChatId(playerId);
                player.status = Player.ProfileStatus.InGame;
                await _botClient.SendMessage(player.ChatId, "✅️ Регистрация подтверждена\nОжидайте начала игры.");
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Вы подтвердили регистрацию \"" + player.Name + "\".");
                await _botClient.EditMessageReplyMarkup(adminChatId, callbackQuery.Message.Id, new InlineKeyboardMarkup());
            }
            else if (action == "reject")
            {
                Player player = GetPlayerFromChatId(playerId);
                player.status = Player.ProfileStatus.Unregistered;
                await _botClient.SendMessage(player.ChatId, "❌ К сожалению, администратор отклонил вашу заявку на регистрацию.");
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Вы отклонили заявку на регистрацию \"" + player.Name + "\".");
            }
        }
    }

    private static Player GetPlayerFromChatId(long chatId)
    {
        Player player = null;
        for (int i = 0; i < players.Count; i++)
            if (players[i].ChatId == chatId)
                player = players[i];
        return player;
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    Message message = update.Message;
                    if (message != null)
                        await HandleMessage(message, message.From, message.Chat);
                    return;
                case UpdateType.CallbackQuery:
                    CallbackQuery callbackQuery = update.CallbackQuery;
                    if (callbackQuery != null)
                        await HandleCallbackQuery(callbackQuery, callbackQuery.From, callbackQuery.Message.Chat);
                    return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    static async Task Main()
    {
        _botClient = new TelegramBotClient(_token); 
        _receiverOptions = new ReceiverOptions 
        {
            AllowedUpdates = new[] 
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            },
            DropPendingUpdates = true
        };

        using var cts = new CancellationTokenSource();

        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token); 

        var me = await _botClient.GetMe(); 
        Console.WriteLine($"Bot {me.FirstName} has been started");

        await Task.Delay(-1); 
    }
}