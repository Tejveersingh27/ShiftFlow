using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Models;
using ShiftFlow.Services;
using ShiftFlow.ViewModels;

namespace ShiftFlow.Controllers;

public class HomeController : Controller
{
    private readonly OrganizationService _organizationService;
    private readonly SchedulingService _schedulingService;

    public HomeController(OrganizationService organizationService, SchedulingService schedulingService)
    {
        _organizationService = organizationService;
        _schedulingService = schedulingService;
    }

    // Not [Authorize] — this is the public landing page, so anonymous
    // visitors still need to land somewhere sensible instead of an error.
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new List<MyScheduleCardViewModel>());
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var memberships = await _organizationService.GetMembershipsForUserAsync(userId);

        var cards = new List<MyScheduleCardViewModel>();
        foreach (var membership in memberships)
        {
            var shifts = await _schedulingService.GetShiftsForMemberAsync(membership.Id);
            var upcoming = shifts
                .Where(s => s.StartsAtUtc.Date >= DateTime.UtcNow.Date)
                .Take(3)
                .ToList();

            cards.Add(new MyScheduleCardViewModel
            {
                OrganizationId = membership.OrganizationId,
                OrganizationName = membership.Organization.Name,
                UpcomingShifts = upcoming
            });
        }

        return View(cards);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
