using System.Net.Http.Headers;
using System.Security.Claims;
using Community.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Community.Pages;

[Authorize(Roles = "Member")]
public class CommunityfrontModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CommunityfrontModel> _logger;
    private readonly IConfiguration _configuration;

    public List<Post> Posts { get; set; } = new();

    public string CenterDisplayText { get; set; } = "Dit center";
    public bool MemberDataFound { get; set; } = false;

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
                MemberDataFound = false;
                CenterDisplayText = "Dit center er ikke koblet endnu";

                var globalResponse = await _httpClient.GetAsync($"{gateway}/api/community/global/posts");

                Posts = globalResponse.IsSuccessStatusCode
                    ? await globalResponse.Content.ReadFromJsonAsync<List<Post>>() ?? new()
                    : new();

                return;
            }

            MemberDataFound = true;
            CenterDisplayText = $"Center {member.HomeCenterId}";

            var response = await _httpClient.GetAsync(
                $"{gateway}/api/community/centers/{member.HomeCenterId}/posts");

            Posts = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Post>>() ?? new()
                : new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading community posts");
            Posts = new();
        }
    }

    private async Task<MemberDto?> GetCurrentMemberAsync()
    {
        var userAccountId = GetCurrentUserAccountId();

        if (string.IsNullOrWhiteSpace(userAccountId))
        {
            _logger.LogWarning("No user/member id found in JWT/cookies");
            return null;
        }

        try
        {
            var client = new HttpClient();

            return await client.GetFromJsonAsync<MemberDto>(
                $"http://haav-member-service:8080/api/Members/by-user/{userAccountId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get member by user account {UserAccountId} from MemberService", userAccountId);
            return null;
        }
    }

    private string? GetCurrentUserAccountId()
    {
        return Request.Cookies["memberId"]
            ?? User.FindFirst("memberId")?.Value
            ?? User.FindFirst("profileId")?.Value
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

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
}