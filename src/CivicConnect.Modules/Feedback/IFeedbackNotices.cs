using CivicConnect.Modules.RequestQueries;

namespace CivicConnect.Modules.Feedback;

public sealed record NoticeItem(long Id, Guid RequestId, string Message, DateTimeOffset CreatedAt, bool IsRead);

/// <summary>
/// FR-006 read side. Notices are written by the lifecycle and intake services in the same
/// transaction as the change they describe, so a notice is never lost after a commit (R-15).
/// </summary>
public interface IFeedbackNotices
{
    Task<PagedResult<NoticeItem>> GetForRecipientAsync(Guid recipientId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid recipientId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid recipientId, CancellationToken cancellationToken = default);
}
