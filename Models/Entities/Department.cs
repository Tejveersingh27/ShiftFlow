namespace ShiftFlow.Models.Entities;

// A team within one organization (Kitchen, Front End, Warehouse...).
// Same "belongs to one Organization" shape as OrganizationMember.
public class Department
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
}
