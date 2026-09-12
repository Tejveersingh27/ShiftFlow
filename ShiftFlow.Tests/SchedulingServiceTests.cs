using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Models.Entities;
using ShiftFlow.Services;

namespace ShiftFlow.Tests;

public class SchedulingServiceTests
{
    private static ApplicationDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    // Shared setup: one org, one Owner (the acting manager) and one Employee
    // (the person being assigned), one Draft schedule. Returns the pieces each
    // test needs so it isn't repeated in every test method.
    private static async Task<(ApplicationDbContext Db, SchedulingService Service, Guid OrgId, string OwnerId, Guid EmployeeMemberId, Guid ScheduleId)>
        SeedAsync()
    {
        var db = CreateInMemoryDb();
        var service = new SchedulingService(db);

        var orgId = Guid.CreateVersion7();
        var ownerId = "owner-user";
        var employeeMemberId = Guid.CreateVersion7();

        db.Organizations.Add(new Organization { Id = orgId, Name = "Test Co", CreatedAtUtc = DateTime.UtcNow });
        db.OrganizationMembers.Add(new OrganizationMember
        {
            Id = Guid.CreateVersion7(), OrganizationId = orgId, UserId = ownerId,
            Role = OrganizationRole.Owner, JoinedAtUtc = DateTime.UtcNow
        });
        db.OrganizationMembers.Add(new OrganizationMember
        {
            Id = employeeMemberId, OrganizationId = orgId, UserId = "employee-user",
            Role = OrganizationRole.Employee, JoinedAtUtc = DateTime.UtcNow
        });

        var scheduleId = Guid.CreateVersion7();
        db.Schedules.Add(new Schedule
        {
            Id = scheduleId, OrganizationId = orgId,
            WeekStartDate = new DateOnly(2026, 9, 14), Status = ScheduleStatus.Draft
        });
        await db.SaveChangesAsync();

        return (db, service, orgId, ownerId, employeeMemberId, scheduleId);
    }

    [Fact]
    public async Task AssignEmployeeAsync_WithOverlappingShift_IsRejected()
    {
        // Arrange: the employee is already assigned Mon 9am-5pm.
        var (db, service, _, ownerId, employeeMemberId, scheduleId) = await SeedAsync();

        var existingShift = new Shift
        {
            Id = Guid.CreateVersion7(), ScheduleId = scheduleId,
            StartsAtUtc = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc),
            EndsAtUtc = new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Utc),
            AssignedMemberId = employeeMemberId
        };
        var overlappingShift = new Shift
        {
            Id = Guid.CreateVersion7(), ScheduleId = scheduleId,
            StartsAtUtc = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc), // overlaps: noon-8pm
            EndsAtUtc = new DateTime(2026, 9, 14, 20, 0, 0, DateTimeKind.Utc)
        };
        db.Shifts.AddRange(existingShift, overlappingShift);
        await db.SaveChangesAsync();

        // Act: try to assign the SAME employee to the overlapping shift.
        var result = await service.AssignEmployeeAsync(overlappingShift.Id, ownerId, employeeMemberId);

        // Assert: rejected, and the shift stays unassigned.
        Assert.Equal(SchedulingService.AssignmentResult.OverlappingShift, result);
        var reloaded = await db.Shifts.FindAsync(overlappingShift.Id);
        Assert.Null(reloaded!.AssignedMemberId);
    }

    [Fact]
    public async Task AssignEmployeeAsync_WithNoOverlap_Succeeds()
    {
        // Arrange: the employee is assigned Mon 9am-5pm; the new shift is Tue 9am-5pm — no overlap.
        var (db, service, _, ownerId, employeeMemberId, scheduleId) = await SeedAsync();

        var existingShift = new Shift
        {
            Id = Guid.CreateVersion7(), ScheduleId = scheduleId,
            StartsAtUtc = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc),
            EndsAtUtc = new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Utc),
            AssignedMemberId = employeeMemberId
        };
        var nextDayShift = new Shift
        {
            Id = Guid.CreateVersion7(), ScheduleId = scheduleId,
            StartsAtUtc = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc),
            EndsAtUtc = new DateTime(2026, 9, 15, 17, 0, 0, DateTimeKind.Utc)
        };
        db.Shifts.AddRange(existingShift, nextDayShift);
        await db.SaveChangesAsync();

        // Act
        var result = await service.AssignEmployeeAsync(nextDayShift.Id, ownerId, employeeMemberId);

        // Assert
        Assert.Equal(SchedulingService.AssignmentResult.Assigned, result);
        var reloaded = await db.Shifts.FindAsync(nextDayShift.Id);
        Assert.Equal(employeeMemberId, reloaded!.AssignedMemberId);
    }

    [Fact]
    public async Task AssignEmployeeAsync_ExceedingWeeklyHours_IsRejected()
    {
        // Arrange: the employee already has 30 hours this week (3 shifts of
        // 10 hours, none overlapping each other or the new one). Their limit
        // is the default 40. The new shift is 15 hours -> 30 + 15 = 45 > 40.
        var (db, service, _, ownerId, employeeMemberId, scheduleId) = await SeedAsync();

        Shift TenHourShift(int day) => new()
        {
            Id = Guid.CreateVersion7(), ScheduleId = scheduleId,
            StartsAtUtc = new DateTime(2026, 9, day, 0, 0, 0, DateTimeKind.Utc),
            EndsAtUtc = new DateTime(2026, 9, day, 10, 0, 0, DateTimeKind.Utc),
            AssignedMemberId = employeeMemberId
        };

        var newShift = new Shift
        {
            Id = Guid.CreateVersion7(), ScheduleId = scheduleId,
            StartsAtUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc),
            EndsAtUtc = new DateTime(2026, 9, 17, 15, 0, 0, DateTimeKind.Utc) // 15 hours, non-overlapping
        };

        db.Shifts.AddRange(TenHourShift(14), TenHourShift(15), TenHourShift(16), newShift);
        await db.SaveChangesAsync();

        // Act
        var result = await service.AssignEmployeeAsync(newShift.Id, ownerId, employeeMemberId);

        // Assert
        Assert.Equal(SchedulingService.AssignmentResult.WeeklyHourLimitExceeded, result);
        var reloaded = await db.Shifts.FindAsync(newShift.Id);
        Assert.Null(reloaded!.AssignedMemberId);
    }

    [Fact]
    public async Task AssignEmployeeAsync_ByEmployee_IsNotAuthorized()
    {
        // Arrange
        var (db, service, _, _, employeeMemberId, scheduleId) = await SeedAsync();

        var shift = new Shift
        {
            Id = Guid.CreateVersion7(), ScheduleId = scheduleId,
            StartsAtUtc = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc),
            EndsAtUtc = new DateTime(2026, 9, 14, 17, 0, 0, DateTimeKind.Utc)
        };
        db.Shifts.Add(shift);
        await db.SaveChangesAsync();

        // Act: "employee-user" (an Employee, not a Manager/Owner) tries to assign.
        var result = await service.AssignEmployeeAsync(shift.Id, "employee-user", employeeMemberId);

        // Assert
        Assert.Equal(SchedulingService.AssignmentResult.NotAuthorized, result);
    }
}
