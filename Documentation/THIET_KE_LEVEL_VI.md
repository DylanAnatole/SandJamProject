# Thiết kế level mới

Mỗi level là một file JSON (`LevelData`). Lúc chạy, prefab `Level001` đọc file này và tự dựng bức tranh, các vùng
màu và các làn khối, nên thêm level mới không cần tạo scene hay prefab.

## Tạo level từ ảnh pixel-art (cách nhanh nhất)

1. Vẽ một ảnh PNG kích thước **84 × 112 px**, mỗi pixel là một ô của bức tranh. Đặt ảnh trong
   `Assets/SandJam/Levels/Pictures/`; ảnh mẫu là `Sunset.png`.
   - Mỗi mảng liền nhau cùng một màu sẽ thành một vùng. Màu trong ảnh được quy về màu gần nhất của game.
   - Pixel trong suốt, pixel đen hoặc pixel màu tím nhạt giống đường viền của game sẽ thành tường.
   - Mảng nhỏ hơn 20 ô được coi là pixel lạc và chuyển thành tường.
2. Chọn ảnh trong cửa sổ Project, rồi vào menu **Sand Jam → Levels → New level from selected picture**.
3. Công cụ tự làm các việc sau:
   - Vùng chạm đáy tranh mở sẵn. Các vùng còn lại mở khi một vùng kề bên được đổ đầy, nên tranh được tô từ dưới
     lên.
   - Lượng cát mỗi vùng = số ô, làm tròn xuống bội số của 20.
   - Khối 400 cát được cắt theo thứ tự vùng mở, rồi chia đều vào 3 làn.
   - Kiểm tra level qua `LevelAuthoring.Publish` (tổng cát, bộ giải), lưu thành `Custom_NNN` và đặt làm level
     chơi tiếp theo.
4. Bấm Play để chơi thử. Muốn chỉnh độ khó thì sửa file JSON vừa tạo: thứ tự khối, khối đặc biệt, ô chờ bị khóa.

Code: `Editor/Tools/LevelFromPicture.cs`.

## Tạo level từ template

1. Vào menu **Sand Jam → Levels → New level from template**.
   - Lệnh này chép template đầu tiên trong `Assets/SandJam/Levels/Templates/` thành
     `Resources/Levels/Data/Custom_NNN.json`.
   - Level mới được thêm vào cuối `Campaign.json` và đặt làm level sẽ chơi tiếp theo.
2. Sửa file JSON vừa tạo, rồi bấm Play để chơi thử.
3. Vào menu **Sand Jam → Levels → Check custom levels** để kiểm tra mọi `Custom_*` và template theo 3 luật:
   - dữ liệu hợp lệ (`SandGame.Validate`);
   - tổng cát của khối mỗi màu bằng tổng cát các vùng màu đó;
   - có thể thắng với 5 ô chờ (`LevelReplaySolver`).
4. Vào menu **Sand Jam → Tests → Run all checks**, phải qua cả 8 bài.

Code liên quan:
- `Editor/Tools/LevelAuthoring.cs`: tạo level và kiểm tra.
- `Scripts/Runtime/Core/Data/LevelData.cs`: định dạng file.
- `Core/Domain/SandGame.cs`: luật chơi.

> Level gốc (`Resources/Levels/Data`, 556 file) chỉ nằm trên máy, không có trong repo. Level tự thiết kế nên đặt
> tên `Custom_*` và lưu vào thư mục level riêng sẽ được đưa lên repo (xem mục "Việc tiếp theo").

## Định dạng `LevelData`

| Trường | Ý nghĩa |
|---|---|
| `sceneName` | id level (trùng tên file) |
| `rowCount`, `columnCount` | kích thước lưới tranh (ô). Level gốc thường 112×84 |
| `gridSlotScale` | kích thước 1 ô (world units) cho số đếm đặt tay |
| `uiDivider` | số hạt cát cho 1 đơn vị đếm hiển thị (gốc: 20). Khối 400 cát hiện số 20 |
| `characterAmmo` | lượng cát chuẩn của 1 khối (gốc: 400) |
| `gridSlotNeedAmmoCount` | 5 số, mỗi số cho 1 ô chờ: cần tiêu bao nhiêu đơn vị cát thì ô đó mới mở (0 = mở sẵn). Ví dụ `[0,0,0,200,300]`: ô 4 và ô 5 bị khóa |
| `opr`, `opc` | hàng và cột của các ô tường (đường viền tím ngăn giữa các vùng) |
| `parts[]` | các vùng màu, xem bảng dưới |
| `laneData[]` | các làn khối, mỗi làn là `ColorAmmoDatas[]` xếp từ trước ra sau, xem bảng dưới |

**Vùng (`parts[]`)**

| Trường | Ý nghĩa |
|---|---|
| `name` | tên vùng, dùng làm khóa cho `prerequests` |
| `ColorType` | màu (1–16…, bảng ở `ColorPalette`) |
| `amount` | số hạt cát cần đổ cho đầy vùng |
| `rows[]`, `cols[]` | các ô thuộc vùng |
| `isOpenedAtStart` | vùng mở sẵn từ đầu |
| `prerequests[]` | vùng đóng sẽ mở khi **một** vùng trong danh sách đầy |
| `isManualPosition`, `cnvPos` | đặt tay vị trí số đếm (x tính từ tâm lưới, y tính từ hàng dưới cùng) |
| `IsUnlockerPart`, `unlockerPartName`, `IsUnlockedPart` | ổ khóa: vùng sọc chỉ nhận khối có chìa |

**Khối (`ColorAmmoDatas[]`)**

| Trường | Ý nghĩa |
|---|---|
| `ColorType`, `AmmoCount` | màu và lượng cát (thường 400; khối cuối của một màu có thể ít hơn để tổng khớp) |
| `IsSecret` | khối bí ẩn "?" (che màu cho tới khi lên đầu làn) |
| `IsChain`, `IsChainSameLane` | khối nối: hai khối liền nhau trong cùng làn, hoặc hai làn cạnh nhau (cùng hàng) |
| `IsFreeze`, `FreezeCount` | khối đóng băng: tan sau `FreezeCount` lần đưa khối khác lên |
| `IsHalf` | nửa khối: hai nửa cùng màu trong ô chờ ghép thành 1 |
| `IsUnlocker` | khối mang chìa khóa cho vùng ổ khóa cùng màu |

## Luật cân bằng (rút ra từ level gốc)

- Tổng `AmmoCount` mỗi màu = tổng `amount` các vùng màu đó. "Check custom levels" báo lỗi nếu lệch.
- Mỗi màu thường có thêm 1 khối thiếu (ví dụ 120, 340) để tổng khớp.
- Độ khó đến từ thứ tự khối trong làn so với thứ tự mở vùng (`prerequests`) và từ số ô chờ bị khóa. Solver bảo đảm
  level thắng được với 5 ô chờ, nhưng không đo độ khó.

## Việc tiếp theo (chuẩn bị cho level tự thiết kế)

- Công cụ đã chia khối theo thứ tự vùng mở. Bước tiếp theo là thêm tùy chọn độ khó, ví dụ trộn khối giữa các vùng
  hoặc tự đặt khối bí ẩn, khối nối, khối đóng băng.
- Thư mục level tự thiết kế được đưa lên repo (tách khỏi `Resources/Levels/Data` vốn bị bỏ qua vì chứa level gốc).
