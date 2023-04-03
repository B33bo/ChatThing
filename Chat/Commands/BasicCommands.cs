namespace Chat.Commands;

internal static class BasicCommands
{
    [Command("alias")]
    public static void Alias(string[] args)
    {
        if (args.Length == 4)
            new Message(args[1], args[2])
            {
                Color = args[3],
            }.Send();
        else
            new Message(args[1], args[2]).Send();
    }

    [Command("color")]
    public static void Color(string[] args)
    {
        if (args.Length > 1)
            Client.Color = args[1];
        else
            Client.Color = ConsoleColorManager.GetColor(Random.Shared.Next(0, 360));
    }

    [Command("quit")]
    public static void Quit(string[] args)
    {
        if (Server.IsServer)
        {
            for (int i = 0; i < Server.Clients.Count; i++)
                Server.Clients[i].NetworkStream.Close();
            Server.Listener.Stop();
            return;
        }

        Message.NetworkStream.Close();
        Message.TCPClient.Close();
    }

    [Command("dm")]
    public static void DM(string[] args)
    {
        if (Server.IsServer)
        {
            var serverMessage = new Message("Server -> " + args[1], args[2])
            {
                Color = "255;255;255",
                Type = Message.MessageType.DirectMessage,
                IsPrivate = true,
            };

            ChatRender.AddMessage(serverMessage);
            var target = Server.ClientData.FromUsername(args[1]);
            serverMessage.SendTo(target);
            return;
        }

        var message = new Message(Client.Username, args[1] + ">" + args[2])
        {
            Color = Client.Color,
            Type = Message.MessageType.DirectMessage,
            IsPrivate = true,
        };
        message.Send();
        message.Sender = Client.Username + " -> " + args[1];
        message.Content = args[2];
        ChatRender.AddMessage(message);
    }

    [Command("output")]
    public static void Output(string[] args)
    {
        string data = ChatRender.OutputString();
        string filePos = "./output.txt";

        if (args.Length > 1)
            filePos = args[1];

        File.WriteAllText(filePos, data);
    }

    [Command("clear")]
    public static void Clear(string[] args)
    {
        Console.Clear();
    }
}
