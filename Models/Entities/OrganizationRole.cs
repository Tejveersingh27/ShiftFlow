namespace ShiftFlow.Models.Entities;

// A user's role WITHIN one organization (not a global role).
// Stored in the database as the number (0/1/2). The numbers are pinned
// explicitly so reordering this list later can't change existing rows.
public enum OrganizationRole
{
    Owner = 0,
    Manager = 1,
    Employee = 2
}
