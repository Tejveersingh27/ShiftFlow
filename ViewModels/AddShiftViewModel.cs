using System.ComponentModel.DataAnnotations;

namespace ShiftFlow.ViewModels;

public class AddShiftViewModel
{
    [Required]
    public Guid ScheduleId { get; set; }

    [Required]
    [Display(Name = "Starts")]
    public DateTime StartsAt { get; set; }

    [Required]
    [Display(Name = "Ends")]
    public DateTime EndsAt { get; set; }
}
