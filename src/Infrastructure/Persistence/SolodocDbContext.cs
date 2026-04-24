using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Common;
using Solodoc.Domain.Entities.Audit;
using Solodoc.Domain.Entities.Billing;
using Solodoc.Domain.Entities.Documents;
using Solodoc.Domain.Entities.Expenses;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Entities.Calendar;
using Solodoc.Domain.Entities.Chemicals;
using Solodoc.Domain.Entities.Checklists;
using Solodoc.Domain.Entities.Locations;
using Solodoc.Domain.Entities.Contacts;
using Solodoc.Domain.Entities.Deviations;
using Solodoc.Domain.Entities.Employees;
using Solodoc.Domain.Entities.Equipment;
using Solodoc.Domain.Entities.Help;
using Solodoc.Domain.Entities.Hms;
using Solodoc.Domain.Entities.Hours;
using Solodoc.Domain.Entities.Notifications;
using Solodoc.Domain.Entities.Procedures;
using Solodoc.Domain.Entities.Projects;
using Solodoc.Domain.Entities.TaskGroups;
using Solodoc.Domain.Entities.Export;
using Solodoc.Domain.Entities.Translations;

namespace Solodoc.Infrastructure.Persistence;

public class SolodocDbContext : DbContext
{
    private readonly ITenantProvider? _tenantProvider;
    private Guid? _auditPerformedById;

    public SolodocDbContext(DbContextOptions<SolodocDbContext> options, ITenantProvider? tenantProvider = null)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Enable automatic audit logging for the next SaveChanges call.
    /// Pass the ID of the user performing the action.
    /// </summary>
    public void EnableAuditLogging(Guid performedById)
    {
        _auditPerformedById = performedById;
    }

    /// <summary>
    /// Disable automatic audit logging.
    /// </summary>
    public void DisableAuditLogging()
    {
        _auditPerformedById = null;
    }

    // Auth
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<CustomRole> CustomRoles => Set<CustomRole>();
    public DbSet<SubcontractorAccess> SubcontractorAccesses => Set<SubcontractorAccess>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<BankIdVerification> BankIdVerifications => Set<BankIdVerification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();
    public DbSet<WorksiteCheckIn> WorksiteCheckIns => Set<WorksiteCheckIn>();

    // Core
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ProjectMembership> ProjectMemberships => Set<ProjectMembership>();
    public DbSet<JobPartsItem> JobPartsItems => Set<JobPartsItem>();
    public DbSet<Deviation> Deviations => Set<Deviation>();
    public DbSet<DeviationCategory> DeviationCategories => Set<DeviationCategory>();
    public DbSet<DeviationPhoto> DeviationPhotos => Set<DeviationPhoto>();
    public DbSet<DeviationComment> DeviationComments => Set<DeviationComment>();
    public DbSet<RelatedDeviation> RelatedDeviations => Set<RelatedDeviation>();
    public DbSet<DeviationVisibility> DeviationVisibilities => Set<DeviationVisibility>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<TimeEntryAllowance> TimeEntryAllowances => Set<TimeEntryAllowance>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<WorkScheduleDay> WorkScheduleDays => Set<WorkScheduleDay>();
    public DbSet<AllowanceRule> AllowanceRules => Set<AllowanceRule>();
    public DbSet<AllowanceGroup> AllowanceGroups => Set<AllowanceGroup>();
    public DbSet<AllowanceGroupMember> AllowanceGroupMembers => Set<AllowanceGroupMember>();
    public DbSet<AllowanceGroupRule> AllowanceGroupRules => Set<AllowanceGroupRule>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<EmployeeScheduleAssignment> EmployeeScheduleAssignments => Set<EmployeeScheduleAssignment>();
    public DbSet<Absence> Absences => Set<Absence>();
    public DbSet<OvertimeBankEntry> OvertimeBankEntries => Set<OvertimeBankEntry>();
    public DbSet<ShiftDefinition> ShiftDefinitions => Set<ShiftDefinition>();
    public DbSet<RotationPattern> RotationPatterns => Set<RotationPattern>();
    public DbSet<RotationPatternDay> RotationPatternDays => Set<RotationPatternDay>();
    public DbSet<EmployeeRotationAssignment> EmployeeRotationAssignments => Set<EmployeeRotationAssignment>();
    public DbSet<OvertimeRule> OvertimeRules => Set<OvertimeRule>();
    public DbSet<PlannerEntry> PlannerEntries => Set<PlannerEntry>();

    // Locations
    public DbSet<Location> Locations => Set<Location>();

    // Checklists
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<ChecklistTemplateVersion> ChecklistTemplateVersions => Set<ChecklistTemplateVersion>();
    public DbSet<ChecklistTemplateItem> ChecklistTemplateItems => Set<ChecklistTemplateItem>();
    public DbSet<ChecklistInstance> ChecklistInstances => Set<ChecklistInstance>();
    public DbSet<TemplateAssignment> TemplateAssignments => Set<TemplateAssignment>();
    public DbSet<ChecklistInstanceItem> ChecklistInstanceItems => Set<ChecklistInstanceItem>();
    public DbSet<ChecklistParticipant> ChecklistParticipants => Set<ChecklistParticipant>();
    public DbSet<ChecklistObject> ChecklistObjects => Set<ChecklistObject>();
    public DbSet<ChecklistObjectTemplate> ChecklistObjectTemplates => Set<ChecklistObjectTemplate>();
    public DbSet<MarketplaceTemplate> MarketplaceTemplates => Set<MarketplaceTemplate>();
    public DbSet<MarketplacePurchase> MarketplacePurchases => Set<MarketplacePurchase>();

    // Procedures
    public DbSet<ProcedureTemplate> ProcedureTemplates => Set<ProcedureTemplate>();
    public DbSet<ProcedureBlock> ProcedureBlocks => Set<ProcedureBlock>();
    public DbSet<ProcedureReadConfirmation> ProcedureReadConfirmations => Set<ProcedureReadConfirmation>();

    // Employees
    public DbSet<EmployeeCertification> EmployeeCertifications => Set<EmployeeCertification>();
    public DbSet<InternalTraining> InternalTrainings => Set<InternalTraining>();
    public DbSet<VacationBalance> VacationBalances => Set<VacationBalance>();
    public DbSet<VacationEntry> VacationEntries => Set<VacationEntry>();
    public DbSet<SickLeaveEntry> SickLeaveEntries => Set<SickLeaveEntry>();

    // Task Groups
    public DbSet<TaskGroup> TaskGroups => Set<TaskGroup>();
    public DbSet<TaskGroupChecklist> TaskGroupChecklists => Set<TaskGroupChecklist>();
    public DbSet<TaskGroupEquipment> TaskGroupEquipment => Set<TaskGroupEquipment>();
    public DbSet<TaskGroupProcedure> TaskGroupProcedures => Set<TaskGroupProcedure>();
    public DbSet<TaskGroupChemical> TaskGroupChemicals => Set<TaskGroupChemical>();
    public DbSet<TaskGroupRole> TaskGroupRoles => Set<TaskGroupRole>();

    // Chemicals
    public DbSet<Chemical> Chemicals => Set<Chemical>();
    public DbSet<ChemicalSds> ChemicalSdsDocuments => Set<ChemicalSds>();
    public DbSet<ChemicalGhsPictogram> ChemicalGhsPictograms => Set<ChemicalGhsPictogram>();
    public DbSet<ChemicalPpeRequirement> ChemicalPpeRequirements => Set<ChemicalPpeRequirement>();

    // Equipment
    public DbSet<Domain.Entities.Equipment.Equipment> Equipment => Set<Domain.Entities.Equipment.Equipment>();
    public DbSet<EquipmentTypeCategory> EquipmentTypeCategories => Set<EquipmentTypeCategory>();
    public DbSet<EquipmentMaintenance> EquipmentMaintenanceRecords => Set<EquipmentMaintenance>();
    public DbSet<EquipmentInspection> EquipmentInspections => Set<EquipmentInspection>();
    public DbSet<EquipmentProjectAssignment> EquipmentProjectAssignments => Set<EquipmentProjectAssignment>();

    // Contacts
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactProjectLink> ContactProjectLinks => Set<ContactProjectLink>();

    // Calendar
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<EventInvitation> EventInvitations => Set<EventInvitation>();

    // HMS
    public DbSet<SjaForm> SjaForms => Set<SjaForm>();
    public DbSet<SjaParticipant> SjaParticipants => Set<SjaParticipant>();
    public DbSet<SjaHazard> SjaHazards => Set<SjaHazard>();
    public DbSet<SafetyRoundSchedule> SafetyRoundSchedules => Set<SafetyRoundSchedule>();
    public DbSet<HmsMeeting> HmsMeetings => Set<HmsMeeting>();
    public DbSet<HmsMeetingMinutes> HmsMeetingMinutes => Set<HmsMeetingMinutes>();
    public DbSet<HmsMeetingActionItem> HmsMeetingActionItems => Set<HmsMeetingActionItem>();

    // Audit
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AuditSnapshot> AuditSnapshots => Set<AuditSnapshot>();

    // Help
    public DbSet<HelpContent> HelpContents => Set<HelpContent>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();

    // Translations
    public DbSet<Translation> Translations => Set<Translation>();

    // Export
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementAcknowledgment> AnnouncementAcknowledgments => Set<AnnouncementAcknowledgment>();
    public DbSet<AnnouncementDismissal> AnnouncementDismissals => Set<AnnouncementDismissal>();
    public DbSet<AnnouncementComment> AnnouncementComments => Set<AnnouncementComment>();

    // Project posts
    public DbSet<ProjectPost> ProjectPosts => Set<ProjectPost>();
    public DbSet<ProjectPostComment> ProjectPostComments => Set<ProjectPostComment>();

    // Documents
    public DbSet<DocumentFolder> DocumentFolders => Set<DocumentFolder>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<BusinessDocument> BusinessDocuments => Set<BusinessDocument>();
    public DbSet<WasteDisposalEntry> WasteDisposalEntries => Set<WasteDisposalEntry>();

    // Billing
    public DbSet<CouponCode> CouponCodes => Set<CouponCode>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ClientError> ClientErrors => Set<ClientError>();

    // Expenses
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<TravelExpense> TravelExpenses => Set<TravelExpense>();
    public DbSet<TravelExpenseDay> TravelExpenseDays => Set<TravelExpenseDay>();
    public DbSet<TravelExpenseRate> TravelExpenseRates => Set<TravelExpenseRate>();
    public DbSet<ExpenseSettings> ExpenseSettingsTable => Set<ExpenseSettings>();

    // Expose current tenant for use in query filters (EF Core evaluates this per query)
    private Guid? CurrentTenantId => _tenantProvider?.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SolodocDbContext).Assembly);

        ApplyGlobalFilters(modelBuilder);
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        LogAuditEvents();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        LogAuditEvents();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    private void LogAuditEvents()
    {
        if (_auditPerformedById is null) return;

        var performedById = _auditPerformedById.Value;
        var now = DateTimeOffset.UtcNow;

        var trackedEntities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not AuditEvent && e.Entity is not AuditSnapshot)
            .ToList();

        foreach (var entry in trackedEntities)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityId = entry.Entity.Id;
            var action = entry.State == EntityState.Deleted ? "Deleted" : "Modified";

            Guid? tenantId = entry.Entity is TenantScopedEntity tenantEntity
                ? tenantEntity.TenantId
                : null;

            string? details = null;
            if (entry.State == EntityState.Modified)
            {
                var changedProperties = entry.Properties
                    .Where(p => p.IsModified && p.Metadata.Name is not "UpdatedAt" and not "CreatedAt")
                    .Select(p => p.Metadata.Name)
                    .ToList();

                if (changedProperties.Count > 0)
                    details = $"Changed: {string.Join(", ", changedProperties)}";
            }

            var auditEvent = new AuditEvent
            {
                TenantId = tenantId,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                PerformedById = performedById,
                Details = details,
                PerformedAt = now
            };

            AuditEvents.Add(auditEvent);
        }
    }

    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");

            // Soft delete filter: e.IsDeleted == false
            var isDeletedProp = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var softDeleteCondition = Expression.Equal(isDeletedProp, Expression.Constant(false));

            Expression filterBody = softDeleteCondition;

            // Tenant filter for TenantScopedEntity: e.TenantId == CurrentTenantId || CurrentTenantId == null
            if (typeof(TenantScopedEntity).IsAssignableFrom(entityType.ClrType))
            {
                var tenantIdProp = Expression.Property(parameter, nameof(TenantScopedEntity.TenantId));
                var currentTenantExpr = Expression.Property(
                    Expression.Constant(this), nameof(CurrentTenantId));
                var currentTenantValue = Expression.Property(currentTenantExpr, "Value");
                var hasValue = Expression.Property(currentTenantExpr, "HasValue");

                // CurrentTenantId == null (bypass) OR e.TenantId == CurrentTenantId
                var tenantMatch = Expression.Equal(tenantIdProp, currentTenantValue);
                var noTenant = Expression.Not(hasValue);
                var tenantCondition = Expression.OrElse(noTenant, tenantMatch);

                filterBody = Expression.AndAlso(softDeleteCondition, tenantCondition);
            }

            var lambda = Expression.Lambda(filterBody, parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
