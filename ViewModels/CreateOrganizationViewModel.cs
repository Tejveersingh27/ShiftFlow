namespace ShiftFlow.ViewModels;

using System.ComponentModel.DataAnnotations;    

public class CreateOrganizationViewModel
{
    [Required] // these are the rules for the form, enforced by the browser and by the server
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Organization Name")]
    public string Name { get; set; } = string.Empty;
}

// SO THAT in the controller you just ask if the ModelState is Valid instead of hand writting the rules tehre