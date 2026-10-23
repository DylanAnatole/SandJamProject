# Level data và hệ thống quản lý màn — 06/10/2026

## Tìm thấy ở đâu?
- Dữ liệu màn: `D:/Unity/testsand/Assets/Resources/compressedlevels/` — **556 JSON**. Tên thư mục có chữ compressed nhưng các file hiện đã là JSON đọc trực tiếp.
- Thứ tự chính: `Default_LevelOrderConfig.json` — **915 lượt**, **315 tên màn khác nhau**.
- Thứ tự thử nghiệm A/B: `Default_LevelOrderConfig_TEST_AB.json` — **1.005 lượt**, **406 tên màn khác nhau**.
- Các tên trong cả hai danh sách đều tìm thấy file tương ứng. Số lượt lớn hơn số file vì có màn được dùng lại; không suy ra Level N từ thứ tự tên file trong thư mục.

Toàn bộ 556 JSON có lưới **112 hàng × 84 cột**, tổng cộng **12.471 vùng tô**. Xem `LEVEL_DATA_INVENTORY.csv` để tra từng file, số vùng/hộp, tổng cát, divider và cờ cơ chế.

## Cấu trúc và ý nghĩa
| Trường | Ý nghĩa / cách bản thử dùng |
|---|---|
| `sceneName` | Mã artwork/dữ liệu, khác số level hiển thị. |
| `rowCount`, `columnCount` | Kích thước grid; hiện tất cả là 112×84. |
| `parts` | Các vùng cần tô. |
| `parts[].name` | ID duy nhất để nối điều kiện mở vùng. |
| `ColorType` | ID màu, không phải mã RGB. |
| `amount` | Lượng cát logic cần phủ vùng, không nhất thiết bằng số pixel. |
| `isOpenedAtStart` | Vùng được nhận cát ngay khi bắt đầu. |
| `prerequests` | Các ID vùng liên quan tới việc mở vùng này. Bản thử hiện mở khi **bất kỳ** vùng liên kết nào hoàn tất và cát đã phủ kín; đây là quy tắc phục dựng, cần tiếp tục đối chiếu blocker/gimmick gốc. |
| `rows`, `cols` | Hai mảng tọa độ song song: pixel thứ i nằm ở `(cols[i], rows[i])`. Không được sắp xếp từng mảng riêng. |
| `opr`, `opc` | Cặp tọa độ pixel đường viền/vật cản của board. |
| `laneData[].ColorAmmoDatas` | Thứ tự hộp trong từng cột; phần tử đầu là hộp đầu hàng. |
| `AmmoCount` | Cát ban đầu trong một hộp, cùng đơn vị raw với `amount`. |
| `uiDivider` | Quy đổi raw sang số hiển thị. Bản thử làm tròn lên khi còn cát: `ceil(raw / uiDivider)`. Ví dụ 800/40 hiện 20; 1/40 vẫn hiện 1. |
| `gridSlotNeedAmmoCount` | Ngưỡng mở từng ô chờ, theo đơn vị hiển thị. Số còn thiếu giảm theo tổng raw đã dùng chia divider. Luôn giữ tối đa 5 ô theo yêu cầu. |
| `characterAmmo` | Thông số lượng cát chuẩn; gameplay nạp trực tiếp từng `AmmoCount`, không sinh số hộp/cát cố định từ trường này. |
| `gridSlotScale` | Thông số bố trí gốc; bản thử hiện dùng bố cục 3D chuẩn hóa, chưa áp dụng trực tiếp giá trị này. |

Bản JSON gốc còn có dữ liệu phụ như vị trí nhãn thủ công. File sao chép được giữ nguyên, nhưng model `LevelData` hiện chỉ đọc các trường đang được hỗ trợ; chưa khẳng định đã tái hiện mọi thuộc tính xuất.

## Cơ chế tìm thấy
Số dưới đây là **số hộp mang cờ**, không phải số màn:
- `IsSecret`: 1.163 — hộp bí mật; hiện có che hình ở scene tham chiếu, chưa có đầy đủ luật chung trong model.
- `IsChain`: 746; trong đó `IsChainSameLane`: 524 — nối hai hộp; đã hỗ trợ.
- `IsFreeze`: 80 — đóng băng, đi kèm `FreezeCount`; đã có bản phục dựng và màn test riêng.
- `IsHalf`: 148 — chưa hỗ trợ.
- `IsUnlocker`: 133 — chưa hỗ trợ.

**339 JSON là ứng viên thử tiếp** vì không có Secret/Half/Unlocker; chưa có nghĩa là tất cả đã được kiểm tra hay giải thành công. **217 JSON** cần bổ sung ít nhất một cơ chế chưa hỗ trợ. Loader sẽ báo lỗi cụ thể, không âm thầm xóa cờ để biến thành hộp thường.

## Ba màn đã đưa vào Unity
Dùng đúng ba vị trí đầu của danh sách chính:
| Level | JSON nguồn | Vùng | Tổng cát raw | Chuỗi thắng đã thử |
|---|---|---:|---:|---:|
| 1 | `112x84_Level31_Tutorial.json` | 5 | 9.000 | 13 lượt |
| 2 | `112x84_Level3_B.json` | 13 | 8.790 | 25 lượt |
| 3 | `112x84_Level2_B_Dupe.json` | 18 | 8.440 | 25 lượt |

Cả ba đều có ba cột hộp, divider 40 và tổng cát cung cấp bằng tổng cát yêu cầu. Không chỉnh hàng hộp hoặc lượng cát để ép thắng.

## Phân chia manager
- **LevelCatalog** — ScriptableObject chứa số màn, mã nguồn, JSON, tên scene, replay kiểm tra và ID nhóm tiến độ. File: `Assets/Resources/LevelPack/Catalog.asset`.
- **LevelDataManager** — đọc JSON, kiểm tra cấu trúc, tọa độ, phụ thuộc, cơ chế và kích thước phù hợp. Mỗi lần load trả về định nghĩa mới để tránh dùng chung dữ liệu bị sửa.
- **LevelManager** — biết màn hiện tại, chọn màn, chặn Next trước khi thắng, chuyển scene, xử lý cuối danh sách. Chọn màn test được mở tự do.
- **LevelProgressManager** — lưu trạng thái đã thắng theo ID catalog và số màn bằng PlayerPrefs; không lưu cát đang chơi dở. Đổi scene / chơi lại không xóa màn đã hoàn thành.
- **SandGame** — vẫn sở hữu luật chọn hộp, cát, mở vùng và thắng/thua.
- **SandJamSceneController / CharacterQueueController** — vẫn quản lý mô phỏng và hình ảnh trong một màn.
- **BuildVideoScene.LevelPack** — dựng scene từ JSON và catalog trong Editor. Runtime chuyển giữa scene đã dựng, chưa phải dựng mesh/nhân vật cho JSON bất kỳ ngay trong player.
- **LevelReplaySolver / LevelPackChecks / LevelPackSmoke** — kiểm tra dữ liệu, tìm chuỗi thắng và kiểm tra toàn bộ luồng nhiều màn; solver chỉ chạy trong Editor.

## Chạy thử
1. Mở `BuildLevelPack/SandJam-LevelPack.exe`.
2. Home có **LV 1 / LV 2 / LV 3** để chọn màn; bấm PLAY để chơi.
3. Thắng → bảng thưởng → Next sang màn tiếp theo. Màn cuối quay về Home.
4. R chơi lại. Chọn màn test không yêu cầu mở khóa tiến độ.

Trong Unity: mở `Assets/Scenes/SandJamGame.unity`. Ba scene đã được thêm vào Build Settings, không xóa scene có sẵn. Project có đầy đủ bản sao JSON/asset cần dùng, không cần thư mục AssetRipper bên ngoài.

## Thêm màn sau này
1. Sao chép JSON muốn thử vào project thử, không sửa bản gốc.
2. Thêm một entry trong `Catalog.asset`: Number duy nhất, SourceId, Json và SceneName duy nhất. Số màn hiển thị không bắt buộc trùng chỉ số mảng.
3. Chọn **Sand Jam > Levels > Create scenes from catalog** hoặc **Build catalog levels**. Builder giữ danh sách catalog đã chỉnh và dựng các entry trong đó.
4. Nếu dữ liệu/cơ chế chưa hỗ trợ hoặc solver không tìm được chuỗi thắng trong giới hạn, sửa/bổ sung cơ chế trước; không bỏ qua cảnh báo.
5. Home hiện có nút tắt cho ba entry đầu; các entry tiếp theo đi tới bằng Next. Có thể mở rộng trang chọn màn khi cần đưa nhiều level vào sản phẩm.

## Kiểm tra và giới hạn
Đã chạy bản player thắng cả ba màn, chuyển 1→2→3, lưu tiến độ qua scene, nạp lại tiến độ, chặn Next khi chưa thắng, từ chối index sai và chơi lại. Đã kiểm tra dependency scene không thiếu GUID. Dùng khóa PlayerPrefs riêng khi chạy smoke test và dọn khóa test sau khi xong.

Dữ liệu gốc 11.049 file giữ nguyên. Chưa đo trên thiết bị mobile; chưa hỗ trợ hot-load JSON ngoài player, tải level từ server hay toàn bộ gimmick. Báo cáo/ảnh kiểm tra ở `Documentation/LevelPack/`. Đợt này chưa commit/push Git.
