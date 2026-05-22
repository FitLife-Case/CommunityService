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
        var gateway = _configuration["GatewayUrl"] ?? "http://haav-gateway";

        try
        {
            AddJwtTokenToRequest();

            var endpoint =
                SelectedScope == "Center" && !string.IsNullOrWhiteSpace(CenterId)
                    ? $"{gateway}/api/community/centers/{CenterId}/posts"
                    : $"{gateway}/api/community/global/posts";

            var response = await _httpClient.PostAsJsonAsync(endpoint, NewPost);

            if (response.IsSuccessStatusCode)
                return Redirect("/CommunityAdmin");

            _logger.LogWarning("Admin failed creating post. Status code: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin post");
        }

        await LoadPostsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeletePostAsync()
    {
        var gateway = _configuration["GatewayUrl"] ?? "http://haav-gateway";

        try
        {
            AddJwtTokenToRequest();

            var response = await _httpClient.DeleteAsync(
                $"{gateway}/api/community/posts/{PostId}");

            if (response.IsSuccessStatusCode)
                return Redirect("/CommunityAdmin");

            _logger.LogWarning("Admin failed deleting post. Status code: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting admin post");
        }

        await LoadPostsAsync();
        return Page();
    }

    private async Task LoadPostsAsync()
    {
        var gateway = _configuration["GatewayUrl"] ?? "http://haav-gateway";

        try
        {
            var response = await _httpClient.GetAsync($"{gateway}/api/community/global/posts");

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