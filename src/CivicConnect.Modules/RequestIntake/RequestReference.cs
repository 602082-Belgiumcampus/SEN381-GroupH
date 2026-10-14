namespace CivicConnect.Modules.RequestIntake;

/// <summary>
/// The human-readable request reference (FR-003), for example CC-2026-000042.
/// The number comes from a database sequence, so it is unique; the unique index on
/// ServiceRequest.Reference is the final guarantee.
/// </summary>
public static class RequestReference
{
    public static string Format(int year, long sequenceNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequenceNumber);
        return $"CC-{year:D4}-{sequenceNumber:D6}";
    }
}
