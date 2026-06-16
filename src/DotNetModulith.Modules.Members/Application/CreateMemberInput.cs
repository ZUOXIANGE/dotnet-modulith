namespace DotNetModulith.Modules.Members.Application;

public sealed record CreateMemberInput(
    string Name,
    string Phone,
    string Email,
    string Address,
    string MembershipType,
    DateOnly JoinDate,
    DateOnly? ExpiryDate);
