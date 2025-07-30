using System;
using Graphing.Core.Models;

namespace Graphing.Core.Adapters.AzureCosmoGraph
{
    public class AzureCosmoGraphAnalyserAdapter : IGraphAnalyser
    {
        public int AverageLinksPerNode()
        {
            throw new NotImplementedException();
        }

        public void FindPagesByDomain(string domain)
        {
            throw new NotImplementedException();
        }

        public void FindReachablePages(string fromUrl, int maxDepth)
        {
            throw new NotImplementedException();
        }

        public void GetDeadEnds()
        {
            throw new NotImplementedException();
        }

        public void GetIsolatedNodes()
        {
            throw new NotImplementedException();
        }

        public void GetMostLinkedPages(int topN)
        {
            throw new NotImplementedException();
        }

        public void GetShortestPath(string fromUrl, string toUrl)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Node> SearchPagesByKeyword(string keyword)
        {
            throw new NotImplementedException();
        }

        public int TotalEdges()
        {
            throw new NotImplementedException();
        }

        public int TotalNodes()
        {
            throw new NotImplementedException();
        }
    }
}
