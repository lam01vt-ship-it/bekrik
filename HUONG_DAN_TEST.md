# Hướng dẫn thử nghiệm phần mềm

Tài liệu này mô tả cách chạy ứng dụng, **tài khoản** dùng thử và **dữ liệu trong bảng** sau khi API (`bekrik`) khởi động lần đầu (migrate + seed).

## 1. Chuẩn bị

1. PostgreSQL và chuỗi kết nối trong `src/Krik.Api/appsettings.json`.
2. Chạy API: từ thư mục `bekrik` — `dotnet run --project src/Krik.Api` (Swagger thường ở `http://localhost:5207/swagger`).
3. Chạy giao diện: từ thư mục `fekrik` — `npm install` rồi `npm run dev` (mặc định `http://localhost:5173`, proxy `/api` tới API).

Sau lần chạy API đầu tiên, cơ sở dữ liệu được tạo bảng và gọi seed (khu BNB, cửa hàng K01 & K02, user mẫu, module công & KPI cho K01). Khi có migration mới, khởi động lại API để `Migrate()` áp dụng.

## 2. Mật khẩu chung (môi trường dev)

Tất cả tài khoản mẫu dưới đây dùng cùng mật khẩu:

**`Admin123!`**

## 3. Bảng tài khoản và phạm vi thử

| Email | Vai trò (trong JWT) | Phạm vi | Việc nên thử trên giao diện |
| --- | --- | --- | --- |
| `admin@krik.local` | AdminHR | Toàn chuỗi | **Cửa hàng** (thêm / xoá nhiều / xuất Excel); **Cấu hình KPI**; **Quản trị người dùng**; **Bảng lương**; **Tổng quan tháng**; **Danh sách nhân viên**; **Bảng công ngày**. |
| `area@krik.local` | AreaManager | Khu BNB (K01, K02) | **Cửa hàng** trong khu (thêm / xoá / Excel); công & KPI trên K01/K02; **Danh sách nhân viên**; không có menu **Cấu hình KPI** trên FE (chỉ Admin). |
| `store@krik.local` | StoreManager | Chỉ K01 | **Bảng công ngày** chỉnh sửa toàn bộ nhân viên K01; **Danh sách nhân viên** (thêm NV kèm tài khoản); **Bảng lương**; **Cửa hàng** chỉ xem / xuất Excel (không thêm xoá). |
| `sales@krik.local` | SalesStaff | Chỉ K01, chỉ bản thân | **Bảng công ngày**: chỉ thấy dòng NV gắn với tài khoản (mã **T1204** trong bảng `StoreStaff`, cột `LinkedUserId` trùng id user). **Bảng lương**: chỉ dòng của mình. |

## 4. Dữ liệu bảng liên quan module Công & KPI (K01)

Các bảng sau được ghi khi seed module (nếu chưa có nhân viên K01):

- **`StoreStaff`**: 8 nhân sự cửa hàng K01 (mã T103 …), chức danh và hợp đồng theo seed.
- **`StoreMonthlyKpiConfigs`**: một dòng cho tháng **2026-05**, `MonthlyTargetAmount` = 1.500.000.000, các trường JSON tỷ trọng theo seed.
- **`StoreDailySummaries`**: 7 ngày từ **2026-05-05** đến **2026-05-11**; cột **`TongDoanhThuHeThong`** bằng **tổng** `ChannelRevenueMorning` + `ChannelRevenueAfternoon` + `ChannelRevenueEvening` cùng dòng (đồng bộ với bảng tổng ngày).
- **`StaffDailyEntries`**: mỗi ngày trên có một dòng cho mỗi nhân viên (giờ ca, doanh thu ca, chỉ số phụ).
- **`CommissionBrackets`**: các bậc hoa hồng theo vị trí / hợp đồng.

Khi **thêm nhân viên** qua giao diện **Danh sách nhân viên**, bắt buộc nhập **email và mật khẩu** — API tạo bản ghi trong bảng **Users** (mật khẩu đã băm), gán **StoreStaff.LinkedUserId**, gán cửa hàng và vai trò JWT: chức danh **QLCH** hoặc **CHP** → `StoreManager`, còn lại → `SalesStaff`.

Trên **Bảng công theo ngày**, chọn cửa hàng **K01** và một ngày trong khoảng trên để thấy dữ liệu đã có. Trên **Tổng quan tháng**, nhập **`2026-05`**.

## 5. Kịch bản gợi ý (ngắn)

1. Đăng nhập **admin** → mở **Cấu hình KPI** → chọn K01, tháng 2026-05 → tải cấu hình (có dữ liệu seed).
2. Đăng nhập **store** → **Bảng công ngày** → sửa một ô giờ hoặc doanh thu → rời ô (blur) → kiểm tra giá trị giữ sau tải lại trang.
3. Đăng nhập **sales** → cùng trang → chỉ còn một dòng (NV T1204) → thử sửa và lưu blur.
4. **store** hoặc **admin** → **Bảng lương** tháng 2026-05 → **Xuất Excel** → mở file kiểm tra cột.

Nếu build API báo không ghi được DLL/EXE: dừng tiến trình API đang chạy hoặc build ra thư mục khác (mô tả trong `README.md` của `bekrik`).