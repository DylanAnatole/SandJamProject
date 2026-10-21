# Nhật ký tiến độ

Mỗi thay đổi được ghi thêm một mục ở cuối file, kèm commit tương ứng, để dễ đối chiếu và kiểm tra.

## Quy tắc code (áp dụng đến hết dự án)

1. **Sạch, chỉn chu, dễ thêm / xóa / sửa.** Mỗi con số chỉnh theo bản gốc là một hằng có tên và có chú thích nguồn đo
   (video nào, giây nào, bao nhiêu px).
2. **Không làm code phình to.** Ưu tiên sửa code đang có hơn là viết thêm.
3. **Dùng lại cái đã có.** Trước khi viết mới, tìm helper, prefab hoặc module sẵn có (`RendererExtensions`,
   `SpriteFactory`, `PopupModule`, `SandFx`, `OutlinedText`, `VideoScreen.NavPlace`, …). Không thấy thiếu là tạo code
   mới, vì dễ bị trùng lặp.
4. **Ghi nhật ký.** Mỗi commit thêm một mục vào file này.

## Quy trình mỗi thay đổi

1. Compile và xem console, không được có lỗi.
2. Chụp màn hình bản của mình và đo pixel so với video gốc (`D:\luyentayunityxgame`).
3. Chạy **Sand Jam → Tests → Run all checks**, phải qua cả 8 bài.
4. Trả save demo về như cũ: level 68, mỗi loại booster 10 cái, 5 mạng.
5. Chạy `git add --dry-run` để chắc chắn không đưa file lấy từ game gốc lên repo (Resources, SourceData,
   OriginalReference, …).
6. Ghi một mục vào nhật ký này, rồi commit và push.

## Cách ghi một mục

```
### YYYY-MM-DD — <tên thay đổi> (`<commit>`)
- Đo / lý do: <so với bản gốc, số liệu>
- Sửa: <file chính — hằng / helper đã đổi>
- Kiểm tra: <checks, ảnh so sánh>
```

---

## 2026-10-15

Các thay đổi trong ngày:

- **Ô vuông trong làn:** nhỏ hơn 10%, bo cạnh mềm hơn, ngả ra sau, xếp sát hơn.
- **Khung tranh:** dựng lại thành viền bo tròn như bản gốc.
- **Số đếm của từng vùng trên tranh:** nhỏ đi 20%.
- **Dây xích (ChainBack gốc):** mỗi cặp dùng một vòng, tròn đều, không bị méo.
- **Home:** bảng level và nhãn JOIN dùng art gốc; màn splash giữa các level khớp bản gốc.
- **Popup:** CONGRATS và Level Failed khớp bản gốc.
- **Lưới Gallery:** chỉnh lại.

## 2026-10-16

**Dọn code**

- Tách `PopupScreens` thành các `PopupModule` riêng cho từng popup.
- Gộp helper bị trùng: `DragScroller`, helper dựng hình, `OutlinedText`.
- Bỏ code chết.

**Luật chơi theo video**

- Cặp xích được đặt vào hai ô chờ trống bất kỳ.
- Cặp xích không bị kéo khi đang bị chặn.
- Flare mở vùng nó làm lộ ra, nên tô được ngay.
- Booster dùng im lặng; Select chọn bằng cách chạm.

**Hiệu ứng**

- Lóe sáng khi đứt xích.
- Khói khi lộ khối bí ẩn.
- Vòng khói khi mở ô chờ bị khóa.
- Thẻ Gallery úp mặt cho tới khi nhận.

## 2026-10-19

| Commit | Thay đổi | Số đo (bản gốc → bản mình) |
|---|---|---|
| `a12db3a` | Home: đèn rọi, bảng level, hoa văn sân khấu, thanh trên cùng | rộng đèn 4.4; bảng level 0.37 |
| `e08c489` | Tranh, khung, chân đế | tranh +3.5%; viền 6.7% bề ngang |
| `b0899fe` | Ô chờ (pad) và khối đang chờ | pad nghiêng 55°; khối chờ 0.86 |
| `89c950e` | Bờ làn và nút booster | bờ 1339 → 1340 px; nút cao 86 px, cách nhau 124 px |
| `4866618` | Hình khối chân đế: gờ trước mỏng, đế sau sâu, mép dưới bo tối | mặt trước 27/21 px → 24/21 px |
| `92d2bc6` | Thanh điều hướng Home: thanh, tab, icon, chữ, dấu "!" | lệch ≤ 3 px |

## 2026-10-20

- Ghi quy tắc code và quy trình làm việc vào đầu file này; tạo nhật ký tiến độ.

### 2026-10-20 — Tự chơi để bắt lỗi, công cụ tạo level từ ảnh

**Đo / lý do**

- Cho máy tự chơi 41 level, từ 2 tới 500, theo lời giải của bộ giải, tốc độ x3–x4.
- Kết quả: thắng cả 41 level, không có level nào kẹt.
- Bắt được một lỗi: mỗi level báo 22 lần `MissingReferenceException`. Nguyên nhân là `CharacterDepthStyle` giữ các
  bản viền chữ đã bị `ReferenceTextOutline` hủy.
- Chuẩn bị thiết kế level mới: sửa tay hàng nghìn ô trong JSON là không khả thi, nên cần công cụ tạo level từ ảnh.

**Sửa**

- `CharacterDepthStyle`: đọc lại danh sách chữ khi có bản viền bị thay; màu viền lấy từ `OutlinedText.Ink`.
- Thêm `LevelFromPicture`: ảnh PNG 84×112 thành level mới.
- `LevelAuthoring.Publish` dùng chung cho cách tạo từ template và tạo từ ảnh.
- `OriginalConfig.ColorEntry.HasInit`: màu 14–16 không có màu nền gốc (bị đen), nên vùng đang chờ dùng màu nhạt
  tự tính.
- Đặt tên cho các màu đường viền `SandBoardTextureView.GameOutline` / `DefaultObstacleTint`, thay cho các số
  viết lặp ở nhiều chỗ.
- Thêm tài liệu `THIET_KE_LEVEL_VI.md`.

**Kiểm tra**

- Sau khi sửa, chạy lại không còn lỗi.
- Level tạo từ `Sunset.png` có 4 vùng và 24 khối, qua bộ giải, chơi tự động thắng.
- Run all checks qua 8/8.

### 2026-10-20 — Kiểm tra luồng thua / booster; popup Hết chỗ và Level Complete theo bản gốc

**Đo / lý do**

- Chạy tự động luồng thua và booster, không có lỗi: dùng Swap, Flare, Select → Hết chỗ → Chơi tiếp 400 xu (thêm
  ô thứ 6) → Hết chỗ → Bỏ cuộc → Level Failed → Chơi lại → Bỏ cuộc → Home.
- Popup Hết chỗ (video 2, giây 74):
  - khung thẻ to hơn 6.5% và thấp hơn 36 px;
  - tiêu đề nhỏ hơn 1.4 lần;
  - nút Play on thấp hơn;
  - dải Level màu cam thay vì hồng (ảnh dải vàng nhuộm không thể ra hồng);
  - thiếu vầng sáng;
  - ô xu không bị làm tối.
- Level Complete (giây 143): khung kính hẹp, viền vàng hai bên dày 42 px (bản gốc 12 px); hộp phần thưởng thấp,
  330 px so với 471 px; tiêu đề, "+10", xu và chữ "Your Reward!" nhỏ hơn 8–28%.

**Sửa**

- `ResultFx`:
  - khung kính và hộp phần thưởng đặt theo số đo (pixel so với khung thẻ); mọi popup dựng từ thẻ này đều được
    hưởng;
  - cỡ chữ riêng của Level Complete đặt trong `Start()`, nên không lan sang các popup chép từ thẻ;
  - vầng sáng xoay tách thành `SpinningLight` dùng chung.
- `PopupScreens`:
  - popup Hết chỗ dùng `FitCard`, `HideCoinBar`;
  - `TintRibbon` dùng ảnh dải gốc `bg-level-red` nếu có trên máy (không đưa lên repo), không có thì nhuộm như cũ.
- `VideoScreen.Place` / `VideoScreen.Px`: helper dùng chung để thu phóng và dịch theo pixel của video (trước chỉ
  có `NavPlace` cho Home).

**Kiểm tra**

- Hết chỗ: thẻ, tiêu đề, dải, nút đóng và nút Play on lệch ≤ 4 px so với bản gốc.
- Level Complete: tiêu đề, "+10" và xu lệch ≤ 4 px.
- Chụp lại các popup Home và Level Failed: không vỡ.
- Run all checks qua 8/8.

### 2026-10-20 — Màu nền tối của Superb, Level Complete và popup theo bản gốc

**Đo / lý do**

- Màu được giải ngược từ màu tranh và màu nền dưới lớp tối, trên video 2.
- Superb! (giây 228): lớp tím than #1E1936 che khoảng 94% (bản mình: nâu sẫm, 80%), nên tranh chỉ còn hiện mờ,
  nút cài đặt và ô Level gần như biến mất.
- Level Complete (giây 143 và 229): nền đều #06030F, không thấy tranh (bản mình: 29,30,49).
- Popup trên màn chơi (Hết chỗ, giây 74): đen khoảng 68%; màu bàn chơi còn khoảng 0,32 độ sáng.

**Sửa**

- `VideoScreen` (lớp tối của Superb).
- `ResultFx` (nền Level Complete).
- `PopupScreens.Build` (lớp tối chung của popup).

**Kiểm tra**

- Superb: tranh (33,28,57) so với bản gốc (31,24,62).
- Level Complete: nền (5,3,14) so với (6,3,16).
- Hết chỗ: nền làn (36,44,54) so với (38,49,62).
- Run all checks qua 8/8.

### 2026-10-20 — Màn Superb!: quầng sáng, cỡ chữ, đốm sáng góc

**Đo / lý do**

- Cách đo: Unity tự chụp mỗi 0,1 giây kể từ lúc thắng, đặt cạnh video 2 (giây 226–228, nhịp 0,2 giây).
- Quầng sáng sau chữ bản gốc giữ độ sáng gần tối đa, rộng khoảng 620 px; bản mình khoảng 120, rộng khoảng 400 px.
- Chữ "Superb!" chiếm 46% bề ngang màn hình; bản mình 41%.
- Đốm sáng góc của bản gốc chớp theo cặp chéo: dưới-phải với trên-trái (đỉnh 0,5–0,7 giây), rồi dưới-trái với
  trên-phải (1,3–1,5 giây); bản mình là hai đốm cố định ở hai góc dưới.

**Sửa** (`SuperbFx`)

- Các hằng số `GlowWidth`, `GlowAlpha`, `LetterScale`.
- Bảng `Corners`: vị trí, thời điểm sáng nhất và độ sáng của từng đốm góc.

**Kiểm tra**

- Quầng sáng rộng khoảng 600 px.
- Chữ rộng 46% bề ngang.
- Thứ tự đốm góc: dưới-phải 0,4 giây → trên-trái 0,6 giây → dưới-trái 1,4 giây.
- Run all checks qua 8/8.

### 2026-10-20 — Bong bóng khi đầy vùng

**Đo / lý do** (video 2, giây 50,0–52,3, cắt vùng tranh, nhịp 0,1 giây)

- Bong bóng bản gốc đường kính khoảng 136 px, lõi sáng trắng, có quầng xanh mềm tỏa ra ngoài, tồn tại khoảng
  0,8 giây.
- Bong bóng bản mình khoảng 80 px, chỉ là vòng tròn viền sắc, tồn tại 0,55 giây.
- Các khối cát bắn ra của bản mình to hơn bản gốc.

**Sửa** (`SandFx`)

- Hằng `BubbleScale` = 1,6 và `BubbleLife` = 0,8; lõi bong bóng sáng hơn.
- Quầng sáng dùng trường `Bubble.Glow`, vốn là trường `Rim` khai báo sẵn nhưng chưa dùng.
- Thuộc tính `SoftSprite` dùng chung cho chớp sáng và quầng sáng.
- Hạt cát nhỏ lại (0,03–0,065).

**Kiểm tra**

- So từng khung hình ở level 267.
- Tự chơi thắng level 267, không có lỗi.
- Run all checks qua 8/8.

### 2026-10-20 — Khối chạy lên ô chờ và khối kính trên ô chờ

**Đo / lý do** (video 2, giây 48,5–52; giây 60)

- Thời gian chạy: bản gốc mất khoảng 0,75 giây từ làn phải tới ô trái cùng; bản mình khoảng 0,55 giây.
- Kích thước: giữa đường chạy khối gốc to khoảng 1,25 lần (nhảy về phía camera), còn khối của mình nhỏ lại. Trên
  ô chờ khối gốc rộng khoảng 103 px, khối của mình 79 px. Ô chờ hai bên bằng nhau (111 px).
- Viền kính: bản gốc mang đúng màu đậm của khối (vàng khoảng #FAD719, xám khoảng #9B9B9D); bản mình pha 72% trắng
  thành màu kem.
- Vách kính: bản gốc gần như trong suốt.
- Góc nhìn: khối gốc trên ô chờ nhìn thấy miệng kính có chiều sâu; khối của mình đứng thẳng.

**Sửa**

- `SceneActorView`:
  - `RunSpeed` 3,5, `MinRun` 0,45, `MaxRun` 0,8;
  - `StashSize` 0,96;
  - `RunBulge` 0,25: khối to lên giữa đường chạy.
- `CharacterSandVessel`:
  - viền kính dùng màu cát;
  - `GlassClarity` 0,5;
  - `DockedPitch` = 0° cho khối đã rời làn. Đã thử −15°: ngả sai chiều.

**Kiểm tra**

- Chạy khoảng 0,7 giây; lúc chạy rộng 127 px, bản gốc 125–133 px.
- Trên ô chờ 103×118 px, bản gốc 103×113 px.
- Cặp xích chạy lên ô chờ vẫn đúng.
- Tự chơi thắng level 68 và 120.
- Run all checks qua 8/8.

### 2026-10-20 — Khối xoay theo hướng chạy, chân lộ ra dưới khối

**Đo / lý do** (video 2, giây 48,8–49,1)

- Khi chạy, khối gốc xoay khoảng 25° theo hướng chạy (thấy mặt trên và mặt bên), chân thò xuống dưới khối.
- Khối của mình quay thẳng mặt về camera (`MaxYaw` = 0), chân bị thân che gần hết.

**Sửa** (`SceneActorView`)

- `MaxYaw` = 25°, xoay quanh trục up của khung khối.
  - Đã thử 40°: thành hình thoi méo.
  - Đã thử xoay quanh trục Y thế giới: khối lật nằm nghiêng (khung khối ngả −54° về camera).
- `RunLegYaw` 55° → 30°, vì thân đã tự xoay.
- Thêm `RunLegDrop` 35°: đùi xoay quanh trục ngang của khung để chân lộ ra dưới khối.
  - Đã thử 60°: chân đè lên mặt khối.
  - Đã thử −40°: chân khuất hẳn.

**Còn khác:** chân bản gốc dài hơn (khoảng 60% chiều cao khối); muốn giống phải kéo dài xương chân.

**Kiểm tra:** tự chơi thắng level 68; Run all checks qua 8/8.

### 2026-10-20 — Chân dài hơn khi chạy

**Đo / lý do:** chân bản gốc thò ra khoảng 60% chiều cao khối khi chạy; sau lần sửa trước chân của mình mới lộ
một phần.

**Sửa** (`SceneActorView`)

- Thêm `RunLegStretch` 1,6: xương đùi kéo dài theo trục Y khi chạy, tăng giảm cùng `legDrop`.
- Khi chân về tư thế nghỉ thì trả đùi về độ dài cũ. Lần chạy đầu bắt được lỗi: một khối đứng yên vẫn giữ đùi dài
  1,2 lần, vì hàm thoát sớm trước khi đặt lại.

**Kiểm tra**

- Ảnh chụp lúc chạy: chân lộ rõ dưới khối.
- Mọi khối đứng yên có đùi bình thường.
- Tự chơi thắng level 68.
- Run all checks qua 8/8.

### 2026-10-20 — Khối xoay về vùng đang đổ và lắc nhẹ

**Đo / lý do** (video 2, giây 59–60,4)

- Khi đổ cát, khối gốc xoay khoảng 25° về phía vùng nhận cát và lắc nhẹ theo nhịp.
- Khối của mình đứng thẳng: phần rung có sẵn chỉ chạy với khối không có khung xương (`!Motion`).

**Sửa**

- `SandJamSceneController`: mỗi lần bắn cát ghi vị trí vùng đích vào `PourTarget` của khối.
- `SceneActorView`: khi đang đổ, xoay về phía vùng đó (`PourYaw` 25°) và lắc (`PourWobble` 4°, tốc độ
  `PourWobbleSpeed`), dùng lại `ApplyYaw` (thêm trục roll).

**Kiểm tra**

- So từng khung hình với bản gốc.
- Tự chơi thắng level 68.
- Run all checks qua 8/8.

### 2026-10-20 — Khối hết cát rời ô chờ

**Đo / lý do** (video 2, giây 60,3–61,4)

- Bản gốc: khối chớp trắng (60,6), nhảy lên khỏi ô (60,7), rồi chạy ra mép màn hình, mất khoảng 0,5 giây.
- Bản mình: không chớp, không nhảy, chạy ra trong 0,75 giây.

**Sửa**

- `SandFx`: `SpawnFlash` nhận thêm kích thước; khối rời đi được chớp trắng (`EmptyFlashSize` 1,1), dùng lại pool
  chớp sáng có sẵn.
- `SceneActorView`: thêm `ExitHop` 0,25 (nhảy ở đầu đường chạy); `ExitSeconds` 0,75 → 0,6.

**Kiểm tra**

- Ghi hình tự động lúc khối bắt đầu rời đi.
- Tự chơi thắng level 68.
- Run all checks qua 8/8.

### 2026-10-20 — Khối đầu làn bật đi ngay, hàng khối trượt lên sau

**Đo / lý do** (video 2, giây 48,45–49,05, nhịp 0,05 giây)

- Bản gốc: khối đầu làn rời chỗ trong dưới 0,1 giây; khối phía sau trượt lên trong khoảng 0,3 giây.
- Bản mình: đường chạy tăng tốc chậm (hai lớp smoothstep), nên khối phía sau trượt tới chỗ đầu làn trong 0,24
  giây khi khối đầu vẫn còn đó, và hai khối đè lên nhau.

**Sửa** (`SceneActorView`)

- Đường chạy xuất phát nhanh rồi chậm dần khi tới ô chờ (pha giữa tuyến tính và ease-out).
- `SlideSeconds` 0,24 → 0,32.

**Kiểm tra**

- Ảnh chụp: không còn chồng hình.
- Tự chơi thắng level 68.
- Run all checks qua 8/8.

### 2026-10-20 — Hàng khối to dần và tỏa ra theo phối cảnh

**Đo / lý do** (video 2, giây 150, level 268 lúc vừa vào)

- Bản gốc: mỗi hàng to hơn hàng trên khoảng 12% và giãn ngang khoảng 11%, khoảng cách hàng đều 186 px. Hàng 3 cao
  166 px so với hàng 1 cao 131 px.
- Bản mình: các hàng gần như bằng nhau (126 → 136 px).

**Sửa**

- `CharacterQueueController.QueuePosition`: gom hai chỗ viết lặp công thức vị trí; giãn ngang quanh tâm các làn
  theo `RowScale(order)`.
- Thêm `RowGrowth` 0,07; khối có thân to sẵn nên 0,07 cho tỉ lệ gần bản gốc nhất (đã thử 0,12: quá to).
- `SceneActorView.Bind` / `SlideTo` nhận kích thước theo hàng, dùng lại cơ chế `size` có sẵn (trượt lên hàng trên
  thì nhỏ dần).
- Đã thử cho hàng tiến về phía camera: hàng 3 giãn tới 223 px nên bỏ.

**Kiểm tra**

- Hàng 1/2/3 cao 126/140/156 px; bản gốc 131/148/166.
- Hàng 3 nằm ở x 197/380/559; bản gốc 208/386/558.
- Xích ở level 68 vẫn đúng.
- Tự chơi thắng level 68 và 268.
- Run all checks qua 8/8.

### 2026-10-20 — Popup Hard Level, Bonus Level và CONGRATS

**Đo / lý do** (ảnh đối chiếu bản gốc: Hard Level của level 260, CONGRATS)

- Hard/Bonus:
  - nền bản gốc là lớp tím than (bàn chơi còn 32,30,61), giống lớp của Superb!; bản mình đen 68%, bàn chơi còn rõ;
  - ô xu và biểu tượng đầu lâu / chồng xu trên thanh trên bị làm tối; bản mình vẫn sáng.
- CONGRATS: nền bản gốc xanh tím than đặc (20,18,37), không thấy bàn chơi; bản mình gần đen (8,9,10), hơi trong.

**Sửa**

- `PopupScreens.DeepDim`: một hằng dùng chung cho Superb! và Hard/Bonus, thay số viết ở `VideoScreen`.
- `LevelIntroPopups` dùng `DeepDim` và `HideCoinBar`.
- `CampaignUi`: biểu tượng loại level có cùng sortingOrder với dải Level (nằm trước dải nhờ độ sâu), nên lớp tối
  của popup phủ được nó.
- `CongratsPopup`: nền đặc #141225.

**Kiểm tra**

- Hard: nền (31,29,54) so với (32,30,61).
- CONGRATS: (19,17,35) so với (21,19,39).
- Thanh trên bình thường khi đóng popup.
- Tự chơi thắng level 30.
- Run all checks qua 8/8.

### 2026-10-20 — Lưới thẻ Gallery: đúng cỡ, tranh phủ kín thẻ

**Đo / lý do** (video 2, giây 30, sau khi nhận hết thẻ để cùng trạng thái)

- Kích thước: thẻ bản gốc 223×272 px, cột cách 288 px, hàng cách 305 px; bản mình 254×285 px, hàng cách 349 px.
- Tranh trong thẻ: bản gốc phủ kín thẻ; bản mình hụt khoảng 20 px ở đáy, lộ dải xanh đậm.
  - Nguyên nhân: khung thẻ và ảnh tranh có pivot không nằm giữa nên vẽ thấp hơn khoảng 0,1 đơn vị, còn mặt nạ
    nằm đúng chỗ, nên mặt nạ cắt đáy tranh.
- Khung vàng của thẻ "?" chưa nhận cũng lệch lên vì cùng lý do.

**Sửa** (`GalleryPopup`)

- Kích thước thẻ, bước hàng và vị trí hàng đầu theo số đo (`cw`, `ch`, `gy`, `firstRow`).
- `MaskedPicture` căn tranh và phần đặc của mặt nạ theo khung bao thực tế, không theo pivot.
- Thêm `InnerCardY(slot)`: tâm vùng trong của thẻ lấy từ khung bao khung đã vẽ; dùng chung cho thẻ đã nhận và
  thẻ "?".

**Kiểm tra**

- Thẻ 223×267 px, hàng đầu ở 971 px (bản gốc 223×272 px, 970 px).
- Tranh phủ kín, viền đều.
- Khung vàng bao đều quanh thẻ "?".
- Run all checks qua 8/8.

### 2026-10-20 — Sand Shop theo bản gốc

**Đo / lý do** (video 2, giây 6–12)

- Shop bản gốc mở ở cuối danh sách, tức mục BOOSTERS (3 booster lẻ, gói combo 6000), cuộn lên mới thấy các gói
  tiền thật. Bản mình mở ở đầu danh sách (Remove Ads).
- Phần tiêu đề:
  - bản gốc: bảng "SAND SHOP" 332×174 px, tâm ở 353 px; dải xanh cao 100 px; chữ cao 49 px;
  - bản mình: 385×130 px, tâm ở 307 px; dải 74 px; chữ 43 px.
- Mục BOOSTERS của bản mình thấp hơn khoảng 85 px.
- Biểu tượng booster của bản mình nhỏ hơn khoảng 25%, chữ giá nhỏ hơn.
- Nền trang bản gốc (27,65,98); bản mình (38,77,122).

**Sửa** (`ShopPopup`)

- `OpenShop` mở ở cuối danh sách.
- `scroll.Max` dừng cao hơn.
- `HeaderY` / `HeaderBottom`: bảng tiêu đề thấp hơn và cao hơn; dải che màu nền trang chạy từ thanh trên xuống đáy
  tiêu đề.
- `ShopTop` hạ xuống để thẻ đầu không bị khuất.
- Biểu tượng booster 48 → 62, chữ "x3" đặt chồng lên biểu tượng, giá to hơn; gói combo 44 → 56.
- Đổi màu nền trang.

**Kiểm tra**

- Tiêu đề chữ ở 321 px, cao 49 px; bản gốc 325 px, cao 49 px.
- Mục BOOSTERS ở 457 px; bản gốc 460 px.
- Ô booster ở 651 px; bản gốc 645 px.
- Đầu danh sách giống giây 9.
- Run all checks qua 8/8.

### 2026-10-20 — Popup Sand Quest

**Đo / lý do** (video 2, giây 22,3)

- Lớp tối trên Home của bản gốc đen khoảng 86% (trời 187 → 26); bản mình 68% (trời → 59).
- Popup của mình tự mang một ô xu sáng.
- Dấu "!" ở nút Gallery vẽ đè lên lớp tối (sortingOrder 40/41).
- Dòng chữ mô tả rộng hơn bản gốc khoảng 1,27 lần.

**Sửa**

- `SandQuestPopup`: lớp đen 0,86, `HideCoinBar`, chữ mô tả 0,03 → 0,024.
- `CampaignUi.AlertBadge`: cùng sortingOrder với biểu tượng, nằm trước nhờ độ sâu, nên lớp tối phủ được.
- Popup Cài đặt dùng lớp xám sáng khác (giây 15), nên chưa gộp chung.

**Kiểm tra**

- Trời (26,31,36) so với bản gốc (26,31,39).
- Chữ mô tả 509×83 px so với 507×84 px.
- Dấu "!" vẫn hiện trên Home.
- Run all checks qua 8/8.

### 2026-10-20 — Popup Cài đặt trên Home

**Đo / lý do** (video 2, giây 15)

- Lớp phủ bản gốc là xám (60,61,68) phủ khoảng 83%: nền Home nhạt màu đi, sàn sáng lên (34 → 58). Bản mình là
  lớp đen xanh 68%.
- Khung bản gốc 712×888 px; bản mình 747×929 px, thấp hơn 47 px.
- Nội dung trong khung của bản gốc nằm thấp hơn so với khung.
- Ô xu của bản mình vẫn sáng.

**Sửa** (`SettingsPopup`)

- Lớp phủ xám, `HideCoinBar`.
- Bản trên Home: `FitDynamic(.95, 6)` thu nhỏ mọi phần tử động (cả vùng chạm); riêng khung nâng thêm 44 px.
- Bản trong màn chơi chưa có cảnh đối chiếu, nên giữ nguyên.

**Kiểm tra**

- Khung 709×884 px.
- Trời (77,84,95) so với (79,84,95).
- Hàng Rung / Âm thanh / nút ✕ ở 645 / 804 / 402 px; bản gốc 649 / 809 / 407 px.
- Nút gạt chạy đúng.
- Run all checks qua 8/8.

### 2026-10-20 — Popup New Feature / Booster mới, huy hiệu booster

**Đo / lý do**

- Hai video gốc quay ở level 200+, không có cảnh popup New Feature (level 15/45/75/107) hay Booster mới
  (12/22/32), nên chưa đo pixel được.
- Theo quy tắc đã đo ở mọi popup khác trên bàn chơi: ô xu và huy hiệu bị làm tối dưới lớp phủ.
- Popup này vẫn mang ô xu sáng.
- Huy hiệu số lượng booster (sortingOrder nút + 2) nổi trên lớp tối.

**Sửa**

- `LevelIntroPopups`: `HideCoinBar(intro)`.
- `BoosterController`: huy hiệu và số cùng sortingOrder với nút, nằm trước nhờ độ sâu.

**Kiểm tra**

- Chụp level 12 và 15: popup không vỡ, huy hiệu bị làm tối.
- Đóng popup thì huy hiệu vẫn hiện trên nút.
- Run all checks qua 8/8.

## 2026-10-21

### Khối chạy: cỡ khi chạy khớp bản gốc

**Đo / lý do** (video 2, giây 48,8–49,1, cùng 4 khung hình)

- Tổng chiều cao khối + chân đã khớp: bản mình 74/116/152/170 px, bản gốc 84/122/150/168 px. Tức là chân đã đủ dài.
- Bề ngang lúc chạy: bản mình 146–150 px, bản gốc 123–133 px. Khối vừa xoay 25° vừa phồng 1,25 lần
  (`RunBulge`) nên quá to.

**Sửa:** `SceneActorView.RunBulge` 0,25 → 0,08.

**Kiểm tra**

- Bề ngang 118/122/126 px, bản gốc 123/126/128 px.
- Chiều cao 91/136/160 px, bản gốc 84/122/150 px.
- Tự chơi thắng level 68.
- Run all checks qua 8/8.

### Level tự thiết kế được đưa lên repo

**Lý do**

- Level tự làm (`Custom_NNN`) được lưu chung với 556 level gốc trong `Resources/Levels/Data`. Thư mục này bị
  `.gitignore` bỏ qua, nên level tự làm không lên được GitHub.
- Ba chỗ code (`Campaign`, `CampaignBuilder`, `LevelStructureViewer`) viết lặp đường dẫn nạp level.

**Sửa**

- Thêm `Campaign.LoadLevel(id)`: tìm level gốc trước, không có thì tìm trong `Resources/CustomLevels/`. Cả ba
  chỗ dùng chung hàm này.
- `LevelAuthoring` lưu level mới vào `CustomLevels`.
- `.gitignore` cho phép đưa riêng thư mục `CustomLevels` lên repo.
- Chuyển `Custom_001` sang thư mục mới; đã kiểm tra đó là level từ template Sunset của dự án, không phải level
  gốc.

**Kiểm tra**

- Tự chơi thắng `Custom_001` (level 916) và level gốc 2.
- "Check custom levels": OK.
- Run all checks qua 8/8.
- `git add --dry-run`: chỉ file của dự án.

### Độ khó cho công cụ tạo level từ ảnh

**Lý do:** chuẩn bị thiết kế level mới tuần sau. Trước đây công cụ chỉ ra level dễ: khối xếp đúng thứ tự vùng mở,
không có khối đặc biệt, không khóa ô chờ.

**Sửa** (`LevelFromPicture`)

- Lớp `Style` với 3 preset Easy / Normal / Hard. Mỗi preset gồm: tỉ lệ trộn khối trong làn, tỉ lệ khối "?", số cặp
  xích cùng làn, số khối đóng băng, ngưỡng mở 5 ô chờ.
- `ApplyStyle`: ngẫu nhiên có seed, nên tái tạo được.
- `BuildWinnable`: thử tối đa 12 seed cho tới khi `LevelAuthoring.Check` (bộ giải) qua; không được thì hạ mức.
- Menu chia thành 3 mục con.
- Dùng lại luật xích (`ChainPairing`) và bộ giải có sẵn, không viết thêm luật.

**Kiểm tra**

- Sunset.png:
  - Easy: 24 khối thường.
  - Normal: 3 khối "?", 1 cặp xích, khóa ô 5.
  - Hard: 6 khối "?", 2 cặp xích, 2 khối đóng băng, khóa ô 4–5.
  - Cả 3 mức đều qua ngay seed 1.
- Level Hard chơi trong game: hiện ổ khóa 150/300 và khối "?"; tự chơi thắng.
- Run all checks qua 8/8.
- Level thử đã xóa, không đưa lên repo.

### Khối bắn cát: hướng nghiêng, giật, khối cát đặc (theo góp ý của người dùng)

**Đo / lý do** (video 2, giây 49,3–49,85 và 58,6–60,3, nhịp 0,05–0,1 giây)

- Bản gốc:
  - khối xoay về phía vùng đang bắn (thấy mặt bên; khung kính trong suốt hiện thành hình lục giác);
  - phần trên nghiêng về phía đó;
  - giật thêm một chút mỗi phát bắn.
- Bản mình trước đó: chỉ xoay ngang 25° và lắc hình sin liên tục, không theo nhịp bắn.
- Khối cát: bản gốc vẫn là khối cát đặc khi bắt đầu bắn (15/20 vẫn nhìn như khối đầy); mặt cát hạ dần, viền kính
  chỉ lộ phía trên. Bản mình biến ngay thành lồng kính với một khối cát nhỏ bên trong (`GlassInner`).

**Sửa**

- `SceneActorView`:
  - `Pour(target)`: mỗi phát bắn ghi vùng đích và tạo cú giật;
  - `PourYaw` 30°, `PourTilt` 8°, `PourKick` 6°, `KickDecay`;
  - nghiêng quanh trục nhìn camera trong `ApplyYaw`;
  - bỏ lắc hình sin.
- `SandJamSceneController` gọi `Pour` mỗi phát bắn.
- `CharacterSandVessel`: cát trong kính giữ đúng cỡ khối, chỉ hạ chiều cao.

**Kiểm tra**

- So từng khung hình.
- Tự chơi thắng level 68 và 267.
- Run all checks qua 8/8.

### Khối bắn cát: chỉ xoay trái/phải, nghiêng rất nhẹ (người dùng chỉnh lại)

**Lý do**

- Người dùng xác nhận bản gốc chỉ xoay trái/phải về phía vùng bắn, nghiêng rất nhẹ, rồi bắn; không giật.
- Lần sửa trước nghiêng 8°, giật 6° mỗi phát, và xoay quanh trục của khung xương. Khung xương ngả −54° về camera,
  nên trên màn hình phép xoay đó vẫn trông như nghiêng.

**Sửa** (`SceneActorView`)

- Bỏ cú giật.
- Thêm `aimYaw`: xoay quanh trục đứng thật (Y thế giới). Góc theo hướng tới vùng đích (atan2), tối đa
  `MaxPourYaw` 40°.
- `PourTilt` 3° quanh trục nhìn.
- Khi chạy vẫn dùng `yaw` như cũ.

**Kiểm tra**

- Ảnh chụp: khối đứng thẳng, lộ mặt bên phía vùng bắn.
- Tự chơi thắng level 68.
- Run all checks qua 8/8.

### Khối bắn cát: nghiêng trên màn hình quanh tâm khối, đo trên ảnh phóng to

**Đo / lý do**

- Phóng to khối ×1,5 ở video 2 (49,35–49,45 và 58,4–59,25 giây), đặt cạnh bản mình cùng tỉ lệ.
- Bản gốc:
  - khối vẫn nhìn thẳng camera, chỉ nghiêng trên mặt phẳng màn hình về phía vùng bắn (số "15" nghiêng khoảng
    20° theo chiều kim đồng hồ);
  - nhấc nhẹ khỏi ô;
  - mặt trên chiếm khoảng 1/4 chiều cao khối.
- Bản mình:
  - xoay ngang tới 40° và văng sang phải (xoay quanh chân, không quanh tâm);
  - mặt trên chiếm gần nửa khối (`DockedPitch` 0).
- Thêm một lỗi: xoay quanh trục Z thế giới bị lật về phía trước, vì camera màn chơi nhìn chéo xuống.

**Sửa**

- `SceneActorView`:
  - bỏ xoay ngang;
  - nghiêng `roll` quanh trục nhìn camera → tâm khối (`PourPivot`), dùng `RotateAround`;
  - độ nghiêng = 0,5 × góc tới vùng đích, tối đa `MaxPourTilt` 22°;
  - nhấc `PourLift` theo độ nghiêng;
  - trả vị trí khung xương về gốc mỗi khung.
- `CharacterSandVessel.DockedPitch` 0 → 16.

**Kiểm tra**

- Ảnh ghép cạnh nhau khớp dáng khối gốc.
- Tự chơi thắng level 68 và 267.
- Run all checks qua 8/8.

### Bỏ chữ thông báo không có trong game gốc; khối bắn cát xoay ổn định quanh tâm (người dùng chỉnh lại)

**Lý do**

- Người dùng: game gốc không có các dòng chữ kiểu "HẾT CHỖ".
- Người dùng: độ nghiêng vẫn chưa đúng.
- Xem lại video 2 dày hơn (58,30–59,26 giây, mỗi 0,04 giây, phóng to):
  - khối xoay khoảng 30° quanh trục đứng, mặt trước quay sang trái, lộ mặt bên phải và mặt trên;
  - giữ nguyên góc đó suốt lúc bắn;
  - thân đứng thẳng, số chỉ nghiêng do phối cảnh.
- Lần trước nghiêng trên màn hình 20° là sai. Lần trước nữa xoay đúng trục nhưng góc đổi theo từng vùng bắn và
  xoay quanh chân.

**Bỏ chữ**

- Dải đỏ "OUT OF SPACE!": popup Hết chỗ hiện sau 0,6 giây (`LosePause`), không chữ.
- `Status` / `SelectionFeedback` dưới bàn chơi: khối không đi được thì chỉ rung.
- Các dòng thông báo trong Shop: "Purchases are not available", "Not enough coins", "+3 booster".
- "Not enough coins" ở popup Hết chỗ và popup Hồi tim: không đủ xu thì không có gì xảy ra.
- Giữ chữ nằm trong popup của chính game gốc (Sand Quest, Daily Rewards…).

**Khối bắn cát** (`SceneActorView`)

- `aimYaw` 30° (`PourYaw`) quanh trục đứng qua tâm khối (`PourPivot`), chiều theo bên của vùng bắn.
- `PourTilt` 3° quanh trục nhìn camera.
- Bỏ phần nhấc lên.

**Kiểm tra**

- Ảnh ghép phóng to: khớp dáng khối gốc.
- Luồng thua / chơi tiếp / bỏ cuộc / chơi lại: popup hiện đúng, không lỗi.
- Tự chơi thắng level 267.
- Run all checks qua 8/8.

### Tia cát bắn lên từ miệng khối (theo yêu cầu người dùng)

**Đo** (video 2, giây 58,5–59,1, phóng to)

- Mỗi phát bắn có 1–3 tia mảnh như kim, phóng thẳng lên từ mặt cát trong khối, hơi xòe sang hai bên.
- Mỗi tia dài khoảng 1/4 bề rộng khối, màu cát đậm hơn một chút, tồn tại khoảng 0,1 giây.

**Sửa** (`SandFx`)

- Thêm hệ hạt `spray`, kiểu kéo dài theo hướng bay (stretched), không trọng lực, dựng bằng `MakeSystem` có sẵn.
- `Spray()` được gọi trong vòng lặp từng khối khi số cát của khối trên ô chờ giảm (tức vừa bắn).
- Hằng `SprayMin` / `SprayMax` / `SpraySpeed` / `SprayLife` / `SprayWidth` / `SprayShade`.
- Miệng khối lấy từ khung bao thực tế đang vẽ (`CombinedBounds`, danh sách renderer lưu sẵn trong `ActorState`).
  - Đã thử `AimPoint`: điểm "trên" của khối chỉ về phía camera, nên tia hiện dưới đáy khối.
- Lúc hủy, giải phóng thêm material của `voxels` (trước bị sót) và `spray`.

**Kiểm tra**

- Ảnh phóng to đặt cạnh bản gốc.
- Tự chơi thắng level 68 và 267.
- Run all checks qua 8/8.

### Chuyển trang không mờ dần, như bản gốc

**Đo / lý do**

- Video 2, giây 44,5–48,6 và 143,5–150, nhịp 0,25 giây: bản gốc chuyển thẳng ở mọi chỗ (Home → màn chơi,
  Level Complete → CONGRATS → Home). Popup Sand Quest có hiệu ứng phóng to riêng.
- Bản mình phủ lớp tím than rồi mờ dần (0,3 giây) mỗi lần đổi trang.
- Viên cát bay lên tranh: đã so phóng to, khớp, không đổi.

**Sửa** (`VideoScreen`)

- Bỏ `StartFade` / `UpdateFade` và đối tượng "Page fade" (khoảng 30 dòng).
- Chớp sáng của Superb! thuộc `SuperbFx`, không bị ảnh hưởng.

**Kiểm tra**

- Ghi hình lúc bấm PLAY.
  - Khung đầu trong ảnh trông như chưa dựng xong là do công cụ chụp chạy ngay trong khung lệnh PLAY, trước bước
    cập nhật.
  - `Restart` chỉ tốn 6–17 ms.
- Tự chơi thắng level 267; luồng thua chạy đúng.
- Run all checks qua 8/8.

### Popup mở / đóng như bản gốc

**Đo** (video 2, bề ngang popup theo từng khung)

- Mở Sand Quest (21,17–21,57 giây): từ khoảng 1/10 cỡ lên đủ cỡ trong khoảng 0,4 giây, gần đều, không vượt cỡ.
- Đóng Settings (20,22–20,55 giây): co về 0 trong khoảng 0,33 giây, nhanh lúc đầu rồi chậm dần. Lớp tối giữ tới
  khi co xong.
- Bản mình: mở từ 1/2 cỡ, 0,3 giây, có vượt cỡ (back-ease); đóng tắt ngay.
- Thêm: sau khi thắng, bản gốc về Home rồi tự mời Sand Quest. Bản mình chỉ mời mỗi 8 giờ; chưa đủ dữ liệu để
  biết luật của bản gốc, nên giữ nguyên.

**Sửa** (`PopupScreens`)

- `PopStart` 0,1, `PopTime` 0,4 (tuyến tính), `SlowPopTime` 0,5 cho Hard/Bonus.
- Đóng: popup chuyển sang `closing` và co theo (1 − t)^1,6 trong `CloseTime` 0,33.
  - `Open` được xóa ngay, nên logic cũ không đổi.
  - Vùng chạm của popup đang co bị tắt rồi bật lại.
  - `Show` / `Close` mới kết thúc ngay popup đang co dở.
- Trang toàn màn hình (Shop, Gallery) vẫn tắt ngay.

**Kiểm tra**

- Ghi tỉ lệ theo thời gian: mở 0,1 → 1 trong 0,4 giây; đóng 1 → 0 trong khoảng 0,33 giây.
- Luồng thua / chơi tiếp / bỏ cuộc / về Home: đúng.
- Tự chơi thắng level 30 (có popup Hard).
- Run all checks qua 8/8.

