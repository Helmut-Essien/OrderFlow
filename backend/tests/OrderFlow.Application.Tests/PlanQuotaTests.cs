using OrderFlow.Domain;

namespace OrderFlow.Application.Tests;

public class PlanQuotaTests
{
    [Theory]
    [InlineData("Starter", "Starter", false)]
    [InlineData("Growth", "Growth", false)]
    [InlineData("Business", "Business", false)]
    [InlineData("growth plus", "Growth", false)]
    [InlineData("Pro Annual", "Starter", true)]
    [InlineData("", "Starter", true)]
    public void FromPlanName_MapsKnownPlansAndFallsBackToStarter(string? input, string expected, bool unrecognized)
    {
        var quota = PlanQuota.FromPlanName(string.IsNullOrEmpty(input) ? input : input);

        Assert.Equal(expected, quota.Name);
        Assert.Equal(unrecognized, quota.IsUnrecognized);
    }

    [Fact]
    public void Growth_HasExpectedLimits()
    {
        var growth = PlanQuota.Growth;
        Assert.Equal(300, growth.MaxProducts);
        Assert.Null(growth.MaxOrdersPerMonth);
        Assert.Equal(3, growth.MaxUsers);
        Assert.False(growth.AiFeatures);
    }
}
