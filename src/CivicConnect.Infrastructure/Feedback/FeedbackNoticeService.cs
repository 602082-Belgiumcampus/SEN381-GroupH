using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.Feedback;
using CivicConnect.Modules.RequestQueries;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Feedback;

/// <summary>FR-006: a user's own notices. Every query is filtered by the recipient, so nobody sees another user's notices.</summary>
public sealed class FeedbackNoticeService(CivicConnectDbContext db, TimeProvider time) : IFeedbackNotices
{
    public const int MaxPageSize = 100;

    public async Task<PagedResult<NoticeItem>> GetForRecipientAsync(
        Guid recipientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var notices = db.FeedbackNotices.AsNoTracking().Where(n => n.RecipientId == recipientId);
        var total = await notices.CountAsync(cancellationToken);
        var items = await notices
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NoticeItem(n.Id, n.RequestId, n.Message, n.CreatedAt, n.ReadAt != null))
            .ToListAsync(cancellationToken);

        return new PagedResult<NoticeItem>(items, page, pageSize, total);
    }

    public Task<int> CountUnreadAsync(Guid recipientId, CancellationToken cancellationToken = default) =>
        db.FeedbackNotices.CountAsync(n => n.RecipientId == recipientId && n.ReadAt == null, cancellationToken);

    public Task MarkAllReadAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        return db.FeedbackNotices
            .Where(n => n.RecipientId == recipientId && n.ReadAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(n => n.ReadAt, now), cancellationToken);
    }
}
