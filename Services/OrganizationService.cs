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

    public enum JoinResult
    {
        Joined,
        AlreadyMember,
        InvalidCode
    }

    public async Task<Organization> CreateOrganizationAsync(string name, string userId)
    {
        // whoever created the organization now its role is the owner
        var organization = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            CreatedAtUtc = DateTime.UtcNow,
            JoinCode = GenerateJoinCode()
        };

        var member = new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organization.Id,
            UserId = userId,
            Role = OrganizationRole.Owner,
            JoinedAtUtc = DateTime.UtcNow
        };

        _db.Organizations.Add(organization); // added ut to the DBContext, but not yet saved to the database
        _db.OrganizationMembers.Add(member);

        // One transaction: both rows land, or neither does.
        await _db.SaveChangesAsync();

        return organization;
    }
    public async Task<List<OrganizationMember>> GetMembershipsForUserAsync(string userId)
    {
        return await _db.OrganizationMembers //FROM OrganizationMembers
            .Where(m => m.UserId == userId)// WHERE UserId = userId
            .Include(m => m.Organization) // INNER JOIN Organizations ON OrganizationMembers.OrganizationId = Organizations.Id
            .OrderBy(m => m.Organization.Name) // Order by organization name
            .ToListAsync(); //Run it and read every tow

    }

    // Self-service join: anyone with a valid code lands as an Employee —
    // matches the "company access code" pattern real scheduling apps use,
    // instead of a manager needing to know your email address in advance.
    public async Task<JoinResult> JoinOrganizationAsync(string joinCode, string userId)
    {
        // Normalize so "abc123xy", " ABC123XY ", etc. all match the same code —
        // people will be copy-pasting or typing this by hand.
        var normalizedCode = joinCode.Trim().ToUpperInvariant();

        var organization = await _db.Organizations
            .FirstOrDefaultAsync(o => o.JoinCode == normalizedCode);

        if (organization is null)
        {
            return JoinResult.InvalidCode;
        }

        bool alreadyMember = await _db.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == organization.Id && m.UserId == userId);
        if (alreadyMember)
        {
            return JoinResult.AlreadyMember;
        }

        _db.OrganizationMembers.Add(new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organization.Id,
            UserId = userId,
            Role = OrganizationRole.Employee,
            JoinedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return JoinResult.Joined;
    }

    private static string GenerateJoinCode()
    {
        // No ambiguous-looking characters (0/O, 1/I) — this gets read aloud
        // or typed by hand, so avoid characters people can't tell apart.
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
