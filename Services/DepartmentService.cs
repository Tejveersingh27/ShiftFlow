using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Models.Entities;

namespace ShiftFlow.Services;

public class DepartmentService
{
    private readonly ApplicationDbContext _db;

    // Outcome of trying to create a department — same "enum of outcomes" pattern
    // MemberService.AddMemberResult used.
    public enum CreateDepartmentResult
    {
        Created,
        NotAuthorized,
        DuplicateName
    }

    public DepartmentService(ApplicationDbContext db)
    {
        _db = db;
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
        // Same authorization check as MemberService.AddMemberAsync: must be an
        // Owner or Manager of THIS org (looked up via OrganizationMembers, the
        // user<->org link table).
        var actingMembership = await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == actingUserId);

        if (actingMembership is null || actingMembership.Role == OrganizationRole.Employee)
        {
            return CreateDepartmentResult.NotAuthorized;
        }

        bool duplicate = await _db.Departments
            .AnyAsync(d => d.OrganizationId == organizationId && d.Name == name);

        if (duplicate)
        {
            return CreateDepartmentResult.DuplicateName;
        }

        _db.Departments.Add(new Department
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            Name = name
        });
        await _db.SaveChangesAsync();

        return CreateDepartmentResult.Created;
    }
}
