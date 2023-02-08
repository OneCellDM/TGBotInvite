using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace TGBotApi.Models.DB;

public class DBChat
{
    public  long Id { get; set; }
    public  string? Title { get; set; }

    
    public  string? ChatMessage { get; set; }
    public  string? UserMessage { get; set; }

    public bool SendUserEnable { get; set; } = false;
    public bool SendChatEnable { get; set; } = false;
    
    
    public List<DBUser> Users { get; set; } = new List<DBUser>();

    public DBChat()
    {
        
    }
    

    public DBChat(long id, string title)
    {
        Id = id;
        Title = title;
    }

    public  Chat ToTGObject() =>  new Chat() { Id = Id, Title = Title };
}