using CivicConnect.Modules.Feedback;
using CivicConnect.Modules.RequestQueries;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages;

/// <summary>FR-006: the signed-in user's notices about their requests, newest first.</summary>
public class NoticesModel(IFeedbackNotices notices) : PageModel
{
    private const int PageSize = 20;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<NoticeItem> Notices { get; private set; } =
        new(Array.Empty<NoticeItem>(), 1, PageSize, 0);

    public bool HasUnread => Notices.Items.Any(n => !n.IsRead);

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Notices = await notices.GetForRecipientAsync(User.GetUserId(), PageNumber, PageSize, cancellationToken);

    public async Task<IActionResult> OnPostMarkAllReadAsync(CancellationToken cancellationToken)
    {
        await notices.MarkAllReadAsync(User.GetUserId(), cancellationToken);
        return RedirectToPage();
    }
}
