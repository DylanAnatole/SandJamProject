# Màu và cảm giác gameplay — 02/10/2026

- Vùng hết lượng cát không hiện dấu tích hoặc dấu ba chấm. Khi hạt cuối ổn định và vùng thực sự đầy, ẩn nhãn và nâng sáng nhẹ trong 0,35 giây, giữ mức sáng mới.
- Shader nhân vật giảm mức màu từ trên xuống theo tỷ lệ cát còn lại. Nội suy ngắn làm mức màu xuống mượt thay vì nhảy mỗi lần bắn; không thay đổi số lượng cát trong luật chơi.
- Phần đã cạn trở nên trong suốt, nhìn xuyên tới nền; viền giữ màu ban đầu. Không dùng màu xám thay cát để tránh nhầm với hộp đen, trắng hoặc xám. Shader loại bỏ mặt ruột và phần outline mặt sau ở vùng cạn, chưa thay mesh gốc bằng mesh thành hộp rỗng thực sự.
- Nhân vật dừng ngắn 0,24 giây sau khi cạn rồi dùng Walk để đi sang mép và ra ngoài màn hình trong khoảng 1,25 giây. Không thu nhỏ để biến mất. Cặp nối vẫn đợi nhau hết cát theo luật đang dùng.
- Di chuyển vào ô chờ dùng đường nội suy mềm hơn và giảm độ nhún.
- Khi thắng, giữ gameplay thêm 1,65 giây để thấy bảng hoàn thành và chuyển động rời màn, sau đó mới hiện chúc mừng.

Thử trong `Assets/Scenes/SandJamVideoUI.unity` hoặc bản `BuildVideoUI/SandJam-VideoUI.exe` được cập nhật trên máy. File build không được đưa vào repository; có thể dựng lại từ project Unity.

Shader dùng MaterialPropertyBlock riêng từng nhân vật, không nhân bản material. Chỉ các nhân vật có CharacterDepthStyle bật hiệu ứng mức cát; shader của khung sân không bị tác động. Màu trên board vẫn dùng một texture chung qua SpriteRenderer.
