using System.Net.Http.Headers;
using Community.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Community.Pages;

public class CommunityfrontModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CommunityfrontModel> _logger;
    private readonly IConfiguration _configuration;

    public List<Post> Posts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string FeedScope { get; set; } = "Global";

    [BindProperty(SupportsGet = true)]
    public string FeedCenterId { get; set; } = string.Empty;

    [BindProperty]
    public CreatePostRequest NewPost { get; set; } = new();

    [BindProperty]
    public string SelectedScope { get; set; } = "Global";

    [BindProperty]
    public string CenterId { get; set; } = string.Empty;

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

            var isCenterPost =
                SelectedScope == "Center" &&
                !string.IsNullOrWhiteSpace(CenterId);

            var endpoint = isCenterPost
                ? $"{gateway}/api/community/centers/{CenterId}/posts"
                : $"{gateway}/api/community/global/posts";

            var response = await _httpClient.PostAsJsonAsync(endpoint, NewPost);

            if (response.IsSuccessStatusCode)
            {
                if (isCenterPost)
                {
                    return Redirect($"/Community?FeedScope=Center&FeedCenterId={CenterId}");
                }

                return Redirect("/Community?FeedScope=Global");
            }

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
            {
                if (FeedScope == "Center" && !string.IsNullOrWhiteSpace(FeedCenterId))
                {
                    return Redirect($"/Community?FeedScope=Center&FeedCenterId={FeedCenterId}");
                }

                return Redirect("/Community?FeedScope=Global");
            }

            _logger.LogWarning(
                "Failed creating comment for post {PostId}. Status code: {StatusCode}",
                PostId,
                response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment for post {PostId}", PostId);
        }

        await LoadPostsAsync();
        return Page();
    }

    private async Task LoadPostsAsync()
    {
        var gateway = _configuration["GatewayUrl"] ?? "http://haav-gateway";

        try
        {
            var endpoint =
                FeedScope == "Center" &&
                !string.IsNullOrWhiteSpace(FeedCenterId)
                    ? $"{gateway}/api/community/centers/{FeedCenterId}/posts"
                    : $"{gateway}/api/community/global/posts";

            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed loading posts. Status code: {StatusCode}", response.StatusCode);
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

    private void AddJwtTokenToRequest()
    {
        var token = Request.Cookies["JwtToken"];

        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
}