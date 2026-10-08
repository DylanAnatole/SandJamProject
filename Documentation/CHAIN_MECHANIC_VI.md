# Cặp hộp nối nhau

## Thử trong Unity

Mở `Assets/Scenes/SandJamChainTest.unity`, nhấn Play rồi nút PLAY. Bấm hộp đen hoặc vàng: cả hai đi vào hai ô chờ cạnh nhau trong một lượt. Nhấn R để thử lại. Bản Windows ở `BuildChainTest/SandJam-VideoUI.exe` khi build tại máy.

Đây là màn test riêng cho cơ chế nối cặp, dùng hình bảng có sẵn để kiểm tra cát. Không phải bản phục dựng tranh và bố trí đầy đủ của Level 97 trong ảnh. Scene gameplay cũ vẫn ở `Assets/Scenes/SandJamVideoUI.unity`.

## Quy tắc đã thực hiện

- Chọn một đầu lấy cả cặp; không lấy một nửa nếu thiếu chỗ.
- Cặp dùng hai ô chờ liền nhau đã mở khóa, tổng số ô vẫn là 5.
- Cặp ngang cần cả hai ra đầu hàng. Cặp dọc cần hai hộp liên tiếp đứng đầu cùng hàng; có thể bấm một trong hai.
- Cả hai cùng bắt đầu di chuyển; chỉ rót khi cả hai đến nơi.
- Mỗi hộp giữ màu và lượng cát riêng. Hộp hết trước ở lại với số 0; cả hai rời khi cùng hết cát.
- Hai thanh nối chỉ hiện khi cả hai hộp còn trong hàng chờ. Ngay khi chọn cặp lên ô rót, dây biến mất và không hiện khi di chuyển hoặc rót cát. Chơi lại khôi phục đúng một dây cho cặp trong hàng.

Điều kiện hai ô liền nhau và chờ nhau khi hết cát là quy tắc triển khai của prototype. Ảnh tĩnh không chứng minh toàn bộ cách xử lý của game gốc.

## JSON và scripts

`IsChain=true` đánh dấu cả hai đầu. `IsChainSameLane=false` ghép hai đầu cùng chỉ số trong hai hàng cạnh nhau. `true` ghép hai hộp liên tiếp trong cùng hàng. Quy tắc đọc dựa trên mẫu JSON xuất ra và tên `OpenRightChain`/`OpenBackChain`; các hàm trích xuất không có thân để xác nhận thuật toán gốc. Dữ liệu thiếu đầu hoặc chồng cặp bị từ chối thay vì đoán.

- `Runtime/Domain/ChainPairing.cs`: kiểm tra và ghép cặp từ JSON.
- `Runtime/Domain/SandGame.cs`: chọn nguyên cặp, chiếm/giải phóng ô, kiểm tra còn nước đi.
- `Runtime/Controllers/Characters/CharacterQueueController.cs`: di chuyển, đợi cả cặp, khởi tạo dây.
- `Runtime/Views/Characters/ChainLinkView.cs`: hiển thị hai thanh nối.
- `Editor/Validation/ChainMechanicChecks.cs`: kiểm tra luật cặp ngang/dọc và tình huống bị chặn.
- `Development/Validation/ChainMechanicSmoke.cs`: kiểm tra scene, chuyển động, hoàn thành bảng và restart bằng `--chain-smoke`.

Tạo lại scene qua menu `Sand Jam/Create linked pair test scene`. Asset sinh riêng ở `Assets/Generated/ChainTest`; không sửa JSON AssetRipper hoặc JSON tham chiếu trong Resources.
