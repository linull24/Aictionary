using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Aictionary.Services;

public class DictionaryDownloadService : IDictionaryDownloadService
{
    private readonly IDictionaryResourceService _resourceService;
    private readonly HttpClient _httpClient;
    private const string DictionaryDirectoryName = "dictionary";

    public DictionaryDownloadService(IDictionaryResourceService resourceService)
    {
        Console.WriteLine("[DictionaryDownloadService] Constructor called");
        _resourceService = resourceService;
        _httpClient = new HttpClient();
    }

    public bool DictionaryExists()
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        var dictionaryPath = Path.Combine(workingDirectory, DictionaryDirectoryName);
        var exists = Directory.Exists(dictionaryPath);
        Console.WriteLine($"[DictionaryDownloadService] DictionaryExists check: {exists}, path: {dictionaryPath}");
        return exists;
    }

    public async Task EnsureDictionaryExistsAsync(Action<string, double>? progressCallback = null)
    {
        Console.WriteLine("[DictionaryDownloadService] EnsureDictionaryExistsAsync called");
        var workingDirectory = Directory.GetCurrentDirectory();
        var dictionaryPath = Path.Combine(workingDirectory, DictionaryDirectoryName);
        Console.WriteLine($"[DictionaryDownloadService] Working directory: {workingDirectory}");
        Console.WriteLine($"[DictionaryDownloadService] Dictionary path: {dictionaryPath}");

        if (Directory.Exists(dictionaryPath))
        {
            Console.WriteLine("[DictionaryDownloadService] Dictionary already exists, skipping download");
            return;
        }

        Console.WriteLine("[DictionaryDownloadService] Starting download...");
        await DownloadAndExtractDictionaryAsync(workingDirectory, progressCallback);
    }

    private async Task DownloadAndExtractDictionaryAsync(string workingDirectory, Action<string, double>? progressCallback)
    {
        Console.WriteLine("[DictionaryDownloadService] DownloadAndExtractDictionaryAsync started");
        Console.WriteLine($"[DictionaryDownloadService] progressCallback is null: {progressCallback == null}");

        try
        {
            Console.WriteLine("[DictionaryDownloadService] Invoking progress callback: Fetching download URL...");
            progressCallback?.Invoke("Fetching download URL...", 0);

            Console.WriteLine("[DictionaryDownloadService] Calling GetDictionaryDownloadUrlAsync...");
            var downloadUrl = await _resourceService.GetDictionaryDownloadUrlAsync();
            Console.WriteLine($"[DictionaryDownloadService] Download URL: {downloadUrl}");

            var tempZipPath = Path.Combine(workingDirectory, "dictionary_temp.zip");
            Console.WriteLine($"[DictionaryDownloadService] Temp zip path: {tempZipPath}");

            Console.WriteLine("[DictionaryDownloadService] Invoking progress callback: Downloading dictionary...");
            progressCallback?.Invoke("Downloading dictionary...", 10);

            Console.WriteLine("[DictionaryDownloadService] Starting HTTP request...");
            var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            Console.WriteLine($"[DictionaryDownloadService] HTTP response status: {response.StatusCode}");

            response.EnsureSuccessStatusCode();
            Console.WriteLine("[DictionaryDownloadService] Response status is successful");

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var canReportProgress = totalBytes != -1;
            Console.WriteLine($"[DictionaryDownloadService] Total bytes: {totalBytes}, Can report progress: {canReportProgress}");

            Console.WriteLine("[DictionaryDownloadService] Starting file download...");
            await using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await using var contentStream = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[8192];
                var totalRead = 0L;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (canReportProgress && totalRead % (1024 * 1024) == 0) // Log every MB
                    {
                        Console.WriteLine($"[DictionaryDownloadService] Downloaded: {totalRead / (1024.0 * 1024):F1} MB");
                        var progress = 10 + (totalRead * 70.0 / totalBytes);
                        progressCallback?.Invoke($"Downloading... {totalRead / (1024.0 * 1024):F1} MB / {totalBytes / (1024.0 * 1024):F1} MB", progress);
                    }
                }

                Console.WriteLine($"[DictionaryDownloadService] Download complete. Total read: {totalRead} bytes");
            }

            Console.WriteLine("[DictionaryDownloadService] Invoking progress callback: Extracting dictionary...");
            progressCallback?.Invoke("Extracting dictionary...", 85);

            Console.WriteLine("[DictionaryDownloadService] Starting extraction...");
            ZipFile.ExtractToDirectory(tempZipPath, workingDirectory);
            Console.WriteLine("[DictionaryDownloadService] Extraction complete");

            Console.WriteLine("[DictionaryDownloadService] Invoking progress callback: Download complete!");
            progressCallback?.Invoke("Download complete!", 100);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DictionaryDownloadService] ERROR: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[DictionaryDownloadService] Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            var tempZipPath = Path.Combine(workingDirectory, "dictionary_temp.zip");
            if (File.Exists(tempZipPath))
            {
                Console.WriteLine("[DictionaryDownloadService] Deleting temp zip file");
                File.Delete(tempZipPath);
            }
        }
    }
}
