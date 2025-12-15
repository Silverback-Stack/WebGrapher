using Microsoft.Extensions.Logging;
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
            if (urls == null) return new HashSet<Uri>();

            baseUrl = GetBaseFolderUri(baseUrl);
            var baseUri = baseUrl.Scheme.StartsWith("http") ? baseUrl : new Uri("https://" + baseUrl.Host);

            var uniqueUrls = new HashSet<Uri>();

            foreach (var url in urls)
            {
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                var trimmedUrl = url.Trim();
                Uri absolute = null;

                // Skip javascript/mailto fragments
                if (trimmedUrl.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    trimmedUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                    trimmedUrl == "#")
                {
                    continue;
                }

                // Protocol-relative URLs (//example.com)
                if (trimmedUrl.StartsWith("//"))
                {
                    var afterSlashes = trimmedUrl.Substring(2);

                    // if looks like a real domain (contains dot)
                    if (afterSlashes.Contains('.'))
                    {
                        var urlWithScheme = $"{baseUri.Scheme}:{trimmedUrl}";
                        if (Uri.TryCreate(urlWithScheme, UriKind.Absolute, out var abs))
                            absolute = abs;
                    }
                    else
                    {
                        // treat as relative path from base
                        absolute = new Uri(baseUri, afterSlashes);
                    }
                }

                // Relative URLs - start with "/" or "./" or no scheme
                else if (trimmedUrl.StartsWith("/") || trimmedUrl.StartsWith("./") || !trimmedUrl.Contains("://"))
                {
                    absolute = new Uri(baseUri, trimmedUrl);
                }

                // Fully-qualified absolute URLs (http, https)
                else if (Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var abs))
                {
                    // Prevent accidental "file://"
                    if (abs.Scheme == Uri.UriSchemeFile)
                    {
                        absolute = new Uri(baseUri, trimmedUrl);
                    }
                    else
                    {
                        absolute = abs;
                    }
                }

                if (absolute != null)
                {
                    absolute = RemoveFragment(absolute);
                    uniqueUrls.Add(absolute);
                }
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

            var patterns = new List<Regex>();

            // Split form input on new lines
            var lines = regex
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    //compile valid patterns
                    patterns.Add(new Regex(line.Trim(), RegexOptions.Compiled));
                }
                catch
                {
                    // Ignore invalid regex line
                }
            }

            // If nothing valid, return original set
            if (patterns.Count == 0)
                return urls;


            var filtered = new HashSet<Uri>();

            foreach (var url in urls)
            {
                // Convert Url to string and try match any pattern
                if (patterns.Any(p => p.IsMatch(url.AbsoluteUri)))
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
