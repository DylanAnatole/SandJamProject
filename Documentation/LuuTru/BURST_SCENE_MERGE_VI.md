# Gộp scene và Burst Jobs — 06/10/2026

Đã gộp Level 1–3 vào `Assets/Scenes/SandJamGame.unity`. Mỗi level là prefab trong Resources/LevelPack/Prefabs; catalog chỉ giữ đường dẫn prefab để không nạp đồng thời toàn bộ các màn. LevelBootstrap nạp một prefab được chọn. Next/chọn màn reload cùng scene, giải phóng dữ liệu màn cũ. Build Settings chỉ còn scene chung.

Đã bỏ 6 scene: CoreGameplay, SandJamGameplay3D, SandJamReferenceUI và SandJamLevel001–003. Giữ SandJamVideoUI, SandJamVideo201, SandJamChainTest, SandJamFreezeTest làm scene đối chiếu / test riêng. Các builder legacy vẫn có thể dựng lại prototype nếu cần; Git giữ lịch sử.

Bổ sung Burst 1.8.7 theo gói tương thích Unity 2022.3. BurstSandSimulation dùng NativeArray cấp phát theo vòng đời vùng, hoàn tất job rồi giải phóng khi restart/destroy. Các vùng được schedule độc lập; trong mỗi vùng hạt giữ thứ tự để không đè nhau. Nhiều bước được gom vào một job. Texture/SpriteRenderer vẫn cập nhật trên main thread. Managed SandPourSimulation được giữ để kiểm tra đối chiếu.

Kiểm tra: so khớp vị trí từng hạt và từng pixel trên 13 vùng, 4 giai đoạn cấp cát, kiểm tra bảo toàn cát. Xác nhận job thực sự chạy Burst. Benchmark CPU trong Editor, 4.800 cell / 5.000 bước / 8 bước mỗi job: managed 18,68 ms, Burst Jobs 9,00 ms. Đây là một phép đo workload mô phỏng, không phải FPS toàn game hoặc hiệu năng mobile.

Player build có lib_burst_generated.dll, đã chơi thắng cả ba level, Next 1→2→3, chọn màn, lưu tiến độ và restart qua. Log không có exception hoặc báo leak native. Chưa profile thiết bị mobile; không cam kết game tổng thể nhanh gấp đôi.

Bản chạy: BuildLevelPack/SandJam-LevelPack.exe. Báo cáo cũ LEVEL_DATA_MANAGERS_VI mô tả bản scene riêng; phần kiến trúc mới ở tài liệu này thay thế mô tả đó. Runtime nay nạp prefab qua PrefabResource, không cần mỗi level một scene.

Giới hạn: prefab hiện vẫn chứa bản bố cục UI cho từng level; gộp tiếp UI/mesh dùng chung là bước sau. Chưa benchmark nhiều thiết bị. Dừng sau bản đã kiểm tra vì gần hạn mức, lưu và đẩy Git theo yêu cầu.

Tài liệu Unity: https://docs.unity3d.com/2022.3/Documentation/Manual/com.unity.burst.html
