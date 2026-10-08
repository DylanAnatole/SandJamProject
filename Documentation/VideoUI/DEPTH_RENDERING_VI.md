# Góc nhìn và shader theo video

## Quan sát

Đối chiếu lại khung hình khoảng 27,2 / 35,8 / 42,1 / 52,0 / 74,5 giây ở độ phân giải cao hơn: mặt trên hộp cát nhìn thấy rõ, khối dấu hỏi có hai mặt, bảng có khung dày và bệ nhô ra, ô chờ nằm trên sàn và có bóng mềm. Vì thế bản render hoàn toàn trực diện trước đó chưa phù hợp.

## Đã chỉnh

- Camera sân chơi dùng phối cảnh hẹp, giữ kích thước bảng gần bố cục cũ; HUD dùng camera trực giao riêng để icon/chữ không bị phóng theo chiều sâu.
- Nhân vật nghiêng khoảng 38 độ; giữ góc gốc qua Bind/Restart thay vì bị đặt lại thành góc 0.
- Sửa material cho **cả hai submesh** của nhân vật. Trước đó chỉ submesh đầu được vẽ, làm thiếu bề mặt hộp khi nhìn nghiêng.
- Shader `SandJamTest/SandToonDepth` tự viết cho Built-in Render Pipeline: texture Sand gốc, sáng tối theo normal, highlight mềm, viền đen bằng inverted hull. Viền nhân vật đầu hàng dày hơn, cập nhật khi hàng tiến lên qua MaterialPropertyBlock.
- Khung bảng, bệ, ô chờ và lan can dùng mesh có độ dày và cạnh vát. Mặt phẳng nắp có normal riêng để không xuất hiện các tam giác sáng tối giả.
- Khối dấu hỏi là cube có mặt trên/mặt trước; ẩn renderer nhân vật bên trong trong lúc còn bị che.
- Bóng tiếp xúc dưới chân, bệ và ô chờ dùng texture alpha mềm. Không dùng shadow map thời gian thực.
- Giữ grid texture/SpriteRenderer của bảng và Walk/Sit trên bộ xương gốc.

Đây là phương án phục dựng hình ảnh, không phải shader/camera gốc được trích nguyên vẹn. Chưa bảo đảm giống từng pixel. Các nút hỗ trợ vẫn chỉ có UI, không bổ sung mua hàng hoặc quảng cáo.

## Cấu trúc

- `Assets/Resources/SandToonDepth.shader`
- `Assets/Editor/ReferenceUI/BuildVideoScene.Depth.cs`
- `Assets/Scripts/Runtime/Characters/CharacterDepthStyle.cs`
- `Assets/Scripts/Runtime/UI/ReferenceUI/OverlayCameraSync.cs`

Mesh, material và texture bóng được sinh vào `Assets/Generated/VideoUI`; shader/asset đều nằm trong project để clone từ GitHub rồi mở trực tiếp.

## Kiểm tra bàn giao

Unity 2022.3.62f3, Windows:

- Build không có lỗi compile/shader.
- Camera phối cảnh và camera UI đúng loại; shader được hỗ trợ.
- Số material khớp số submesh; góc nghiêng vẫn còn sau khi bind lại nhân vật.
- Walk làm thay đổi góc khớp chân.
- Replay 24 lượt hoàn thành đủ 18 vùng; ô thứ năm mở, chơi lại được.
- Chụp và xem lại hình Gameplay và nhân vật khi đi.
- Kiểm tra tham chiếu GUID sau khi đồng bộ scene sang project chính.

Kết quả tự động: `depth-validation.txt`. Ảnh: `depth-preview.jpg` và `depth-walking.jpg`.

Chưa đo hiệu năng trên điện thoại thật. Bản hiện tại dùng skinning và outline cho nhân vật; chỉ vùng cát được tối ưu thành một texture/quad. Cache Unity, test logs, bản build Windows và video riêng không nằm trong Git.
