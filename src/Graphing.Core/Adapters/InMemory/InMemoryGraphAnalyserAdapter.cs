using System;
using System.Collections.Concurrent;
using Graphing.Core.Models;

namespace Graphing.Core.Adapters.InMemory
{
    public class InMemoryGraphAnalyserAdapter : IGraphAnalyser
    {
        protected readonly ConcurrentDictionary<string, Node> _nodes;

        public InMemoryGraphAnalyserAdapter(ConcurrentDictionary<string, Node> data)
        {
            _nodes = data;
        }

        public int TotalNodes()
        {
            return _nodes.Values.Count(n => n.HasData);
        }

        public int TotalEdges()
        {
            return _nodes.Values.Sum(n => n.Edges.Count);

        }
        public int AverageLinksPerNode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Traverse the graph breadth-first or depth-first to discover pages reachable from a given node.
        /// </summary>
        /// <param name="fromUrl"></param>
        /// <param name="maxDepth"></param>
        /// <returns></returns>
        public void FindReachablePages(string fromUrl, int maxDepth)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Use BFS or Dijkstra’s to find the minimal link chain between two nodes (e.g. Google → Buy.com → Target.com).
        /// </summary>
        /// <param name="fromUrl"></param>
        /// <param name="toUrl"></param>
        public void GetShortestPath(string fromUrl, string toUrl)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return pages with the highest incoming link count — to identify popular or central hubs.
        /// </summary>
        /// <param name="topN"></param>
        public void GetMostLinkedPages(int topN)
        {

        }

        /// <summary>
        /// Find nodes with no outgoing links.
        /// </summary>
        public void GetDeadEnds()
        {

        }

        /// <summary>
        /// Return nodes with no links in or out — might be loading failures or irrelevant content.
        /// </summary>
        public void GetIsolatedNodes()
        {

        }

        /// <summary>
        /// Filter nodes by metadata (e.g. page title and content).
        /// </summary>
        /// <param name="keyword"></param>
        public IEnumerable<Node> SearchPagesByKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<Node>();

            string lowerKeyword = keyword.ToLowerInvariant();

            return _nodes.Values
                .Where(page =>
                    !string.IsNullOrEmpty(page.Title) && page.Title.ToLowerInvariant().Contains(keyword) ||
                    !string.IsNullOrEmpty(page.Keywords) && page.Keywords.ToLowerInvariant().Contains(keyword))
                .ToList();
        }

        /// <summary>
        /// Return all nodes from a specific domain (e.g. all .gov pages or all under apple.com).
        /// </summary>
        /// <param name="domain"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void FindPagesByDomain(string domain)
        {
            throw new NotImplementedException();
        }
    }
}
