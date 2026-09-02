using System;
using System.Text.RegularExpressions;

namespace Normalisation.Core.Processors
{
    public static class UrlProcessor
    {

        /// <summary>
        /// Resolves relative and absolute URI references into unique absolute URLs.
        /// </summary>
        public static HashSet<Uri> MakeAbsolute(IEnumerable<string> uriReferences, Uri baseUrl)
        {
            if (uriReferences == null) return new HashSet<Uri>();

            baseUrl = GetBaseFolderUri(baseUrl);

            var baseUri =
                baseUrl.Scheme == Uri.UriSchemeHttp ||
                baseUrl.Scheme == Uri.UriSchemeHttps
                    ? baseUrl 
                    : new Uri("https://" + baseUrl.Host);

            var uniqueUrls = new HashSet<Uri>();

            foreach (var uriReference in uriReferences)
            {
                if (string.IsNullOrWhiteSpace(uriReference))
                    continue;

                var trimmedReference = uriReference.Trim();
                Uri? absolute = null;

                // Skip javascript/mailto fragments
                if (trimmedReference.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    trimmedReference.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                    trimmedReference == "#")
                {
                    continue;
                }

                // Protocol-relative URLs (//example.com)
                if (trimmedReference.StartsWith("//"))
                {
                    var afterSlashes = trimmedReference.Substring(2);

                    // if looks like a real domain (contains dot)
                    if (afterSlashes.Contains('.'))
                    {
                        var urlWithScheme = $"{baseUri.Scheme}:{trimmedReference}";
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
                else if (trimmedReference.StartsWith("/") || trimmedReference.StartsWith("./") || !trimmedReference.Contains("://"))
                {
                    absolute = new Uri(baseUri, trimmedReference);
                }

                // Fully-qualified absolute URLs (http, https)
                else if (Uri.TryCreate(trimmedReference, UriKind.Absolute, out var abs))
                {
                    // Prevent accidental "file://"
                    if (abs.Scheme == Uri.UriSchemeFile)
                    {
                        absolute = new Uri(baseUri, trimmedReference);
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


        /// <summary>
        /// Filters URLs to the allowed schemes.
        /// </summary>
        public static HashSet<Uri> FilterByScheme(HashSet<Uri> urls, IEnumerable<string> schemes)
        {
            if (!schemes.Any()) return urls;

            return urls.Where(u => schemes.Contains(u.Scheme)).ToHashSet();
        }


        /// <summary>
        /// Filters URLs using one or more regular expressions.
        /// </summary>
        public static HashSet<Uri> FilterByRegex(
            HashSet<Uri> urls,
            IEnumerable<string> regexPatterns)
        {
            var patterns = new List<Regex>();

            //// Split form input on new lines
            //var lines = regex
            //    .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pattern in regexPatterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                    continue;

                try
                {
                    //compile valid patterns
                    patterns.Add(
                        new Regex(pattern.Trim(), RegexOptions.Compiled));
                }
                catch
                {
                    // Ignore invalid regex line
                }
            }

            // Return original set if no patterns provided
            if (patterns.Count == 0)
                return urls;

            return urls
                .Where(url => patterns.Any(
                    pattern => pattern.IsMatch(url.AbsoluteUri)))
                .ToHashSet();
        }


        /// <summary>
        /// Removes URLs that point to external hosts.
        /// </summary>
        public static HashSet<Uri> RemoveExternalLinks(HashSet<Uri> urls, Uri baseUrl)
        {
            return urls.Where(url => IsInternalLink(url, baseUrl)).ToHashSet();
        }


        /// <summary>
        /// Removes query strings from URLs.
        /// </summary>
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


        /// <summary>
        /// Removes links that point back to the current page.
        /// </summary>
        public static HashSet<Uri> RemoveCyclicalLinks(HashSet<Uri> urls, Uri baseUrl)
        {
            return urls.Where(u => !u.Equals(baseUrl)).ToHashSet();
        }


        /// <summary>
        /// Limits the number of links returned using a deterministic URL order.
        /// </summary>
        public static HashSet<Uri> LimitLinks(HashSet<Uri> urls, int size)
        {
            return urls
                .OrderBy(url => url.AbsoluteUri, StringComparer.Ordinal)
                .Take(size)
                .ToHashSet();
        }


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


        /// <summary>
        /// Removes the fragment from a URL.
        /// </summary>
        private static Uri RemoveFragment(Uri uri)
        {
            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                return new Uri(uri.GetLeftPart(UriPartial.Query));
            }
            return uri;
        }


        /// <summary>
        /// Determines whether a URL belongs to the same host as the base URL.
        /// </summary>
        private static bool IsInternalLink(Uri url, Uri baseUrl)
        {
            return url.Authority == baseUrl.Authority;
        }

    }
}
