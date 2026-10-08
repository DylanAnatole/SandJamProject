# Cấu trúc script hiện tại

Áp dụng ngày 01/10/2026 trong CoreGameplayTest. Dữ liệu AssetRipper ngoài project này không thay đổi. Tài liệu này thay thế phần sơ đồ thư mục trong SCRIPT_ARCHITECTURE_VI.md; phần khảo sát source gốc trong tài liệu cũ vẫn là tư liệu tham khảo.

| Thư mục trong Assets/Scripts | Trách nhiệm |
|---|---|
| Runtime/Data | LevelData: cấu trúc JSON, giữ nguyên tên trường khi đọc màn |
| Runtime/Domain | SandGame và GameEntities: luật mở vùng, đạn, hàng, ô chờ, thắng/thua |
| Runtime/Sand | SandPourSimulation: mô phỏng lưới cát |
| Runtime/Controllers/Gameplay | SandJamSceneController: khởi tạo và điều phối một màn; partial UI là HUD thử nghiệm cũ |
| Runtime/Controllers/Characters | CharacterQueueController: gắn Shooter với view, sắp hàng, đưa vào ô chờ, kiểm tra đã đứng yên để bắn |
| Runtime/Controllers/UI | VideoScreen: chuyển Loading/Home/Gameplay/Celebration/Result, nhận thao tác màn hình |
| Runtime/Managers | ProjectileManager: tạo, tái sử dụng, di chuyển và thu hồi hiệu ứng đạn |
| Runtime/Views/Board | SceneRegionView và SandBoardTextureView: vẽ vùng và texture cát qua SpriteRenderer |
| Runtime/Views/Characters | SceneActorView, CharacterMotion, CharacterDepthStyle: di chuyển, animation, vật liệu nhân vật |
| Runtime/Views/UI | Nút, chữ, confetti, che hàng chưa mở, đồng bộ camera UI và màn tham chiếu cũ |
| Development/Validation | Kiểm tra gameplay của scene thử nghiệm |
| Legacy | Prototype đời đầu, giữ để tương thích |
| Assets/Editor | Công cụ tạo scene/build và kiểm tra trong Unity Editor |

## Luồng phối hợp

Scene controller đọc LevelData, tạo SandGame và các đối tượng hỗ trợ cho màn đó. CharacterQueueController đồng bộ hàng/ô chờ từ SandGame xuống SceneActorView. Khi SandGame phát sinh Shot, scene controller gọi ProjectileManager tạo hiệu ứng. Các region view nhận trạng thái vùng và mô phỏng cát, sau đó cập nhật texture chung.

ProjectileManager và CharacterQueueController là lớp C# được scene controller tạo trực tiếp, không cần gắn thêm component hoặc kéo lại Inspector. Chúng thuộc vòng đời scene; không dùng singleton hay DontDestroyOnLoad. Restart gắn lại dữ liệu nhân vật và trả mọi đạn đang bay về pool.

Các script được di chuyển kèm file .meta. Tên class, namespace và trường Inspector cũ được giữ để bảo toàn scene/prefab. Không thay luật chơi, số ô chờ hoặc định dạng level trong lần tổ chức này.

## Cách mở rộng

- Luật chơi mới: thêm vào Domain; không đặt luật trong animation hoặc nút UI.
- Animation/vật liệu mới: sửa Views/Characters; cách xếp hàng hoặc vào ô chờ nằm trong CharacterQueueController.
- Hiệu ứng bắn mới: mở rộng ProjectileManager; lượng cát và điều kiện bắn vẫn do Domain quyết định.
- Nút hỗ trợ: đặt hiển thị ở Views/UI và lệnh điều khiển ở Controllers; triển khai luật hỗ trợ trong Domain khi làm chức năng.
- Chuyển nhiều level hoặc lưu tiến độ: thêm manager có trách nhiệm riêng, truyền tham chiếu từ scene controller; không gom mọi chức năng vào một GameManager toàn cục.

## Phần còn có thể tách tiếp

Scene controller vẫn giữ input gameplay, âm thanh và nhịp tick. VideoScreen vẫn có bài kiểm tra tự động bên trong. Palette còn tham chiếu prototype Legacy. Chưa thêm hệ thống lưu, level manager nhiều màn hoặc asmdef vì chưa có chức năng cần chúng. Đây là các điểm mở rộng tiếp theo, không phải chức năng đã triển khai.
