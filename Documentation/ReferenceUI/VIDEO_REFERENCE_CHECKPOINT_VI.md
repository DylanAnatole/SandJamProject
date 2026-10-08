# Phục dựng giao diện theo video — điểm lưu 01/10/2026

> Đã tiếp tục sau khi hạn mức được làm mới: xem `../VideoUI/README_VI.md`. Scene mới là `SandJamVideoUI.unity`, đã dựng năm màn và chạy thử thắng đủ 18 vùng. Phần bên dưới là ghi chép tại thời điểm dừng lượt trước.

## Trạng thái

- Nguồn: `C:\Users\duong\Videos\8324080125505.mp4` (khoảng 86 giây).
- Đã trích và xem 24 khung hình cách đều toàn video, chưa kiểm tra chuyển động từng frame.
- Ảnh nằm ở `TestResults/VideoReference/frame-00.jpg` đến `frame-23.jpg`; bảng tổng hợp là `contact.jpg`. Script đọc video: `Tools/Extract-ReferenceVideo.ps1`, chạy bằng Windows PowerShell 5.
- Dừng ở 92% hạn mức theo yêu cầu trước của người dùng. Chưa chỉnh scene hoặc build theo video. Bản SandJamReferenceUI hiện tại vẫn là màn con hươu từ ảnh trước.
- Không sửa dữ liệu AssetRipper gốc. Tiếp tục làm trong CoreGameplayTest.

## Các màn hình quan sát được

| Thời điểm mẫu | Nội dung | Chi tiết cần dựng |
| --- | --- | --- |
| 0–11 giây | Giao diện điện thoại, chuyển sang ứng dụng | Không phải UI game; không đưa vào scene |
| 15 giây | Loading | Nền tím, logo Sand Jam, khối màu, thanh tiến trình ở dưới |
| 18,7–22,4 giây | Home | Nền trời xanh/mây, bảng treo Level 147, nút Play xanh, thanh dưới Shop/Home/Gallery, settings, tim 5, tiền 2040, rương Join |
| 26,2–78,6 giây | Gameplay | Tranh khuôn mặt hình học, nền tím nhạt, khung tím đậm, hàng khối cát ba cột, năm ô chờ, ba booster bên phải |
| 82,3 giây | Chúc mừng | Nền tối, confetti, chữ Superb! |
| 86 giây | Kết quả | Bảng viền vàng Level 147 / Level Complete!, thưởng +10, Your Reward!, nút x2 và Next |

Video không cho thấy nội dung bên trong Shop, Gallery hoặc Settings. Không suy đoán đó là màn đã được quan sát.

## Khác biệt so với scene đang có

1. Gameplay dùng Level 147 với tranh khuôn mặt, không phải Level 111/con hươu. Cần tìm đúng JSON bằng hình dạng các vùng; số level hiển thị chưa đủ để kết luận tên file JSON.
2. Nền sáng tím nhạt, khung/tray tím đậm. Header nằm sát phía trên hơn bản ảnh cũ. Số dư trong video là 2040.
3. Khối đầu màn hiển thị 40; không được đổi toàn bộ ammo sang 40 một cách tùy tiện. Cần lấy ammo và uiDivider từ JSON đúng màn.
4. Vùng đầu mở là thanh mũi tím (45) và môi hồng (42). Các vùng khóa màu trung tính, không hiển thị số. Mở thêm vùng khi hoàn tất vùng trước.
5. Vẫn có đúng năm ô chờ. Video có ô ngoài cùng bên phải bị khóa với số đếm thay đổi; chức năng khóa cần xác minh riêng. Không thêm ô thứ sáu.
6. Ba booster có giá 150, 500, 700 trong video. Theo yêu cầu trước: dựng UI trước, chưa làm hành vi booster/mua hàng.
7. Có khối bí mật dấu hỏi. Cần phân biệt mô phỏng giao diện và logic secret thực; hiện model còn từ chối dữ liệu cơ chế đặc biệt.
8. Video cho thấy các đường cát cong từ khối trên hàng chờ đến vùng đang tô. Ảnh mẫu cách quãng chưa đủ để xác nhận tốc độ và quy tắc đổ; cần xem dày khung hình hơn ở 34–52 giây trước khi thay đổi chuyển động.

## Công việc tiếp theo

1. Đối chiếu các JSON 112x84 bằng ảnh thumbnail để xác định tranh khuôn mặt trong frame-07; giữ nguyên bản gốc, tạo bản thử riêng nếu phải giản lược cơ chế.
2. Kiểm kê texture/sprite/prefab gốc cho Home, loading và kết quả; dùng bản sao asset, giữ bảng nguồn.
3. Tạo scene video riêng để không thay mất scene con hươu đang dùng: Home → Play → Gameplay → chúc mừng → kết quả. Shop/Gallery/Settings/booster/x2 chỉ là UI nếu chưa có yêu cầu chức năng.
4. Ưu tiên layout gameplay và năm ô chờ, sau đó Home và bảng kết quả. Cần nút/đường tắt xem từng màn để kiểm tra mà không phải chơi hết level.
5. Build bằng ValidationProject đã có để tránh ảnh hưởng Unity editor đang mở. Chụp ảnh từng màn, kiểm tra ở tỷ lệ điện thoại và xác minh dữ liệu gốc không đổi.

## Điểm kỹ thuật để tiếp tục

- Builder hiện tại: `Assets/Editor/ReferenceUI/BuildReferenceScene.cs`.
- Runtime UI: `Assets/Scripts/Runtime/UI/ReferenceUI/ReferenceScreen.cs`.
- Scene hiện tại: `Assets/Scenes/SandJamReferenceUI.unity`.
- Build hiện tại: `BuildReferenceUI/SandJam-ReferenceUI.exe` (chưa phản ánh video).
- ValidationProject: `TestResults/LockedRegions/ValidationProject`; đồng bộ source mới trước khi build. Khi copy material về project chính phải dùng đúng GUID shader PixelSandSprite của project chính.
- Không nhận hạn mức reset, không tự đổi model, không tự hẹn chạy tiếp.
