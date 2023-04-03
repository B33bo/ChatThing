using System.Net.Sockets;
using System.Text;

namespace Chat;

public class Message
{
    public const int Version = 1;

    public static Dictionary<string, string> Variables = new ()
    {
        { "amp", "&" },
        { "newline", "\n" },
        { "backslash", "\\" },
        { "pipe", "|" },
        { "randcol", "" },
        { "startcol", ConsoleColorManager.START_COLOR },
        { "endcol", ConsoleColorManager.END_COLOR },
    };
    public const int MaxMessageSize = 256;
    public static int Port { get; set; }
    public static string IP { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime DateTime { get; set; }

    public static TcpClient TCPClient { get; set; }
    public static NetworkStream NetworkStream { get; set; }

    public string Sender, Content, Color = "255;255;255";
    public MessageType Type = MessageType.Message;

    public Message(string sender, string content)
    {
        Sender = sender;
        Content = content.Replace("|", "&pipe;");
        DateTime = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{Sender}|{Content}|{(int)Type}|{Color}|{IsPrivate}|";
    }

    public string GetContentText()
    {
        foreach (var item in Variables)
        {
            if (item.Key == "randcol")
            {
                if (!Content.Contains("&randcol;"))
                    continue;
                string color = ConsoleColorManager.END_COLOR + ConsoleColorManager.START_COLOR + "[" + ConsoleColorManager.FOREGROUND + ";2;";
                color += ConsoleColorManager.GetColor(Random.Shared.Next(0, 360));
                color += "m";
                Content = Content.Replace("&randcol;", color);
                continue;
            }

            Content = Content.Replace($"&{item.Key};", item.Value);
        }
        return Content;
    }

    public static Message Parse(string s)
    {
        var args = s.Split('|');
        var message = new Message(args[0], args[1])
        {
            Type = (MessageType)int.Parse(args[2]),
            Color = args[3],
            IsPrivate = args[4].ToLower() == "true"
        };
        return message;
    }

    public void Send()
    {
        if (Server.IsServer)
        {
            string messageText = ToString();
            byte[] bytes = Encoding.ASCII.GetBytes(messageText);

            for (int i = 0; i < Server.Clients.Count; i++)
                Server.Clients[i].NetworkStream.Write(bytes);
            return;
        }
        byte[] data = Encoding.ASCII.GetBytes(ToString());
        NetworkStream.Write(data, 0, data.Length);
    }

    public void SendTo(Server.ClientData client)
    {
        string messageText = ToString();
        byte[] bytes = Encoding.ASCII.GetBytes(messageText);
        client.NetworkStream.Write(bytes);
    }

    public static void Close()
    {
        TCPClient.Close();
        NetworkStream.Close();
    }

    public enum MessageType
    {
        Message,
        DirectMessage,
        CommandDone,
        Admin,
        RunCommandOnServer,
        UsernameChange,
        InfoRequest,
        ChangeMoney,
        Rob,
    }
}
