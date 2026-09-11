using System.ComponentModel.DataAnnotations;

namespace ShiftFlow.ViewModels;

public class CreateScheduleViewModel
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [Display(Name = "Week starting (Monday)")]
    public DateOnly WeekStartDate { get; set; }
}
