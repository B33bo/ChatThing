namespace Chat;

internal class Program
{
    public static void Main(string[] args)
    {
        ConsoleColorManager.Enable();

        Console.WriteLine("Are you hosting? (Y/N)");
        var isServer = Console.ReadKey().Key == ConsoleKey.Y;

        if (!isServer)
        {
            Console.WriteLine("Username?");
            Client.Username = Console.ReadLine();
            Console.WriteLine("Target IP?");
            Message.IP = Console.ReadLine();
            Console.Title = Client.Username;
        }

        if (isServer)
            Server.StartRecieving();
        else
            Client.StartSending();
    }
}