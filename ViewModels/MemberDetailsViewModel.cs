using ShiftFlow.Models.Entities;

namespace ShiftFlow.ViewModels;

// One employee's profile page: who they are, plus what a manager can edit
// about them, plus their upcoming shifts.
public class MemberDetailsViewModel
{
    public OrganizationMember Member { get; set; } = null!;
    public List<Department> Departments { get; set; } = new();
    public List<Shift> UpcomingShifts { get; set; } = new();
}
