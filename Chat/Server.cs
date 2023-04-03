using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Chat;

public static class Server
{
    public static List<string> ReservedUsernames = new()
    {
        "server",
        "money",
        "policeman"
    };
    public static int UsernameLimit { get; } = 20;
    public static List<string> BanList { get; private set; } = new();
    public const int Port = 1234;
    public static List<ClientData> Clients { get; private set; } = new();
    public static TcpListener Listener { get; private set; }
    public static bool IsServer { get; private set; } = false;
    private static string serverIp;

    public static void StartRecieving()
    {
        Console.Title = "Server";
        CommandManager.LoadCommands();
        IsServer = true;

        serverIp = GetLocalIPAddress();
        Listener = new TcpListener(IPAddress.Parse(serverIp), Port);
        Listener.Start();

        Console.WriteLine("Worked (r)");
        Console.Clear();

        ChatRender.AddMessage(new Message("Server", "Server Started"));

        new Thread(GetMessage).Start();

        while (true)
        {
            TcpClient client = Listener.AcceptTcpClient();
            new Thread(() => { UserJoined(client); }).Start();
        }
    }

    private static void GetMessage()
    {
        while (true)
        {
            var message = ChatRender.GetInput();

            if (message.StartsWith("!"))
            {
                var args = message[1..].Split('|');
                if (!CommandManager.DoCommand(args))
                {
                    ChatRender.AddMessage(new Message("Error", $"{args[0]} is not a valid command. Your message was not sent")
                    {
                        Color = "255;0;0"
                    });
                }
                continue;
            }

            var newMessage = new Message("Server", message);
            newMessage.Send();
            ChatRender.AddMessage(newMessage);
        }
    }

    private static bool IsUsernameFree(string name)
    {
        name = name.ToLower();

        for (int i = 0; i < Clients.Count; i++)
        {
            if (Clients[i].Username.ToLower() == name)
                return false;
        }
        return true;
    }

    private static bool IsUsernameValid(string name)
    {
        if (name.Length > UsernameLimit)
            return false;
        name = name.ToLower();
        if (ReservedUsernames.Contains(name))
            return false;

        for (int i = 0; i < name.Length; i++)
        {
            if (name[i] >= 'a' && name[i] <= 'z')
                // is alphabetic
                continue;
            if (name[i] >= '0' && name[i] <= '9')
                // is numeric
                continue;
            if (name[i] == '_' || name[i] == '-')
                // these are fine I guess.
                continue;
            return false;
        }
        return true;
    }

    private static void UserJoined(TcpClient tcpClient)
    {
        string ip = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();

        NetworkStream stream;

        if (BanList.Contains(ip))
            return;

        string userName;
        int userVersion;

        try
        {
            stream = tcpClient.GetStream();
            byte[] clientInfo = new byte[1024];
            int infoBytesRead = stream.Read(clientInfo, 0, clientInfo.Length);
            string[] info = Encoding.ASCII.GetString(clientInfo, 0, infoBytesRead).Split('|');
            userName = info[0];
            userVersion = int.Parse(info[1]);
        }
        catch (Exception)
        {
            ChatRender.AddMessage(new Message("Server", "Someone tried to join but he glitched"));
            return;
        }

        if (userVersion != Message.Version)
            return;

        if (!IsUsernameValid(userName))
        {
            userName = userName.Replace("\n", ""); // could get annoying
            if (userName.Length > UsernameLimit)
                userName = userName[..UsernameLimit] + "...";
            ChatRender.AddMessage(new Message("Server", $"'{userName}' is an invalid username"));
            return;
        }

        ClientData clientData = new(stream, ip, userName);

        if (!IsUsernameFree(userName))
        {
            int usernameTries = 2;
            while (!IsUsernameFree(userName + " " + usernameTries))
                usernameTries++;

            userName += " " + usernameTries;
            clientData.Username = userName;

            new Message("Server", userName)
            {
                Type = Message.MessageType.UsernameChange,
            }.SendTo(clientData);
        }

        Clients.Add(clientData);

        var newUserJoined = new Message("Server", $"{userName} joined the chat.");
        newUserJoined.Send();
        newUserJoined.Content += " IP = " + ip;
        ChatRender.AddMessage(newUserJoined);
        Listen(stream, clientData);
    }

    private static void Listen(NetworkStream stream, ClientData clientData)
    {
        while (true)
        {
            try
            {
                // Buffer to store incoming data
                byte[] data = new byte[Message.MaxMessageSize];

                // Read the incoming message from the client
                int bytesRead = stream.Read(data, 0, data.Length);
                string messageText = Encoding.ASCII.GetString(data, 0, bytesRead);
                HandleMessage(messageText, clientData, data);
            }
            catch (IOException)
            {
                KillClient(clientData);
                return;
            }
            catch (SocketException)
            {
                KillClient(clientData);
                return;
            }
            catch (Exception ex)
            {
                KillClient(clientData, $"caused an error: {ex.Message}");
                return;
            }
        }
    }

    private static void KillClient(ClientData clientData, string message = "quit")
    {
        Clients.Remove(clientData);
        var quitMSG = new Message("Server", clientData.Username + " " + message);
        ChatRender.AddMessage(quitMSG);
        quitMSG.Send();
    }

    private static void ChangeMoney(Message message, ClientData from)
    {
        ChatRender.AddMessage(new Message("Money", $"{from.Username} sent {message.Content}"));
        var moneyArgs = message.Content.Split(',');

        if (moneyArgs.Length != 2)
            return;
        if (!int.TryParse(moneyArgs[1], out int targetMoney))
            return;
        if (targetMoney < 0)
            return; // no stealing >:(
        if (from.Money < targetMoney)
            return; // no debt

        var recipient = ClientData.FromUsername(moneyArgs[0]);
        if (recipient == null)
            return;

        recipient.Money += targetMoney;
        from.Money -= targetMoney;

        message.SendTo(recipient);

        new Message("Money", (-targetMoney).ToString())
        {
            Type = Message.MessageType.ChangeMoney,
        }.SendTo(from);
    }

    private static void Rob(Message message, ClientData theif)
    {
        if (theif.InJail)
            return;
        var victim = ClientData.FromUsername(message.Content);

        if (victim == null)
        {
            new Message("Policeman", "He doesn't even exist?").SendTo(theif);
            victim.Imprison(1);
            return;
        }

        if (victim.InJail)
        {
            new Message("Policeman", "He's literally in jail cmon man :(").SendTo(theif);
            theif.Imprison(10);
            return;
        }

        if (Random.Shared.Next(0, 100) >= 10) // 10% chance
        {
            theif.Imprison(60);
            new Message("Policeman", "oi oi oi what's this? stealing? go to jail!!").SendTo(theif);
            return;
        }

        int amountStolen = victim.Money / 20;
        new Message("Money", $"You successfully robbed {message.Content}. +{amountStolen}").SendTo(theif);
        new Message("Money", $"You were robbed by {message.Sender}. -{amountStolen}").SendTo(victim);

        new Message("Money", amountStolen.ToString())
        {
            Type = Message.MessageType.ChangeMoney,
        }.SendTo(theif);

        theif.Money += amountStolen;

        new Message("Money", (-amountStolen).ToString())
        {
            Type = Message.MessageType.ChangeMoney,
        }.SendTo(victim);

        victim.Money -= amountStolen;
    }

    private static void HandleMessage(string messageText, ClientData clientData, byte[] messageData)
    {
        Message message = Message.Parse(messageText);
        message.Sender = clientData.Username;

        if (!message.IsPrivate && !clientData.IsAdmin)
        {
            if (clientData.ImprisonedAt + clientData.ImprisonedFor >= DateTime.UtcNow.Ticks)
            {
                new Message("Policeman", "You can't talk, you're in jail. Do !sentence for more info")
                {
                    Color = "255;0;0",
                }.SendTo(clientData);
                ChatRender.AddMessage(message);
                return;
            }
        }

        switch (message.Type)
        {
            case Message.MessageType.DirectMessage:
                if (clientData.InJail)
                    return;
                DirectMessage(message);
                return;
            case Message.MessageType.RunCommandOnServer:
                if (!clientData.IsAdmin)
                    return;
                CommandManager.DoCommand(message.Content.Split("&pipe;"));
                break;
            case Message.MessageType.UsernameChange:
                return; // ADMINS ONLY >:(
            case Message.MessageType.InfoRequest:
                new Message("Server", InfoRequest(message.Content, clientData)).SendTo(clientData);
                break;
            case Message.MessageType.ChangeMoney:
                ChangeMoney(message, clientData);
                return;
            case Message.MessageType.Rob:
                Rob(message, clientData);
                return;
            default:
                break;
        }

        if (message.IsPrivate)
        {
            ChatRender.AddMessage(message);
            return;
        }

        ChatRender.AddMessage(message);

        for (int i = 0; i < Clients.Count; i++)
            Clients[i].NetworkStream.Write(messageData);
    }

    private static string InfoRequest(string type, ClientData clientData)
    {
        switch (type)
        {
            default:
                return "?";
            case "list":
                return GetPlayerList(clientData.IsAdmin);
            case "sentence":
                long timeInCell = DateTime.UtcNow.Ticks - clientData.ImprisonedAt;
                long timeLeft = clientData.ImprisonedFor - timeInCell;

                if (timeLeft < 0)
                    return "You're not in jail";

                return $"You're in jail for {timeLeft / TimeSpan.TicksPerSecond} more seconds.";
        }
    }

    private static string GetPlayerList(bool showIps)
    {
        StringBuilder usernames = new("Player list:\n");
        if (showIps)
            usernames.AppendLine(("Server - " + serverIp).PadRight(UsernameLimit * 3, ' '));
        else
            usernames.AppendLine("Server".PadRight(UsernameLimit * 3, ' '));

        for (int i = 0; i < Clients.Count; i++)
        {
            string name = Clients[i].Username + " - " + Clients[i].Money;

            if (Clients[i].IsAdmin)
                name = "(admin)" + name;

            if (showIps)
                name += " - " + Clients[i].IP;

            usernames.AppendLine(name.PadRight(UsernameLimit * 3, ' '));
        }

        return usernames.ToString();
    }

    private static void DirectMessage(Message message)
    {
        string recipient = message.Content.Split('>')[0].ToLower();
        message.Sender = $"{message.Sender} -> {recipient}";
        message.Content = message.Content[(recipient.Length + 1)..];
        message.Type = Message.MessageType.Message;

        ChatRender.AddMessage(message);
        var recipientData = ClientData.FromUsername(recipient);
        message.SendTo(recipientData);
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("Local IP Address Not Found!");
    }

    public class ClientData
    {
        public NetworkStream NetworkStream { get; set; }
        public string Username { get; set; }
        public string IP { get; set; }
        public bool IsAdmin { get; set; }
        public int Money { get; set; }
        public long ImprisonedAt { get; set; }
        public long ImprisonedFor { get; set; }
        public bool InJail => DateTime.UtcNow.Ticks - ImprisonedAt < ImprisonedFor;

        public ClientData(NetworkStream networkStream, string ip, string username)
        {
            NetworkStream = networkStream;
            Username = username;
            IP = ip;
            Money = 100;
        }

        public void Imprison(long time)
        {
            time *= TimeSpan.TicksPerSecond;

            if (InJail)
            {
                ImprisonedFor += time;
                return;
            }    

            ImprisonedAt = DateTime.UtcNow.Ticks;
            ImprisonedFor = time;
        }

        public static ClientData FromUsername(string username)
        {
            for (int i = 0; i < Clients.Count; i++)
            {
                if (Clients[i].Username == username)
                    return Clients[i];
            }
            return null;
        }
    }
}
