using Community.Models;

namespace Community.Service;

public interface ICommunityService
{
    Task<List<Post>> GetGlobalPostsAsync();

    Task<List<Post>> GetCenterPostsAsync(string centerId);

    Task CreateGlobalPostAsync(
        string authorUserId,
        CreatePostRequest request);

    Task CreateCenterPostAsync(
        string authorUserId,
        string centerId,
        CreatePostRequest request);

    Task AddCommentAsync(
        string postId,
        string authorUserId,
        CreateCommentRequest request);
}