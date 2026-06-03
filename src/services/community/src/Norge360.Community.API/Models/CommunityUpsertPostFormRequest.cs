namespace Norge360.Community.API.Models;

public sealed class CommunityUpsertPostFormRequest
{
    public string? Caption { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public List<IFormFile>? MediaFiles { get; set; }
    public List<Guid>? ExistingMediaIds { get; set; }
    public List<Guid>? RemoveMediaIds { get; set; }
    public List<Guid>? MediaOrder { get; set; }
}
