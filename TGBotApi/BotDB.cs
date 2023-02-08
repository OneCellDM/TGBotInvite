using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using TGBotApi.Models.DB;
using File = System.IO.File;

namespace TGBotApi;

public class BotDB:DbContext
{
    public const string DataBaseName = "data.db";
    public DbSet<DBUser> Users {get; set; } = null!;
    public DbSet<DBChat> Chats { get; set; } = null!;

    public BotDB():base()
    {
        if (File.Exists(DataBaseName) == false)
            this.Database.EnsureCreated();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source = {DataBaseName}");
    }
    

   
   
}