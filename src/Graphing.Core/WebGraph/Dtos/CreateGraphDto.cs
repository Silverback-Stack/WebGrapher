using System;
using System.ComponentModel.DataAnnotations;

namespace Graphing.Core.WebGraph.Dtos
{
    public class CreateGraphDto
    {
        [Required]
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}
