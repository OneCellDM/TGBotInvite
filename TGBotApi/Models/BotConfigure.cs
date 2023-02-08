using Telegram.Bot.Types;

namespace TGBotApi.Models;

public class BotConfigure
{
    public string? BotToken { get; set; }
    public int MaxRetry { get; set; } = 3;

    public string DefaultChatMessage { get; set; } = "ПРИВЕТ!";
    public string DefaultUserMessage { get; set; } = "Приветствую в чате UST!";

    public bool DefaultUserMessageEnable { get; set; } = true;
    public bool DefaultChatMessageEnable { get; set; } = true;


}