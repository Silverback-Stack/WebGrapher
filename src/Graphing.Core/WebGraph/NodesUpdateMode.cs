using System;

namespace Graphing.Core.WebGraph
{
    /// <summary>
    /// Determines how outgoing node relationships are updated when mapping page data.
    /// </summary>
    public enum OutgoingNodesUpdateMode
    {
        /// <summary>
        /// Replaces existing outgoing relationships with those in the current page data.
        /// </summary>
        Replace,

        /// <summary>
        /// Preserves existing outgoing relationships and adds those in the current page data.
        /// </summary>
        Append
    }
}
