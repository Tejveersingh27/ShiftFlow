namespace ShiftFlow.Models.Entities;

// One week's schedule for one organization. Starts Draft, becomes Published.
public class Schedule
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    // The Monday of the week this schedule covers.
    public DateOnly WeekStartDate { get; set; }

    public ScheduleStatus Status { get; set; } = ScheduleStatus.Draft;

    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
