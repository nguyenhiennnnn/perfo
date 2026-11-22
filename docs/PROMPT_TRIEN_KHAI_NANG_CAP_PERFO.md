# Bộ prompt triển khai nâng cấp business Perfo Cinema

Nguồn nghiệp vụ: `NANG_CAP_BUSINESS_PERFO.md` (23 hạng mục). Mỗi hạng mục được chia thành các task vừa, mỗi task có một prompt độc lập để giao cho AI code.

## Cách dùng

1. Đính kèm `NANG_CAP_BUSINESS_PERFO.md` và mã nguồn cho AI.
2. Dán khối **Quy tắc chung** (mục A) trước, rồi dán prompt của task cần làm.
3. Làm tuần tự theo thứ tự trong tài liệu. Task chỉ phụ thuộc task có số nhỏ hơn (cột "Phụ thuộc" ghi rõ).
4. Sau mỗi task, tự kiểm tra thủ công theo dòng "Tiêu chí hoàn thành" của task đó.
5. Từ T1.1 đến T1.8 hệ thống có thể tạm chưa chạy trọn vẹn vì đang thay mô hình đơn hàng; chạy kiểm tra thủ công sau T1.10.

---

## A. Quy tắc chung (dán trước mọi prompt)

```text
QUY TẮC CHUNG CHO MỌI TASK

- Nguồn nghiệp vụ là NANG_CAP_BUSINESS_PERFO.md. Đọc phần 3 (quy ước chung) và hạng mục của task trước khi code. Nếu prompt lệch tài liệu thì theo tài liệu và nêu chỗ lệch ở cuối câu trả lời.
- Chỉ làm đúng phạm vi task. Không làm trước phần của task sau; chỉ chừa điểm mở rộng tối thiểu khi prompt nêu rõ.
- Không build, không chạy lệnh, không viết comment trong code, xóa code thừa.
- Không làm và không sửa phần testing: không viết test mới (backend lẫn frontend, gồm unit, integration, e2e, loadtest), không sửa, không xóa project hay file test hiện có (thư mục tests, e2e, loadtest và các file test khác), kể cả khi test cũ không còn khớp với code mới. Chỉ nêu ở cuối câu trả lời những test có thể bị ảnh hưởng.
- Bám convention hiện có: Cinema.Api / Cinema.Application / Cinema.Infrastructure / Cinema.Shared; truy cập dữ liệu qua IAppDbContext; service tách theo trách nhiệm nhỏ, logic dùng chung đặt ở lớp static hoặc service riêng; không đổi route, DTO, hành vi hiện có nếu task không yêu cầu.
- Mọi chuỗi người dùng nhìn thấy (lỗi, email, thông báo) lưu trong Messages.resx và cập nhật Messages.Designer.cs viết tay; không hard-code tiếng Việt trong code.
- Thời gian: dùng VietnamTime và Brand:TimeZoneId, cột timestamp without time zone. "Theo ngày" luôn là ngày kinh doanh theo mục 3.1.
- Tiền: decimal, làm tròn đến đồng, hiển thị VND; không dùng số thực.
- Đồng thời: giữ ghế, giữ chỗ tài nguyên, trừ tồn, trừ số dư, giữ lượt là thao tác nguyên tử (UPDATE có điều kiện hoặc transaction kèm concurrency token). Xử lý nền và xử lý sự kiện phải an toàn khi chạy lặp.
- Thao tác nhạy cảm ghi nhật ký theo mục 3.8 qua hạ tầng của T1.4. Dữ liệu cấu hình đọc nhiều dùng bộ nhớ đệm theo mục 3.9 và tự làm mới khi quản trị chỉnh sửa.
- Phân quyền theo bảng 3.11: endpoint admin chỉ quản trị; endpoint quầy cho nhân viên và quản trị.
- Tham số cấu hình mới của task thêm vào cài đặt rạp đúng theo Phụ lục của tài liệu.
- Frontend: React 19 + TypeScript, theo skill cinema-design-system (semantic token, cn, component ở @/components/ui, PageHeader, data-table, useDialogStore / useAlertDialogStore / useSheetStore, TanStack Query, formatter tiền và ngày ở @/lib/formatters). Giao diện tiếng Việt, responsive (màn hình khách và quầy dùng được trên điện thoại), có skeleton và empty state, không hard-code màu.
- Thay đổi schema: cập nhật entity và cấu hình EF; không chạy lệnh. Không chuyển đổi dữ liệu cũ.
- Đầu ra: chỉ xuất các file mới hoặc thay đổi .zip theo cấu trúc thư mục project. Cuối câu trả lời liệt kê ngắn việc thủ công (package, lệnh migration, appsettings, seed), có thể điều chỉnh project gốc nếu thấy vướng khi thực hiện, không cần tạo các file migration hay các file có thể tự generate.
```

---

## B. Mục lục task

| Hạng mục | Tên | Task | Số task |
|---|---|---|---|
| 1 | Nền tảng đơn hàng, sổ thanh toán, sự kiện, nhật ký | T1.1 – T1.11 | 11 |
| 2 | Loại vé và quy tắc giá | T2.1 – T2.5 | 5 |
| 3 | Khuyến mãi | T3.1 – T3.5 | 5 |
| 4 | Kho bắp nước và bán combo tại quầy | T4.1 – T4.5 | 5 |
| 5 | Đóng phòng, khóa ghế, xử lý suất bị ảnh hưởng | T5.1 – T5.4 | 4 |
| 6 | Báo cáo, phân tích lấp đầy, tài chính, đối soát cổng | T6.1 – T6.7 | 7 |
| 7 | Nhà phát hành và hợp đồng phim | T7.1 – T7.3 | 3 |
| 8 | Chốt doanh thu cuối ngày và chốt ca quầy | T8.1 – T8.3 | 3 |
| 9 | Hóa đơn VAT theo yêu cầu | T9.1 – T9.2 | 2 |
| 10 | Thẻ quà tặng | T10.1 – T10.5 | 5 |
| 11 | Đổi suất chiếu | T11.1 – T11.3 | 3 |
| 12 | Combo đặt trước và nhắc lịch chiếu | T12.1 – T12.3 | 3 |
| 13 | Loyalty mạnh hơn | T13.1 – T13.6 | 6 |
| 14 | Hỗ trợ khách hàng và hoàn tiền ngoại lệ | T14.1 – T14.3 | 3 |
| 15 | Gợi ý phim cá nhân hóa | T15.1 – T15.3 | 3 |
| 16 | Đánh giá xác thực và khảo sát | T16.1 – T16.2 | 2 |
| 17 | Danh sách chờ và báo khi mở bán | T17.1 – T17.3 | 3 |
| 18 | Chống giữ ghế ảo và hạn chế lạm dụng | T18.1 – T18.2 | 2 |
| 19 | Thuê phòng và bán sỉ | T19.1 – T19.5 | 5 |
| 20 | Gói sản phẩm | T20.1 – T20.3 | 3 |
| 21 | Gói thành viên trả phí | T21.1 – T21.5 | 5 |
| 22 | Phân khúc khách hàng và chiến dịch | T22.1 – T22.4 | 4 |
| 23 | Suất chiếu sự kiện | T23.1 – T23.3 | 3 |
| | **Tổng** | | **95** |

---

## C. Prompt theo task

---

# HẠNG MỤC 1. Nền tảng đơn hàng, sổ thanh toán, sự kiện và nhật ký

---

### T1.1 — Mô hình dữ liệu đơn hàng

**Phụ thuộc:** không.

```text
Áp dụng QUY TẮC CHUNG. Đọc mục 2, 3.1 đến 3.5 và H1.1.

Thay Booking bằng Order ở tầng domain và persistence:
- Enum: OrderType (vé, combo riêng, thẻ quà tặng, gói thành viên, thuê phòng, đơn bổ sung), SalesChannel (online, quầy), OrderStatus (chờ thanh toán, đã xác nhận, đã hủy, hết hạn), OrderLineType (vé, combo, thẻ quà tặng, phí), CancelReason (khách đổi ý, trùng đơn, lỗi thanh toán, rạp hủy suất, đóng phòng, khác), CancelledBy (khách, nhân viên, quản trị, hệ thống).
- Entity Order: mã đơn duy nhất, loại, kênh, khách có tài khoản hoặc khách vãng lai (tên, số điện thoại), trạng thái, thời điểm tạo, hạn thanh toán, nhân viên tạo, ca quầy (nullable, task sau dùng), đơn gốc (nullable), tổng gốc, tổng giảm, số phải trả, lý do và người hủy, concurrency token. Suất chiếu không bắt buộc trên đơn.
- Entity OrderLine: loại dòng; ghế, suất, loại vé (nullable với dòng không phải vé); combo, số lượng, số đã nhận, giá vốn tại thời điểm bán; đơn giá niêm yết, giá đã khóa, giá thực trả của dòng; mã QR; trạng thái check-in.
- Entity OrderPriceAdjustment: loại (ưu đãi hạng, quyền lợi gói thành viên, mã khuyến mãi, giá gói), số tiền, tham chiếu (loại và id đối tượng), phần phân bổ về từng dòng hàng.
- Cấu hình EF, index, ràng buộc (tiền không âm; đơn phải có tài khoản hoặc thông tin vãng lai).
- Cập nhật mọi entity và ràng buộc đang trỏ tới Booking (vé, check-in, đánh giá, dòng điểm loyalty...) sang Order/OrderLine ở mức mô hình dữ liệu. Chưa sửa service.

Tiêu chí hoàn thành: mô hình biểu diễn được đơn vé, đơn chỉ combo, đơn thẻ quà, đơn bổ sung; không còn tham chiếu Booking trong domain và cấu hình EF.
```

---

### T1.2 — Sổ giao dịch thanh toán và cổng thanh toán

**Phụ thuộc:** T1.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.1, 3.6 và H1.2.

Thay Payment bằng PaymentTransaction:
- Entity: đơn, loại (thu, hoàn), phương thức (VNPay, MoMo, Stripe, tiền mặt, thẻ quà tặng, chuyển khoản), số tiền, phí cổng, mã giao dịch hệ thống (duy nhất), mã giao dịch cổng (duy nhất theo phương thức, nullable), trạng thái, thời điểm, người thực hiện, với tiền mặt có số tiền nhận và tiền thối, với giao dịch hoàn có trỏ về giao dịch thu gốc.
- Service tính "số tiền khách thực trả" (tổng thu thành công, gồm thẻ quà, trừ đã hoàn), số đã hoàn, và hàm "đã đủ tiền" cho một đơn. Hoàn không được vượt số đã thu của giao dịch gốc; hỗ trợ hoàn từng phần nhiều lần.
- Điều chỉnh các handler thông báo cổng VNPay, MoMo, Stripe: giữ nguyên kiểm tra chữ ký, số tiền, chống xử lý trùng; ghi giao dịch thu vào sổ. Thu thừa (khách trả hai lần) tự tạo giao dịch hoàn qua cổng tương ứng; nếu không tự hoàn được thì đánh dấu để task hỗ trợ xử lý sau.
- Hoàn qua cổng: tái dùng client hiện có, ghi thành giao dịch hoàn riêng.
- Chưa xác nhận đơn ở task này (làm ở T1.6).

Tiêu chí hoàn thành: một đơn ghi được nhiều giao dịch thu và nhiều giao dịch hoàn; hoàn vượt số đã thu bị chặn; thông báo cổng gửi lặp không tạo giao dịch trùng.
```

---

### T1.3 — Hạ tầng sự kiện nghiệp vụ

**Phụ thuộc:** T1.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.7 và H1.5.

Xây hạ tầng sự kiện nghiệp vụ:
- Sự kiện: OrderConfirmed, OrderCancelled, OrderRefunded, ShowtimeEnded, TicketCheckedIn.
- Dispatcher và interface handler, đăng ký qua DI. Hai nhóm handler: chạy trong cùng giao dịch (đồng bộ, lỗi thì rollback toàn bộ) và chạy nền.
- Nhóm nền dùng outbox lưu trong DB, xử lý bởi cơ chế tác vụ nền hiện có, có thử lại; đánh dấu đã xử lý theo cặp (sự kiện, handler) để chạy lặp không xử lý hai lần.
- Chèn điểm phát sự kiện tại các chỗ xác nhận đơn, hủy, hoàn, check-in, suất kết thúc; chưa di chuyển logic cũ (làm ở T1.6, T1.7). Chưa viết handler nghiệp vụ nào.

Tiêu chí hoàn thành: một handler thử nghiệm đăng ký được ở cả hai nhóm; handler nền lỗi được thử lại; chạy lặp không xử lý trùng.
```

---

### T1.4 — Nhật ký thao tác và bộ nhớ đệm

**Phụ thuộc:** T1.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.8, 3.9, 3.11 và H1.5.

Backend:
- AuditLog chỉ thêm: người thực hiện, thời điểm, loại thao tác, đối tượng (loại và id), giá trị trước và sau (JSON). Chặn sửa và xóa ở EF (SaveChanges interceptor) và bằng trigger trong migration.
- IAuditLogger để các task sau gọi. API admin tra cứu nhật ký có lọc theo đối tượng, người thực hiện, khoảng thời gian, phân trang.
- Bộ nhớ đệm: abstraction dùng IMemoryCache với khóa theo nhóm, hàm làm mới theo khóa, helper để các task sau đăng ký dữ liệu cache (quy tắc giá, khuyến mãi, hạng thành viên, cấu hình rạp, loại vé, lịch ngày lễ). Áp dụng ngay cho cấu hình rạp và bảng hạng thành viên hiện có, tự làm mới khi quản trị chỉnh.

Frontend: trang admin xem nhật ký (PageHeader, data-table, bộ lọc, xem chi tiết giá trị trước/sau).

Tiêu chí hoàn thành: không sửa hoặc xóa được dòng nhật ký bằng bất kỳ cách nào; đổi cấu hình rạp thì cache làm mới ngay và có dòng nhật ký.
```

---

### T1.5 — Hồ sơ khách: tùy chọn nhận thông tin và mã giới thiệu

**Phụ thuộc:** không (làm sau T1.1).

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.10 và phần mã giới thiệu ở H13.5.

- Thêm vào hồ sơ khách ba lựa chọn tách biệt: nhận email và thông báo tiếp thị (mặc định tắt), gợi ý phim cá nhân hóa (mặc định bật), nhận nhắc lịch và mời đánh giá (mặc định bật).
- Mã giới thiệu riêng, duy nhất, sinh khi tạo tài khoản (tài khoản cũ được sinh bổ sung trong seed).
- API đọc và cập nhật ba lựa chọn. Ô bật nhận tiếp thị ở form đăng ký (không chọn sẵn) và ở trang cá nhân.
- Liên kết hủy đăng ký dùng token ký, endpoint công khai không cần đăng nhập.
- Helper dùng chung "được gửi email loại X cho khách này hay không" (email dịch vụ bắt buộc luôn gửi).
- Chỉ làm trường dữ liệu, API và giao diện lựa chọn. Chưa làm logic thưởng giới thiệu.

Tiêu chí hoàn thành: khách mới có mặc định đúng; tắt từng lựa chọn lưu được; link hủy đăng ký tắt tiếp thị mà không cần đăng nhập.
```

---

### T1.6 — Luồng giữ ghế, khóa giá, giữ chỗ tài nguyên, xác nhận và hết hạn

**Phụ thuộc:** T1.1, T1.2, T1.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.1, 3.5, 3.7 và H1.3.

Chuyển luồng đặt vé sang Order:
- Giữ ghế nguyên tử như hiện có, tạo đơn chờ thanh toán với dòng vé và dòng combo, khóa giá hiện hành vào dòng (giá lấy từ giá ghế lưu sẵn như cũ; bộ tính giá ở T2.2 sẽ thay), hạn thanh toán lấy từ cấu hình giữ ghế.
- Cơ chế giữ chỗ tài nguyên tổng quát: interface IResourceHold với Reserve, Commit, Release, đăng ký qua DI; mỗi đơn gắn các phần giữ chỗ của mình. Hết hạn hoặc hủy khi chưa thanh toán gọi Release cho tất cả cùng lúc với nhả ghế; xác nhận gọi Commit. Chưa có tài nguyên cụ thể (task sau thêm).
- Xác nhận đơn khi hàm "đã đủ tiền" (T1.2) đúng. Trong cùng giao dịch: đổi trạng thái, phát vé và QR, ghi điểm loyalty (giữ logic hiện có, tính trên số khách thực trả của vé và combo), Commit các giữ chỗ, phát OrderConfirmed. Email gửi qua handler nền.
- Tác vụ nền hết hạn chuyển sang xử lý đơn. Mọi bước idempotent.
- Cập nhật ghế thời gian thực giữ nguyên hành vi.

Tiêu chí hoàn thành: đơn trả bằng hai phương thức xác nhận đúng; hết hạn thì ghế và giữ chỗ nhả cùng nhau; xác nhận hai lần không phát vé hai lần.
```

---

### T1.7 — Hủy, hoàn tiền và hủy suất

**Phụ thuộc:** T1.2, T1.3, T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.6, 3.3 và H1.4.

- Hủy đơn (khách tự hủy online, nhân viên, quản trị): giữ phần trăm hoàn và hạn hủy theo cấu hình rạp; khách tự hủy chỉ áp dụng đơn có thanh toán qua cổng hoặc thẻ quà, đơn tiền mặt hủy tại quầy. Bắt buộc ghi lý do và người hủy.
- Tính hoàn trên số tiền khách thực trả nhân phần trăm hoàn, chia theo tỷ lệ từng phương thức đã trả (cổng hoàn về cổng, tiền mặt hoàn tại quầy, chừa điểm mở rộng cho thẻ quà). Phí cổng không hoàn. Phần combo đã nhận không nằm trong số hoàn. Ghi qua sổ giao dịch.
- Điểm tích từ đơn bị thu hồi theo phần hoàn (giữ logic hiện có). Chừa điểm gọi để task sau hoàn kho (T4.2) và không trả lại lượt mã khuyến mãi (T3.3).
- Hủy suất bởi quản trị: mọi đơn liên quan bị hủy và hoàn đầy đủ (cả combo chưa nhận); đơn qua cổng hoàn tự động; đơn tiền mặt đánh dấu "cần hoàn tiền mặt" có trạng thái đã hoàn hay chưa, quầy xác nhận đã hoàn thì ghi giao dịch hoàn; email báo khách.
- Dịch vụ hoàn từng phần theo dòng hàng dựa trên giá thực trả của dòng (3.3), dùng nội bộ cho các task sau.
- Phát OrderCancelled và OrderRefunded.

Tiêu chí hoàn thành: hoàn một phần hai lần truy vết đúng trên sổ; hủy suất hoàn đủ; đơn tiền mặt theo dõi được đã hoàn hay chưa.
```

---

### T1.8 — Quầy, trợ lý AI, sơ đồ ghế và API đơn (backend)

**Phụ thuộc:** T1.6, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc 2.1, H1.3 và H1.6.

- Luồng bán tại quầy trên Order: kênh quầy, khách vãng lai (tên, số điện thoại), thanh toán tiền mặt (nhận, thối) hoặc thẻ, ghi nhân viên tạo. Hỗ trợ đơn chỉ có combo không có suất (dựng khung, kiểm kho ở T4). Đơn tiền mặt chỉ hủy được tại quầy.
- Check-in bằng QR chuyển sang OrderLine, chống quét trùng, phát TicketCheckedIn.
- Cập nhật công cụ trợ lý AI tra đơn, suất, ghế sang Order.
- Cập nhật API sơ đồ ghế, danh sách và chi tiết đơn của khách và admin, lịch sử thanh toán sang mô hình mới; liệt kê DTO mới ở cuối câu trả lời để frontend bám theo.
- Xuất Excel đơn hiện có cập nhật theo mô hình mới.

Tiêu chí hoàn thành: bán vé quầy và đơn chỉ combo chạy qua Order; check-in quét hai lần chỉ nhận một; trợ lý AI tra đúng đơn mới.
```

---

### T1.9 — Frontend khách theo mô hình đơn hàng

**Phụ thuộc:** T1.8.

```text
Áp dụng QUY TẮC CHUNG. Đọc H1.3 và H1.6.

Cập nhật frontend khách theo DTO mới của T1.8: types, API client, hooks, rồi các màn hình chọn ghế, thanh toán, lịch sử đơn, chi tiết đơn và vé, hủy đơn.
- Chi tiết đơn hiển thị dòng hàng (vé, combo), khoản điều chỉnh giá, các giao dịch thu và hoàn, lý do hủy.
- Thanh toán xử lý được đơn có nhiều giao dịch (còn thiếu tiền thì hiện số còn phải trả).
- Giữ nguyên trải nghiệm giữ ghế thời gian thực và đếm ngược.

Tiêu chí hoàn thành: đặt vé, thanh toán, xem vé, hủy đơn chạy trên mô hình mới; không còn type hoặc API client của Booking.
```

---

### T1.10 — Frontend quầy và admin theo mô hình đơn hàng

**Phụ thuộc:** T1.8, T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc H1.3, H1.4 và H1.6.

- Màn bán vé tại quầy chuyển sang Order, có khách vãng lai, thanh toán tiền mặt (nhận, thối), dựng sẵn khung bán combo riêng không cần suất.
- Admin: danh sách đơn và chi tiết đơn (dòng hàng, điều chỉnh giá, sổ giao dịch thu và hoàn), hủy và hoàn, danh sách đơn tiền mặt cần hoàn kèm nút xác nhận đã hoàn, quản lý hủy suất theo luồng mới.
- Bỏ mọi type và API client cũ của Booking và Payment.

Tiêu chí hoàn thành: nhân viên bán vé và hủy tại quầy, admin xem sổ giao dịch của một đơn và xác nhận hoàn tiền mặt.
```

---

### T1.11 — Dữ liệu demo mới

**Phụ thuộc:** T1.10.

```text
Áp dụng QUY TẮC CHUNG. Đọc 2.2 và phần 6 (dòng Kiểm thử).

Viết lại seed theo mô hình mới, không chuyển đổi dữ liệu cũ, chạy lại nhiều lần không sinh trùng:
- Một rạp, 3 đến 4 phòng với sơ đồ ghế khác nhau, phim đang chiếu và sắp chiếu, suất nhiều ngày (cả ngày đã qua), hạng thành viên, khách có tài khoản, nhân viên, quản trị.
- Đơn mẫu: online đã xác nhận, đơn hết hạn, đơn hủy, hoàn một phần, đơn tiền mặt tại quầy, đơn trả bằng hai phương thức, đơn chỉ combo.
- Combo mẫu, loại vé mặc định để chờ T2.1.
Tách seed theo nhóm để các task sau thêm seed riêng.

Tiêu chí hoàn thành: reset DB và chạy seed cho dữ liệu đủ để thử mọi luồng của Hạng mục 1.
```

---

# HẠNG MỤC 2. Loại vé và quy tắc giá

---

### T2.1 — Loại vé, quy tắc giá, lịch ngày lễ (dữ liệu và API)

**Phụ thuộc:** T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc H2.1, H2.2 và 3.9.

- Entity TicketType: tên, hệ số giá, cờ yêu cầu xuất trình thẻ, giới hạn độ tuổi phim, khung giờ hoặc ngày được phép, trạng thái.
- Entity PricingRule: tên, loại (khung giờ, thứ trong tuần, ngày lễ và sự kiện, mức lấp đầy), điều kiện có cấu trúc theo từng loại (mức lấp đầy có các bậc), cách điều chỉnh (nhân hệ số hoặc cộng/trừ tiền), độ ưu tiên, thời gian hiệu lực, phạm vi phòng hoặc định dạng, phạm vi phim.
- Entity SpecialDay: lịch ngày lễ do quản trị khai báo.
- API admin CRUD cho cả ba, validate, xóa mềm, ghi nhật ký trước và sau, cache theo 3.9 và làm mới khi sửa.
- Cài đặt rạp mới: bước làm tròn giá vé (mặc định 1.000), trần tăng và sàn giảm của quy tắc giá động, giá tối thiểu một vé.
- Seed loại vé mặc định: người lớn, học sinh - sinh viên, trẻ em, người cao tuổi.

Tiêu chí hoàn thành: CRUD đầy đủ, mọi thay đổi có dòng nhật ký, sửa quy tắc làm mới cache ngay.
```

---

### T2.2 — Bộ tính giá (nguồn tính giá duy nhất)

**Phụ thuộc:** T2.1, T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.2 (bước 1), H2.2 và H2.3.

Tạo IPricingService, là nguồn tính giá duy nhất của hệ thống:
- Đầu vào: suất, ghế, loại vé, thời điểm tính. Đầu ra: giá niêm yết kèm danh sách quy tắc tác động và lý do.
- Công thức: giá cơ sở của suất × hệ số loại ghế × hệ số loại vé × quy tắc giá động. Phần quy tắc giá động bị chặn trần tăng và sàn giảm so với (giá cơ sở × hệ số ghế). Hệ số loại vé nằm ngoài trần và sàn. Sau cùng chặn giá tối thiểu và làm tròn theo bước cấu hình.
- Cách kết hợp nhiều quy tắc (cộng dồn hoặc lấy ưu tiên cao nhất) theo từng loại quy tắc; chọn phương án và nêu rõ ở cuối câu trả lời.
- Quy tắc mức lấp đầy đọc tỷ lệ ghế đã bán và đang giữ của suất; chừa điểm nối tốc độ bán từ bảng tổng hợp (T6.1).
- Kiểm tra điều kiện loại vé (độ tuổi phim, khung giờ, ngày).
- Suất riêng không áp quy tắc giá; suất sự kiện chỉ áp quy tắc riêng của sự kiện (chừa điểm cho T23.1).
- Hiệu năng: dùng cache quy tắc, có hàm tính theo lô cho cả sơ đồ ghế.
- API admin xem trước giá: chọn suất, trả bảng giá theo loại ghế × loại vé kèm quy tắc tác động.

Tiêu chí hoàn thành: một suất cho giá đúng ở các khung giờ và mức lấp đầy khác nhau; không vượt trần, không dưới sàn và giá tối thiểu.
```

---

### T2.3 — Tích hợp bộ tính giá vào luồng đặt vé

**Phụ thuộc:** T2.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H2.1 (phần xuất trình thẻ) và H2.3.

Dùng IPricingService ở mọi nơi hiển thị hoặc tính giá:
- Sơ đồ ghế trả giá từng ghế theo loại ghế và loại vé.
- Giữ ghế: tính lại giá ở thời điểm giữ, khóa vào OrderLine kèm loại vé. Nếu giá khác với giá khách đã thấy thì API trả trạng thái yêu cầu xác nhận giá mới, và chỉ tiếp tục khi request mang cờ chấp nhận giá.
- Quầy kiểm tra điều kiện loại vé trực tiếp.
- Lịch chiếu và trang phim trả giá "từ" thấp nhất đang áp dụng.
- Trợ lý AI, email, vé, báo cáo dùng cùng nguồn giá.
- Vé lưu loại vé và cờ cần xuất trình thẻ. API check-in trả cờ nhắc kiểm tra thẻ; nhân viên ghi nhận "không có thẻ" và quầy tạo đơn bổ sung thu chênh lệch.

Tiêu chí hoàn thành: giá khóa không đổi sau khi giữ ghế; giá đổi trước khi giữ thì API yêu cầu xác nhận; check-in vé có điều kiện thẻ trả đúng cờ nhắc.
```

---

### T2.4 — Frontend admin: loại vé, quy tắc giá, lịch lễ, xem trước giá

**Phụ thuộc:** T2.1, T2.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H2.4.

Các trang admin:
- Danh sách và form loại vé.
- Danh sách và form quy tắc giá theo từng loại quy tắc (khung giờ, thứ, ngày lễ, mức lấp đầy với các bậc), chọn phạm vi phòng, định dạng, phim.
- Lịch ngày lễ.
- Xem trước giá: chọn suất, hiển thị bảng giá loại ghế × loại vé và danh sách quy tắc tác động kèm lý do.
- Lịch sử thay đổi quy tắc lấy từ nhật ký thao tác.

Tiêu chí hoàn thành: quản trị tạo quy tắc, xem trước giá thấy thay đổi, xem lịch sử sửa.
```

---

### T2.5 — Frontend khách và quầy: loại vé, giá, xác nhận đổi giá

**Phụ thuộc:** T2.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H2.1 và H2.3.

- Màn chọn ghế: chọn loại vé cho từng ghế, hiển thị giá theo loại ghế và loại vé, tổng tạm tính.
- Khi API trả yêu cầu xác nhận giá mới: hộp thoại nêu giá cũ, giá mới, chờ khách đồng ý mới tiếp tục.
- Lịch chiếu và trang phim hiển thị giá "từ".
- Quầy: chọn loại vé, hiển thị điều kiện, nhắc xuất trình thẻ.
- Màn check-in: hiện nhắc kiểm tra thẻ, nút ghi nhận không có thẻ và tạo đơn thu chênh lệch.

Tiêu chí hoàn thành: khách chọn loại vé từng ghế, thấy cảnh báo khi giá đổi, nhân viên check-in thấy nhắc thẻ.
```

---

# HẠNG MỤC 3. Khuyến mãi

---

### T3.1 — Dữ liệu và CRUD khuyến mãi

**Phụ thuộc:** T1.4, T2.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H3.1.

- Entity Promotion: mã (duy nhất, không phân biệt hoa thường), tên, mô tả; loại ưu đãi (phần trăm có trần giảm, số tiền cố định); điều kiện (giá trị đơn tối thiểu, thời gian hiệu lực, kênh bán, phim hoặc suất, loại suất, thứ trong tuần, hạng thành viên được áp dụng, chỉ khách mới); phạm vi giảm (chỉ vé, chỉ combo, toàn đơn); giới hạn (tổng lượt, lượt mỗi khách, ngân sách); độ ưu tiên và cờ không dùng chung; loại (mã nhập tay, tự động áp dụng, mã cá nhân dùng một lần).
- Entity PromotionRedemption: khuyến mãi, đơn, khách, trạng thái (giữ chỗ, đã dùng, nhả), số tiền giảm, thời điểm.
- API admin: tạo, sửa, tạm dừng, xóa mềm; tạo hàng loạt mã cá nhân (mỗi mã gắn một tài khoản, hạn dùng mặc định cấu hình); xuất lịch sử dùng bằng hạ tầng Excel hiện có.
- Ghi nhật ký, cache khuyến mãi đang hoạt động theo 3.9.

Tiêu chí hoàn thành: tạo được các loại khuyến mãi, tạo hàng loạt mã cá nhân gắn đúng tài khoản, mọi thay đổi có nhật ký.
```

---

### T3.2 — Bộ áp khuyến mãi và phân bổ giảm giá về dòng hàng

**Phụ thuộc:** T3.1, T2.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.2, 3.3 và H3.2.

- IPromotionEngine: kiểm tra từng điều kiện, trả kết quả hợp lệ hoặc mã lỗi kèm lý do tiếng Việt (hết hạn, chưa đủ điều kiện, hết lượt, không thuộc hạng, hết ngân sách...).
- Khi nhiều ưu đãi đủ điều kiện: theo độ ưu tiên, tôn trọng cờ không dùng chung, nếu không dùng chung thì chọn ưu đãi có lợi hơn cho khách. Tổng giảm không vượt giá trị phạm vi áp dụng; đơn không bao giờ âm. Giá trị tính giảm là giá trị phạm vi sau khi đã trừ ưu đãi hạng và quyền lợi gói thành viên.
- Mở rộng bộ tính giá cấp đơn theo đúng thứ tự 3.2 bước 4 đến 7: ưu đãi hạng (chỉ trên tiền vé), chỗ trống cho quyền lợi gói thành viên (T21.1), mã khuyến mãi, tổng phải thanh toán.
- DiscountAllocator phân bổ mọi khoản giảm cấp đơn về từng dòng hàng theo tỷ lệ giá trị trong phạm vi áp dụng, dòng cuối nhận phần dư; ghi "giá thực trả của dòng" và OrderPriceAdjustment.
- Đơn thẻ quà, gói thành viên, thuê phòng không hưởng ưu đãi hạng và không dùng mã. Hỗ trợ khuyến mãi loại tự động áp dụng.

Tiêu chí hoàn thành: tổng các dòng sau phân bổ khớp số phải trả đến đồng; không có đơn giá âm; cờ không dùng chung hoạt động đúng.
```

---

### T3.3 — Vòng đời lượt dùng khuyến mãi (nguyên tử)

**Phụ thuộc:** T3.2, T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.5 và H3.3.

- Cài IResourceHold cho khuyến mãi: giữ chỗ tổng lượt, lượt mỗi khách và ngân sách (tính bằng tiền giảm) bằng UPDATE có điều kiện nguyên tử.
- API áp và gỡ mã trên đơn chờ thanh toán, tính lại số phải trả sau mỗi lần.
- Vòng đời: áp mã là giữ chỗ; xác nhận đơn là đã dùng (handler trong cùng giao dịch); hết hạn, hủy khi chưa thanh toán hoặc gỡ mã là nhả; đơn đã thanh toán rồi hủy hoặc hoàn thì giữ nguyên đã dùng; đổi suất giữ nguyên và không kiểm tra lại điều kiện (chừa điểm cho T11).
- Hết ngân sách thì mã ngừng áp dụng kể cả còn lượt. Mã cá nhân chỉ dùng được bởi đúng tài khoản được cấp, dùng một lần.
- Quầy nhập mã cho khách, kể cả khách vãng lai với mã nhập tay.

Tiêu chí hoàn thành: mã giới hạn 100 lượt không bị dùng quá 100 khi 200 khách cùng áp; gỡ mã hoặc hết hạn nhả lại lượt; hủy sau thanh toán không trả lượt.
```

---

### T3.4 — Frontend khách và quầy: nhập mã khuyến mãi

**Phụ thuộc:** T3.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H3.4.

- Bước thanh toán của khách: ô nhập mã, nút áp và gỡ, hiển thị số tiền giảm, hoặc lý do mã không hợp lệ lấy từ API; danh sách mã cá nhân đang có trong tài khoản, bấm để áp.
- Tổng tiền cập nhật ngay khi áp hoặc gỡ.
- Quầy: nhập mã cho khách trực tiếp, kể cả khách vãng lai.

Tiêu chí hoàn thành: khách áp, gỡ mã và thấy lý do lỗi rõ ràng; quầy áp được mã nhập tay.
```

---

### T3.5 — Frontend admin và báo cáo khuyến mãi

**Phụ thuộc:** T3.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H3.4 và H3.5.

- API báo cáo theo từng khuyến mãi: số lượt dùng, tổng tiền giảm đã chi, doanh thu các đơn dùng mã, mức sử dụng so với giới hạn và ngân sách, hiệu quả theo kênh và khung giờ.
- Trang admin: danh sách khuyến mãi, form tạo và sửa theo từng loại, tạm dừng, xóa mềm, tạo hàng loạt mã cá nhân, xem và xuất lịch sử dùng, trang báo cáo khuyến mãi.

Tiêu chí hoàn thành: quản trị tạo chiến dịch khuyến mãi, theo dõi mức dùng và xuất lịch sử.
```

---

# HẠNG MỤC 4. Kho bắp nước và bán combo tại quầy

---

### T4.1 — Dữ liệu kho và công thức combo

**Phụ thuộc:** T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc H4.1.

- Entity: InventoryItem (mã, tên, đơn vị tính, giá vốn hiện hành, ngưỡng cảnh báo, trạng thái); ComboRecipe và dòng công thức (sản phẩm kho, số lượng tiêu hao cho một combo); StockReceipt và dòng (nhà cung cấp, ngày nhập, số lượng, đơn giá nhập; mỗi lần nhập cập nhật giá vốn bình quân gia quyền); StocktakeSheet và dòng (số đếm thực tế, chênh lệch ghi thành điều chỉnh); WasteSheet (hủy hàng hết hạn, hỏng, đổ vỡ, có lý do); StockLedger (mọi biến động nhập, bán, hoàn, kiểm kê, hao hụt đều một dòng, tồn là cộng dồn).
- Combo chưa khai báo công thức thì không theo dõi kho và luôn được bán.
- API admin CRUD, nhập kho, kiểm kê, hao hụt; nhân viên chỉ xem tồn; sổ cái theo sản phẩm và khoảng thời gian; báo cáo giá trị tồn.
- Ghi nhật ký các điều chỉnh kho.

Tiêu chí hoàn thành: tồn bằng tổng sổ cái; nhập kho cập nhật đúng giá vốn bình quân gia quyền.
```

---

### T4.2 — Giữ chỗ tồn, trừ kho, hoàn kho, giá vốn

**Phụ thuộc:** T4.1, T1.6, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.5 và H4.2.

- IResourceHold cho kho: khi giữ ghế kèm combo có công thức, giữ chỗ nguyên liệu theo công thức bằng UPDATE có điều kiện đủ hàng (bảng giữ chỗ riêng). Tồn khả dụng = tồn sổ sách − phần đang giữ chỗ.
- Handler trong giao dịch khi xác nhận đơn: ghi sổ cái bán, Commit giữ chỗ. Hết hạn hoặc hủy chưa thanh toán thì Release.
- Hủy đơn đã xác nhận: hoàn kho chỉ khi combo chưa nhận (theo số đã nhận trên dòng).
- Lưu giá vốn combo (tổng giá vốn theo công thức) vào OrderLine tại thời điểm bán.
- Combo thiếu nguyên liệu tự ẩn khỏi danh sách bán online và quầy, hiện lại khi có hàng; kiểm tra thêm khi thêm combo vào đơn.

Tiêu chí hoàn thành: hai khách cùng giữ combo cuối cùng chỉ một người giữ được; hết hạn giữ thì tồn trở lại; hủy đơn chưa nhận hoàn kho, đã nhận không hoàn.
```

---

### T4.3 — Nhận combo, đơn chỉ combo, cảnh báo sắp hết

**Phụ thuộc:** T4.2, T1.8.

```text
Áp dụng QUY TẮC CHUNG. Đọc H4.2.

- API quầy đánh dấu đã nhận combo bằng quét QR đơn hoặc nhập mã đơn, nhận từng phần (cập nhật số đã nhận trên dòng, lưu lịch sử mỗi lần nhận và nhân viên nhận).
- Đơn chỉ có combo quá ngày mà chưa nhận được đánh dấu "chưa nhận"; API rà soát danh sách.
- Hoàn thiện bán combo riêng tại quầy: tạo đơn chỉ có combo không cần suất, kiểm kho, thanh toán ngay.
- Tác vụ nền định kỳ (chu kỳ cấu hình) kiểm tra tồn chạm ngưỡng, gửi thông báo cho quản trị bằng kênh hiện có, không gửi lặp; API cảnh báo cho màn hình kho.
- Báo cáo phân biệt combo đã bán và đã giao.

Tiêu chí hoàn thành: nhận combo từng phần đúng; đơn chỉ combo quá ngày hiện cờ chưa nhận; tồn chạm ngưỡng gửi đúng một cảnh báo.
```

---

### T4.4 — Frontend admin: kho

**Phụ thuộc:** T4.1, T4.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H4.3.

Trang admin: danh mục sản phẩm kho, công thức combo, nhập kho, kiểm kê, hao hụt, sổ cái theo sản phẩm và khoảng thời gian, bảng tồn kho có cảnh báo sắp hết, báo cáo giá trị tồn, danh sách đơn combo chưa nhận.

Tiêu chí hoàn thành: quản trị thực hiện được nhập, kiểm kê, hao hụt và xem sổ cái khớp tồn.
```

---

### T4.5 — Frontend quầy và khách: combo riêng, nhận combo, tồn

**Phụ thuộc:** T4.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H4.3.

- Quầy: màn bán combo riêng (chọn combo, thanh toán ngay), màn nhận combo (quét QR hoặc nhập mã đơn, nhận từng phần), màn xem tồn cho nhân viên.
- Khách: combo hết hàng không hiện ở bước chọn combo; xử lý lỗi hết hàng khi giữ ghế.

Tiêu chí hoàn thành: nhân viên bán combo riêng và giao combo từng phần; khách không thấy combo đã hết.
```

---

# HẠNG MỤC 5. Đóng phòng bảo trì, khóa ghế và xử lý suất bị ảnh hưởng

---

### T5.1 — Đóng phòng và chặn tạo suất

**Phụ thuộc:** T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc H5 (đầu mục).

- Entity RoomClosure: phòng, khoảng thời gian, lý do.
- Chặn tạo hoặc sửa suất trùng khoảng đóng ở mọi đường: tạo tay, tạo hàng loạt, xếp lịch tự động; chừa điểm cho đơn thuê phòng (T19.3).
- API liệt kê các suất đã có bị ảnh hưởng, kèm số đơn và số khách.
- Lịch phòng trả thêm các khoảng bảo trì.
- Đánh dấu phòng đóng để báo cáo và phân tích lấp đầy không coi là bỏ trống.
- Ghi nhật ký đóng phòng.

Tiêu chí hoàn thành: đóng phòng thì không tạo được suất mới trong khoảng đó bằng bất kỳ đường nào; API liệt kê đúng suất ảnh hưởng.
```

---

### T5.2 — Chuyển suất sang phòng khác và hủy suất do đóng phòng

**Phụ thuộc:** T5.1, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc H5.

- API chuyển một suất sang phòng khác cùng giờ: kiểm tra phòng trống (tính thời gian đệm) và đủ ghế; ánh xạ ghế sang phòng mới theo hàng, số, loại ghế; ghế không có chỗ tương ứng thì đề xuất ghế gần nhất và cho quản trị chỉnh trước khi áp dụng.
- Đơn, vé, QR giữ nguyên. Khách nhận email thông báo đổi phòng.
- Ghế mới thuộc loại rẻ hơn: ghi nhận khoản cần hoàn chênh lệch để quản trị hoàn thủ công qua sổ giao dịch (chừa điểm nối thẻ tín dụng ở T11).
- Khách không đồng ý được hủy và hoàn đầy đủ.
- Hủy suất dùng luồng hiện có (T1.7) với lý do "đóng phòng".
- Ghi nhật ký.

Tiêu chí hoàn thành: chuyển phòng giữ nguyên đơn và vé, QR vẫn dùng được; hủy suất hoàn đủ tiền.
```

---

### T5.3 — Khóa ghế (trạng thái bị khóa)

**Phụ thuộc:** T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc H5 (phần ghế hỏng) và bảng hiện trạng ở mục 2.1.

- Thêm trạng thái ghế "bị khóa" cho ghế của suất chiếu: entity SeatBlock (suất, ghế, loại khóa: bảo trì hoặc hỏng, dành cho đơn thuê phòng, dành tạm cho người chờ; lý do; thời hạn).
- Ghế bị khóa không giữ và không bán được ở mọi luồng (online, quầy, sơ đồ ghế, trợ lý AI); cập nhật thời gian thực.
- Chặn khóa nếu ghế đã có người đặt, trả lỗi yêu cầu xử lý đơn trước.
- Ghế khóa không tính vào sức chứa bán được trong thống kê.
- API admin khóa và mở khóa; ghi nhật ký. Hạ tầng này được T17 và T19 dùng lại.

Tiêu chí hoàn thành: ghế khóa không bán được, không tính sức chứa; khóa ghế đã có đơn bị chặn.
```

---

### T5.4 — Frontend admin: đóng phòng, chuyển suất, khóa ghế

**Phụ thuộc:** T5.2, T5.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H5.

- Form đóng phòng, danh sách suất bị ảnh hưởng với số đơn và số khách, mỗi suất chọn chuyển phòng hoặc hủy suất.
- Giao diện chuyển phòng hiển thị ánh xạ ghế, ghế cần chỉnh, cho sửa trước khi xác nhận.
- Lịch phòng hiển thị khoảng bảo trì.
- Khóa và mở khóa ghế trên sơ đồ ghế admin; sơ đồ ghế của khách và quầy hiển thị ghế khóa không chọn được.

Tiêu chí hoàn thành: quản trị xử lý trọn một phòng đóng và khóa được ghế hỏng trên sơ đồ.
```

---

# HẠNG MỤC 6. Báo cáo, phân tích lấp đầy, tài chính và đối soát cổng thanh toán

---

### T6.1 — Bảng tổng hợp theo ngày và công cụ dựng lại

**Phụ thuộc:** T1.3, T4.2, T5.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.1 (ngày kinh doanh), 3.4 và H6.1.

- Bảng ShowtimeDailyStat: suất, ngày kinh doanh, sức chứa bán được (không tính lối đi, ghế khóa, ghế hỏng), số vé bán, doanh thu vé gộp, giảm giá, hoàn tiền, doanh thu combo, số đơn hủy, loại suất.
- Bảng PaymentDailyStat: ngày thu, kênh, phương thức, số giao dịch, tiền thu, tiền hoàn, phí cổng.
- Bảng ShowtimeSalesPace: tỷ lệ đã bán tại các mốc trước giờ chiếu (24 giờ, 3 giờ, mốc cấu hình).
- Cập nhật bằng tác vụ nền định kỳ và bằng handler nền khi suất kết thúc hoặc đơn thay đổi; idempotent; đúng ngày kinh doanh kể cả rạp mở qua nửa đêm.
- Công cụ dựng lại bảng tổng hợp cho một khoảng ngày (API admin và chạy nền), có tiến độ qua cơ chế job hiện có.
- Nối tốc độ bán vào quy tắc giá mức lấp đầy của IPricingService nếu phù hợp.

Tiêu chí hoàn thành: dựng lại một khoảng ngày cho cùng số liệu với cập nhật tăng dần; ngày kinh doanh qua nửa đêm tính đúng.
```

---

### T6.2 — API dashboard nâng cao

**Phụ thuộc:** T6.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H6.2.

Mọi số liệu đọc từ bảng tổng hợp:
- Tỷ lệ lấp đầy theo phòng, khung giờ, phim, ngày trong tuần, so sánh kỳ trước, lọc loại suất (mặc định chỉ suất thường).
- Bản đồ nhiệt ghế: tần suất từng ghế được bán và được chọn rồi bỏ trong từng phòng, lọc theo thời gian và loại phim. Nếu chưa có dữ liệu chọn rồi bỏ thì thêm bảng ghi lịch sử giữ và nhả ghế.
- Doanh thu combo: theo combo, theo kênh, tỷ lệ đơn có combo, giá trị combo trung bình mỗi đơn, đã bán so với đã giao.
- Tỷ lệ hủy: theo lý do, người hủy, khoảng thời gian từ lúc hủy đến giờ chiếu, theo phim; đổi suất không tính là hủy.
- Giữ nguyên các chỉ số hiện có (doanh thu hôm nay, vé bán, lấp đầy, top phim, hiệu suất nhân viên).

Tiêu chí hoàn thành: các API tải nhanh với dữ liệu lớn; lọc loại suất đúng; đổi suất không nằm trong tỷ lệ hủy.
```

---

### T6.3 — Phân tích lấp đầy và gợi ý lịch chiếu

**Phụ thuộc:** T6.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H6.3.

- API báo cáo phim × phòng × khung giờ: số suất, lấp đầy trung bình, doanh thu mỗi suất, doanh thu mỗi ghế mỗi giờ phòng.
- Gợi ý cho quản trị kèm lý do dễ hiểu: phim lấp đầy cao liên tục thì tăng suất và chỉ ra phòng, khung giờ còn trống; phim lấp đầy thấp liên tục thì giảm suất hoặc chuyển phòng nhỏ hơn, khung giờ khác; phòng và khung giờ bỏ trống nhiều thì đề xuất phim phù hợp.
- Ngưỡng cao, thấp, số suất tối thiểu để kết luận, số ngày nhìn lại cấu hình ở cài đặt rạp.
- Loại suất riêng và suất sự kiện khỏi mọi phân tích và khỏi "độ nóng" phim; phòng đóng bảo trì không bị coi là bỏ trống.
- API áp dụng đề xuất: chuyển các đề xuất được chọn sang xếp lịch tự động hiện có ở bước xem trước, không tạo suất trực tiếp.

Tiêu chí hoàn thành: đề xuất sinh đúng theo ngưỡng; áp dụng đề xuất dẫn tới màn xem trước của xếp lịch tự động.
```

---

### T6.4 — Báo cáo tài chính và xuất Excel, PDF

**Phụ thuộc:** T6.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.4 và H6.4.

- Xây khung báo cáo theo khối để các hạng mục sau thêm khối của mình (đăng ký khối báo cáo qua interface).
- Báo cáo: doanh thu theo kênh và theo phương thức thanh toán (tách vé, combo, thẻ quà tặng); giảm giá (ưu đãi hạng, khuyến mãi); hoàn tiền và hủy (số lượng, số tiền, tỷ lệ so doanh thu); phí cổng và doanh thu thực nhận sau phí; lợi nhuận combo = doanh thu combo trừ giá vốn tại thời điểm bán.
- Các khối lợi nhuận theo phim (T7.3) và khoản phải trả thẻ quà (T10.5) chỉ khai báo chỗ chờ, chưa hiển thị khi chưa có dữ liệu.
- Sửa báo cáo doanh thu hiện có để tổng vé và combo khớp tổng doanh thu sau giảm giá; thu tiền và doanh thu là hai khái niệm tách biệt (3.4).
- Xuất Excel và PDF bằng hạ tầng hiện có.

Tiêu chí hoàn thành: tổng vé + combo khớp doanh thu sau giảm giá ở mọi kỳ.
```

---

### T6.5 — Đối soát cổng thanh toán (backend)

**Phụ thuộc:** T1.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H6.5.

- Nhập file sao kê VNPay và MoMo bằng hạ tầng nhập file hiện có (chạy nền); Stripe lấy từ dữ liệu giao dịch của cổng qua client hiện có.
- Khớp từng dòng với sổ giao dịch theo mã giao dịch và phân loại: khớp, có ở cổng nhưng không có ở hệ thống, có ở hệ thống nhưng không có ở cổng, lệch số tiền, lệch trạng thái.
- Dòng bất thường lưu thành danh sách xử lý, có ghi chú và người xử lý. Cập nhật phí cổng trong file vào giao dịch tương ứng.
- Mỗi lần đối soát là một phiên có thống kê tổng, lưu để tra cứu. Nhập lại cùng file không tạo dòng trùng.
- Giao dịch "đã thu ở cổng nhưng đơn chưa xác nhận" vào danh sách bất thường, có điểm nối để T14.1 tự tạo yêu cầu hỗ trợ.

Tiêu chí hoàn thành: một file mẫu phân loại đúng cả năm kết quả; nhập lại file không sinh dòng trùng.
```

---

### T6.6 — Frontend dashboard nâng cao và phân tích lấp đầy

**Phụ thuộc:** T6.2, T6.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H6.2 và H6.3.

- Dashboard: biểu đồ lấp đầy theo phòng, khung giờ, phim, thứ, so kỳ trước, bộ lọc loại suất; bản đồ nhiệt ghế theo sơ đồ phòng; doanh thu combo; tỷ lệ hủy; giữ các thẻ chỉ số hiện có.
- Trang phân tích phim × phòng × khung giờ, danh sách gợi ý có lý do, chọn nhiều đề xuất và nút áp dụng chuyển sang màn xem trước xếp lịch tự động.
- Dùng thư viện biểu đồ đã có trong project; bộ lọc đồng bộ URL.

Tiêu chí hoàn thành: quản trị xem lấp đầy, bản đồ nhiệt, nhận gợi ý và áp dụng được sang xem trước xếp lịch.
```

---

### T6.7 — Frontend báo cáo tài chính và đối soát

**Phụ thuộc:** T6.4, T6.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc H6.4 và H6.5.

- Trang báo cáo tài chính hiển thị các khối của T6.4 theo khung khối, bộ lọc kỳ, nút xuất Excel và PDF.
- Trang đối soát: tải lên file sao kê, chọn cổng, xem phiên đối soát và thống kê, danh sách dòng bất thường theo loại, ghi chú và gán người xử lý, lịch sử các phiên.

Tiêu chí hoàn thành: quản trị tải file sao kê, thấy kết quả phân loại, xử lý dòng bất thường; xuất được báo cáo tài chính.
```

---

# HẠNG MỤC 7. Nhà phát hành và hợp đồng phim

---

### T7.1 — Nhà phát hành và hợp đồng phim

**Phụ thuộc:** T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc H7.

- Entity Distributor: tên, liên hệ, mã số thuế. Mỗi phim gắn một nhà phát hành.
- Entity FilmContract: phim, ngày khởi chiếu tại rạp, các tuần chiếu (mỗi tuần đủ 7 ngày tính từ ngày khởi chiếu) và tỷ lệ chia theo từng tuần, cách tính trước hoặc sau thuế, mức bảo đảm tối thiểu, thời gian hiệu lực, mức phí cố định cho suất riêng (tùy chọn).
- API admin CRUD cho cả hai, validate tuần liên tục và không chồng, ghi nhật ký.
- Phim chưa có hợp đồng trả trạng thái "chưa có hợp đồng", không coi tỷ lệ bằng 0.

Tiêu chí hoàn thành: một phim khai được hợp đồng với tỷ lệ khác nhau theo ba tuần; phim chưa có hợp đồng nhận diện được.
```

---

### T7.2 — Đối soát nhà phát hành

**Phụ thuộc:** T7.1, T6.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.3, 3.4 và H7.

- Doanh thu vé để chia = tổng "giá thực trả của dòng" của vé thuộc suất thường trong tuần chiếu, sau khi trừ hoàn tiền. Suất riêng không tính (trừ khi hợp đồng có phí cố định); suất sự kiện tính như suất thường; chừa điểm cộng giá trị quy đổi vé miễn phí từ gói thành viên (T21.3).
- Đối soát theo kỳ (tuần hoặc tháng, cấu hình): với mỗi phim tính doanh thu từng tuần chiếu nhân tỷ lệ, so với mức bảo đảm tối thiểu và lấy giá trị lớn hơn; tính theo trước hoặc sau thuế theo hợp đồng.
- Trạng thái bảng đối soát: nháp, đã gửi, đã chốt, đã thanh toán, ghi ngày thanh toán. Đối soát đã chốt không tự đổi khi có hoàn tiền muộn; chênh lệch phát sinh chuyển sang kỳ kế tiếp dưới dạng điều chỉnh.
- Xuất PDF và Excel để gửi nhà phát hành.

Tiêu chí hoàn thành: tính ra số phải trả khớp tay với ba tuần có tỷ lệ khác nhau; có mức bảo đảm thì lấy mức lớn hơn; hoàn muộn đi vào kỳ sau.
```

---

### T7.3 — Frontend nhà phát hành và lợi nhuận theo phim

**Phụ thuộc:** T7.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H7 và H6.4.

- Trang admin: nhà phát hành, hợp đồng phim (form tuần chiếu và tỷ lệ), danh sách đối soát với đổi trạng thái, xem chi tiết và tải PDF, Excel, danh sách phim chưa có hợp đồng, lịch thanh toán.
- Hiển thị khối lợi nhuận theo phim trong báo cáo tài chính (doanh thu vé trừ phần chia nhà phát hành); phim chưa có hợp đồng hiện "chưa có hợp đồng".

Tiêu chí hoàn thành: quản trị tạo hợp đồng, chạy đối soát kỳ, xuất file; báo cáo lợi nhuận theo phim hiện đúng.
```

---

# HẠNG MỤC 8. Chốt doanh thu cuối ngày và chốt ca quầy

---

### T8.1 — Ca quầy (backend)

**Phụ thuộc:** T1.8, T6.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H8.1 và 3.11.

- Entity CounterShift: nhân viên, thời điểm mở, tiền đầu ca, thời điểm đóng, số tiền thực đếm, số phải có, chênh lệch, ghi chú.
- Mở ca và đóng ca; số phải có = tiền đầu ca + thu tiền mặt − hoàn tiền mặt − tiền thối. Mọi đơn quầy gắn vào ca đang mở của nhân viên.
- Chặn bán tại quầy khi chưa mở ca. Mỗi nhân viên chỉ một ca mở. Nhân viên chỉ xem và đóng ca của mình; quản trị xem và đóng mọi ca.
- Tác vụ nền cảnh báo quản trị khi cuối ngày còn ca chưa đóng.
- Ghi nhật ký mở, đóng và chênh lệch.

Tiêu chí hoàn thành: không bán được khi chưa mở ca; đóng ca ghi đúng chênh lệch; ca quên đóng được cảnh báo.
```

---

### T8.2 — Chốt ngày và bút toán điều chỉnh

**Phụ thuộc:** T8.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.4 (phần sau chốt ngày) và H8.2.

- Báo cáo chốt ngày gồm: doanh thu vé, combo, thẻ quà bán và thẻ quà đã dùng, tiền giảm giá, hoàn tiền, thu theo từng phương thức, số đơn chờ hoặc lỗi. Các dòng thẻ quà, gói, thuê phòng xuất hiện khi hạng mục tương ứng đã có (đăng ký qua khung khối báo cáo).
- Tiền mặt tại quầy tổng hợp từ các ca; quản trị nhập tổng tiền thực đếm cả ngày, hệ thống ghi chênh lệch và ghi chú.
- Chốt ngày khóa số liệu. Chỉ chốt khi mọi ca đã đóng và không còn đơn lỗi, trừ khi cài đặt cho phép bỏ qua có ghi chú.
- Entity bút toán điều chỉnh: ghi vào ngày kinh doanh đang mở, tham chiếu ngày gốc, lý do, người tạo. Hoàn tiền, đổi suất, hủy phát sinh sau chốt tự tạo bút toán. Báo cáo cho xem "theo ngày gốc" và "theo ngày ghi sổ", thể hiện rõ phần điều chỉnh.
- Báo cáo chốt gửi email quản trị và lưu để tra cứu theo ngày. Ghi nhật ký chốt và bút toán.

Tiêu chí hoàn thành: ngày đã chốt không sửa được số trực tiếp; hoàn tiền muộn đi vào bút toán của ngày đang mở.
```

---

### T8.3 — Frontend ca quầy và chốt ngày

**Phụ thuộc:** T8.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H8.1 và H8.2.

- Quầy: màn mở ca (nhập tiền đầu ca), đóng ca (nhập thực đếm, xem chênh lệch, ghi chú), chặn vào màn bán khi chưa mở ca kèm hướng dẫn.
- Admin: danh sách ca, trang chốt ngày (báo cáo chốt, xác nhận tiền thực đếm, cảnh báo điều kiện chưa đủ, nút chốt), danh sách bút toán điều chỉnh, báo cáo theo ngày gốc và ngày ghi sổ, lịch sử báo cáo chốt.

Tiêu chí hoàn thành: nhân viên mở và đóng ca, quản trị chốt ngày và xem bút toán điều chỉnh.
```

---

# HẠNG MỤC 9. Hóa đơn VAT theo yêu cầu

---

### T9.1 — Hóa đơn VAT (backend)

**Phụ thuộc:** T1.8.

```text
Áp dụng QUY TẮC CHUNG. Đọc H9 và 3.4.

- Cài đặt thuế suất theo loại dòng hàng (vé, combo, thẻ quà tặng, gói thành viên) và thời hạn yêu cầu hóa đơn. Giá hiển thị đã gồm VAT; tách giá trước thuế và tiền thuế theo từng dòng cho hóa đơn và báo cáo. Thuế thẻ quà tính vào lúc thẻ được dùng.
- Entity InvoiceProfile (mã số thuế, tên đơn vị, địa chỉ, email nhận; lưu dùng lại) và InvoiceRequest (đơn, hồ sơ, trạng thái chờ xuất, đã xuất, từ chối kèm lý do, số hóa đơn, tệp đính kèm, người xuất, thời điểm).
- Khách yêu cầu hóa đơn cho đơn đã thanh toán trong thời hạn cấu hình; quầy nhập cho khách vãng lai. Quản trị cập nhật số hóa đơn và tải tệp bằng hạ tầng file hiện có; khách tải hóa đơn từ lịch sử đơn. Không tích hợp nhà cung cấp hóa đơn điện tử.
- Đơn đã xuất hóa đơn mà bị hoàn tiền: đánh dấu "cần điều chỉnh hóa đơn" và nhắc quản trị.
- Báo cáo thuế: doanh thu trước thuế, thuế phải nộp theo kỳ và theo loại dòng hàng (đăng ký vào khung khối báo cáo).
- Email thông báo trạng thái, ghi nhật ký xuất hóa đơn.

Tiêu chí hoàn thành: tách đúng giá trước thuế và thuế theo từng loại dòng; đơn hoàn sau hóa đơn bị đánh dấu.
```

---

### T9.2 — Frontend hóa đơn VAT

**Phụ thuộc:** T9.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H9.

- Khách: form yêu cầu hóa đơn trên chi tiết đơn, quản lý hồ sơ xuất hóa đơn, theo dõi trạng thái, tải hóa đơn.
- Quầy: nhập yêu cầu cho khách vãng lai.
- Admin: danh sách yêu cầu theo trạng thái, cập nhật số hóa đơn và tệp, từ chối kèm lý do, danh sách đơn cần điều chỉnh hóa đơn, báo cáo thuế.

Tiêu chí hoàn thành: khách gửi yêu cầu và tải được hóa đơn sau khi quản trị cập nhật.
```

---

# HẠNG MỤC 10. Thẻ quà tặng (e-gift)

---

### T10.1 — Dữ liệu thẻ, mua thẻ và phát hành

**Phụ thuộc:** T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.2 (ngoại lệ), 3.4, H10.1 và H10.2.

- Entity GiftCard: mã thẻ ngẫu nhiên mật mã đủ dài khó đoán, loại (quà tặng, tín dụng đổi suất), mệnh giá, số dư, người mua, email và tên người nhận, lời nhắn, thời điểm gửi, hạn dùng, trạng thái (chờ gửi, đã gửi, đang dùng, đã dùng hết, hết hạn, đã hủy). Entity GiftCardLedger ghi phát hành, giữ chỗ, sử dụng, hoàn lại, hết hạn, hủy.
- Cài đặt: mệnh giá có sẵn, mệnh giá tối thiểu và tối đa, thời hạn thẻ.
- Mua thẻ qua Order loại thẻ quà: người mua có tài khoản, chọn mệnh giá có sẵn hoặc tự nhập trong khoảng, nhập thông tin người nhận và lời nhắn, gửi ngay hoặc hẹn giờ. Không dùng thẻ để mua thẻ; không ưu đãi hạng, không mã khuyến mãi, không tích điểm.
- Khi đơn xác nhận, handler trong giao dịch phát hành thẻ. Email gửi người nhận (lời nhắn, mã, hướng dẫn) qua tác vụ nền hẹn giờ; người mua nhận email xác nhận. API gửi lại email cho quản trị.

Tiêu chí hoàn thành: mua thẻ xong thì thẻ được phát hành đúng mệnh giá; gửi hẹn giờ đúng lúc; mã thẻ không đoán được.
```

---

### T10.2 — Dùng thẻ để thanh toán, hoàn về thẻ, an toàn

**Phụ thuộc:** T10.1, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.1, 3.5, 3.6, H10.3 và H10.4.

- IResourceHold cho thẻ: khi khách áp thẻ vào đơn chờ, giữ chỗ số dư bằng UPDATE có điều kiện; chỉ trừ thật khi đơn xác nhận; nhả khi hết hạn hoặc gỡ thẻ. Một thẻ không bị hai đơn cùng tiêu.
- API áp và gỡ thẻ; thẻ trả một phần hoặc toàn bộ, ghi giao dịch thu phương thức thẻ quà. Thẻ đủ trả hết thì xác nhận ngay không qua cổng; chưa đủ thì phần còn lại trả bằng phương thức khác. Dùng được cho vé, combo, gói sản phẩm, gói thành viên, thuê phòng; không dùng để mua thẻ quà.
- Hoàn tiền về lại thẻ theo 3.6 (mở rộng T1.7); thẻ đã hết hạn khi nhận hoàn được gia hạn tối thiểu số ngày cấu hình. Điểm vẫn tích trên phần khách thực trả kể cả bằng thẻ.
- Giới hạn số lần thử kiểm tra số dư và áp mã, khóa tạm khi nhập sai quá ngưỡng (cấu hình).
- Quản trị hủy thẻ, thẻ đã dùng một phần thì chọn cách xử lý số dư kèm lý do, ghi nhật ký.
- Tác vụ nền cho thẻ hết hạn, ghi sổ thẻ.
- Quầy nhận thẻ bằng cách nhập mã.
- Loại thẻ tín dụng đổi suất dùng được như thẻ thường nhưng chỉ cho đúng tài khoản (T11 sẽ cấp).
- Ghi nhận doanh thu khi thẻ được dùng (3.4).

Tiêu chí hoàn thành: thẻ trả một phần cộng cổng xác nhận đúng; hai đơn không tiêu cùng số dư; nhập sai nhiều lần bị khóa tạm; hoàn đơn đưa tiền về thẻ.
```

---

### T10.3 — Quà doanh nghiệp (mua nhiều thẻ)

**Phụ thuộc:** T10.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H10.5.

- Doanh nghiệp tải lên file danh sách người nhận (tên, email, mệnh giá, lời nhắn) bằng hạ tầng nhập file hiện có, chạy nền.
- Kiểm tra danh sách: email hợp lệ, mệnh giá trong khoảng, không trùng; trả báo cáo lỗi theo dòng, cho tải file mẫu.
- Tạo một đơn tổng cho cả danh sách, thanh toán như đơn thường. Khi đơn xác nhận thì phát hành từng thẻ và gửi từng thẻ cho người nhận.

Tiêu chí hoàn thành: file hợp lệ tạo một đơn tổng và phát hành đủ thẻ; dòng lỗi được báo rõ.
```

---

### T10.4 — Frontend khách và quầy: thẻ quà tặng

**Phụ thuộc:** T10.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H10.6.

- Khách: trang mua thẻ quà (chọn mệnh giá, nhập người nhận, lời nhắn, hẹn giờ gửi), danh sách thẻ đã mua và trạng thái gửi, ô nhập thẻ ở bước thanh toán hiển thị số dư và số tiền trừ, danh sách thẻ tín dụng đang có, thông báo khi bị khóa tạm.
- Quầy: nhập mã thẻ khi thanh toán.
- Trang tải lên danh sách quà doanh nghiệp cho khách doanh nghiệp.

Tiêu chí hoàn thành: khách mua thẻ, dùng thẻ trả một phần và thanh toán phần còn lại.
```

---

### T10.5 — Frontend admin và khoản phải trả thẻ quà

**Phụ thuộc:** T10.2, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc H10.6 và H6.4.

- Trang admin: danh sách thẻ, tra cứu theo mã hoặc email, lịch sử sử dụng của thẻ, hủy thẻ kèm lý do, gửi lại email.
- Báo cáo khoản phải trả thẻ quà tặng và thẻ tín dụng (tổng đã bán chưa dùng, gồm cả số dư hết hạn) và gắn vào khung khối của báo cáo tài chính; số liệu khớp tổng số dư.

Tiêu chí hoàn thành: quản trị tra cứu và hủy thẻ; khoản phải trả khớp tổng số dư.
```

---

# HẠNG MỤC 11. Đổi suất chiếu

---

### T11.1 — Điều kiện và tính chênh lệch đổi suất (xem trước)

**Phụ thuộc:** T10.2, T2.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.3 và H11.

API xem trước đổi suất (chưa thay đổi dữ liệu):
- Điều kiện: đơn đã xác nhận; suất mới cùng phim, là suất thường hoặc sự kiện còn mở bán, không đổi sang hoặc từ suất riêng; trước hạn đổi (số giờ trước giờ chiếu cấu hình); số lần đổi của đơn chưa vượt cấu hình (mặc định một); vé đã check-in không được đổi; đổi theo từng vé hoặc cả đơn, số ghế đổi không vượt số vé; đơn tiền mặt chỉ đổi tại quầy.
- Tính giá mới bằng IPricingService với loại vé giữ nguyên và kiểm tra lại điều kiện loại vé, so với giá thực trả của dòng vé cũ: đắt hơn thì số cần thu thêm, rẻ hơn thì số tín dụng được cấp.
- Trả lỗi có lý do rõ ràng khi không đủ điều kiện.
- Cài đặt: hạn đổi, số lần đổi tối đa, hạn thẻ tín dụng đổi suất.

Tiêu chí hoàn thành: xem trước trả đúng chênh lệch và chặn đúng các trường hợp không đủ điều kiện.
```

---

### T11.2 — Thực thi đổi suất

**Phụ thuộc:** T11.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.4, 3.5 và H11.

- Giữ ghế mới (nguyên tử). Suất đắt hơn: tạo khoản thu thêm qua đơn bổ sung hoặc giao dịch thu thông thường trên đơn, đơn chỉ hoàn tất đổi khi đủ tiền. Suất rẻ hơn: cấp thẻ tín dụng đổi suất (T10) gắn tài khoản khách, có hạn cấu hình, không hoàn về cổng; đơn tiền mặt của khách vãng lai tại quầy hoàn chênh lệch bằng tiền mặt và ghi giao dịch hoàn.
- Khi hoàn tất: nhả ghế cũ, vô hiệu vé và QR cũ, phát vé và QR mới, email xác nhận, ghi lịch sử từng lần đổi trên đơn.
- Mã khuyến mãi đã dùng, điểm đã tích, số tiền giảm của vé cũ giữ nguyên; combo không bị ảnh hưởng.
- Doanh thu vé chuyển từ ngày chiếu cũ sang ngày chiếu mới, qua bút toán điều chỉnh nếu ngày cũ đã chốt (T8.2).
- Đổi suất không tính là hủy và không tạo hoàn tiền ra ngoài. Cập nhật bảng tổng hợp.
- Quầy thực hiện đổi suất giúp khách.

Tiêu chí hoàn thành: đổi sang suất đắt hơn thu đúng chênh lệch; rẻ hơn cấp đúng tín dụng; vé và QR cũ không còn quét được.
```

---

### T11.3 — Frontend và báo cáo đổi suất

**Phụ thuộc:** T11.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H11.

- Khách: nút đổi suất trên chi tiết đơn, chọn suất mới cùng phim, xem trước chênh lệch, thanh toán thêm hoặc nhận tín dụng, xem lịch sử đổi.
- Quầy: đổi suất giúp khách trực tiếp.
- Báo cáo admin: số lần đổi, tỷ lệ đổi so với hủy, chênh lệch thu thêm và tín dụng đã cấp, phim và khung giờ hay bị đổi (đăng ký vào khung báo cáo).

Tiêu chí hoàn thành: khách đổi suất trọn luồng; báo cáo hủy không đếm đổi suất.
```

---

# HẠNG MỤC 12. Bán thêm combo sau khi đặt vé và nhắc lịch chiếu

---

### T12.1 — Đơn bổ sung combo đặt trước

**Phụ thuộc:** T4.2, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.6 và H12.

- Tạo đơn bổ sung (loại đơn bổ sung, trỏ về đơn gốc) chỉ gồm combo, thanh toán riêng, giữ chỗ kho như đơn thường; vé và ghế của đơn gốc không bị ảnh hưởng.
- Giá ưu đãi cho combo đặt trước (cấu hình), còn hiệu lực đến trước giờ chiếu.
- Gợi ý combo theo số vé (hai vé gợi ý combo đôi) và lịch sử mua của khách.
- Đơn bổ sung không bị hủy theo đơn gốc, nhưng khi đơn gốc hủy hoặc đổi suất thì khách được nhắc xử lý (hủy và hoàn nếu chưa nhận, hoặc giữ nhận vào suất mới). Khi rạp hủy suất hoặc đóng phòng thì đơn bổ sung bị hủy và hoàn cùng đơn gốc (mở rộng T1.7 và T5.2).
- Combo đặt trước nhận tại quầy bằng QR.

Tiêu chí hoàn thành: đơn bổ sung thanh toán độc lập và nhận được bằng QR; hủy suất hoàn cả hai đơn.
```

---

### T12.2 — Nhắc lịch chiếu và email gợi ý combo

**Phụ thuộc:** T12.1, T1.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.10 và H12.

- Tác vụ nền gửi nhắc lịch trước giờ chiếu theo số giờ cấu hình: mã QR, địa chỉ rạp, phòng, giờ mở check-in, hướng dẫn nhận combo. Chỉ gửi cho khách chưa tắt nhắc lịch; idempotent, mỗi vé chỉ nhắc một lần.
- Suất bị đổi hoặc hủy thì hủy nhắc cũ.
- Email sau khi đặt vé gợi ý thêm combo đặt trước kèm liên kết.

Tiêu chí hoàn thành: nhắc gửi đúng giờ, không gửi cho suất đã đổi hoặc hủy, không gửi cho khách đã tắt.
```

---

### T12.3 — Frontend và báo cáo combo đặt trước

**Phụ thuộc:** T12.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H12.

- Khách: khối gợi ý combo trên trang xác nhận đặt vé và chi tiết đơn, thanh toán đơn bổ sung, thông báo nhắc xử lý đơn bổ sung khi đơn gốc hủy hoặc đổi.
- Báo cáo admin: tỷ lệ khách mua thêm combo sau khi đặt vé, doanh thu combo đặt trước, so sánh với combo mua tại quầy, tỷ lệ combo đặt trước đã nhận.

Tiêu chí hoàn thành: khách mua thêm combo từ trang xác nhận; báo cáo hiển thị đủ các chỉ số.
```

---

# HẠNG MỤC 13. Loyalty mạnh hơn

---

### T13.1 — Sổ cái điểm theo lô, hai loại điểm

**Phụ thuộc:** T1.6, T3.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H13.1 và H13.2.

- Tách điểm xếp hạng (cửa sổ cấu hình hiện có, quyết định hạng, đổi quà không làm giảm) và số dư điểm đổi quà (có hạn dùng riêng cấu hình).
- Mở rộng dòng điểm hiện có thành lô: ngày nhận, hạn dùng, số điểm còn lại. Loại biến động: tích từ đơn, thưởng, đổi quà, hết hạn, thu hồi khi hủy hoặc hoàn, điều chỉnh của quản trị. Đổi quà tiêu hao lô cũ nhất trước.
- Tích điểm trên số tiền khách thực trả của đơn vé và combo; đơn thẻ quà, gói thành viên, thuê phòng không tích.
- Thu hồi theo tỷ lệ phần hoàn khi hủy; nếu đã tiêu mất thì số dư có thể âm và trừ vào lần tích tiếp theo.
- Tác vụ nền xử lý điểm hết hạn và gửi nhắc khách có điểm sắp hết hạn.
- API điều chỉnh điểm của quản trị có ghi nhật ký.

Tiêu chí hoàn thành: điểm hết hạn đúng thứ tự lô cũ trước; hủy đơn thu hồi điểm, số dư âm được trừ ở lần tích sau; đổi quà không đổi hạng.
```

---

### T13.2 — Danh mục quà và đổi điểm

**Phụ thuộc:** T13.1, T3.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H13.3.

- Entity quà: tên, số điểm cần, loại (vé miễn phí, combo miễn phí, mã giảm giá), số lượng còn, thời gian hiệu lực. API admin CRUD.
- API khách đổi quà: trừ điểm nguyên tử từ lô cũ nhất, trừ số lượng còn, rồi cấp mã khuyến mãi cá nhân dùng một lần gắn tài khoản, có hạn dùng. Mã hết hạn không dùng thì không hoàn điểm; trả thông tin này khi đổi.
- Ghi nhật ký tạo và sửa quà.

Tiêu chí hoàn thành: đổi quà trừ đúng điểm và cấp đúng mã cá nhân; hết số lượng thì không đổi được.
```

---

### T13.3 — Thử thách theo tháng

**Phụ thuộc:** T13.1, T1.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.1 (định nghĩa Đã xem) và H13.4.

- Entity thử thách: tên, kỳ áp dụng, điều kiện (ví dụ xem 3 phim khác nhau trong tháng), phần thưởng (điểm hoặc mã quà). API admin CRUD. Tiến độ của khách.
- Handler nền cho TicketCheckedIn cập nhật tiến độ; chỉ tính vé đã check-in, không tính suất riêng; idempotent.
- Hoàn thành thì cấp thưởng tự động và gửi thông báo; mỗi khách chỉ nhận một lần cho mỗi thử thách.

Tiêu chí hoàn thành: đặt vé nhưng không đến xem không tính vào tiến độ; thưởng cấp đúng một lần.
```

---

### T13.4 — Giới thiệu bạn bè

**Phụ thuộc:** T13.1, T1.5, T3.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H13.5.

- Người mới nhập mã giới thiệu khi đăng ký; lưu quan hệ giới thiệu.
- Thưởng cho cả hai bên khi đơn đầu tiên của người được giới thiệu được xác nhận (handler nền cho OrderConfirmed, idempotent), không phải lúc đăng ký.
- Chống lạm dụng: mỗi người chỉ được giới thiệu một lần, không tự giới thiệu, trùng số điện thoại hoặc email với tài khoản đã có không được tính, trần thưởng giới thiệu mỗi tháng cho mỗi người (cấu hình); chừa điểm gọi kiểm tra thiết bị của T18.1.
- API danh sách người đã giới thiệu.

Tiêu chí hoàn thành: thưởng chỉ cấp một lần sau đơn đầu xác nhận; tự giới thiệu và trùng thông tin bị chặn.
```

---

### T13.5 — Frontend khách: điểm, quà, thử thách, giới thiệu

**Phụ thuộc:** T13.2, T13.3, T13.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc H13.6.

Trang khách: số dư điểm và hạng, lịch sử điểm theo lô, điểm sắp hết hạn, danh mục quà và đổi quà (nói rõ mã hết hạn không hoàn điểm), thử thách và tiến độ, mã giới thiệu có nút sao chép và danh sách người đã giới thiệu. Ô nhập mã giới thiệu ở form đăng ký.

Tiêu chí hoàn thành: khách đổi quà, theo dõi thử thách, chia sẻ mã giới thiệu.
```

---

### T13.6 — Frontend admin và báo cáo loyalty

**Phụ thuộc:** T13.2, T13.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H13.6.

Trang admin: quản lý quà, quản lý thử thách, tra cứu khách và điều chỉnh điểm (nhập lý do, có nhật ký), báo cáo điểm đã phát, đã đổi, đã hết hạn theo kỳ (đăng ký vào khung báo cáo).

Tiêu chí hoàn thành: quản trị tạo quà và thử thách, điều chỉnh điểm có nhật ký, xem báo cáo điểm.
```

---

# HẠNG MỤC 14. Hỗ trợ khách hàng và hoàn tiền ngoại lệ có duyệt

---

### T14.1 — Yêu cầu hỗ trợ (backend)

**Phụ thuộc:** T1.8, T6.5, T3.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H14.1.

- Entity yêu cầu hỗ trợ gắn một đơn: loại (không nhận email vé, bị trừ tiền nhưng đơn chưa xác nhận, muốn hoàn ngoài chính sách, góp ý suất chiếu...), trạng thái (mới, đang xử lý, chờ khách phản hồi, đã giải quyết), người phụ trách, hạn xử lý, lịch sử trao đổi. Khách xem tiến độ và trả lời trên yêu cầu; nhân viên và quản trị xử lý.
- Tự tạo yêu cầu khi đối soát (T6.5) phát hiện giao dịch đã thu mà đơn chưa xác nhận, và khi thu thừa không tự hoàn được (T1.2).
- Trong yêu cầu, nhân viên cấp mã quà xin lỗi (mã khuyến mãi cá nhân) hoặc gửi lại email vé.
- Email thông báo khi có cập nhật.
- Cài đặt hạn xử lý mặc định.

Tiêu chí hoàn thành: giao dịch bị trừ tiền nhưng đơn chưa xác nhận tự sinh yêu cầu; khách theo dõi và trả lời được.
```

---

### T14.2 — Hoàn tiền ngoại lệ có duyệt

**Phụ thuộc:** T14.1, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.6, 3.11 và H14.2.

- Entity đề xuất hoàn ngoại lệ: đơn, hoàn từng vé hoặc một phần số tiền, lý do, số tiền, người đề xuất, trạng thái, người duyệt.
- Nhân viên quầy và quản trị được đề xuất; vượt ngưỡng cấu hình thì cần quản trị duyệt, dưới ngưỡng quản trị duyệt nhanh; người đề xuất không tự duyệt đề xuất của mình.
- Khi duyệt, hoàn qua sổ giao dịch theo 3.6. Hoàn ngoại lệ không tự thay đổi vé; nếu hoàn kèm hủy vé thì quản trị chọn hủy vé trong cùng thao tác để nhả ghế.
- Ghi nhật ký đề xuất, duyệt và lý do. Combo đã nhận được phép hoàn ở luồng này.

Tiêu chí hoàn thành: hoàn vượt ngưỡng không thực hiện được khi chưa duyệt; không tự duyệt đề xuất của mình.
```

---

### T14.3 — Frontend và báo cáo hỗ trợ

**Phụ thuộc:** T14.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H14.3.

- Khách: tạo yêu cầu từ chi tiết đơn, danh sách yêu cầu, trang theo dõi và trả lời.
- Nhân viên và quản trị: bảng yêu cầu theo trạng thái, gán người phụ trách, trao đổi, cấp mã quà xin lỗi, gửi lại email vé, đề xuất và duyệt hoàn ngoại lệ.
- Báo cáo: số yêu cầu theo loại, thời gian xử lý trung bình, tỷ lệ giải quyết trong hạn, tổng tiền hoàn ngoại lệ theo lý do và theo người đề xuất.

Tiêu chí hoàn thành: xử lý trọn một yêu cầu từ khách gửi đến hoàn ngoại lệ có duyệt.
```

---

# HẠNG MỤC 15. Gợi ý phim cá nhân hóa

---

### T15.1 — Bộ gợi ý phim (backend)

**Phụ thuộc:** T1.8, T1.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.1, 3.10 và H15.1.

- Tính điểm phù hợp giữa khách và từng phim đang chiếu từ: sở thích theo lịch sử đã xem (vé đã check-in) và đánh giá đã viết (thể loại, diễn viên, đạo diễn; phim xem gần đây và chấm cao có trọng số lớn hơn; vé đã mua chưa quét có trọng số thấp hơn); tương đồng giữa phim theo hành vi (người xem A thường xem B); độ phổ biến và đánh giá chung cho khách mới; thói quen đặt vé (khung giờ, định dạng phòng, mức giá) để xếp suất.
- Loại trừ: phim khách đã mua vé hoặc đã xem; chỉ phim đang chiếu còn suất thường trong những ngày tới; ưu tiên suất còn ghế; không gợi ý suất riêng; không gợi ý suất sự kiện cho khách chưa đủ điều kiện mua.
- Mỗi gợi ý kèm lý do ngắn.
- Tác vụ nền cập nhật dữ liệu gợi ý (dùng sự kiện TicketCheckedIn và đánh giá mới).
- API "Dành cho bạn"; khách tắt cá nhân hóa thì chỉ trả danh sách phổ biến chung.

Tiêu chí hoàn thành: khách mới thấy danh sách phổ biến; khách có lịch sử thấy phim cùng thể loại hoặc đạo diễn kèm lý do; phim đã xem không xuất hiện.
```

---

### T15.2 — Công cụ trợ lý AI và email giới thiệu phim

**Phụ thuộc:** T15.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.10 và H15.2.

- Thêm công cụ gợi ý phim cho trợ lý AI: khi khách hỏi "tối nay xem gì", trợ lý gọi công cụ, diễn đạt lại kết quả kèm suất chiếu cụ thể; công cụ chỉ thấy suất thường và sự kiện công khai.
- Email giới thiệu phim mới phù hợp qua tác vụ nền, chỉ gửi khách đã bật tiếp thị, có liên kết hủy đăng ký, không gửi lặp cùng phim.

Tiêu chí hoàn thành: trợ lý gợi ý đúng kèm suất; khách chưa bật tiếp thị không nhận email.
```

---

### T15.3 — Frontend "Dành cho bạn"

**Phụ thuộc:** T15.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H15.2 và H15.3.

- Mục "Dành cho bạn" ở trang chủ và trang phim, hiển thị lý do gợi ý, dùng MovieCard hiện có.
- Trang cá nhân có công tắc tắt gợi ý cá nhân hóa; khi tắt chỉ hiện danh sách phổ biến chung.

Tiêu chí hoàn thành: tắt cá nhân hóa thì không còn mục "Dành cho bạn" cá nhân.
```

---

# HẠNG MỤC 16. Đánh giá xác thực và khảo sát sau khi xem

---

### T16.1 — Mời đánh giá, thưởng điểm, khảo sát, chất lượng phòng (backend)

**Phụ thuộc:** T1.3, T13.1, T1.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.10 và H16.

- Giữ nguyên quy tắc đánh giá hiện có (chỉ khi có vé đã check-in, mỗi khách một đánh giá cho mỗi phim). Đánh giá gắn nhãn "đã xem" và tính vào điểm trung bình.
- Tác vụ nền gửi email mời đánh giá sau suất một đến ba giờ (cấu hình), chỉ cho khách chưa tắt; idempotent. Đánh giá hợp lệ được thưởng điểm nhỏ, mỗi phim chỉ thưởng một lần.
- Entity khảo sát nhanh: chấm một đến năm cho phòng, âm thanh, hình ảnh, vệ sinh, dịch vụ quầy; gắn với đơn và suất đã xem, mỗi suất một lần. API gửi và đọc.
- Báo cáo chất lượng theo phòng và theo thời gian; điểm một phòng thấp liên tục (ngưỡng cấu hình) thì cảnh báo quản trị.
- Đánh giá xác thực là tín hiệu cho gợi ý phim (T15.1) và độ nóng phim ở xếp lịch.

Tiêu chí hoàn thành: mời đánh giá gửi đúng thời điểm; thưởng điểm một lần mỗi phim; cảnh báo phòng xuất hiện khi điểm thấp liên tục.
```

---

### T16.2 — Frontend đánh giá, khảo sát, báo cáo chất lượng

**Phụ thuộc:** T16.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H16.

- Khách: hiển thị nhãn "đã xem" trên đánh giá, trang đánh giá mở từ email mời, form khảo sát nhanh sau suất.
- Admin: báo cáo chất lượng theo phòng và theo thời gian, hiển thị cảnh báo phòng điểm thấp.

Tiêu chí hoàn thành: khách gửi đánh giá và khảo sát từ liên kết email; quản trị thấy cảnh báo chất lượng phòng.
```

---

# HẠNG MỤC 17. Danh sách chờ và báo khi mở bán

---

### T17.1 — Danh sách chờ cho suất hết ghế (backend)

**Phụ thuộc:** T5.3, T1.6, T1.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H17.1 và H5 (khóa ghế).

- Entity đăng ký chờ: suất, khách, số ghế cần, yêu cầu ghế liền kề, thứ tự, trạng thái. Mỗi khách chỉ chờ một lần cho mỗi suất, rút đăng ký bất kỳ lúc nào. Tự đóng khi suất bắt đầu hoặc bị hủy.
- Khi ghế được nhả (hết hạn giữ, đơn hủy), handler nền kiểm tra có đủ tổ hợp ghế hợp lệ theo quy tắc chống để trống ghế lẻ hay không; nếu có, chọn khách đứng đầu phù hợp và thông báo.
- Khách đó được ưu tiên giữ ghế trong thời gian cấu hình: các ghế được khóa dành riêng bằng SeatBlock loại người chờ (T5.3) có hạn, người khác không giữ được; hết thời gian thì mở cho người kế tiếp rồi cho khách bình thường.
- API đăng ký, rút, xem trạng thái.

Tiêu chí hoàn thành: người đứng đầu được thông báo và có ghế dành riêng; hết thời gian ưu tiên thì chuyển người kế; ghế dành riêng không bị người khác giữ.
```

---

### T17.2 — Báo khi mở bán và quan tâm phim

**Phụ thuộc:** T1.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H17.2 và 3.10.

- Entity quan tâm phim: khách đánh dấu quan tâm một phim sắp chiếu, bỏ quan tâm được.
- Khi phim có suất chiếu đầu tiên mở bán, gửi thông báo cho người quan tâm (thông báo theo yêu cầu của khách, không cần bật tiếp thị), idempotent.
- API quản trị xem số người quan tâm theo từng phim; chừa API danh sách người quan tâm cho mở bán sớm (T23.1).

Tiêu chí hoàn thành: suất đầu tiên mở bán thì người quan tâm nhận thông báo đúng một lần.
```

---

### T17.3 — Frontend và báo cáo danh sách chờ

**Phụ thuộc:** T17.1, T17.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H17.

- Khách: nút "Đăng ký chờ" ở suất hết ghế (chọn số ghế, ghế liền kề), trạng thái chờ, thông báo có ghế với đồng hồ đếm ngược ưu tiên và nút giữ ghế, nút rút đăng ký; nút "Quan tâm" ở phim sắp chiếu.
- Admin: báo cáo số lượt chờ theo suất, tỷ lệ người chờ được ghế rồi mua, số người quan tâm theo phim.

Tiêu chí hoàn thành: khách đăng ký chờ, nhận thông báo và giữ được ghế trong thời gian ưu tiên.
```

---

# HẠNG MỤC 18. Chống giữ ghế ảo và hạn chế lạm dụng

---

### T18.1 — Hạn chế và giới hạn (backend)

**Phụ thuộc:** T1.6, T3.3, T13.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc H18 và 3.1 (khách vãng lai).

- Đếm số đơn hết hạn liên tiếp của từng khách; vượt ngưỡng cấu hình thì rút ngắn thời gian giữ ghế hoặc giới hạn số đơn chờ cùng lúc trong khoảng thời gian cấu hình.
- Giới hạn số ghế giữ đồng thời và số đơn chờ của mỗi tài khoản. Biểu mẫu công khai không cần tài khoản (yêu cầu thuê phòng, liên hệ) giới hạn tần suất theo địa chỉ mạng và thiết bị.
- Dấu hiệu bất thường (nhiều lần giữ rồi bỏ trong thời gian ngắn, nhiều lần thanh toán thất bại) thì yêu cầu xác minh bổ sung trước khi tiếp tục.
- Chống lạm dụng ưu đãi: nhiều tài khoản chung số điện thoại hoặc thiết bị không cùng hưởng ưu đãi khách mới hoặc thưởng giới thiệu (nối vào T3.1 và T13.4).
- Entity hạn chế khách: lý do, thời hạn, trạng thái. API admin xem danh sách và gỡ hạn chế; ghi nhật ký. Khách bị hạn chế nhận lỗi nêu rõ lý do và thời hạn.

Tiêu chí hoàn thành: bỏ liên tiếp quá ngưỡng bị rút ngắn thời gian giữ; quản trị gỡ được hạn chế; hai tài khoản chung số điện thoại không cùng nhận thưởng giới thiệu.
```

---

### T18.2 — Frontend hạn chế khách

**Phụ thuộc:** T18.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H18.

- Admin: danh sách khách đang bị hạn chế với lý do và thời hạn, nút gỡ hạn chế kèm xác nhận.
- Khách: banner hoặc hộp thoại nêu rõ lý do và thời hạn khi bị hạn chế, bước xác minh bổ sung khi hệ thống yêu cầu.

Tiêu chí hoàn thành: quản trị gỡ hạn chế; khách thấy thông báo hạn chế rõ ràng.
```

---

# HẠNG MỤC 19. Thuê phòng và bán sỉ

---

### T19.1 — Yêu cầu thuê phòng và trang theo dõi công khai (backend)

**Phụ thuộc:** T1.1, T18.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.1 (khách vãng lai) và H19.1.

- Entity yêu cầu thuê phòng: họ tên, đơn vị, loại đối tượng (doanh nghiệp, trường học, nhóm bạn), liên hệ, ngày giờ mong muốn, khoảng dự phòng, số người, phim mong muốn hoặc tự mang nội dung, định dạng phòng, combo, yêu cầu khác, trạng thái (mới, đang xử lý, đã báo giá, khách chấp nhận, khách từ chối, đã đặt cọc, đã thanh toán đủ, hoàn tất, hủy, hết hạn báo giá).
- Endpoint công khai gửi yêu cầu không cần tài khoản, có giới hạn tần suất theo địa chỉ mạng và thiết bị (T18.1).
- Đường dẫn theo dõi riêng gửi qua email: token khó đoán, có hạn, giới hạn số lần thử.
- API admin danh sách theo trạng thái và đổi trạng thái; ghi nhật ký.

Tiêu chí hoàn thành: khách không tài khoản gửi được yêu cầu và vào được trang theo dõi qua link riêng; gửi dồn dập bị chặn.
```

---

### T19.2 — Báo giá thuê phòng

**Phụ thuộc:** T19.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.2 (ngoại lệ) và H19.2.

- Bảng giá thuê theo phòng, khung giờ, thời lượng (admin CRUD, cache); báo giá = giá thuê + phí theo đầu người nếu có + gói combo + khoản phụ − chiết khấu của quản trị.
- Báo giá có hạn hiệu lực, ghi rõ tiền đặt cọc (phần trăm cấu hình), hạn thanh toán nốt, chính sách hủy. Mỗi lần sửa tạo phiên bản mới giữ lịch sử, ghi nhật ký.
- Xuất báo giá PDF và gửi email cho khách.
- Khách xem và bấm chấp nhận hoặc từ chối trên trang theo dõi; quá hạn thì chuyển hết hạn báo giá (tác vụ nền).

Tiêu chí hoàn thành: soạn, gửi và PDF báo giá được; khách chấp nhận hoặc từ chối qua link; quá hạn tự chuyển trạng thái.
```

---

### T19.3 — Suất riêng, thanh toán nhiều đợt và hủy

**Phụ thuộc:** T19.2, T5.1, T5.3, T10.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.4 và H19.3, H19.4.

- Khi khách chấp nhận và đặt cọc, tạo suất chiếu riêng trong phòng đã chốt: kiểm tra xung đột với suất khác, thời gian đệm và khoảng đóng phòng (T5.1). Suất riêng cho phép gắn nội dung khách mang, không bắt buộc có phim trong danh mục. Khóa toàn bộ ghế cho đơn thuê phòng bằng SeatBlock (T5.3).
- Suất riêng không hiện trong lịch chiếu công khai, trợ lý AI, gợi ý phim, thống kê lấp đầy, độ nóng; có tính vào doanh thu và có báo cáo riêng. Không áp quy tắc giá, ưu đãi hạng, khuyến mãi, tích điểm.
- Thanh toán nhiều đợt: cọc rồi thanh toán nốt trước hạn; quản trị ghi nhận chuyển khoản, khách trả qua cổng hoặc thẻ quà. Tiền cọc ghi nhận doanh thu vào ngày sự kiện diễn ra.
- Chính sách hủy riêng theo mốc thời gian cấu hình: hủy sớm hoàn toàn bộ hoặc một phần cọc, hủy muộn mất cọc. Quá hạn thanh toán nốt thì hủy yêu cầu, xử lý cọc theo chính sách hủy muộn và giải phóng suất. Rạp hủy vì sự cố thì hoàn đầy đủ.

Tiêu chí hoàn thành: từ yêu cầu đi trọn đến suất riêng đã đặt cọc, suất không lộ công khai và ghế khóa kín; xung đột phòng bị chặn; quá hạn thanh toán nốt thì suất được giải phóng.
```

---

### T19.4 — Frontend khách: biểu mẫu và theo dõi thuê phòng

**Phụ thuộc:** T19.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H19.5.

- Trang công khai biểu mẫu gửi yêu cầu thuê phòng (không cần đăng nhập), xử lý lỗi giới hạn tần suất.
- Trang theo dõi yêu cầu qua link riêng: trạng thái, xem báo giá các phiên bản, tải PDF, chấp nhận hoặc từ chối, thanh toán cọc và thanh toán nốt.

Tiêu chí hoàn thành: khách gửi yêu cầu, xem báo giá và chấp nhận trên điện thoại.
```

---

### T19.5 — Frontend admin và báo cáo thuê phòng

**Phụ thuộc:** T19.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H19.5.

- Bảng theo dõi yêu cầu theo trạng thái (dạng bảng hoặc cột kanban), màn soạn báo giá với bảng giá thuê, lịch phòng hiển thị các suất riêng, ghi nhận thanh toán chuyển khoản, quản lý bảng giá thuê.
- Báo cáo số yêu cầu, tỷ lệ chốt, doanh thu thuê phòng.

Tiêu chí hoàn thành: quản trị xử lý trọn một yêu cầu từ mới đến hoàn tất.
```

---

# HẠNG MỤC 20. Gói sản phẩm (bundle)

---

### T20.1 — Dữ liệu và CRUD gói sản phẩm

**Phụ thuộc:** T2.2, T3.2, T4.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H20.

- Entity gói: thành phần (số vé theo từng loại vé, các combo), giá gói thấp hơn tổng giá lẻ, điều kiện (suất, phim, ngày, khung giờ, loại ghế được phép), hạn mức số gói mỗi ngày, thời gian hiệu lực, cờ không dùng chung với mã khuyến mãi.
- API admin CRUD, validate giá gói thấp hơn tổng lẻ, ghi nhật ký, cache.
- IResourceHold cho hạn mức số gói mỗi ngày: giữ chỗ khi giữ ghế, nhả cùng ghế, chốt khi xác nhận.
- API khách danh sách gói áp dụng được cho một suất kèm mức tiết kiệm so với mua lẻ.

Tiêu chí hoàn thành: hết hạn mức ngày thì gói ngừng bán; hạn mức không bị vượt khi nhiều khách cùng giữ.
```

---

### T20.2 — Mua gói, tách giá về dòng hàng, hoàn từng thành phần

**Phụ thuộc:** T20.1, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.2 (bước 3), 3.3 và H20.

- Khách mua gói: chọn ghế như đặt vé thường; hệ thống tách giá gói về từng dòng hàng theo tỷ lệ giá lẻ niêm yết tại thời điểm mua (dùng DiscountAllocator, dòng cuối nhận phần dư), ghi OrderPriceAdjustment loại giá gói.
- Ưu đãi hạng và mã khuyến mãi áp trên giá đã tách; cờ không dùng chung của gói chặn mã nếu quản trị chọn.
- Hoàn một thành phần của gói tính theo giá đã tách, khách chỉ được hoàn phần tương ứng. Trừ kho combo và chia nhà phát hành dùng giá đã tách.
- Báo cáo: số gói bán, doanh thu gói, so sánh với trường hợp mua lẻ, tỷ lệ chuyển đổi (đăng ký vào khung báo cáo).

Tiêu chí hoàn thành: tổng các dòng đã tách bằng đúng giá gói; hoàn một vé của gói hoàn đúng giá đã tách.
```

---

### T20.3 — Frontend gói sản phẩm

**Phụ thuộc:** T20.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H20.

- Khách: hiển thị gói và mức tiết kiệm ở trang chọn suất, chọn gói rồi chọn ghế theo số vé của gói, chi tiết đơn thể hiện từng thành phần với giá đã tách.
- Quầy: bán gói.
- Admin: quản lý gói (form thành phần, giá, điều kiện, hạn mức), báo cáo gói.

Tiêu chí hoàn thành: khách mua gói trọn luồng; admin tạo gói và xem báo cáo.
```

---

# HẠNG MỤC 21. Gói thành viên trả phí

---

### T21.1 — Gói thành viên, mua gói và quyền lợi trong bộ tính giá

**Phụ thuộc:** T3.2, T13.1, T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.2 (bước 5 và ngoại lệ) và H21.

- Entity gói thành viên (theo tháng hoặc quý): giá, thời hạn, quyền lợi (giảm phần trăm cho vé, số vé miễn phí mỗi tháng, giảm giá combo, quyền đặt sớm cho suất đặc biệt), cờ dùng chung với mã. API admin CRUD, ghi nhật ký.
- Khách mua gói như một đơn thường (không ưu đãi hạng, không mã, không tích điểm); entity đăng ký của khách có hiệu lực từ đến. Hết hạn thì nhắc gia hạn bằng email; gia hạn do khách chủ động thanh toán, không tự trừ tiền.
- Điền chỗ trống ở bộ tính giá: quyền lợi gói áp sau ưu đãi hạng và trước mã khuyến mãi; quy tắc chồng ưu đãi quyết định gói có dùng chung với mã không. Phân bổ về dòng hàng qua DiscountAllocator.

Tiêu chí hoàn thành: quyền lợi giảm giá áp đúng thứ tự; hết hạn gói thì mất quyền lợi.
```

---

### T21.2 — Vé miễn phí của gói thành viên

**Phụ thuộc:** T21.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.5 và H21.

- Sổ vé miễn phí riêng: cấp theo tháng, không cộng dồn sang tháng sau, ghi rõ vé nào dùng vào đơn nào.
- IResourceHold cho vé miễn phí: giữ chỗ khi khách dùng cho đơn chờ, nhả khi hết hạn hoặc gỡ, chốt khi xác nhận.
- Giới hạn số vé miễn phí trên mỗi suất (cấu hình) để tránh chia sẻ tài khoản. Không dùng cho suất sự kiện trừ khi sự kiện cho phép.
- API xem số vé miễn phí còn lại và áp vào đơn.

Tiêu chí hoàn thành: vé miễn phí tháng này không sang tháng sau; bị chặn khi quá giới hạn mỗi suất.
```

---

### T21.3 — Ghi nhận doanh thu, quy đổi vé miễn phí, hủy và hoàn

**Phụ thuộc:** T21.2, T7.2, T6.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.4, H21 và mục 7 (quyết định số 5).

- Doanh thu gói ghi nhận chia đều theo từng tháng hiệu lực (bút toán ghi nhận hằng tháng, báo cáo phân biệt doanh thu đã ghi nhận và chưa ghi nhận).
- Mỗi gói khai báo giá trị quy đổi của một vé miễn phí; khi vé miễn phí được dùng, chuyển phần giá trị quy đổi từ doanh thu gói sang doanh thu vé của suất (ghi theo ngày chiếu) và để dòng vé tham gia chia nhà phát hành (cộng vào doanh thu vé để chia ở T7.2). Phần còn lại giữ là doanh thu gói.
- Hủy và hoàn: mặc định không hoàn gói đã bắt đầu hiệu lực, trừ lỗi hệ thống hoặc quản trị cho ngoại lệ; chính sách cấu hình.
- Cập nhật bảng tổng hợp và báo cáo chốt ngày.

Tiêu chí hoàn thành: doanh thu gói chia đều theo tháng; vé miễn phí chuyển đúng giá trị quy đổi sang doanh thu vé và vào đối soát nhà phát hành.
```

---

### T21.4 — Frontend khách: gói thành viên

**Phụ thuộc:** T21.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H21.

- Trang danh sách gói thành viên và mua gói; trang gói của tôi: thời hạn, quyền lợi, số vé miễn phí còn lại, nút gia hạn, nhắc sắp hết hạn.
- Bước chọn ghế và thanh toán: áp quyền lợi gói, chọn dùng vé miễn phí, hiển thị rõ khoản giảm từ gói.

Tiêu chí hoàn thành: khách mua gói, dùng vé miễn phí và thấy quyền lợi áp trong đơn.
```

---

### T21.5 — Frontend admin và báo cáo gói thành viên

**Phụ thuộc:** T21.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H21.

- Admin: quản lý gói thành viên (form quyền lợi, giá trị quy đổi vé miễn phí, chính sách hoàn), danh sách hội viên, hoàn ngoại lệ gói.
- Báo cáo: số hội viên đang hoạt động, tỷ lệ gia hạn, giá trị quyền lợi đã dùng so với tiền gói, lợi nhuận so với bán vé lẻ, doanh thu gói đã ghi nhận và chưa ghi nhận (đăng ký vào khung báo cáo).

Tiêu chí hoàn thành: quản trị tạo gói và xem đủ chỉ số báo cáo.
```

---

# HẠNG MỤC 22. Phân khúc khách hàng và chiến dịch

---

### T22.1 — Phân khúc khách hàng

**Phụ thuộc:** T13.1, T1.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.10 và H22.1.

- Phân nhóm khách tự động bằng tác vụ nền định kỳ: hoạt động (khách mới, thường xuyên, lâu chưa quay lại theo số ngày cấu hình); giá trị (chi tiêu cao, tần suất cao chấm theo gần đây, tần suất, chi tiêu, giá trị vòng đời ước tính); sở thích (thể loại, định dạng phòng); tình trạng điểm (sắp hết hạn, sắp lên hạng).
- Phân khúc tùy chỉnh do quản trị tạo bằng điều kiện kết hợp. Mỗi phân khúc trả số khách và số khách đủ điều kiện nhận thông tin.
- API admin CRUD phân khúc, xem danh sách khách trong phân khúc (che dữ liệu cá nhân khi không cần).

Tiêu chí hoàn thành: phân khúc cập nhật định kỳ; số khách đủ điều kiện nhận thông tin đúng.
```

---

### T22.2 — Chiến dịch và đo kết quả

**Phụ thuộc:** T22.1, T3.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.10 và H22.2.

- Entity chiến dịch: phân khúc, nội dung email hoặc thông báo, mã khuyến mãi cá nhân kèm theo (mỗi khách một mã), thời điểm hẹn gửi, trạng thái.
- Chỉ gửi khách đã bật tiếp thị; mỗi email có liên kết hủy đăng ký; thông báo trong ứng dụng không cần email. Mỗi khách nhận tối đa một chiến dịch trong khoảng thời gian cấu hình. Gửi thử cho quản trị trước khi gửi thật.
- Gửi bằng tác vụ nền, idempotent, có thử lại.
- Đo kết quả: số email gửi, số khách mở (theo dõi mở email), số khách dùng mã, doanh thu phát sinh từ chiến dịch.

Tiêu chí hoàn thành: chiến dịch chỉ đến khách đã đồng ý; mỗi khách một mã riêng; kết quả ghi đúng số dùng mã và doanh thu.
```

---

### T22.3 — Báo cáo giữ chân khách

**Phụ thuộc:** T22.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc H22.3.

API báo cáo giữ chân theo nhóm khách đăng ký cùng tháng: tỷ lệ phần trăm quay lại đặt vé ở tháng thứ nhất, thứ hai, thứ ba. Đọc từ dữ liệu đã tổng hợp hoặc bảng tổng hợp mới cập nhật theo ngày, không quét bảng giao dịch trực tiếp.

Tiêu chí hoàn thành: số liệu giữ chân đúng với dữ liệu mẫu; tải nhanh.
```

---

### T22.4 — Frontend phân khúc, chiến dịch, giữ chân

**Phụ thuộc:** T22.2, T22.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc H22.

Trang admin: danh sách phân khúc với số khách và số khách đủ điều kiện nhận thông tin, form tạo phân khúc tùy chỉnh bằng điều kiện kết hợp, soạn chiến dịch (chọn phân khúc, nội dung, kèm mã cá nhân, hẹn giờ, gửi thử), danh sách chiến dịch và kết quả, bảng giữ chân theo nhóm tháng đăng ký.

Tiêu chí hoàn thành: quản trị tạo phân khúc, gửi thử và hẹn chiến dịch, xem kết quả.
```

---

# HẠNG MỤC 23. Suất chiếu sự kiện

---

### T23.1 — Suất sự kiện: loại suất, hạn mức, mở bán sớm, giá riêng

**Phụ thuộc:** T2.2, T3.1, T4.2, T17.2 (mở bán sớm theo hội viên gói cần T21.1).

```text
Áp dụng QUY TẮC CHUNG. Đọc H23 và H2.2 (suất sự kiện).

- Gắn loại "sự kiện" cho suất; có thể có phụ thu cố định hoặc quy tắc giá riêng (nối vào IPricingService: suất sự kiện chỉ áp quy tắc riêng này).
- Hạn mức vé theo loại (ví dụ số vé dành cho hội viên) và hạn mức số vé mỗi khách, kiểm tra nguyên tử khi giữ ghế.
- Mở bán sớm cho nhóm được chọn: hạng thành viên, hội viên gói trả phí (chỉ bật khi đã có T21.1), khách đã bấm quan tâm phim (T17.2). Đến giờ mở bán chung mới mở cho mọi khách; hiển thị đúng ở lịch chiếu, sơ đồ ghế và API giữ ghế.
- Cờ không áp dụng một số khuyến mãi cho suất sự kiện (điều kiện loại suất ở T3.1). Vé miễn phí của gói chỉ dùng nếu sự kiện cho phép.
- Loại suất sự kiện khỏi gợi ý lịch chiếu và độ nóng (H6.3), vẫn tính doanh thu và đối soát nhà phát hành.

Tiêu chí hoàn thành: mở bán sớm chỉ đúng nhóm được chọn; hạn mức mỗi khách bị chặn khi vượt.
```

---

### T23.2 — Quà tặng kèm vé

**Phụ thuộc:** T23.1, T4.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc 3.5 và H23.

- Khai báo quà tặng kèm của suất sự kiện (poster, ly, móc khóa) với số lượng, dùng lại cơ chế kho ở T4.1.
- IResourceHold cho quà: giữ chỗ theo vé khi giữ ghế, chốt khi xác nhận, nhả khi hết hạn. Vé hủy thì quà chưa trao được hoàn lại.
- Quầy đánh dấu đã trao quà cho khách (quét QR vé), lưu nhân viên và thời điểm.
- Cảnh báo sắp hết quà qua cơ chế cảnh báo kho.

Tiêu chí hoàn thành: quà không phát vượt số lượng; hủy vé hoàn quà chưa trao.
```

---

### T23.3 — Frontend và báo cáo suất sự kiện

**Phụ thuộc:** T23.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc H23.

- Admin: tạo suất sự kiện (phụ thu, hạn mức, nhóm mở bán sớm và giờ mở bán chung, quà tặng kèm), báo cáo riêng từng sự kiện (số vé, doanh thu, lấp đầy, số quà đã trao).
- Khách: hiển thị nhãn sự kiện, điều kiện, đếm ngược mở bán chung, thông báo khi chưa đủ điều kiện mua.
- Quầy: màn trao quà.

Tiêu chí hoàn thành: báo cáo sự kiện đủ bốn chỉ số; khách ngoài nhóm không mua được trước giờ mở bán chung.
```

---

## D. Thứ tự làm đề nghị

1. T1.1 → T1.11 (hoàn thành xong mới sang bước sau).
2. T2.1 → T2.5, T3.1 → T3.5, T4.1 → T4.5, T5.1 → T5.4.
3. T6.1 → T6.7 (mốc có giá trị kinh doanh rõ nhất).
4. T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23 theo thứ tự lộ trình ở phần 5 của tài liệu; từ T7 trở đi có thể đổi thứ tự miễn phụ thuộc đã xong.

## E. Điểm cần bạn xác nhận trước khi giao

- Mục 7 của tài liệu liệt kê 14 quyết định đã tự chọn; các prompt đã bám theo đúng các quyết định đó.
- T2.2: cách kết hợp nhiều quy tắc giá (cộng dồn hay ưu tiên cao nhất) để AI tự chọn và báo lại; nên chốt trước nếu muốn cố định.
- T21.3 và T7.2: giá trị quy đổi vé miễn phí cần đối chiếu với hợp đồng thực tế với nhà phát hành.
