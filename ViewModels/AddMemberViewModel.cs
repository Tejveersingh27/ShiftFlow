using System.ComponentModel.DataAnnotations;
using ShiftFlow.Models.Entities;

namespace ShiftFlow.ViewModels;

// The "add a member" form: which org, whose email, what role.
public class AddMemberViewModel
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public OrganizationRole Role { get; set; } = OrganizationRole.Employee;
}
