namespace Krik.Api.Entities;

public class Area
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Store> Stores { get; set; } = new List<Store>();
    public ICollection<UserArea> UserAreas { get; set; } = new List<UserArea>();
}
