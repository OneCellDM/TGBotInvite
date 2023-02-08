namespace TGBotApi.Models;

public class ChatData
{
    public long chatId { get; set; }
    public string Title { get; set; }
    public  List<(long id, string name)> Users { get; set; }
}