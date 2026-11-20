namespace LiveGateway.Tests;

public class SessionCostTests
{
    [Fact]
    public void Thirty_active_minutes_cost_one_fifty()
        => Assert.Equal(1.50m, SessionCost.Estimate(30 * 60, 0));

    [Fact]
    public void Ten_active_and_twenty_standby_minutes_cost_ninety_cents()
        => Assert.Equal(0.90m, SessionCost.Estimate(10 * 60, 20 * 60));

    [Fact]
    public void Standby_minute_is_sixty_percent_cheaper()
        => Assert.Equal(0.6m, 1 - SessionCost.StandbyPerMinute / SessionCost.ActivePerMinute);

    [Fact]
    public void Billing_is_per_second()
        => Assert.Equal(0.075m, SessionCost.Estimate(90, 0));
}
