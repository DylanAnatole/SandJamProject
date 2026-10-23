# Phân chia script để phát triển Sand Jam

Sơ đồ bên dưới ghi nhận giai đoạn đầu. Xem [cấu trúc hiện tại](SCRIPT_STRUCTURE_V2_VI.md) và [cơ chế cặp nối](CHAIN_MECHANIC_VI.md) cho các thay đổi mới.

## Kết quả đọc bản AssetRipper
Đã quét nội dung 1.120 file C# trong Assets/Scripts, ghi đường dẫn, namespace, type và số dòng vào EXTRACTED_SCRIPT_INDEX.csv; đọc chi tiết các controller và dữ liệu gameplay chính.
Bản xuất nằm chủ yếu trong Assembly-CSharp, không phản ánh đầy đủ thư mục source ban đầu. Nhiều hàm có thân rỗng, trả về null/0/false; có cả lớp coroutine do compiler sinh. Vì vậy tên trường và chữ ký hàm là bằng chứng về cấu trúc, không chứng minh thuật toán gốc.

| Nhóm gốc | Bằng chứng tiêu biểu | Trách nhiệm tham khảo |
|---|---|---|
| Luồng game/màn | GameManager.State, LevelController.UiDivider/ShootingCharacters, LevelManager | Trạng thái game, vòng đời màn |
| Level/grid tô | PaintableGrid.CompressedLevel/CompressedPaintablePart, LoadFromCompressedLevel, rows/cols/prerequests | Đọc JSON, bố trí vùng và điều kiện mở |
| Vùng tô | PaintableController.GetNextPaintablePart, PaintablePart, PaintableGridSlot | Chọn vùng nhận màu, tiến độ phủ |
| Hàng và ô chờ | LaneController, Lane/LaneSlot, StashController._stashCount/Stash | Hàng nhân vật, sức chứa và di chuyển |
| Nhân vật/input | Character, CharacterVisual, CharacterAnimator, SelectionController.CheckStashAndGo | Đạn/màu, hiển thị và chọn nhân vật |
| UI | UiCanvas, UICanvasManager, các Ui*/Fail*/Tutorial* | HUD, menu, thắng/thua, tutorial |
| Dịch vụ | ISaveSystem/JsonSaveSystem, AudioManager, SFXManager, GameEvents | Lưu game, âm thanh, sự kiện |
| Meta game | __PROJECT/Dev/Scripts/Shop, Health, Config, Save, Ui; __Funflare | Shop, mạng sống, cấu hình, ưu đãi |
| SDK/thư viện | Voodoo, AppHarbr, Audiomob, Consent*, AlmostEngine, Coffee... | Không đưa vào core gameplay khi chưa cần |

GridController/GridAStar phục vụ grid/di chuyển; PaintableGrid phục vụ bảng tô. Không gộp hai hệ thống chỉ vì đều có tên grid. GetPathForSand xuất hiện trong bản trích xuất nhưng thân hàm không cung cấp thuật toán để sao chép.

## Cấu trúc đã áp dụng trong project thử

```text
Assets/Scripts/
  Runtime/
    Data/LevelData.cs                 DTO khớp tên trường JSON
    Gameplay/SandGame.cs              Luật chơi, ô chờ, đạn, thắng/thua
    Gameplay/GameEntities.cs          Shooter, Region, Shot, GameState
    Sand/SandPourSimulation.cs        Mô phỏng ô cát độc lập Unity
    Board/SceneRegionView.cs          Hiển thị trạng thái vùng
    Board/SandBoardTextureView.cs     Texture chung + SpriteRenderer
    Characters/SceneActorView.cs      Di chuyển/hiển thị nhân vật
    Flow/SandJamSceneController.cs    Khởi tạo, input, tick, phối hợp view
    UI/SandJamSceneController.UI.cs   HUD, nút, popup thắng/thua
  Development/Validation/
    SandJamSceneController.Smoke.cs   Chạy kiểm tra bằng --scene-smoke-output
  Legacy/
    SandJamDemo.cs                    Bản thử 2D cũ
    OriginalAssetView.cs              Render portrait cũ
Assets/Editor/
  Build/BuildGameplayScene.cs         Tạo scene và build
  Validation/SandFlowChecks.cs        Kiểm tra mô phỏng cát
  Legacy/BuildDemo.cs                 Build/test đời đầu
```

Đã di chuyển kèm .meta của script có sẵn, giữ tên class/namespace và GUID để scene/prefab không mất liên kết. Các DTO giữ nguyên tên trường JSON. UI và smoke là partial của controller: đã tách file theo trách nhiệm, nhưng chưa phải các service độc lập. Không thêm asmdef lúc này vì còn phụ thuộc chung và cần giữ scene cũ tương thích.

Luồng hiện tại: JSON → LevelData → SandGame → SandPourSimulation → SceneRegionView → SandBoardTextureView → SpriteRenderer. Controller điều phối input/tick; UI chỉ trình bày và gọi lệnh. Game chỉ mở vùng sau khi view xác nhận cát đã phủ đủ. Luật ANY prerequisite hiện vẫn là giả định prototype đã ghi từ trước, chưa được chứng minh bằng code trích xuất.

## Phần chuẩn bị cho giai đoạn tiếp theo
1. Tách input, điều phối hàng/5 ô chờ, âm thanh, projectile khỏi controller thành component/service độc lập khi thêm chức năng thực tế.
2. Chuyển palette còn dùng qua SandJamDemo sang dữ liệu dùng chung, rồi loại phụ thuộc Legacy.
3. Thêm level loader/save khi làm chuyển nhiều màn; bổ sung DTO cho freeze/chain/unlocker sau khi xác minh JSON tương ứng. Hiện chỉ tutorial thường được hỗ trợ.
4. Sau khi ranh giới phụ thuộc ổn định mới chia assembly Runtime/Data, Simulation, Presentation và Editor.
5. Không mang SDK quảng cáo/analytics/shop và coroutine stub sang core mới.

Thay đổi nằm trong CoreGameplayTest. Bản APK trích xuất chỉ dùng để đọc và đối chiếu, không di chuyển/sửa script gốc.
