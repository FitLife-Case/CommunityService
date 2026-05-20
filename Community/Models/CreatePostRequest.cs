namespace Community.Models;

public class CreatePostRequest
{
    public string AuthorMemberId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}