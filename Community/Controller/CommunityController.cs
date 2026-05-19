using Community.Models;
using Community.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Community.Controllers;

[ApiController]
[Route("api/community")]
[Authorize]
public class CommunityController : ControllerBase
{
    private readonly ICommunityService _communityService;
    private readonly ILogger<CommunityController> _logger;

    public CommunityController(
        ICommunityService communityService,
        ILogger<CommunityController> logger)
    {
        _communityService = communityService;
        _logger = logger;
    }

    // Public test endpoint: henter alle globale posts
    [AllowAnonymous]
    [HttpGet("global/posts")]
    public async Task<ActionResult<List<Post>>> GetGlobalPosts()
    {
        var posts = await _communityService.GetGlobalPostsAsync();
        return Ok(posts);
    }

    // Public test endpoint: henter posts for et bestemt center
    [AllowAnonymous]
    [HttpGet("centers/{centerId}/posts")]
    public async Task<ActionResult<List<Post>>> GetCenterPosts(string centerId)
    {
        var posts = await _communityService.GetCenterPostsAsync(centerId);
        return Ok(posts);
    }

    // Midlertidig test: opret global post uden JWT
    [AllowAnonymous]
    [HttpPost("global/posts")]
    public async Task<ActionResult> CreateGlobalPost(CreatePostRequest request)
    {
        var userId = "test-user";

        await _communityService.CreateGlobalPostAsync(userId, request);

        _logger.LogInformation("Global post created by user {UserId}", userId);

        return Created();
    }

    // Midlertidig test: opret center post uden JWT
    [AllowAnonymous]
    [HttpPost("centers/{centerId}/posts")]
    public async Task<ActionResult> CreateCenterPost(
        string centerId,
        CreatePostRequest request)
    {
        var userId = "test-user";

        await _communityService.CreateCenterPostAsync(
            userId,
            centerId,
            request);

        _logger.LogInformation(
            "Center post created by user {UserId} for center {CenterId}",
            userId,
            centerId);

        return Created();
    }

    // Midlertidig test: tilføj kommentar uden JWT
    [AllowAnonymous]
    [HttpPost("posts/{postId}/comments")]
    public async Task<ActionResult> AddComment(
        string postId,
        CreateCommentRequest request)
    {
        var userId = "test-user";

        await _communityService.AddCommentAsync(
            postId,
            userId,
            request);

        _logger.LogInformation(
            "Comment added to post {PostId} by user {UserId}",
            postId,
            userId);

        return Ok();
    }

    // Bruges senere når rigtig JWT auth er koblet på
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new UnauthorizedAccessException();
    }
}