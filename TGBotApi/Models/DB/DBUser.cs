using Telegram.Bot.Types;

namespace TGBotApi.Models.DB;

public class DBUser
{
    public  long Id { get; set; }
    public  string? FirstName { get; set; }
    public  string? LastName { get; set; }
    public  string? UserName { get; set; }

    public List<DBChat> Chats { get; set; } = new List<DBChat>();

    public DBUser(long id,  string firstName, string lastName, string userName)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        UserName = userName;
    }

    public DBUser()
    {
    }

    public DBUser(long id,  string firstName, string lastName)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
    }

    public User? ToTGUser() => new User() { Id = Id, Username = UserName, FirstName = FirstName, LastName = LastName};
}