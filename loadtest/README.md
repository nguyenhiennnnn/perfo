# Load test (k6)

> ⚠️ **Chỉ chạy trên staging hoặc local. TUYỆT ĐỐI không chạy trên production.**
> Test tạo tải lớn, đăng nhập hàng loạt và (ở các kịch bản sau) giữ ghế/đặt vé thật, làm bẩn dữ liệu
> và có thể khóa tài khoản hoặc làm chậm người dùng thật.

## Cài k6

- macOS: `brew install k6`
- Windows: `winget install k6 --source winget`
- Linux / Docker: xem https://grafana.com/docs/k6/latest/set-up/install-k6/

Kiểm tra: `k6 version`.

## Cấu trúc

```
loadtest/
  lib/config.js   BASE_URL, header CSRF, endpoint
  lib/auth.js     login bằng API, getToken() cache theo VU, đọc tài khoản bằng SharedArray
  lib/checks.js   checkOk / checkOkOrConflict, counter conflict_409 và rate_limited_429
  data/users.json tài khoản test: [{ "email", "password", "portal": "customer" | "admin" }]
  scripts/        seed-users.mjs, seed-showtime.mjs (Node >= 18) và api-client.mjs dùng chung
```

## Biến môi trường

Truyền bằng `k6 run -e TÊN=giá_trị script.js`.

| Biến | Mặc định | Ý nghĩa |
|---|---|---|
| `BASE_URL` | `http://localhost:8080` | Gốc server, không kèm `/api/v1` |
| `LOGIN_MAX_ATTEMPTS` | `3` | Số lần thử tối đa khi login bị 429 |
| `DEFAULT_RETRY_AFTER_SECONDS` | `10` | Thời gian chờ nếu 429 không có `Retry-After` |

## Tài khoản test

`data/users.json` mặc định chứa 2 tài khoản khách seed (`customer@`, `customer2@cinema.local`).
Mỗi VU được gán một tài khoản theo vòng; để tránh nhiều VU tranh cùng tài khoản,
hãy thêm tài khoản (đã xác thực email, đang hoạt động) trên staging.
Tài khoản admin/nhân viên dùng `"portal": "admin"`.

## Rate limit

Backend giới hạn theo IP: login 5 lần/60 giây, toàn cục 100 request/60 giây (`RateLimit:*` trong
`appsettings.json`). Mọi VU chạy từ một máy dùng chung một IP nên sẽ bị 429 rất nhanh.
Trên staging, nâng giới hạn bằng biến môi trường backend (ví dụ `RateLimit__AuthPermitLimit`,
`RateLimit__GlobalPermitLimit`) trước khi chạy tải thật. Đừng nâng trên production.

## Seed dữ liệu (chạy một lần trước khi test)

Cấu hình backend theo `.env.loadtest.example` (nâng RateLimit) rồi khởi động lại backend.
Script seed chạy bằng Node, đọc `BASE_URL` giống k6; với host không phải localhost phải thêm `--yes`.

```
# 500 tài khoản khách loadtest.user001..500@cinema.local, ghi đè loadtest/data/users.json
node loadtest/scripts/seed-users.mjs --yes

# 1 suất chiếu trong phòng 320 ghế; in showtimeId (dòng SHOWTIME_ID=...)
node loadtest/scripts/seed-showtime.mjs --yes
```

`seed-users.mjs` cần xác thực email cho tài khoản mới. Mặc định (`--confirm=mock`) script tự bắt token
qua hộp thư giả, nên backend phải trỏ `Email__ResendApiUrl` vào máy chạy script (xem mục C trong
`.env.loadtest.example`). Nếu backend không kết nối được tới máy bạn, dùng `--confirm=sql`: script đăng ký rồi
in câu `UPDATE "Users" ...` để bạn tự chạy trên DB staging. Chạy lại an toàn: tài khoản đã có sẽ được bỏ qua.
