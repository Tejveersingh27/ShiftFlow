using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Models.Entities;
using ShiftFlow.Services;
using ShiftFlow.ViewModels;

namespace ShiftFlow.Controllers;

[Authorize]
public class MembersController : Controller
{
    private readonly MemberService _memberService;
    private readonly DepartmentService _departmentService;
    private readonly SchedulingService _schedulingService;

    public MembersController(MemberService memberService, DepartmentService departmentService, SchedulingService schedulingService)
    {
        _memberService = memberService;
        _departmentService = departmentService;
        _schedulingService = schedulingService;
    }

    // GET /Members?organizationId=...  — the team list.
    [HttpGet]
    public async Task<IActionResult> Index(Guid organizationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Must belong to this org (any role) to even see its team list.
        var role = await _memberService.GetRoleAsync(organizationId, userId);
        if (role is null)
        {
            return Forbid();
        }

        var members = await _memberService.GetMembersAsync(organizationId);
        ViewData["OrganizationId"] = organizationId;
        var isManager = role != OrganizationRole.Employee;
        ViewData["IsManager"] = isManager; // hides the add-member form for Employees
        ViewData["Departments"] = await _departmentService.GetDepartmentsAsync(organizationId); // for the add-member dropdown

        // Only managers need to see who's waiting on approval.
        ViewData["PendingMembers"] = isManager
            ? await _memberService.GetPendingMembersAsync(organizationId)
            : new List<OrganizationMember>();

        return View(members);
    }

    // GET /Members/Details/{membershipId} — one employee's profile. Manager-only:
    // this is a management tool (edit department/role/hours), not something
    // Employees need to click into for each other.
    [HttpGet]
    public async Task<IActionResult> Details(Guid membershipId, Guid organizationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _memberService.IsManagerAsync(organizationId, userId))
        {
            return Forbid();
        }

        var member = await _memberService.GetMemberByIdAsync(membershipId);
        if (member is null || member.OrganizationId != organizationId)
        {
            return NotFound();
        }

        var shifts = await _schedulingService.GetShiftsForMemberAsync(member.Id);
        var upcoming = shifts.Where(s => s.StartsAtUtc.Date >= DateTime.UtcNow.Date).Take(5).ToList();

        ViewData["OrganizationId"] = organizationId;
        return View(new MemberDetailsViewModel
        {
            Member = member,
            Departments = await _departmentService.GetDepartmentsAsync(organizationId),
            UpcomingShifts = upcoming
        });
    }

    // POST /Members/Update — a manager edits someone's department, role, or hour limit.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid membershipId, Guid organizationId, Guid? departmentId, OrganizationRole role, int maxWeeklyHours)
    {
        var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _memberService.UpdateMemberAsync(membershipId, actingUserId, departmentId, role, maxWeeklyHours);

        if (result == MemberService.MembershipActionResult.NotAuthorized)
        {
            return Forbid();
        }

        TempData["Success"] = result == MemberService.MembershipActionResult.Updated
            ? "Employee details updated."
            : null;

        return RedirectToAction("Details", new { membershipId, organizationId });
    }

    // POST /Members/Approve — an Owner/Manager approves a pending join request.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid membershipId, Guid organizationId)
    {
        var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _memberService.ApproveMembershipAsync(membershipId, actingUserId);

        if (result == MemberService.MembershipActionResult.NotAuthorized)
        {
            return Forbid();
        }

        TempData["Success"] = result == MemberService.MembershipActionResult.Approved
            ? "Request approved."
            : null;

        return RedirectToAction("Index", new { organizationId });
    }

    // POST /Members/Reject — an Owner/Manager rejects (deletes) a pending request.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid membershipId, Guid organizationId)
    {
        var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _memberService.RejectMembershipAsync(membershipId, actingUserId);

        if (result == MemberService.MembershipActionResult.NotAuthorized)
        {
            return Forbid();
        }

        TempData["Success"] = result == MemberService.MembershipActionResult.Rejected
            ? "Request rejected."
            : null;

        return RedirectToAction("Index", new { organizationId });
    }

    // POST /Members/Remove — an Owner/Manager removes an active member from the org.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid membershipId, Guid organizationId)
    {
        var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _memberService.RemoveMemberAsync(membershipId, actingUserId);

        if (result == MemberService.MembershipActionResult.NotAuthorized)
        {
            return Forbid();
        }

        TempData["Error"] = result == MemberService.MembershipActionResult.CannotRemoveOwner
            ? "The organization's owner can't be removed."
            : null;

        TempData["Success"] = result == MemberService.MembershipActionResult.Removed
            ? "Member removed."
            : null;

        return RedirectToAction("Index", new { organizationId });
    }

    // POST /Members/Add — add someone to the org. Only an Owner/Manager of
    // THIS org may succeed; MemberService enforces that, this method just
    // translates the result into the right HTTP response.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddMemberViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a valid email and role.";
            return RedirectToAction("Index", new { organizationId = model.OrganizationId });
        }

        var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _memberService.AddMemberAsync(model.OrganizationId, actingUserId, model.Email, model.Role, model.DepartmentId);

        switch (result)
        {
            case MemberService.AddMemberResult.NotAuthorized:
                return Forbid(); // real 403-style rejection — not just a hidden button

            case MemberService.AddMemberResult.UserNotFound:
                TempData["Error"] = "No registered user with that email.";
                break;

            case MemberService.AddMemberResult.AlreadyMember:
                TempData["Error"] = "That person is already a member.";
                break;

            case MemberService.AddMemberResult.Added:
                TempData["Success"] = $"Added {model.Email} to the organization.";
                break;
        }

        return RedirectToAction("Index", new { organizationId = model.OrganizationId });
    }
}
