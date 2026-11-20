namespace LiveGateway;

public static class SessionCost
{
    public const decimal ActivePerMinute = 0.05m;
    public const decimal StandbyPerMinute = 0.02m;

    // GPT-Live bills voice sessions per second at the active and standby rates.
    public static decimal Estimate(int activeSeconds, int standbySeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(activeSeconds);
        ArgumentOutOfRangeException.ThrowIfNegative(standbySeconds);

        return activeSeconds * ActivePerMinute / 60m
             + standbySeconds * StandbyPerMinute / 60m;
    }
}
