namespace TGBotApi;

public static class Extensions
{
    public static string ReadLine(string message = "")
    {
        Console.WriteLine(message);
        return Console.ReadLine();
    }

    public static string InputStr(string message)
    {
        var token = ReadLine(message);
        if (string.IsNullOrEmpty(token.Trim()))
        {
            Console.WriteLine("Строка не должна быть пустой");
            return InputStr(message);
        }

        return token;
    }

    public static int IntInput(string message)
    {
        var result = 0;
        var str = ReadLine(message);
        return int.TryParse(str, out result) ? result : IntInput(message);
    }

    public static bool BoolInput(string message, string yesKey, string NoKey)
    {
        var str = ReadLine(message);

        if (string.Equals(str.Trim(), yesKey.Trim(), StringComparison.CurrentCultureIgnoreCase)) return true;
        if (string.Equals(str.Trim(), NoKey.Trim(), StringComparison.CurrentCultureIgnoreCase)) return false;

        Console.WriteLine($"Ошибка ввода! Введите {yesKey} или {NoKey}");

        return BoolInput(message, yesKey, NoKey);
    }

    public static string? ReadMessageData(string? message = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Console.WriteLine(message+"\n");
        }
        
        var messagetext = string.Empty;
        Console.WriteLine("1 - Загрузить текст из файла");
        Console.WriteLine("2 - Ввести текст");
        Console.WriteLine("0 - выход");

        switch (IntInput("Выберите метод >"))
        {
            case 1:
            {
                Console.Write("Введите путь к файлу>");
                try
                {
                    messagetext = File.ReadAllText(Console.ReadLine());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка чтения файла: " + ex.Message);
                }

                break;
            }
            case 2:
            {
                Console.WriteLine("Введите текст > ");
                messagetext = Console.ReadLine();
                break;
            }
            case 0:
                return messagetext;
        }

        return messagetext;
    }
}