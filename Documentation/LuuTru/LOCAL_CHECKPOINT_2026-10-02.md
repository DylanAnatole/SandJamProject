# Mốc lưu trên máy — 02/10/2026

Ghi chú cập nhật: người dùng đã yêu cầu đưa các thay đổi lên Git sau mốc này. Phần dưới ghi lại trạng thái lịch sử khi tạm dừng; các cải thiện màu, ruột trong suốt và chuyển động mới được mô tả trong `FEELING_VI.md`.

Đã thêm ba hỗ trợ theo yêu cầu và cập nhật dây nối chỉ hiện trong hàng chờ. Không commit hoặc đẩy Git theo yêu cầu mới của người dùng. Giữ nguyên mọi thay đổi chưa commit khi tiếp tục.

- Mở project CoreGameplayTest bằng Unity 2022.3.62f3, scene `Assets/Scenes/SandJamVideoUI.unity` để thử cả ba hỗ trợ.
- Bản Windows mới nhất có ba hỗ trợ: `BuildVideoUI/SandJam-VideoUI.exe`.
- Scene cặp nối cũng nhận logic hỗ trợ mới khi chạy trong Editor, nhưng chỉ có một cặp và tất cả vùng đã mở nên không đủ dữ liệu để thử đổi hàng/hé lộ. File exe riêng trong BuildChainTest vẫn là bản kiểm tra dây chỉ trong hàng chờ từ bước trước; chưa dựng lại với booster.
- Đã kiểm tra model và thao tác UI của ba hỗ trợ; kết quả và hình ở `Documentation/Boosters`.
- Kiểm tra hồi quy hoàn tất 24 lượt, thắng với 18 vùng đã phủ và restart thành công.
- AssetRipper ngoài project thử không được chỉnh sửa. Code mới nằm trong CoreGameplayTest.
- Nút hỗ trợ dùng miễn phí để test. Chưa có chi phí, số lượt hỗ trợ, quảng cáo hoặc lưu tiến trình.

Xem `BOOSTERS_VI.md` để biết cách hoạt động và vị trí scripts. Dừng tại mốc này vì hạn mức 5 giờ đã dùng khoảng 88% khi kiểm tra gần cuối.
