namespace Chat.Commands;

internal class MoneyCommands
{
    [Command("list")]
    public static void List(string[] args)
    {
        new Message(Client.Username, "list")
        {
            Type = Message.MessageType.InfoRequest,
            IsPrivate = true,
        }.Send();
    }

    [Command("points")]
    public static void Points(string[] args)
    {
        ChatRender.AddMessage(new Message("Money manager", $"You have {Client.Points} points"));
    }

    [Command("pay")]
    public static void Pay(string[] args)
    {
        new Message(Client.Username, args[1] + "," + args[2])
        {
            Type = Message.MessageType.ChangeMoney,
            IsPrivate = true,
        }.Send();
    }

    [Command("rob")]
    public static void Rob(string[] args)
    {
        new Message(Client.Username, args[1])
        {
            Type = Message.MessageType.Rob,
            IsPrivate = true,
        }.Send();
    }
}
