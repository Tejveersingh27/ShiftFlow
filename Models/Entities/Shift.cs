namespace ShiftFlow.Models.Entities;

// One time slot within a Schedule. AssignedMemberId is nullable — a shift can
// exist unassigned ("open shift") before someone is put on it.
public class Shift
{
    public Guid Id { get; set; }

    public Guid ScheduleId { get; set; }
    public Schedule Schedule { get; set; } = null!;

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }

    // Nullable: the shift might not have anyone assigned yet.
    public Guid? AssignedMemberId { get; set; }
    public OrganizationMember? AssignedMember { get; set; }
}
