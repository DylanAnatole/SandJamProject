# Thêm mechanic / booster mới

Những chỗ phải chạm khi thêm một mechanic hay một booster. Lấy các mechanic và booster có sẵn làm mẫu.

## Booster mới

1. **Bảng booster** — `Scripts/Runtime/Meta/BoosterCatalog.cs`: thêm một dòng `new Info("<id>", <giá xu>)` và
   một hằng cho id. Số lượng trong túi, giá, lưu save (`SaveData.boosters`, theo id) tự theo bảng.
2. **Hiệu ứng** — `BoosterController.Activate`: thêm một nhánh cho id mới. Mẫu:
   - dùng ngay (Swap): gọi luật trong `SandGame`, thành công thì `Pay(action)`;
   - cần chọn mục tiêu (Select): mở picker (`PickerOpen`, `BoosterInputBlocked`), chọn xong mới `Pay`;
   - có phần diễn (Flare): coroutine đặt `Busy` trong lúc diễn.
3. **Luật** — đặt trong `Core/Domain/SandGame.Boosters.cs` (không phụ thuộc Unity, bộ giải dùng lại được). Sau khi
   đổi trạng thái: `Revision++` và `Evaluate()`.
4. **Nút trên HUD** — trình dựng scene (`BuildVideoScene.cs`, vòng `for (i < 3)`) tạo 3 nút theo
   `BoosterCatalog.All`; nút thứ 4 cần chỗ trên HUD và icon `VideoUI/icon-booster-N`.
5. **Mở khoá theo level** — `OriginalConfig.BoosterUnlockLevel`: id mới chưa có trong cấu hình gốc thì mở từ level 1.
6. **Tuỳ chọn**: nhiệm vụ (`TaskBoard.Metric`), quà (`DailyRewards`), shop (`ShopPopup.BoosterActions`).

## Mechanic mới (thuộc tính của khối)

1. **Dữ liệu level** — `Core/Data/LevelData.cs`, `CharacterData`: thêm cờ (mẫu: `IsFreeze`, `IsSecret`, `IsHalf`,
   `IsUnlocker`, `IsChain`).
2. **Trạng thái** — `Core/Domain/GameEntities.cs`, `Shooter`: đọc cờ trong constructor.
3. **Luật** — `Core/Domain/SandGame*.cs`: chọn được hay không (`SelectionSlot`, `CanSelectLane`), che màu
   (`IsHidden`), đếm lượt (`SandGame.Freeze.cs`). Booster Select dùng `PrioritySlot`: nhớ chặn ở đó nếu cần.
4. **Hình ảnh** — `Controllers/Gameplay/SandJamSceneController.Dynamic.cs`: gắn phần nhìn khi dựng khối (mẫu:
   `AddFreezeShell`, `AddKey`, `AddSecretCover`); view riêng đặt trong `Views/Characters/`.
5. **Tạo level** — `Editor/Tools/LevelFromPicture.cs` và template: cho công cụ đặt được mechanic mới.
6. **Kiểm tra** — `Editor/Tools/LevelReplaySolver` chạy trên `SandGame`, nên luật mới tự được bộ giải dùng; chạy
   "Check custom levels" và tự chơi vài level có mechanic.
7. **Popup giới thiệu** (tuỳ chọn) — `LevelIntroPopups`.

## Quy trình

Biên dịch → chạy thử level có mechanic / booster mới → tự chơi một loạt level cũ để chắc không vỡ → ghi
`NHAT_KY_TIEN_DO_VI.md` → commit.
