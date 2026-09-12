namespace ShiftFlow.Models.Entities;

// ENTITY : DB Table written as C# class

// One business using ShiftFlow (a coffee shop, a warehouse, etc.).
// This class IS the "Organizations" database table — one object = one row.
public class Organization
{
    public Guid Id { get; set; } // Primary key !
    

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    // Shared with people you want to invite — they use this to join instead
    // of you needing to know their email address ahead of time.
    public string JoinCode { get; set; } = string.Empty;

    // "This organization has many members." EF uses this so I can write
    // organization.Members instead of writing a manual SQL join.
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
}
