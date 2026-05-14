namespace Community.Models;

public class Comment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AuthorUserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}