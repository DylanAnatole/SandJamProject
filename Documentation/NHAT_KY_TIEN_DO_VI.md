# Nhật ký tiến độ

Mỗi thay đổi được ghi thêm một mục ở cuối file, kèm commit tương ứng, để dễ đối chiếu và kiểm tra.

## Quy tắc code (áp dụng đến hết dự án)

1. **Sạch, chỉn chu, dễ thêm / xóa / sửa.** Mỗi con số chỉnh theo bản gốc là một hằng có tên và có chú thích nguồn đo
   (video nào, giây nào, bao nhiêu px).
2. **Không làm code phình to.** Ưu tiên sửa code đang có hơn là viết thêm.
3. **Dùng lại cái đã có.** Trước khi viết mới, tìm helper, prefab hoặc module sẵn có (`RendererExtensions`,
   `SpriteFactory`, `PopupModule`, `SandFx`, `OutlinedText`, `VideoScreen.NavPlace`, …). Không thấy thiếu là tạo code
   mới, vì dễ bị trùng lặp.
4. **Ghi nhật ký.** Mỗi commit thêm một mục vào file này.

## Quy trình mỗi thay đổi

1. Compile và xem console, không được có lỗi.
2. Chụp màn hình bản của mình và đo pixel so với video gốc (`D:\luyentayunityxgame`).
3. Chạy **Sand Jam → Tests → Run all checks**, phải qua cả 8 bài.
4. Trả save demo về như cũ: level 68, mỗi loại booster 10 cái, 5 mạng.
5. Chạy `git add --dry-run` để chắc chắn không đưa file lấy từ game gốc lên repo (Resources, SourceData,
   OriginalReference, …).
6. Ghi một mục vào nhật ký này, rồi commit và push.

## Cách ghi một mục

```
### YYYY-MM-DD — <tên thay đổi> (`<commit>`)
- Đo / lý do: <so với bản gốc, số liệu>
- Sửa: <file chính — hằng / helper đã đổi>
- Kiểm tra: <checks, ảnh so sánh>
```

---

## 2026-10-15

Các thay đổi trong ngày:

- **Ô vuông trong làn:** nhỏ hơn 10%, bo cạnh mềm hơn, ngả ra sau, xếp sát hơn.
- **Khung tranh:** dựng lại thành viền bo tròn như bản gốc.
- **Số đếm của từng vùng trên tranh:** nhỏ đi 20%.
- **Dây xích (ChainBack gốc):** mỗi cặp dùng một vòng, tròn đều, không bị méo.
- **Home:** bảng level và nhãn JOIN dùng art gốc; màn splash giữa các level khớp bản gốc.
- **Popup:** CONGRATS và Level Failed khớp bản gốc.
- **Lưới Gallery:** chỉnh lại.

## 2026-10-16

**Dọn code**

- Tách `PopupScreens` thành các `PopupModule` riêng cho từng popup.
- Gộp helper bị trùng: `DragScroller`, helper dựng hình, `OutlinedText`.
- Bỏ code chết.

**Luật chơi theo video**

- Cặp xích được đặt vào hai ô chờ trống bất kỳ.
- Cặp xích không bị kéo khi đang bị chặn.
- Flare mở vùng nó làm lộ ra, nên tô được ngay.
- Booster dùng im lặng; Select chọn bằng cách chạm.

**Hiệu ứng**

- Lóe sáng khi đứt xích.
- Khói khi lộ khối bí ẩn.
- Vòng khói khi mở ô chờ bị khóa.
- Thẻ Gallery úp mặt cho tới khi nhận.

## 2026-10-19

| Commit | Thay đổi | Số đo (bản gốc → bản mình) |
|---|---|---|
| `a12db3a` | Home: đèn rọi, bảng level, hoa văn sân khấu, thanh trên cùng | rộng đèn 4.4; bảng level 0.37 |
| `e08c489` | Tranh, khung, chân đế | tranh +3.5%; viền 6.7% bề ngang |
| `b0899fe` | Ô chờ (pad) và khối đang chờ | pad nghiêng 55°; khối chờ 0.86 |
| `89c950e` | Bờ làn và nút booster | bờ 1339 → 1340 px; nút cao 86 px, cách nhau 124 px |
| `4866618` | Hình khối chân đế: gờ trước mỏng, đế sau sâu, mép dưới bo tối | mặt trước 27/21 px → 24/21 px |
| `92d2bc6` | Thanh điều hướng Home: thanh, tab, icon, chữ, dấu "!" | lệch ≤ 3 px |

## 2026-10-20

- Ghi quy tắc code và quy trình làm việc vào đầu file này; tạo nhật ký tiến độ.

### 2026-10-20 — Tự chơi để bắt lỗi, công cụ tạo level từ ảnh

**Đo / lý do**

- Cho máy tự chơi 41 level, từ 2 tới 500, theo lời giải của bộ giải, tốc độ x3–x4.
- Kết quả: thắng cả 41 level, không có level nào kẹt.
- Bắt được một lỗi: mỗi level báo 22 lần `MissingReferenceException`. Nguyên nhân là `CharacterDepthStyle` giữ các
  bản viền chữ đã bị `ReferenceTextOutline` hủy.
- Chuẩn bị thiết kế level mới: sửa tay hàng nghìn ô trong JSON là không khả thi, nên cần công cụ tạo level từ ảnh.

**Sửa**

- `CharacterDepthStyle`: đọc lại danh sách chữ khi có bản viền bị thay; màu viền lấy từ `OutlinedText.Ink`.
- Thêm `LevelFromPicture`: ảnh PNG 84×112 thành level mới.
- `LevelAuthoring.Publish` dùng chung cho cách tạo từ template và tạo từ ảnh.
- `OriginalConfig.ColorEntry.HasInit`: màu 14–16 không có màu nền gốc (bị đen), nên vùng đang chờ dùng màu nhạt
  tự tính.
- Đặt tên cho các màu đường viền `SandBoardTextureView.GameOutline` / `DefaultObstacleTint`, thay cho các số
  viết lặp ở nhiều chỗ.
- Thêm tài liệu `THIET_KE_LEVEL_VI.md`.

**Kiểm tra**

- Sau khi sửa, chạy lại không còn lỗi.
- Level tạo từ `Sunset.png` có 4 vùng và 24 khối, qua bộ giải, chơi tự động thắng.
- Run all checks qua 8/8.
