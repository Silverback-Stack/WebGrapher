using System;
using System.Text.RegularExpressions;

namespace Normalisation.Core.Processors
{
    public static class UrlNormaliser
    {

        /// <summary>
        /// Returns a base folder URI suitable for resolving relative URLs.
        /// If the URL is a file (does not end with /), returns the parent folder.
        /// If it already ends with /, returns it as-is.
        /// </summary>
        private static Uri GetBaseFolderUri(Uri pageUrl)
        {
            if (pageUrl.AbsoluteUri.EndsWith("/"))
            {
                // Already a folder URL
                return pageUrl;
            }

            // Last segment of path
            var lastSegment = pageUrl.Segments.LastOrDefault() ?? "";

            if (!lastSegment.Contains("."))
            {
                // No dot → likely a folder URL missing trailing slash, add it
                return new Uri(pageUrl.AbsoluteUri + "/");
            }

            // Resolve relative to parent folder
            return new Uri(pageUrl, "."); // the "." trick gives parent folder
        }


        public static HashSet<Uri> MakeAbsolute(IEnumerable<string> urls, Uri baseUrl)
        {
            var baseUri = GetBaseFolderUri(baseUrl);
            var uniqueUrls = new HashSet<Uri>();

            foreach (var url in urls)
            {
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                var trimmedUrl = url.Trim();
                Uri absolute;

                // Handle protocol-relative URLs (//something)
                if (trimmedUrl.StartsWith("//"))
                {
                    // Prepend the scheme from baseUrl
                    var urlWithScheme = $"{baseUri.Scheme}:{trimmedUrl}";

                    if (Uri.TryCreate(urlWithScheme, UriKind.Absolute, out var abs) 
                        && abs.Host.Contains(".")) //simple heuristic to detect actual domain name 
                    {
                        // Valid host → treat as absolute
                        absolute = abs;
                    }
                    else
                    {
                        // No valid host → treat as relative path
                        absolute = new Uri(baseUri, trimmedUrl.Substring(2)); // remove leading //
                    }

                }
                else if (Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var abs))
                {
                    // Already absolute
                    absolute = abs;
                }
                else
                {
                    // Relative URL → resolve against base
                    absolute = new Uri(baseUri, trimmedUrl);
                }

                // Remove fragments (#something) if any
                absolute = RemoveFragment(absolute);
                uniqueUrls.Add(absolute);
            }

            return uniqueUrls;
        }

        private static Uri RemoveFragment(Uri uri)
        {
            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                return new Uri(uri.GetLeftPart(UriPartial.Query));
            }
            return uri;
        }

        public static HashSet<Uri> FilterBySchema(HashSet<Uri> urls, IEnumerable<string> schemas)
        {
            if (!schemas.Any()) return urls;

            return urls.Where(u => schemas.Contains(u.Scheme)).ToHashSet();
        }

        public static HashSet<Uri> FilterByRegex(HashSet<Uri> urls, string regex)
        {
            if (string.IsNullOrWhiteSpace(regex)) return urls;

            Regex pattern;
            try
            {
                // Compile the regex to validate syntax
                pattern = new Regex(regex, RegexOptions.Compiled);
            }
            catch (Exception)
            {
                // Invalid regex — return original set
                return urls;
            }

            var filtered = new HashSet<Uri>();
            foreach (var url in urls)
            {
                if (pattern.IsMatch(url.AbsoluteUri))
                {
                    filtered.Add(url);
                }
            }

            return filtered;
        }

        public static HashSet<Uri> RemoveExternalLinks(HashSet<Uri> urls, Uri baseUrl)
        {
            return urls.Where(url => IsInternalLink(url, baseUrl)).ToHashSet();
        }

        private static bool IsInternalLink(Uri url, Uri baseUrl)
        {
            return url.Authority == baseUrl.Authority;
        }

        public static HashSet<Uri> RemoveQueryStrings(HashSet<Uri> urls)
        {
            var results = new HashSet<Uri>();

            foreach (var url in urls)
            {
                var builder = new UriBuilder(url)
                {
                    Query = string.Empty
                };

                results.Add(builder.Uri);
            }
            return results;
        }

        public static HashSet<Uri> RemoveCyclicalLinks(HashSet<Uri> urls, Uri baseUrl)
        {
            return urls.Where(u => !u.Equals(baseUrl)).ToHashSet();
        }

        public static HashSet<Uri> Truncate(HashSet<Uri> urls, int size)
        {
            return urls.Take(size).ToHashSet();
        }

    }
}
