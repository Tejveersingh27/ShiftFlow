using System.ComponentModel.DataAnnotations;

namespace ShiftFlow.ViewModels;

public class JoinOrganizationViewModel
{
    [Required]
    [Display(Name = "Join code")]
    public string JoinCode { get; set; } = string.Empty;
}
