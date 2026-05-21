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

    [Authorize(Roles = "Member,Admin")]
    [HttpPost("global/posts")]
    public async Task<ActionResult> CreateGlobalPost(CreatePostRequest request)
    {
        var memberId = GetCurrentMemberId();

        if (string.IsNullOrWhiteSpace(memberId))
            return Unauthorized("No member id found in JWT claims.");

        request.AuthorMemberId = memberId;

        var authorName = await GetAuthorNameAsync(memberId);

        await _communityService.CreateGlobalPostAsync(authorName, request);

        _logger.LogInformation(
            "Global post created by member {MemberId} as {AuthorName}",
            memberId,
            authorName);

        return Created();
    }

    [Authorize(Roles = "Member,Admin")]
    [HttpPost("centers/{centerId}/posts")]
    public async Task<ActionResult> CreateCenterPost(
        string centerId,
        CreatePostRequest request)
    {
        var memberId = GetCurrentMemberId();

        if (string.IsNullOrWhiteSpace(memberId))
            return Unauthorized("No member id found in JWT claims.");

        request.AuthorMemberId = memberId;

        var authorName = await GetAuthorNameAsync(memberId);

        await _communityService.CreateCenterPostAsync(
            authorName,
            centerId,
            request);

        _logger.LogInformation(
            "Center post created by member {MemberId} as {AuthorName} for center {CenterId}",
            memberId,
            authorName,
            centerId);

        return Created();
    }

    [Authorize(Roles = "Member,Admin")]
    [HttpPost("posts/{postId}/comments")]
    public async Task<ActionResult> AddComment(
        string postId,
        CreateCommentRequest request)
    {
        var memberId = GetCurrentMemberId();

        if (string.IsNullOrWhiteSpace(memberId))
            return Unauthorized("No member id found in JWT claims.");

        request.AuthorMemberId = memberId;

        var authorName = await GetAuthorNameAsync(memberId);

        await _communityService.AddCommentAsync(
            postId,
            authorName,
            request);

        _logger.LogInformation(
            "Comment added to post {PostId} by member {MemberId} as {AuthorName}",
            postId,
            memberId,
            authorName);

        return Ok();
    }

    private string? GetCurrentMemberId()
    {
        return Request.Cookies["memberId"]
            ?? User.FindFirst("memberId")?.Value
            ?? User.FindFirst("MemberId")?.Value
            ?? User.FindFirst("member_id")?.Value
            ?? User.FindFirst("profileId")?.Value
            ?? User.FindFirst("ProfileId")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private async Task<string> GetAuthorNameAsync(string memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
            return GetUsernameFallback();

        try
        {
            var client = _httpClientFactory.CreateClient();

            var member = await client.GetFromJsonAsync<MemberDto>(
                $"http://haav-member-service:8080/api/Members/{memberId}");

            if (member == null)
                return GetUsernameFallback();

            var fullName = $"{member.FirstName} {member.LastName}".Trim();

            return string.IsNullOrWhiteSpace(fullName)
                ? GetUsernameFallback()
                : fullName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get member {MemberId} from MemberService", memberId);
            return GetUsernameFallback();
        }
    }

    private string GetUsernameFallback()
    {
        return Request.Cookies["username"]
            ?? User.FindFirst("username")?.Value
            ?? User.FindFirst("name")?.Value
            ?? "member";
    }
}