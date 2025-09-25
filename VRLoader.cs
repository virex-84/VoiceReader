using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace VoiceReader
{
    /// <summary>
    /// Represents a book loaded from a .vrbook file
    /// </summary>
    public class BookInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;

        [JsonProperty("contentType")]
        public string ContentType { get; set; } = string.Empty;

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Loader for .vrbook files (ZIP archives containing book.json)
    /// </summary>
    public class VRLoader : IDisposable
    {
        private const string APP_TEMP_FOLDER = "VoiceReader";

        private BookInfo? _book;
        private string? _extractedPath;
        private bool _disposed = false;

        /// <summary>
        /// Gets the book information from book.json
        /// </summary>
        public BookInfo? Book => _book;

        /// <summary>
        /// Gets the full path to the temporary folder where the vrbook was extracted
        /// </summary>
        public string? Path => _extractedPath;

        /// <summary>
        /// Loads a .vrbook file and extracts it to a temporary folder
        /// </summary>
        /// <param name="filename">Path to the .vrbook file</param>
        /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
        /// <exception cref="InvalidDataException">Thrown when the file is not a valid ZIP or doesn't contain book.json</exception>
        public void LoadFile(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"File not found: {filename}");
            }

            // Clean up previous extraction if any
            Cleanup();

            try
            {
                // First, read the title from book.json inside the ZIP to generate GUID
                string title = ExtractTitleFromZip(filename);
                
                // Generate GUID based on title
                string guid = GenerateGuidFromTitle(title);
                
                // Create temporary directory with the GUID
                string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), APP_TEMP_FOLDER);
                _extractedPath = System.IO.Path.Combine(tempDir, $"vrbook_{guid}");
                
                // Extract the ZIP file
                if (Directory.Exists(_extractedPath))
                {
                    Directory.Delete(_extractedPath, true);
                }
                
                ZipFile.ExtractToDirectory(filename, _extractedPath);
                
                // Load book.json
                string bookJsonPath = System.IO.Path.Combine(_extractedPath, "book.json");
                if (!File.Exists(bookJsonPath))
                {
                    throw new InvalidDataException("book.json not found in the .vrbook file");
                }
                
                string jsonContent = File.ReadAllText(bookJsonPath, Encoding.UTF8);
                _book = JsonConvert.DeserializeObject<BookInfo>(jsonContent);
                
                if (_book == null)
                {
                    throw new InvalidDataException("Failed to parse book.json");
                }
            }
            catch (Exception ex) when (!(ex is FileNotFoundException || ex is InvalidDataException))
            {
                Cleanup();
                throw new InvalidDataException($"Error loading .vrbook file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extracts the title from book.json inside the ZIP without fully extracting the archive
        /// </summary>
        /// <param name="zipFilePath">Path to the ZIP file</param>
        /// <returns>The title from book.json</returns>
        private string ExtractTitleFromZip(string zipFilePath)
        {
            using (var archive = ZipFile.OpenRead(zipFilePath))
            {
                var bookJsonEntry = archive.GetEntry("book.json");
                if (bookJsonEntry == null)
                {
                    throw new InvalidDataException("book.json not found in the .vrbook file");
                }

                using (var stream = bookJsonEntry.Open())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string jsonContent = reader.ReadToEnd();
                    var bookInfo = JsonConvert.DeserializeObject<BookInfo>(jsonContent);
                    
                    if (bookInfo == null || string.IsNullOrEmpty(bookInfo.Title))
                    {
                        throw new InvalidDataException("Invalid or missing title in book.json");
                    }
                    
                    return bookInfo.Title;
                }
            }
        }

        /// <summary>
        /// Generates a deterministic GUID based on the book title
        /// </summary>
        /// <param name="title">The book title</param>
        /// <returns>A GUID string</returns>
        private string GenerateGuidFromTitle(string title)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(title));
                
                // Convert the hash to a GUID format
                var guid = new Guid(hash);
                return guid.ToString("N"); // Returns GUID without hyphens
            }
        }

        /// <summary>
        /// Cleans up the extracted temporary folder
        /// </summary>
        private void Cleanup()
        {
            if (!string.IsNullOrEmpty(_extractedPath) && Directory.Exists(_extractedPath))
            {
                try
                {
                    Directory.Delete(_extractedPath, true);
                }
                catch (Exception)
                {
                    // Ignore cleanup errors - temporary files will be cleaned up eventually
                }
            }
            
            _extractedPath = null;
            _book = null;
        }

        /// <summary>
        /// Releases all resources used by the VRLoader
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Cleanup();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer that ensures cleanup if Dispose wasn't called
        /// </summary>
        ~VRLoader()
        {
            Dispose(false);
        }
    }
}