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

    public MembersController(MemberService memberService)
    {
        _memberService = memberService;
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
        ViewData["IsManager"] = role != OrganizationRole.Employee; // hides the add-member form for Employees
        return View(members);
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

        var result = await _memberService.AddMemberAsync(model.OrganizationId, actingUserId, model.Email, model.Role);

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
