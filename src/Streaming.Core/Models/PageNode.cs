using System;
using Streaming.Core.Dtos;

namespace Streaming.Core.Models
{
    public record PageNode
    {
        public required Uri Url { get; init; }
        public string Title { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public List<PageNode> Links { get; init; } = new();

        public override string ToString()
        {
            return $"{Url} - {Title}";
        }

        public static PageNodeDto ToDto(PageNode node)
        {
            return new PageNodeDto
            {
                Url = node.Url.AbsoluteUri,
                Title = node.Title,
                CreatedAt = node.CreatedAt,
                Links = node.Links.Select(link => ToDto(link)).ToList()
            };
        }
    }
}
