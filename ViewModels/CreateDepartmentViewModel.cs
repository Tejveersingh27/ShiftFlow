using System.ComponentModel.DataAnnotations;

namespace ShiftFlow.ViewModels;

public class CreateDepartmentViewModel
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Department name")]
    public string Name { get; set; } = string.Empty;
}
