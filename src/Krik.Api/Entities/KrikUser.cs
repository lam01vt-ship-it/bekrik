namespace Krik.Api.Entities;

public class KrikUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid? StoreId { get; set; }
    public Store? Store { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserArea> UserAreas { get; set; } = new List<UserArea>();
}
