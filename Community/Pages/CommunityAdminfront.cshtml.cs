using System.Net.Http.Headers;
using Community.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Community.Pages;

[Authorize(Roles = "Admin")]
public class CommunityAdminModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CommunityAdminModel> _logger;
    private readonly IConfiguration _configuration;

    public List<Post> Posts { get; set; } = new();

    public string StatusMessage { get; set; } = string.Empty;

    [BindProperty]
    public CreatePostRequest NewPost { get; set; } = new();

    [BindProperty]
    public string SelectedScope { get; set; } = "Global";

    [BindProperty]
    public string CenterId { get; set; } = string.Empty;

    [BindProperty]
    public string PostId { get; set; } = string.Empty;

    public CommunityAdminModel(
        IHttpClientFactory httpClientFactory,
        ILogger<CommunityAdminModel> logger,
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
        var gateway = GetGatewayUrl();

        if (string.IsNullOrWhiteSpace(NewPost.Title))
        {
            StatusMessage = "Titel mangler.";
            await LoadPostsAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewPost.Content))
        {
            StatusMessage = "Indhold mangler.";
            await LoadPostsAsync();
            return Page();
        }

        if (SelectedScope == "Center" &&
            string.IsNullOrWhiteSpace(CenterId))
        {
            StatusMessage = "CenterId mangler.";
            await LoadPostsAsync();
            return Page();
        }

        try
        {
            AddJwtTokenToRequest();

            NewPost.Title = NewPost.Title.Trim();
            NewPost.Content = NewPost.Content.Trim();
            NewPost.AuthorMemberId = GetAdminAuthorId();

            var endpoint =
                SelectedScope == "Center"
                    ? $"{gateway}/api/community/centers/{CenterId.Trim()}/posts"
                    : $"{gateway}/api/community/global/posts";

            var response = await _httpClient.PostAsJsonAsync(
                endpoint,
                NewPost);

            if (response.IsSuccessStatusCode)
                return Redirect("/CommunityAdmin");

            StatusMessage =
                $"Opslag kunne ikke oprettes. Status: {(int)response.StatusCode}";

            _logger.LogWarning(
                "Admin failed creating post. Status code: {StatusCode}",
                response.StatusCode);
        }
        catch (Exception ex)
        {
            StatusMessage = "Der skete en fejl ved oprettelse af opslag.";
            _logger.LogError(ex, "Error creating admin post");
        }

        await LoadPostsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeletePostAsync()
    {
        var gateway = GetGatewayUrl();

        if (string.IsNullOrWhiteSpace(PostId))
        {
            StatusMessage = "PostId mangler.";
            await LoadPostsAsync();
            return Page();
        }

        try
        {
            AddJwtTokenToRequest();

            var response = await _httpClient.DeleteAsync(
                $"{gateway}/api/community/posts/{PostId}");

            if (response.IsSuccessStatusCode)
                return Redirect("/CommunityAdmin");

            StatusMessage =
                $"Opslag kunne ikke slettes. Status: {(int)response.StatusCode}";

            _logger.LogWarning(
                "Admin failed deleting post {PostId}. Status code: {StatusCode}",
                PostId,
                response.StatusCode);
        }
        catch (Exception ex)
        {
            StatusMessage = "Der skete en fejl ved sletning af opslag.";
            _logger.LogError(ex, "Error deleting admin post {PostId}", PostId);
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

            var response = await _httpClient.GetAsync(
                $"{gateway}/api/community/global/posts");

            Posts = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Post>>() ?? new()
                : new();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Admin failed loading global posts. Status code: {StatusCode}",
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin community posts");
            Posts = new();
        }
    }

    private string GetAdminAuthorId()
    {
        return Request.Cookies["memberId"]
            ?? User.FindFirst("memberId")?.Value
            ?? Request.Cookies["username"]
            ?? User.Identity?.Name
            ?? "admin";
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

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
}