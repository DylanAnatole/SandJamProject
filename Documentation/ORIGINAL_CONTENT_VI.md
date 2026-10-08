# Bộ dữ liệu bổ sung từ game gốc

## Đã có trong project
- `Assets/RecoveredOriginal`: 54 mesh bổ sung, 1 avatar, 11 animation clip độc lập, 1.261 texture, 1.205 sprite, 7 audio, 8 font, 123 material xem trước và 563 JSON (gồm 556 level cùng cấu hình/thứ tự màn). Asset đã có được tái sử dụng theo GUID.
- `SourceData/AssetRipper`: bản sao đầy đủ Assets, Packages, ProjectSettings của dữ liệu trích xuất, gồm prefab, animator controller, các cấu hình ScriptableObject, UI shop/reward, vật cản, hiệu ứng, scripts và shader gốc.
- `SourceData/DeferredImports`: ảnh tham chiếu và animation phụ thuộc script/DLL hoặc asset chưa được phục dựng. Không nạp tự động vào Unity.
- `Documentation/RECOVERED_ASSET_INDEX.csv`: tra asset gốc và vị trí tương ứng.
- `Documentation/RECOVERY_LIMITATIONS.json`: các liên kết nguồn còn thiếu/phụ thuộc ngoài phần đã nhập.

## Material và shader
123 material trong thư viện mới là bản xem trước sử dụng màu/texture tìm được và shader tương thích, không phải phục hồi chính xác shader gốc. Hai texture atlas font thiếu liên kết trong dữ liệu nguồn dùng texture mặc định; bản gốc giữ nguyên trong SourceData. Không thay material của scene gameplay hiện tại.

## Để thành game hoàn chỉnh vẫn cần triển khai
1. Luật Secret / Half / Unlocker, đối chiếu điều kiện mở vùng và blocker gốc.
2. Nạp thêm màn theo Default_LevelOrderConfig, xác thực dữ liệu và chơi thử từng nhóm cơ chế; không mặc định 556 màn đều hoạt động.
3. Gallery, shop, tiền tệ/phần thưởng, tiến trình và tutorial theo cấu hình trích xuất.
4. Khôi phục VFX/animation/prefab phụ thuộc script, điều chỉnh shader/ánh sáng theo video.
5. Kiểm tra lưu game, màn hình điện thoại, hiệu năng, âm thanh và luồng thắng/thua.

Code trích từ IL2CPP có thể chỉ còn khai báo, thiếu thân hàm. Việc có đầy đủ file không tự tạo lại toàn bộ logic game. SDK quảng cáo/IAP/analytics cũ chỉ lưu tham khảo trong SourceData, chưa tích hợp vào game mới.

Thư mục Resources gốc trong thư viện được đổi tên SourceResources để Unity không tự đóng gói toàn bộ asset tham khảo vào bản game. GUID asset vẫn giữ nguyên.
