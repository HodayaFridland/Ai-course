using OrderService.Services;
using Xunit;

namespace OrderService.Tests;

/// <summary>
/// Unit tests for the core saga decision rule used by OrderSagaConsumer.
/// </summary>
public class SagaDecisionTests
{
    [Fact]
    public void Reserved_stock_confirms_the_order()
    {
        Assert.Equal("Confirmed", SagaDecision.FromReservation(reserved: true));
    }

    [Fact]
    public void Rejected_stock_cancels_the_order()
    {
        Assert.Equal("Cancelled", SagaDecision.FromReservation(reserved: false));
    }

    [Theory]
    [InlineData(true, "Confirmed")]
    [InlineData(false, "Cancelled")]
    public void Maps_reservation_outcome_to_status(bool reserved, string expected)
    {
        Assert.Equal(expected, SagaDecision.FromReservation(reserved));
    }
}
