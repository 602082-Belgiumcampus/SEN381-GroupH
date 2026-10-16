using CivicConnect.Modules.RequestIntake;

namespace CivicConnect.Domain.Tests;

/// Pure parts of TC-IN-01 to TC-IN-03 (validation before anything is saved).
/// The database parts (reference uniqueness after commit) belong to the TC-PE integration tests.
public class RequestIntakeRulesTests
{
    private static readonly Guid ActiveCategory = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid InactiveCategory = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly IReadOnlySet<Guid> ActiveCategories = new HashSet<Guid> { ActiveCategory };

    private static SubmitRequestInput Valid() =>
        new(ActiveCategory, "Broken street light", "The light outside number 12 has been off for a week.", null);

    [Fact]
    public void TC_IN_01_Valid_category_title_and_description_pass()
    {
        Assert.Empty(SubmitRequestRules.Validate(Valid(), ActiveCategories));
    }

    [Fact]
    public void TC_IN_02a_Missing_category_is_identified()
    {
        var errors = SubmitRequestRules.Validate(Valid() with { CategoryId = null }, ActiveCategories);

        var error = Assert.Single(errors);
        Assert.Equal(nameof(SubmitRequestInput.CategoryId), error.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TC_IN_02b_Empty_title_is_identified(string? title)
    {
        var errors = SubmitRequestRules.Validate(Valid() with { Title = title }, ActiveCategories);

        Assert.Equal(nameof(SubmitRequestInput.Title), Assert.Single(errors).Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TC_IN_02c_Empty_description_is_identified(string? description)
    {
        var errors = SubmitRequestRules.Validate(Valid() with { Description = description }, ActiveCategories);

        Assert.Equal(nameof(SubmitRequestInput.Description), Assert.Single(errors).Field);
    }

    [Fact]
    public void TC_IN_02d_All_mandatory_fields_missing_reports_each_one()
    {
        var errors = SubmitRequestRules.Validate(new SubmitRequestInput(null, null, null, null), ActiveCategories);

        Assert.Equal(
            new[] { nameof(SubmitRequestInput.CategoryId), nameof(SubmitRequestInput.Title), nameof(SubmitRequestInput.Description) },
            errors.Select(e => e.Field));
    }

    [Fact]
    public void TC_IN_03_Inactive_category_is_rejected()
    {
        var errors = SubmitRequestRules.Validate(Valid() with { CategoryId = InactiveCategory }, ActiveCategories);

        Assert.Equal(nameof(SubmitRequestInput.CategoryId), Assert.Single(errors).Field);
    }

    [Fact]
    public void Title_at_the_limit_passes_and_one_over_fails()
    {
        var atLimit = new string('a', SubmitRequestRules.TitleMaxLength);
        var overLimit = new string('a', SubmitRequestRules.TitleMaxLength + 1);

        Assert.Empty(SubmitRequestRules.Validate(Valid() with { Title = atLimit }, ActiveCategories));
        Assert.Equal(nameof(SubmitRequestInput.Title),
            Assert.Single(SubmitRequestRules.Validate(Valid() with { Title = overLimit }, ActiveCategories)).Field);
    }

    [Fact]
    public void Location_is_optional()
    {
        Assert.Empty(SubmitRequestRules.Validate(Valid() with { Location = null }, ActiveCategories));
        Assert.Empty(SubmitRequestRules.Validate(Valid() with { Location = "Corner of Main and 2nd" }, ActiveCategories));
    }

    [Fact]
    public void Normalise_trims_text_and_turns_blank_location_into_null()
    {
        var normalised = SubmitRequestRules.Normalise(new SubmitRequestInput(ActiveCategory, "  Title  ", " Text ", "   "));

        Assert.Equal("Title", normalised.Title);
        Assert.Equal("Text", normalised.Description);
        Assert.Null(normalised.Location);
    }

    [Fact]
    public void Reference_is_formatted_with_year_and_padded_number()
    {
        Assert.Equal("CC-2026-000042", RequestReference.Format(2026, 42));
    }

    [Fact]
    public void Reference_number_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RequestReference.Format(2026, 0));
    }
}
