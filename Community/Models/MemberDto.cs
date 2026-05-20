namespace Community.Models;

public class MemberDto
{
    public Guid Id { get; set; }

    public Guid HomeCenterId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}