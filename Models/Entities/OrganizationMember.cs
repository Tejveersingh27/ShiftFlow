namespace ShiftFlow.Models.Entities;

// The link between one user and one organization, plus that user's role there.
// This class IS the "OrganizationMembers" table. Later, a user's org-specific
// info (max weekly hours, department, active status) will live here too.
public class OrganizationMember
{
    public Guid Id { get; set; }

    // --- which organization ---
    public Guid OrganizationId { get; set; }          // the raw foreign-key value
    public Organization Organization { get; set; } = null!;  // the loaded row (EF fills this)

    // --- which user (Identity's AspNetUsers.Id is a string) ---
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    // --- what they can do in this org ---
    public OrganizationRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    // How many hours a week this person may be scheduled for.
    // Checked by SchedulingService.AssignEmployeeAsync before an assignment.
    public int MaxWeeklyHours { get; set; } = 40;
}
