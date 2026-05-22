using Community.Models;

namespace Community.Repository;

public interface ICommunityRepository
{
    Task<List<Post>> GetGlobalPostsAsync();

    Task<List<Post>> GetCenterPostsAsync(string centerId);

    Task<Post?> GetPostByIdAsync(string postId);

    Task CreatePostAsync(Post post);

    Task AddCommentAsync(string postId, Comment comment);

    Task DeletePostAsync(string postId);
}