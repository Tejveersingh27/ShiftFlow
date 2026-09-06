namespace ShiftFlow.Models.Entities;

// One business using ShiftFlow (a coffee shop, a warehouse, etc.).
// This class IS the "Organizations" database table — one object = one row.
public class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    // "This organization has many members." EF uses this so I can write
    // organization.Members instead of writing a manual SQL join.
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
}
