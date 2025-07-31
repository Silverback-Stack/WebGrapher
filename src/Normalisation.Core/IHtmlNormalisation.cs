
namespace Normalisation.Core
{
    public interface IHtmlNormalisation
    {
        public string NormaliseTitle(string text);

        public string NormaliseContent(string text);
        
        public string NormaliseKeywords(string text);

        IEnumerable<Uri> NormaliseLinks(
            IEnumerable<string> links, 
            Uri baseUrl, 
            bool allowExternal, 
            bool removeQueryStrings,
            IEnumerable<string> pathFilters);
    }
}