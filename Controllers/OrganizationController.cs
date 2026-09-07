using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Services;
using ShiftFlow.ViewModels;

namespace ShiftFlow.Controllers;

public class OrganizationController : Controller
{
    private OrganizationService orgService;
    public OrganizationController(OrganizationService orgService)
    {
        this.orgService = orgService;
    }
    //Show the form to create a new organization
    [HttpGet]
        public IActionResult Create()
    {
        return View();
    }
    // Handle the form submission to create a new organization  
    
    public async Task<IActionResult> Create(CreateOrganizationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organization = await orgService.CreateOrganizationAsync(model.Name, userId);
        return RedirectToAction("Index", "Home");
    }


}