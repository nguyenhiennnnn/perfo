# Tài liệu nâng cấp business – Perfo Cinema

Tài liệu mô tả những hạng mục sẽ triển khai để nâng project từ mức demo lên một hệ thống bán vé và vận hành rạp chiếu phim thực tế. Nội dung chỉ mô tả nghiệp vụ, dữ liệu và hành vi hệ thống, không đi vào mã nguồn.

Mô hình áp dụng: một rạp, nhiều phòng chiếu, hai kênh bán (online và quầy).

---

## 1. Mục tiêu

1. Có bảng giá linh hoạt (theo giờ, ngày, loại vé, mức lấp đầy) để tối đa hóa doanh thu mỗi suất.
2. Có công cụ marketing (khuyến mãi, loyalty, thẻ quà tặng, giới thiệu bạn bè).
3. Bán được nhiều loại sản phẩm ngoài vé xem phim: combo bắp nước riêng, thẻ quà tặng, thuê phòng.
4. Quản lý được kho bắp nước và giá vốn.
5. Có báo cáo đủ để đưa quyết định: lấp đầy, lợi nhuận, hủy vé, tài chính, đối soát cổng thanh toán, gợi ý lịch chiếu.
6. Gợi ý phim phù hợp với từng khách.
7. Giữ doanh thu và tăng giá trị mỗi đơn: đổi suất, danh sách chờ, bán thêm combo, gói sản phẩm, gói thành viên.
8. Vận hành rạp có quy trình: chia doanh thu với nhà phát hành, hóa đơn VAT, chốt ngày, xử lý sự cố phòng chiếu, hỗ trợ khách.

## 2. Những thay đổi cốt lõi trên hệ thống hiện có

| Thành phần hiện có | Thay đổi |
|---|---|
| Booking (đặt vé) | Thay bằng **Order** (đơn hàng) gồm nhiều dòng hàng, có thể chứa vé, combo, thẻ quà tặng, phí. |
| Payment (thanh toán) | Thay bằng **sổ giao dịch thanh toán**, một đơn có nhiều giao dịch thu và hoàn. |
| Giá ghế cố định lúc tạo suất | Thay bằng **bộ quy tắc giá**, giá được tính lúc khách chọn ghế và khóa vào đơn. |
| Điểm loyalty dùng chung cho xếp hạng và tích lũy | Tách thành **điểm xếp hạng** và **số dư điểm đổi quà**, ghi bằng sổ cái. |
| Báo cáo tính trực tiếp từ bảng thanh toán | Chuyển sang **bảng tổng hợp theo ngày**, cập nhật định kỳ. |
| Xử lý xác nhận đơn gom nhiều việc vào một hàm | Tách thành các **sự kiện nghiệp vụ**, mỗi bộ xử lý lo một việc. |
| Dữ liệu demo | Khởi tạo lại bằng bộ dữ liệu mẫu mới, không chuyển đổi dữ liệu cũ. |

Giữ nguyên: cơ chế giữ ghế nguyên tử, cập nhật ghế thời gian thực, xử lý thông báo từ cổng thanh toán (kiểm tra chữ ký, số tiền, chống xử lý trùng), tác vụ nền, hạ tầng nhập/xuất file, check-in bằng QR. Các phần này được điều chỉnh để làm việc với mô hình đơn hàng mới.

---

## 3. Quy ước chung cho toàn hệ thống

### 3.1 Thứ tự tính giá của một đơn

1. Giá niêm yết của từng vé: giá cơ sở của suất × hệ số loại ghế × hệ số loại vé × các quy tắc giá động đang hiệu lực.
2. Giá combo theo bảng giá combo.
3. Ưu đãi hạng thành viên (theo phần trăm của hạng, áp dụng trên tiền vé).
4. Mã khuyến mãi (áp dụng sau ưu đãi hạng, theo quy tắc chồng ưu đãi ở mục 6).
5. Tổng phải thanh toán = tổng các dòng hàng − các khoản ưu đãi.
6. Thẻ quà tặng và các phương thức thanh toán là **cách trả tiền**, không phải giảm giá, nên không làm giảm doanh thu.

Mọi khoản tiền làm tròn đến đồng. Hệ thống lưu bảng chi tiết giá của từng đơn (giá gốc, từng khoản ưu đãi, số phải trả) để tra cứu và báo cáo.

### 3.2 Ghi nhận doanh thu

- **Thu tiền** và **doanh thu** là hai khái niệm tách biệt trong báo cáo.
- Doanh thu vé được ghi nhận theo **ngày chiếu**, đã trừ giảm giá và hoàn tiền.
- Combo bán kèm vé ghi nhận cùng ngày chiếu. Combo bán riêng tại quầy ghi nhận theo ngày bán.
- Bán thẻ quà tặng là **khoản phải trả**; chỉ chuyển thành doanh thu khi thẻ được dùng để thanh toán.
- Tiền đặt cọc thuê phòng là khoản nhận trước, chuyển thành doanh thu vào ngày sự kiện diễn ra.

### 3.3 Khóa giá và giữ chỗ

- Khi khách giữ ghế, giá từng vé được **khóa vào đơn**. Giá thay đổi sau đó không ảnh hưởng đơn đang chờ thanh toán.
- Lượt dùng mã khuyến mãi được giữ chỗ cùng lúc với ghế. Hết thời gian giữ hoặc hủy đơn chưa thanh toán thì ghế và lượt dùng mã được nhả cùng nhau.

### 3.4 Sự kiện nghiệp vụ

Hệ thống phát các sự kiện: đơn được xác nhận, đơn bị hủy, suất chiếu kết thúc. Hai nhóm xử lý:

- **Trong cùng giao dịch với việc xác nhận đơn** (vì sai sẽ lệch tiền hoặc tồn kho): chốt lượt dùng mã, trừ kho, trừ thẻ quà, ghi điểm.
- **Chạy nền sau đó**: gửi email, cập nhật tiến độ thử thách, thưởng giới thiệu, cập nhật dữ liệu gợi ý.

Có cơ chế gửi lại khi xử lý nền thất bại, nên không mất email hay thưởng.

### 3.5 Nhật ký thao tác (audit log)

Ghi lại các thao tác nhạy cảm của quản trị: đổi quy tắc giá, tạo hoặc sửa khuyến mãi, điều chỉnh kho, điều chỉnh điểm, hoàn tiền thủ công, báo giá thuê phòng. Mỗi dòng gồm người thực hiện, thời điểm, đối tượng, giá trị trước và sau.

### 3.6 Bộ nhớ đệm

Các dữ liệu ít thay đổi và được đọc nhiều (quy tắc giá, khuyến mãi đang hoạt động, bảng hạng thành viên, cấu hình rạp) được lưu trong bộ nhớ của ứng dụng và tự làm mới khi quản trị chỉnh sửa.

---

## 4. Hạng mục 1 – Nền tảng đơn hàng và thanh toán

**Mục tiêu:** tạo mô hình dữ liệu đủ rộng để mọi tính năng bên dưới dùng chung.

### 4.1 Đơn hàng

- **Đơn hàng:** mã đơn, kênh bán (online, quầy), khách hàng (có tài khoản hoặc khách vãng lai), trạng thái (chờ thanh toán, đã xác nhận, đã hủy, hết hạn), thời điểm tạo, thời hạn thanh toán, nhân viên tạo (với đơn quầy), các khoản tiền tổng hợp.
- **Dòng hàng:** có bốn loại.
  - Vé xem phim: gắn với một ghế của một suất, có loại vé, giá đã khóa, mã QR, trạng thái check-in.
  - Combo bắp nước: gắn với combo, số lượng, đơn giá, trạng thái đã nhận hay chưa.
  - Thẻ quà tặng: gắn với thẻ được phát hành từ đơn này.
  - Phí: dùng cho các khoản phụ như thuê phòng.
- **Khoản điều chỉnh giá:** loại (ưu đãi hạng thành viên, mã khuyến mãi), số tiền, tham chiếu đến hạng hoặc mã tương ứng.
- Đơn không bắt buộc có suất chiếu hay ghế. Nhờ đó quầy bán được combo riêng, và đơn thẻ quà hoặc đơn thuê phòng cùng dùng khung đơn này.

### 4.2 Sổ giao dịch thanh toán

- Mỗi giao dịch gồm: đơn liên quan, loại (thu, hoàn), phương thức (VNPay, MoMo, Stripe, tiền mặt, thẻ quà tặng, chuyển khoản), số tiền, phí cổng thanh toán, mã giao dịch của hệ thống, mã giao dịch của cổng, trạng thái, thời điểm, và với tiền mặt thì số tiền nhận và tiền thối.
- Một đơn có thể có nhiều giao dịch thu (ví dụ trả một phần bằng thẻ quà tặng, phần còn lại qua VNPay; hoặc đặt cọc rồi thanh toán nốt).
- Hoàn tiền là các giao dịch riêng, nên hoàn một phần nhiều lần vẫn truy vết được.
- Đơn được xác nhận khi tổng các khoản thu thành công đủ số phải trả.
- Thêm phương thức chuyển khoản cho đơn thuê phòng, do quản trị đánh dấu đã nhận tiền.

### 4.3 Luồng đặt vé sau khi nâng cấp

1. Khách chọn ghế, loại vé, combo, nhập mã khuyến mãi (nếu có).
2. Hệ thống giữ ghế, khóa giá, giữ lượt dùng mã, tạo đơn ở trạng thái chờ thanh toán.
3. Khách có thể áp dụng hoặc gỡ mã khuyến mãi, hoặc dùng thẻ quà tặng ở bước thanh toán. Hệ thống tính lại số phải trả.
4. Khách thanh toán. Khi đủ tiền, đơn được xác nhận: phát vé và QR, trừ kho combo, chốt lượt dùng mã, ghi điểm, gửi email.
5. Hết hạn mà chưa đủ tiền: đơn hết hạn, nhả ghế, nhả lượt dùng mã.

Quầy dùng cùng luồng, với thanh toán tiền mặt hoặc thẻ, và có thể bán chỉ combo.

### 4.4 Hủy và hoàn tiền

- Giữ chính sách hiện có: phần trăm hoàn và hạn hủy theo cấu hình rạp.
- Số hoàn tính trên số tiền khách thực trả.
- Khi đơn đã thanh toán bị hủy, lượt dùng mã khuyến mãi không được hoàn lại; kho combo chưa nhận được hoàn lại.
- Khi hủy, hệ thống ghi nhận **lý do** (khách đổi ý, trùng đơn, lỗi thanh toán, rạp hủy suất, khác) và **người hủy** (khách, quản trị, hệ thống) để làm báo cáo tỷ lệ hủy.
- Rạp hủy suất: hoàn tiền tự động như hiện tại, nhưng ghi qua sổ giao dịch mới.

### 4.5 Thay đổi trên phần hiện có

- Điều chỉnh các luồng: giữ ghế, thanh toán, quầy, xác nhận đơn, hủy đơn, xử lý thông báo cổng thanh toán, hủy suất, trợ lý AI, sơ đồ ghế.
- Điều chỉnh màn hình khách (chọn ghế, thanh toán, lịch sử đơn, vé), màn hình quầy, màn hình quản trị đơn và thanh toán.

---

## 5. Hạng mục 2 – Quy tắc giá và loại vé

**Mục tiêu:** giá thay đổi theo nhu cầu, tăng giờ cao điểm, giảm giờ vắng để lấp ghế.

### 5.1 Loại vé

- Danh mục loại vé do quản trị quản lý, ví dụ: người lớn, học sinh – sinh viên, trẻ em, người cao tuổi.
- Mỗi loại vé có hệ số giá, và có thể yêu cầu điều kiện (xuất trình thẻ, giới hạn độ tuổi phim, chỉ một số khung giờ hoặc ngày).
- Khách chọn loại vé cho từng ghế khi đặt. Quầy kiểm tra điều kiện trực tiếp.

### 5.2 Quy tắc giá

Mỗi quy tắc gồm: tên, loại quy tắc, điều kiện áp dụng, cách điều chỉnh (nhân hệ số hoặc cộng/trừ số tiền), độ ưu tiên, thời gian hiệu lực, phạm vi (tất cả phòng hoặc một số phòng/định dạng, tất cả phim hoặc một số phim). Các loại quy tắc:

- **Khung giờ:** ví dụ giờ vàng buổi tối tăng, suất sáng giảm.
- **Thứ trong tuần:** cuối tuần tăng, giữa tuần giảm.
- **Ngày lễ và sự kiện:** danh sách ngày đặc biệt do quản trị khai báo.
- **Mức lấp đầy:** suất đã bán vượt ngưỡng thì tăng giá theo bậc; suất còn rất vắng khi gần giờ chiếu thì giảm theo bậc.

Hệ thống quy định:

- Cách kết hợp nhiều quy tắc cùng lúc (cộng dồn hay lấy quy tắc ưu tiên cao nhất) cho từng loại.
- **Trần tăng và sàn giảm** (ví dụ không vượt ±30% giá cơ sở), để tránh giá bất thường.
- Giá tối thiểu không thấp hơn mức quản trị cấu hình.

### 5.3 Hành vi

- Sơ đồ ghế hiển thị giá đã tính theo quy tắc, theo loại ghế và loại vé.
- Khi khách giữ ghế, giá được tính lại ở thời điểm đó và khóa vào đơn. Nếu giá khác với giá khách thấy trước đó, hệ thống thông báo rõ trước khi tiếp tục.
- Giá hiển thị cho khách, vé, hóa đơn và báo cáo đều dùng cùng một nguồn tính giá.

### 5.4 Màn hình quản trị

- Danh sách và chỉnh sửa loại vé, quy tắc giá, lịch ngày lễ.
- **Xem trước giá:** chọn một suất, hệ thống hiển thị bảng giá từng loại ghế và loại vé, kèm danh sách quy tắc đang tác động.
- Lịch sử thay đổi quy tắc (từ nhật ký thao tác).

---

## 6. Hạng mục 3 – Khuyến mãi

**Mục tiêu:** công cụ marketing đầy đủ, kiểm soát được chi phí và lạm dụng.

### 6.1 Thông tin một khuyến mãi

- Mã (duy nhất, không phân biệt hoa thường), tên, mô tả hiển thị cho khách.
- Loại ưu đãi: giảm theo phần trăm (có trần số tiền giảm tối đa) hoặc giảm số tiền cố định.
- Điều kiện: giá trị đơn tối thiểu, thời gian hiệu lực, kênh bán (online, quầy hoặc cả hai), phim hoặc suất cụ thể, thứ trong tuần, hạng thành viên được áp dụng (có thể chọn nhiều hạng; để trống là mọi khách).
- Phạm vi giảm: chỉ tiền vé, chỉ combo, hoặc toàn đơn.
- Giới hạn: tổng số lượt dùng, số lượt tối đa mỗi khách, ngân sách tổng của chiến dịch.
- Độ ưu tiên và cờ **không dùng chung** với ưu đãi khác.
- Loại: mã nhập tay, hoặc chiến dịch tự động áp dụng cho đơn đủ điều kiện, hoặc mã cá nhân dùng một lần (phát từ chương trình đổi điểm, giới thiệu, thử thách).

### 6.2 Quy tắc áp dụng

- Ưu đãi hạng thành viên luôn tính trước; sau đó mới đến mã khuyến mãi.
- Khi nhiều ưu đãi cùng đủ điều kiện, hệ thống áp dụng theo độ ưu tiên và tôn trọng cờ không dùng chung; nếu không cho dùng chung thì lấy ưu đãi có lợi hơn cho khách.
- Tổng giảm không vượt quá giá trị phạm vi áp dụng; đơn không bao giờ có giá âm.
- Khách chỉ dùng mã trên đơn của mình. Mã cá nhân gắn đúng tài khoản được cấp.

### 6.3 Vòng đời lượt dùng

| Thời điểm | Trạng thái lượt dùng |
|---|---|
| Khách áp mã trên đơn chờ thanh toán | Giữ chỗ |
| Đơn được xác nhận | Đã dùng |
| Đơn hết hạn hoặc hủy khi chưa thanh toán, hoặc khách gỡ mã | Nhả lại |
| Đơn đã thanh toán rồi bị hủy | Giữ nguyên là đã dùng |

Giữ chỗ lượt dùng là thao tác nguyên tử, nên mã giới hạn số lượt không bị dùng vượt khi nhiều khách cùng áp.

### 6.4 Màn hình

- Khách: ô nhập mã ở bước thanh toán, hiển thị số tiền giảm hoặc lý do mã không hợp lệ (hết hạn, chưa đủ điều kiện, hết lượt, không thuộc hạng...), danh sách mã đang có trong tài khoản.
- Quầy: nhập mã cho khách trực tiếp.
- Quản trị: tạo, sửa, tạm dừng, xóa mềm khuyến mãi; xem và xuất lịch sử dùng; tạo hàng loạt mã cá nhân.

### 6.5 Báo cáo khuyến mãi

Theo từng khuyến mãi: số lượt dùng, tổng tiền giảm đã chi, doanh thu của các đơn dùng mã, mức sử dụng so với giới hạn và ngân sách, hiệu quả theo kênh và khung giờ.

---

## 7. Hạng mục 4 – Kho bắp nước và bán combo tại quầy

**Mục tiêu:** quản lý tồn kho thực, biết giá vốn từng combo, bán combo không cần vé.

### 7.1 Dữ liệu

- **Sản phẩm kho:** mã, tên, đơn vị tính, giá vốn hiện hành, ngưỡng cảnh báo sắp hết, trạng thái.
- **Công thức combo:** mỗi combo gồm danh sách sản phẩm kho và số lượng tiêu hao cho một combo.
- **Phiếu nhập kho:** nhà cung cấp, ngày nhập, số lượng, đơn giá nhập. Mỗi lần nhập cập nhật giá vốn bình quân của sản phẩm.
- **Phiếu kiểm kê:** số liệu đếm thực tế, chênh lệch với sổ sách được ghi thành điều chỉnh.
- **Phiếu hao hụt:** hủy hàng hết hạn, hỏng, đổ vỡ, có lý do.
- **Sổ cái kho:** mọi biến động (nhập, bán, hoàn, kiểm kê, hao hụt) đều ghi một dòng, tồn kho là kết quả cộng dồn.

### 7.2 Hành vi

- Khi đơn được xác nhận, hệ thống trừ kho theo công thức của từng combo bán ra. Trừ kho là thao tác nguyên tử có điều kiện đủ hàng, nên không bán vượt tồn.
- Combo mà nguyên liệu không đủ sẽ tự ẩn khỏi danh sách bán (online và quầy) và hiện lại khi có hàng.
- Khi đơn bị hủy: kho được hoàn lại nếu combo chưa được nhận.
- **Trạng thái đã nhận combo:** quầy đánh dấu khi khách lấy combo (quét QR đơn hoặc nhập mã đơn). Báo cáo phân biệt combo đã bán và combo đã giao.
- **Bán combo riêng tại quầy:** nhân viên tạo đơn chỉ có combo, thanh toán ngay, không cần chọn suất.
- **Cảnh báo sắp hết:** tác vụ nền kiểm tra định kỳ, khi tồn chạm ngưỡng thì gửi thông báo cho quản trị và hiển thị trên màn hình kho.
- **Giá vốn combo** = tổng giá vốn các sản phẩm theo công thức; lưu lại tại thời điểm bán để báo cáo lợi nhuận không đổi khi giá nhập đổi về sau.

### 7.3 Màn hình quản trị

- Danh mục sản phẩm kho và công thức combo.
- Nhập kho, kiểm kê, hao hụt, xem sổ cái theo sản phẩm và khoảng thời gian.
- Bảng tồn kho với cảnh báo, báo cáo giá trị tồn.
- Màn bán combo tại quầy.

---

## 8. Hạng mục 5 – Báo cáo và phân tích

**Mục tiêu:** số liệu nhanh, đúng, đủ để ra quyết định về lịch chiếu, giá, khuyến mãi, kho.

### 8.1 Dữ liệu tổng hợp

Hệ thống duy trì các bảng tổng hợp theo ngày, cập nhật bằng tác vụ nền định kỳ và khi suất chiếu kết thúc:

- **Tổng hợp theo suất chiếu:** sức chứa bán được (không tính lối đi, ghế khóa, ghế hỏng), số vé bán, doanh thu vé gộp, giảm giá, hoàn tiền, doanh thu combo, số đơn hủy.
- **Tổng hợp thanh toán:** theo ngày, kênh, phương thức: số giao dịch, tiền thu, tiền hoàn, phí cổng.
- **Tốc độ bán của suất:** tỷ lệ đã bán tại các mốc trước giờ chiếu (ví dụ 24 giờ, 3 giờ), dùng làm cơ sở cho quy tắc giá theo lấp đầy và gợi ý lịch chiếu.

Báo cáo đọc từ các bảng này, nên tải nhanh kể cả khi dữ liệu lớn.

### 8.2 Dashboard nâng cao

- **Tỷ lệ lấp đầy** theo phòng, khung giờ, phim, ngày trong tuần; so sánh với kỳ trước.
- **Bản đồ nhiệt ghế:** tần suất từng ghế được chọn trong từng phòng, lọc theo khoảng thời gian và loại phim.
- **Doanh thu combo:** theo combo, theo kênh, tỷ lệ đơn có combo, giá trị combo trung bình mỗi đơn.
- **Tỷ lệ hủy:** theo lý do, theo người hủy, theo khoảng thời gian từ lúc hủy đến giờ chiếu, theo phim.
- Các chỉ số hiện có (doanh thu hôm nay, vé bán, lấp đầy, top phim) được giữ lại.

### 8.3 Phân tích lấp đầy và gợi ý lịch chiếu

- Báo cáo theo phim × phòng × khung giờ: số suất, lấp đầy trung bình, doanh thu mỗi suất, doanh thu mỗi ghế mỗi giờ phòng.
- **Gợi ý cho quản trị**, kèm lý do dễ hiểu:
  - Phim lấp đầy cao liên tục thì đề xuất tăng suất, chỉ ra phòng và khung giờ còn trống phù hợp.
  - Phim lấp đầy thấp liên tục thì đề xuất giảm suất hoặc chuyển sang phòng nhỏ hơn hay khung giờ khác.
  - Phòng và khung giờ bị bỏ trống nhiều thì đề xuất lấp bằng phim phù hợp.
- Ngưỡng đánh giá (cao, thấp, số suất tối thiểu để kết luận, số ngày nhìn lại) cấu hình được ở cài đặt rạp.
- Nút **áp dụng đề xuất**: chuyển đề xuất đã chọn sang chức năng xếp lịch tự động hiện có ở dạng bản nháp để quản trị duyệt trước khi tạo suất.
- Suất chiếu riêng (thuê phòng) không được tính vào các phân tích này.

### 8.4 Báo cáo tài chính

- **Doanh thu theo kênh** (online, quầy) và **theo phương thức thanh toán**, tách vé, combo, thẻ quà tặng.
- **Giảm giá:** tổng ưu đãi hạng, tổng khuyến mãi.
- **Hoàn tiền và hủy:** số lượng, số tiền, tỷ lệ so với doanh thu.
- **Phí cổng thanh toán** và doanh thu thực nhận sau phí.
- **Lợi nhuận theo phim:** doanh thu vé trừ phần chia cho nhà phát hành. Tỷ lệ chia theo hợp đồng của từng phim (xem mục 20).
- **Lợi nhuận combo:** doanh thu combo trừ giá vốn tại thời điểm bán.
- **Khoản phải trả thẻ quà tặng:** tổng giá trị thẻ đã bán chưa dùng.
- Báo cáo doanh thu hiện có được sửa để tổng vé và combo khớp với tổng doanh thu sau giảm giá.
- Mọi báo cáo xuất được Excel và PDF.

### 8.5 Đối soát cổng thanh toán

- Quản trị nhập file giao dịch (sao kê) do VNPay, MoMo tải về; Stripe nhập từ dữ liệu giao dịch của cổng.
- Hệ thống khớp từng dòng với sổ giao dịch theo mã giao dịch, và phân loại kết quả:
  - Khớp.
  - Có ở cổng nhưng không có ở hệ thống.
  - Có ở hệ thống nhưng không có ở cổng.
  - Lệch số tiền.
  - Lệch trạng thái.
- Dòng bất thường được liệt kê để xử lý, kèm ghi chú và người xử lý. Phí cổng trong file được cập nhật vào giao dịch tương ứng.
- Mỗi lần đối soát là một phiên có thống kê tổng, lưu lại để tra cứu.

---

## 9. Hạng mục 6 – Loyalty mạnh hơn

**Mục tiêu:** giữ chân khách bằng đổi điểm, thử thách và giới thiệu bạn bè.

### 9.1 Hai loại điểm

- **Điểm xếp hạng:** vẫn tính từ chi tiêu trong cửa sổ thời gian cấu hình sẵn, quyết định hạng thành viên và mức ưu đãi hạng. Đổi quà không làm giảm điểm này.
- **Số dư điểm đổi quà:** cộng khi tích điểm, trừ khi đổi quà.

### 9.2 Sổ cái điểm

- Mỗi lần cộng điểm là một **lô** có ngày nhận, hạn dùng, số điểm còn lại.
- Đổi quà tiêu hao lô cũ nhất trước, nên điểm hết hạn được tính đúng.
- Các loại biến động: tích từ đơn, thưởng (thử thách, giới thiệu), đổi quà, hết hạn, thu hồi khi đơn bị hủy, điều chỉnh của quản trị.
- Khi đơn bị hủy, điểm tích từ đơn đó bị thu hồi; nếu khách đã tiêu mất, số dư có thể âm và được trừ vào lần tích tiếp theo.

### 9.3 Đổi điểm lấy quà

- Danh mục quà do quản trị quản lý: tên, số điểm cần, loại (vé miễn phí, combo miễn phí, mã giảm giá), số lượng còn, thời gian hiệu lực.
- Khi khách đổi, hệ thống trừ điểm và cấp một **mã khuyến mãi cá nhân dùng một lần** gắn tài khoản, có hạn dùng. Khách dùng mã ở bước thanh toán như mọi mã khác.

### 9.4 Thử thách theo tháng

- Quản trị tạo thử thách: tên, kỳ áp dụng, điều kiện (ví dụ xem 3 phim khác nhau trong tháng), phần thưởng (điểm hoặc mã quà).
- Tiến độ của khách được cập nhật khi suất chiếu kết thúc và vé đã được check-in. Phim đặt nhưng không đến xem không được tính.
- Khi hoàn thành, hệ thống cấp thưởng tự động và gửi thông báo.
- Mỗi khách chỉ nhận thưởng một lần cho mỗi thử thách.

### 9.5 Giới thiệu bạn bè

- Mỗi khách có mã giới thiệu riêng.
- Người mới nhập mã khi đăng ký. Thưởng cho cả hai bên được cấp khi **đơn đầu tiên của người được giới thiệu được xác nhận**, không phải lúc đăng ký.
- Chống lạm dụng: mỗi người chỉ được giới thiệu một lần; không tự giới thiệu; trùng số điện thoại hoặc email với tài khoản đã có không được tính; giới hạn số thưởng giới thiệu tối đa mỗi tháng cho mỗi người.

### 9.6 Màn hình

- Khách: số dư, hạng, lịch sử điểm, điểm sắp hết hạn, danh mục quà, thử thách và tiến độ, mã giới thiệu và danh sách người đã giới thiệu.
- Quản trị: quản lý quà, thử thách, điều chỉnh điểm (có ghi nhật ký), báo cáo điểm đã phát, đã đổi, đã hết hạn.

---

## 10. Hạng mục 7 – Thẻ quà tặng (e-gift)

**Mục tiêu:** bán thẻ giá trị để tặng người khác, phù hợp dịp lễ và quà doanh nghiệp.

### 10.1 Thẻ quà tặng

- Thông tin: mã thẻ (chuỗi ngẫu nhiên đủ dài, khó đoán), mệnh giá, số dư, người mua, email người nhận, tên người nhận, lời nhắn, thời điểm gửi, hạn dùng, trạng thái (chờ gửi, đã gửi, đang dùng, đã dùng hết, hết hạn, đã hủy).
- Sổ giao dịch thẻ ghi từng lần phát hành, sử dụng, hoàn lại.

### 10.2 Mua thẻ

- Khách chọn mệnh giá (các mức có sẵn hoặc tự nhập trong khoảng cho phép), nhập thông tin người nhận, lời nhắn, chọn gửi ngay hoặc hẹn ngày giờ.
- Thanh toán như một đơn thường. Khi đơn được xác nhận, thẻ được phát hành.
- Email gửi cho người nhận gồm lời nhắn, mã thẻ và hướng dẫn dùng; người mua nhận email xác nhận.

### 10.3 Dùng thẻ

- Ở bước thanh toán, khách nhập mã thẻ. Thẻ trả một phần hoặc toàn bộ số phải trả.
- Nếu thẻ đủ trả hết, đơn được xác nhận ngay mà không qua cổng thanh toán; nếu chưa đủ, phần còn lại thanh toán bằng phương thức khác.
- Số dư thẻ chỉ bị trừ khi đơn được xác nhận. Đơn được hoàn thì tiền hoàn về lại thẻ.
- Quầy cũng nhận thẻ quà tặng.

### 10.4 An toàn

- Kiểm tra số dư và áp mã có giới hạn số lần thử, khóa tạm khi nhập sai nhiều lần.
- Quản trị có thể hủy thẻ (ví dụ phát hành nhầm) với ghi nhật ký.

### 10.5 Quà doanh nghiệp

- Doanh nghiệp mua nhiều thẻ một lần: tải lên danh sách người nhận (tên, email, mệnh giá, lời nhắn), hệ thống kiểm tra danh sách, tạo một đơn tổng và phát hành, gửi từng thẻ khi đơn được thanh toán.

### 10.6 Màn hình

- Khách: trang mua thẻ quà, xem thẻ đã mua và trạng thái gửi, nhập thẻ khi thanh toán.
- Quản trị: danh sách thẻ, tra cứu theo mã hoặc email, lịch sử sử dụng, hủy thẻ, gửi lại email, báo cáo khoản phải trả.

---

## 11. Hạng mục 8 – Thuê phòng và bán sỉ

**Mục tiêu:** doanh nghiệp, trường học, nhóm bạn thuê suất chiếu riêng; quản trị xử lý yêu cầu và báo giá.

### 11.1 Yêu cầu thuê phòng

- Khách gửi yêu cầu (có thể không cần tài khoản): họ tên, đơn vị, loại đối tượng (doanh nghiệp, trường học, nhóm bạn), liên hệ, ngày giờ mong muốn, khoảng thời gian dự phòng, số người, phim mong muốn (hoặc tự mang nội dung), định dạng phòng, combo, yêu cầu khác.
- Trạng thái: mới, đang xử lý, đã báo giá, khách chấp nhận, khách từ chối, đã đặt cọc, đã thanh toán đủ, hoàn tất, hủy, hết hạn báo giá.

### 11.2 Báo giá

- Giá thuê phòng theo bảng giá thuê (theo phòng, khung giờ, thời lượng), cộng phí theo đầu người nếu có, gói combo, các khoản phụ, trừ chiết khấu của quản trị.
- Báo giá có thời hạn hiệu lực, ghi rõ khoản đặt cọc (phần trăm cấu hình), hạn thanh toán nốt và chính sách hủy.
- Xuất báo giá thành tệp PDF và gửi email cho khách. Mỗi lần sửa báo giá tạo phiên bản mới, giữ lịch sử.
- Khách xem báo giá và bấm chấp nhận hoặc từ chối trên trang theo dõi yêu cầu (truy cập bằng đường dẫn riêng gửi qua email).

### 11.3 Từ yêu cầu đến suất chiếu

- Khi khách chấp nhận và đặt cọc, hệ thống tạo **suất chiếu riêng** trong phòng đã chốt, kiểm tra xung đột với suất khác và thời gian đệm giữa các suất.
- Toàn bộ ghế của suất được khóa cho đơn thuê phòng. Suất riêng không hiển thị trong lịch chiếu công khai và không xuất hiện trong trợ lý AI.
- Suất riêng không được tính vào thống kê lấp đầy, gợi ý lịch chiếu và độ nóng của phim; được tính vào doanh thu và có báo cáo riêng.

### 11.4 Thanh toán và hủy

- Thanh toán nhiều đợt: cọc, sau đó thanh toán nốt trước hạn. Quản trị ghi nhận tiền chuyển khoản; khách cũng có thể trả qua cổng thanh toán.
- Chính sách hủy riêng: hủy sớm hoàn toàn bộ hoặc một phần cọc, hủy muộn mất cọc, theo mốc thời gian cấu hình.
- Khách không thanh toán nốt đúng hạn thì yêu cầu bị hủy và suất riêng được giải phóng.

### 11.5 Màn hình

- Khách: biểu mẫu gửi yêu cầu, trang theo dõi yêu cầu và báo giá.
- Quản trị: bảng theo dõi yêu cầu theo trạng thái, màn soạn báo giá, lịch phòng thể hiện các suất riêng, ghi nhận thanh toán, báo cáo số yêu cầu, tỷ lệ chốt và doanh thu thuê phòng.

---

## 12. Hạng mục 9 – Gợi ý phim cá nhân hóa

**Mục tiêu:** khách thấy phim và suất phù hợp sở thích, tăng tỷ lệ đặt vé.

### 12.1 Cách gợi ý

Hệ thống tính điểm phù hợp giữa khách và từng phim đang chiếu dựa trên:

- **Sở thích của khách:** từ lịch sử vé đã xem (đã check-in hoặc suất đã kết thúc) và đánh giá khách đã viết. Gồm thể loại, diễn viên, đạo diễn; phim xem gần đây và phim được chấm cao có trọng số lớn hơn.
- **Tương đồng giữa các phim theo hành vi:** khách xem phim A thường cũng xem phim B.
- **Độ phổ biến và đánh giá chung:** dùng cho khách mới chưa có lịch sử.
- **Thói quen đặt vé:** khung giờ, định dạng phòng, mức giá khách thường chọn, dùng để xếp suất phù hợp.

Quy tắc loại trừ: bỏ phim khách đã xem; chỉ gợi ý phim đang chiếu và còn suất trong những ngày tới; ưu tiên suất còn ghế.

Mỗi gợi ý kèm lý do ngắn ("vì bạn đã xem phim cùng đạo diễn", "nhiều người xem phim này cũng xem phim kia").

### 12.2 Nơi hiển thị

- Mục "Dành cho bạn" ở trang chủ và trang phim.
- Trợ lý AI có công cụ gợi ý phim: khi khách hỏi "tối nay xem gì", trợ lý gọi công cụ này và diễn đạt lại kết quả, kèm suất chiếu cụ thể.
- Email giới thiệu phim mới phù hợp (chỉ gửi cho khách đồng ý nhận).

### 12.3 Quyền riêng tư

- Khách có thể tắt gợi ý cá nhân hóa và email giới thiệu trong trang cá nhân; khi tắt, hệ thống chỉ hiện danh sách phổ biến chung.

---

# Phần mở rộng – các hạng mục đề xuất thêm

Các hạng mục dưới đây bổ sung cho chín hạng mục trên. Chúng bám vào những thứ rạp thật phải xử lý hằng ngày: giữ doanh thu khi khách đổi ý, tăng giá trị mỗi đơn, làm việc với nhà phát hành, chốt sổ, xử lý sự cố và chăm sóc khách. Mỗi hạng mục dùng lại nền tảng ở hạng mục 1 (đơn hàng, sổ thanh toán, bộ tính giá, sự kiện nghiệp vụ).

---

## 13. Đổi suất chiếu

**Mục tiêu:** khi khách không đi được, cho đổi sang suất khác thay vì hủy và hoàn tiền, giữ lại doanh thu.

- Khách đổi vé của một đơn đã xác nhận sang một suất khác của **cùng phim**, trước hạn đổi (số giờ trước giờ chiếu, cấu hình ở cài đặt rạp).
- Mỗi đơn được đổi tối đa số lần cấu hình (mặc định một lần). Vé đã check-in không được đổi.
- Hệ thống giữ ghế mới, tính giá theo bộ quy tắc giá của suất mới, rồi so với số tiền đã trả cho vé cũ:
  - Suất mới đắt hơn: khách trả thêm phần chênh lệch qua các phương thức thanh toán thông thường.
  - Suất mới rẻ hơn: phần chênh lệch được cấp lại dưới dạng mã quà cá nhân dùng một lần (không hoàn về cổng thanh toán).
- Khi đổi xong: ghế cũ được nhả, vé và mã QR cũ vô hiệu, vé và mã QR mới được phát hành, khách nhận email xác nhận.
- Mã khuyến mãi đã dùng và điểm đã tích của đơn giữ nguyên.
- Lịch sử đơn ghi rõ từng lần đổi. Trong báo cáo, đổi suất được tính riêng, **không tính là hủy** và không tạo hoàn tiền.
- Quầy hỗ trợ khách đổi suất trực tiếp.

---

## 14. Danh sách chờ và báo khi mở bán

**Mục tiêu:** không để mất khách ở những suất hết chỗ, và đo được nhu cầu thật của từng phim.

### 14.1 Danh sách chờ cho suất hết ghế

- Suất không còn ghế phù hợp: khách đăng ký chờ với số ghế cần.
- Khi có ghế được nhả (hết hạn giữ, đơn bị hủy), hệ thống xếp theo thứ tự đăng ký và thông báo cho khách đứng đầu danh sách. Khách này được ưu tiên giữ ghế trong một khoảng ngắn (cấu hình, ví dụ mười phút); hết thời gian đó mới mở cho người kế tiếp và cho khách bình thường.
- Danh sách chờ tự đóng khi suất bắt đầu. Khách có thể rút đăng ký bất kỳ lúc nào.

### 14.2 Báo khi mở bán

- Khách đánh dấu quan tâm một phim sắp chiếu. Khi phim có suất chiếu đầu tiên mở bán, hệ thống gửi thông báo.
- Quản trị xem số người quan tâm theo từng phim, làm một căn cứ để quyết định số suất mở đầu.

---

## 15. Bán thêm combo sau khi đặt vé và nhắc lịch chiếu

**Mục tiêu:** tăng giá trị trung bình mỗi đơn bằng combo đặt trước, giảm tình trạng khách quên suất.

- Sau khi đặt vé thành công, trang xác nhận và email gợi ý thêm combo đặt trước, nhận tại quầy. Combo gợi ý theo số vé (hai vé gợi ý combo đôi) và lịch sử mua của khách. Có thể áp giá ưu đãi cho combo đặt trước, còn hiệu lực đến trước giờ chiếu.
- Combo thêm vào là **đơn bổ sung** liên kết với đơn gốc, thanh toán riêng; vé và ghế của đơn gốc không bị ảnh hưởng.
- Trước giờ chiếu (số giờ cấu hình, ví dụ ba giờ), hệ thống gửi nhắc lịch kèm mã QR, địa chỉ rạp, phòng, giờ mở check-in, và hướng dẫn nhận combo.
- Báo cáo: tỷ lệ khách mua thêm combo sau khi đặt vé, doanh thu từ combo đặt trước, so sánh với combo mua tại quầy.

---

## 16. Gói sản phẩm (bundle)

**Mục tiêu:** bán các gói quen thuộc của rạp thật như "vé đôi + combo đôi" hoặc "gói gia đình".

- Quản trị tạo gói gồm nhiều thành phần (số vé theo từng loại vé, các combo) với **giá gói** thấp hơn tổng giá lẻ.
- Điều kiện của gói: áp dụng cho suất, phim, ngày, khung giờ nào; hạn mức số gói mỗi ngày; thời gian hiệu lực.
- Khi khách mua gói, hệ thống chọn ghế như đặt vé thường và tách giá gói về từng dòng hàng theo tỷ lệ giá lẻ. Cách tách này làm cho báo cáo, hoàn tiền một phần và trừ kho đều đúng theo thành phần.
- Gói hiển thị rõ mức tiết kiệm so với mua lẻ ở trang chọn suất.
- Báo cáo: số gói bán, doanh thu gói, so sánh doanh thu với trường hợp khách mua lẻ.

---

## 17. Gói thành viên trả phí

**Mục tiêu:** tạo nguồn doanh thu định kỳ và tăng tần suất đến rạp.

- Quản trị tạo gói theo tháng hoặc quý: giá, thời hạn, quyền lợi. Quyền lợi có thể là: giảm phần trăm cho vé, số vé miễn phí mỗi tháng (không cộng dồn sang tháng sau), giảm giá combo, quyền đặt sớm cho suất đặc biệt.
- Khách mua gói như một đơn thường. Hết hạn, hệ thống nhắc gia hạn; việc gia hạn do khách chủ động thanh toán.
- Quyền lợi áp dụng ngay trong bộ tính giá, sau ưu đãi hạng thành viên và trước mã khuyến mãi; quy tắc chồng ưu đãi quy định rõ gói có dùng chung được với mã hay không.
- Vé miễn phí được cấp qua sổ riêng, trừ khi dùng, ghi rõ vé nào dùng vào đơn nào. Có giới hạn số vé miễn phí trên mỗi suất để tránh chia sẻ tài khoản.
- Doanh thu của gói được ghi nhận **chia đều theo từng tháng hiệu lực**, không ghi một lần lúc bán.
- Báo cáo: số hội viên đang hoạt động, tỷ lệ gia hạn, giá trị quyền lợi đã sử dụng so với tiền gói, lợi nhuận so với việc bán vé lẻ.

---

## 18. Phân khúc khách hàng và chiến dịch

**Mục tiêu:** chăm sóc khách có chủ đích và đo được hiệu quả.

### 18.1 Phân khúc

Hệ thống tự phân nhóm khách theo các chỉ số cập nhật định kỳ:

- Mức độ hoạt động: khách mới, khách thường xuyên, khách lâu chưa quay lại (số ngày cấu hình).
- Giá trị: chi tiêu cao, tần suất cao (chấm theo ba chỉ số gần đây, tần suất, chi tiêu) và giá trị vòng đời ước tính.
- Sở thích: nhóm thích một thể loại hoặc một định dạng phòng.
- Tình trạng điểm: khách có điểm sắp hết hạn, khách sắp lên hạng.

Quản trị cũng tự tạo phân khúc theo điều kiện kết hợp.

### 18.2 Chiến dịch

- Quản trị chọn phân khúc, soạn nội dung email hoặc thông báo, chọn kèm mã khuyến mãi cá nhân (mỗi khách một mã), hẹn thời điểm gửi.
- Chỉ gửi cho khách đã đồng ý nhận thông tin; mỗi email có liên kết hủy đăng ký.
- Đo kết quả theo số khách dùng mã và doanh thu phát sinh từ chiến dịch.

### 18.3 Giữ chân khách

- Báo cáo giữ chân theo nhóm khách đăng ký cùng tháng: bao nhiêu phần trăm quay lại đặt vé ở tháng thứ nhất, thứ hai, thứ ba.

---

## 19. Đánh giá xác thực và khảo sát sau khi xem

**Mục tiêu:** đánh giá đáng tin hơn và có dữ liệu chất lượng dịch vụ.

- Chỉ khách có vé đã xem (đã check-in hoặc suất đã kết thúc) mới được đánh giá phim, và đánh giá gắn nhãn "đã xem".
- Sau suất chiếu một đến ba giờ, hệ thống gửi email mời đánh giá. Đánh giá hợp lệ được thưởng một lượng điểm nhỏ.
- Đánh giá có nhãn "đã xem" được tính vào điểm trung bình của phim.
- Khảo sát nhanh về trải nghiệm tại rạp, chấm từ một đến năm cho phòng, âm thanh, hình ảnh, vệ sinh, dịch vụ quầy.
- Báo cáo chất lượng theo phòng và theo thời gian. Điểm của một phòng thấp liên tục thì hiện cảnh báo cho quản trị.
- Dữ liệu đánh giá xác thực cũng được dùng làm tín hiệu cho gợi ý phim (hạng mục 9).

---

## 20. Nhà phát hành và hợp đồng phim

**Mục tiêu:** quản lý đúng cách rạp chia doanh thu với nhà phát hành. Hạng mục này thay thế tỷ lệ chia cố định trên từng phim đã nêu ở mục 8.4.

- **Nhà phát hành:** tên, thông tin liên hệ, mã số thuế; mỗi phim gắn với một nhà phát hành.
- **Hợp đồng phim:** tỷ lệ chia doanh thu cho nhà phát hành theo **từng tuần chiếu** (ví dụ tuần đầu cao hơn các tuần sau), cách tính (trên doanh thu vé trước hay sau thuế), mức bảo đảm tối thiểu nếu có, thời gian hiệu lực.
- **Đối soát nhà phát hành** theo kỳ (tuần hoặc tháng): với từng phim, hệ thống tính doanh thu vé thực thu (sau giảm giá và hoàn tiền) của từng tuần chiếu, nhân tỷ lệ tương ứng để ra số phải trả, so với mức bảo đảm tối thiểu.
- Bảng đối soát xuất PDF và Excel để gửi nhà phát hành; có trạng thái (nháp, đã gửi, đã chốt, đã thanh toán) và ghi nhận ngày thanh toán.
- Báo cáo lợi nhuận theo phim ở mục 8.4 dùng số phải trả này.
- Không tính suất thuê phòng riêng vào đối soát, trừ khi hợp đồng quy định khác.

---

## 21. Chống giữ ghế ảo và hạn chế lạm dụng

**Mục tiêu:** bảo vệ ghế khỏi việc giữ rồi bỏ, và bảo vệ khuyến mãi khỏi bị lạm dụng.

- Hệ thống đếm số đơn bị hết hạn liên tiếp của từng khách. Vượt ngưỡng cấu hình, khách bị rút ngắn thời gian giữ ghế hoặc bị giới hạn số đơn chờ thanh toán cùng lúc, trong một khoảng thời gian.
- Giới hạn số ghế giữ đồng thời và số đơn chờ của mỗi khách; khách vãng lai bị giới hạn theo thiết bị và địa chỉ mạng.
- Khi có dấu hiệu bất thường (nhiều lần giữ rồi bỏ trong thời gian ngắn, nhiều lần thanh toán thất bại), hệ thống yêu cầu xác minh bổ sung trước khi tiếp tục.
- Chống lạm dụng ưu đãi: nhiều tài khoản dùng chung số điện thoại hoặc thiết bị không được cùng hưởng ưu đãi dành cho khách mới hoặc thưởng giới thiệu.
- Quản trị xem danh sách khách đang bị hạn chế, lý do, thời hạn, và có thể gỡ hạn chế. Mọi hạn chế được ghi vào nhật ký.

---

## 22. Đóng phòng bảo trì và xử lý suất bị ảnh hưởng

**Mục tiêu:** khi một phòng hỏng hoặc bảo trì, quản trị xử lý nhanh, khách được thông báo và hoàn tiền đúng.

- Quản trị đặt phòng ở trạng thái **đóng** trong một khoảng thời gian, kèm lý do. Hệ thống không cho tạo thêm suất trong khoảng đó.
- Hệ thống liệt kê các suất đã có bị ảnh hưởng, số đơn và số khách liên quan.
- Với mỗi suất, quản trị chọn:
  - **Chuyển sang phòng khác** cùng giờ nếu phòng đó trống và đủ sức chứa. Hệ thống ánh xạ ghế sang phòng mới, giữ nguyên đơn, gửi thông báo đổi phòng cho khách.
  - **Hủy suất**, dùng luồng hủy suất hiện có: hoàn tiền tự động và thông báo khách.
- Lịch phòng hiển thị các khoảng bảo trì.
- Ghế hỏng tạm thời được khóa theo từng ghế, không tính vào sức chứa bán được.

---

## 23. Hóa đơn VAT theo yêu cầu

**Mục tiêu:** đáp ứng khách doanh nghiệp cần hóa đơn.

- Khách yêu cầu xuất hóa đơn cho đơn đã thanh toán trong thời hạn cấu hình: nhập mã số thuế, tên đơn vị, địa chỉ, email nhận. Thông tin được lưu thành hồ sơ xuất hóa đơn để dùng lại.
- Giá hiển thị cho khách đã gồm VAT. Thuế suất cấu hình theo loại dòng hàng (vé, combo, thẻ quà tặng); hệ thống tách giá trước thuế và tiền thuế để dùng cho hóa đơn và báo cáo.
- Yêu cầu hóa đơn có trạng thái: chờ xuất, đã xuất, từ chối (kèm lý do). Quản trị cập nhật số hóa đơn và đính kèm tệp; khách tải hóa đơn trong lịch sử đơn.
- Đơn đã xuất hóa đơn mà sau đó bị hoàn tiền: hệ thống đánh dấu cần điều chỉnh hóa đơn và nhắc quản trị xử lý.
- Báo cáo thuế: doanh thu trước thuế, thuế phải nộp theo kỳ, theo loại dòng hàng.

---

## 24. Hỗ trợ khách hàng và hoàn tiền ngoại lệ có duyệt

**Mục tiêu:** xử lý sự cố có quy trình, có lịch sử, kiểm soát được rủi ro hoàn tiền.

### 24.1 Yêu cầu hỗ trợ

- Khách gửi yêu cầu gắn với một đơn: không nhận được email vé, bị trừ tiền nhưng đơn chưa xác nhận, muốn hoàn tiền ngoài chính sách, góp ý về suất chiếu, v.v.
- Mỗi yêu cầu có loại, trạng thái (mới, đang xử lý, chờ khách phản hồi, đã giải quyết), người phụ trách, hạn xử lý mong muốn, lịch sử trao đổi. Khách xem tiến độ và trả lời ngay trên yêu cầu.
- Khi đối soát cổng thanh toán (mục 8.5) phát hiện giao dịch đã thu tiền mà đơn chưa xác nhận, hệ thống tự tạo yêu cầu hỗ trợ tương ứng.

### 24.2 Hoàn tiền ngoại lệ

- Nhân viên quầy hoặc quản trị đề xuất hoàn tiền ngoài chính sách, ghi lý do và số tiền.
- Số tiền vượt ngưỡng cấu hình thì cần quản trị duyệt; dưới ngưỡng quản trị có thể duyệt nhanh.
- Hoàn tiền được thực hiện qua sổ giao dịch, ghi nhật ký người đề xuất, người duyệt, lý do.

### 24.3 Báo cáo

- Số yêu cầu theo loại, thời gian xử lý trung bình, tỷ lệ giải quyết trong hạn, tổng tiền hoàn ngoại lệ theo lý do.

---

## 25. Chốt doanh thu cuối ngày

**Mục tiêu:** có con số chính thức mỗi ngày, không sửa lặng lẽ.

- Cuối ngày kinh doanh, hệ thống tạo báo cáo chốt gồm: doanh thu vé, combo, thẻ quà tặng bán và thẻ quà tặng đã dùng, tiền giảm giá, hoàn tiền, thu theo từng phương thức thanh toán, số đơn đang chờ hoặc lỗi.
- **Tiền mặt tại quầy:** tổng thu, tổng hoàn, số tiền phải có theo sổ sách. Quản trị nhập số tiền thực đếm; hệ thống ghi chênh lệch và ghi chú giải thích.
- Sau khi quản trị bấm **chốt ngày**, số liệu của ngày bị khóa. Muốn sửa phải tạo bút toán điều chỉnh có lý do, được ghi nhật ký, và báo cáo luôn thể hiện rõ phần điều chỉnh.
- Báo cáo chốt tự động gửi email cho quản trị và lưu lại để tra cứu theo ngày.

---

## 26. Suất chiếu sự kiện

**Mục tiêu:** bán các suất đặc biệt như chiếu sớm, ra mắt, gặp gỡ người hâm mộ với điều kiện riêng.

- Suất được gắn loại **sự kiện**, có thể có phụ thu cố định hoặc quy tắc giá riêng.
- Hạn mức vé theo loại (ví dụ số vé dành cho hội viên) và hạn mức số vé mỗi khách.
- **Mở bán sớm** cho nhóm được chọn: hạng thành viên, hội viên gói trả phí, hoặc khách đã bấm quan tâm phim (mục 14.2). Đến giờ mở bán chung mới mở cho mọi khách.
- Cờ không áp dụng một số khuyến mãi cho suất sự kiện.
- **Quà tặng kèm vé** (poster, ly, móc khóa): khai báo số lượng quà, hệ thống trừ theo vé bán ra, quầy đánh dấu khi đã trao quà cho khách, và cảnh báo khi sắp hết quà (tái dùng cơ chế kho ở hạng mục 4).
- Báo cáo riêng cho từng sự kiện: số vé, doanh thu, lấp đầy, số quà đã trao.

---

## 27. Lộ trình triển khai

### 27.1 Chín hạng mục chính

| Bước | Hạng mục | Quy mô | Phụ thuộc |
|---|---|---|---|
| 1 | Nền tảng đơn hàng, sổ thanh toán, sự kiện nghiệp vụ, nhật ký thao tác, quy ước ghi nhận doanh thu | Lớn | – |
| 2 | Loại vé và quy tắc giá | Vừa | Bước 1 |
| 3 | Khuyến mãi | Vừa | Bước 1, 2 |
| 4 | Kho bắp nước và bán combo tại quầy | Vừa | Bước 1 |
| 5 | Báo cáo, phân tích lấp đầy, gợi ý lịch chiếu, tài chính, đối soát | Lớn | Bước 1–4 |
| 6 | Loyalty: sổ cái điểm, đổi quà, thử thách, giới thiệu | Vừa | Bước 1, 3 |
| 7 | Thẻ quà tặng | Vừa | Bước 1 |
| 8 | Thuê phòng và bán sỉ | Vừa | Bước 1, 2, 7 |
| 9 | Gợi ý phim cá nhân hóa | Nhỏ đến vừa | Bước 1 |

Ghi chú:

- Bước 1 là việc lớn nhất vì chạm hầu hết dịch vụ hiện có; hoàn thành bước này trước khi làm các bước sau.
- Bước 2 đến 5 mang lại giá trị kinh doanh rõ nhất; sau bước 5 hệ thống đã có giá linh hoạt, khuyến mãi, quản lý kho và báo cáo đầy đủ.
- Bước 6 đến 9 độc lập với nhau, có thể làm theo thứ tự ưu tiên của rạp.
- Báo cáo ở bước 5 được mở rộng dần khi hoàn thành bước 6 đến 8 (điểm, thẻ quà, thuê phòng).

### 27.2 Các hạng mục mở rộng

Sắp xếp theo mức ưu tiên. Các hạng mục mở rộng chỉ bắt đầu sau khi bước 1 hoàn thành.

| Ưu tiên | Hạng mục | Quy mô | Phụ thuộc |
|---|---|---|---|
| Cao | 13. Đổi suất chiếu | Nhỏ đến vừa | Bước 1, 2 |
| Cao | 15. Bán thêm combo sau khi đặt vé và nhắc lịch | Nhỏ đến vừa | Bước 1, 4 |
| Cao | 20. Nhà phát hành và hợp đồng phim | Vừa | Bước 5 |
| Cao | 25. Chốt doanh thu cuối ngày | Nhỏ | Bước 5 |
| Cao | 22. Đóng phòng bảo trì | Nhỏ đến vừa | Bước 1 |
| Cao | 23. Hóa đơn VAT theo yêu cầu | Nhỏ | Bước 1 |
| Trung bình | 24. Hỗ trợ khách hàng và hoàn tiền ngoại lệ | Vừa | Bước 1, 5 (đối soát) |
| Trung bình | 14. Danh sách chờ và báo khi mở bán | Vừa | Bước 1 |
| Trung bình | 16. Gói sản phẩm | Vừa | Bước 2, 3, 4 |
| Trung bình | 19. Đánh giá xác thực và khảo sát | Nhỏ | Bước 1, 9 |
| Trung bình | 21. Chống giữ ghế ảo và lạm dụng | Nhỏ | Bước 1, 3 |
| Sau | 18. Phân khúc khách hàng và chiến dịch | Vừa | Bước 3, 6 |
| Sau | 26. Suất chiếu sự kiện | Vừa | Bước 2, 3, 4, 14 |
| Sau | 17. Gói thành viên trả phí | Lớn | Bước 2, 3, 6 |

---

## 28. Kết quả sau khi hoàn thành

- Giá vé thay đổi theo giờ, ngày, loại vé và mức lấp đầy, có kiểm soát bằng trần và sàn.
- Marketing có khuyến mãi, đổi điểm, thử thách, giới thiệu, thẻ quà tặng, gói sản phẩm, gói thành viên và chiến dịch theo phân khúc, đều đo được hiệu quả.
- Quầy và online bán được vé, combo riêng, thẻ quà, gói sản phẩm; thuê phòng được xử lý trọn quy trình từ yêu cầu đến thanh toán.
- Khách đổi suất, vào danh sách chờ, đặt thêm combo sau khi mua vé, và được nhắc lịch trước giờ chiếu.
- Kho bắp nước theo dõi được tồn, hao hụt, giá vốn; không bán vượt tồn.
- Quản trị có số liệu lấp đầy, doanh thu, lợi nhuận theo phim và combo, tỷ lệ hủy, đối soát cổng thanh toán, và gợi ý điều chỉnh lịch chiếu.
- Việc chia doanh thu với nhà phát hành, xuất hóa đơn VAT, chốt doanh thu cuối ngày và xử lý hoàn tiền ngoại lệ đều có quy trình, có nhật ký.
- Sự cố phòng chiếu được xử lý có hệ thống, khách được thông báo và hoàn tiền đúng.
- Khách nhận gợi ý phim và suất phù hợp với bản thân, và đánh giá phim đáng tin hơn nhờ xác thực đã xem.
