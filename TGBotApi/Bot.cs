using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TGBotApi.Models;
using TGBotApi.Models.DB;

namespace TGBotApi;

public class Bot:IDisposable
{
    private TelegramBotClient _telegramBotClient;
    private BotConfigure _configure;
    private CancellationTokenSource cts;
    public User? meInfo;
    
    public  BotConfigure Configure
    {
        get => _configure; 
    }

    private Random randomDelay = new Random();
    public delegate Task TelegramReciveHandler(ITelegramBotClient client, Update update, CancellationToken cts);

    public delegate Task TelegramErrorHandler(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken);

    public static TelegramErrorHandler TelegramErrorEvent;
    public static TelegramReciveHandler TelegramReciveEvent;
    
    public TelegramBotClient TelegramBotClient
    {
        get => _telegramBotClient;
    }
    public Bot(BotConfigure configure)
    {
        _configure = configure;
        _telegramBotClient = new TelegramBotClient(_configure.BotToken);
    }
    
    public Bot()
    {
        
    }

    public async Task<bool> Approve(Chat? chat, User? user)
    {
        try
        {
            Console.WriteLine($"Заявка на вход от {user.FirstName} {user.LastName} (id: {user.Id})  в чат: {chat.Title}");
            
            await _telegramBotClient.ApproveChatJoinRequest(chat.Id, user.Id);
            
            Console.WriteLine($"Пользователь {user.FirstName} {user.LastName} (id: {user.Id}) принят в чат: {chat.Title}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Произошла ошибка принятия пользователя {user.FirstName} {user.LastName} (id: {user.Id}) в чат: {chat.Title}");
            Console.WriteLine(ex.Message);
            return false;
        }
    }
   
    /// <summary>
    /// Отправляет сообщение пользователю
    /// </summary>
    /// <param name="user"></param>
    /// <param name="message"></param>
    public async Task<bool> SendMessage(User? user, string message) => 
        await SendMessage(user.Id, $"{user.FirstName} {user.LastName}", message);
    
    /// <summary>
    /// Отправляет сообщение в канал
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<bool> SendMessage(Chat? channel,  string message) =>  
        await SendMessage(channel.Id, $"{channel.Title}", message);
    
    /// <summary>
    /// Отправляет сообщение
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="retry"></param>
    /// <returns></returns>
    public async Task<bool> SendMessage(long channelId,  string title, string message,  int retry = 1)
    {
        try
        {
            Console.WriteLine($"Отправка сообщения {title}" );
            Console.WriteLine($"Попытка {retry} из {_configure.MaxRetry}");
            await _telegramBotClient.SendTextMessageAsync(channelId, message);
            Console.WriteLine($"Сообщение успешно отправлено : {title}");
            return true;
        }
        catch(Exception ex)
        {
            if (retry >= _configure.MaxRetry)
            {
                Console.WriteLine($"Попытки отправки сообщения: {title} кончились");
                return false;

            }
            
            Console.WriteLine("Проблема при отправке сообщения: " + ex.Message);
            int delay = randomDelay.Next(2, 5);
            Console.WriteLine($"Повтор отправки через {delay}  сек");
            await Task.Delay(TimeSpan.FromSeconds(delay));
            return await SendMessage(channelId, title,  message, retry + 1);
            

        }

    }
    public async Task Stop()
    {

        if (_telegramBotClient != null)
        {
            cts?.Cancel();
            try
            {
                await _telegramBotClient?.CloseAsync();
            }
            catch (Exception){}
            finally
            {
                _telegramBotClient = null;
            }
        }
        Console.WriteLine("Бот остановлен");
        
    }
    
    public async Task<bool> Start(BotConfigure configure)
    {
        this._configure = configure;
        return await Start();
    }
    public async Task<bool> Start()
    {
        await Stop();
        if(string.IsNullOrEmpty(_configure?.BotToken))
        {
            Console.WriteLine("Бот не настроен, настройте бота");
            var configure = await ConfigureBot();
            _configure = configure;
            Console.WriteLine("Параметры применены");
          
            return await Start();
            
        }

        _telegramBotClient = new TelegramBotClient(_configure.BotToken);
         
        Console.WriteLine("Запуск бота");
        cts = new CancellationTokenSource();
        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };
        try
        {
            meInfo = await _telegramBotClient.GetMeAsync();
            _telegramBotClient.StartReceiving(
                updateHandler: UpdateHandler,
                pollingErrorHandler: PollingErrorHandler,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );
            System.IO.File.WriteAllText(Constants.ConfigFile, JsonSerializer.Serialize(_configure));
            Console.WriteLine($"Бот ({meInfo.FirstName}) @{meInfo.Username} запущен");
            return true;
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException initEx)
        {
            if (initEx.ErrorCode == 404)
            {
                Console.WriteLine("Ошибка запуска бота: не правильный токен");
                _configure.BotToken = null;
                return await Start();
            }
            else
            {
                Console.WriteLine("Ошибка запуска бота:" + initEx.Message);
                return false;
            }

        }
        catch(Exception ex)
        {
            Console.WriteLine("Произошла ошибка при запуске:" + ex +" Проверьте конфигурации бота");
            return false;
        }

      
    }
    private async Task PollingErrorHandler(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3) =>
        TelegramErrorEvent?.Invoke(arg1, arg2, arg3);


    private async Task UpdateHandler(ITelegramBotClient arg1, Update arg2, CancellationToken arg3) => TelegramReciveEvent?.Invoke(arg1, arg2, arg3);



    public async Task<BotConfigure> ConfigureBot()
    {
        BotConfigure botConfigure;
        
        if (_configure?.BotToken is null)
        {
            botConfigure = new()
            {
                BotToken = Extensions.InputStr("Введите токен:"),
                MaxRetry = Extensions.IntInput("Введите количество попыток отправки сообщений:"),
                DefaultChatMessageEnable =
                    Extensions.BoolInput("Включить отправку сообщений по умолчанию, в канал  Y - да, N - нет:", "Y", "N"),
                DefaultUserMessageEnable =
                    Extensions.BoolInput("Включить отправку сообщений по умололанию, пользователю Y - да, N - нет:", "Y", "N"),
                DefaultUserMessage = Extensions.ReadMessageData("Cтандартное сообщение отправляемое пользователю:"),
                DefaultChatMessage = Extensions.ReadMessageData("Cтандартное сообщение отправляемое В чат:"),
            };
        }
        else
        {

            botConfigure = new BotConfigure()
            {
                BotToken = _configure?.BotToken,
                DefaultChatMessage = _configure?.DefaultChatMessage,
                DefaultUserMessage = _configure?.DefaultUserMessage,
                MaxRetry = _configure.MaxRetry,
                DefaultChatMessageEnable = _configure.DefaultChatMessageEnable,
                DefaultUserMessageEnable = _configure.DefaultUserMessageEnable,

            };
            
            bool run = true;
            while (run)
            {


                Console.WriteLine("0 - выйти");
                Console.WriteLine(
                    $"1 - {(botConfigure.DefaultChatMessageEnable ? "Выключить" : "включить")} отправку стандартного сообщения в канал");
                Console.WriteLine(
                    $"2 - {(botConfigure.DefaultUserMessageEnable ? "Выключить" : "включить")} отправку стандартного сообщения пользователю");

                Console.WriteLine("3 - Изменить сообщение по умолчанию для пользователя");
                Console.WriteLine("4 - Изменить сообщение по умолчанию для чата");

                int val = Extensions.IntInput("Выберите действие >");
                switch (val)
                {
                    case 0:
                    {
                        run = false;
                        break;
                    }
                    case 1:
                    {
                        Console.WriteLine(
                            $"Отправка стандартного сообщения в канал: {((botConfigure.DefaultChatMessageEnable = !botConfigure.DefaultChatMessageEnable) ? "Включена" : "Выключена")}");
                        break;
                    }
                    case 2:
                    {
                        Console.WriteLine(
                            $"Отправка стандартного сообщения пользователю: {((botConfigure.DefaultUserMessageEnable = !botConfigure.DefaultUserMessageEnable) ? "Включена" : "Выключена")}");
                        break;
                    }
                    case 3:
                    {
                        string? message = Extensions.ReadMessageData();
                        if (!string.IsNullOrEmpty(message))
                        {
                            botConfigure.DefaultUserMessage = message;
                            Console.WriteLine("Сообщение для пользователей изменено");
                        }

                        break;
                    }
                    case 4:
                    {
                        string? message = Extensions.ReadMessageData();
                        if (!string.IsNullOrEmpty(message))
                        {
                            botConfigure.DefaultChatMessage = message;
                            Console.WriteLine("Сообщение для чатов изменено");
                        }

                       
                        break;
                    }
                }
            }
        }


        
        return botConfigure;
    }


    public async void Dispose()
    {
        cts?.Dispose();
        _configure = null;
        await Stop();
        await _telegramBotClient?.CloseAsync();
        
        
    }
}