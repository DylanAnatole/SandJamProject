# Cát trong biên vùng và shader — 07/10/2026

- Cửa rót nằm trên cột gần trục giữa vùng nhất, tại ô cao nhất của cột đó. Với đỉnh nhọn lệch bên, cửa rót không bị kéo sang mép vùng.
- Cả mô phỏng managed và Burst đi theo đường nối các cell hợp lệ trong vùng; không đi xuyên vùng khác. Ưu tiên đi xuống/ngang; các hốc lõm phía trên được lấp qua nhánh nội bộ. Thứ tự lấp giữ đường vào thông thoáng tới khi các nhánh đã đầy.
- Vệt rót thẳng xuống, được cắt theo mask riêng của vùng; vệt bắn từ miệng hộp đến đúng cửa nhận được khôi phục riêng, không ghi màu vào các vùng nó đi ngang qua. Điểm vệt rót cùng mặt phẳng board để tránh lệch do phối cảnh.
- Sửa liên kết shader UI, tách hiệu ứng kính của board khỏi UI; kiểm tra shader/material trước build và làm mới liên kết shader đã lưu trong Editor. Menu: Sand Jam > Validation > Repair and validate shaders.
- Kiểm tra đường hạt và bảo toàn lượng cát trên ba level gốc cùng Level 201, bốn đợt nạp. Bài test runtime kiểm tra pixel ngoài vùng không đổi, cửa rót thẳng, mask đúng và reset sạch.

Phần lưu lượng đã thay thuật toán nên số benchmark cũ chỉ là lịch sử; không suy ra mức tăng FPS hiện tại. Chưa đo trên điện thoại.

Các log/ảnh mới nằm trong TestResults/CenteredFlow*, báo cáo đính kèm trong Documentation/ContainedFlow. Không sửa dữ liệu trích xuất bên ngoài CoreGameplayTest.
