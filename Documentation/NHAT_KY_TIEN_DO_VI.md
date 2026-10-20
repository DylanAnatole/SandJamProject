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

