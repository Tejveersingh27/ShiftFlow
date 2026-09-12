using ShiftFlow.Models.Entities;

namespace ShiftFlow.ViewModels;

// One "My Schedule" card on the Home dashboard — one per org the user
// belongs to, showing just their next few upcoming shifts.
public class MyScheduleCardViewModel
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public List<Shift> UpcomingShifts { get; set; } = new();
}
