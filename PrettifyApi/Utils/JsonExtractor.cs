using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PrettifyApi.Utils
{
    /// <summary>
    /// Utility class for extracting the "output" text from a string.
    /// </summary>
    public static class JsonExtractor
    {
        /// <summary>
        /// Extracts the value of the "output" property from the input text.
        /// Handles incomplete or truncated JSON gracefully.
        /// </summary>
        /// <param name="text">The text containing JSON with an "output" property.</param>
        /// <param name="defaultValue">Value returned if extraction fails (default: empty string).</param>
        /// <returns>The extracted "output" string, or defaultValue if not found.</returns>
        public static string ExtractJson(string? json, string defaultValue = "")
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("output", out var outputElement))
                {
                    return outputElement.GetString();
                }
            }
            catch (JsonException)
            {
                // JSON might be incomplete or malformed
                // Try a simple fallback using string manipulation
                var marker = "\"output\":";
                int index = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    index += marker.Length;
                    // Find the first ending quote after marker
                    int startQuote = json.IndexOf('"', index);
                    if (startQuote >= 0)
                    {
                        startQuote++;
                        int endQuote = json.LastIndexOf('"');
                        if (endQuote > startQuote)
                        {
                            return json[startQuote..endQuote].Replace("\\n", "\n").Replace("\\\"", "\"");
                        }
                    }
                }
            }

            return null;
        }
    }
}
