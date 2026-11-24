using System.Diagnostics;

namespace GamepadServer;

// Environment.TickCount64 فقط تو .NET Core 3.0+ هست؛ این جایگزینش تو هر دو پروژه (net48 و جدید) کار میکنه.
internal static class Clock
{
    public static long Ms => Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;
}
