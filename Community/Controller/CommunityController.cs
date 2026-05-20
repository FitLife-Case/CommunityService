using Community.Models;
using Community.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Community.Controllers;

[ApiController]
[Route("api/community")]
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

    [AllowAnonymous]
    [HttpGet("global/posts")]
    public async Task<ActionResult<List<Post>>> GetGlobalPosts()
    {
        var posts = await _communityService.GetGlobalPostsAsync();
        return Ok(posts);
    }

    [AllowAnonymous]
    [HttpGet("centers/{centerId}/posts")]
    public async Task<ActionResult<List<Post>>> GetCenterPosts(string centerId)
    {
        var posts = await _communityService.GetCenterPostsAsync(centerId);
        return Ok(posts);
    }

    [Authorize(Roles = "Member,Admin")]
    [HttpPost("global/posts")]
    public async Task<ActionResult> CreateGlobalPost(CreatePostRequest request)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        await _communityService.CreateGlobalPostAsync(userId, request);

        _logger.LogInformation(
            "Global post created by user {UserId} with role {Role}",
            userId,
            role);

        return Created();
    }

    [Authorize(Roles = "Member,Admin")]
    [HttpPost("centers/{centerId}/posts")]
    public async Task<ActionResult> CreateCenterPost(
        string centerId,
        CreatePostRequest request)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        await _communityService.CreateCenterPostAsync(
            userId,
            centerId,
            request);

        _logger.LogInformation(
            "Center post created by user {UserId} with role {Role} for center {CenterId}",
            userId,
            role,
            centerId);

        return Created();
    }

    [Authorize(Roles = "Member,Admin")]
    [HttpPost("posts/{postId}/comments")]
    public async Task<ActionResult> AddComment(
        string postId,
        CreateCommentRequest request)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        await _communityService.AddCommentAsync(
            postId,
            userId,
            request);

        _logger.LogInformation(
            "Comment added to post {PostId} by user {UserId} with role {Role}",
            postId,
            userId,
            role);

        return Ok();
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? User.FindFirstValue(ClaimTypes.Name)
               ?? User.FindFirstValue("name")
               ?? User.FindFirstValue("username")
               ?? throw new UnauthorizedAccessException("User id/name claim missing from token");
    }

    private string GetUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role)
               ?? User.FindFirstValue("role")
               ?? "Unknown";
    }
}