using System;

namespace Graphing.Core.WebGraph
{
    /// <summary>
    /// Determines how node relationships are updated when mapping page data.
    /// </summary>
    public enum NodeEdgeUpdateStrategy
    {
        /// <summary>
        /// Replaces existing relationships with those in the current page data.
        /// </summary>
        Replace,

        /// <summary>
        /// Preserves existing relationships and adds those in the current page data.
        /// </summary>
        Append
    }
}
