using ShiftFlow.Data;
using ShiftFlow.Models.Entities;
using Microsoft.EntityFrameworkCore;
// This does the real work !

namespace ShiftFlow.Services;

// Business logic for organizations.
// Rule enforced here: whoever creates an organization becomes its Owner.
public class OrganizationService
{
    private readonly ApplicationDbContext _db;

    public OrganizationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Organization> CreateOrganizationAsync(string name, string userId)
    {
        var organization = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            CreatedAtUtc = DateTime.UtcNow
        };

        var member = new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organization.Id,
            UserId = userId,
            Role = OrganizationRole.Owner,
            JoinedAtUtc = DateTime.UtcNow
        };

        _db.Organizations.Add(organization);
        _db.OrganizationMembers.Add(member);

        // One transaction: both rows land, or neither does.
        await _db.SaveChangesAsync();

        return organization;
    }
    public async Task<List<OrganizationMember>> GetMembershipsForUserAsync(string userId)
    {
        return await _db.OrganizationMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Organization) // Include the related Organization entity
            .OrderBy(m => m.Organization.Name) // Order by organization name
            .ToListAsync();
            
    }
}
