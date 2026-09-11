using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Models.Entities;

namespace ShiftFlow.Services;

public class MemberService
{
    private readonly ApplicationDbContext _db;

    // Outcome of trying to add a member. Only Added is success —
    // the other three are different reasons it can fail.
    public enum AddMemberResult
    {
        Added,
        AlreadyMember,
        NotAuthorized,
        UserNotFound
    }

    public MemberService(ApplicationDbContext db) // the constructor takes in an ApplicationDbContext
    {
        _db = db; // framework calls this constructor
    }

    // Is this user a member (any role) of this org? Used to guard the team page itself —
    // otherwise a logged-in user could view ANY org's member list just by knowing its id.
    public async Task<bool> IsMemberAsync(Guid organizationId, string userId)
    {
        return await _db.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == organizationId && m.UserId == userId);
    }

    // Which role does this user hold in this org? Null if they're not a member at all.
    // Used where "any member" isn't specific enough — e.g. Employees should see
    // less than Owners/Managers on the Schedules pages.
    public async Task<OrganizationRole?> GetRoleAsync(Guid organizationId, string userId)
    {
        var membership = await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId);
        return membership?.Role;
    }

    public async Task<List<OrganizationMember>> GetMembersAsync(Guid organizationId) // return all the members
    {
        return await _db.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId)
            .Include(m => m.User) // JOIN Users ON OrganizationMembers.UserId = Users.Id
            .OrderBy(m => m.User.UserName)
            .ToListAsync();
    }

    public async Task<AddMemberResult> AddMemberAsync(Guid organizationId, string actingUserId, string email, OrganizationRole role)
    {
        // Is the person doing this an Owner or Manager of THIS org?
        var actingMembership = await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == actingUserId);

        if (actingMembership is null || actingMembership.Role == OrganizationRole.Employee)
        {
            return AddMemberResult.NotAuthorized;
        }

        // Does a registered user with this email exist?
        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (targetUser is null)
        {
            return AddMemberResult.UserNotFound;
        }

        // Are they already a member of this org?
        bool alreadyMember = await _db.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == organizationId && m.UserId == targetUser.Id);
        if (alreadyMember)
        {
            return AddMemberResult.AlreadyMember;
        }

        // All checks passed — add them.
        _db.OrganizationMembers.Add(new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            UserId = targetUser.Id,
            Role = role,
            JoinedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return AddMemberResult.Added;
    }
}
