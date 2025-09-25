using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace VoiceReader
{
    /// <summary>
    /// Service for managing Vosk models - downloading, extracting, and tracking status
    /// </summary>
    public class VoskModelManager : IDisposable
    {
        private const string MODEL_LIST_URL = "https://alphacephei.com/vosk/models/model-list.json";
        private const string APP_TEMP_FOLDER = "VoiceReader";
        private const string MODELS_FOLDER = "models";
        
        private readonly HttpClient httpClient;
        private readonly string appTempPath;
        private readonly string modelsPath;
        
        public event EventHandler<ModelDownloadProgressEventArgs>? DownloadProgressChanged;
        public event EventHandler<ModelStatusChangedEventArgs>? ModelStatusChanged;

        public VoskModelManager()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(30); // Long timeout for large downloads
            
            // Use Windows TEMP directory
            appTempPath = Path.Combine(Path.GetTempPath(), APP_TEMP_FOLDER);
            modelsPath = Path.Combine(appTempPath, MODELS_FOLDER);
            
            // Create application temp directory and models subdirectory if they don't exist
            if (!Directory.Exists(appTempPath))
            {
                Directory.CreateDirectory(appTempPath);
            }
            
            if (!Directory.Exists(modelsPath))
            {
                Directory.CreateDirectory(modelsPath);
            }
        }

        /// <summary>
        /// Fetches the list of available models, starting with local models then online models
        /// </summary>
        public async Task<List<VoskModel>> GetAvailableModelsAsync()
        {
            try
            {
                // First, get all local models
                var localModels = await GetLocalModelsAsync();
                var allModels = new List<VoskModel>(localModels);
                
                // Then fetch online models and merge with local ones
                var response = await httpClient.GetStringAsync(MODEL_LIST_URL);
                var onlineModels = JsonConvert.DeserializeObject<List<VoskModel>>(response) ?? new List<VoskModel>();

                // Filter out spk and tts models
                onlineModels = onlineModels.Where(x => !x.Type.Contains("spk") & !x.Type.Contains("tts")).ToList();

                // Merge online models with local models, avoiding duplicates
                foreach (var onlineModel in onlineModels)
                {
                    var existingModel = allModels.FirstOrDefault(m => m.Name == onlineModel.Name);
                    if (existingModel != null)
                    {
                        // Update existing local model with online information
                        existingModel.Url = onlineModel.Url;
                        existingModel.Md5 = onlineModel.Md5;
                        existingModel.Size = onlineModel.Size;
                        if (string.IsNullOrEmpty(existingModel.SizeText))
                            existingModel.SizeText = onlineModel.SizeText;
                        if (string.IsNullOrEmpty(existingModel.LanguageText))
                            existingModel.LanguageText = onlineModel.LanguageText;
                        if (string.IsNullOrEmpty(existingModel.Language))
                            existingModel.Language = onlineModel.Language;
                        if (string.IsNullOrEmpty(existingModel.Type))
                            existingModel.Type = onlineModel.Type;
                        if (string.IsNullOrEmpty(existingModel.Version))
                            existingModel.Version = onlineModel.Version;
                        existingModel.ObsoleteString = onlineModel.ObsoleteString;
                    }
                    else
                    {
                        // Add new online model
                        onlineModel.Status = ModelStatus.Available;
                        allModels.Add(onlineModel);
                    }
                }
                
                return allModels.OrderBy(m => m.LanguageText).ThenBy(m => m.Name).ToList();
            }
            catch (Exception ex)
            {
                // If network fails, return only local models
                var localModels = await GetLocalModelsAsync();
                if (localModels.Count > 0)
                {
                    return localModels;
                }
                throw new Exception($"Failed to fetch model list and no local models available: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Downloads and extracts a model
        /// </summary>
        public async Task<bool> DownloadModelAsync(VoskModel model, CancellationToken cancellationToken = default)
        {
            if (model.Status != ModelStatus.Available)
                return false;

            try
            {
                model.Status = ModelStatus.Downloading;
                OnModelStatusChanged(model);

                var modelFolderPath = Path.Combine(modelsPath, model.Name);
                var zipFilePath = Path.Combine(modelsPath, $"{model.Name}.zip");

                // Download the model
                using (var response = await httpClient.GetAsync(model.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var downloadedBytes = 0L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(zipFilePath))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                            downloadedBytes += bytesRead;
                            
                            var progress = totalBytes > 0 ? (int)((downloadedBytes * 100) / totalBytes) : 0;
                            OnDownloadProgressChanged(model, progress, downloadedBytes, totalBytes);
                        }
                    }
                }

                // Verify MD5 if provided
                if (!string.IsNullOrEmpty(model.Md5))
                {
                    var calculatedMd5 = CalculateMD5(zipFilePath);
                    if (!calculatedMd5.Equals(model.Md5, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(zipFilePath);
                        throw new Exception("Downloaded file MD5 checksum does not match expected value");
                    }
                }

                // Extract the model
                if (Directory.Exists(modelFolderPath))
                {
                    Directory.Delete(modelFolderPath, true);
                }
                
                ZipFile.ExtractToDirectory(zipFilePath, modelsPath);
                
                // Save model information to info.json in the model directory
                await SaveModelInfoAsync(model, modelFolderPath);
                
                // Delete the zip file after extraction
                File.Delete(zipFilePath);
                
                // Update model status
                model.Status = ModelStatus.Downloaded;
                model.LocalPath = modelFolderPath;
                OnModelStatusChanged(model);
                
                return true;
            }
            catch (OperationCanceledException)
            {
                model.Status = ModelStatus.Available;
                OnModelStatusChanged(model);
                throw;
            }
            catch (Exception ex)
            {
                model.Status = ModelStatus.Available;
                OnModelStatusChanged(model);
                throw new Exception($"Failed to download model {model.Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves model information to info.json file in the model directory
        /// </summary>
        private async Task SaveModelInfoAsync(VoskModel model, string modelFolderPath)
        {
            try
            {
                var infoFilePath = Path.Combine(modelFolderPath, "info.json");
                
                // Create model info object with available data
                var modelInfo = new
                {
                    lang = model.Language,
                    lang_text = model.LanguageText,
                    name = model.Name,
                    type = model.Type,
                    version = model.Version,
                    size = model.Size,
                    size_text = model.SizeText,
                    url = model.Url,
                    md5 = model.Md5,
                    obsolete = model.IsObsolete.ToString().ToLower(),
                    download_date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    local_path = modelFolderPath
                };
                
                // Serialize to JSON and save
                var jsonContent = JsonConvert.SerializeObject(modelInfo, Formatting.Indented);
                await File.WriteAllTextAsync(infoFilePath, jsonContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the download process
                // In a production app, you might want to use a proper logging framework
                System.Diagnostics.Debug.WriteLine($"Failed to save model info: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets only locally downloaded models without fetching from internet
        /// </summary>
        public async Task<List<VoskModel>> GetLocalModelsAsync()
        {
            var localModels = new List<VoskModel>();
            
            try
            {
                // Check if models directory exists
                if (!Directory.Exists(modelsPath))
                    return localModels;
                
                // Get all directories in models path
                var modelDirectories = Directory.GetDirectories(modelsPath);
                
                foreach (var modelDir in modelDirectories)
                {
                    var modelName = Path.GetFileName(modelDir);
                    
                    // Check if this directory contains model files (basic validation)
                    if (Directory.Exists(modelDir) && 
                        (File.Exists(Path.Combine(modelDir, "model")) || 
                         Directory.Exists(Path.Combine(modelDir, "am")) ||
                         File.Exists(Path.Combine(modelDir, "info.json"))))
                    {
                        var model = new VoskModel
                        {
                            Name = modelName,
                            LocalPath = modelDir,
                            Status = ModelStatus.Downloaded
                        };
                        
                        // Try to get model info from info.json if available
                        var infoPath = Path.Combine(modelDir, "info.json");
                        if (File.Exists(infoPath))
                        {
                            try
                            {
                                var infoContent = await File.ReadAllTextAsync(infoPath);
                                var infoData = JsonConvert.DeserializeObject<dynamic>(infoContent);
                                
                                // Read comprehensive model information
                                if (infoData?.lang != null)
                                    model.Language = infoData.lang.ToString();
                                if (infoData?.lang_text != null)
                                    model.LanguageText = infoData.lang_text.ToString();
                                if (infoData?.type != null)
                                    model.Type = infoData.type.ToString();
                                if (infoData?.version != null)
                                    model.Version = infoData.version.ToString();
                                long savedSize = 0;
                                if (infoData?.size != null && long.TryParse(infoData.size.ToString(), out savedSize))
                                    model.Size = savedSize;
                                if (infoData?.size_text != null)
                                    model.SizeText = infoData.size_text.ToString();
                                if (infoData?.url != null)
                                    model.Url = infoData.url.ToString();
                                if (infoData?.md5 != null)
                                    model.Md5 = infoData.md5.ToString();
                                if (infoData?.obsolete != null)
                                    model.ObsoleteString = infoData.obsolete.ToString();
                            }
                            catch
                            {
                                // If can't parse info.json, fall back to directory scanning
                            }
                        }
                        
                        // Get directory size if not from info.json
                        if (model.Size == 0)
                        {
                            /*
                            try
                            {
                                var dirInfo = new DirectoryInfo(modelDir);
                                var size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
                                model.Size = size;
                                model.SizeText = FormatBytes(size);
                            }
                            catch
                            {
                            */
                                model.SizeText = "Unknown";
                            //}
                        }
                        
                        localModels.Add(model);
                    }
                }
                
                return localModels.OrderBy(m => m.LanguageText).ThenBy(m => m.Name).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get local models: {ex.Message}", ex);
            }
        }
        
       
        /// <summary>
        /// Formats bytes to human readable string
        /// </summary>
        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        /// <summary>
        /// Gets the path where models are stored
        /// </summary>
        public string GetModelsPath()
        {
            return modelsPath;
        }

        /// <summary>
        /// Gets the application temp directory path
        /// </summary>
        public string GetAppTempPath()
        {
            return appTempPath;
        }

        /// <summary>
        /// Deletes a downloaded model
        /// </summary>
        public bool DeleteModel(VoskModel model)
        {
            try
            {
                if (Directory.Exists(model.LocalPath))
                {
                    Directory.Delete(model.LocalPath, true);
                }
                
                model.Status = ModelStatus.Available;
                model.LocalPath = string.Empty;
                OnModelStatusChanged(model);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Calculates MD5 hash of a file
        /// </summary>
        private string CalculateMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private void OnDownloadProgressChanged(VoskModel model, int progressPercentage, long downloadedBytes, long totalBytes)
        {
            DownloadProgressChanged?.Invoke(this, new ModelDownloadProgressEventArgs(model, progressPercentage, downloadedBytes, totalBytes));
        }

        private void OnModelStatusChanged(VoskModel model)
        {
            ModelStatusChanged?.Invoke(this, new ModelStatusChangedEventArgs(model));
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Event args for download progress updates
    /// </summary>
    public class ModelDownloadProgressEventArgs : EventArgs
    {
        public VoskModel Model { get; }
        public int ProgressPercentage { get; }
        public long DownloadedBytes { get; }
        public long TotalBytes { get; }

        public ModelDownloadProgressEventArgs(VoskModel model, int progressPercentage, long downloadedBytes, long totalBytes)
        {
            Model = model;
            ProgressPercentage = progressPercentage;
            DownloadedBytes = downloadedBytes;
            TotalBytes = totalBytes;
        }
    }

    /// <summary>
    /// Event args for model status changes
    /// </summary>
    public class ModelStatusChangedEventArgs : EventArgs
    {
        public VoskModel Model { get; }

        public ModelStatusChangedEventArgs(VoskModel model)
        {
            Model = model;
        }
    }
}