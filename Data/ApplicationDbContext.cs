using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShiftFlow.Models.Entities;

namespace ShiftFlow.Data;

// IdentityDbContext<ApplicationUser> gives us the Users/Roles/Claims tables
// for free. As we build each vertical slice, we'll add a DbSet<T> here for
// each new entity (Organization, Shift, etc.) — nothing beyond
// ApplicationUser exists yet, on purpose, per the spec's "don't create all
// entities up front" rule.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // This tells EF core that these two classes are tables too !
    
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Department> Departments => Set<Department>(); // This is table too
    //dotnet ef migrations add AddDepartments then dotnet ef database update.

    public DbSet<Schedule> Schedules => Set<Schedule>();
public DbSet<Shift> Shifts => Set<Shift>();

}
