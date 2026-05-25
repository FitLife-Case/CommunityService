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

    public record CenterOption(string Id, string Name);

    public List<CenterOption> AvailableCenters { get; } = new()
    {
        new("11111111-1111-1111-1111-111111111111", "FitLife Copenhagen"),
        new("22222222-2222-2222-2222-222222222222", "FitLife Aarhus"),
        new("33333333-3333-3333-3333-333333333333", "FitLife Odense"),
        new("44444444-4444-4444-4444-444444444444", "FitLife Aalborg"),
        new("55555555-5555-5555-5555-555555555555", "FitLife Esbjerg")
    };

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

    [BindProperty(SupportsGet = true)]
    public string ViewScope { get; set; } = "Global";

    [BindProperty(SupportsGet = true)]
    public string ViewCenterId { get; set; } = string.Empty;

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

        if (SelectedScope == "Center" && string.IsNullOrWhiteSpace(CenterId))
        {
            StatusMessage = "Center mangler.";
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

            var response = await _httpClient.PostAsJsonAsync(endpoint, NewPost);

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

            HttpResponseMessage response;

            if (ViewScope == "Center" && !string.IsNullOrWhiteSpace(ViewCenterId))
            {
                response = await _httpClient.GetAsync(
                    $"{gateway}/api/community/centers/{ViewCenterId}/posts");
            }
            else
            {
                response = await _httpClient.GetAsync(
                    $"{gateway}/api/community/global/posts");
            }

            Posts = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<Post>>() ?? new()
                : new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin community posts");
            Posts = new();
        }
    }

    public string GetCenterName(string? centerId)
    {
        return AvailableCenters.FirstOrDefault(c => c.Id == centerId)?.Name
            ?? centerId
            ?? "Ukendt center";
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