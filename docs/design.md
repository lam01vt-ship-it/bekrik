# Thiết kế — Module Công & KPI NV cửa hàng

Tài liệu ngắn cho bài take-home (MVP 5 trang + API). Excel/Google Sheet tab **1.Nhập DL** là tham chiếu nghiệp vụ: tổng kênh theo ca → từng NV (giờ, DT, chỉ số phụ); hệ thống chuẩn hoá đa cửa hàng.

## 1. ERD (tóm tắt)

| Bảng | Cột / ý nghĩa chính |
| --- | --- |
| `Stores` | Đã có; liên kết `AreaId`. |
| `StoreStaff` | `StoreId`, `StaffCode`, `FullName`, `PositionCode`, `ContractType`, `HourlyRate`, `TeamBonusBase`, `LinkedUserId` (nullable). |
| `StaffDailyEntries` | `StoreStaffId`, `WorkDate`, giờ 4 cột, DT 3 ca, `Customers` / `TryOns` / `Orders` / `Products`, **`Version`** (optimistic concurrency). |
| `StoreDailySummaries` | `StoreId`, `WorkDate`, doanh thu kênh theo ca, chỉ số cửa hàng, `StoreDayKpiTarget`, `TongDoanhThuHeThong` (tổng ba ca kênh trong bảng tổng ngày). |
| `StoreMonthlyKpiConfigs` | `YearMonth`, `MonthlyTargetAmount`, JSON tuần/ngày/ca, `IsMonthLocked`. |
| `CommissionBrackets` | `PositionCode`, `ContractType`, `KpiPctMin`, `KpiPctMax`, `CommissionPct`, `EffectiveFrom` / `EffectiveTo`. |

Quan hệ: `Store` 1—n `StoreStaff`; `StoreStaff` 1—n `StaffDailyEntries` (theo ngày); `Store` 1—n `StoreDailySummaries`, `StoreMonthlyKpiConfigs`.

## 2. Permission model

- **JWT**: `role` (ClaimTypes.Role), `store_id`, `area_id` (lặp).
- **CanAccessStore** (`StoreAccess`): AdminHR — mọi store có trong DB; AreaManager — store thuộc `area_id` claim; StoreManager / SalesStaff — `Users.StoreId` trùng `storeId`.
- **KPI tháng (PUT)**: chỉ **AdminHR** (`CanEditKpiMonthConfig`).
- **CRUD nhân sự (staff master)**: AdminHR | AreaManager | StoreManager.
- **Bảng công ngày**: SalesStaff chỉ PATCH row có `LinkedUserId` = user hiện tại; quản lý cửa hàng trở lên sửa được cả cửa hàng (theo `CanAccessStore`).

## 3. Save-on-blur & race condition

- Mỗi `StaffDailyEntries` có **`Version`** (int). API `PATCH daily-entry` nhận `expectedVersion`.
- Khớp → cập nhật field, `Version++`, `200` + row mới.
- Không khớp → **`409 Conflict`** + payload gợi ý tải lại (`row` đã tính lại KPI).

## 4. Công thức (server + test)

Logic thuần trong `ShiftKpiMath`. Unit test trong `Krik.Api.Tests`.

## 5. Trade-offs lớn

1. **PostgreSQL vs InMemory trong test**: Giữ production Npgsql; test công thức tách class thuần để không phụ thuộc DB.
2. **Tỷ trọng KPI phức tạp (tuần / T2–CN / ca weekday-weekend)**: Lưu JSON trên `StoreMonthlyKpiConfigs`; derive `StoreDayKpiTarget` có thể cron hoặc on-read — MVP có thể set target ngày qua seed/tay; rebalance tuần đã có trong math layer.

## 6. FE routes (fekrik)

| Route | Trang |
| --- | --- |
| `/app/staff-shift-kpi/daily` | Bảng công ngày |
| `/app/staff-shift-kpi/monthly` | Tổng quan tháng |
| `/app/staff-shift-kpi/kpi-config` | Cấu hình KPI (Admin) |
| `/app/staff-shift-kpi/staff` | Danh sách NV |
| `/app/staff-shift-kpi/payroll` | Bảng lương + export Excel |

Sidebar nhóm **Công & KPI NV** cùng khu vực menu vận hành với tổng quan và cửa hàng.
