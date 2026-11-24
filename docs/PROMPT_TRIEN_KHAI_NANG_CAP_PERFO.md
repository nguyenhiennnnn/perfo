# Bộ prompt triển khai nâng cấp business Perfo Cinema

Nguồn nghiệp vụ: `NANG_CAP_BUSINESS_PERFO.md` (23 hạng mục). Mỗi hạng mục được chia thành các task nhỏ, mỗi task có một prompt độc lập để giao cho AI code (hạng mục 7 trở đi chia nhỏ hơn để vừa một phiên claude.ai).

## Cách dùng

1. Đính kèm `NANG_CAP_BUSINESS_PERFO.md` và mã nguồn cho AI.
2. Dán khối **Quy tắc chung** (mục A) trước, rồi dán prompt của task cần làm.
3. Làm tuần tự theo thứ tự trong tài liệu. Task chỉ phụ thuộc task có số nhỏ hơn (cột "Phụ thuộc" ghi rõ).
4. Sau mỗi task, tự kiểm tra thủ công theo dòng "Tiêu chí hoàn thành" của task đó.
5. Từ T1.1 đến T1.8 hệ thống có thể tạm chưa chạy trọn vẹn vì đang thay mô hình đơn hàng; chạy kiểm tra thủ công sau T1.10.
6. Từ hạng mục 7, mỗi task cũ đã được tách thành các task con nhỏ hơn (T7.1a, T7.1b, ...) để vừa một phiên làm việc. Làm tuần tự theo chữ cái a → b → c. Khi task cũ hơn nhắc "T21.1", "T19.3"... thì hiểu là cả nhóm task con (hoặc task con có chữ cái được ghi ở task mới). Mỗi task ghi sẵn số dòng cần đọc ở dòng "Đọc dòng ..."; bản đồ dòng nằm ở mục A2.
7. T6.1 đã làm xong, không giao lại.

---

## A. Quy tắc chung (dán trước mọi prompt)

```text
QUY TẮC CHUNG CHO MỌI TASK

- Nguồn nghiệp vụ là docs/NANG_CAP_BUSINESS_PERFO.md. Chỉ đọc đúng các dòng mà task ghi ở "Đọc" (bản đồ dòng ở mục A2 của prompt này; không cần mở lại). Nếu prompt lệch tài liệu thì theo tài liệu và nêu chỗ lệch ở cuối câu trả lời.
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

TIẾT KIỆM TOKEN
- CLAUDE.md đã có quy ước; không đọc lại docs/PROMPT_TRIEN_KHAI_NANG_CAP_PERFO.md. Chỉ đọc đúng dòng của docs/NANG_CAP_BUSINESS_PERFO.md mà task ghi ("Đọc").
- Grep/Glob rồi đọc đúng file cần. Không đọc: Migrations, bin, obj, node_modules, tests, e2e, loadtest, các Messages.resx ngoài việc thêm key ở cuối file (dùng grep -n để xác nhận key chưa có).
- Sửa file bằng chỉnh sửa cục bộ; không viết lại cả file. Không in lại code đã đọc.
- T6.1 đã xong, tái sử dụng, không viết lại: bảng ShowtimeDailyStat (ShowtimeId, BusinessDate, Kind, SellableSeats, TicketsSold, GrossTicketRevenue, DiscountAmount, RefundAmount, ComboRevenue, CancelledOrders), PaymentDailyStat (BusinessDate, Channel, Method, TransactionCount, CollectedAmount, RefundedAmount, GatewayFee), ShowtimeSalesPace; Services/SalesStats/SalesStatsService.cs; Utils/BusinessDay.cs (ngày kinh doanh).
- Lưu ý: Showtime chưa có trường loại suất; ShowtimeDailyStat.Kind hiện luôn là Regular. Vẫn lọc theo Kind để T19/T23 sau này đúng.
- Câu trả lời cuối tối đa 15 dòng: file đã đổi (chỉ tên), việc thủ công, chỗ lệch tài liệu, test có thể bị ảnh hưởng. Không tóm tắt lại code.
```

## A2. Bản đồ dòng của docs/NANG_CAP_BUSINESS_PERFO.md

Tài liệu nghiệp vụ đánh số hạng mục và mục khác với cách các prompt gọi (H1.1, mục 3.x ở T1 đến T6 là cách gọi cũ). Dùng bảng này để đọc đúng dòng, không cần đọc cả file. Với T1 đến T6, "H{n}.x" nghĩa là hạng mục n ở bảng dưới; "mục 3.x" nghĩa là đoạn liên quan trong dòng 36-80.

| Hạng mục (theo prompt) | Dòng cần đọc |
|---|---|
| Quy ước chung | 36-80 (giá 38-48, ghi nhận doanh thu 49-56, khóa giá và giữ chỗ 57-61, sự kiện 62-70, nhật ký 71-74, bộ nhớ đệm 75-80); thay đổi cốt lõi 20-35 |
| 1. Nền tảng đơn hàng | 81-128 |
| 2. Loại vé và quy tắc giá | 129-167 |
| 3. Khuyến mãi | 168-211 |
| 4. Kho bắp nước | 212-243 |
| 5. Đóng phòng, khóa ghế | 586-599 |
| 6. Báo cáo, đối soát cổng | 244-302 |
| 7. Nhà phát hành và hợp đồng | 561-573 |
| 8. Chốt ngày và ca quầy | 634-644 |
| 9. Hóa đơn VAT | 600-611 |
| 10. Thẻ quà tặng | 344-381 |
| 11. Đổi suất chiếu | 452-467 |
| 12. Combo đặt trước, nhắc lịch | 485-495 |
| 13. Loyalty | 303-343 |
| 14. Hỗ trợ khách hàng | 612-633 |
| 15. Gợi ý phim | 417-445 |
| 16. Đánh giá, khảo sát | 548-560 |
| 17. Danh sách chờ, báo mở bán | 468-484 |
| 18. Chống giữ ghế ảo | 574-585 |
| 19. Thuê phòng, bán sỉ | 382-416 |
| 20. Gói sản phẩm | 496-507 |
| 21. Gói thành viên | 508-520 |
| 22. Phân khúc, chiến dịch | 521-547 |
| 23. Suất sự kiện | 645-657 |

Nếu số dòng lệch do tài liệu được sửa, tìm lại bằng grep tiêu đề hạng mục rồi đọc đúng đoạn đó.

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
| 7 | Nhà phát hành và hợp đồng phim | T7.1a – T7.3c | 9 |
| 8 | Chốt doanh thu cuối ngày và chốt ca quầy | T8.1a – T8.3c | 10 |
| 9 | Hóa đơn VAT theo yêu cầu | T9.1a – T9.2b | 6 |
| 10 | Thẻ quà tặng | T10.1a – T10.5b | 12 |
| 11 | Đổi suất chiếu | T11.1a – T11.3b | 7 |
| 12 | Combo đặt trước và nhắc lịch chiếu | T12.1a – T12.3b | 7 |
| 13 | Loyalty mạnh hơn | T13.1a – T13.6b | 14 |
| 14 | Hỗ trợ khách hàng và hoàn tiền ngoại lệ | T14.1a – T14.3c | 8 |
| 15 | Gợi ý phim cá nhân hóa | T15.1a – T15.3 | 6 |
| 16 | Đánh giá xác thực và khảo sát | T16.1a – T16.2b | 5 |
| 17 | Danh sách chờ và báo khi mở bán | T17.1a – T17.3b | 7 |
| 18 | Chống giữ ghế ảo và hạn chế lạm dụng | T18.1a – T18.2 | 4 |
| 19 | Thuê phòng và bán sỉ | T19.1a – T19.5b | 13 |
| 20 | Gói sản phẩm | T20.1a – T20.3b | 7 |
| 21 | Gói thành viên trả phí | T21.1a – T21.5b | 12 |
| 22 | Phân khúc khách hàng và chiến dịch | T22.1a – T22.4b | 8 |
| 23 | Suất chiếu sự kiện | T23.1a – T23.3b | 8 |
| | **Tổng** | | **180** |

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

### T7.1a — Nhà phát hành

**Phụ thuộc:** T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 561-573.

- Entity Distributor: tên, liên hệ, mã số thuế, trạng thái. Mỗi phim gắn một nhà phát hành (khóa ngoại nullable trên phim, cấu hình EF).
- API admin CRUD nhà phát hành (xóa mềm, không xóa khi còn phim gắn) và API gắn hoặc đổi nhà phát hành cho phim. Ghi nhật ký qua T1.4.
- Chưa làm hợp đồng.

Tiêu chí hoàn thành: tạo nhà phát hành, gắn vào phim, đổi nhà phát hành có dòng nhật ký.
```

---

### T7.1b — Hợp đồng phim

**Phụ thuộc:** T7.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 561-573.

- Entity FilmContract: phim, ngày khởi chiếu tại rạp, các tuần chiếu (mỗi tuần đủ 7 ngày tính từ ngày khởi chiếu) kèm tỷ lệ chia từng tuần, cách tính (trước hoặc sau thuế), mức bảo đảm tối thiểu (nullable), thời gian hiệu lực, mức phí cố định cho suất riêng (nullable).
- Validate: tuần liên tục và không chồng, tỷ lệ từ 0 đến 100, một phim không có hai hợp đồng hiệu lực chồng nhau.
- API admin CRUD, ghi nhật ký.
- Service trả "hợp đồng hiệu lực của phim tại ngày D" và "ngày D thuộc tuần chiếu thứ mấy" cho T7.2a dùng.
- Phim chưa có hợp đồng trả trạng thái "chưa có hợp đồng", không coi tỷ lệ bằng 0; API liệt kê phim chưa có hợp đồng.

Tiêu chí hoàn thành: khai được hợp đồng với tỷ lệ khác nhau ba tuần; hợp đồng chồng thời gian bị chặn; phim chưa có hợp đồng nhận diện được.
```

---

### T7.2a — Doanh thu vé để chia theo tuần chiếu

**Phụ thuộc:** T7.1b, T6.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 561-573 và 49-56.

- Service nhận (phim, khoảng ngày) và trả doanh thu vé để chia theo từng tuần chiếu của hợp đồng: số vé, doanh thu gộp, giảm giá, hoàn tiền, doanh thu thực.
- Nguồn là ShowtimeDailyStat của T6.1 (gộp trừ giảm giá trừ hoàn), không quét OrderLine. Lọc theo Kind: Regular và Event tính; Private không tính, trừ khi hợp đồng có phí cố định (khi đó dùng phí cố định thay vì chia). Chừa điểm cộng giá trị quy đổi vé miễn phí của gói thành viên (T21.3b).
- Ngày thuộc tuần nào theo ngày kinh doanh của suất (Utils/BusinessDay.cs), không theo ngày bán.
- Cách tính trước thuế cần tách thuế (T9.1a); nếu T9.1a chưa làm thì coi thuế suất bằng 0 và chừa điểm nối.

Tiêu chí hoàn thành: số liệu khớp bảng tổng hợp; suất qua nửa đêm vào đúng tuần; suất Private bị loại.
```

---

### T7.2b — Tính đối soát theo kỳ

**Phụ thuộc:** T7.2a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 561-573.

- Entity DistributorSettlement (nhà phát hành, kỳ từ-đến, trạng thái, tổng phải trả, thời điểm tạo) và dòng theo phim và tuần chiếu (doanh thu, tỷ lệ, phần chia, mức bảo đảm tối thiểu, số phải trả).
- Kỳ tuần hoặc tháng lấy từ cài đặt rạp. Với mỗi phim: số phải trả = lớn hơn giữa (doanh thu từng tuần × tỷ lệ tuần đó, cộng lại) và mức bảo đảm tối thiểu; trước hoặc sau thuế theo hợp đồng.
- API tạo đối soát nháp cho một kỳ (tính lại được khi còn nháp), danh sách, xem chi tiết.
- Phim chưa có hợp đồng nằm trong danh sách cảnh báo của đối soát, không có dòng chia.

Tiêu chí hoàn thành: ba tuần có tỷ lệ khác nhau cho số phải trả khớp tính tay; có mức bảo đảm thì lấy mức lớn hơn.
```

---

### T7.2c — Trạng thái, chốt kỳ và điều chỉnh kỳ sau

**Phụ thuộc:** T7.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 561-573.

- Trạng thái: nháp, đã gửi, đã chốt, đã thanh toán (ghi ngày thanh toán). Chỉ đi tới theo thứ tự, nháp và đã gửi được quay lại nháp bởi quản trị. Ghi nhật ký mỗi lần đổi.
- Đối soát đã chốt không tự đổi khi có hoàn tiền muộn: phần chênh lệch giữa số đã chốt và số tính lại được ghi thành dòng điều chỉnh (entity riêng, trỏ về kỳ gốc) vào đối soát kỳ kế tiếp.
- Chặn tạo hai đối soát chồng kỳ cho cùng nhà phát hành.

Tiêu chí hoàn thành: hoàn tiền muộn đi vào kỳ sau dưới dạng điều chỉnh; kỳ đã chốt không đổi số.
```

---

### T7.2d — Xuất PDF và Excel đối soát

**Phụ thuộc:** T7.2c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 561-573.

- Xuất PDF và Excel bảng đối soát bằng hạ tầng hiện có: thông tin nhà phát hành, kỳ, từng phim và tuần chiếu, điều chỉnh, tổng phải trả, trạng thái.
- API tải file; chỉ đối soát từ trạng thái đã gửi trở đi mới xuất bản chính thức.

Tiêu chí hoàn thành: file xuất khớp số trên API chi tiết.
```

---

### T7.3a — Frontend nhà phát hành và hợp đồng

**Phụ thuộc:** T7.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 561-573.

Trang admin: danh sách và form nhà phát hành; danh sách và form hợp đồng phim (thêm bớt tuần chiếu, tỷ lệ từng tuần, kiểm tra tuần liên tục ở form); danh sách phim chưa có hợp đồng; chọn nhà phát hành ở form phim.

Tiêu chí hoàn thành: quản trị tạo nhà phát hành, khai hợp đồng nhiều tuần, thấy được phim chưa có hợp đồng.
```

---

### T7.3b — Frontend đối soát nhà phát hành

**Phụ thuộc:** T7.2d.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 561-573.

Trang admin: danh sách đối soát, tạo đối soát theo kỳ, chi tiết theo phim và tuần chiếu kèm dòng điều chỉnh, đổi trạng thái và ghi ngày thanh toán, tải PDF và Excel, lịch thanh toán sắp tới.

Tiêu chí hoàn thành: quản trị chạy đối soát một kỳ, chuyển trạng thái, tải file.
```

---

### T7.3c — Khối lợi nhuận theo phim trong báo cáo tài chính

**Phụ thuộc:** T7.2b, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 561-573.

- Backend: đăng ký khối "lợi nhuận theo phim" vào khung báo cáo của T6.4: doanh thu vé trừ phần chia nhà phát hành của kỳ; phim chưa có hợp đồng trả "chưa có hợp đồng". Có trong xuất Excel và PDF của báo cáo.
- Frontend: hiển thị khối này trong trang báo cáo tài chính.

Tiêu chí hoàn thành: báo cáo lợi nhuận theo phim khớp đối soát; phim chưa có hợp đồng hiện đúng nhãn.
```

---

# HẠNG MỤC 8. Chốt doanh thu cuối ngày và chốt ca quầy

---

### T8.1a — Ca quầy: mở và đóng ca

**Phụ thuộc:** T1.8.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644 (phần ca quầy theo prompt này).

- Entity CounterShift: nhân viên, thời điểm mở, tiền đầu ca, thời điểm đóng, số thực đếm, số phải có, chênh lệch, ghi chú, trạng thái.
- API mở ca (mỗi nhân viên một ca mở, nhập tiền đầu ca) và đóng ca (nhập số thực đếm; hệ thống ghi số phải có và chênh lệch). Số phải có = tiền đầu ca + thu tiền mặt − hoàn tiền mặt − tiền thối, tính từ sổ giao dịch của các đơn thuộc ca.
- Nhân viên chỉ xem và đóng ca của mình; quản trị xem và đóng mọi ca. Ghi nhật ký mở, đóng và chênh lệch.

Tiêu chí hoàn thành: đóng ca ghi đúng chênh lệch; một nhân viên không mở được hai ca.
```

---

### T8.1b — Gắn đơn quầy vào ca và chặn bán khi chưa mở ca

**Phụ thuộc:** T8.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644.

- Mọi đơn quầy (vé, combo riêng, đơn bổ sung bán tại quầy) gắn vào ca đang mở của nhân viên thao tác; giao dịch tiền mặt tính theo ca.
- Chặn tạo đơn quầy và thu tiền khi nhân viên chưa có ca mở (lỗi trong Messages.resx).
- Quầy xác nhận đã hoàn tiền mặt (đơn cần hoàn của T1.7) ghi vào ca đang mở của người xác nhận.

Tiêu chí hoàn thành: không bán được khi chưa mở ca; đơn và tiền hoàn đi vào đúng ca.
```

---

### T8.1c — Cảnh báo ca chưa đóng

**Phụ thuộc:** T8.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644.

- Tác vụ nền cuối ngày kinh doanh (Utils/BusinessDay.cs) quét ca còn mở, cảnh báo quản trị bằng kênh thông báo hiện có, không gửi lặp.
- API danh sách ca còn mở; quản trị đóng thay ca kèm ghi chú, có nhật ký.

Tiêu chí hoàn thành: ca quên đóng được cảnh báo đúng một lần; quản trị đóng thay được.
```

---

### T8.2a — Báo cáo chốt ngày (dựng số liệu)

**Phụ thuộc:** T8.1b, T6.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644 và 49-56.

- Entity DailyClosing: ngày kinh doanh, trạng thái, ảnh chụp số liệu (JSON), tiền mặt theo sổ, số thực đếm, chênh lệch, ghi chú, người chốt, thời điểm chốt.
- Service dựng số liệu một ngày từ ShowtimeDailyStat, PaymentDailyStat và các ca: doanh thu vé, combo, thẻ quà bán và thẻ quà đã dùng, giảm giá, hoàn tiền, thu theo từng phương thức, số đơn chờ hoặc lỗi. Các dòng thẻ quà, gói, thuê phòng, thuế đăng ký qua khung khối báo cáo của T6.4; chưa có thì bỏ qua.
- API xem báo cáo nháp của một ngày chưa chốt.

Tiêu chí hoàn thành: số liệu ngày khớp báo cáo doanh thu của T6.4 cùng ngày.
```

---

### T8.2b — Chốt ngày

**Phụ thuộc:** T8.2a, T8.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644.

- Quản trị nhập tổng tiền thực đếm cả ngày; tiền mặt theo sổ là tổng các ca; hệ thống ghi chênh lệch và ghi chú.
- Điều kiện chốt: mọi ca đã đóng và không còn đơn lỗi, trừ khi cài đặt cho phép bỏ qua kèm ghi chú.
- Sau chốt: khóa ảnh chụp số liệu, không API nào sửa trực tiếp; lưu để tra cứu theo ngày. Ghi nhật ký.

Tiêu chí hoàn thành: ngày đã chốt không sửa trực tiếp được; chốt khi còn ca mở bị chặn.
```

---

### T8.2c — Bút toán điều chỉnh

**Phụ thuộc:** T8.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644.

- Entity LedgerAdjustment: ngày ghi sổ (ngày kinh doanh đang mở), ngày gốc, loại, số tiền theo từng chỉ tiêu, lý do, người tạo, tham chiếu đơn hoặc giao dịch.
- Handler nền cho OrderRefunded và OrderCancelled: nếu ngày gốc đã chốt thì tự tạo bút toán; idempotent. Chừa điểm gọi cho đổi suất (T11.2c).
- API admin tạo bút toán thủ công kèm lý do, có nhật ký.

Tiêu chí hoàn thành: hoàn tiền muộn tạo bút toán vào ngày đang mở; số của ngày đã chốt không đổi.
```

---

### T8.2d — Báo cáo theo ngày gốc và ngày ghi sổ, email chốt

**Phụ thuộc:** T8.2c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644.

- Báo cáo chốt có hai chế độ "theo ngày gốc" và "theo ngày ghi sổ", thể hiện rõ phần điều chỉnh.
- Khi chốt, handler nền gửi báo cáo chốt qua email cho quản trị (nội dung ở Messages.resx), idempotent; lưu để tra cứu theo ngày.

Tiêu chí hoàn thành: hai chế độ cho số khác nhau đúng bằng phần điều chỉnh; email chốt chỉ gửi một lần.
```

---

### T8.3a — Frontend quầy: mở và đóng ca

**Phụ thuộc:** T8.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644.

Quầy: màn mở ca (nhập tiền đầu ca), đóng ca (nhập thực đếm, xem chênh lệch, ghi chú), chặn vào màn bán khi chưa mở ca kèm hướng dẫn mở ca.

Tiêu chí hoàn thành: nhân viên mở và đóng ca; chưa mở ca thì không vào được màn bán.
```

---

### T8.3b — Frontend admin: ca và chốt ngày

**Phụ thuộc:** T8.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644.

Admin: danh sách ca (lọc theo ngày, nhân viên), đóng thay ca; trang chốt ngày (báo cáo nháp, nhập tiền thực đếm, cảnh báo điều kiện chưa đủ, nút chốt).

Tiêu chí hoàn thành: quản trị chốt được một ngày; điều kiện chưa đủ hiện cảnh báo rõ.
```

---

### T8.3c — Frontend admin: bút toán và báo cáo chốt

**Phụ thuộc:** T8.2d.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 634-644.

Admin: danh sách và form bút toán điều chỉnh, báo cáo chốt theo ngày gốc và ngày ghi sổ, lịch sử báo cáo chốt theo ngày.

Tiêu chí hoàn thành: quản trị tạo bút toán, đổi chế độ xem, tra cứu báo cáo chốt cũ.
```

---

# HẠNG MỤC 9. Hóa đơn VAT theo yêu cầu

---

### T9.1a — Thuế suất và tách giá trước thuế

**Phụ thuộc:** T1.8.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 600-611.

- Cài đặt thuế suất theo loại dòng hàng (vé, combo, thẻ quà tặng, gói thành viên) và thời hạn yêu cầu hóa đơn (số ngày).
- Service tách giá đã gồm VAT thành giá trước thuế và tiền thuế theo từng dòng dựa trên giá thực trả của dòng, làm tròn đến đồng, tổng hai phần luôn bằng giá thực trả. Thuế thẻ quà tính vào lúc thẻ được dùng (chừa điểm nối T10.2b).
- Chưa tạo hóa đơn.

Tiêu chí hoàn thành: trước thuế + thuế = giá thực trả của dòng ở mọi trường hợp làm tròn.
```

---

### T9.1b — Hồ sơ và yêu cầu hóa đơn

**Phụ thuộc:** T9.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 600-611.

- Entity InvoiceProfile (khách, mã số thuế, tên đơn vị, địa chỉ, email nhận) và InvoiceRequest (đơn, hồ sơ, trạng thái chờ xuất, đã xuất, từ chối, lý do, số hóa đơn, tệp, người xuất, thời điểm).
- API khách: CRUD hồ sơ, gửi yêu cầu cho đơn đã thanh toán trong thời hạn; mỗi đơn một yêu cầu đang hiệu lực.
- API quầy: nhập yêu cầu cho khách vãng lai bằng thông tin nhập tay.

Tiêu chí hoàn thành: chỉ đơn đã thanh toán và còn trong hạn mới yêu cầu được; hồ sơ dùng lại được.
```

---

### T9.1c — Xử lý yêu cầu hóa đơn, tệp và email

**Phụ thuộc:** T9.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 600-611.

- API admin: danh sách theo trạng thái, cập nhật số hóa đơn và tải tệp bằng hạ tầng file hiện có, từ chối kèm lý do. Ghi nhật ký.
- API khách tải hóa đơn từ lịch sử đơn.
- Email thông báo khi đổi trạng thái (Messages.resx). Không tích hợp nhà cung cấp hóa đơn điện tử.

Tiêu chí hoàn thành: admin cập nhật số hóa đơn thì khách tải được và nhận email.
```

---

### T9.1d — Điều chỉnh hóa đơn khi hoàn tiền và báo cáo thuế

**Phụ thuộc:** T9.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 600-611.

- Handler nền cho OrderRefunded: đơn đã xuất hóa đơn thì đánh dấu "cần điều chỉnh hóa đơn" và nhắc quản trị; API danh sách và đánh dấu đã xử lý.
- Khối báo cáo thuế đăng ký vào khung báo cáo T6.4: doanh thu trước thuế, thuế phải nộp theo kỳ và theo loại dòng hàng; có trong xuất Excel và PDF.

Tiêu chí hoàn thành: đơn hoàn sau khi xuất hóa đơn bị đánh dấu; báo cáo thuế khớp tổng thuế các dòng.
```

---

### T9.2a — Frontend khách và quầy: hóa đơn VAT

**Phụ thuộc:** T9.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 600-611.

- Khách: form yêu cầu hóa đơn trên chi tiết đơn, quản lý hồ sơ xuất hóa đơn, theo dõi trạng thái, tải hóa đơn.
- Quầy: nhập yêu cầu cho khách vãng lai.

Tiêu chí hoàn thành: khách gửi yêu cầu và tải được hóa đơn sau khi quản trị cập nhật.
```

---

### T9.2b — Frontend admin: hóa đơn VAT

**Phụ thuộc:** T9.1d.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 600-611.

Admin: danh sách yêu cầu theo trạng thái, cập nhật số hóa đơn và tải tệp, từ chối kèm lý do, danh sách đơn cần điều chỉnh hóa đơn, trang báo cáo thuế.

Tiêu chí hoàn thành: quản trị xử lý yêu cầu từ chờ xuất đến đã xuất và xem báo cáo thuế.
```

---

# HẠNG MỤC 10. Thẻ quà tặng (e-gift)

---

### T10.1a — Dữ liệu thẻ quà tặng

**Phụ thuộc:** T1.1, T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 344-352.

- Entity GiftCard: mã thẻ (sinh bằng bộ sinh số ngẫu nhiên mật mã, đủ dài, khó đoán), loại (quà tặng, tín dụng đổi suất), mệnh giá, số dư, người mua, email và tên người nhận, lời nhắn, thời điểm gửi, hạn dùng, trạng thái (chờ gửi, đã gửi, đang dùng, đã dùng hết, hết hạn, đã hủy), concurrency token.
- Entity GiftCardLedger: phát hành, giữ chỗ, sử dụng, hoàn lại, hết hạn, hủy; tham chiếu đơn; số dư luôn bằng tổng sổ.
- Cài đặt rạp: các mệnh giá có sẵn, mệnh giá tối thiểu và tối đa, thời hạn thẻ.
- Service sinh mã và tra cứu theo mã. Chưa mua, chưa dùng.

Tiêu chí hoàn thành: sinh được thẻ với mã không đoán được; số dư khớp tổng sổ.
```

---

### T10.1b — Mua thẻ và phát hành

**Phụ thuộc:** T10.1a, T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 353-358.

- API mua thẻ tạo Order loại thẻ quà: người mua có tài khoản, mệnh giá có sẵn hoặc tự nhập trong khoảng cho phép, thông tin người nhận, lời nhắn, gửi ngay hoặc hẹn ngày giờ.
- Đơn thẻ quà không hưởng ưu đãi hạng, không dùng mã khuyến mãi, không tích điểm.
- Handler trong giao dịch khi OrderConfirmed: phát hành thẻ và ghi sổ; idempotent.

Tiêu chí hoàn thành: đơn mua thẻ xác nhận thì thẻ được phát hành đúng mệnh giá, xác nhận hai lần không phát hai thẻ.
```

---

### T10.1c — Gửi email thẻ

**Phụ thuộc:** T10.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 353-358.

- Tác vụ nền gửi email cho người nhận khi đến thời điểm gửi (lời nhắn, mã thẻ, hướng dẫn dùng); chuyển chờ gửi sang đã gửi; idempotent, có thử lại. Người mua nhận email xác nhận.
- API admin gửi lại email. Nội dung ở Messages.resx.

Tiêu chí hoàn thành: thẻ hẹn giờ được gửi đúng lúc, đúng một lần.
```

---

### T10.2a — Áp thẻ vào đơn và giữ chỗ số dư

**Phụ thuộc:** T10.1a, T1.2, T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 359-365 và 57-61.

- IResourceHold cho thẻ: giữ chỗ số dư bằng UPDATE có điều kiện; chỉ trừ thật khi đơn xác nhận (handler trong giao dịch, ghi sổ thẻ và giao dịch thu phương thức thẻ quà); nhả khi hết hạn hoặc gỡ thẻ. Một thẻ không bị hai đơn cùng tiêu quá số dư.
- API áp và gỡ thẻ trên đơn chờ. Thẻ trả một phần hoặc toàn bộ; đủ trả hết thì xác nhận ngay không qua cổng; chưa đủ thì phần còn lại trả bằng phương thức khác.
- Dùng được cho vé, combo, gói sản phẩm, gói thành viên, thuê phòng; không dùng để mua thẻ quà. Thẻ loại tín dụng đổi suất chỉ dùng cho đúng tài khoản được cấp (T11.2b cấp).
- Quầy nhận thẻ bằng cách nhập mã.

Tiêu chí hoàn thành: thẻ trả một phần cộng cổng xác nhận đúng; hai đơn không tiêu cùng một số dư.
```

---

### T10.2b — Hoàn về thẻ, hết hạn và ghi nhận doanh thu

**Phụ thuộc:** T10.2a, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 359-365 và 49-56.

- Mở rộng hoàn tiền của T1.7: phần đã trả bằng thẻ hoàn về lại thẻ (ghi sổ thẻ và giao dịch hoàn); thẻ đã hết hạn khi nhận hoàn được gia hạn tối thiểu số ngày cấu hình.
- Tác vụ nền cho thẻ hết hạn, ghi sổ hết hạn, idempotent.
- Bán thẻ là khoản phải trả, chỉ thành doanh thu khi thẻ được dùng: ghi nhận dữ liệu này để báo cáo (T10.5b), chốt ngày (T8.2a) và thuế (T9.1a) dùng. Điểm vẫn tích trên phần khách thực trả kể cả trả bằng thẻ.

Tiêu chí hoàn thành: hoàn đơn đưa tiền về thẻ; thẻ hết hạn chuyển trạng thái đúng; doanh thu chỉ phát sinh khi dùng thẻ.
```

---

### T10.2c — Chống dò mã, khóa tạm và hủy thẻ

**Phụ thuộc:** T10.2a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 366-370.

- Giới hạn số lần thử kiểm tra số dư và áp mã theo tài khoản và địa chỉ mạng; nhập sai quá ngưỡng thì khóa tạm (ngưỡng và thời gian khóa cấu hình).
- API admin hủy thẻ (ví dụ phát hành nhầm): thẻ đã dùng một phần thì chọn cách xử lý số dư kèm lý do. Ghi nhật ký.

Tiêu chí hoàn thành: nhập sai nhiều lần bị khóa tạm; hủy thẻ có dòng nhật ký và dòng sổ thẻ.
```

---

### T10.3a — Quà doanh nghiệp: nhập và kiểm tra danh sách

**Phụ thuộc:** T10.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 371-374.

- Tải lên file danh sách người nhận (tên, email, mệnh giá, lời nhắn) bằng hạ tầng nhập file hiện có, chạy nền.
- Kiểm tra: email hợp lệ, mệnh giá trong khoảng, không trùng; báo cáo lỗi theo dòng; API tải file mẫu. Chưa tạo đơn.

Tiêu chí hoàn thành: file có dòng lỗi trả báo cáo lỗi rõ theo dòng.
```

---

### T10.3b — Đơn tổng và phát hành hàng loạt

**Phụ thuộc:** T10.3a, T10.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 371-374.

- Từ danh sách hợp lệ tạo một Order tổng, mỗi người nhận một dòng thẻ; thanh toán như đơn thường.
- Khi đơn xác nhận phát hành từng thẻ và gửi từng thẻ cho người nhận (dùng lại T10.1b, T10.1c), idempotent.

Tiêu chí hoàn thành: file hợp lệ tạo một đơn tổng và phát hành đủ thẻ.
```

---

### T10.4a — Frontend khách: mua thẻ và thẻ đã mua

**Phụ thuộc:** T10.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 375-381.

Khách: trang mua thẻ quà (chọn mệnh giá, thông tin người nhận, lời nhắn, hẹn giờ gửi), danh sách thẻ đã mua và trạng thái gửi.

Tiêu chí hoàn thành: khách mua thẻ và theo dõi trạng thái gửi.
```

---

### T10.4b — Frontend khách và quầy: dùng thẻ, quà doanh nghiệp

**Phụ thuộc:** T10.2c, T10.3b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 359-365 và 375-381.

- Khách: ô nhập thẻ ở bước thanh toán (số dư, số tiền trừ, phần còn lại), danh sách thẻ tín dụng đang có, thông báo khi bị khóa tạm; trang tải lên danh sách quà doanh nghiệp.
- Quầy: nhập mã thẻ khi thanh toán.

Tiêu chí hoàn thành: khách dùng thẻ trả một phần rồi thanh toán phần còn lại.
```

---

### T10.5a — Frontend admin: quản lý thẻ

**Phụ thuộc:** T10.2c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 375-381.

Admin: danh sách thẻ, tra cứu theo mã hoặc email, lịch sử sử dụng của thẻ, hủy thẻ kèm lý do, gửi lại email.

Tiêu chí hoàn thành: quản trị tra cứu, hủy thẻ và gửi lại email.
```

---

### T10.5b — Báo cáo khoản phải trả thẻ quà

**Phụ thuộc:** T10.2b, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 375-381 và 49-56.

- Backend: đăng ký khối "khoản phải trả thẻ quà tặng" vào khung báo cáo T6.4 (đã bán chưa dùng, gồm số dư hết hạn, theo loại quà tặng và tín dụng đổi suất); có trong xuất Excel và PDF.
- Frontend: hiển thị khối trong báo cáo tài chính.

Tiêu chí hoàn thành: khoản phải trả khớp tổng số dư các thẻ.
```

---

# HẠNG MỤC 11. Đổi suất chiếu

---

### T11.1a — Cài đặt, lịch sử đổi và kiểm tra điều kiện

**Phụ thuộc:** T2.3, T1.8.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 452-467.

- Cài đặt rạp: hạn đổi (giờ trước giờ chiếu), số lần đổi tối đa mỗi đơn (mặc định 1), hạn thẻ tín dụng đổi suất (ngày).
- Entity OrderExchange: đơn, vé cũ và vé mới, suất cũ và mới, chênh lệch, thời điểm, người thực hiện.
- Service kiểm tra điều kiện: đơn đã xác nhận; suất mới cùng phim, là suất thường hoặc sự kiện còn mở bán, không đổi sang hoặc từ suất riêng; còn trước hạn đổi; chưa vượt số lần đổi; vé đã check-in không được đổi; đổi từng vé hoặc cả đơn; đơn tiền mặt chỉ đổi tại quầy. Trả lỗi có lý do rõ ràng.

Tiêu chí hoàn thành: mỗi điều kiện không đạt trả đúng lý do.
```

---

### T11.1b — API xem trước chênh lệch

**Phụ thuộc:** T11.1a, T2.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 452-467 và 38-48.

- API xem trước đổi suất (không đổi dữ liệu): liệt kê suất đổi được cùng phim, với từng suất tính giá mới bằng IPricingService giữ nguyên loại vé và kiểm tra lại điều kiện loại vé.
- So với giá thực trả của dòng vé cũ: đắt hơn thì số cần thu thêm, rẻ hơn thì số tín dụng được cấp.

Tiêu chí hoàn thành: xem trước trả đúng chênh lệch hai chiều và chặn đúng các suất không hợp lệ.
```

---

### T11.2a — Giữ ghế mới và thu thêm

**Phụ thuộc:** T11.1b, T1.6, T1.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 452-467 và 57-61.

- API bắt đầu đổi: giữ ghế mới nguyên tử kèm hạn, khóa giá mới.
- Suất đắt hơn: tạo khoản thu thêm qua đơn bổ sung hoặc giao dịch thu thông thường trên đơn; đổi chỉ hoàn tất khi đủ tiền. Hết hạn thì nhả ghế mới, vé cũ còn nguyên.

Tiêu chí hoàn thành: đổi sang suất đắt hơn thu đúng chênh lệch; hết hạn không làm mất vé cũ.
```

---

### T11.2b — Suất rẻ hơn: tín dụng đổi suất hoặc hoàn tiền mặt

**Phụ thuộc:** T11.2a, T10.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 452-467.

- Suất rẻ hơn: cấp thẻ tín dụng đổi suất (GiftCard loại tín dụng, T10.1a) gắn tài khoản khách, hạn theo cấu hình, không hoàn về cổng thanh toán.
- Đơn tiền mặt của khách vãng lai tại quầy: hoàn chênh lệch bằng tiền mặt, ghi giao dịch hoàn vào ca đang mở (T8.1b).

Tiêu chí hoàn thành: đổi sang suất rẻ hơn cấp đúng tín dụng; khách vãng lai nhận hoàn tiền mặt đúng.
```

---

### T11.2c — Hoàn tất đổi suất

**Phụ thuộc:** T11.2a, T11.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 452-467 và 49-56.

- Khi đủ điều kiện hoàn tất, trong một giao dịch: nhả ghế cũ, vô hiệu vé và QR cũ, phát vé và QR mới (giữ loại vé, giá đã khóa), ghi OrderExchange, tăng số lần đổi, gửi email xác nhận.
- Mã khuyến mãi đã dùng, điểm đã tích, số tiền giảm của vé cũ giữ nguyên; combo không bị ảnh hưởng.
- Không phát OrderCancelled và không tạo hoàn tiền ra ngoài. Cập nhật bảng tổng hợp T6.1 (doanh thu vé chuyển sang ngày chiếu mới); ngày cũ đã chốt thì tạo bút toán điều chỉnh (T8.2c).
- Quầy thực hiện đổi suất giúp khách.

Tiêu chí hoàn thành: vé và QR cũ không còn quét được; báo cáo hủy không đếm đổi suất.
```

---

### T11.3a — Frontend khách và quầy: đổi suất

**Phụ thuộc:** T11.2c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 452-467.

- Khách: nút đổi suất trên chi tiết đơn, chọn suất mới cùng phim, xem trước chênh lệch, thanh toán thêm hoặc nhận tín dụng, xem lịch sử đổi.
- Quầy: đổi suất giúp khách.

Tiêu chí hoàn thành: khách đổi suất trọn luồng cả hai chiều giá.
```

---

### T11.3b — Báo cáo đổi suất

**Phụ thuộc:** T11.2c, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 452-467.

- Backend: đăng ký khối báo cáo đổi suất vào khung T6.4: số lần đổi, tỷ lệ đổi so với hủy, chênh lệch thu thêm, tín dụng đã cấp, phim và khung giờ hay bị đổi.
- Frontend: hiển thị khối trong trang báo cáo.

Tiêu chí hoàn thành: số liệu khớp OrderExchange; báo cáo hủy không đếm đổi suất.
```

---

# HẠNG MỤC 12. Combo đặt trước và nhắc lịch chiếu

---

### T12.1a — Đơn bổ sung combo

**Phụ thuộc:** T4.2, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 485-495.

- Tạo đơn bổ sung (loại đơn bổ sung, trỏ về đơn gốc đã xác nhận, trước giờ chiếu) chỉ gồm combo, thanh toán riêng, giữ chỗ kho như đơn thường (T4.2). Vé và ghế của đơn gốc không bị ảnh hưởng.
- Giá ưu đãi cho combo đặt trước (cấu hình), còn hiệu lực đến trước giờ chiếu.
- Nhận combo tại quầy bằng QR của đơn bổ sung (dùng luồng nhận combo T4.3).

Tiêu chí hoàn thành: đơn bổ sung thanh toán độc lập và nhận được bằng QR.
```

---

### T12.1b — Gợi ý combo

**Phụ thuộc:** T12.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 485-495.

API gợi ý combo cho một đơn: theo số vé (hai vé gợi ý combo đôi, ánh xạ số vé sang combo cấu hình được) và lịch sử mua combo của khách; chỉ gợi ý combo còn hàng; kèm giá đặt trước.

Tiêu chí hoàn thành: đơn hai vé nhận gợi ý combo đôi; combo hết hàng không được gợi ý.
```

---

### T12.1c — Liên kết đơn bổ sung với đơn gốc

**Phụ thuộc:** T12.1a, T5.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 485-495.

- Đơn gốc bị hủy hoặc đổi suất: không tự hủy đơn bổ sung, nhắc khách xử lý (hủy và hoàn nếu chưa nhận, hoặc giữ nhận vào suất mới). Handler nền cho OrderCancelled; chừa điểm gọi từ T11.2c.
- Rạp hủy suất hoặc đóng phòng: đơn bổ sung bị hủy và hoàn cùng đơn gốc (mở rộng T1.7 và T5.2).

Tiêu chí hoàn thành: hủy suất hoàn cả hai đơn; khách hủy đơn gốc nhận nhắc về đơn bổ sung.
```

---

### T12.2a — Nhắc lịch chiếu

**Phụ thuộc:** T12.1a, T1.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 485-495.

- Tác vụ nền gửi nhắc trước giờ chiếu theo số giờ cấu hình: mã QR, địa chỉ rạp, phòng, giờ mở check-in, hướng dẫn nhận combo. Chỉ gửi khách chưa tắt nhắc lịch (helper T1.5); idempotent, mỗi vé nhắc một lần.
- Suất bị đổi hoặc hủy thì hủy nhắc cũ.

Tiêu chí hoàn thành: nhắc đúng giờ, không gửi cho suất đã đổi hoặc hủy, không gửi cho khách đã tắt.
```

---

### T12.2b — Email gợi ý combo sau đặt vé

**Phụ thuộc:** T12.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 485-495.

Email xác nhận đặt vé có khối gợi ý combo đặt trước kèm liên kết tới trang mua; xử lý qua handler nền của OrderConfirmed, không làm chậm xác nhận đơn.

Tiêu chí hoàn thành: email xác nhận có gợi ý combo đúng đơn.
```

---

### T12.3a — Frontend khách: combo đặt trước

**Phụ thuộc:** T12.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 485-495.

Khách: khối gợi ý combo ở trang xác nhận đặt vé và chi tiết đơn, thanh toán đơn bổ sung, thông báo nhắc xử lý đơn bổ sung khi đơn gốc hủy hoặc đổi suất.

Tiêu chí hoàn thành: khách mua thêm combo từ trang xác nhận.
```

---

### T12.3b — Báo cáo combo đặt trước

**Phụ thuộc:** T12.1a, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 485-495.

- Backend: khối báo cáo vào khung T6.4: tỷ lệ khách mua thêm combo sau đặt vé, doanh thu combo đặt trước, so sánh với combo mua tại quầy, tỷ lệ combo đặt trước đã nhận.
- Frontend: hiển thị khối trong trang báo cáo.

Tiêu chí hoàn thành: báo cáo hiển thị đủ bốn chỉ số.
```

---

# HẠNG MỤC 13. Loyalty mạnh hơn

---

### T13.1a — Mô hình điểm: hai loại và lô điểm

**Phụ thuộc:** T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 303-318.

- Tách điểm xếp hạng (cửa sổ cấu hình hiện có, quyết định hạng, đổi quà không làm giảm) và số dư điểm đổi quà (hạn dùng riêng, cấu hình).
- Mở rộng dòng điểm hiện có thành lô: ngày nhận, hạn dùng, số điểm còn lại. Loại biến động: tích từ đơn, thưởng, đổi quà, hết hạn, thu hồi khi hủy hoặc hoàn, điều chỉnh của quản trị.
- Service đọc số dư, hạng; hàm trừ điểm tiêu hao lô cũ nhất trước (chưa có API đổi quà).

Tiêu chí hoàn thành: trừ điểm lấy đúng thứ tự lô cũ trước; đổi quà không làm đổi hạng.
```

---

### T13.1b — Tích và thu hồi điểm theo đơn

**Phụ thuộc:** T13.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 303-318.

- Tích điểm trên số tiền khách thực trả của đơn vé và combo (handler trong giao dịch khi OrderConfirmed); đơn thẻ quà, gói thành viên, thuê phòng không tích.
- Thu hồi theo tỷ lệ phần hoàn khi hủy hoặc hoàn (handler cho OrderRefunded, OrderCancelled); nếu đã tiêu mất thì số dư có thể âm và trừ vào lần tích tiếp theo. Idempotent.

Tiêu chí hoàn thành: hủy đơn thu hồi điểm đúng tỷ lệ; số dư âm được trừ ở lần tích sau.
```

---

### T13.1c — Hết hạn điểm và nhắc

**Phụ thuộc:** T13.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 303-318 và 62-70.

- Tác vụ nền xử lý điểm hết hạn theo lô, idempotent.
- Nhắc khách có điểm sắp hết hạn (khoảng ngày cấu hình), chỉ gửi theo tùy chọn thông báo của khách (helper T1.5), mỗi lô nhắc một lần.

Tiêu chí hoàn thành: điểm hết hạn đúng lô; nhắc không gửi lặp.
```

---

### T13.1d — Điều chỉnh điểm của quản trị

**Phụ thuộc:** T13.1a, T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 303-318 và 71-74.

API admin tra cứu khách và điều chỉnh điểm cộng hoặc trừ, bắt buộc lý do, tạo dòng lô loại điều chỉnh, ghi nhật ký. API báo cáo điểm đã phát, đã đổi, đã hết hạn theo kỳ (khối báo cáo của khung T6.4).

Tiêu chí hoàn thành: điều chỉnh có dòng lô và dòng nhật ký; báo cáo điểm khớp sổ.
```

---

### T13.2a — Danh mục quà

**Phụ thuộc:** T13.1a, T3.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 319-323.

Entity quà: tên, số điểm cần, loại (vé miễn phí, combo miễn phí, mã giảm giá), số lượng còn, thời gian hiệu lực, hạn dùng của mã được cấp. API admin CRUD, ghi nhật ký tạo và sửa; API khách xem danh mục đang đổi được.

Tiêu chí hoàn thành: CRUD đầy đủ; khách chỉ thấy quà còn hiệu lực và còn số lượng.
```

---

### T13.2b — API đổi quà

**Phụ thuộc:** T13.2a, T3.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 319-323.

API khách đổi quà: trừ điểm nguyên tử từ lô cũ nhất, trừ số lượng quà, rồi cấp mã khuyến mãi cá nhân dùng một lần gắn tài khoản có hạn dùng (T3.1). Mã hết hạn không dùng thì không hoàn điểm; trả thông tin này khi đổi. Trong một giao dịch, lỗi thì hoàn tác cả ba.

Tiêu chí hoàn thành: đổi quà trừ đúng điểm và cấp đúng mã; hết số lượng hoặc thiếu điểm thì không đổi được.
```

---

### T13.3a — Thử thách: dữ liệu và CRUD

**Phụ thuộc:** T13.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 324-330.

Entity thử thách: tên, kỳ áp dụng, điều kiện (ví dụ xem 3 phim khác nhau trong tháng), phần thưởng (điểm hoặc quà); entity tiến độ của khách. API admin CRUD, API khách xem thử thách và tiến độ.

Tiêu chí hoàn thành: tạo được thử thách; khách xem được tiến độ.
```

---

### T13.3b — Cập nhật tiến độ và trao thưởng

**Phụ thuộc:** T13.3a, T1.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 324-330.

- Handler nền cho TicketCheckedIn cập nhật tiến độ; chỉ tính vé đã check-in, không tính suất riêng; idempotent.
- Hoàn thành thì cấp thưởng tự động và gửi thông báo; mỗi khách chỉ nhận một lần cho mỗi thử thách.

Tiêu chí hoàn thành: đặt vé nhưng không đến xem không tính tiến độ; thưởng cấp đúng một lần.
```

---

### T13.4a — Quan hệ giới thiệu

**Phụ thuộc:** T1.5, T13.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 331-336.

- Người mới nhập mã giới thiệu khi đăng ký; lưu quan hệ (người giới thiệu, người được giới thiệu). Mỗi người chỉ được giới thiệu một lần, không tự giới thiệu.
- API danh sách người đã giới thiệu và trạng thái nhận thưởng. Chưa trao thưởng.

Tiêu chí hoàn thành: mã đúng tạo quan hệ; tự giới thiệu và giới thiệu lần hai bị chặn.
```

---

### T13.4b — Thưởng giới thiệu và chống lạm dụng

**Phụ thuộc:** T13.4a, T13.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 331-336.

- Handler nền cho OrderConfirmed: thưởng cả hai bên khi đơn đầu tiên của người được giới thiệu được xác nhận, không phải lúc đăng ký; idempotent.
- Chống lạm dụng: trùng số điện thoại hoặc email với tài khoản đã có không được tính; trần thưởng giới thiệu mỗi tháng cho mỗi người (cấu hình); chừa điểm gọi kiểm tra thiết bị của T18.1c.

Tiêu chí hoàn thành: thưởng chỉ cấp một lần sau đơn đầu xác nhận; trùng thông tin bị chặn.
```

---

### T13.5a — Frontend khách: điểm và quà

**Phụ thuộc:** T13.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 337-343.

Khách: số dư điểm và hạng, lịch sử điểm theo lô, điểm sắp hết hạn, danh mục quà và đổi quà (nói rõ mã hết hạn không hoàn điểm).

Tiêu chí hoàn thành: khách xem điểm theo lô và đổi quà thành công.
```

---

### T13.5b — Frontend khách: thử thách và giới thiệu

**Phụ thuộc:** T13.3b, T13.4b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 337-343.

Khách: trang thử thách và tiến độ; mã giới thiệu có nút sao chép và danh sách người đã giới thiệu; ô nhập mã giới thiệu ở form đăng ký.

Tiêu chí hoàn thành: khách theo dõi thử thách, chia sẻ mã và đăng ký bằng mã.
```

---

### T13.6a — Frontend admin: quà và thử thách

**Phụ thuộc:** T13.2a, T13.3a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 337-343.

Admin: danh sách và form quản lý quà, danh sách và form quản lý thử thách.

Tiêu chí hoàn thành: quản trị tạo quà và thử thách.
```

---

### T13.6b — Frontend admin: điều chỉnh điểm và báo cáo

**Phụ thuộc:** T13.1d.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 337-343.

Admin: tra cứu khách và điều chỉnh điểm (nhập lý do), báo cáo điểm đã phát, đã đổi, đã hết hạn theo kỳ.

Tiêu chí hoàn thành: điều chỉnh điểm có nhật ký; báo cáo điểm hiển thị đúng.
```

---

# HẠNG MỤC 14. Hỗ trợ khách hàng và hoàn tiền ngoại lệ

---

### T14.1a — Yêu cầu hỗ trợ: dữ liệu và API khách

**Phụ thuộc:** T1.8.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 612-621.

- Entity yêu cầu hỗ trợ gắn một đơn: loại (không nhận email vé, bị trừ tiền nhưng đơn chưa xác nhận, muốn hoàn ngoài chính sách, góp ý suất chiếu...), trạng thái (mới, đang xử lý, chờ khách phản hồi, đã giải quyết), người phụ trách, hạn xử lý; entity tin nhắn trao đổi.
- API khách: tạo yêu cầu từ một đơn của mình, xem danh sách và tiến độ, trả lời.
- Cài đặt hạn xử lý mặc định.

Tiêu chí hoàn thành: khách tạo và trả lời yêu cầu; không xem được yêu cầu của người khác.
```

---

### T14.1b — Xử lý yêu cầu (nhân viên và quản trị)

**Phụ thuộc:** T14.1a, T3.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 612-621.

- API nhân viên và quản trị: danh sách theo trạng thái và người phụ trách, gán người phụ trách, đổi trạng thái, trả lời khách.
- Trong yêu cầu: cấp mã quà xin lỗi (mã khuyến mãi cá nhân, T3.1) và gửi lại email vé.
- Email thông báo khách khi có cập nhật (Messages.resx).

Tiêu chí hoàn thành: xử lý trọn một yêu cầu, khách nhận email mỗi lần cập nhật.
```

---

### T14.1c — Tự tạo yêu cầu từ đối soát và thu thừa

**Phụ thuộc:** T14.1a, T6.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 612-621.

Tự tạo yêu cầu hỗ trợ (idempotent theo giao dịch) khi đối soát cổng (T6.5) phát hiện giao dịch đã thu mà đơn chưa xác nhận, và khi thu thừa không tự hoàn được (T1.2). Gắn đúng đơn và khách.

Tiêu chí hoàn thành: giao dịch bị trừ tiền nhưng đơn chưa xác nhận tự sinh đúng một yêu cầu.
```

---

### T14.2a — Đề xuất hoàn ngoại lệ và quy tắc duyệt

**Phụ thuộc:** T14.1a, T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 622-627.

- Entity đề xuất hoàn ngoại lệ: đơn, hoàn từng vé hoặc một phần số tiền, lý do, số tiền, người đề xuất, trạng thái, người duyệt, thời điểm.
- Nhân viên quầy và quản trị được đề xuất. Vượt ngưỡng cấu hình thì cần quản trị duyệt, dưới ngưỡng quản trị duyệt nhanh. Người đề xuất không tự duyệt đề xuất của mình. Ghi nhật ký đề xuất, duyệt và lý do.

Tiêu chí hoàn thành: không tự duyệt được đề xuất của chính mình; ngưỡng áp đúng.
```

---

### T14.2b — Thực thi hoàn ngoại lệ

**Phụ thuộc:** T14.2a, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 622-627.

- Khi duyệt, hoàn qua sổ giao dịch (T1.7), không vượt số đã thu. Combo đã nhận được phép hoàn ở luồng này.
- Hoàn ngoại lệ không tự thay đổi vé; nếu hoàn kèm hủy vé thì quản trị chọn hủy vé trong cùng thao tác để nhả ghế. Phát OrderRefunded.

Tiêu chí hoàn thành: hoàn vượt ngưỡng không thực hiện được khi chưa duyệt; hoàn kèm hủy vé nhả ghế.
```

---

### T14.3a — Frontend khách: hỗ trợ

**Phụ thuộc:** T14.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 612-621.

Khách: tạo yêu cầu từ chi tiết đơn, danh sách yêu cầu, trang theo dõi và trả lời.

Tiêu chí hoàn thành: khách tạo yêu cầu và trao đổi với nhân viên.
```

---

### T14.3b — Frontend nhân viên và quản trị: xử lý và hoàn ngoại lệ

**Phụ thuộc:** T14.1b, T14.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 612-627.

Nhân viên và quản trị: bảng yêu cầu theo trạng thái, gán người phụ trách, trao đổi, cấp mã quà xin lỗi, gửi lại email vé, đề xuất hoàn ngoại lệ, danh sách chờ duyệt và nút duyệt (chỉ quản trị).

Tiêu chí hoàn thành: xử lý trọn một yêu cầu từ khách gửi đến hoàn ngoại lệ có duyệt.
```

---

### T14.3c — Báo cáo hỗ trợ

**Phụ thuộc:** T14.2b, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 628-633.

- Backend: khối báo cáo vào khung T6.4: số yêu cầu theo loại, thời gian xử lý trung bình, tỷ lệ giải quyết trong hạn, tổng tiền hoàn ngoại lệ theo lý do và theo người đề xuất.
- Frontend: hiển thị khối trong trang báo cáo.

Tiêu chí hoàn thành: số liệu khớp dữ liệu yêu cầu và đề xuất hoàn.
```

---

# HẠNG MỤC 15. Gợi ý phim cá nhân hóa

---

### T15.1a — Tín hiệu sở thích của khách

**Phụ thuộc:** T1.8, T1.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 417-433.

- Tác vụ nền (dùng sự kiện TicketCheckedIn và đánh giá mới) cập nhật hồ sơ sở thích mỗi khách: thể loại, diễn viên, đạo diễn từ vé đã check-in và đánh giá đã viết; phim xem gần đây và chấm cao có trọng số lớn hơn; vé đã mua chưa quét có trọng số thấp hơn.
- Hồ sơ thói quen đặt vé: khung giờ, định dạng phòng, mức giá thường chọn.
- Khách tắt cá nhân hóa (T1.5) thì không cập nhật hồ sơ.

Tiêu chí hoàn thành: hồ sơ cập nhật sau check-in; khách tắt cá nhân hóa không có hồ sơ.
```

---

### T15.1b — Tương đồng giữa phim và độ phổ biến

**Phụ thuộc:** T15.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 417-433.

Tác vụ nền tính: mức tương đồng giữa các phim theo hành vi (người xem A thường xem B, dựa trên vé đã check-in) và điểm phổ biến chung kèm đánh giá chung (dùng cho khách mới). Lưu bảng kết quả để API đọc nhanh.

Tiêu chí hoàn thành: bảng tương đồng và phổ biến cập nhật định kỳ; chạy lặp không sinh trùng.
```

---

### T15.1c — Chấm điểm, loại trừ và API "Dành cho bạn"

**Phụ thuộc:** T15.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 417-433.

- Tính điểm phù hợp khách với từng phim từ hồ sơ sở thích, tương đồng, phổ biến; xếp suất theo thói quen đặt vé.
- Loại trừ: phim khách đã mua vé hoặc đã xem; chỉ phim đang chiếu còn suất thường trong những ngày tới; ưu tiên suất còn ghế; không gợi ý suất riêng; không gợi ý suất sự kiện cho khách chưa đủ điều kiện mua.
- Mỗi gợi ý kèm lý do ngắn. API "Dành cho bạn"; khách tắt cá nhân hóa thì chỉ trả danh sách phổ biến chung.

Tiêu chí hoàn thành: khách mới thấy danh sách phổ biến; khách có lịch sử thấy phim cùng thể loại hoặc đạo diễn kèm lý do; phim đã xem không xuất hiện.
```

---

### T15.2a — Công cụ gợi ý cho trợ lý AI

**Phụ thuộc:** T15.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 434-445.

Thêm công cụ gợi ý phim cho trợ lý AI: khi khách hỏi "tối nay xem gì", trợ lý gọi công cụ, diễn đạt lại kết quả kèm suất chiếu cụ thể. Công cụ chỉ thấy suất thường và sự kiện công khai, tôn trọng cờ tắt cá nhân hóa.

Tiêu chí hoàn thành: trợ lý gợi ý đúng kèm suất; khách tắt cá nhân hóa chỉ nhận danh sách chung.
```

---

### T15.2b — Email giới thiệu phim mới

**Phụ thuộc:** T15.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 434-445.

Tác vụ nền gửi email giới thiệu phim mới phù hợp: chỉ cho khách đã bật tiếp thị và bật gợi ý (helper T1.5), có liên kết hủy đăng ký, không gửi lặp cùng phim cho cùng khách, idempotent.

Tiêu chí hoàn thành: khách chưa bật tiếp thị không nhận email; mỗi phim chỉ gửi một lần cho mỗi khách.
```

---

### T15.3 — Frontend "Dành cho bạn"

**Phụ thuộc:** T15.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 434-445.

Mục "Dành cho bạn" ở trang chủ và trang phim, hiển thị lý do gợi ý, dùng MovieCard hiện có; công tắc tắt gợi ý cá nhân hóa ở trang cá nhân (khi tắt chỉ hiện danh sách phổ biến chung).

Tiêu chí hoàn thành: tắt cá nhân hóa thì không còn mục "Dành cho bạn" cá nhân.
```

---

# HẠNG MỤC 16. Đánh giá xác thực và khảo sát sau khi xem

---

### T16.1a — Mời đánh giá và thưởng điểm

**Phụ thuộc:** T1.3, T13.1b, T1.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 548-560.

- Giữ nguyên quy tắc đánh giá hiện có (chỉ khi có vé đã check-in, mỗi khách một đánh giá mỗi phim). Đánh giá gắn nhãn "đã xem" và tính vào điểm trung bình phim.
- Tác vụ nền gửi email mời đánh giá sau suất một đến ba giờ (cấu hình), chỉ cho khách chưa tắt nhắc và mời đánh giá (helper T1.5); idempotent.
- Đánh giá hợp lệ được thưởng điểm nhỏ (qua sổ điểm T13.1a), mỗi phim thưởng một lần.
- Đánh giá xác thực là tín hiệu cho T15.1a và độ nóng phim.

Tiêu chí hoàn thành: mời đánh giá gửi đúng thời điểm, một lần; thưởng điểm một lần mỗi phim.
```

---

### T16.1b — Khảo sát nhanh trải nghiệm

**Phụ thuộc:** T1.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 548-560.

Entity khảo sát: chấm một đến năm cho phòng, âm thanh, hình ảnh, vệ sinh, dịch vụ quầy; gắn với đơn và suất đã xem, mỗi suất một lần, chỉ khách có vé đã check-in. API gửi khảo sát và đọc lại khảo sát của mình.

Tiêu chí hoàn thành: một suất chỉ gửi được một khảo sát; khách không có vé đã check-in bị chặn.
```

---

### T16.1c — Báo cáo chất lượng phòng và cảnh báo

**Phụ thuộc:** T16.1b, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 548-560.

- API báo cáo chất lượng theo phòng, theo tiêu chí và theo thời gian (khối báo cáo vào khung T6.4).
- Tác vụ nền cảnh báo quản trị khi điểm một phòng thấp liên tục (ngưỡng và số ngày cấu hình), không cảnh báo lặp.

Tiêu chí hoàn thành: cảnh báo xuất hiện khi phòng điểm thấp liên tục, đúng một lần cho mỗi đợt.
```

---

### T16.2a — Frontend khách: đánh giá và khảo sát

**Phụ thuộc:** T16.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 548-560.

Khách: nhãn "đã xem" trên đánh giá, trang đánh giá mở từ liên kết email mời, form khảo sát nhanh sau suất.

Tiêu chí hoàn thành: khách gửi đánh giá và khảo sát từ liên kết email.
```

---

### T16.2b — Frontend admin: chất lượng phòng

**Phụ thuộc:** T16.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 548-560.

Admin: báo cáo chất lượng theo phòng và theo thời gian, hiển thị cảnh báo phòng điểm thấp.

Tiêu chí hoàn thành: quản trị thấy cảnh báo chất lượng phòng.
```

---

# HẠNG MỤC 17. Danh sách chờ và báo khi mở bán

---

### T17.1a — Đăng ký chờ cho suất hết ghế

**Phụ thuộc:** T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 468-477.

- Entity đăng ký chờ: suất, khách, số ghế cần, yêu cầu ghế liền kề, thứ tự, trạng thái (đang chờ, đã thông báo, đã giữ, đã rút, đã đóng).
- Mỗi khách một lần chờ cho mỗi suất; rút đăng ký bất kỳ lúc nào; tự đóng khi suất bắt đầu hoặc bị hủy (tác vụ nền, idempotent).
- API đăng ký, rút, xem trạng thái. Chỉ cho đăng ký khi suất thật sự không còn tổ hợp ghế phù hợp.

Tiêu chí hoàn thành: đăng ký trùng bị chặn; suất bắt đầu thì danh sách chờ đóng.
```

---

### T17.1b — Phát hiện ghế được nhả và thông báo

**Phụ thuộc:** T17.1a, T1.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 468-477.

- Handler nền khi ghế được nhả (hết hạn giữ, đơn hủy): kiểm tra có đủ tổ hợp ghế hợp lệ cho người chờ theo quy tắc chống để trống ghế lẻ hiện có hay không; có thì chọn khách đứng đầu phù hợp (đúng số ghế, liền kề nếu yêu cầu) và gửi thông báo.
- Idempotent: một lần nhả không thông báo hai lần cho cùng khách.

Tiêu chí hoàn thành: nhả ghế đủ tổ hợp thì người đứng đầu nhận thông báo đúng một lần.
```

---

### T17.1c — Ưu tiên giữ ghế cho người chờ

**Phụ thuộc:** T17.1b, T5.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 468-477.

- Khi thông báo, khóa các ghế dành riêng bằng SeatBlock loại người chờ (T5.3) có hạn theo cấu hình (ví dụ mười phút); người khác không giữ được các ghế này.
- Khách giữ ghế trong thời gian ưu tiên thì gỡ khóa và chuyển sang luồng giữ ghế thường. Hết thời gian: gỡ khóa, mở cho người kế tiếp, sau cùng mở cho khách bình thường. Tác vụ nền xử lý hết hạn, idempotent.

Tiêu chí hoàn thành: ghế dành riêng không bị người khác giữ; hết thời gian thì chuyển người kế.
```

---

### T17.2a — Quan tâm phim sắp chiếu

**Phụ thuộc:** T1.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 478-484.

Entity quan tâm phim: khách đánh dấu quan tâm một phim sắp chiếu, bỏ quan tâm được, mỗi khách một dòng cho mỗi phim. API khách đánh dấu, bỏ, xem danh sách. API admin xem số người quan tâm theo từng phim; chừa API danh sách người quan tâm cho mở bán sớm (T23.1c).

Tiêu chí hoàn thành: đánh dấu và bỏ quan tâm đúng; admin thấy số người quan tâm.
```

---

### T17.2b — Thông báo khi mở bán

**Phụ thuộc:** T17.2a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 478-484.

Khi phim có suất chiếu đầu tiên mở bán, handler nền gửi thông báo cho người quan tâm (thông báo theo yêu cầu của khách, không cần bật tiếp thị); idempotent, mỗi người một lần cho mỗi phim.

Tiêu chí hoàn thành: suất đầu tiên mở bán thì người quan tâm nhận thông báo đúng một lần.
```

---

### T17.3a — Frontend khách: chờ và quan tâm

**Phụ thuộc:** T17.1c, T17.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 468-484.

Khách: nút "Đăng ký chờ" ở suất hết ghế (chọn số ghế, ghế liền kề), trạng thái chờ, thông báo có ghế kèm đồng hồ đếm ngược ưu tiên và nút giữ ghế, nút rút đăng ký; nút "Quan tâm" ở phim sắp chiếu.

Tiêu chí hoàn thành: khách đăng ký chờ, nhận thông báo và giữ được ghế trong thời gian ưu tiên.
```

---

### T17.3b — Báo cáo danh sách chờ và quan tâm

**Phụ thuộc:** T17.1c, T17.2a, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 468-484.

- Backend: khối báo cáo vào khung T6.4: số lượt chờ theo suất, tỷ lệ người chờ được ghế rồi mua, số người quan tâm theo phim.
- Frontend: hiển thị khối trong trang báo cáo và số người quan tâm ở trang quản lý phim.

Tiêu chí hoàn thành: báo cáo khớp dữ liệu đăng ký chờ và quan tâm.
```

---

# HẠNG MỤC 18. Chống giữ ghế ảo và hạn chế lạm dụng

---

### T18.1a — Đếm đơn hết hạn và giới hạn giữ ghế

**Phụ thuộc:** T1.6.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 574-585.

- Đếm số đơn hết hạn liên tiếp của từng khách (reset khi có đơn xác nhận); vượt ngưỡng cấu hình thì rút ngắn thời gian giữ ghế hoặc giới hạn số đơn chờ cùng lúc trong khoảng thời gian cấu hình.
- Giới hạn số ghế giữ đồng thời và số đơn chờ của mỗi tài khoản (cấu hình). Lỗi nêu rõ lý do và thời hạn (Messages.resx).

Tiêu chí hoàn thành: bỏ liên tiếp quá ngưỡng thì thời gian giữ ghế bị rút ngắn; vượt giới hạn đồng thời bị chặn.
```

---

### T18.1b — Giới hạn tần suất công khai và xác minh bổ sung

**Phụ thuộc:** T18.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 574-585.

- Dịch vụ giới hạn tần suất theo địa chỉ mạng và thiết bị cho biểu mẫu công khai không cần tài khoản (yêu cầu thuê phòng, liên hệ); dùng lại ở T19.1a.
- Dấu hiệu bất thường (nhiều lần giữ rồi bỏ trong thời gian ngắn, nhiều lần thanh toán thất bại) thì yêu cầu xác minh bổ sung trước khi tiếp tục; API xác minh và cờ trạng thái.

Tiêu chí hoàn thành: gửi dồn dập bị chặn; hành vi bất thường kích hoạt yêu cầu xác minh.
```

---

### T18.1c — Hạn chế khách và chống lạm dụng ưu đãi

**Phụ thuộc:** T18.1b, T3.3, T13.4b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 574-585.

- Entity hạn chế khách: lý do, thời hạn, trạng thái. API admin xem danh sách, gỡ hạn chế; ghi nhật ký mọi hạn chế. Khách bị hạn chế nhận lỗi nêu rõ lý do và thời hạn.
- Chống lạm dụng ưu đãi: nhiều tài khoản chung số điện thoại hoặc thiết bị không cùng hưởng ưu đãi khách mới (T3.1) hoặc thưởng giới thiệu (T13.4b).

Tiêu chí hoàn thành: quản trị gỡ được hạn chế; hai tài khoản chung số điện thoại không cùng nhận thưởng giới thiệu.
```

---

### T18.2 — Frontend hạn chế khách

**Phụ thuộc:** T18.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 574-585.

- Admin: danh sách khách đang bị hạn chế (lý do, thời hạn), nút gỡ hạn chế kèm xác nhận.
- Khách: banner hoặc hộp thoại nêu lý do và thời hạn khi bị hạn chế, bước xác minh bổ sung khi hệ thống yêu cầu.

Tiêu chí hoàn thành: quản trị gỡ hạn chế; khách thấy thông báo hạn chế rõ ràng.
```

---

# HẠNG MỤC 19. Thuê phòng và bán sỉ

---

### T19.1a — Yêu cầu thuê phòng (dữ liệu và endpoint công khai)

**Phụ thuộc:** T1.1, T18.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 382-390.

- Entity yêu cầu thuê phòng: họ tên, đơn vị, loại đối tượng (doanh nghiệp, trường học, nhóm bạn), liên hệ, ngày giờ mong muốn, khoảng dự phòng, số người, phim mong muốn hoặc tự mang nội dung, định dạng phòng, combo, yêu cầu khác, trạng thái (mới, đang xử lý, đã báo giá, khách chấp nhận, khách từ chối, đã đặt cọc, đã thanh toán đủ, hoàn tất, hủy, hết hạn báo giá).
- Endpoint công khai gửi yêu cầu không cần tài khoản, giới hạn tần suất theo địa chỉ mạng và thiết bị (T18.1b).

Tiêu chí hoàn thành: khách không tài khoản gửi được yêu cầu; gửi dồn dập bị chặn.
```

---

### T19.1b — Trang theo dõi qua liên kết riêng và API admin

**Phụ thuộc:** T19.1a, T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 382-390.

- Đường dẫn theo dõi riêng gửi qua email: token khó đoán, có hạn, giới hạn số lần thử; API công khai xem trạng thái theo token.
- API admin: danh sách theo trạng thái, xem chi tiết, đổi trạng thái hợp lệ; ghi nhật ký. Email xác nhận đã nhận yêu cầu.

Tiêu chí hoàn thành: khách vào được trang theo dõi qua link riêng; token sai nhiều lần bị khóa.
```

---

### T19.2a — Bảng giá thuê và tính báo giá

**Phụ thuộc:** T19.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 391-397 và 38-48 (ngoại lệ: thuê phòng không dùng bộ tính giá vé).

- Entity bảng giá thuê theo phòng, khung giờ, thời lượng; API admin CRUD, cache, nhật ký.
- Service tính báo giá = giá thuê + phí theo đầu người nếu có + gói combo + khoản phụ − chiết khấu của quản trị. Mỗi khoản là một dòng báo giá.

Tiêu chí hoàn thành: báo giá cộng đúng các khoản và chiết khấu; sửa bảng giá làm mới cache ngay.
```

---

### T19.2b — Phiên bản báo giá, PDF và email

**Phụ thuộc:** T19.2a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 391-397.

- Entity báo giá: yêu cầu, phiên bản, các dòng, hạn hiệu lực, tiền đặt cọc (phần trăm cấu hình), hạn thanh toán nốt, chính sách hủy theo mốc. Mỗi lần sửa tạo phiên bản mới, giữ lịch sử; ghi nhật ký.
- Xuất PDF báo giá và gửi email cho khách kèm liên kết trang theo dõi.

Tiêu chí hoàn thành: soạn, sửa tạo phiên bản mới, xuất PDF và gửi email được.
```

---

### T19.2c — Khách chấp nhận hoặc từ chối, hết hạn báo giá

**Phụ thuộc:** T19.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 391-397.

- API công khai theo token: xem các phiên bản báo giá, tải PDF, chấp nhận hoặc từ chối phiên bản mới nhất còn hiệu lực.
- Tác vụ nền chuyển quá hạn sang "hết hạn báo giá", idempotent.

Tiêu chí hoàn thành: chấp nhận hoặc từ chối đổi đúng trạng thái; quá hạn tự chuyển trạng thái.
```

---

### T19.3a — Suất riêng, loại suất và khóa ghế

**Phụ thuộc:** T19.2c, T5.1, T5.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 398-403.

- Showtime chưa có trường loại suất: thêm nếu chưa có (Regular, Private, Event; dùng chung với T23.1a), nối vào ShowtimeDailyStat.Kind của T6.1.
- Khi khách chấp nhận và có cọc, tạo suất riêng trong phòng đã chốt: kiểm tra xung đột với suất khác, thời gian đệm và khoảng đóng phòng (T5.1). Cho gắn nội dung khách mang, không bắt buộc có phim trong danh mục.
- Khóa toàn bộ ghế của suất bằng SeatBlock loại thuê phòng (T5.3).

Tiêu chí hoàn thành: xung đột phòng bị chặn; suất riêng khóa kín mọi ghế.
```

---

### T19.3b — Ẩn suất riêng khỏi các luồng công khai và thống kê

**Phụ thuộc:** T19.3a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 398-403.

- Suất riêng không hiện ở lịch chiếu công khai, trợ lý AI, gợi ý phim; không áp quy tắc giá, ưu đãi hạng, khuyến mãi, tích điểm.
- Loại khỏi thống kê lấp đầy, gợi ý lịch chiếu, độ nóng phim (lọc theo Kind của T6.1); vẫn tính vào doanh thu; thêm báo cáo riêng cho thuê phòng (khối vào khung T6.4).
- Rà các điểm lọc đã có (T6.2, T6.3, T15.1c) bảo đảm lọc đúng.

Tiêu chí hoàn thành: suất riêng không lộ ở bất kỳ API công khai nào và không làm lệch thống kê lấp đầy.
```

---

### T19.3c — Thanh toán nhiều đợt và ghi nhận doanh thu

**Phụ thuộc:** T19.3a, T10.2a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 404-409 và 49-56.

- Tạo Order loại thuê phòng cho đợt cọc và đợt thanh toán nốt; quản trị ghi nhận chuyển khoản, khách trả qua cổng hoặc thẻ quà.
- Tiền cọc là khoản nhận trước, chuyển thành doanh thu vào ngày sự kiện diễn ra (xử lý bằng tác vụ nền theo ngày kinh doanh, idempotent; ghi nhận vào bảng tổng hợp T6.1 và bút toán nếu ngày đã chốt).
- Cập nhật trạng thái yêu cầu theo từng đợt thanh toán.

Tiêu chí hoàn thành: cọc rồi trả nốt chuyển đúng trạng thái; doanh thu chỉ ghi vào ngày sự kiện.
```

---

### T19.3d — Chính sách hủy và quá hạn thanh toán

**Phụ thuộc:** T19.3c, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 404-409.

- Chính sách hủy riêng theo mốc thời gian cấu hình: hủy sớm hoàn toàn bộ hoặc một phần cọc, hủy muộn mất cọc; hoàn qua sổ giao dịch (T1.7). Rạp hủy vì sự cố thì hoàn đầy đủ.
- Tác vụ nền: quá hạn thanh toán nốt thì hủy yêu cầu, xử lý cọc theo chính sách hủy muộn, xóa suất riêng và gỡ SeatBlock để giải phóng phòng. Idempotent.
- Ghi nhật ký.

Tiêu chí hoàn thành: quá hạn thanh toán nốt thì suất được giải phóng; hủy sớm hoàn đúng tỷ lệ.
```

---

### T19.4a — Frontend khách: biểu mẫu yêu cầu thuê phòng

**Phụ thuộc:** T19.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 410-416.

Trang công khai biểu mẫu gửi yêu cầu thuê phòng (không cần đăng nhập), xử lý lỗi giới hạn tần suất, trang xác nhận đã gửi.

Tiêu chí hoàn thành: khách gửi yêu cầu trên điện thoại.
```

---

### T19.4b — Frontend khách: theo dõi, báo giá và thanh toán

**Phụ thuộc:** T19.2c, T19.3c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 410-416.

Trang theo dõi qua liên kết riêng: trạng thái, các phiên bản báo giá, tải PDF, chấp nhận hoặc từ chối, thanh toán cọc và thanh toán nốt.

Tiêu chí hoàn thành: khách xem báo giá, chấp nhận và thanh toán cọc trên điện thoại.
```

---

### T19.5a — Frontend admin: yêu cầu và báo giá

**Phụ thuộc:** T19.2c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 410-416.

Admin: bảng theo dõi yêu cầu theo trạng thái (bảng hoặc cột kanban), chi tiết yêu cầu, màn soạn báo giá và phiên bản, quản lý bảng giá thuê.

Tiêu chí hoàn thành: quản trị soạn và gửi báo giá từ một yêu cầu mới.
```

---

### T19.5b — Frontend admin: lịch phòng, thanh toán và báo cáo

**Phụ thuộc:** T19.3d, T19.3b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 410-416.

Admin: lịch phòng hiển thị các suất riêng, ghi nhận thanh toán chuyển khoản, hủy yêu cầu theo chính sách, báo cáo số yêu cầu, tỷ lệ chốt và doanh thu thuê phòng.

Tiêu chí hoàn thành: quản trị xử lý trọn một yêu cầu từ mới đến hoàn tất.
```

---

# HẠNG MỤC 20. Gói sản phẩm (bundle)

---

### T20.1a — Dữ liệu và CRUD gói sản phẩm

**Phụ thuộc:** T2.2, T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 496-507.

- Entity gói: thành phần (số vé theo từng loại vé, các combo), giá gói, điều kiện (suất, phim, ngày, khung giờ, loại ghế được phép), hạn mức số gói mỗi ngày, thời gian hiệu lực, cờ không dùng chung với mã khuyến mãi.
- API admin CRUD, validate giá gói thấp hơn tổng giá lẻ tính bằng IPricingService, ghi nhật ký, cache.

Tiêu chí hoàn thành: tạo gói hợp lệ; giá gói không thấp hơn tổng lẻ bị chặn.
```

---

### T20.1b — Hạn mức gói và API gói áp dụng

**Phụ thuộc:** T20.1a, T4.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 496-507 và 57-61.

- IResourceHold cho hạn mức số gói mỗi ngày: giữ chỗ khi giữ ghế, nhả cùng ghế, chốt khi xác nhận (UPDATE có điều kiện).
- API khách danh sách gói áp dụng được cho một suất kèm mức tiết kiệm so với mua lẻ; gói hết hạn mức ngày hoặc thiếu kho combo tự ẩn.

Tiêu chí hoàn thành: hết hạn mức ngày thì gói ngừng bán; hạn mức không vượt khi nhiều khách cùng giữ.
```

---

### T20.2a — Mua gói và tách giá về dòng hàng

**Phụ thuộc:** T20.1b, T3.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 496-507 và 38-48.

- Khách mua gói: chọn ghế như đặt vé thường; hệ thống tách giá gói về từng dòng hàng theo tỷ lệ giá lẻ niêm yết tại thời điểm mua (dùng DiscountAllocator, dòng cuối nhận phần dư), ghi OrderPriceAdjustment loại giá gói.
- Trừ kho combo và chia nhà phát hành dùng giá đã tách.

Tiêu chí hoàn thành: tổng các dòng đã tách bằng đúng giá gói đến đồng.
```

---

### T20.2b — Chồng ưu đãi và hoàn từng thành phần

**Phụ thuộc:** T20.2a, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 496-507.

- Ưu đãi hạng và mã khuyến mãi áp trên giá đã tách; cờ không dùng chung của gói chặn mã khi quản trị chọn.
- Hoàn một thành phần của gói tính theo giá đã tách; khách chỉ được hoàn phần tương ứng (dùng dịch vụ hoàn từng phần của T1.7).

Tiêu chí hoàn thành: hoàn một vé của gói hoàn đúng giá đã tách; gói có cờ không dùng chung từ chối mã.
```

---

### T20.2c — Báo cáo gói sản phẩm

**Phụ thuộc:** T20.2b, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 496-507.

Khối báo cáo vào khung T6.4: số gói bán, doanh thu gói, so sánh với trường hợp khách mua lẻ, tỷ lệ chuyển đổi (xem gói rồi mua).

Tiêu chí hoàn thành: số liệu khớp các dòng đã tách.
```

---

### T20.3a — Frontend khách và quầy: gói sản phẩm

**Phụ thuộc:** T20.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 496-507.

- Khách: hiển thị gói và mức tiết kiệm ở trang chọn suất, chọn gói rồi chọn ghế theo số vé của gói, chi tiết đơn thể hiện từng thành phần với giá đã tách.
- Quầy: bán gói.

Tiêu chí hoàn thành: khách mua gói trọn luồng.
```

---

### T20.3b — Frontend admin: gói sản phẩm

**Phụ thuộc:** T20.2c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 496-507.

Admin: danh sách và form gói (thành phần, giá, điều kiện, hạn mức), trang báo cáo gói.

Tiêu chí hoàn thành: quản trị tạo gói và xem báo cáo.
```

---

# HẠNG MỤC 21. Gói thành viên trả phí

---

### T21.1a — Gói thành viên và mua gói

**Phụ thuộc:** T1.6, T1.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520.

- Entity gói thành viên (theo tháng hoặc quý): giá, thời hạn, quyền lợi (giảm phần trăm cho vé, số vé miễn phí mỗi tháng, giảm giá combo, quyền đặt sớm cho suất đặc biệt), cờ dùng chung với mã. API admin CRUD, ghi nhật ký.
- Khách mua gói như một đơn thường (không ưu đãi hạng, không mã, không tích điểm). Khi OrderConfirmed tạo đăng ký của khách có hiệu lực từ đến; mua khi còn hiệu lực thì nối tiếp.

Tiêu chí hoàn thành: mua gói xong khách có đăng ký đúng thời hạn; mua chồng nối tiếp đúng.
```

---

### T21.1b — Nhắc hết hạn và gia hạn

**Phụ thuộc:** T21.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520.

- Tác vụ nền nhắc khách sắp hết hạn bằng email (số ngày cấu hình), idempotent. Gia hạn do khách chủ động thanh toán, không tự trừ tiền.
- Hết hạn thì đăng ký chuyển trạng thái hết hiệu lực (nền, idempotent).

Tiêu chí hoàn thành: nhắc gửi một lần; hết hạn thì mất quyền lợi.
```

---

### T21.1c — Quyền lợi trong bộ tính giá

**Phụ thuộc:** T21.1a, T3.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520 và 38-48.

Điền chỗ trống quyền lợi gói ở bộ tính giá cấp đơn (T3.2): áp sau ưu đãi hạng và trước mã khuyến mãi; quy tắc chồng ưu đãi quyết định gói có dùng chung với mã không; giảm vé và giảm combo theo phần trăm của gói. Phân bổ về dòng hàng qua DiscountAllocator, ghi OrderPriceAdjustment loại quyền lợi gói.

Tiêu chí hoàn thành: quyền lợi áp đúng thứ tự; hết hạn gói thì không còn giảm.
```

---

### T21.2a — Sổ vé miễn phí

**Phụ thuộc:** T21.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520.

Sổ vé miễn phí riêng: cấp theo tháng hiệu lực, không cộng dồn sang tháng sau, mỗi dòng ghi rõ vé nào dùng vào đơn nào. Tác vụ nền cấp vé đầu mỗi tháng của đăng ký (idempotent). API xem số vé còn lại.

Tiêu chí hoàn thành: vé miễn phí tháng này không sang tháng sau; chạy cấp lặp không cấp trùng.
```

---

### T21.2b — Dùng vé miễn phí vào đơn

**Phụ thuộc:** T21.2a, T21.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520 và 57-61.

- IResourceHold cho vé miễn phí: giữ chỗ khi khách dùng cho đơn chờ, nhả khi hết hạn hoặc gỡ, chốt khi xác nhận.
- API áp vé miễn phí vào đơn (giá dòng vé về 0 qua OrderPriceAdjustment, ghi giá trị quy đổi cho T21.3b).
- Giới hạn số vé miễn phí trên mỗi suất (cấu hình); không dùng cho suất sự kiện trừ khi sự kiện cho phép.

Tiêu chí hoàn thành: bị chặn khi quá giới hạn mỗi suất; hai đơn không cùng dùng một vé.
```

---

### T21.3a — Ghi nhận doanh thu gói theo tháng

**Phụ thuộc:** T21.1a, T6.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520 và 49-56.

Doanh thu gói ghi nhận chia đều theo từng tháng hiệu lực: tác vụ nền tạo bút toán ghi nhận hằng tháng (idempotent); báo cáo phân biệt doanh thu đã ghi nhận và chưa ghi nhận; cập nhật báo cáo chốt ngày (T8.2a).

Tiêu chí hoàn thành: tổng các tháng bằng giá gói; mỗi tháng ghi nhận đúng một lần.
```

---

### T21.3b — Quy đổi vé miễn phí sang doanh thu vé

**Phụ thuộc:** T21.2b, T21.3a, T7.2a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520 và 49-56.

- Mỗi gói khai báo giá trị quy đổi của một vé miễn phí. Khi vé miễn phí được dùng (xác nhận đơn), chuyển phần giá trị quy đổi từ doanh thu gói sang doanh thu vé của suất (ghi theo ngày chiếu), phần còn lại giữ là doanh thu gói.
- Nối vào T7.2a: giá trị quy đổi cộng vào doanh thu vé để chia nhà phát hành. Cập nhật bảng tổng hợp T6.1.

Tiêu chí hoàn thành: giá trị quy đổi xuất hiện ở doanh thu vé và đối soát nhà phát hành, không tính hai lần.
```

---

### T21.3c — Hủy và hoàn gói thành viên

**Phụ thuộc:** T21.3a, T1.7.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520.

Mặc định không hoàn gói đã bắt đầu hiệu lực, trừ lỗi hệ thống hoặc quản trị cho ngoại lệ; chính sách cấu hình. Hủy gói chưa hiệu lực hoàn đủ. Hoàn thì dừng ghi nhận doanh thu các tháng còn lại và thu hồi vé miễn phí chưa dùng.

Tiêu chí hoàn thành: hoàn gói dừng đúng các tháng chưa ghi nhận.
```

---

### T21.4a — Frontend khách: mua gói và gói của tôi

**Phụ thuộc:** T21.1b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520.

Khách: danh sách gói thành viên và mua gói; trang gói của tôi (thời hạn, quyền lợi, số vé miễn phí còn lại, nút gia hạn, nhắc sắp hết hạn).

Tiêu chí hoàn thành: khách mua gói và xem quyền lợi còn lại.
```

---

### T21.4b — Frontend khách: áp quyền lợi khi đặt vé

**Phụ thuộc:** T21.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520.

Bước chọn ghế và thanh toán: hiển thị khoản giảm từ gói, chọn dùng vé miễn phí, thông báo khi bị chặn vì quá giới hạn mỗi suất.

Tiêu chí hoàn thành: khách dùng vé miễn phí và thấy quyền lợi áp trong đơn.
```

---

### T21.5a — Frontend admin: quản lý gói và hội viên

**Phụ thuộc:** T21.3c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520.

Admin: form gói (quyền lợi, giá trị quy đổi vé miễn phí, chính sách hoàn), danh sách hội viên, hoàn ngoại lệ gói.

Tiêu chí hoàn thành: quản trị tạo gói và xử lý hoàn ngoại lệ.
```

---

### T21.5b — Báo cáo gói thành viên

**Phụ thuộc:** T21.3b, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 508-520.

- Backend: khối báo cáo vào khung T6.4: số hội viên đang hoạt động, tỷ lệ gia hạn, giá trị quyền lợi đã dùng so với tiền gói, lợi nhuận so với bán vé lẻ, doanh thu gói đã ghi nhận và chưa ghi nhận.
- Frontend: hiển thị khối trong trang báo cáo.

Tiêu chí hoàn thành: báo cáo đủ chỉ số và khớp bút toán ghi nhận.
```

---

# HẠNG MỤC 22. Phân khúc khách hàng và chiến dịch

---

### T22.1a — Phân khúc tự động

**Phụ thuộc:** T13.1a, T1.5.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 521-535.

Tác vụ nền định kỳ tính chỉ số và phân nhóm khách: hoạt động (khách mới, thường xuyên, lâu chưa quay lại theo số ngày cấu hình), giá trị (chi tiêu cao, tần suất cao chấm theo gần đây, tần suất, chi tiêu; giá trị vòng đời ước tính), sở thích (thể loại, định dạng phòng), tình trạng điểm (sắp hết hạn, sắp lên hạng). Lưu kết quả để đọc nhanh; idempotent.

Tiêu chí hoàn thành: phân nhóm cập nhật định kỳ và khớp dữ liệu mẫu.
```

---

### T22.1b — Phân khúc tùy chỉnh và API

**Phụ thuộc:** T22.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 521-535.

Entity phân khúc tùy chỉnh bằng điều kiện kết hợp (và, hoặc) trên các chỉ số của T22.1a. API admin CRUD, xem số khách và số khách đủ điều kiện nhận thông tin (đã bật tiếp thị), xem danh sách khách trong phân khúc (che dữ liệu cá nhân khi không cần).

Tiêu chí hoàn thành: số khách đủ điều kiện nhận thông tin đúng; danh sách khách che dữ liệu cá nhân mặc định.
```

---

### T22.2a — Chiến dịch: dữ liệu, điều kiện gửi và gửi thử

**Phụ thuộc:** T22.1b, T3.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 536-541.

- Entity chiến dịch: phân khúc, nội dung email hoặc thông báo, mã khuyến mãi cá nhân kèm theo (mỗi khách một mã), thời điểm hẹn gửi, trạng thái. API admin CRUD.
- Chỉ gửi khách đã bật tiếp thị; mỗi email có liên kết hủy đăng ký; thông báo trong ứng dụng không cần email. Mỗi khách nhận tối đa một chiến dịch trong khoảng thời gian cấu hình.
- API gửi thử cho quản trị trước khi gửi thật.

Tiêu chí hoàn thành: gửi thử đến đúng quản trị; danh sách người nhận loại khách chưa đồng ý.
```

---

### T22.2b — Gửi chiến dịch bằng tác vụ nền

**Phụ thuộc:** T22.2a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 536-541.

Tác vụ nền gửi chiến dịch đến giờ hẹn: tạo mã cá nhân cho từng khách nhận, gửi email hoặc thông báo, idempotent theo (chiến dịch, khách), có thử lại, ghi trạng thái từng người nhận.

Tiêu chí hoàn thành: chiến dịch chỉ đến khách đã đồng ý; mỗi khách một mã riêng; chạy lặp không gửi trùng.
```

---

### T22.2c — Đo kết quả chiến dịch

**Phụ thuộc:** T22.2b.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 536-541.

Đo: số email gửi, số khách mở (theo dõi mở email bằng điểm ảnh theo dõi, tôn trọng quyền riêng tư), số khách dùng mã, doanh thu phát sinh từ các đơn dùng mã của chiến dịch. API kết quả theo chiến dịch.

Tiêu chí hoàn thành: số dùng mã và doanh thu khớp đơn thật.
```

---

### T22.3 — Báo cáo giữ chân khách

**Phụ thuộc:** T22.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 542-547.

API báo cáo giữ chân theo nhóm khách đăng ký cùng tháng: tỷ lệ phần trăm quay lại đặt vé ở tháng thứ nhất, thứ hai, thứ ba. Đọc từ bảng tổng hợp cập nhật theo ngày, không quét bảng giao dịch trực tiếp.

Tiêu chí hoàn thành: số liệu đúng với dữ liệu mẫu và tải nhanh.
```

---

### T22.4a — Frontend admin: phân khúc và giữ chân

**Phụ thuộc:** T22.1b, T22.3.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 521-535 và 542-547.

Admin: danh sách phân khúc với số khách và số khách đủ điều kiện nhận thông tin, form tạo phân khúc tùy chỉnh bằng điều kiện kết hợp, bảng giữ chân theo nhóm tháng đăng ký.

Tiêu chí hoàn thành: quản trị tạo phân khúc tùy chỉnh và xem bảng giữ chân.
```

---

### T22.4b — Frontend admin: chiến dịch

**Phụ thuộc:** T22.2c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 536-541.

Admin: soạn chiến dịch (chọn phân khúc, nội dung, kèm mã cá nhân, hẹn giờ, gửi thử), danh sách chiến dịch và trạng thái, trang kết quả.

Tiêu chí hoàn thành: quản trị tạo, gửi thử, hẹn giờ và xem kết quả chiến dịch.
```

---

# HẠNG MỤC 23. Suất chiếu sự kiện

---

### T23.1a — Loại suất sự kiện, phụ thu và giá riêng

**Phụ thuộc:** T2.2.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 645-657.

- Gắn loại "sự kiện" cho suất (trường loại suất Regular, Private, Event trên Showtime; thêm nếu T19.3a chưa thêm), nối vào ShowtimeDailyStat.Kind của T6.1.
- Phụ thuộc cố định hoặc quy tắc giá riêng cho suất sự kiện; nối vào IPricingService: suất sự kiện chỉ áp quy tắc riêng này (chỗ chừa của T2.2).

Tiêu chí hoàn thành: suất sự kiện có giá đúng phụ thu; không áp quy tắc giá thường.
```

---

### T23.1b — Hạn mức vé của suất sự kiện

**Phụ thuộc:** T23.1a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 645-657 và 57-61.

Hạn mức vé theo loại (ví dụ số vé dành cho hội viên) và hạn mức số vé mỗi khách; kiểm tra nguyên tử khi giữ ghế (IResourceHold hoặc UPDATE có điều kiện), nhả khi hết hạn, hủy.

Tiêu chí hoàn thành: hạn mức không bị vượt khi nhiều khách cùng giữ; vượt hạn mức mỗi khách bị chặn.
```

---

### T23.1c — Mở bán sớm

**Phụ thuộc:** T23.1a, T17.2a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 645-657.

- Mở bán sớm cho nhóm được chọn: hạng thành viên, hội viên gói trả phí (chỉ bật khi đã có T21.1a), khách đã bấm quan tâm phim (T17.2a). Đến giờ mở bán chung mới mở cho mọi khách.
- Hiển thị đúng ở lịch chiếu, sơ đồ ghế và API giữ ghế (chặn giữ ghế ngoài nhóm trước giờ mở bán chung).

Tiêu chí hoàn thành: mở bán sớm chỉ đúng nhóm được chọn; đến giờ chung mở cho mọi khách.
```

---

### T23.1d — Cờ khuyến mãi và loại khỏi gợi ý, độ nóng

**Phụ thuộc:** T23.1a, T3.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 645-657.

- Cờ không áp dụng một số khuyến mãi cho suất sự kiện (điều kiện loại suất ở T3.1). Vé miễn phí của gói chỉ dùng nếu sự kiện cho phép (T21.2b).
- Loại suất sự kiện khỏi gợi ý lịch chiếu và độ nóng phim (T6.3); vẫn tính doanh thu và đối soát nhà phát hành.

Tiêu chí hoàn thành: mã bị loại không áp được cho suất sự kiện; gợi ý lịch chiếu không tính suất sự kiện.
```

---

### T23.2a — Quà tặng kèm vé: khai báo và giữ chỗ

**Phụ thuộc:** T23.1b, T4.1.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 645-657 và 57-61.

- Khai báo quà tặng kèm của suất sự kiện (poster, ly, móc khóa) với số lượng, dùng lại cơ chế kho ở T4.1.
- IResourceHold cho quà: giữ chỗ theo vé khi giữ ghế, chốt khi xác nhận, nhả khi hết hạn; vé hủy thì quà chưa trao được hoàn lại.

Tiêu chí hoàn thành: quà không phát vượt số lượng; hủy vé hoàn quà chưa trao.
```

---

### T23.2b — Trao quà tại quầy và cảnh báo sắp hết

**Phụ thuộc:** T23.2a.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 645-657.

API quầy đánh dấu đã trao quà bằng quét QR vé (lưu nhân viên và thời điểm, chống trao hai lần); cảnh báo sắp hết quà qua cơ chế cảnh báo kho (T4.3).

Tiêu chí hoàn thành: quét lần hai không trao thêm; quà chạm ngưỡng gửi đúng một cảnh báo.
```

---

### T23.3a — Frontend admin: suất sự kiện và báo cáo

**Phụ thuộc:** T23.2b, T6.4.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 645-657.

- Admin: tạo suất sự kiện (phụ thu, hạn mức, nhóm mở bán sớm, giờ mở bán chung, quà tặng kèm).
- Báo cáo riêng từng sự kiện (backend khối vào khung T6.4 và hiển thị): số vé, doanh thu, lấp đầy, số quà đã trao.

Tiêu chí hoàn thành: báo cáo sự kiện đủ bốn chỉ số.
```

---

### T23.3b — Frontend khách và quầy: suất sự kiện

**Phụ thuộc:** T23.2b, T23.1c.

```text
Áp dụng QUY TẮC CHUNG. Đọc dòng 645-657.

- Khách: nhãn sự kiện, điều kiện, đếm ngược mở bán chung, thông báo khi chưa đủ điều kiện mua.
- Quầy: màn trao quà (quét QR vé).

Tiêu chí hoàn thành: khách ngoài nhóm không mua được trước giờ mở bán chung; quầy trao quà được.
```

---

## D. Thứ tự làm đề nghị

1. T1.1 → T1.11 (hoàn thành xong mới sang bước sau). T6.1 đã xong.
2. T2.1 → T2.5, T3.1 → T3.5, T4.1 → T4.5, T5.1 → T5.4.
3. T6.1 → T6.7 (mốc có giá trị kinh doanh rõ nhất).
4. T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23 theo thứ tự lộ trình ở phần 5 của tài liệu; từ T7 trở đi có thể đổi thứ tự miễn phụ thuộc đã xong. Trong một hạng mục, làm các task con theo thứ tự chữ cái và mỗi task con là một phiên riêng.

## E. Điểm cần bạn xác nhận trước khi giao

- Mục 7 của tài liệu liệt kê 14 quyết định đã tự chọn; các prompt đã bám theo đúng các quyết định đó.
- T2.2: cách kết hợp nhiều quy tắc giá (cộng dồn hay ưu tiên cao nhất) để AI tự chọn và báo lại; nên chốt trước nếu muốn cố định.
- T21.3 và T7.2: giá trị quy đổi vé miễn phí cần đối chiếu với hợp đồng thực tế với nhà phát hành.
- Từ hạng mục 7, phụ thuộc ghi chính xác đến task con. T7.2a, T19.3a, T23.1a cùng dùng trường loại suất của Showtime (Regular, Private, Event); task nào làm trước thì thêm trường, các task sau dùng lại.
- Tài liệu nghiệp vụ không có phần ca quầy (CounterShift) và không đánh số H*/mục 3.x như prompt T1 đến T6 gọi; mục A2 là bảng đối chiếu dòng.
