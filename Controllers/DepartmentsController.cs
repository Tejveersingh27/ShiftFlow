using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Models.Entities;
using ShiftFlow.Services;
using ShiftFlow.ViewModels;

namespace ShiftFlow.Controllers;

[Authorize]
public class DepartmentsController : Controller
{
    private readonly DepartmentService _departmentService;
    private readonly MemberService _memberService; // reused just for IsMemberAsync — the same
                                                     // "must belong to this org to view it" guard
                                                     // MembersController.Index uses.

    public DepartmentsController(DepartmentService departmentService, MemberService memberService)
    {
        _departmentService = departmentService;
        _memberService = memberService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid organizationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var role = await _memberService.GetRoleAsync(organizationId, userId);
        if (role is null)
        {
            return Forbid();
        }

        var departments = await _departmentService.GetDepartmentsAsync(organizationId);
        ViewData["OrganizationId"] = organizationId;
        ViewData["IsManager"] = role != OrganizationRole.Employee; // hides the add-department form for Employees
        return View(departments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDepartmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a department name (2-50 characters).";
            return RedirectToAction("Index", new { organizationId = model.OrganizationId });
        }

        var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var result = await _departmentService.CreateDepartmentAsync(model.OrganizationId, actingUserId, model.Name);

        switch (result)
        {
            case DepartmentService.CreateDepartmentResult.NotAuthorized:
                return Forbid();

            case DepartmentService.CreateDepartmentResult.DuplicateName:
                TempData["Error"] = "A department with that name already exists.";
                break;

            case DepartmentService.CreateDepartmentResult.Created:
                TempData["Success"] = $"Department \"{model.Name}\" created.";
                break;
        }

        return RedirectToAction("Index", new { organizationId = model.OrganizationId });
    }
}
