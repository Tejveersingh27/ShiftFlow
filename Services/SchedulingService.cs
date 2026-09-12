using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Models.Entities;

namespace ShiftFlow.Services;

public class SchedulingService
{
    private readonly ApplicationDbContext _db;
    private readonly MemberService _memberService;

    public enum ScheduleResult
    {
        Created,
        NotAuthorized,
         ScheduleNotFound,
    InvalidTimeRange
    }

    public enum AssignmentResult
    {
        Assigned,
        NotAuthorized,
        ShiftNotFound,
        MemberNotInOrganization,
        OverlappingShift,
        WeeklyHourLimitExceeded
    }

    public SchedulingService(ApplicationDbContext db, MemberService memberService)
    {
        _db = db;
        _memberService = memberService;
    }

    // THE conflict-detection check: can this member take this shift?
    // Blocks the assignment if they're already on another shift whose time
    // range overlaps this one — the classic interval-overlap test:
    // two ranges overlap if (startA < endB) AND (startB < endA).
    public async Task<AssignmentResult> AssignEmployeeAsync(Guid shiftId, string actingUserId, Guid memberId)
    {
        var shift = await _db.Shifts.Include(s => s.Schedule).FirstOrDefaultAsync(s => s.Id == shiftId);
        if (shift is null) return AssignmentResult.ShiftNotFound;

        var organizationId = shift.Schedule.OrganizationId;

        if (!await _memberService.IsManagerAsync(organizationId, actingUserId))
            return AssignmentResult.NotAuthorized;

        // The person being assigned must belong to the SAME org as the shift —
        // this is the multi-tenancy rule applied to assignment specifically.
        var targetMember = await _db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == organizationId);
        if (targetMember is null) return AssignmentResult.MemberNotInOrganization;

// We look at every other shift this person has and see if overlaps iwth the shift being assigned. If any do, we block the assignment. This is the classic interval-overlap test: two ranges overlap if (startA < endB) AND (startB < endA).
        bool hasOverlap = await _db.Shifts
            .Where(s => s.AssignedMemberId == memberId && s.Id != shiftId)
            .AnyAsync(s => s.StartsAtUtc < shift.EndsAtUtc && s.EndsAtUtc > shift.StartsAtUtc);

        if (hasOverlap) return AssignmentResult.OverlappingShift;

        // A Schedule represents one week, so "this schedule's shifts" IS
        // "this person's hours this week." Sum their other shifts in this
        // same schedule, add this shift's hours, compare to their limit.
        var otherShiftsThisWeek = await _db.Shifts
            .Where(s => s.AssignedMemberId == memberId && s.Id != shiftId && s.ScheduleId == shift.ScheduleId)
            .ToListAsync();

        double hoursAlreadyScheduled = otherShiftsThisWeek.Sum(s => (s.EndsAtUtc - s.StartsAtUtc).TotalHours);
        double thisShiftHours = (shift.EndsAtUtc - shift.StartsAtUtc).TotalHours;

        if (hoursAlreadyScheduled + thisShiftHours > targetMember.MaxWeeklyHours)
        {
            return AssignmentResult.WeeklyHourLimitExceeded;
        }

        shift.AssignedMemberId = memberId;
        await _db.SaveChangesAsync();

        return AssignmentResult.Assigned;
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
        // and MemberService.AddMemberAsync — via the shared MemberService.IsManagerAsync.
        if (!await _memberService.IsManagerAsync(organizationId, actingUserId))
        {
            return ScheduleResult.NotAuthorized;
        }

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
        var schedule = await _db.Schedules.FindAsync(scheduleId);
        if (schedule is null)
        {
            return ScheduleResult.ScheduleNotFound;
        }

        if (!await _memberService.IsManagerAsync(schedule.OrganizationId, actingUserId))
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

        if (!await _memberService.IsManagerAsync(schedule.OrganizationId, actingUserId))
            return ScheduleResult.NotAuthorized;

        // `schedule` was already loaded above, so EF is already tracking it.
        // Just change the field and save — no .Add() needed. This becomes an UPDATE, not an INSERT.
        schedule.Status = ScheduleStatus.Published;
        await _db.SaveChangesAsync();

        return ScheduleResult.Created;
    }
}
