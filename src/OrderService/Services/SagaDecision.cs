namespace OrderService.Services;

/// <summary>
/// The pure business rule at the heart of the order saga: given the inventory outcome,
/// what should the order's final status be? Kept as a tiny, dependency-free function so it
/// can be unit-tested in isolation (see OrderService.Tests).
/// </summary>
public static class SagaDecision
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    /// <summary>Stock reserved -> Confirmed; stock rejected -> Cancelled (compensation).</summary>
    public static string FromReservation(bool reserved) => reserved ? Confirmed : Cancelled;
}
