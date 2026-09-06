using Microsoft.AspNetCore.Identity;

namespace ShiftFlow.Models.Entities;

// ApplicationUser represents the authenticated user, per the spec.
// IdentityUser already gives us Email, PasswordHash, etc. — we inherit
// from it so we can add ShiftFlow-specific fields later (first/last name,
// for example) without touching Identity's own plumbing.
public class ApplicationUser : IdentityUser
{
}
