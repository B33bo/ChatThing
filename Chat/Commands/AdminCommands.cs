namespace Chat.Commands;

internal static class AdminCommands
{
    [Command("kick")]
    public static void Kick(string[] args)
    {
        var target = Server.ClientData.FromUsername(args[1]);
        target.NetworkStream.Close();
    }

    [Command("ban")]
    public static void Ban(string[] args)
    {
        var target = Server.ClientData.FromUsername(args[1]);
        Server.BanList.Add(target.IP);
        target.NetworkStream.Close(); // bye =D
    }

    [Command("admin")]
    public static void Admin(string[] args)
    {
        args[1] = args[1].ToLower();

        if (args[1] == Client.Username.ToLower())
        {
            ChatRender.AddMessage(new Message("Server", "You are no longer admin!")); // hehehe
            return;
        }

        var target = Server.ClientData.FromUsername(args[1]);

        target.IsAdmin = true;
        new Message("Server", "You are now admin!").SendTo(target);
    }

    [Command("unadmin")]
    public static void Unadmin(string[] args)
    {
        args[1] = args[1].ToLower();

        if (args[1] == Client.Username.ToLower())
        {
            ChatRender.AddMessage(new Message("Server", "You are no longer admin!")); // hehehe
            return;
        }

        var target = Server.ClientData.FromUsername(args[1]);

        target.IsAdmin = false;
        new Message("Server", "You are no longer admin!").SendTo(target);
    }

    [Command("sudo")]
    public static void Sudo(string[] args)
    {
        string comamnd = "";
        for (int i = 1; i < args.Length; i++)
            comamnd += args[i] + "&pipe;";

        new Message(Client.Username, comamnd)
        {
            Type = Message.MessageType.RunCommandOnServer,
            IsPrivate = true,
        }.Send();
    }

    [Command("rename")]
    public static void Rename(string[] args)
    {
        args[1] = args[1].ToLower();
        var target = Server.ClientData.FromUsername(args[1]);

        new Message("Server", args[2])
        {
            Type = Message.MessageType.UsernameChange,
            IsPrivate = true,
        }.SendTo(target);
    }

    [Command("sentence")]
    public static void Sentence(string[] args)
    {
        if (args.Length == 2)
        {
            var target = Server.ClientData.FromUsername(args[1]);
            long secondsLeft = (target.ImprisonedAt - target.ImprisonedFor) / TimeSpan.TicksPerSecond;
            ChatRender.AddMessage(new Message("Policeman", $"{args[1]} is in jail for {secondsLeft} more seconds"));
            return;
        }

        new Message(Client.Username, "sentence")
        {
            Type = Message.MessageType.InfoRequest,
            IsPrivate = true,
        }.Send();
    }

    [Command("jail")]
    public static void Jail(string[] args)
    {
        Server.ClientData.FromUsername(args[1]).Imprison(int.Parse(args[2]));
    }

    [Command("bail")]
    public static void Bail(string[] args)
    {
        Server.ClientData.FromUsername(args[1]).ImprisonedFor = 0;
    }
}
