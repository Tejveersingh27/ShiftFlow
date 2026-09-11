using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Models.Entities;

namespace ShiftFlow.Services;

public class SchedulingService
{
    private readonly ApplicationDbContext _db;

    public enum ScheduleResult
    {
        Created,
        NotAuthorized,
         ScheduleNotFound,
    InvalidTimeRange
    }

    public SchedulingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Schedule>> GetSchedulesAsync(Guid organizationId)
    {
        return await _db.Schedules
            .Where(s => s.OrganizationId == organizationId)
            .OrderByDescending(s => s.WeekStartDate)
            .ToListAsync();
    }

    // Loads one schedule together with its shifts, and each shift's assigned
    // person (if any) — three levels deep, via Include + ThenInclude.
    public async Task<Schedule?> GetScheduleWithShiftsAsync(Guid scheduleId)
    {
        return await _db.Schedules
            .Include(s => s.Shifts)
                .ThenInclude(shift => shift.AssignedMember)
                    .ThenInclude(member => member!.User)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
    }

    // creates anew empty weekly schedule
    public async Task<ScheduleResult> CreateScheduleAsync(Guid organizationId, string actingUserId, DateOnly weekStartDate)
    {
        // Same Owner/Manager check as DepartmentService.CreateDepartmentAsync
        // and MemberService.AddMemberAsync.
        var actingMembership = await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == actingUserId);

        if (actingMembership is null || actingMembership.Role == OrganizationRole.Employee)
        {
            return ScheduleResult.NotAuthorized;
        }
        // checks if the requester is amnager/owner ie not an employee

// imsert one schedule row into the database, with no shifts yet. The weekStartDate is the Monday of the week this schedule covers.

        _db.Schedules.Add(new Schedule
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            WeekStartDate = weekStartDate,
            Status = ScheduleStatus.Draft
        });
        await _db.SaveChangesAsync();

        return ScheduleResult.Created;
    }
    // This adds a timeslot (unassigned) to an existing schedule. The schedule must exist, and the requester must be an Owner or Manager of the org that owns the schedule. 
        public async Task<ScheduleResult> CreateShiftAsync(Guid scheduleId, string actingUserId, DateTime startsAtUtc, DateTime endsAtUtc)
    {
        // Same Owner/Manager check as DepartmentService.CreateDepartmentAsync
        // and MemberService.AddMemberAsync.
          var schedule = await _db.Schedules.FindAsync(scheduleId);
        if (schedule is null)
        {
            return ScheduleResult.ScheduleNotFound;
        }

        var actingMembership = await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == schedule.OrganizationId && m.UserId == actingUserId);

        if (actingMembership is null || actingMembership.Role == OrganizationRole.Employee)
        {
            return ScheduleResult.NotAuthorized;
        }

        if (startsAtUtc >= endsAtUtc)
        {
            return ScheduleResult.InvalidTimeRange;
        }

        _db.Shifts.Add(new Shift
        {
            Id = Guid.CreateVersion7(),
            ScheduleId = scheduleId,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc
        });
        await _db.SaveChangesAsync();

        return ScheduleResult.Created;
    }
    // Flips a schedule from Draft to Published — now employees see it as official.
    public async Task<ScheduleResult> PublishScheduleAsync(Guid scheduleId, string actingUserId)
    {
        var schedule = await _db.Schedules.FindAsync(scheduleId);
        if (schedule is null) return ScheduleResult.ScheduleNotFound;

        var actingMembership = await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == schedule.OrganizationId && m.UserId == actingUserId);
        if (actingMembership is null || actingMembership.Role == OrganizationRole.Employee)
            return ScheduleResult.NotAuthorized;

        // `schedule` was already loaded above, so EF is already tracking it.
        // Just change the field and save — no .Add() needed. This becomes an UPDATE, not an INSERT.
        schedule.Status = ScheduleStatus.Published;
        await _db.SaveChangesAsync();

        return ScheduleResult.Created;
    }
}
