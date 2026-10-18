using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestIntake;
using CivicConnect.Modules.RequestQueries;

namespace CivicConnect.Web.Pages.Shared;

/// <summary>What the search and filter bar (_RequestFilters) shows, and the filter it stands for (FR-008).</summary>
public sealed record RequestFilterForm(
    string? Search,
    RequestStatus? Status,
    Guid? CategoryId,
    RequestSort Sort,
    IReadOnlyList<CategoryOption> Categories)
{
    public RequestFilter ToFilter() => new(Search, Status, CategoryId, Sort);

    public bool IsFiltered => !string.IsNullOrWhiteSpace(Search) || Status is not null || CategoryId is not null;

    public static string SortLabel(RequestSort sort) => sort switch
    {
        RequestSort.Newest => "Newest first",
        RequestSort.RecentlyUpdated => "Recently updated",
        _ => "Oldest first"
    };
}
