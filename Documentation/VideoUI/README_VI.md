# Scene phục dựng theo video Sand Jam

Đã bổ sung nhân vật có bộ xương và animation Walk/Sit từ dữ liệu gốc, cả ở Home và Gameplay. Chi tiết: `CHARACTER_ANIMATION_VI.md`.

Đã cập nhật camera phối cảnh, góc nghiêng, mesh có độ dày và shader toon. Xem `DEPTH_RENDERING_VI.md`; hướng dẫn mở/build hiện tại nằm ở README tại gốc repository.

## Mở để test

- Project Unity: `D:\Unity\testsand\CoreGameplayTest` (Unity 2022.3.62f3).
- Scene: `Assets/Scenes/SandJamVideoUI.unity`.
- Bản Windows: `BuildVideoUI/SandJam-VideoUI.exe`.
- Khi chạy: Loading → Home. Bấm **PLAY** để vào màn khuôn mặt Level 147.
- Chọn khối đầu một trong ba hàng bằng chuột hoặc phím **1 / 2 / 3**. **R** chơi lại, **Space** tạm dừng gameplay, **Esc** về Home.
- Phím xem nhanh giao diện: **F1** Loading, **F2** Home, **F3** bắt đầu lại Gameplay, **F4** chúc mừng, **F5** bảng thưởng. F4/F5 chỉ xem giao diện, không cấp thưởng.
- Thắng màn sẽ tự chuyển chúc mừng rồi bảng thưởng. **Next** về Home để thử lại cùng màn; chưa có hệ thống chuyển sang level tiếp theo.
- Menu tạo lại scene: `Sand Jam > Video UI > Create video screens`. Lưu scene đang sửa trước khi dùng menu.

## Những gì đã dựng

1. Loading: logo, các khối màu, thanh tải minh họa.
2. Home: nền trời, bảng Level 147, thanh tim/coin, rương, Play, Shop/Home/Gallery và nhân vật trang trí chuyển động.
3. Gameplay: nền tím nhạt, khung tím, tranh khuôn mặt, 18 vùng, ba hàng khối cát, đúng năm ô chờ, ba booster 150/500/700. Vùng khóa không lộ màu và số lượng.
4. Chúc mừng: nền tối, confetti, chữ Superb!.
5. Kết quả: Level Complete!, +10, x2 và Next.

Settings, Shop, Gallery, Join, mua tim/coin, booster và x2 **chỉ có giao diện**, theo yêu cầu trước. Tiền 2040, tim 5, nhãn Level 147 và thưởng +10 là số tham chiếu của video; chưa có lưu tiến trình, thanh toán, quảng cáo hoặc cộng thưởng thật.

## Dữ liệu và mức độ phục dựng

- Xác định được đúng hình từ `Assets/Resources/compressedlevels/112x84_New_PopArt_13.json` và bản `_Dupe`.
- Có 18 vùng; đầu màn mở môi hồng 42 và mũi tím 45. `uiDivider=10`, ammo đầu hàng 400 nên hiển thị 40.
- Giữ hai bản JSON xuất nguyên vẹn trong `Assets/Resources/VideoUI/`. File thử sinh riêng là `Assets/Generated/VideoUI/ReferencePlayable.json`.
- JSON thử dùng vùng/hàng cát từ bản thường và `gridSlotNeedAmmoCount=[0,0,0,0,300]` từ bản `_Dupe`, phù hợp khung hình video. Các cờ secret dùng làm lớp che dấu hỏi; chưa phục hồi toàn bộ cơ chế đặc biệt từ APK.
- Ô thứ năm khóa tới khi đã dùng 300 đơn vị hiển thị. Đây là cách triển khai suy ra từ dữ liệu và video, không phải logic gốc được khôi phục. Tùy chọn này chỉ bật trong scene mới; scene cũ giữ hành vi trước.
- Các màu khối lấy từ `_BaseColor` của material Character gốc; bảng màu lưu trong `palette.json`.
- Tận dụng texture/UI/mesh đã xuất. Một số khung, nhân vật và chữ dùng vật liệu, hình học và font thay thế, vì vậy chưa giống từng pixel hoặc shader của bản gốc.
- Cát vẫn dùng mô phỏng grid texture + SpriteRenderer có sẵn. Chuyển động rót hiện tại chưa phải bản sao chính xác chuyển động trong video.
- Không phục dựng các màn bên trong Shop/Gallery/Settings vì video chưa mở chúng.

## Tổ chức phần mới

- `Assets/Editor/ReferenceUI/BuildVideoScene.cs`: tạo level, bảng và khối cát.
- `Assets/Editor/ReferenceUI/BuildVideoScene.Screens.cs`: tạo các màn và nút.
- `Assets/Scripts/Runtime/UI/ReferenceUI/VideoScreen.cs`: chuyển màn, phím kiểm tra, chạy kiểm tra tự động.
- `VideoUiButton.cs`, `VideoConfetti.cs`: nút điều hướng và confetti.
- `Assets/Resources/VideoUI`: bản sao dữ liệu/ảnh gốc; nguồn và SHA256 ở `copied-assets.csv`.
- `Assets/Generated/VideoUI`: chỉ các tài nguyên sinh riêng cho scene này.

## Kiểm tra

Kết quả tại `TestResults/VideoReference/FinalSmoke/checks.txt`, ảnh từng màn trong cùng thư mục:

- Unity build thành công; không phát hiện exception trong log chạy thử.
- Năm màn hiển thị và được chụp ảnh.
- Đúng 18 vùng, 5 ô, divider 10 và hai số mở đầu 45/42.
- Chọn khối tím làm cát chảy; số mở khóa ô thứ năm giảm.
- Replay 24 lần chọn khối hoàn thành level, cả 18 vùng phủ kín; ô thứ năm đã mở.
- Chơi lại khôi phục level.
- Kiểm tra tham chiếu scene/tài nguyên sau khi chuyển từ project kiểm tra sang project chính: không thiếu GUID.

Chưa kiểm tra trên điện thoại thật. Scene con hươu `SandJamReferenceUI.unity` và các build trước được giữ lại. Dữ liệu AssetRipper ở root không được sửa.
