using ShiftFlow.Data;
using ShiftFlow.Models.Entities;

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
}
