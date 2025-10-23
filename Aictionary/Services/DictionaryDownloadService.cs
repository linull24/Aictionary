using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Aictionary.Models;

namespace Aictionary.Services;

public class DictionaryDownloadService : IDictionaryDownloadService
{
    private readonly IDictionaryResourceService _resourceService;
    private readonly HttpClient _httpClient;
    private const int MinimumDictionaryFiles = 20000;

    public DictionaryDownloadService(IDictionaryResourceService resourceService)
    {
        Console.WriteLine("[DictionaryDownloadService] Constructor called");
        _resourceService = resourceService;
        _httpClient = new HttpClient();
    }

    public bool DictionaryExists(string dictionaryPath)
    {
        Console.WriteLine($"[DictionaryDownloadService] DictionaryExists check for path: {dictionaryPath}");
        
        if (string.IsNullOrEmpty(dictionaryPath) || !Directory.Exists(dictionaryPath))
        {
            Console.WriteLine("[DictionaryDownloadService] Path is empty or doesn't exist");
            return false;
        }

        try
        {
            var fileCount = Directory.GetFiles(dictionaryPath, "*.json").Length;
            var isValid = fileCount >= MinimumDictionaryFiles;
            Console.WriteLine($"[DictionaryDownloadService] Found {fileCount} files, minimum required: {MinimumDictionaryFiles}, valid: {isValid}");
            return isValid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DictionaryDownloadService] Error checking dictionary: {ex.Message}");
            return false;
        }
    }

    public async Task EnsureDictionaryExistsAsync(string dictionaryPath, DictionaryDownloadSource downloadSource, Action<string, double>? progressCallback = null)
    {
        Console.WriteLine("[DictionaryDownloadService] EnsureDictionaryExistsAsync called");
        Console.WriteLine($"[DictionaryDownloadService] Dictionary path: {dictionaryPath}");
        Console.WriteLine($"[DictionaryDownloadService] Download source: {downloadSource}");

        if (string.IsNullOrEmpty(dictionaryPath))
        {
            var errorMessage = "Dictionary path is not configured.";
            Console.WriteLine($"[DictionaryDownloadService] {errorMessage}");
            progressCallback?.Invoke(errorMessage, 0);
            throw new InvalidOperationException(errorMessage);
        }

        // Check if directory exists and has enough files
        if (Directory.Exists(dictionaryPath))
        {
            try
            {
                var fileCount = Directory.GetFiles(dictionaryPath, "*.json").Length;
                Console.WriteLine($"[DictionaryDownloadService] Found {fileCount} files in existing directory");

                if (fileCount >= MinimumDictionaryFiles)
                {
                    Console.WriteLine("[DictionaryDownloadService] Dictionary already exists with sufficient files, skipping download");
                    progressCallback?.Invoke("Dictionary already exists. No download needed.", 100);
                    return;
                }

                // Directory exists but doesn't have enough files - it's broken
                Console.WriteLine($"[DictionaryDownloadService] Dictionary is broken (only {fileCount} files, need {MinimumDictionaryFiles}), removing...");
                progressCallback?.Invoke($"Dictionary incomplete ({fileCount} files). Removing and re-downloading...", 5);
                Directory.Delete(dictionaryPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DictionaryDownloadService] Error checking/removing existing directory: {ex.Message}");
                progressCallback?.Invoke("Error checking dictionary. Re-downloading...", 5);
                try
                {
                    Directory.Delete(dictionaryPath, true);
                }
                catch
                {
                    // Ignore deletion errors
                }
            }
        }

        Console.WriteLine("[DictionaryDownloadService] Starting download...");
        await DownloadAndExtractDictionaryAsync(dictionaryPath, downloadSource, progressCallback);
    }

    private async Task DownloadAndExtractDictionaryAsync(string dictionaryPath, DictionaryDownloadSource downloadSource, Action<string, double>? progressCallback)
    {
        Console.WriteLine("[DictionaryDownloadService] DownloadAndExtractDictionaryAsync started");
        Console.WriteLine($"[DictionaryDownloadService] Dictionary path: {dictionaryPath}");
        Console.WriteLine($"[DictionaryDownloadService] Download source: {downloadSource}");
        Console.WriteLine($"[DictionaryDownloadService] progressCallback is null: {progressCallback == null}");

        var parentDirectory = Directory.GetParent(dictionaryPath)?.FullName ?? Directory.GetCurrentDirectory();
        var tempZipPath = Path.Combine(parentDirectory, "dictionary_temp.zip");

        try
        {
            Console.WriteLine("[DictionaryDownloadService] Invoking progress callback: Fetching download URL...");
            progressCallback?.Invoke("Fetching download URL...", 0);

            Console.WriteLine("[DictionaryDownloadService] Calling GetDictionaryDownloadUrlAsync...");
            var downloadUrl = await _resourceService.GetDictionaryDownloadUrlAsync(downloadSource);
            Console.WriteLine($"[DictionaryDownloadService] Download URL: {downloadUrl}");

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
            ZipFile.ExtractToDirectory(tempZipPath, parentDirectory);
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
            if (File.Exists(tempZipPath))
            {
                Console.WriteLine("[DictionaryDownloadService] Deleting temp zip file");
                File.Delete(tempZipPath);
            }
        }
    }
}
