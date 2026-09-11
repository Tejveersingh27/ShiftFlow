using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Models.Entities;
using ShiftFlow.Services;
using ShiftFlow.ViewModels;

namespace ShiftFlow.Controllers;
//URLs: Index (list), Create (POST), Details (view one), AddShift (POST), Publish (POST). 
[Authorize] // thats middlewre
public class SchedulesController : Controller
{
    private readonly SchedulingService _schedulingService;
    private readonly MemberService _memberService; // reused for IsMemberAsync, same guard pattern as Members/Departments

    public SchedulesController(SchedulingService schedulingService, MemberService memberService)
    {
        //takes these services in the constructor, and the framework will call this constructor and pass in the services automatically.
        _schedulingService = schedulingService;
        _memberService = memberService;
    }

    // GET /Schedules?organizationId=... — list this org's weekly schedules.
    [HttpGet]
    public async Task<IActionResult> Index(Guid organizationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = await _memberService.GetRoleAsync(organizationId, userId);
        if (role is null)
        {
            return Forbid(); // not a member of this org at all
        }

        var schedules = await _schedulingService.GetSchedulesAsync(organizationId);

        // Employees only ever see the official, published schedule — drafts are
        // a manager's working copy, not something employees should see mid-edit.
        bool isManager = role != OrganizationRole.Employee;
        if (!isManager)
        {
            schedules = schedules.Where(s => s.Status == ScheduleStatus.Published).ToList();
        }

        ViewData["OrganizationId"] = organizationId;
        ViewData["IsManager"] = isManager; // the view uses this to hide the "new schedule" form
        return View(schedules);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateScheduleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Pick a valid week start date.";
            return RedirectToAction("Index", new { organizationId = model.OrganizationId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _schedulingService.CreateScheduleAsync(model.OrganizationId, userId, model.WeekStartDate);

        if (result == SchedulingService.ScheduleResult.NotAuthorized)
        {
            return Forbid();
        }

        TempData["Success"] = "Schedule created.";
        return RedirectToAction("Index", new { organizationId = model.OrganizationId });
    }

    // GET /Schedules/Details/{id} — one schedule + its shifts + add-shift form + publish button.
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var schedule = await _schedulingService.GetScheduleWithShiftsAsync(id);
        if (schedule is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role = await _memberService.GetRoleAsync(schedule.OrganizationId, userId);
        if (role is null)
        {
            return Forbid(); // not a member of this org
        }

        // Employees may only view schedules once published — a Draft is a
        // manager's working copy, not yet the "official" schedule.
        if (role == OrganizationRole.Employee && schedule.Status == ScheduleStatus.Draft)
        {
            return Forbid();
        }

        var members = await _memberService.GetMembersAsync(schedule.OrganizationId);
        ViewData["IsManager"] = role != OrganizationRole.Employee;
        return View(new ScheduleDetailsViewModel { Schedule = schedule, OrganizationMembers = members });
    }

    // POST /Schedules/AssignShift — assign an org member to a shift. Blocked if
    // they're already on an overlapping shift (SchedulingService.AssignEmployeeAsync).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignShift(Guid shiftId, Guid memberId, Guid scheduleId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _schedulingService.AssignEmployeeAsync(shiftId, userId, memberId);

        TempData["Error"] = result switch
        {
            SchedulingService.AssignmentResult.OverlappingShift => "This person is already working an overlapping shift.",
            SchedulingService.AssignmentResult.MemberNotInOrganization => "That person isn't a member of this organization.",
            SchedulingService.AssignmentResult.ShiftNotFound => "Shift not found.",
            _ => null
        };

        if (result == SchedulingService.AssignmentResult.NotAuthorized)
        {
            return Forbid();
        }

        if (result == SchedulingService.AssignmentResult.Assigned)
        {
            TempData["Success"] = "Shift assigned.";
        }

        return RedirectToAction("Details", new { id = scheduleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddShift(AddShiftViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter valid start and end times.";
            return RedirectToAction("Details", new { id = model.ScheduleId });
        }

        // The <input type="datetime-local"> form field comes back with Kind=Unspecified
        // (no timezone attached). Postgres's timestamptz column requires an explicit
        // UTC marker, so we stamp it here. (Simplification: we're treating the entered
        // time as if it were already UTC — real timezone-aware input is future work.)
        var startsAtUtc = DateTime.SpecifyKind(model.StartsAt, DateTimeKind.Utc);
        var endsAtUtc = DateTime.SpecifyKind(model.EndsAt, DateTimeKind.Utc);

        var result = await _schedulingService.CreateShiftAsync(model.ScheduleId, userId, startsAtUtc, endsAtUtc);

        TempData["Error"] = result switch
        {
            SchedulingService.ScheduleResult.InvalidTimeRange => "End time must be after start time.",
            SchedulingService.ScheduleResult.ScheduleNotFound => "Schedule not found.",
            _ => null
        };

        if (result == SchedulingService.ScheduleResult.NotAuthorized)
        {
            return Forbid();
        }

        return RedirectToAction("Details", new { id = model.ScheduleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid scheduleId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _schedulingService.PublishScheduleAsync(scheduleId, userId);

        if (result == SchedulingService.ScheduleResult.NotAuthorized)
        {
            return Forbid();
        }

        TempData["Success"] = "Schedule published.";
        return RedirectToAction("Details", new { id = scheduleId });
    }
}
