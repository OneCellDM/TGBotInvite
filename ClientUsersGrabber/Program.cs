
using System;
using System.Diagnostics;
using System.Threading.Channels;

using TL;

using WTelegram;

namespace ClientUsersGrabber
{
   
    internal class Program
    {
        static int app_id =7789789;
        static string app_hash = "";
        static string phoneNumber = "";
        static WTelegram.Client client;
        static async Task Main(string[] args)
        {
            client = new WTelegram.Client(app_id,app_hash); 
            await DoLogin(phoneNumber);
            var chats = await client.Messages_GetAllChats();

            var channels = chats.chats.Values.OfType<TL.Channel>().ToList();

            StreamWriter streamWriter=null;
            foreach (var chaneel in channels)
            {

                if (chaneel.Title != "Чат общения")
                    continue;
                try
                {
                    if (chaneel.IsGroup)
                    {
                        var stream = File.OpenWrite(chaneel.ID+"2.txt");
                        streamWriter = new StreamWriter(stream);

                        var participantsData = await client.Channels_GetAllParticipants(chaneel);
                        streamWriter.WriteLine(chaneel.ID+";"+chaneel.Title);
                        foreach (var item in participantsData.users.Values)
                        {
                            streamWriter.WriteLine($"USER:{item.first_name} {item.last_name} USER_ID:{item.ID} USERNAME: @{item.username}");
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            Console.WriteLine("Работа завершена");
            streamWriter.Close();
            


        }

        static async Task DoLogin(string loginInfo) // (add this method to your code)
        {
            while (client.User == null)
                switch (await client.Login(loginInfo)) // returns which config is needed to continue login
                {
                    case "verification_code": Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
                    default: loginInfo = null; break;
                }
            Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");




        }
    }
}