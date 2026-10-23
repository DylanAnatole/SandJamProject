# Nâng cấp theo video ngày 05/10/2026

Tham chiếu: video `1791165209378_274719800131546148_6733657778166304902.mp4`, dài khoảng 354 giây. Đối chiếu các khung hình gameplay ở 44–76 giây, 160–204 giây và đoạn hết chỗ/thua ở khoảng 316–324 giây.

## Đã nâng cấp

- Thay các chấm bay riêng lẻ từ nhân vật lên board bằng một dòng cát mảnh, liên tục cho mỗi ô đang rót. Tái sử dụng renderer theo ô, không tạo projectile mới mỗi nhịp. Dòng tắt khi ngừng bắn và khi chơi lại. Đây là phần trình bày; số lượng cát và mô phỏng filling trên board giữ nguyên.
- Hiển thị khóa và lượng cát còn cần để mở riêng cho từng ô trong 5 ô chờ. Trước đây giao diện chỉ hỗ trợ khóa ô thứ năm. Màn hiện có vẫn dùng ngưỡng cũ trong JSON; không tự gán ngưỡng Level 201 cho màn 147.
- Khi hết chỗ và không còn nước đi, hiện popup sau một khoảng ngắn để hộp đi xong. Có Chơi lại và Về trang chủ, đồng thời chặn nhấn xuyên popup vào nút phía sau.

## Phạm vi còn lại

Video có nhiều màn 200–203, quảng cáo và luồng mua thêm chỗ. Lần nâng cấp này chưa dựng lại toàn bộ các màn đó, chưa thêm quảng cáo, mua ô hoặc trừ mạng. Giữ đúng 5 ô theo yêu cầu đã có. Artwork của màn test vẫn là màn hiện tại.

Mở `Assets/Scenes/SandJamVideoUI.unity` hoặc `BuildVideoUI/SandJam-VideoUI.exe` trên máy để thử. Không đẩy Git trong đợt này. Dữ liệu AssetRipper gốc không thay đổi.

Code: `ProjectileManager` quản lý dòng cát; `GameplayFeedbackController` quản lý khóa ô và popup; `OctoberVideoSmoke` kiểm tra riêng các thay đổi. Các chức năng Booster và ruột hộp trong suốt trước đó được giữ lại.
