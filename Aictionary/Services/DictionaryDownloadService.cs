using System;
using System.Collections.Generic;
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

    private static readonly HashSet<string> WindowsReservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private static bool IsWindowsReservedName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // Check if the name (without extension) is reserved
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
        return WindowsReservedNames.Contains(nameWithoutExtension);
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
            progressCallback?.Invoke("Downloading dictionary...", 5);

            Console.WriteLine("[DictionaryDownloadService] Starting HTTP request...");
            using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            Console.WriteLine($"[DictionaryDownloadService] HTTP response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to download dictionary. Server returned status code: {response.StatusCode}");
            }

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var canReportProgress = totalBytes != -1;
            Console.WriteLine($"[DictionaryDownloadService] Total bytes: {totalBytes}, Can report progress: {canReportProgress}");

            if (!canReportProgress)
            {
                progressCallback?.Invoke("Downloading dictionary (size unknown)...", 5);
            }

            Console.WriteLine("[DictionaryDownloadService] Starting file download...");
            await using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920))
            {
                await using var contentStream = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[81920]; // 80KB buffer for better performance
                var totalRead = 0L;
                int bytesRead;
                var lastProgressUpdate = 0L;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    // Update progress more frequently (every 500KB) to avoid appearing stuck
                    if (canReportProgress && (totalRead - lastProgressUpdate) >= (512 * 1024))
                    {
                        lastProgressUpdate = totalRead;
                        var downloadedMB = totalRead / (1024.0 * 1024);
                        var totalMB = totalBytes / (1024.0 * 1024);
                        var progress = 5 + (totalRead * 75.0 / totalBytes);

                        Console.WriteLine($"[DictionaryDownloadService] Downloaded: {downloadedMB:F1} MB / {totalMB:F1} MB ({progress:F1}%)");
                        progressCallback?.Invoke($"Downloading: {downloadedMB:F1} MB / {totalMB:F1} MB", progress);
                    }
                    else if (!canReportProgress && (totalRead - lastProgressUpdate) >= (5 * 1024 * 1024))
                    {
                        lastProgressUpdate = totalRead;
                        var downloadedMB = totalRead / (1024.0 * 1024);
                        Console.WriteLine($"[DictionaryDownloadService] Downloaded: {downloadedMB:F1} MB");
                        progressCallback?.Invoke($"Downloading: {downloadedMB:F1} MB", 50);
                    }
                }

                Console.WriteLine($"[DictionaryDownloadService] Download complete. Total read: {totalRead} bytes ({totalRead / (1024.0 * 1024):F1} MB)");
                progressCallback?.Invoke($"Download complete ({totalRead / (1024.0 * 1024):F1} MB)", 80);
            }

            // Ensure the directory doesn't exist before extraction
            if (Directory.Exists(dictionaryPath))
            {
                Console.WriteLine("[DictionaryDownloadService] Removing existing dictionary directory before extraction...");
                progressCallback?.Invoke("Preparing extraction...", 82);
                Directory.Delete(dictionaryPath, true);
            }

            Console.WriteLine("[DictionaryDownloadService] Starting extraction...");
            progressCallback?.Invoke("Extracting dictionary files...", 85);

            try
            {
                // Extract with overwrite support for Windows
                using (var archive = System.IO.Compression.ZipFile.OpenRead(tempZipPath))
                {
                    var totalEntries = archive.Entries.Count;
                    var extractedEntries = 0;
                    var skippedEntries = 0;
                    var lastProgressUpdate = 0;

                    Console.WriteLine($"[DictionaryDownloadService] Extracting {totalEntries} files...");

                    foreach (var entry in archive.Entries)
                    {
                        // Check for Windows reserved names in the path
                        var pathParts = entry.FullName.Split('/', '\\');
                        var hasReservedName = false;

                        foreach (var part in pathParts)
                        {
                            if (IsWindowsReservedName(part))
                            {
                                hasReservedName = true;
                                Console.WriteLine($"[DictionaryDownloadService] Skipping entry with Windows reserved name: {entry.FullName}");
                                skippedEntries++;
                                break;
                            }
                        }

                        if (hasReservedName)
                        {
                            extractedEntries++;
                            continue;
                        }

                        var destinationPath = Path.Combine(parentDirectory, entry.FullName);

                        // Create directory if it's a directory entry
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            try
                            {
                                Directory.CreateDirectory(destinationPath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[DictionaryDownloadService] Failed to create directory {destinationPath}: {ex.Message}");
                                skippedEntries++;
                            }
                        }
                        else
                        {
                            try
                            {
                                // Ensure parent directory exists
                                var destinationDir = Path.GetDirectoryName(destinationPath);
                                if (!string.IsNullOrEmpty(destinationDir))
                                {
                                    Directory.CreateDirectory(destinationDir);
                                }

                                // Extract file with overwrite
                                entry.ExtractToFile(destinationPath, overwrite: true);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[DictionaryDownloadService] Failed to extract {entry.FullName}: {ex.Message}");
                                skippedEntries++;
                            }
                        }

                        extractedEntries++;

                        // Update progress every 5% of files
                        var progressPercent = (extractedEntries * 100) / totalEntries;
                        if (progressPercent - lastProgressUpdate >= 5)
                        {
                            lastProgressUpdate = progressPercent;
                            var extractionProgress = 85 + (progressPercent * 0.14); // 85% to 99%
                            Console.WriteLine($"[DictionaryDownloadService] Extracted {extractedEntries}/{totalEntries} files ({progressPercent}%)");
                            progressCallback?.Invoke($"Extracting: {extractedEntries}/{totalEntries} files ({progressPercent}%)", extractionProgress);
                        }
                    }

                    if (skippedEntries > 0)
                    {
                        Console.WriteLine($"[DictionaryDownloadService] Skipped {skippedEntries} problematic files during extraction");
                    }
                }

                Console.WriteLine("[DictionaryDownloadService] Extraction complete");
                progressCallback?.Invoke("Verifying installation...", 99);

                // Verify extraction succeeded
                if (!Directory.Exists(dictionaryPath))
                {
                    throw new InvalidOperationException("Extraction failed: Dictionary directory not found after extraction. The archive may not contain the expected 'dictionary' folder.");
                }

                var fileCount = Directory.GetFiles(dictionaryPath, "*.json").Length;
                Console.WriteLine($"[DictionaryDownloadService] Verification: Found {fileCount} dictionary files");

                if (fileCount < MinimumDictionaryFiles)
                {
                    throw new InvalidOperationException($"Extraction incomplete: Only {fileCount} files found, expected at least {MinimumDictionaryFiles}. Please try downloading again.");
                }

                Console.WriteLine("[DictionaryDownloadService] Installation complete and verified");
                progressCallback?.Invoke("Download complete!", 100);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"[DictionaryDownloadService] Access denied during extraction: {ex.Message}");
                throw new InvalidOperationException($"Cannot extract files - access denied. Please run the application as administrator or choose a different dictionary location. Error: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[DictionaryDownloadService] IO error during extraction: {ex.Message}");
                throw new InvalidOperationException($"File system error during extraction. Please ensure you have enough disk space and the dictionary path is accessible. Error: {ex.Message}", ex);
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[DictionaryDownloadService] Network error: {ex.Message}");
            throw new InvalidOperationException($"Network error while downloading dictionary. Please check your internet connection and try again. Error: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[DictionaryDownloadService] Download timeout: {ex.Message}");
            throw new InvalidOperationException($"Download timed out. Please check your internet connection and try again. Error: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Console.WriteLine($"[DictionaryDownloadService] Unexpected error: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[DictionaryDownloadService] Stack trace: {ex.StackTrace}");
            throw new InvalidOperationException($"Unexpected error during dictionary download: {ex.Message}. Please try again or contact support if the problem persists.", ex);
        }
        finally
        {
            if (File.Exists(tempZipPath))
            {
                try
                {
                    Console.WriteLine("[DictionaryDownloadService] Deleting temp zip file");
                    File.Delete(tempZipPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DictionaryDownloadService] Failed to delete temp file: {ex.Message}");
                }
            }
        }
    }
}
