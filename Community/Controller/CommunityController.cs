using System.Security.Claims;
using Community.Models;
using Community.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Community.Controllers;

[ApiController]
[Route("api/community")]
public class CommunityController : ControllerBase
{
    private readonly ICommunityService _communityService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CommunityController> _logger;

    public CommunityController(
        ICommunityService communityService,
        IHttpClientFactory httpClientFactory,
        ILogger<CommunityController> logger)
    {
        _communityService = communityService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("global/posts")]
    public async Task<ActionResult<List<Post>>> GetGlobalPosts()
    {
        var posts = await _communityService.GetGlobalPostsAsync();
        return Ok(posts);
    }

    [HttpGet("centers/{centerId}/posts")]
    public async Task<ActionResult<List<Post>>> GetCenterPosts(string centerId)
    {
        var posts = await _communityService.GetCenterPostsAsync(centerId);
        return Ok(posts);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("global/posts")]
    public async Task<ActionResult> CreateGlobalPost(CreatePostRequest request)
    {
        var authorName = GetUsernameFallback();
        request.AuthorMemberId = GetCurrentUserId() ?? "admin";

        await _communityService.CreateGlobalPostAsync(authorName, request);
        return Created();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("centers/{centerId}/posts")]
    public async Task<ActionResult> CreateCenterPost(string centerId, CreatePostRequest request)
    {
        var authorName = GetUsernameFallback();
        request.AuthorMemberId = GetCurrentUserId() ?? "admin";

        await _communityService.CreateCenterPostAsync(authorName, centerId, request);
        return Created();
    }

    [Authorize(Roles = "Member,Admin")]
    [HttpPost("posts/{postId}/comments")]
    public async Task<ActionResult> AddComment(string postId, CreateCommentRequest request)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized("No user id found.");

        request.AuthorMemberId = userId;

        var authorName = GetUsernameFallback();

        await _communityService.AddCommentAsync(postId, authorName, request);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("posts/{postId}")]
    public async Task<ActionResult> DeletePost(string postId)
    {
        await _communityService.DeletePostAsync(postId);
        return NoContent();
    }

    private string? GetCurrentUserId()
    {
        return Request.Cookies["memberId"]
            ?? User.FindFirst("memberId")?.Value
            ?? User.FindFirst("profileId")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string GetUsernameFallback()
    {
        return Request.Cookies["username"]
            ?? User.FindFirst("username")?.Value
            ?? User.FindFirst("name")?.Value
            ?? "member";
    }
}