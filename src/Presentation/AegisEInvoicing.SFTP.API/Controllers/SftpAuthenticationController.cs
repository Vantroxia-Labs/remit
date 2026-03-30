using AegisEInvoicing.SFTP.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.SFTP.API.Controllers;

/// <summary>
/// API endpoint for SFTPGo external authentication
/// SFTPGo will call this endpoint to validate users against the database
/// </summary>
[ApiController]
[Route("api/sftp-auth")]
public class SftpAuthenticationController(
    IVirtualUserAuthenticationService authService,
    ISftpGoAdminService sftpGoAdmin,
    ILogger<SftpAuthenticationController> logger) : ControllerBase
{
    private readonly IVirtualUserAuthenticationService _authService = authService;
    private readonly ISftpGoAdminService _sftpGoAdmin = sftpGoAdmin;
    private readonly ILogger<SftpAuthenticationController> _logger = logger;

    /// <summary>
    /// Authenticate SFTP user - Called by SFTPGo
    /// </summary>
    [HttpPost("check-credentials")]
    public async Task<IActionResult> CheckCredentials([FromBody] SftpAuthRequest request)
    {
        try
        {
            _logger.LogInformation("SFTP authentication request for user: {Username}", request.Username);

            // Authenticate against database
            var isValid = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (!isValid)
            {
                _logger.LogWarning("SFTP authentication failed for user: {Username}", request.Username);
                return Ok(new SftpAuthResponse
                {
                    Status = 0,
                    Message = "Invalid credentials"
                });
            }

            // Get user context
            var userContext = await _authService.GetUserContextAsync(request.Username);

            if (userContext == null || !userContext.IsEnabled)
            {
                _logger.LogWarning("SFTP disabled for user: {Username}", request.Username);
                return Ok(new SftpAuthResponse
                {
                    Status = 0,
                    Message = "SFTP access disabled"
                });
            }

            // Return success with user details
            // SFTPGo external auth requires ABSOLUTE path for home_dir
            var homeDir = $"C:/ftproot/uploads/{userContext.BusinessId}";

            // Folder structure:
            // /Pending - Client drop zone (upload only)
            // /In-Progress - System-managed processing folder (no client access)
            // /Receipts - Success outputs with IRN subfolders (read-only)
            // /Rejected - Failed files with NACK responses (read-only)
            var permissions = new Dictionary<string, List<string>>
            {
                ["/"] = ["list"],  // Root: navigation only
                ["/Pending"] = ["list", "upload", "create_dirs", "overwrite", "delete"],  // Inbox: clients drop files here
                ["/In-Progress"] = [],  // No client access: system-managed processing
                ["/Receipts"] = ["list", "download"],  // Read-only: download success receipts
                ["/Rejected"] = ["list", "download"]   // Read-only: download failed files with NACK
            };

            // Ensure user exists in SFTPGo via Admin API (creates home dir and subdirectories)
            await _sftpGoAdmin.EnsureUserExistsAsync(request.Username, homeDir, permissions);

            _logger.LogInformation("SFTP authentication successful for user: {Username}, HomeDir: {HomeDir}", 
                request.Username, homeDir);

            return Ok(new SftpAuthResponse
            {
                Status = 1,
                Username = request.Username,
                HomeDir = homeDir,
                Uid = 0,
                Gid = 0,
                MaxSessions = 5,
                QuotaSize = 10737418240, // 10 GB
                QuotaFiles = 10000,
                Permissions = permissions,
                Message = "Authentication successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SFTP authentication for user: {Username}", request.Username);
            return Ok(new SftpAuthResponse
            {
                Status = 0,
                Message = "Authentication error"
            });
        }
    }

    /// <summary>
    /// Get user details - Called by SFTPGo for user info
    /// </summary>
    [HttpPost("get-user")]
    public async Task<IActionResult> GetUser([FromBody] SftpGetUserRequest request)
    {
        try
        {
            var userContext = await _authService.GetUserContextAsync(request.Username);

            if (userContext == null || !userContext.IsEnabled)
            {
                return NotFound(new { Message = "User not found or disabled" });
            }

            // SFTPGo external auth requires ABSOLUTE path for home_dir
            var homeDir = $"C:/ftproot/uploads/{userContext.BusinessId}";

            // Folder structure:
            // /Pending - Client drop zone (upload only)
            // /In-Progress - System-managed processing folder (no client access)
            // /Receipts - Success outputs with IRN subfolders (read-only)
            // /Rejected - Failed files with NACK responses (read-only)
            var permissions = new Dictionary<string, List<string>>
            {
                ["/"] = ["list"],  // Root: navigation only
                ["/Pending"] = ["list", "upload", "create_dirs", "overwrite", "delete"],  // Inbox: clients drop files here
                ["/In-Progress"] = [],  // No client access: system-managed processing
                ["/Receipts"] = ["list", "download"],  // Read-only: download success receipts
                ["/Rejected"] = ["list", "download"]   // Read-only: download failed files with NACK
            };

            // Ensure user exists in SFTPGo via Admin API
            await _sftpGoAdmin.EnsureUserExistsAsync(request.Username, homeDir, permissions);

            _logger.LogInformation("GetUser: Username={Username}, HomeDir={HomeDir}", request.Username, homeDir);

            return Ok(new SftpAuthResponse
            {
                Status = 1,
                Username = request.Username,
                HomeDir = homeDir,
                Uid = 0,
                Gid = 0,
                MaxSessions = 5,
                QuotaSize = 10737418240,
                QuotaFiles = 10000,
                Permissions = permissions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details: {Username}", request.Username);
            return NotFound(new { Message = "User not found" });
        }
    }
}

public class SftpAuthRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
}

public class SftpGetUserRequest
{
    public string Username { get; set; } = string.Empty;
}
public class SftpAuthResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("home_dir")]
    public string? HomeDir { get; set; }

    [JsonPropertyName("uid")]
    public int Uid { get; set; }

    [JsonPropertyName("gid")]
    public int Gid { get; set; }

    [JsonPropertyName("max_sessions")]
    public int MaxSessions { get; set; }

    [JsonPropertyName("quota_size")]
    public long QuotaSize { get; set; }

    [JsonPropertyName("quota_files")]
    public int QuotaFiles { get; set; }

    [JsonPropertyName("permissions")]
    public Dictionary<string, List<string>>? Permissions { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
public class SftpFilters
{
    [JsonPropertyName("start_directory")]
    public string StartDirectory { get; set; } = "/";
}
