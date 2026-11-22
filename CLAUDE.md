# Perfo Cinema

Hệ thống bán vé và vận hành rạp chiếu phim: một rạp, nhiều phòng chiếu, hai kênh bán (online, quầy). Backend .NET, frontend React. Đang nâng cấp từ demo lên hệ thống thực tế theo `docs/NANG_CAP_BUSINESS_PERFO.md`; danh sách task và quy tắc làm task nằm ở `docs/PROMPT_TRIEN_KHAI_NANG_CAP_PERFO.md`.

## Cách làm việc

- Chỉ làm đúng phạm vi task được giao. Không làm trước phần của task sau.
- Không build, không chạy lệnh, không viết comment trong code, xóa code thừa.
- Không viết, sửa, xóa test (`backend/tests`, `e2e`, `loadtest`), kể cả khi test cũ không còn khớp. Chỉ nêu test có thể bị ảnh hưởng.
- Nếu prompt lệch tài liệu nghiệp vụ thì theo tài liệu và nêu chỗ lệch.
- Tìm bằng Grep/Glob rồi đọc đúng file cần, không đọc cả thư mục. Trước khi đổi tên hoặc xóa một key, type, hook, hãy grep toàn project xem ai đang dùng.
- Đầu ra khi giao qua zip: chỉ file mới hoặc thay đổi, đúng cấu trúc thư mục; liệt kê file cần xóa riêng. Cuối câu trả lời nêu việc thủ công, chỗ lệch tài liệu, test có thể bị ảnh hưởng.
- Không đổi route, DTO, hành vi hiện có nếu task không yêu cầu.

## Lệnh (chạy từ thư mục gốc)

- `npm run dev`: chạy backend và frontend; `npm run typecheck`, `npm run lint`, `npm run build`.
- `npm run format` / `format:check`: CSharpier cho C#.
- `npm run db:migration:add -- Ten`, `db:update`: EF Core migration. Không tự tạo file migration trong task.
- `npm run test:backend`, `npm run test:e2e`: chỉ chạy khi người dùng yêu cầu.

## Cấu trúc

```
backend/
  Cinema.Api            Controllers (Admin/, Client/), Filters, Middlewares, Program.cs
  Cinema.Application    Services/<miền>, DTOs, Interfaces, Events, BackgroundJobs, Caching, Validators, Utils
  Cinema.Domain         Entities, Enums
  Cinema.Infrastructure Persistence (EF), Migrations, Payments, Email, Pdf, Export, Import, Realtime, Ai
  Cinema.Shared         Constants, Exceptions, Resources/Messages.resx
  tests                 UnitTests, IntegrationTests
frontend/src
  api/                  axios.ts, endpoints.ts, query-keys.ts, query-invalidation.ts, query-client.ts
  features/admin/*      Mỗi module: api, hooks, types, components, pages, utils
  features/client/*     Phía khách (orders, bookings, showtimes, account...)
  components/ui         Shadcn/Base UI; components/common: dùng chung
  config/               routes.ts, admin-nav.ts, default-constants.ts, http-status.ts
  hooks/, stores/, lib/ use-query-params, use-table-pagination, formatters, enum-options, error
e2e/                    Playwright
docs/                   Tài liệu nghiệp vụ và bộ prompt triển khai
```

## Quy ước backend

- Truy cập dữ liệu qua `IAppDbContext`. Service tách theo trách nhiệm nhỏ; logic dùng chung đặt ở lớp static hoặc service riêng.
- Chuỗi người dùng nhìn thấy (lỗi, email, thông báo) lưu trong `Messages.resx` và cập nhật `Messages.Designer.cs` viết tay. Không hard-code tiếng Việt.
- Thời gian: `VietnamTime`, `BusinessTime`, `Brand:TimeZoneId`; cột `timestamp without time zone`. "Theo ngày" là ngày kinh doanh.
- Tiền: `decimal`, làm tròn đến đồng. Không dùng số thực.
- Đồng thời (giữ ghế, trừ tồn, trừ số dư, giữ lượt dùng mã): thao tác nguyên tử bằng UPDATE có điều kiện hoặc transaction kèm concurrency token. Xử lý nền và xử lý sự kiện phải an toàn khi chạy lặp.
- Phân quyền: endpoint admin chỉ quản trị (`Policies.AdminOnly`); endpoint quầy cho nhân viên và quản trị (`Policies.StaffOnly`).
- Thao tác nhạy cảm ghi audit log. Dữ liệu cấu hình đọc nhiều dùng bộ nhớ đệm và tự làm mới khi quản trị chỉnh sửa.
- Mô hình đơn hàng: `Order` (dòng hàng, khoản điều chỉnh giá, sổ giao dịch thanh toán). Sự kiện nghiệp vụ ở `Cinema.Application/Events`.
- Lưu ý đặt tên: controller admin đơn hàng vẫn có route `/admin/bookings` (`AdminBookingsController`, `AdminCounterController`), module xuất file tên `bookings`. Frontend dùng khái niệm "Đơn hàng" (orders).

## Quy ước frontend

- React 19, TypeScript, Tailwind v4, TanStack Query/Table, nuqs, react-hook-form + zod, zustand. Tuân theo skill `cinema-design-system`: semantic token, `cn`, component ở `@/components/ui`, không hard-code màu.
- Giao diện tiếng Việt. Thuật ngữ: Đơn hàng, Suất chiếu, Vé, Combo. Responsive (khách và quầy dùng được trên điện thoại), có skeleton và empty state.
- Tiền và ngày dùng `formatCurrency`, `formatDateTime`, `formatDate` ở `@/lib/formatters`. Tiền là số nguyên đồng.
- Danh sách admin: `PageHeader` + `DataTable` (`@/components/common/data-table/*`) + `useTablePagination` + `use-query-params` (nuqs, đồng bộ lên URL) + `ExportButton` khi có xuất file. Cột khai báo bằng `createColumnHelper`.
- Chi tiết: `QueryState` (skeleton, lỗi, not found), `NotFoundState`, `EmptyState`, `CardSkeleton`/`LineSkeleton`, `StatusBadge` (`BookingStatusBadge`, `BookingChannelBadge`, `PaymentStatusBadge`).
- Hộp thoại và panel: `useDialogStore`, `useAlertDialogStore` (có `content`, `confirmDisabled`), `useSheetStore`.
- Lỗi API: `extractApiError`, `getDisplayMessage` ở `@/lib/error`. Mutation meta: `invalidates: "bookingChanged"` (làm mới đơn, thanh toán), `skipErrorToast`, `skipSuccessToast`, `errorTitle`.
- Query key tập trung ở `api/query-keys.ts` (đơn admin: `adminOrders`, đơn khách: `orders`). Đổi key phải cập nhật `query-invalidation.ts` và mọi nơi dùng.
- Endpoint tập trung ở `api/endpoints.ts`; route ở `config/routes.ts`; menu admin ở `config/admin-nav.ts`; route admin ở `app/routes/admin-routes.tsx`.
- Phần dùng chung giữa hai module thì trích ra một nơi hợp lý (ví dụ phần hủy đơn dùng chung nằm ở `features/admin/orders`), không sao chép.

## Module đơn hàng phía admin (để tra nhanh)

- `features/admin/orders`: danh sách (`orders-page`), chi tiết (`order-detail-page`), hủy đơn (`use-order-cancel-flow`, `OrderCancelResult`, `OrderCancelReasonPicker`), nhãn và hàm trợ giúp ở `utils/order-cancel.ts`, `utils/order-detail.ts`.
- `features/admin/counter`: bán vé, bán combo, hủy đơn tại quầy; dùng lại phần hủy đơn của module orders.
- `features/client/orders`: đơn phía khách; nhãn dùng chung ở `utils/order-labels.ts`; types gốc của `OrderDetailDto`, `OrderListItemDto`.
- Backend: `Cinema.Application/Services/Orders` (query, hold, checkout, cancel, refund, export, ticket PDF).
