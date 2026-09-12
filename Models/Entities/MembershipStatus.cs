namespace ShiftFlow.Models.Entities;

// Active = a real, working member. Pending = requested to join via a join
// code but not yet approved by an Owner/Manager — they can't do or see
// anything in the org until approved.
public enum MembershipStatus
{
    Active,
    Pending
}
