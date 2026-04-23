using System.Text;

namespace Requests.Core
{
    public record HttpResponseData
    {
        public byte[]? Payload;

        /// <summary>
        /// Decodes and returns content as a string.
        /// </summary>
        public string DecodeAsString(string? encoding)
        {
            // Return empty if no data
            if (Payload is null || Payload.Length == 0)
                return string.Empty;

            try
            {
                // Attempt to use the provided encoding name
                var encoder = Encoding.GetEncoding(encoding!);

                // Decode byte payload using requested encoding
                return encoder.GetString(this.Payload);
            }
            catch (Exception)
            {
                // Fallback: use UTF-8 with replacement for invalid bytes
                var utf8Encoding = Encoding.GetEncoding(
                    "UTF-8",
                    EncoderFallback.ReplacementFallback,
                    DecoderFallback.ReplacementFallback);

                // Decode payload safely, substituting invalid sequences
                return utf8Encoding.GetString(this.Payload);
            }
        }
    }

}


