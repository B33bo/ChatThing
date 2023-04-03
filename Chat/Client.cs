using System.Net.Sockets;
using System.Text;

namespace Chat;

public static class Client
{
    public static string Color;
    public static string Username = "Server";
    public static int Points = 100;

    public static void StartSending()
    {
        Color = ConsoleColorManager.GetColor(Random.Shared.Next(0, 360));
        CommandManager.LoadCommands();

        Message.TCPClient = new TcpClient(Message.IP, Server.Port);
        Message.NetworkStream = Message.TCPClient.GetStream();

        byte[] data = Encoding.ASCII.GetBytes(Username + "|" + Message.Version);
        Message.NetworkStream.Write(data, 0, data.Length);

        new Thread(SendLoop).Start();
        Console.WriteLine("Worked (s)");
        Console.Clear();

        while (true)
        {
            byte[] x = new byte[Message.MaxMessageSize];
            Message.NetworkStream.Read(x, 0, Message.MaxMessageSize);

            Message message = Message.Parse(Encoding.ASCII.GetString(x));

            switch (message.Type)
            {
                case Message.MessageType.UsernameChange:
                    Client.Username = message.Content;
                    Console.Title = message.Content;
                    break;
                case Message.MessageType.ChangeMoney:
                    if (int.TryParse(message.Content, out int newMoney))
                        Points += newMoney;
                    else
                    {
                        var args = message.Content.Split(',');
                        if (args.Length == 2)
                        {
                            if (!int.TryParse(args[1], out int newMoney2))
                                break;
                            Points += newMoney2;
                            ChatRender.AddMessage(new Message("Money", $"{message.Sender} gave you " + newMoney));
                        }
                    }
                    break;
                default:
                    ChatRender.AddMessage(message);
                    break;
            }
        }
    }

    private static void SendLoop()
    {
        while (true)
        {
            var message = ChatRender.GetInput();

            if (message.StartsWith("!"))
            {
                var args = message[1..].Split('|');

                new Message(Client.Username, message.Replace("|", "&pipe;"))
                {
                    Type = Message.MessageType.CommandDone,
                    IsPrivate = true,
                }.Send();

                if (CommandManager.DoCommand(args))
                    continue;
            }

            var newMessage = new Message(Username, message)
            {
                Color = Color,
            };
            newMessage.Send();
        }
    }
}
