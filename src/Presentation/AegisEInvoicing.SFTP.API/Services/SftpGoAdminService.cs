using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.SFTP.API.Services;

public interface ISftpGoAdminService
{
    Task<bool> EnsureUserExistsAsync(string username, string homeDir, Dictionary<string, List<string>> permissions);
}

public class SftpGoAdminService : ISftpGoAdminService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SftpGoAdminService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SftpGoAdminService(HttpClient httpClient, ILogger<SftpGoAdminService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
    public async Task<bool> EnsureUserExistsAsync(string username, string homeDir, Dictionary<string, List<string>> permissions)
    {
        try
        {
            var checkResponse = await _httpClient.GetAsync($"/api/v2/users/{username}");

            if (checkResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var authError = await checkResponse.Content.ReadAsStringAsync();
                _logger.LogError("SFTPGo API authentication failed: {Error}", authError);
                return false;
            }

            if (!checkResponse.IsSuccessStatusCode)
            {
                // User doesn't exist, create them
                var userPayload = new SftpGoUser
                {
                    Username = username,
                    Status = 1,
                    HomeDir = homeDir,
                    Permissions = permissions,
                    Filters = new SftpGoFilters { WebClient = [] }
                };

                var createResponse = await _httpClient.PostAsJsonAsync("/api/v2/users", userPayload, _jsonOptions);

                if (!createResponse.IsSuccessStatusCode)
                {
                    var error = await createResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create SFTPGo user {Username}: {StatusCode} - {Error}",
                        username, createResponse.StatusCode, error);

                    return false;
                }

                _logger.LogInformation("✓ Created SFTPGo user {Username}", username);
            }

            // Always ensure subdirectories exist
            // await EnsureUserDirectoriesExistAsync(username);
            EnsureUserDirectoriesExistOnDisk(homeDir);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring SFTPGo user exists: {Username}", username);
            return false;
        }
    }
    private void EnsureUserDirectoriesExistOnDisk(string homeDir)
    {
        var directories = new[] { "Pending", "In-Progress", "Receipts", "Rejected" };
        var normalizedHomeDir = homeDir.Replace('/', Path.DirectorySeparatorChar);

        _logger.LogInformation("=== DIR DEBUG ===");
        _logger.LogInformation("Raw homeDir: {HomeDir}", homeDir);
        _logger.LogInformation("Normalized homeDir: {HomeDir}", normalizedHomeDir);
        _logger.LogInformation("Home dir exists: {Exists}", Directory.Exists(normalizedHomeDir));

        if (!Directory.Exists(normalizedHomeDir))
        {
            Directory.CreateDirectory(normalizedHomeDir);
            _logger.LogInformation("Created home dir: {Path}", normalizedHomeDir);
        }

        foreach (var dir in directories)
        {
            var fullPath = Path.Combine(normalizedHomeDir, dir);
            _logger.LogInformation("Attempting: {Path}", fullPath);
            try
            {
                Directory.CreateDirectory(fullPath);
                _logger.LogInformation("✓ Success: {Path}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError("✗ FAILED: {Path} - {Error}", fullPath, ex.Message);
            }
        }
    }
}

public class SftpGoUser
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; } = 1;

    [JsonPropertyName("home_dir")]
    public string HomeDir { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public Dictionary<string, List<string>>? Permissions { get; set; }

    [JsonPropertyName("filters")]
    public SftpGoFilters? Filters { get; set; }
}

public class SftpGoFilters
{
    [JsonPropertyName("web_client")]
    public List<string> WebClient { get; set; } = [];
}
