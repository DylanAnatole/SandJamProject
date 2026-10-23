# Hộp cát theo ảnh ezgif — cập nhật 05/10/2026

Đã tiếp tục và xử lý xong lỗi thân cũ còn hiện khi cạn cát ở bản tạm buổi sáng.

## Đã thực hiện
- Đối chiếu 311 ảnh ezgif-frame, đặc biệt ezgif-frame-040_1.png.
- Hộp có khung bo góc 3D, mặt trên và cạnh bên; phần cát riêng giảm chiều cao theo lượng còn lại. Hết cát thì tắt hoàn toàn khối cát, để rỗng bên trong và giữ màu khung.
- Tách chân từ rig gốc, giữ animation đi lên hàng bắn và rời màn. Shader làm cạnh bên bớt tối và tăng độ rõ hạt cát.
- Tích hợp hộp bí mật, dùng chung mesh/material giữa các nhân vật.

## Nguyên nhân lỗi buổi sáng
Mesh trích xuất không cho đọc indices trong player. Lọc mesh lúc chạy không có tác dụng nên thân cũ vẫn hiện. CharacterVesselMeshBuilder nay tách chân trong Editor, lưu CharacterLegs.asset riêng; runtime chỉ nạp mesh đã chuẩn bị. Không sửa mesh hay prefab trích xuất gốc.

## Kiểm tra
- Build Windows thành công, không còn lỗi truy cập mesh trong log chạy.
- Feeling smoke: đầy/vơi/cạn, mesh chân có tam giác và ít hơn mesh gốc, khối cát tắt khi rỗng, giữ màu khung, đi ra khỏi màn, vùng hoàn tất sáng lên, chơi lại khôi phục.
- Video smoke: 24 lượt hoàn thành đủ 18 vùng, 5 màn hình, 5 ô chờ, animation rig, shader, mở ô và chơi lại đều qua.
- Verify-OriginalData: 11.049 file gốc không thay đổi/thêm/xóa.

Bản chạy cập nhật: BuildVideoUI/SandJam-VideoUI.exe. Scene: Assets/Scenes/SandJamVideoUI.unity.
Ảnh và kết quả: Documentation/EzgifVessels/.

Đây là bản phục dựng theo ảnh, chưa khẳng định khớp hoàn toàn mọi model/shader game gốc; chưa đo hiệu năng trên thiết bị mobile thật. Chưa commit hoặc push Git theo yêu cầu.
