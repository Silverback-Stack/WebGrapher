using System.Reflection.PortableExecutable;
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
            if (this.Payload is null)
                return string.Empty;

            try
            {
                var encoder = Encoding.GetEncoding(encoding!);
                return encoder.GetString(this.Payload);
            }
            catch (Exception)
            {
                return Encoding.UTF8.GetString(this.Payload);
            }
        }
    }

}


