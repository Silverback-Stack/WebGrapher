namespace Events.Core.Dtos
{
    public class GraphCreationOptionsDto
    {
        public required string UserId { get; set; }
        public required string Name { get; set; }
        public string Description {  get; set; } = string.Empty;
    }
}