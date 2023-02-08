using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TGBotApi;
using TGBotApi.Models;
using TGBotApi.Models.DB;
using File = System.IO.File;

public class Program
{
    private static Bot Bot { get; set; }

    private static async Task Main()
    {
       
        try
        {
            Directory.CreateDirectory(Constants.ChatsDirName);
        }
        catch (Exception ex)
        {
        }

        if (File.Exists(Constants.ConfigFile))
        {
            try
            {
                var _BotConfigure = JsonSerializer.Deserialize<BotConfigure>(File.ReadAllText(Constants.ConfigFile));
                Console.WriteLine("Файл конфигурации считан");
                Bot = new Bot(_BotConfigure);
            }
            catch (Exception ex)
            {
                Bot = new Bot();
            }
        }
        else
        {
            Bot = new Bot();
        }


        if (await Bot.Start())
        {
            Bot.TelegramReciveEvent += TelegramReciveEvent;
            Bot.TelegramErrorEvent += TelegramErrorEvent;
        }

        var runned = true;
        while (runned)
        {
            Console.WriteLine("\nMenu");
            Console.WriteLine("/start - Запустить бота");
            Console.WriteLine("/stop - Остановить бота");
            Console.WriteLine("/config - перенастроить бота");
            Console.WriteLine("/chats - Управление чатами");
            Console.WriteLine("/sendmessage - Произвести рассылку");
            Console.WriteLine("/exit - Выход");

            switch ( Extensions.ReadLine("\nВведите команду >"))
            {
                case "/start":
                {
                    if (await Bot.Start())
                    {
                        Bot.TelegramReciveEvent += TelegramReciveEvent;
                        Bot.TelegramErrorEvent += TelegramErrorEvent;
                    }

                    break;
                }
                case "/stop":
                {
                    await Bot.Stop();
                    break;
                }
                case "/config":
                {
                    await Bot.Start(await Bot.ConfigureBot());
                    break;
                }
                case "/sendmessage":
                {
                    var messagetext = Extensions.ReadMessageData();
                    if (messagetext.Length == 0)
                    {
                        Console.WriteLine("Сообщение пусто");
                        break;
                    }

                    Console.WriteLine("1 - Отправить сообщение всем пользователям");
                    Console.WriteLine("2 - Отправить сообщение пользователям из чата");

                    if (int.TryParse(Console.ReadLine(), out var selectedMenu))
                        switch (selectedMenu)
                        {
                            case 1:
                            {
                                SendAllMessage(messagetext);
                                break;
                            }
                            case 2:
                            {
                                SendMessageToChats(messagetext);
                                break;
                            }
                        }

                    break;
                }

                case "/chats":
                {
                    Console.WriteLine("0 - Выход");
                    Console.WriteLine("1 - Просмотреть список чатов ");
                    Console.WriteLine("2 - Просмотреть информацию о чате ");
                    Console.WriteLine("3 - Изменить настройки чата");

                    var selectmenu = -1;

                    if (int.TryParse(Extensions.ReadLine("Выберите действие >"), out selectmenu))
                        switch (selectmenu)
                        {
                            case 1:
                            {
                                await SeeChatList();
                                break;
                            }
                            case 2:
                            {
                                await SeeChatInfo();
                                break;
                            }
                            case 3:
                            {
                                await ChatSettingsConfigure();
                                break;
                            }
                        }

                    break;
                }
                case "/exit":
                {
                    Bot.Dispose();
                    Environment.Exit(0);
                    break;
                }
            }
        }
    }


    

    private static async Task ChatSettingsConfigure()
    {
        await using var db = new BotDB();
        if (await db.Chats.CountAsync().ConfigureAwait(false) == 0)
        {
            Console.WriteLine("Список чатов пуст");
            return;
        }

        Console.WriteLine("Выберите чат из списка:");

        var chats = await db.Chats.ToListAsync();
        for (var i = 0; i < chats.Count; i++)
        {
            var chat = chats[i];
            Console.WriteLine($"{i + 1}:{chat.Title} (id: {chat.Id})");
        }

        if (int.TryParse( Extensions.ReadLine("Введите номер чата >"), out var chatNumber))
        {
            var chat = chats.ElementAtOrDefault(chatNumber - 1);
            if (chat == null)
            {
                Console.WriteLine("Ошибка выбора чата, введите правильный номер");
                return;
            }

            await SeeChatInfo(chat);
            bool menu = true;
            while (menu)
            {
                Console.WriteLine("Действия");
                Console.WriteLine("0 - выйти");
                Console.WriteLine($"1 - {(chat.SendChatEnable ? "Выключить" : "включить")} отправку сообщения в канал");
                Console.WriteLine($"2 - {(chat.SendUserEnable ? "Выключить" : "включить")} отправку сообщения пользователю");

                Console.WriteLine("3 - Изменить сообщение для пользователя");
                Console.WriteLine("4 - Изменить сообщение для чата");

                if (int.TryParse( Extensions.ReadLine("Выберите действие >"), out var selectedAction))
                {
                    switch (selectedAction)
                    {
                        case 0:
                        {
                            menu = false;
                            break;
                        }
                        case 1:
                        {
                            Console.WriteLine(
                                $"Отправка сообщений в чат: {((chat.SendChatEnable = !chat.SendChatEnable) ? "Включена" : "Выключена")}");
                            break;
                        }
                        case 2:
                        {
                            Console.WriteLine(
                                $"Отправка сообщений пользователю: {((chat.SendUserEnable = !chat.SendUserEnable) ? "Включена" : "Выключена")}");
                            break;
                        }
                        case 3:
                        {
                            string? message = Extensions.ReadMessageData();
                            if (!string.IsNullOrEmpty(message))
                            {
                                chat.UserMessage = message;
                                Console.WriteLine("Сообщение для пользователей изменено");
                            }

                            break;

                        }
                        case 4:
                        {
                            string? message = Extensions.ReadMessageData();
                            if (!string.IsNullOrEmpty(message))
                            {
                                chat.ChatMessage = message;
                                Console.WriteLine("Сообщение для чата изменено");
                            }

                            break;

                        }
                    }
                   db.Chats.Update(chat);
                   await db.SaveChangesAsync();
                }
                else
                    Console.WriteLine("Ошибка выобора, введите число");
            }
        }
        else
        {
            Console.WriteLine("Ошибка выобора чата, введите число");
        }
        
    }

    private static async Task SendAllMessage(string message)
    {
        try
        {
            await using var db = new BotDB();
            if (await db.Chats.CountAsync().ConfigureAwait(false) == 0)
            {
                Console.WriteLine("Список чатов пуст");
                return;
            }

            var chats = await db.Chats.Include(x => x.Users).ToListAsync();
            Console.WriteLine("Начинаем рассылку всем пользователям");
            await SendMessageToChats(message, chats);
        }
        catch (Exception e)
        {
            Console.WriteLine("Произошла ошибка во время отправки сообщений:" + e.Message);
        }
    }

    private static async Task SendMessageToChats(string message)
    {
        await using var db = new BotDB();
        if (await db.Chats.CountAsync().ConfigureAwait(false) == 0)
        {
            Console.WriteLine("Список чатов пуст");
            return;
        }

        Console.WriteLine("Выберите чаты из списка:");

        var chats = await db.Chats.Include(x => x.Users).ToListAsync();
        for (var i = 0; i < chats.Count; i++)
        {
            var chat = chats[i];
            Console.WriteLine($"{i + 1}:{chat.Title} (id: {chat.Id})");
        }

        while (true)
            try
            {
                chats = Extensions.ReadLine("Введите номера чатов через запятую >")
                    .Split(",")
                    .Select(x => chats.ElementAt(int.Parse(x) - 1))
                    .ToList();
                await SendMessageToChats(message, chats);
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошла ошибка во время ввода чатов или отправки сообщений: " + ex.Message);

                int.TryParse( Extensions.ReadLine("Повторить попытку? 1 - да 0 - нет >"), out var res);
                if (res == 0) break;
            }
    }

    private static async Task SendMessageToChats(string message, IEnumerable<DBChat> chats)
    {
        foreach (var chat in chats)
        {
            Console.WriteLine("Отправка сообщений в чат: " + chat.Title);
            foreach (var user in chat.Users)
                try
                {
                    await Bot.SendMessage(user.ToTGUser(), message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Произошла ошибка: " + ex.Message);
                }
        }
    }

    private static async Task SeeChatInfo(DBChat chat, bool showUsers = false)
    {
        Console.WriteLine($"Чат: {chat.Title} (id: {chat.Id})\n");
        Console.WriteLine("Параметры чата\n");
        Console.WriteLine("Приветственное сообщение в чат: " + (chat.SendChatEnable ? "Включено" : "Выключено"));
        Console.WriteLine("\tСообщение: " + (chat.ChatMessage.Length == 0 ? "Сообщение не задано" : chat.ChatMessage));
        Console.WriteLine("Приветственное сообщение пользователю: " + (chat.SendUserEnable ? "Включено" : "Выключено"));
        Console.WriteLine("\tСообщение: " + (chat.UserMessage.Length == 0 ? "Сообщение не задано" : chat.UserMessage));

        Console.WriteLine($"\n Сохранённые пользователи чата: {chat.Users.Count}\n");

        if (showUsers)
        {
            if (chat.Users.Count == 0)
                Console.WriteLine("Список пользователей пуст");
            else
                foreach (var user in chat.Users)
                    Console.WriteLine($"{user.FirstName} {user.LastName} (id: {user.Id} userName: {user.UserName})");
        }
    }

    private static async Task SeeChatInfo()
    {
        await using var db = new BotDB();
        if (await db.Chats.CountAsync().ConfigureAwait(false) == 0)
        {
            Console.WriteLine("Список чатов пуст");
            return;
        }

        Console.WriteLine("Выберите чат из списка:");

        var chats = await db.Chats.Include(x => x.Users).ToListAsync();
        for (var i = 0; i < chats.Count; i++)
        {
            var chat = chats[i];
            Console.WriteLine($"{i + 1}:{chat.Title} (id: {chat.Id})");
        }

        if (int.TryParse( Extensions.ReadLine("Введите номер чата >"), out var chatSelectIndex))
        {
            var chat = chats.ElementAtOrDefault(chatSelectIndex - 1);
            if (chat == null)
            {
                Console.WriteLine("Ошибка выбора чата, введите правильный номер");
                return;
            }

            await SeeChatInfo(chat, true);
        }
    }

    private static async Task SeeChatList()
    {
        await using var db = new BotDB();
        if (await db.Chats.CountAsync().ConfigureAwait(false) == 0)
        {
            Console.WriteLine("Список чатов пуст");
            return;
        }

        Console.WriteLine("Чаты:\n");
        await db.Chats.ForEachAsync(x => Console.WriteLine($"{x.Title} (id: {x.Id})"));
    }

    private static async Task TelegramErrorEvent(ITelegramBotClient botclient, Exception exception,
        CancellationToken cancellationtoken)
    {
        var ErrorMessage = string.Empty;
        if (exception is ApiRequestException apiRequestException)
        {
            ErrorMessage = $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}";
            if (apiRequestException.ErrorCode == 409)
                Console.WriteLine($"Закройте другие экземпляры бота: {Bot.meInfo.FirstName} (@{Bot.meInfo.Username}) ");
        }
        else
        {
            ErrorMessage = exception.Message;
        }

        Console.WriteLine(ErrorMessage);
        Console.WriteLine("Если меню не появилось автоматически, нажмите Enter");
    }

    private static async Task TelegramReciveEvent(ITelegramBotClient client, Update update, CancellationToken cts)
    {
        Chat? channel = null;

        User? user = null; 

        switch (update.Type)
        {
            case UpdateType.Message:
            {
                channel = update.Message.Chat;
                user = update.Message.From;
                break;
            }
            case UpdateType.ChatJoinRequest:
            {
                channel = update.ChatJoinRequest.Chat;
                user = update.ChatJoinRequest.From;
                break;
            }
        }

        if (channel != null || user != null)
        {
            DBChat? findchat = null;
            DBUser? findUser = null;

            await using (var dbcontext = new BotDB())
            {
                if (channel is not null)
                    findchat = dbcontext.Chats
                        .Include(x => x.Users)
                        .FirstOrDefault(dbchat => dbchat.Id == channel.Id);

                if (findchat is null)
                {
                    findchat = new DBChat()
                    {
                        Id = channel.Id,
                        Title = channel.Title,
                        SendChatEnable = true,
                        SendUserEnable = true,

                    };
                    await dbcontext.Chats.AddAsync(findchat);

                }

                if (user is not null)
                    findUser = dbcontext.Users.FirstOrDefault(x => x.Id == user.Id);

                if (findUser is null)
                {
                    findUser = new DBUser(user.Id, user.FirstName, user.LastName, user.Username);
                    await dbcontext.Users.AddAsync(findUser);
                }

                if (findchat.Users.FindIndex(x => x.Id == findUser.Id) == -1)
                    findchat.Users.Add(findUser);

                await dbcontext.SaveChangesAsync();
            }


            if (update.Type == UpdateType.ChatJoinRequest)
            {

                async  Task<bool> SendMessageToUser()
                {
                    if (findchat.SendUserEnable)
                    {
                        string message = string.Empty;
                        if (string.IsNullOrEmpty(findchat.UserMessage) && (Bot.Configure.DefaultUserMessageEnable))
                            message = Bot.Configure.DefaultUserMessage;

                        else message = findchat.UserMessage; 
                    
                      return  await Bot.SendMessage(user, message);
                    }

                    return false;
                }
                bool isSended = await SendMessageToUser();
                
                await Bot.Approve(channel, user);
                
                if(isSended == false) 
                    await SendMessageToUser();
                
                if (findchat.SendChatEnable)
                {
                    string message = string.Empty;
                    if (!string.IsNullOrEmpty(user?.Username))
                        message = $"@{user.Username} ";
                            
                    if (string.IsNullOrEmpty(findchat.ChatMessage) && (Bot.Configure.DefaultChatMessageEnable))
                        message += Bot.Configure.DefaultChatMessage;

                    else
                        message += findchat.ChatMessage;
                    
                    await Bot.SendMessage(channel,message);
                }
            }
        }
    }


}