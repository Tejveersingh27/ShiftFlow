using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Models.Entities;

namespace ShiftFlow.Services;

public class DepartmentService
{
    private readonly ApplicationDbContext _db;
    private readonly MemberService _memberService;
    private readonly ILogger<DepartmentService> _logger;

    // Outcome of trying to create a department — same "enum of outcomes" pattern
    // MemberService.AddMemberResult used.
    public enum CreateDepartmentResult
    {
        Created,
        NotAuthorized,
        DuplicateName
    }

    public DepartmentService(ApplicationDbContext db, MemberService memberService, ILogger<DepartmentService> logger)
    {
        _db = db;
        _memberService = memberService;
        _logger = logger;
    }

    public async Task<List<Department>> GetDepartmentsAsync(Guid organizationId)
    {
        return await _db.Departments
            .Where(d => d.OrganizationId == organizationId)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<CreateDepartmentResult> CreateDepartmentAsync(Guid organizationId, string actingUserId, string name)
    {
        // Same authorization check as MemberService.AddMemberAsync — pulled out
        // into MemberService.IsManagerAsync so it isn't copy-pasted per service.
        if (!await _memberService.IsManagerAsync(organizationId, actingUserId))
        {
            _logger.LogWarning("User {UserId} is not authorized to create departments in organization {OrganizationId}", actingUserId, organizationId);
            return CreateDepartmentResult.NotAuthorized;
        }

        bool duplicate = await _db.Departments
            .AnyAsync(d => d.OrganizationId == organizationId && d.Name == name);

        if (duplicate)
        {
            _logger.LogWarning("Department name {Name} already exists in organization {OrganizationId}", name, organizationId);
            return CreateDepartmentResult.DuplicateName;
        }

        _db.Departments.Add(new Department
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            Name = name
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("Department {Name} created in organization {OrganizationId}", name, organizationId);
        return CreateDepartmentResult.Created;
    }
}
