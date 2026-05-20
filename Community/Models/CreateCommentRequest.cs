namespace Community.Models;

public class CreateCommentRequest
{
    public string AuthorMemberId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}