using System.Reflection.PortableExecutable;
using System.Text;

namespace Requests.Core
{
    public record HttpResponseData
    {
        public byte[]? Data;

        /// <summary>
        /// Decodes and returns content as a string.
        /// </summary>
        public string DecodeAsString(string? encoding)
        {
            if (this.Data is null)
                return string.Empty;

            try
            {
                var encoder = Encoding.GetEncoding(encoding);
                return encoder.GetString(this.Data);
            }
            catch (Exception)
            {
                return Encoding.UTF8.GetString(this.Data);
            }
        }
    }

}


