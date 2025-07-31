using System;
using Streaming.Core.Dtos;

namespace Streaming.Core.Models
{
    public record PageEdge
    {
        public Uri From { get; set; }
        public Uri To { get; set; }
        public string AnchorText { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public double Weight { get; set; }

        public PageEdge(Uri from, Uri to, string anchorText = "", double weight = 1.0)
        {
            From = from;
            To = to;
            AnchorText = anchorText;
            CreatedAt = DateTimeOffset.UtcNow;
            Weight = weight;
        }

        public override string ToString()
        {
            return $"{From} → {To} (Weight: {Weight})";
        }

        public static PageEdgeDto ToDto(PageEdge edge)
        {
            return new PageEdgeDto
            {
                From = edge.From.AbsoluteUri,
                To = edge.To.AbsoluteUri,
                AnchorText = edge.AnchorText,
                CreatedAt= edge.CreatedAt,
                Weight = edge.Weight
            };
        }
    }
}
