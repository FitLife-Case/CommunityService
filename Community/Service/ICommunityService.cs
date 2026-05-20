using Community.Models;

namespace Community.Service;

public interface ICommunityService
{
    Task<List<Post>> GetGlobalPostsAsync();

    Task<List<Post>> GetCenterPostsAsync(string centerId);

    Task CreateGlobalPostAsync(
        string authorDisplayName,
        CreatePostRequest request);

    Task CreateCenterPostAsync(
        string authorDisplayName,
        string centerId,
        CreatePostRequest request);

    Task AddCommentAsync(
        string postId,
        string authorDisplayName,
        CreateCommentRequest request);
}