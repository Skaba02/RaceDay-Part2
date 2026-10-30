using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>Thrown when the Blob Storage connection string has not been set up.</summary>
public class BlobStorageNotConfiguredException : Exception
{
    public BlobStorageNotConfiguredException()
        : base("Blob Storage is not configured. Set BlobStorage:ConnectionString.") { }
}

/// <summary>A file downloaded from Blob Storage.</summary>
public record BlobDownload(Stream Content, string ContentType);

/// <summary>
/// The only class in the solution that talks to Azure Blob Storage.
/// The MVC website never calls Azure: it sends files to the API, and the API uses this class.
/// </summary>
public class BlobStorageService
{
    public const string EventBannerContainer = "event-banners";
    public const string ProfilePictureContainer = "profile-pictures";

    private readonly IConfiguration _configuration;

    public BlobStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private async Task<BlobContainerClient> GetContainerAsync(string containerName)
    {
        var connectionString = _configuration["BlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new BlobStorageNotConfiguredException();
        }

        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync();
        return container;
    }

    public async Task UploadAsync(string containerName, string blobName, Stream content, string contentType)
    {
        var container = await GetContainerAsync(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
    }

    public async Task<BlobDownload?> DownloadAsync(string containerName, string blobName)
    {
        var container = await GetContainerAsync(containerName);
        var blob = container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync())
        {
            return null;
        }

        var response = await blob.DownloadStreamingAsync();
        return new BlobDownload(response.Value.Content, response.Value.Details.ContentType);
    }

    public async Task DeleteAsync(string containerName, string blobName)
    {
        var container = await GetContainerAsync(containerName);
        await container.DeleteBlobIfExistsAsync(blobName);
    }
}

/// <summary>Simple rules for uploaded images.</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    /// <summary>Returns a friendly error message, or null when the file is fine.</summary>
    public static string? Check(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "Please choose an image file.";
        }

        if (file.Length > MaxBytes)
        {
            return "The image must be 5 MB or smaller.";
        }

        if (!Extensions.ContainsKey(file.ContentType))
        {
            return "Only JPG, PNG or WEBP images are allowed.";
        }

        return null;
    }

    public static string ExtensionFor(string contentType) => Extensions[contentType];
}
