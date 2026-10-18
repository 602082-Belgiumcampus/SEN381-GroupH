using CivicConnect.Infrastructure.Identity;
using CivicConnect.Modules.Feedback;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestIntake;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Persistence;

/// <summary>
/// The CivicConnect database (PED Figure 4, ADR-005). Identity supplies AppUser, roles and user-role
/// tables; the rest is the request data. Constraints here mirror the application rules (Section 6.4).
/// </summary>
public class CivicConnectDbContext(DbContextOptions<CivicConnectDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public const string ReferenceSequence = "ServiceRequestReferenceNumbers";

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<RequestAction> RequestActions => Set<RequestAction>();
    public DbSet<FeedbackNotice> FeedbackNotices => Set<FeedbackNotice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var statusValues = string.Join(", ", Enum.GetNames<RequestStatus>().Select(name => $"'{name}'"));

        builder.Entity<AppUser>(user =>
        {
            user.Property(u => u.DisplayName).HasMaxLength(AppUser.DisplayNameMaxLength).IsRequired();
            user.HasIndex(u => u.NormalizedEmail).IsUnique();
        });

        builder.Entity<Category>(category =>
        {
            category.Property(c => c.Name).HasMaxLength(Category.NameMaxLength).IsRequired();
            category.HasIndex(c => c.Name).IsUnique();
            category.ToTable(t => t.HasCheckConstraint(
                "CK_Categories_TargetResolutionDays_Positive", "\"TargetResolutionDays\" > 0"));
        });

        builder.HasSequence<long>(ReferenceSequence);

        builder.Entity<ServiceRequest>(request =>
        {
            request.Property(r => r.Reference).HasMaxLength(ServiceRequest.ReferenceMaxLength).IsRequired();
            request.HasIndex(r => r.Reference).IsUnique();

            request.Property(r => r.Title).HasMaxLength(SubmitRequestRules.TitleMaxLength).IsRequired();
            request.Property(r => r.Description).HasMaxLength(SubmitRequestRules.DescriptionMaxLength).IsRequired();
            request.Property(r => r.Location).HasMaxLength(SubmitRequestRules.LocationMaxLength);
            request.Property(r => r.ContactChannel).HasMaxLength(ServiceRequest.ContactChannelMaxLength).IsRequired();

            request.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            request.Property(r => r.Version).IsConcurrencyToken();

            request.HasOne(r => r.Requester).WithMany().HasForeignKey(r => r.RequesterId).OnDelete(DeleteBehavior.Restrict);
            request.HasOne(r => r.Category).WithMany().HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.Restrict);
            request.HasOne(r => r.AssignedTo).WithMany().HasForeignKey(r => r.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            request.HasOne(r => r.LoggedBy).WithMany().HasForeignKey(r => r.LoggedById).OnDelete(DeleteBehavior.Restrict);

            // Indexes for "my requests", the staff queue and later reporting (FR-005, FR-008)
            request.HasIndex(r => new { r.RequesterId, r.CreatedAt });
            request.HasIndex(r => new { r.Status, r.CreatedAt });

            request.ToTable(t =>
            {
                t.HasCheckConstraint("CK_ServiceRequests_Status", $"\"Status\" IN ({statusValues})");
                t.HasCheckConstraint("CK_ServiceRequests_Version_Positive", "\"Version\" > 0");
                t.HasCheckConstraint(
                    "CK_ServiceRequests_ResolutionNote_WhenResolved",
                    "\"Status\" NOT IN ('Resolved', 'Closed') OR \"ResolutionNote\" IS NOT NULL");
                t.HasCheckConstraint(
                    "CK_ServiceRequests_RejectionReason_WhenRejected",
                    "\"Status\" <> 'Rejected' OR \"RejectionReason\" IS NOT NULL");
            });
        });

        builder.Entity<AuditEntry>(audit =>
        {
            audit.Property(a => a.EventType).HasMaxLength(AuditEntry.EventTypeMaxLength).IsRequired();
            audit.Property(a => a.FromStatus).HasConversion<string>().HasMaxLength(20);
            audit.Property(a => a.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();

            audit.HasOne(a => a.Request).WithMany().HasForeignKey(a => a.RequestId).OnDelete(DeleteBehavior.Restrict);
            audit.HasOne(a => a.Actor).WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.Restrict);

            audit.HasIndex(a => new { a.RequestId, a.OccurredAt });
            // FR-017 activity report reads the audit trail by period
            audit.HasIndex(a => a.OccurredAt);
        });

        builder.Entity<RequestAction>(action =>
        {
            action.Property(a => a.Text).HasMaxLength(RequestActionRules.TextMaxLength).IsRequired();
            action.HasOne(a => a.Request).WithMany().HasForeignKey(a => a.RequestId).OnDelete(DeleteBehavior.Restrict);
            action.HasOne(a => a.Author).WithMany().HasForeignKey(a => a.AuthorId).OnDelete(DeleteBehavior.Restrict);
            action.HasIndex(a => new { a.RequestId, a.CreatedAt });
        });

        builder.Entity<FeedbackNotice>(notice =>
        {
            notice.Property(n => n.Message).HasMaxLength(FeedbackMessages.MessageMaxLength).IsRequired();
            notice.HasOne(n => n.Request).WithMany().HasForeignKey(n => n.RequestId).OnDelete(DeleteBehavior.Restrict);
            notice.HasOne(n => n.Recipient).WithMany().HasForeignKey(n => n.RecipientId).OnDelete(DeleteBehavior.Restrict);
            // "My notices" page and the unread count in the menu
            notice.HasIndex(n => new { n.RecipientId, n.CreatedAt });
        });
    }
}
