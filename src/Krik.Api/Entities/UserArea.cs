namespace Krik.Api.Entities;

public class UserArea
{
    public Guid UserId { get; set; }
    public KrikUser User { get; set; } = null!;
    public Guid AreaId { get; set; }
    public Area Area { get; set; } = null!;
}
