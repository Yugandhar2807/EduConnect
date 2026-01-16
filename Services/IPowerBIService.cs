namespace EduConnect.Services
{
    public interface IPowerBIService
    {
        Task<PowerBIEmbedConfig> GetEmbedConfigAsync(string userId);
    }

    public class PowerBIEmbedConfig
    {
        public string? EmbedUrl { get; set; }
        public string? EmbedToken { get; set; }
        public string? ReportId { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
