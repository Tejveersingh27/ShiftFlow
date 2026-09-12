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

    public MembersController(MemberService memberService, DepartmentService departmentService)
    {
        _memberService = memberService;
        _departmentService = departmentService;
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
