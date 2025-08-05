using System;
using System.IO;

namespace Normalisation.Core.Processors
{
    public static class UrlNormaliser
    {
        private static Uri GetBaseUrl(Uri url)
        {
            var baseUrl = $"{url.Scheme}://{url.Authority}";

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                throw new ArgumentException(nameof(baseUrl), $"Invalid Url: {url}");
            }

            return baseUri;
        }

        public static HashSet<Uri> MakeAbsolute(IEnumerable<string> urls, Uri baseUrl)
        {
            var baseUri = GetBaseUrl(baseUrl);
            var uniqueUrls = new HashSet<Uri>();

            foreach (var url in urls)
            {
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                Uri absolute;

                if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
                {
                    absolute = abs;
                }
                else
                {
                    var combined = new Uri(baseUri, url);
                    absolute = RemoveFragment(combined);
                }

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

        public static HashSet<Uri> FilterByPath(HashSet<Uri> urls, IEnumerable<string>? paths)
        {
            if (paths is null || !paths.Any()) return urls;

            return urls.Where(u =>
                paths.Any(p => u.AbsolutePath.Contains(p, StringComparison.OrdinalIgnoreCase))
            ).ToHashSet();
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

        public static HashSet<Uri> RemoveTrailingSlash(HashSet<Uri> urls)
        {
            var results = new HashSet<Uri>();

            foreach (var url in urls)
            {
                var path = url.AbsolutePath;
                var builder = new UriBuilder(url)
                {
                    Path = path == "/" ? path : path.TrimEnd('/')
                };

                results.Add(builder.Uri);
            }

            return results;
        }
        public static HashSet<Uri> Truncate(HashSet<Uri> urls, int size)
        {
            return urls.Take(size).ToHashSet();
        }

    }
}
