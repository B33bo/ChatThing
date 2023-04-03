using System.Runtime.InteropServices;

internal static class ConsoleColorManager
{
    public const string START_COLOR = "\u001b";
    public const string END_COLOR = "\u001b[0m";

    public const int FOREGROUND = 38;

    public static bool ColorsAllowed;

    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    public static void Enable()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

            ColorsAllowed = GetConsoleMode(iStdOut, out var outConsoleMode)
                         && SetConsoleMode(iStdOut, outConsoleMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }


        ColorsAllowed = Environment.GetEnvironmentVariable("NO_COLOR") == null;
    }

    public static string GetColor(int hue)
    {
        // Convert hue to RGB
        double h = hue / 60.0;
        double c = 1.0;
        double x = (1.0 - Math.Abs((h % 2) - 1.0));
        double r = 0, g = 0, b = 0;
        if (h >= 0 && h < 1)
        {
            r = c;
            g = x;
        }
        else if (h >= 1 && h < 2)
        {
            r = x;
            g = c;
        }
        else if (h >= 2 && h < 3)
        {
            g = c;
            b = x;
        }
        else if (h >= 3 && h < 4)
        {
            g = x;
            b = c;
        }
        else if (h >= 4 && h < 5)
        {
            r = x;
            b = c;
        }
        else if (h >= 5 && h < 6)
        {
            r = c;
            b = x;
        }

        byte R = (byte)(r * 255);
        byte G = (byte)(g * 255);
        byte B = (byte)(b * 255);
        return $"{R};{G};{B}";
    }
}