using System.Net.Http.Headers;
using System.Security.Claims;
using Community.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Community.Pages;

[Authorize(Roles = "Member,Admin")]
public class CommunityfrontModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CommunityfrontModel> _logger;
    private readonly IConfiguration _configuration;

    public List<Post> Posts { get; set; } = new();

    public string FeedScope { get; set; } = "Center";
    public string FeedCenterId { get; set; } = string.Empty;
    public string CenterDisplayText { get; set; } = "Dit center";

    [BindProperty]
    public CreatePostRequest NewPost { get; set; } = new();

    [BindProperty]
    public string PostId { get; set; } = string.Empty;

    [BindProperty]
    public CreateCommentRequest NewComment { get; set; } = new();

    public CommunityfrontModel(
        IHttpClientFactory httpClientFactory,
        ILogger<CommunityfrontModel> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _configuration = configuration;
    }

    public async Task OnGetAsync()
    {
        await LoadPostsAsync();
    }

    public async Task<IActionResult> OnPostCreatePostAsync()
    {
        var gateway = _configuration["GatewayUrl"] ?? "http://haav-gateway";

        try
        {
            AddJwtTokenToRequest();

            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                _logger.LogWarning("Could not resolve current member when creating post");
                return Unauthorized();
            }

            FeedCenterId = member.HomeCenterId.ToString();
            CenterDisplayText = $"Center {FeedCenterId}";

            var endpoint = $"{gateway}/api/community/centers/{FeedCenterId}/posts";

            var response = await _httpClient.PostAsJsonAsync(endpoint, NewPost);

            if (response.IsSuccessStatusCode)
                return Redirect("/Community");

            _logger.LogWarning("Failed creating post. Status code: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
        }

        await LoadPostsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddCommentAsync()
    {
        var gateway = _configuration["GatewayUrl"] ?? "http://haav-gateway";

        try
        {
            AddJwtTokenToRequest();

            var response = await _httpClient.PostAsJsonAsync(
                $"{gateway}/api/community/posts/{PostId}/comments",
                NewComment);

            if (response.IsSuccessStatusCode)
                return Redirect("/Community");

            _logger.LogWarning("Failed creating comment. Status code: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment");
        }

        await LoadPostsAsync();
        return Page();
    }

    private async Task LoadPostsAsync()
    {
        var gateway = _configuration["GatewayUrl"] ?? "http://haav-gateway";

        try
        {
            AddJwtTokenToRequest();

            var member = await GetCurrentMemberAsync();

            if (member == null)
            {
                Posts = new();
                return;
            }

            FeedCenterId = member.HomeCenterId.ToString();
            CenterDisplayText = $"Center {FeedCenterId}";

            var endpoint = $"{gateway}/api/community/centers/{FeedCenterId}/posts";

            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                Posts = new();
                return;
            }

            Posts = await response.Content.ReadFromJsonAsync<List<Post>>() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading community posts");
            Posts = new();
        }
    }

    private async Task<MemberDto?> GetCurrentMemberAsync()
    {
        var memberId = GetCurrentMemberId();

        if (string.IsNullOrWhiteSpace(memberId))
        {
            _logger.LogWarning("No memberId found in JWT/cookies");
            return null;
        }

        try
        {
            var client = new HttpClient();

            return await client.GetFromJsonAsync<MemberDto>(
                $"http://haav-member-service:8080/api/Members/{memberId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get member {MemberId} from MemberService", memberId);
            return null;
        }
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

    private void AddJwtTokenToRequest()
    {
        var token =
            Request.Cookies["JwtToken"]
            ?? Request.Cookies["jwt"]
            ?? Request.Cookies["access_token"];

        _httpClient.DefaultRequestHeaders.Authorization = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("JWT cookie was missing");
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}