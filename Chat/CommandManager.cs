using System.Reflection;

namespace Chat;

internal static class CommandManager
{
    private static readonly Dictionary<string, MethodInfo> CommandData = new();

    public static void LoadCommands()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods();

            for (int i = 0; i < methods.Length; i++)
            {
                var attribute = methods[i].GetCustomAttribute<CommandAttribute>();
                if (attribute == null)
                    continue;
                CommandData.Add(attribute.Name.ToLower(), methods[i]);
            }
        }
    }

    public static bool DoCommand(string[] args)
    {
        if (!CommandData.ContainsKey(args[0]))
            return false;

        try
        {
            CommandData[args[0]].Invoke(null, new object[] { args });
        }
        catch (Exception exc)
        {
            ChatRender.AddMessage(new Message("Error", exc.Message)
            {
                Color = "255;0;0",
            });
        }

        return true;
    }
}
