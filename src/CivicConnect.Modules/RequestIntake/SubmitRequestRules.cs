namespace CivicConnect.Modules.RequestIntake;

/// <summary>What a requester enters on the submit form (CL-05).</summary>
public sealed record SubmitRequestInput(Guid? CategoryId, string? Title, string? Description, string? Location);

/// <summary>A validation problem tied to one input field, so the form can show it beside that field.</summary>
public sealed record FieldError(string Field, string Message);

/// <summary>
/// Server-side validation of a new service request (FR-001, FR-002, NFR-011).
/// Mandatory: category (must be active), title, description. Optional: location.
/// The database constraints mirror these limits; neither replaces the other.
/// </summary>
public static class SubmitRequestRules
{
    public const int TitleMaxLength = 120;
    public const int DescriptionMaxLength = 4000;
    public const int LocationMaxLength = 200;

    public static IReadOnlyList<FieldError> Validate(SubmitRequestInput input, IReadOnlySet<Guid> activeCategoryIds)
    {
        var errors = new List<FieldError>();

        if (input.CategoryId is not Guid categoryId)
            errors.Add(new(nameof(SubmitRequestInput.CategoryId), "Choose a category."));
        else if (!activeCategoryIds.Contains(categoryId))
            errors.Add(new(nameof(SubmitRequestInput.CategoryId), "That category is not available. Choose another one."));

        CheckRequired(errors, nameof(SubmitRequestInput.Title), "title", input.Title, TitleMaxLength);
        CheckRequired(errors, nameof(SubmitRequestInput.Description), "description", input.Description, DescriptionMaxLength);

        if (input.Location is { } location && location.Trim().Length > LocationMaxLength)
            errors.Add(new(nameof(SubmitRequestInput.Location), $"Keep the location to {LocationMaxLength} characters or fewer."));

        return errors;
    }

    /// <summary>Trims the text fields and turns a blank location into null, ready to save.</summary>
    public static SubmitRequestInput Normalise(SubmitRequestInput input) =>
        input with
        {
            Title = input.Title?.Trim(),
            Description = input.Description?.Trim(),
            Location = string.IsNullOrWhiteSpace(input.Location) ? null : input.Location.Trim()
        };

    private static void CheckRequired(List<FieldError> errors, string field, string label, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(new(field, $"Enter a {label}."));
        else if (value.Trim().Length > maxLength)
            errors.Add(new(field, $"Keep the {label} to {maxLength} characters or fewer."));
    }
}
