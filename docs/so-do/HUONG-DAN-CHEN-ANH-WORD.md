# HƯỚNG DẪN CHI TIẾT CHÈN ẢNH SƠ ĐỒ VÀO BÁO CÁO WORD (docs/bao-cao.docx)

Tài liệu này giải thích nguyên nhân và bảng ánh xạ chuẩn 1:1 giữa file mã nguồn, file ảnh và vị trí trong báo cáo Word, giúp bạn chèn ảnh chính xác 100%, không bị xô lệch layout hay sai số thứ tự hình.

---

## 1. NGUYÊN NHÂN LỖI ẢNH 4.26 & LỆCH 1 SỐ TRONG DBML

### 1.1. Lỗi ảnh 4.26 không preview được trong Windows
* **Nguyên nhân kỹ thuật:** Trước đó, khi xuất file SVG từ PlantUML bằng PowerShell, luồng đầu ra đã vô tình bị ghi bằng bảng mã **UTF-16 LE** (chứa tiền tố byte BOM `\xff\xfe`), trong khi dòng đầu tiên của file SVG lại khai báo `encoding="UTF-8"`.
* Trình xem trước của Windows File Explorer (Preview Pane), Microsoft Edge và Microsoft Word phát hiện xung đột bảng mã XML nên báo lỗi:  
  `"This document does not support preview at this time"`.
* **Đã sửa:** File `hinh-4-26-so-do-lop-nhap-xuat-du-lieu.svg` và `hinh-4-26.svg` đã được chuyển đổi về bảng mã **UTF-8 thuần (No BOM)** và đã qua kiểm tra cú pháp XML thành công 100%. Bây giờ Windows, trình duyệt và Word có thể preview và hiển thị bình thường.

### 1.2. Đã đổi tên toàn bộ file DBML khớp chuẩn 100% với số thứ tự trong Word
Trước đây thư mục `docs/so-do/dbml/` bị lùi 1 số (do trong Word mục 4.2.25 có thêm Sơ đồ lớp 4.26).  
**Hiện tại toàn bộ 10 file DBML đã được đổi tên lại chuẩn xác từ 4.27 đến 4.36:**
* `hinh-4-26.dbml` ➔ **`hinh-4-27.dbml`** (Sơ đồ ERD đầy đủ của hệ thống)
* `hinh-4-27.dbml` ➔ **`hinh-4-28.dbml`** (ERD module Tài khoản người dùng)
* `hinh-4-28.dbml` ➔ **`hinh-4-29.dbml`** (ERD module Danh mục phim)
* `hinh-4-29.dbml` ➔ **`hinh-4-30.dbml`** (ERD module Phim)
* `hinh-4-30.dbml` ➔ **`hinh-4-31.dbml`** (ERD module Phòng chiếu)
* `hinh-4-31.dbml` ➔ **`hinh-4-32.dbml`** (ERD module Suất chiếu)
* `hinh-4-32.dbml` ➔ **`hinh-4-33.dbml`** (ERD module Đặt vé)
* `hinh-4-33.dbml` ➔ **`hinh-4-34.dbml`** (ERD module Thanh toán)
* `hinh-4-34.dbml` ➔ **`hinh-4-35.dbml`** (ERD module Thành viên)
* `hinh-4-35.dbml` ➔ **`hinh-4-36.dbml`** (ERD module Cấu hình rạp)

---

## 2. BẢNG ÁNH XẠ CHUẨN 1:1 ĐỂ CHÈN VÀO WORD

Bạn có thể lấy ảnh từ:
* Thư mục có tên mô tả rõ ràng: `docs/so-do/anh-chuan-theo-word/`
* Hoặc thư mục ảnh xem trước: `docs/so-do/anh-xem-truoc-svg/`

### Bảng A: Sơ đồ ERD Cơ sở dữ liệu & Sơ đồ lớp (Chương 4) - ĐÃ MATCH 1:1

| Số hiệu Word | Tên hình trong báo cáo Word | File mã nguồn | File ảnh trong `anh-xem-truoc-svg/` | File ảnh trong `anh-chuan-theo-word/` |
| :--- | :--- | :--- | :--- | :--- |
| **Hình 4.26** | Sơ đồ lớp package Nhập/xuất dữ liệu | `plantuml/hinh-4-26.puml` | `hinh-4-26.svg` | `hinh-4-26-so-do-lop-nhap-xuat-du-lieu.svg` |
| **Hình 4.27** | Sơ đồ ERD đầy đủ của hệ thống *(Trang ngang)* | `dbml/hinh-4-27.dbml` | `hinh-4-27.svg` | `hinh-4-27-erd-day-du-he-thong.svg` |
| **Hình 4.28** | Sơ đồ ERD module Tài khoản người dùng | `dbml/hinh-4-28.dbml` | `hinh-4-28.svg` | `hinh-4-28-erd-tai-khoan-nguoi-dung.svg` |
| **Hình 4.29** | Sơ đồ ERD module Danh mục phim | `dbml/hinh-4-29.dbml` | `hinh-4-29.svg` | `hinh-4-29-erd-danh-muc-phim.svg` |
| **Hình 4.30** | Sơ đồ ERD module Phim | `dbml/hinh-4-30.dbml` | `hinh-4-30.svg` | `hinh-4-30-erd-phim.svg` |
| **Hình 4.31** | Sơ đồ ERD module Phòng chiếu | `dbml/hinh-4-31.dbml` | `hinh-4-31.svg` | `hinh-4-31-erd-phong-chieu.svg` |
| **Hình 4.32** | Sơ đồ ERD module Suất chiếu | `dbml/hinh-4-32.dbml` | `hinh-4-32.svg` | `hinh-4-32-erd-suat-chieu.svg` |
| **Hình 4.33** | Sơ đồ ERD module Đặt vé | `dbml/hinh-4-33.dbml` | `hinh-4-33.svg` | `hinh-4-33-erd-dat-ve.svg` |
| **Hình 4.34** | Sơ đồ ERD module Thanh toán | `dbml/hinh-4-34.dbml` | `hinh-4-34.svg` | `hinh-4-34-erd-thanh-toan.svg` |
| **Hình 4.35** | Sơ đồ ERD module Thành viên | `dbml/hinh-4-35.dbml` | `hinh-4-35.svg` | `hinh-4-35-erd-thanh-vien.svg` |
| **Hình 4.36** | Sơ đồ ERD module Cấu hình rạp | `dbml/hinh-4-36.dbml` | `hinh-4-36.svg` | `hinh-4-36-erd-cau-hinh-rap.svg` |

---

### Bảng B: 22 Sơ đồ Use Case (Chương 3: Hình 3.2 – Hình 3.24)

| Số hiệu | Tên sơ đồ Use Case | File mã nguồn | File ảnh trong `anh-chuan-theo-word/` |
| :--- | :--- | :--- | :--- |
| **Hình 3.2** | Tổng quan Use Case Hệ thống | `plantuml/hinh-3-2.puml` | `hinh-3-02-tong-quan-use-case.svg` |
| **Hình 3.3** | Phân rã chức năng hệ thống | `plantuml/hinh-3-3.puml` | `hinh-3-03-phan-ra-chuc-nang.svg` |
| **Hình 3.4** | UC Phân hệ Xác thực & Ủy quyền | `plantuml/hinh-3-4.puml` | `hinh-3-04-uc-xac-thuc-va-uy-quyen.svg` |
| **Hình 3.5** | UC Phân hệ Quản lý Tài khoản | `plantuml/hinh-3-5.puml` | `hinh-3-05-uc-quan-ly-tai-khoan.svg` |
| **Hình 3.6** | UC Phân hệ Quản lý Phim | `plantuml/hinh-3-6.puml` | `hinh-3-06-uc-quan-ly-phim.svg` |
| **Hình 3.7** | UC Phân hệ Quản lý Phòng chiếu | `plantuml/hinh-3-7.puml` | `hinh-3-07-uc-quan-ly-phong-chieu.svg` |
| **Hình 3.8** | UC Phân hệ Quản lý Suất chiếu | `plantuml/hinh-3-8.puml` | `hinh-3-08-uc-quan-ly-suat-chieu.svg` |
| **Hình 3.9** | UC Phân hệ Đặt vé trực tuyến | `plantuml/hinh-3-9.puml` | `hinh-3-09-uc-dat-ve-truc-tuyen.svg` |
| **Hình 3.10** | UC Phân hệ Thanh toán trực tuyến | `plantuml/hinh-3-10.puml` | `hinh-3-10-uc-thanh-toan-truc-tuyen.svg` |
| **Hình 3.11** | UC Phân hệ Bán vé tại quầy | `plantuml/hinh-3-11.puml` | `hinh-3-11-uc-ban-ve-tai-quay.svg` |
| **Hình 3.12** | UC Phân hệ Quản lý Bắp nước | `plantuml/hinh-3-12.puml` | `hinh-3-12-uc-quan-ly-bap-nuoc.svg` |
| **Hình 3.13** | UC Phân hệ Soát vé | `plantuml/hinh-3-13.puml` | `hinh-3-13-uc-soat-ve.svg` |
| **Hình 3.14** | UC Phân hệ Báo cáo & Thống kê | `plantuml/hinh-3-14.puml` | `hinh-3-14-uc-bao-cao-va-thong-ke.svg` |
| **Hình 3.15** | UC Phân hệ Quản lý Khuyến mãi | `plantuml/hinh-3-15.puml` | `hinh-3-15-uc-quan-ly-khuyen-mai.svg` |
| **Hình 3.16** | UC Phân hệ Quản lý Thành viên | `plantuml/hinh-3-16.puml` | `hinh-3-16-uc-quan-ly-thanh-vien.svg` |
| **Hình 3.17** | UC Phân hệ Đánh giá & Phản hồi | `plantuml/hinh-3-17.puml` | `hinh-3-17-uc-danh-gia-va-phan-hoi.svg` |
| **Hình 3.18** | UC Phân hệ Quản lý Tin tức | `plantuml/hinh-3-18.puml` | `hinh-3-18-uc-quan-ly-tin-tuc.svg` |
| **Hình 3.19** | UC Phân hệ Quản trị Giao diện & Banner | `plantuml/hinh-3-19.puml` | `hinh-3-19-uc-quan-tri-giao-dien-va-banner.svg` |
| **Hình 3.20** | UC Phân hệ Cấu hình Hệ thống | `plantuml/hinh-3-20.puml` | `hinh-3-20-uc-cau-hinh-he-thong.svg` |
| **Hình 3.21** | UC Phân hệ Quản lý Giá vé | `plantuml/hinh-3-21.puml` | `hinh-3-21-uc-quan-ly-gia-ve.svg` |
| **Hình 3.22** | UC Phân hệ Quản lý Thất lạc đồ | `plantuml/hinh-3-22.puml` | `hinh-3-22-uc-quan-ly-that-lac-do.svg` |
| **Hình 3.23** | UC Phân hệ Nhập/Xuất Dữ liệu | `plantuml/hinh-3-23.puml` | `hinh-3-23-uc-nhap-xuat-du-lieu.svg` |
| **Hình 3.24** | UC Phân hệ Giám sát & Vận hành | `plantuml/hinh-3-24.puml` | `hinh-3-24-uc-giam-sat-va-van-hanh.svg` |

---

## 3. CÁCH CHÈN VÀO WORD CHUẨN ĐẸP KHÔNG BỊ LỆCH LỀ

1. **Chọn kiểu hiển thị ảnh (Wrap Text):**
   * Sau khi chèn ảnh (`Insert` -> `Pictures`), bấm chuột phải vào ảnh -> chọn **Wrap Text** -> chọn **In Line with Text** (Trong dòng văn bản).
2. **Căn giữa ảnh:**
   * Đặt con trỏ chuột vào dòng chứa ảnh.
   * Mở hộp thoại Paragraph (`Ctrl + D` hoặc thanh Home) -> chọn **Alignment: Centered**, **Indentation -> Special: (none)** để không bị thụt lề 1.27 cm.
3. **Kích thước ảnh:**
   * Các sơ đồ thông thường: Đặt chiều rộng khoảng **14 cm – 16 cm**.
   * Riêng **Hình 4.27 (Sơ đồ ERD đầy đủ)**: Chèn vào trang nằm ngang (Landscape), đặt chiều rộng ảnh khoảng **24 cm – 25 cm**.
