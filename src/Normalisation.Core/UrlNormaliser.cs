using System;

namespace Normalisation.Core
{
    public static class UrlNormaliser
    {
        public static IEnumerable<string> Truncate(IEnumerable<string> urls, int size)
        {
            return urls.Take(size);
        }

        public static Uri GetBaseUrl(Uri url)
        {
            var baseUrl = $"{url.Scheme}://{url.Authority}";

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                throw new ArgumentException(nameof(baseUrl), $"Invalid Url: {url}");
            }

            return baseUri;
        }

        public static Uri RemoveFragments(Uri uri)
        {
            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                return new Uri(uri.GetLeftPart(UriPartial.Query));
            }
            return uri;
        }

        public static IEnumerable<string> MakeAbsolute(IEnumerable<string> urls, Uri baseUrl)
        {
            var baseUri = GetBaseUrl(baseUrl);
            var uniqueUrls = new HashSet<string>();

            foreach (var url in urls)
            {
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                string absolute;

                if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
                {
                    absolute = abs.AbsoluteUri;
                }
                else
                {
                    var combined = new Uri(baseUri, url);
                    absolute = RemoveFragments(combined).AbsoluteUri;
                }

                uniqueUrls.Add(absolute);
            }

            return uniqueUrls;
        }

        public static IEnumerable<string> FilterBySchema(IEnumerable<string> urls, IEnumerable<string> schemas)
        {
            if (!schemas.Any()) return urls;

            return urls.Where(url =>
                schemas.Any(schema => url.StartsWith(schema + ":", StringComparison.OrdinalIgnoreCase)));
        }

        public static IEnumerable<string> FilterByPath(IEnumerable<string> urls, IEnumerable<string>? paths)
        {
            if (paths is null || !paths.Any()) return urls;
            
            return urls.Where(url =>
                Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                paths.Any(p => uri.AbsolutePath.Contains(p, StringComparison.OrdinalIgnoreCase))
            );
        }

        public static IEnumerable<string> RemoveExternalLinks(IEnumerable<string> urls, Uri baseUrl)
        {
            return urls.Where(url => IsInternalLink(url, baseUrl));
        }

        private static bool IsInternalLink(string url, Uri baseUrl)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (uri.Authority == baseUrl.Authority)
                    return true;
            }
            return false;
        }

        public static IEnumerable<string> RemoveQueryStrings(IEnumerable<string> urls)
        {
            var results = new HashSet<string>();
            foreach (var url in urls)
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    if (uri.Query.Any()) {
                        results.Add(new UriBuilder(uri) { Query = string.Empty }.Uri.AbsoluteUri);
                    } 
                    else
                    {
                        results.Add(url);
                    }
                }
            }
            return results;
        }

    }
}
