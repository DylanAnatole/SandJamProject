# Ba hỗ trợ trong màn chơi

Mở `Assets/Scenes/SandJamVideoUI.unity`, Play → PLAY. Ba nút bên phải dùng miễn phí trong bản thử, hiện chữ **Dùng**. Chưa kết nối tiền, số lượt sử dụng, quảng cáo hoặc mua hàng.

1. **Tên lửa:** bay tới một vùng chưa hiện thông tin được chọn ngẫu nhiên, hiện màu và số cát còn cần. Vùng vẫn khóa và không nhận cát cho đến khi đủ điều kiện mở bình thường. Nếu không còn vùng ẩn, nút chỉ báo thông tin.
2. **Đổi hàng:** đổi vị trí hàng ngang đầu tiên với hàng ngang thứ hai trong cả ba cột. Các hộp sâu hơn và hộp đang rót không đổi. Cột có ít hơn hai hộp được giữ nguyên.
3. **Chọn vượt lượt:** mở danh sách cuộn các hộp trong hàng chờ có màu trùng với vùng đang mở và còn cần cát. Chọn một hộp để đưa lên ô rót dù còn ở sâu phía sau. Các hộp khác giữ thứ tự. Vùng chỉ được hé lộ bởi tên lửa chưa tính là màu đang mở.

Khi chọn vượt lượt một hộp thuộc cặp nối, cả cặp đi cùng nhau và cần hai ô trống cạnh nhau. Dây chỉ hiện trong hàng chờ, tắt ngay khi cặp rời hàng. Không thể chọn khi ô chờ đã đầy hoặc bị khóa. Có nút Hủy trong bảng chọn; gameplay tạm dừng khi đang chọn để danh sách không thay đổi dưới tay người chơi.

Chơi lại đặt lại thông tin hé lộ và thứ tự hàng ban đầu. Các thao tác hỗ trợ không đổi JSON gốc, không sinh thêm cát, không tự lấp vùng.

## Phân chia code

- `Runtime/Domain/SandGame.Boosters.cs`: luật hé lộ, đổi hàng và chọn vượt lượt.
- `Runtime/Domain/GameEntities.cs`: phân biệt vùng được thấy thông tin với vùng được mở để nhận cát.
- `Runtime/Controllers/UI/BoosterController.cs`: nối ba nút, hiệu ứng tên lửa, bảng chọn và thông báo.
- `Runtime/Views/Board/SceneRegionView.cs`: màu/số lượng của vùng hé lộ.
- `Editor/Validation/BoosterChecks.cs`: kiểm tra luật, bảo toàn dữ liệu, cặp nối và trường hợp thiếu chỗ.
- `Development/Validation/BoosterSmoke.cs`: kiểm tra hit area nút, hiệu ứng, hiển thị, chọn hộp sâu và reset bằng `--booster-smoke`.

BoosterController được VideoScreen tạo khi chạy nên scene có sẵn dùng được ngay, không phải kéo lại tham chiếu Inspector. Bộ điều khiển và manager trước đó tiếp tục được dùng cho di chuyển và cát.
