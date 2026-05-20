using System.Net.Http.Json;
using Community.Models;
using Community.Service;
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

    [HttpPost("global/posts")]
    public async Task<ActionResult> CreateGlobalPost(CreatePostRequest request)
    {
        var authorName = await GetAuthorNameAsync(request.AuthorMemberId);

        await _communityService.CreateGlobalPostAsync(authorName, request);

        _logger.LogInformation(
            "Global post created by member {MemberId} as {AuthorName}",
            request.AuthorMemberId,
            authorName);

        return Created();
    }

    [HttpPost("centers/{centerId}/posts")]
    public async Task<ActionResult> CreateCenterPost(
        string centerId,
        CreatePostRequest request)
    {
        var authorName = await GetAuthorNameAsync(request.AuthorMemberId);

        await _communityService.CreateCenterPostAsync(
            authorName,
            centerId,
            request);

        _logger.LogInformation(
            "Center post created by member {MemberId} as {AuthorName} for center {CenterId}",
            request.AuthorMemberId,
            authorName,
            centerId);

        return Created();
    }

    [HttpPost("posts/{postId}/comments")]
    public async Task<ActionResult> AddComment(
        string postId,
        CreateCommentRequest request)
    {
        var authorName = await GetAuthorNameAsync(request.AuthorMemberId);

        await _communityService.AddCommentAsync(
            postId,
            authorName,
            request);

        _logger.LogInformation(
            "Comment added to post {PostId} by member {MemberId} as {AuthorName}",
            postId,
            request.AuthorMemberId,
            authorName);

        return Ok();
    }

    private async Task<string> GetAuthorNameAsync(string memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
            return "guest";

        try
        {
            var client = _httpClientFactory.CreateClient();

            var member = await client.GetFromJsonAsync<MemberDto>(
                $"http://haav-member-service:8080/api/Members/{memberId}");

            if (member == null)
                return memberId;

            var fullName = $"{member.FirstName} {member.LastName}".Trim();

            return string.IsNullOrWhiteSpace(fullName)
                ? memberId
                : fullName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get member {MemberId} from MemberService", memberId);
            return memberId;
        }
    }
}