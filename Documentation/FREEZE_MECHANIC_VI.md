# Cơ chế mới: hộp cát đóng băng

## Căn cứ từ dữ liệu trích xuất
Đã lập danh sách 1.140 ảnh PNG trong Texture2D, tìm nhóm hình cơ chế và đối chiếu các chuỗi ezgif. Những dấu hiệu gồm hộp dấu hỏi (ezgif-frame-001_3), cặp nối dây (ezgif-frame-001_5), ngưỡng mở ô (ezgif-frame-040_1), và tài nguyên băng ice_new.png / IceMatCap.png. Không phải mọi ảnh PNG đều đã được xem thủ công.

Chọn đóng băng vì chưa có logic trong bản thử. Bằng chứng bổ sung:
- Assets/Scripts/Assembly-CSharp/CharacterFreeze.cs ở dự án trích xuất có NeedFreezeCount, CurrentFreezeCount và sự kiện OnCharacterMoveToStash; thân hàm đã mất.
- Assets/Resources/compressedlevels/112x84_AnimalPortrait_1.json chứa hộp màu 4, AmmoCount 400, IsFreeze true, FreezeCount 4.
- Sao chép ice_new.png vào dự án thử làm lớp băng; không sửa texture gốc. IceMatCap được xem để tham khảo, không dùng vào shader vì có chữ mẫu trên ảnh.

**Quy tắc là phục dựng có suy luận**, chưa xác minh hoàn toàn từ game gốc: mỗi hộp khác được nhận vào hàng bắn giảm một lớp băng trên các hộp còn trong hàng chờ. Cặp nối dây được nhận cùng lúc tính hai hộp.

## Cách test
Chạy BuildFreezeTest/SandJam-VideoUI.exe và bấm Play. Hoặc mở Assets/Scenes/SandJamFreezeTest.unity trong Unity, Play rồi bấm nút Play trên màn Home.

Màn có 6 hộp và 5 ô chờ; các hộp đóng băng có nhãn BĂNG 1/2/3. Chọn hộp tím đầu cột trái: hộp BĂNG 1 tan, BĂNG 2 giảm còn 1. Tiếp tục chọn hộp đã tan để mở các hộp khác. Băng tan có hiệu ứng mờ và nở nhẹ, sau đó hiện lại lượng cát.

Chuỗi test có thể thắng: cột trái → giữa → phải → trái → giữa → phải. Chờ hộp rót khi cần chỗ. Phím R để chơi lại. Đây là màn kiểm tra luật chơi dùng lại hình board, không phải phục dựng chính xác một level gốc.

- Bấm hộp còn băng bị chặn và có thông báo số lớp còn lại.
- Booster chọn ưu tiên không lấy được hộp còn băng hoặc cặp có thành viên còn băng.
- Đổi hai hàng vẫn đổi vị trí nhưng không giảm số lớp.
- Tick, bấm sai và ô chờ đầy không giảm băng.
- Chơi lại phục hồi toàn bộ số lớp ban đầu.

## Cấu trúc
- SandGame.Freeze.cs: luật giảm băng; Shooter giữ FreezeRemaining.
- CharacterFreezeView.cs + SandIce.shader: lớp băng, bộ đếm, hiệu ứng tan.
- BuildVideoScene.Freeze.cs: tạo scene / dữ liệu test riêng.
- FreezeMechanicChecks.cs: kiểm tra luật, booster, cặp nối, kẹt toàn bộ hàng, dữ liệu sai.
- FreezeMechanicSmoke.cs: kiểm tra player và chụp hình.

Menu Unity: Sand Jam > Create freeze test scene hoặc Build freeze test.

## Kết quả
Build thành công; test luật và player qua, thắng toàn màn với 6 hộp, restart phục hồi. Màn chính vẫn qua replay 24 lượt / 18 vùng. Log chạy không có lỗi mesh, shader hay exception. 11.049 file gốc không đổi. Chưa đo trên mobile thật. Chưa commit hoặc push Git.
