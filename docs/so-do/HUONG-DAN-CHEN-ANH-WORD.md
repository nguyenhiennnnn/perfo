# HƯỚNG DẪN CHI TIẾT CHÈN ẢNH SƠ ĐỒ VÀO BÁO CÁO WORD (docs/bao-cao.docx)

Tài liệu này tổng hợp bảng ánh xạ chuẩn 1:1 giữa file mã nguồn, file ảnh và vị trí các hình trong báo cáo Word `bao-cao.docx`, giúp bạn tự chèn ảnh chính xác 100%, không bị nhầm lẫn số hiệu hình hay xô lệch giao diện.

---

## 1. THƯ MỤC CHỨA ẢNH CHUẨN

* **Thư mục ảnh chuẩn theo tên Word:**  
  👉 [docs/so-do/anh-chuan-theo-word/](file:///d:/Project/Cinema/docs/so-do/anh-chuan-theo-word/)  
  *(Đã đặt tên kèm chú thích tiếng Việt khớp 100% với đề mục Word để bạn chỉ việc kéo-thả)*
* **Thư mục ảnh xem trước ngắn gọn:**  
  👉 [docs/so-do/anh-xem-truoc-svg/](file:///d:/Project/Cinema/docs/so-do/anh-xem-truoc-svg/)

---

## 2. BẢNG ÁNH XẠ CHƯƠNG 3: SƠ ĐỒ USE CASE (Hình 3.2 – Hình 3.25)

| Số hiệu | Tên hình chuẩn trong Word | File mã nguồn | File ảnh trong `anh-chuan-theo-word/` |
| :--- | :--- | :--- | :--- |
| **Hình 3.2** | Sơ đồ ngữ cảnh hệ thống | `plantuml/hinh-3-2.puml` | `hinh-3-02-tong-quan-use-case.svg` |
| **Hình 3.3** | Sơ đồ package use case tổng quát | `plantuml/hinh-3-3.puml` | `hinh-3-03-phan-ra-chuc-nang.svg` |
| **Hình 3.4** | Sơ đồ use case package Xác thực | `plantuml/hinh-3-4.puml` | `hinh-3-04-uc-xac-thuc-va-uy-quyen.svg` |
| **Hình 3.5** | Sơ đồ use case package Hồ sơ cá nhân | `plantuml/hinh-3-5.puml` | `hinh-3-05-uc-quan-ly-tai-khoan.svg` |
| **Hình 3.6** | Sơ đồ use case package Duyệt phim | `plantuml/hinh-3-6.puml` | `hinh-3-06-uc-quan-ly-phim.svg` |
| **Hình 3.7** | Sơ đồ use case package Đánh giá phim | `plantuml/hinh-3-7.puml` | `hinh-3-07-uc-quan-ly-phong-chieu.svg` |
| **Hình 3.8** | Sơ đồ use case package Đặt vé trực tuyến | `plantuml/hinh-3-8.puml` | `hinh-3-08-uc-quan-ly-suat-chieu.svg` |
| **Hình 3.9** | Sơ đồ use case package Thanh toán trực tuyến | `plantuml/hinh-3-9.puml` | `hinh-3-09-uc-dat-ve-truc-tuyen.svg` |
| **Hình 3.10** | Sơ đồ use case package Trợ lý AI | `plantuml/hinh-3-10.puml` | `hinh-3-10-uc-thanh-toan-truc-tuyen.svg` |
| **Hình 3.11** | Sơ đồ use case package Bán vé tại quầy | `plantuml/hinh-3-11.puml` | `hinh-3-11-uc-ban-ve-tai-quay.svg` |
| **Hình 3.12** | Sơ đồ use case package Soát vé | `plantuml/hinh-3-12.puml` | `hinh-3-12-uc-quan-ly-bap-nuoc.svg` |
| **Hình 3.13** | Sơ đồ use case package Quản lý booking | `plantuml/hinh-3-13.puml` | `hinh-3-13-uc-soat-ve.svg` |
| **Hình 3.14** | Sơ đồ use case package Quản lý phim | `plantuml/hinh-3-14.puml` | `hinh-3-14-uc-bao-cao-va-thong-ke.svg` |
| **Hình 3.15** | Sơ đồ use case package Quản lý danh mục | `plantuml/hinh-3-15.puml` | `hinh-3-15-uc-quan-ly-khuyen-mai.svg` |
| **Hình 3.16** | Sơ đồ use case package Quản lý phòng chiếu | `plantuml/hinh-3-16.puml` | `hinh-3-16-uc-quan-ly-thanh-vien.svg` |
| **Hình 3.17** | Sơ đồ use case package Quản lý suất chiếu | `plantuml/hinh-3-17.puml` | `hinh-3-17-uc-danh-gia-va-phan-hoi.svg` |
| **Hình 3.18** | Sơ đồ use case package Quản lý người dùng | `plantuml/hinh-3-18.puml` | `hinh-3-18-uc-quan-ly-tin-tuc.svg` |
| **Hình 3.19** | Sơ đồ use case package Quản lý đánh giá | `plantuml/hinh-3-19.puml` | `hinh-3-19-uc-quan-tri-giao-dien-va-banner.svg` |
| **Hình 3.20** | Sơ đồ use case package Quản lý thanh toán | `plantuml/hinh-3-20.puml` | `hinh-3-20-uc-cau-hinh-he-thong.svg` |
| **Hình 3.21** | Sơ đồ use case package Báo cáo thống kê | `plantuml/hinh-3-21.puml` | `hinh-3-21-uc-quan-ly-gia-ve.svg` |
| **Hình 3.22** | Sơ đồ use case package Cấu hình rạp | `plantuml/hinh-3-22.puml` | `hinh-3-22-uc-quan-ly-that-lac-do.svg` |
| **Hình 3.23** | Sơ đồ use case package Tải tệp | `plantuml/hinh-3-23.puml` | `hinh-3-23-uc-nhap-xuat-du-lieu.svg` |
| **Hình 3.24** | Sơ đồ use case package Vận hành hệ thống | `plantuml/hinh-3-24.puml` | `hinh-3-24-uc-giam-sat-va-van-hanh.svg` |
| **Hình 3.25** | **Sơ đồ use case package Nhập/xuất dữ liệu** | `plantuml/hinh-3-25.puml` | **`hinh-3-25-uc-nhap-xuat-du-lieu.svg`** |

---

## 3. BẢNG ÁNH XẠ CHƯƠNG 4: SƠ ĐỒ LỚP & ERD (Hình 4.26 – Hình 4.36)

| Số hiệu Word | Tên hình trong báo cáo Word | File mã nguồn | File ảnh trong `anh-chuan-theo-word/` |
| :--- | :--- | :--- | :--- |
| **Hình 4.26** | Sơ đồ lớp package Nhập/xuất dữ liệu | `plantuml/hinh-4-26.puml` | `hinh-4-26-so-do-lop-nhap-xuat-du-lieu.svg` |
| **Hình 4.27** | Sơ đồ ERD đầy đủ của hệ thống *(Trang ngang)* | `dbml/hinh-4-27.dbml` | `hinh-4-27-erd-day-du-he-thong.svg` |
| **Hình 4.28** | Sơ đồ ERD module Tài khoản người dùng | `dbml/hinh-4-28.dbml` | `hinh-4-28-erd-tai-khoan-nguoi-dung.svg` |
| **Hình 4.29** | Sơ đồ ERD module Danh mục phim | `dbml/hinh-4-29.dbml` | `hinh-4-29-erd-danh-muc-phim.svg` |
| **Hình 4.30** | Sơ đồ ERD module Phim | `dbml/hinh-4-30.dbml` | `hinh-4-30-erd-phim.svg` |
| **Hình 4.31** | Sơ đồ ERD module Phòng chiếu | `dbml/hinh-4-31.dbml` | `hinh-4-31-erd-phong-chieu.svg` |
| **Hình 4.32** | Sơ đồ ERD module Suất chiếu | `dbml/hinh-4-32.dbml` | `hinh-4-32-erd-suat-chieu.svg` |
| **Hình 4.33** | Sơ đồ ERD module Đặt vé | `dbml/hinh-4-33.dbml` | `hinh-4-33-erd-dat-ve.svg` |
| **Hình 4.34** | Sơ đồ ERD module Thanh toán | `dbml/hinh-4-34.dbml` | `hinh-4-34-erd-thanh-toan.svg` |
| **Hình 4.35** | Sơ đồ ERD module Thành viên | `dbml/hinh-4-35.dbml` | `hinh-4-35-erd-thanh-vien.svg` |
| **Hình 4.36** | Sơ đồ ERD module Cấu hình rạp | `dbml/hinh-4-36.dbml` | `hinh-4-36-erd-cau-hinh-rap.svg` |

---

## 4. QUY TẮC ĐỊNH DẠNG TRONG WORD TRÁNH LỖI LỆCH LỀ

1. **Wrap Text:** Chuột phải vào ảnh -> chọn **In Line with Text** (Trong dòng văn bản).
2. **Canh lề đoạn văn:**
   * Chọn dòng chứa ảnh -> mở hộp thoại Paragraph (`Ctrl + D` hoặc Home).
   * **Alignment:** Chọn `Centered`.
   * **Indentation -> Special:** Chọn `(none)` (tránh thụt đầu dòng 1.27 cm làm lệch ảnh).
3. **Kích thước:**
   * Hình 3.25 và các sơ đồ thông thường: Đặt chiều rộng từ **14 cm – 16 cm**.
   * Riêng **Hình 4.27 (ERD đầy đủ)**: Đặt trên trang ngang (Landscape) với chiều rộng khoảng **24 cm – 25 cm**.
