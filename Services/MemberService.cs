using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Models.Entities;

namespace ShiftFlow.Services;

public class MemberService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MemberService> _logger;
    // Outcome of trying to add a member. Only Added is success —
    // the other three are different reasons it can fail.
    public enum AddMemberResult
    {
        Added,
        AlreadyMember,
        NotAuthorized,
        UserNotFound
    }

    public MemberService(ApplicationDbContext db, ILogger<MemberService> logger) // the constructor takes in an ApplicationDbContext
    {
        _db = db; // framework calls this constructor
        _logger = logger; // store the logger instance
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

    // The one check every "can this person create/change things in this org"
    // decision boils down to: are they a member at all, and if so, not an Employee.
    // Owner and Manager are treated identically everywhere today, so this is a
    // single yes/no gate rather than returning the specific role.
    public async Task<bool> IsManagerAsync(Guid organizationId, string userId)
    {
        var role = await GetRoleAsync(organizationId, userId);
        return role is OrganizationRole.Owner or OrganizationRole.Manager;
    }

    public async Task<List<OrganizationMember>> GetMembersAsync(Guid organizationId) // return all the members
    {
        return await _db.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId)
            .Include(m => m.User) // JOIN Users ON OrganizationMembers.UserId = Users.Id
            .Include(m => m.Department) // JOIN Departments ON OrganizationMembers.DepartmentId = Departments.Id (may be null)
            .OrderBy(m => m.User.UserName)
            .ToListAsync();
    }

    public async Task<AddMemberResult> AddMemberAsync(Guid organizationId, string actingUserId, string email, OrganizationRole role, Guid? departmentId = null)
    {
        // Is the person doing this an Owner or Manager of THIS org?
        if (!await IsManagerAsync(organizationId, actingUserId))
        {
            _logger.LogWarning("User {UserId} is not authorized to add members to organization {OrganizationId}", actingUserId, organizationId);
            return AddMemberResult.NotAuthorized;
        }

        // Does a registered user with this email exist?
        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (targetUser is null)
        {
            _logger.LogWarning("User with email {Email} not found", email);
            return AddMemberResult.UserNotFound;
        }

        // Are they already a member of this org?
        bool alreadyMember = await _db.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == organizationId && m.UserId == targetUser.Id);
        if (alreadyMember)
        {   
            _logger.LogWarning("User with email {Email} is already a member of organization {OrganizationId}", email, organizationId);
            return AddMemberResult.AlreadyMember;
        }

        // All checks passed — add them.
        _db.OrganizationMembers.Add(new OrganizationMember
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            UserId = targetUser.Id,
            Role = role,
            JoinedAtUtc = DateTime.UtcNow,
            DepartmentId = departmentId
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {Email} added to organization {OrganizationId} as {Role}", email, organizationId, role);
        return AddMemberResult.Added;
    }
}
