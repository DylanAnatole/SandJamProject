# Nâng cấp Burst và hình khối — 06/10/2026

## Thay đổi
- Giữ Burst Jobs mô phỏng riêng từng vùng; bổ sung `PaintRegionJob` tính màu hạt và độ sáng hoàn thành bằng Burst. Job màu chạy đồng bộ (`Run`) để tránh chi phí điều phối luồng cho các vùng nhỏ; không tuyên bố phần này chạy song song.
- Buffer board chuyển sang NativeArray cấp phát một lần, upload bằng SetPixelData, vẫn tối đa một lần mỗi frame khi có thay đổi. Giải phóng khi hủy board, giải phóng buffer vùng khi restart/hủy.
- Khối cát có mép vát, pháp tuyến mượt và ánh sáng rõ hơn; tăng chiều sâu hộp và góc nhìn mặt trên. Khi cạn vẫn rỗng thật, giữ khung màu.
- Camera phối cảnh gần hơn, giữ kích thước màn chơi tại mặt phẳng board. HUD dùng camera riêng.
- Board bo góc, phản sáng nhẹ trên mặt kính. Dữ liệu màu/cell gốc không thay đổi.

## Kiểm tra
- So sánh màu Burst với công thức cũ trên 256 màu, bốn mức glow, gồm vùng chưa tô: đạt.
- So sánh từng vị trí hạt và ô đầy trên 13 vùng, bốn đợt nạp: đạt.
- Build Windows có Burst AOT; chạy bộ ba màn và màn video 201 tới thắng, restart, chuyển màn và lưu tiến độ.
- Chưa đo FPS hoặc nhiệt/điện năng trên điện thoại. Benchmark mô phỏng trong Editor không đại diện mức tăng FPS toàn game; chưa benchmark riêng job màu.

## Mở để thử
- Scene chính: Assets/Scenes/SandJamGame.unity.
- Scene theo video: Assets/Scenes/SandJamVideo201.unity.
- Bản chạy: BuildVideo201/SandJam-VideoUI.exe và BuildLevelPack/SandJam-LevelPack.exe.

Hình ảnh tiếp tục cần tinh chỉnh; đây là bước cải thiện hình khối, chưa phải bản sao hoàn toàn video. Toàn bộ thay đổi nằm trong CoreGameplayTest, không sửa dữ liệu AssetRipper bên ngoài.
