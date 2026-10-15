namespace CivicConnect.Modules.RequestIntake;

public sealed record CategoryOption(Guid Id, string Name);

public sealed record SubmitRequestResult(Guid? RequestId, string? Reference, IReadOnlyList<FieldError> Errors)
{
    public bool Succeeded => Reference is not null;

    public static SubmitRequestResult Created(Guid requestId, string reference) =>
        new(requestId, reference, Array.Empty<FieldError>());

    public static SubmitRequestResult Invalid(IReadOnlyList<FieldError> errors) => new(null, null, errors);
}

/// <summary>
/// Request Intake contract (ADR-008). The reference is returned only after the request is committed (NFR-007).
/// </summary>
public interface ISubmitRequest
{
    Task<IReadOnlyList<CategoryOption>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);

    Task<SubmitRequestResult> SubmitAsync(Guid requesterId, SubmitRequestInput input, CancellationToken cancellationToken = default);
}
