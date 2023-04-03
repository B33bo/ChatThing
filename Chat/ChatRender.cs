using System.Text;

namespace Chat;

public static class ChatRender
{
    private static List<Message> messages = new List<Message>();
    private static bool isRefreshing;

    public static void AddMessage(Message message)
    {
        messages.Add(message);
        Refresh();
    }

    public static void Refresh()
    {
        isRefreshing = true;
        int currentLeft = Console.CursorLeft;
        var messagesToLoad = MathF.Min(Console.WindowHeight - 5, messages.Count);

        Console.SetCursorPosition(0, 2);

        for (int i = 1; i <= messagesToLoad; i++)
        {
            Message message = messages[^i];

            string date = message.DateTime.ToString("yyyy/MM/dd HH:mm:ss");
            string messageText = $"[{date}] {message.Sender}: {message.GetContentText()}";
            string colorInfo = $"{ConsoleColorManager.START_COLOR}[{ConsoleColorManager.FOREGROUND};2;{message.Color}m";

            if (ConsoleColorManager.ColorsAllowed)
                Console.WriteLine((colorInfo + messageText + ConsoleColorManager.END_COLOR).PadRight(Console.WindowWidth, ' '));
            else
                Console.WriteLine(messageText.PadRight(Console.WindowWidth, ' '));
        }

        Console.SetCursorPosition(currentLeft, 0);
        isRefreshing = false;
    }

    public static string GetInput()
    {
        while (isRefreshing) { }

        Console.SetCursorPosition(0, 0);
        string message = Console.ReadLine();
        Console.SetCursorPosition(0, 0);
        Console.Write("".PadRight(message.Length, ' '));

        if (message is null)
            return string.Empty;
        return message;
    }

    public static string OutputString()
    {
        StringBuilder stringBuilder = new();

        for (int i = messages.Count - 1; i >= 0; i--)
            stringBuilder.AppendLine($"[{messages[i].DateTime.ToString("yyyy/MM/dd HH:mm:ss")}] {messages[i].Sender}: {messages[i].Content}");

        return stringBuilder.ToString();
    }
}
