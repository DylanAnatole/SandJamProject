# Bảng tiến độ — Sand Jam clone

Cập nhật: 22/10/2026. Chi tiết từng thay đổi xem `NHAT_KY_TIEN_DO_VI.md`.

**Ký hiệu**

- ✅ Ổn: đã có, đã so với video gốc và khớp (lệch vài px), đã qua kiểm tra.
- 🟡 Có nhưng còn khác bản gốc (ghi rõ chỗ khác).
- ⬜ Chưa làm.

## 1. Luật chơi (Gameplay)

| Hạng mục | Trạng thái | Ghi chú |
|---|---|---|
| Đổ cát, mở vùng theo thứ tự, thắng/thua | ✅ | Tự chơi 41 level (2–500): thắng cả 41, không lỗi |
| 5 ô chờ, ô bị khóa mở theo lượng cát | ✅ | |
| Khối nối xích (dọc / ngang), vào 2 ô trống bất kỳ | ✅ | Không kéo được khi bị chặn |
| Khối bí ẩn "?", đóng băng, nửa khối, chìa khóa / ổ khóa | ✅ | Có bài kiểm tra tự động |
| Booster Flare / Swap / Select | ✅ | Flare mở vùng ngay; Select chỉ chọn khối đang thấy |
| Hết chỗ → chơi tiếp 400 xu (ô thứ 6) / bỏ cuộc / chơi lại | ✅ | Luồng chạy tự động không lỗi |
| Tim (5 tim, hồi 30 phút), xu, thưởng thắng | ✅ | |

## 2. Màn chơi (hình ảnh)

| Hạng mục | Trạng thái | Ghi chú |
|---|---|---|
| Tranh, khung tranh, chân đế | ✅ | Hai đầu chân đế bản gốc bo tròn (nhỏ) |
| Ô chờ (pad), bờ làn, nút booster | ✅ | |
| Khối trong làn: cỡ, hàng to dần theo phối cảnh | ✅ | |
| Khối chạy lên ô chờ: tốc độ, xoay, chân | ✅ | Bề ngang và chiều cao (khối + chân) lệch ≤ 10 px |
| Khối trên ô chờ: kính, cỡ, xoay khi đổ, lắc | ✅ | |
| Khối hết cát: chớp trắng, nhảy, chạy ra | ✅ | |
| Hàng khối dồn lên | ✅ | |
| Bong bóng, pháo giấy khi mở vùng mới; màu hạt cát; tốc độ đổ | ✅ | Đo theo video 2 (giây 57–61, 152–157) |
| Số đếm trên vùng, màu vùng đang chờ | ✅ | Đã sửa màu 14–16 bị đen |
| Thanh trên (Level, xu, cài đặt) trong màn chơi | ✅ | |

## 3. Kết thúc màn và popup

| Hạng mục | Trạng thái | Ghi chú |
|---|---|---|
| Phóng to tranh + "Superb!" (quầng sáng, chữ, đốm góc) | ✅ | |
| Level Complete (khung, hộp thưởng, xu bay, nền) | ✅ | |
| Hết chỗ (Out of Space) | ✅ | Dải "OUT OF SPACE!", ảnh ô chờ nền xám tối, khung tối như bản gốc |
| Level Failed, Hết tim | ✅ | |
| Màu nền tối của popup | ✅ | |
| Hard Level / Bonus Level | ✅ | Nền tím than, biểu tượng thanh trên bị làm tối như bản gốc |
| New Feature / Booster mới | 🟡 | Có đủ, không vỡ; video gốc (level 200+) không có cảnh này để đo |
| CONGRATS (thẻ bộ sưu tập) | ✅ | Nền xanh tím than đặc như bản gốc |

## 4. Home và các trang

| Hạng mục | Trạng thái | Ghi chú |
|---|---|---|
| Home: đèn, bảng Level, sân khấu, thanh trên | ✅ | |
| Thanh điều hướng Shop / Home / Gallery | ✅ | |
| Sand Shop | ✅ | Mở ở đầu trang, trượt vào từ trái như bản gốc; cuộn có quán tính; gói tiền thật chỉ hiển thị (không thanh toán) |
| Gallery (bộ sưu tập, thẻ úp "Claim") | ✅ | Lưới thẻ đúng cỡ; nút "Claim All Cards" màu như bản gốc |
| Sand Quest (rương JOIN) | ✅ | Mở từ level 85 (rương ẩn trước đó); popup mời khớp video 2 (giây 22) |
| Cài đặt | ✅ | Bản trên Home đo theo video 2 (giây 15); chữ tiêu đề, bề rộng nút còn chênh nhỏ |
| Loading / splash giữa các level | ✅ | |

## 5. Công cụ, code, quy trình

| Hạng mục | Trạng thái | Ghi chú |
|---|---|---|
| Kiểm tra level tự làm (Check custom levels, bộ giải) | ✅ | Thư mục test đã bỏ theo yêu cầu (23/10) |
| Tự chơi hàng loạt level để bắt lỗi | ✅ | Script cục bộ |
| Tạo level từ ảnh pixel-art (84×112) | ✅ | Menu Sand Jam → Levels |
| Tạo level từ template, kiểm tra level tự làm | ✅ | |
| Tài liệu thiết kế level | ✅ | `THIET_KE_LEVEL_VI.md` |
| Nhật ký tiến độ, quy tắc code | ✅ | `NHAT_KY_TIEN_DO_VI.md` |
| Thư mục level tự thiết kế được đưa lên repo | ✅ | `Resources/CustomLevels/`, nạp qua `Campaign.LoadLevel` |
| Tùy chọn độ khó cho công cụ tạo level | ✅ | Easy / Normal / Hard, luôn qua bộ giải |

## 6. Tuần sau

| Hạng mục | Trạng thái |
|---|---|
| Tính năng mới không có trong game gốc | ⬜ |
| Bộ level thiết kế mới | ⬜ |
