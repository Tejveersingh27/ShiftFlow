using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Services;
using ShiftFlow.ViewModels;

namespace ShiftFlow.Controllers;

// [Authorize] on the class => every action here needs a logged-in user.
// Not logged in -> the framework redirects to the login page. This is the
// real, server-side enforcement (not just hiding a link in the UI).
[Authorize]
public class OrganizationsController : Controller
{
    private readonly OrganizationService _organizationService;

    public OrganizationsController(OrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    // GET /Organizations/Create  — show the empty form.
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateOrganizationViewModel());
    }

    // POST /Organizations/Create — handle the submitted form.
    [HttpPost]
    [ValidateAntiForgeryToken] // rejects the request if the form's anti-CSRF token is missing/forged
    public async Task<IActionResult> Create(CreateOrganizationViewModel model)
    {
        // The framework already validated `model` against its [Required]/[StringLength]
        // attributes while binding the form. We just read the result.
        if (!ModelState.IsValid)
        {
            return View(model); // re-render the form, now showing the error messages
        }

        // The logged-in user's id, read from a claim in their auth cookie.
        // `!` = "this is never null here" — [Authorize] guarantees a user.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var organization = await _organizationService.CreateOrganizationAsync(model.Name, userId);

        // Post/Redirect/Get: redirect after a successful POST so a browser
        // refresh doesn't submit the form again.
        TempData["Success"] = $"Organization \"{organization.Name}\" created.";
        return RedirectToAction("Index"); // -> /Organizations (this controller's Index)
    }
    // GET /Organizations  — list the organizations the logged-in user belongs to.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // who's logged in? get their ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // what organizations is this user a member of? (the service does the DB query)
        var memberships = await _organizationService.GetMembershipsForUserAsync(userId);

        // render the page, passing it that list to display
        return View(memberships);
    }
}
