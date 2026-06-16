namespace DotNetModulith.Modules.Members.Application;

public sealed record UpdateMemberInput(
    string Name,
    string Phone,
    string Email,
    string Address,
    string MembershipType,
    DateOnly? ExpiryDate);
