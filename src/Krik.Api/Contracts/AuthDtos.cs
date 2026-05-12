namespace Krik.Api.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresInSeconds,
    UserSummaryDto User);

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    Guid? StoreId,
    IReadOnlyList<Guid> AreaIds);

public sealed record StoreDto(Guid Id, string Code, string Name, Guid AreaId, string AreaCode, string AreaName);

public sealed record UserListItemDto(Guid Id, string Email, string FullName, Guid? StoreId, IReadOnlyList<string> Roles);
