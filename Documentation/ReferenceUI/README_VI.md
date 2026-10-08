# Scene theo ảnh chụp — 01/10/2026

Mở project `CoreGameplayTest`, scene `Assets/Scenes/SandJamReferenceUI.unity`, nhấn Play.
Bản chạy Windows: `BuildReferenceUI/SandJam-ReferenceUI.exe`.

## Đã dựng
- Bố cục dọc 483:1075 theo ảnh người dùng: nền tối, khung bảng, thanh level/coin, ba hàng khối cát, 5 ô chờ, ba booster bên phải.
- Đã kiểm kê tất cả thư mục trong Assets (folders.csv), rà các nhóm liên quan: Scripts, Resources/JSON, Sprite, Texture2D, Mesh, Material, GameObject/prefab và Font. Không tuyên bố đã đọc thủ công từng asset.
- Tìm đúng hình hươu trong `112x84_wildlife_11.json`: 39 vùng, ba vùng ban đầu hiển thị 15, 11, 15, uiDivider=20.
- Icon cài đặt, coin, dấu cộng, nền level, nền nút booster và ba icon booster dùng bản sao PNG gốc. Khối dùng bản sao mesh RoundedCube và texture Sand gốc; shader hiển thị tương thích prototype. Frame/rail/ô chờ được dựng lại bằng hình bo góc.
- Scene có các object riêng trong HUD để chỉnh trực tiếp; UI sử dụng SpriteRenderer và TextMesh. Có preview bảng hươu trong Editor, được thay bằng texture grid khi Play.

## Giới hạn đã chủ động giữ
- Settings, cộng coin và ba booster chỉ có UI. `ReferenceUiPlaceholder` ghi rõ action/giá mẫu, không trừ coin, không mua hàng, không xử lý booster.
- Level 111, coin 210, giá 150/500 và số lượng 1 là số hiển thị theo ảnh; không khẳng định JSON này là màn thứ 111 trong thứ tự phát hành.
- Bản JSON gốc và bản Dupe được sao chép nguyên vào Resources/ReferenceUI. `Generated/ReferenceUI/ReferencePlayable.json` là bản dựng thử: đổi thứ tự một số nhân vật để khớp hàng đầu đỏ/trắng/vàng; vẫn giữ tổng màu/đạn. Không sửa JSON gốc.
- Dây và khối dấu hỏi là thể hiện hình ảnh. Dấu hỏi mở khi đến đầu hàng; chưa triển khai cơ chế chain/secret gốc. Gameplay thường (chọn nhân vật, rót cát, mở vùng) dùng mô phỏng đang có.
- Chưa xác nhận toàn bộ level mới có thể giải bằng mọi thứ tự hoặc đo hiệu năng trên điện thoại. Scene cũ vẫn còn để test core tutorial đầy đủ.

## Kiểm tra
Build Windows thành công. Kiểm tra tự động: 39 vùng, 5 ô chờ, 5 control chỉ UI, chọn vàng làm giảm lượng cần tô, restart phục hồi. Ảnh start/flow kiểm tra trực tiếp trong TestResults/ReferenceUI/FinalSmoke. Kiểm tra GUID scene/material trong project chính đạt. 11.049 file gốc trước đó được xác nhận không đổi bằng SHA256.

## File để làm tiếp
- Editor/ReferenceUI/BuildReferenceScene.cs: dựng scene, menu Sand Jam / Reference UI / Create screenshot layout. Menu có nhắc lưu scene đang chỉnh trước khi tạo.
- Scripts/Runtime/UI/ReferenceUI/ReferenceScreen.cs: dữ liệu preview và kết quả game.
- ReferenceUiPlaceholder.cs: nơi đánh dấu nút để nối chức năng sau.
- ReferenceQueueCover.cs: phần dấu hỏi hiển thị tạm.
- ReferenceTextOutline.cs: đồng bộ viền chữ với số thay đổi.
- copied-assets.csv: nguồn các PNG gốc đã sao chép. RoundedCube.asset sao chép từ Assets/Mesh/RoundedCube.asset; WildlifeOriginal.json từ Assets/Resources/compressedlevels/112x84_wildlife_11.json.

Việc dựng/build dùng project validation riêng, sau đó chép scene và asset mới về CoreGameplayTest; không đóng phiên Unity của người dùng, không ghi đè scene prototype cũ.
