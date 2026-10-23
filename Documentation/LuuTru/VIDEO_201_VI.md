# Đối chiếu video — Level 201

Video: D:/luyentayunityxgame/1791165209378_274719800131546148_6733657778166304902.mp4. Xem lại các khung 44–76, 150–204, 300–324 giây; tập trung đoạn Level 201 từ 160 giây. Ảnh trích đã lưu tại TestResults/OctoberDetail và OctoberReference.

## Đã khớp dữ liệu
Default_LevelOrderConfig.json ánh xạ màn 201 sang 112x84_New_Sport_6. Biến thể 112x84_New_Sport_6_Dupe.json khớp hình board, vùng mở đầu trắng 9 / hồng 6, divider 20, khóa ô 150/250 và hai cặp nối dọc. Sao chép nguyên JSON thành Assets/Resources/VideoUI/Level201Original.json, không đổi nguồn gốc.

Hàng chờ cũng khớp: chọn hai hộp tím rồi trắng ở cột giữa sẽ lộ hàng hồng–hồng–đen và cặp cam–vàng như khung 162 giây.

## Thay đổi trình bày
- Scene riêng dùng board và hàng chờ đúng Level 201, không dùng hình mặt của Level 147.
- Nền tối #32333E, khung xám tím và thành hai bên giảm độ bão hòa.
- Hộp thu về 86% kích thước trước để gần tỷ lệ video, số lượng cát nhỏ hơn; giữ góc nghiêng 3D, miệng rỗng và chân hoạt hình.
- Dây cùng cột bắt chéo giữa hai hộp, tô màu theo hai đầu. Chỉ hiện ở hàng chờ.
- Dòng cát bắt đầu từ miệng hộp thay vì một điểm cố định cao hơn model.
- Nhãn Level 201, số xu và nhãn booster theo video, đúng 5 ô chờ với 150/250 mở dần.

## Chạy thử
BuildVideo201/SandJam-VideoUI.exe → Play.
Unity: Assets/Scenes/SandJamVideo201.unity → Play → nút Play.
Menu dựng lại: Sand Jam > Video UI > Build Level 201 from video.

Để đối chiếu nhanh khung video 162 giây: bấm hộp tím đầu cột giữa, rồi hộp trắng tiếp theo. Phím R chơi lại.

## Kiểm chứng
Solver tìm được chuỗi thắng 24 lượt trên dữ liệu xuất. Player chạy lại đủ 24 lượt, mọi vùng được phủ kín, hai ô được mở, chơi lại khôi phục khóa và hai dây nối. Shader/mesh không phát sinh exception trong log. 11.049 file gốc nguyên vẹn. Không commit/push.

## Chưa khớp hoàn toàn
Model hộp là hình phục dựng, chưa phải toàn bộ shader/model gốc. Tốc độ cát, độ trễ bộ đếm, bo góc board và chuyển cảnh còn có thể tinh chỉnh khi đối chiếu video từng khung. Giá booster/số xu là giao diện tham chiếu; booster vẫn miễn phí trong bản test, chưa có kinh tế, quảng cáo hay trừ mạng. Màn 200/202/203 chưa được dựng trong scene này. Chưa kiểm tra hiệu năng trên điện thoại.

Bản test cũ và màn đóng băng giữ riêng; dùng BuildVideo201 để xem thay đổi mới nhất. Kết quả và ảnh: Documentation/Video201/.
