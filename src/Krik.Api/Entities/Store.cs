namespace Krik.Api.Entities;

public class Store
{
    public Guid Id { get; set; }
    public Guid AreaId { get; set; }
    public Area Area { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
