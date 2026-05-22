using Community.Models;
using MongoDB.Driver;

namespace Community.Repository;

public class CommunityRepository : ICommunityRepository
{
    private readonly IMongoCollection<Post> _posts;
    private readonly ILogger<CommunityRepository> _logger;

    public CommunityRepository(
        IMongoDatabase database,
        ILogger<CommunityRepository> logger)
    {
        _posts = database.GetCollection<Post>("Posts");
        _logger = logger;
    }

    public async Task<List<Post>> GetGlobalPostsAsync()
    {
        return await _posts
            .Find(p => p.Scope == CommunityScope.Global && !p.IsDeleted)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Post>> GetCenterPostsAsync(string centerId)
    {
        return await _posts
            .Find(p =>
                p.Scope == CommunityScope.Center &&
                p.CenterId == centerId &&
                !p.IsDeleted)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Post?> GetPostByIdAsync(string postId)
    {
        return await _posts
            .Find(p => p.Id == postId && !p.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task CreatePostAsync(Post post)
    {
        await _posts.InsertOneAsync(post);

        _logger.LogInformation("Post {PostId} created in MongoDB", post.Id);
    }

    public async Task AddCommentAsync(string postId, Comment comment)
    {
        var update = Builders<Post>.Update.Push(p => p.Comments, comment);

        await _posts.UpdateOneAsync(
            p => p.Id == postId && !p.IsDeleted,
            update);

        _logger.LogInformation("Comment {CommentId} added to post {PostId}", comment.Id, postId);
    }

    public async Task DeletePostAsync(string postId)
    {
        var update = Builders<Post>.Update.Set(p => p.IsDeleted, true);

        await _posts.UpdateOneAsync(
            p => p.Id == postId && !p.IsDeleted,
            update);

        _logger.LogInformation("Post {PostId} soft deleted", postId);
    }
}