# Nhân vật mang hộp cát — animation gốc

Đã tìm được liên kết trong `Assets/AnimatorController/Character.controller` và `Character_View.controller`:

- Walk trỏ tới `Assets/AnimationClip/Take 001.anim` (0,833 giây, loop).
- Idle/Idle2 trỏ tới `Assets/AnimationClip/Sit.anim` (1,3 giây).
- Các trạng thái Fly/Jump/Stack trong controller được xem có motion rỗng; không coi đó là animation đã phục hồi.
- Script gốc `CharacterAnimator.cs` chỉ còn stub, vì vậy phần điều khiển trạng thái phải viết lại.

Scene `SandJamVideoUI.unity` dùng rig 41 xương từ bản prefab `Resources/Original/CharacterVisual`, giữ skinning trực tiếp. Hai clip nguồn được sao chép nguyên vẹn vào `Resources/CharacterAnimation`. Bản clip/controller dành cho thử nghiệm nằm ở `Generated/VideoUI`; Walk lặp, Sit giữ tư thế cuối thay vì lặp ngồi liên tục.

`CharacterMotion.cs` điều khiển Walk/Sit. `SceneActorView` vẫn điều khiển vị trí: khi lên ô chờ hoặc hàng tiến lên thì Walk, tới nơi thì Sit, khi rời ô thì đi và thu nhỏ như bản prototype hiện có. Home cũng dùng model và Walk gốc thay nhân vật hình khối dựng tạm.

Đã sửa vấn đề tỷ lệ centimet của mesh xuất từ APK: dùng bone weights và bind poses để tính kích thước lúc dựng scene, không bake mesh mỗi frame. Vật liệu vẫn là shader tương thích của project thử, chưa phục hồi toon/outline shader gốc. Chuyển động đến ô do code thử điều khiển, không phải logic gốc đã dịch ngược.

Kiểm tra: hai thời điểm của Walk cho góc xương chân khác nhau, có ảnh khi nhân vật đang đi, và replay toàn bộ 18 vùng vẫn thắng. Kết quả mới nhất lưu ở `TestResults/VideoReference/AnimationFinal`.

Chỉ scene video được thay model. Các scene thử trước giữ nguyên; thay đổi SceneActorView có trường Motion tùy chọn nên scene cũ tiếp tục dùng chuyển động trước.
