# Thiết kế — Module Công & KPI NV cửa hàng

Tài liệu ngắn cho bài take-home (MVP 5 trang + API). Excel/Google Sheet tab **1.Nhập DL** là tham chiếu nghiệp vụ: tổng kênh theo ca → từng NV (giờ, DT, chỉ số phụ); hệ thống chuẩn hoá đa cửa hàng.

## 1. ERD (tóm tắt)

| Bảng | Cột / ý nghĩa chính |
| --- | --- |
| `Stores` | Đã có; liên kết `AreaId`. |
| `StoreStaff` | `StoreId`, `StaffCode`, `FullName`, `PositionCode`, `ContractType`, `HourlyRate`, `TeamBonusBase`, `LinkedUserId` (nullable). |
| `StaffDailyEntries` | `StoreStaffId`, `WorkDate`, giờ 4 cột, DT 3 ca, `Customers` / `TryOns` / `Orders` / `Products`, **`Version`** (optimistic concurrency). |
| `StoreDailySummaries` | `StoreId`, `WorkDate`, DT kênh (mock/API), chỉ số cửa hàng, `StoreDayKpiTarget`, `MockApiRevenueTotal`. |
| `StoreMonthlyKpiConfigs` | `YearMonth`, `MonthlyTargetAmount`, JSON tuần/ngày/ca, `IsMonthLocked`. |
| `CommissionBrackets` | `PositionCode`, `ContractType`, `MinKpiPercent`, `MaxKpiPercent`, `CommissionPct`, `EffectiveFrom` / `EffectiveTo`. |

Quan hệ: `Store` 1—n `StoreStaff`; `StoreStaff` 1—n `StaffDailyEntries` (theo ngày); `Store` 1—n `StoreDailySummaries`, `StoreMonthlyKpiConfigs`.

## 2. Permission model

- **JWT**: `role` (ClaimTypes.Role), `store_id`, `area_id` (lặp).
- **CanAccessStore** (`StoreAccess`): AdminHR — mọi store có trong DB; AreaManager — store thuộc `area_id` claim; StoreManager / SalesStaff — `Users.StoreId` trùng `storeId`.
- **KPI tháng (PUT)**: chỉ **AdminHR** (`CanEditKpiMonthConfig`).
- **CRUD nhân sự (staff master)**: AdminHR | AreaManager | StoreManager (chưa tách “không sửa QLCH đồng cấp” trong MVP — có thể bổ sung bằng rule theo `PositionCode` + `LinkedUserId`).
- **Bảng công ngày**: SalesStaff chỉ PATCH row có `LinkedUserId` = user hiện tại; QLCH+ sửa được cả store (theo `CanAccessStore`).

Khoá ngày / khoá tháng / bypass QLCH: có thể mở rộng bằng cờ trên `StoreDailySummaries` / `StoreMonthlyKpiConfigs` + claim `bypass_day_lock` — chưa bắt buộc trong code tối thiểu hiện tại.

## 3. Save-on-blur & race condition

- Mỗi `StaffDailyEntries` có **`Version`** (int). API `PATCH daily-entry` nhận `expectedVersion`.
- Khớp → cập nhật field, `Version++`, `200` + row mới.
- Không khớp → **`409 Conflict`** + payload gợi ý tải lại (`row` đã tính lại KPI).
- FE: blur gửi PATCH kèm version hiện tại; nếu 409 thì merge/reload sheet (tránh mất dữ liệu khi gõ nhanh nhiều ô).

## 4. Công thức (server + test)

Logic thuần trong `ShiftKpiMath` (§5.1 rebalance tuần, bracket, lương tháng). Unit test `Krik.Api.Tests` bao phủ 4 khối công thức.

**Integration test HTTP** (một flow nhập công): có thể thêm `WebApplicationFactory` + DB in-memory + JWT test; trade-off là chỉnh `Program` (bỏ qua migrate khi `Environment == Testing`) và seed tối giản — chưa bật trong repo để tránh phình test harness; ưu tiên đã có unit + FE blur + version.

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

Sidebar nhóm **Công & KPI NV** (cùng nhóm vận hành conceptually với store overview).
