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

    public List<Post> CenterPosts { get; set; } = new();
    public List<Post> GlobalPosts { get; set; } = new();

    public string CenterDisplayText { get; set; } = "Dit center";
    public bool MemberDataFound { get; set; }

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
        var gateway = GetGatewayUrl();

        try
        {
            AddJwtTokenToRequest();

            var response = await _httpClient.PostAsJsonAsync(
                $"{gateway}/api/community/posts/{PostId}/comments",
                NewComment);

            if (response.IsSuccessStatusCode)
                return Redirect("/Community");

            _logger.LogWarning(
                "Failed creating comment. Status code: {StatusCode}",
                response.StatusCode);
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
        var gateway = GetGatewayUrl();

        try
        {
            AddJwtTokenToRequest();

            var globalResponse = await _httpClient.GetAsync(
                $"{gateway}/api/community/global/posts");

            GlobalPosts = globalResponse.IsSuccessStatusCode
                ? await globalResponse.Content.ReadFromJsonAsync<List<Post>>() ?? new()
                : new();

            var member = await GetCurrentMemberAsync();

            if (member is null || member.HomeCenterId == Guid.Empty)
            {
                MemberDataFound = false;
                CenterDisplayText = "Dit center er ikke koblet endnu";
                CenterPosts = new();
                return;
            }

            MemberDataFound = true;
            var centerId = member.HomeCenterId.ToString();
            CenterDisplayText = GetCenterDisplayName(centerId);

            var centerResponse = await _httpClient.GetAsync(
                $"{gateway}/api/community/centers/{centerId}/posts");

            CenterPosts = centerResponse.IsSuccessStatusCode
                ? await centerResponse.Content.ReadFromJsonAsync<List<Post>>() ?? new()
                : new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading community posts");

            MemberDataFound = false;
            CenterDisplayText = "Dit center er ikke koblet endnu";

            CenterPosts = new();
            GlobalPosts = new();
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
            AddJwtTokenToRequest();
            var gateway = GetGatewayUrl();

            return await _httpClient.GetFromJsonAsync<MemberDto>(
              $"{gateway}/api/members/by-account/{userAccountId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not get member by user account {UserAccountId} from MemberService",
                userAccountId);

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

    private string GetCenterDisplayName(string centerId)
    {
        return centerId switch
        {
            "00000000-0000-0000-0000-000000000001" => "FitLife Aarhus C",
            "00000000-0000-0000-0000-000000000002" => "FitLife Aarhus Nord",
            "00000000-0000-0000-0000-000000000003" => "FitLife Viby",
            "00000000-0000-0000-0000-000000000004" => "FitLife Randers",
            "00000000-0000-0000-0000-000000000005" => "FitLife Horsens",
            "00000000-0000-0000-0000-000000000006" => "FitLife Silkeborg",
            _ => $"Center {centerId}"
        };
    }

    private string GetGatewayUrl()
    {
        return _configuration["GatewayUrl"]
            ?? _configuration["GATEWAY_URL"]
            ?? "http://haav-gateway";
    }

    private void AddJwtTokenToRequest()
    {
        var token =
            Request.Cookies["JwtToken"]
            ?? Request.Cookies["jwt"]
            ?? Request.Cookies["access_token"];

        _httpClient.DefaultRequestHeaders.Authorization = null;
        _httpClient.DefaultRequestHeaders.Remove("Cookie");

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        var cookies = string.Join("; ",
            Request.Cookies.Select(c => $"{c.Key}={c.Value}"));

        if (!string.IsNullOrWhiteSpace(cookies))
        {
            _httpClient.DefaultRequestHeaders.Add("Cookie", cookies);
        }
    }
}