using Gefc.Ai.Gemini;
using PrettifyApi.Utils;
using System.Runtime.CompilerServices;
using System.Text;

namespace PrettifyApi.Services
{
    /// <summary>
    /// Service for interacting with Gemini API to prettify text or code using a single prompt template.
    /// Supports both normal and streaming responses.
    /// </summary>
    public class GeminiService : IDisposable
    {
        private readonly GeminiClient _client;
        private readonly string _promptTemplate;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="GeminiService"/>.
        /// </summary>
        /// <param name="apiKey">Gemini API key.</param>
        /// <param name="promptPath">Path to the prompt template file.</param>
        public GeminiService(string apiKey, string promptPath)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("Gemini API key is missing.", nameof(apiKey));

            _client = new GeminiClient(apiKey);
            _promptTemplate = LoadPromptTemplate(promptPath);
        }

        /// <summary>
        /// Prettifies text or code using the prompt template (normal response).
        /// </summary>
        public async Task<string> PrettifyAsync(string input, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "{}";

            string prompt = _promptTemplate.Replace("{input}", input);

            try
            {
                var response = await _client.GenerateContentAsync(prompt, modelName: "gemini-2.5-flash", cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                return "{}";
            }
        }

        /// <summary>
        /// Prettifies text or code using the prompt template (streaming response).
        /// Returns partial JSON text as it arrives.
        /// </summary>
        public async IAsyncEnumerable<string> PrettifyStreamAsync(string input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                yield break;
            }

            string prompt = _promptTemplate.Replace("{input}", input);

            await foreach (var part in _client.GenerateContentStreamAsync(prompt, modelName: "gemini-2.5-flash", cancellationToken))
            {
                if (!string.IsNullOrEmpty(part))
                    yield return part;
            }
        }

        #region Helpers

        private static string LoadPromptTemplate(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Prompt template file not found.", path);

            return File.ReadAllText(path, Encoding.UTF8);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _client.Dispose();
                _disposed = true;
            }
        }

        #endregion
    }
}
