using System.Net.Http.Json;
using System.Text.Json;

namespace EduConnect.Services
{
    public class PowerBIService : IPowerBIService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PowerBIService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public PowerBIService(IConfiguration configuration, ILogger<PowerBIService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PowerBIEmbedConfig> GetEmbedConfigAsync(string userId)
        {
            try
            {
                var config = new PowerBIEmbedConfig();
                
                var clientId = _configuration["PowerBI:ClientId"];
                var clientSecret = _configuration["PowerBI:ClientSecret"];
                var tenantId = _configuration["PowerBI:TenantId"];
                var reportId = _configuration["PowerBI:ReportId"];
                var workspaceId = _configuration["PowerBI:WorkspaceId"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    config.Success = false;
                    config.Error = "Power BI credentials not configured";
                    return config;
                }

                // Get access token using client credentials
                var accessToken = await GetAccessTokenAsync(clientId, clientSecret, tenantId);
                if (string.IsNullOrEmpty(accessToken))
                {
                    config.Success = false;
                    config.Error = "Failed to acquire Power BI access token";
                    return config;
                }

                // Generate embed token
                var embedToken = await GetEmbedTokenAsync(accessToken, workspaceId, reportId, userId);
                if (string.IsNullOrEmpty(embedToken))
                {
                    config.Success = false;
                    config.Error = "Failed to generate embed token";
                    return config;
                }

                config.EmbedToken = embedToken;
                config.ReportId = reportId;
                config.EmbedUrl = $"https://app.powerbi.com/reportEmbed?reportId={reportId}";
                config.Success = true;

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Power BI embed config");
                return new PowerBIEmbedConfig 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        private async Task<string?> GetAccessTokenAsync(string clientId, string clientSecret, string tenantId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("scope", "https://analysis.windows.net/powerbi/api/.default")
                });

                var response = await client.PostAsync(tokenUrl, requestContent);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to get access token: {response.StatusCode}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseContent);
                var accessToken = jsonDocument.RootElement.GetProperty("access_token").GetString();

                return accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acquiring access token");
                return null;
            }
        }

        private async Task<string?> GetEmbedTokenAsync(string accessToken, string workspaceId, string reportId, string userId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var url = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/reports/{reportId}/generateToken";
                _logger.LogInformation($"Generating token for report {reportId} in workspace {workspaceId}");

                var requestBody = new
                {
                    accessLevel = "View"
                };

                var response = await client.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to generate embed token: {response.StatusCode} - {errorContent}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(responseContent);
                var embedToken = jsonDocument.RootElement.GetProperty("token").GetString();
                _logger.LogInformation($"Successfully generated embed token");

                return embedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embed token");
                return null;
            }
        }
    }
}
