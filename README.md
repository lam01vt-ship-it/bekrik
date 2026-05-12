# bekrik — API .NET 8 (PostgreSQL + JWT + RBAC)

## Cần có

- .NET SDK 8
- PostgreSQL chạy local (mặc định `localhost:5432`)

## Chuẩn bị database

```sql
CREATE DATABASE krik_dev;
```

## Cấu hình kết nối

Sửa `src/Krik.Api/appsettings.json` → `ConnectionStrings:DefaultConnection` (username/password/port/database cho máy bạn).

Ví dụ:

`Host=localhost;Port=5432;Database=krik_dev;Username=postgres;Password=YOUR_PASSWORD`

## Chạy API

Từ thư mục `bekrik`:

```powershell
dotnet tool restore
dotnet run --project src/Krik.Api
```

Nếu **`dotnet build` lỗi MSB3021 / MSB3027** (không copy được `Krik.Api.exe`): một tiến trình API đang chạy đã khoá `bin\Debug\net8.0\`. Dừng tiến trình đó, hoặc build ra thư mục khác, ví dụ:

```powershell
dotnet build src/Krik.Api/Krik.Api.csproj -o _build_out
```

Mặc định profile **http**: Swagger tại `http://localhost:5207/swagger`.

Lần đầu chạy, EF sẽ `Migrate` + seed dữ liệu mẫu (khu BNB, cửa hàng K01 & K02, 4 user).

## Tài khoản seed (mật khẩu: `Admin123!`)

| Email | Role | Ghi chú |
| --- | --- | --- |
| admin@krik.local | AdminHR | Toàn chuỗi + `/api/users`, `/api/admin/ping` |
| area@krik.local | AreaManager | Cửa hàng thuộc khu BNB |
| store@krik.local | StoreManager | Chỉ K01 |
| sales@krik.local | SalesStaff | Chỉ K01 |

## Migration

Tool `dotnet-ef` nằm trong manifest local (`.config/dotnet-tools.json`).

```powershell
dotnet tool run dotnet-ef migrations add TenMigration --project src/Krik.Api --output-dir Data/Migrations
```

## RBAC (tóm tắt)

- JWT chứa `role` (ClaimTypes.Role), `store_id`, `area_id` (lặp theo từng khu).
- `StoresController` lọc theo role.
- `UsersController` + `AdminController` chỉ **AdminHR**.

FE mẫu nằm ở `../fekrik` (Vite proxy `/api` → cổng 5207).

## Module take-home: Công & KPI NV (`shift-kpi`)

- **API**: `GET/PATCH .../api/stores/{storeId}/shift-kpi/...` — xem Swagger (`daily`, `daily-entry`, `kpi-months`, `monthly-dashboard`, `payroll`, `payroll-export`, `staff`).
- **Seed demo**: cửa hàng **K01**, 8 NV, KPI tháng 2026-05, ~1 tuần `StaffDailyEntry` + tổng ngày (`StaffShiftKpiDemoSeed`). User `sales@krik.local` gắn 1 NV bán hàng để thử quyền Sales (chỉ thấy row mình).
- **Thiết kế ngắn**: [docs/design.md](docs/design.md) (ERD tóm tắt, permission, save-on-blur/version, trade-offs).
- **Test**: `dotnet test tests/Krik.Api.Tests` — unit cho `ShiftKpiMath` (4 công thức mục 5 đề bài).
- **FE**: sau khi chạy API, trong `fekrik` dùng menu **Công & KPI NV** — 5 route MVP (`daily`, `monthly`, `kpi-config`, `staff`, `payroll`).
