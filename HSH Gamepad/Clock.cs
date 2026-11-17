using System.Diagnostics;

namespace GamepadServer;

internal static class Clock
{
    public static long Ms => Stopwatch.GetTimestamp() * 1000L / Stopwatch.Frequency;
}
