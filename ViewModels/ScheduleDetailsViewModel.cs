using ShiftFlow.Models.Entities;

namespace ShiftFlow.ViewModels;

// What the Schedule Details page needs: the schedule itself, plus the org's
// members (to populate the "assign to" dropdown next to each open shift).
public class ScheduleDetailsViewModel
{
    public Schedule Schedule { get; set; } = null!;
    public List<OrganizationMember> OrganizationMembers { get; set; } = new();
}
