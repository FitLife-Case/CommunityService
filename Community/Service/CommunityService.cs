using Community.Models;
using Community.Repository;
using Microsoft.Extensions.Caching.Memory;

namespace Community.Service;

public class CommunityService : ICommunityService
{
    private const string GlobalPostsCacheKey = "community-global-posts";
    private const string CenterPostsCacheKeyPrefix = "community-center-posts-";

    private readonly ICommunityRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CommunityService> _logger;

    public CommunityService(
        ICommunityRepository repository,
        IMemoryCache cache,
        ILogger<CommunityService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Post>> GetGlobalPostsAsync()
    {
        if (_cache.TryGetValue(GlobalPostsCacheKey, out List<Post>? cachedPosts))
        {
            _logger.LogInformation("Global posts returned from cache");
            return cachedPosts!;
        }

        var posts = await _repository.GetGlobalPostsAsync();

        _cache.Set(GlobalPostsCacheKey, posts, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Global posts returned from MongoDB and cached");

        return posts;
    }

    public async Task<List<Post>> GetCenterPostsAsync(string centerId)
    {
        var cacheKey = GetCenterPostsCacheKey(centerId);

        if (_cache.TryGetValue(cacheKey, out List<Post>? cachedPosts))
        {
            _logger.LogInformation("Center posts for {CenterId} returned from cache", centerId);
            return cachedPosts!;
        }

        var posts = await _repository.GetCenterPostsAsync(centerId);

        _cache.Set(cacheKey, posts, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Center posts for {CenterId} returned from MongoDB and cached", centerId);

        return posts;
    }

    public async Task CreateGlobalPostAsync(string authorDisplayName, CreatePostRequest request)
    {
        ValidateCreatePostRequest(request);

        var post = new Post
        {
            AuthorUserId = request.AuthorMemberId,
            AuthorMemberId = request.AuthorMemberId,
            AuthorDisplayName = authorDisplayName,
            Scope = CommunityScope.Global,
            Title = request.Title.Trim(),
            Content = request.Content.Trim()
        };

        await _repository.CreatePostAsync(post);

        _cache.Remove(GlobalPostsCacheKey);

        _logger.LogInformation(
            "Global post created by member {AuthorMemberId} as {AuthorDisplayName}",
            request.AuthorMemberId,
            authorDisplayName);
    }

    public async Task CreateCenterPostAsync(string authorDisplayName, string centerId, CreatePostRequest request)
    {
        ValidateCreatePostRequest(request);

        if (string.IsNullOrWhiteSpace(centerId))
        {
            throw new ArgumentException("CenterId is required");
        }

        var post = new Post
        {
            AuthorUserId = request.AuthorMemberId,
            AuthorMemberId = request.AuthorMemberId,
            AuthorDisplayName = authorDisplayName,
            CenterId = centerId,
            Scope = CommunityScope.Center,
            Title = request.Title.Trim(),
            Content = request.Content.Trim()
        };

        await _repository.CreatePostAsync(post);

        _cache.Remove(GetCenterPostsCacheKey(centerId));

        _logger.LogInformation(
            "Center post created by member {AuthorMemberId} as {AuthorDisplayName} for center {CenterId}",
            request.AuthorMemberId,
            authorDisplayName,
            centerId);
    }

    public async Task AddCommentAsync(string postId, string authorDisplayName, CreateCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            throw new ArgumentException("PostId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Comment content is required");
        }

        var post = await _repository.GetPostByIdAsync(postId);

        if (post is null)
        {
            throw new ArgumentException("Post was not found");
        }

        var comment = new Comment
        {
            AuthorUserId = request.AuthorMemberId,
            AuthorMemberId = request.AuthorMemberId,
            AuthorDisplayName = authorDisplayName,
            Content = request.Content.Trim()
        };

        await _repository.AddCommentAsync(postId, comment);

        RemovePostCache(post);

        _logger.LogInformation(
            "Comment added to post {PostId} by member {AuthorMemberId} as {AuthorDisplayName}",
            postId,
            request.AuthorMemberId,
            authorDisplayName);
    }

    public async Task DeletePostAsync(string postId)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            throw new ArgumentException("PostId is required");
        }

        var post = await _repository.GetPostByIdAsync(postId);

        if (post is null)
        {
            throw new ArgumentException("Post was not found");
        }

        await _repository.DeletePostAsync(postId);

        RemovePostCache(post);

        _logger.LogInformation("Post {PostId} deleted by admin", postId);
    }

    private void RemovePostCache(Post post)
    {
        if (post.Scope == CommunityScope.Global)
        {
            _cache.Remove(GlobalPostsCacheKey);
            return;
        }

        if (post.Scope == CommunityScope.Center &&
            !string.IsNullOrWhiteSpace(post.CenterId))
        {
            _cache.Remove(GetCenterPostsCacheKey(post.CenterId));
        }
    }

    private static string GetCenterPostsCacheKey(string centerId)
    {
        return $"{CenterPostsCacheKeyPrefix}{centerId}";
    }

    private static void ValidateCreatePostRequest(CreatePostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Content is required");
        }
    }
}