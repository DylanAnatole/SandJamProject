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
5. **Luôn tối ưu cho mobile** (thêm 26/10): game phải chạy ổn định, mượt, không giật lag trên điện thoại. Không cấp phát
   bộ nhớ mỗi khung (LINQ, `new[]`, ghép chuỗi, `GetComponent` / `Find` trong `Update`); cache component; giữ ít draw
   call, ít vẽ chồng, ít hạt; việc nặng dùng Burst / job; đo trước và sau khi tối ưu.

## Quy trình mỗi thay đổi

1. Compile và xem console, không được có lỗi.
2. Chụp màn hình bản của mình và đo pixel so với video gốc (`D:\luyentayunityxgame`).
3. Tự chơi một loạt level bằng bộ giải và chạy luồng booster / thua (script cục bộ; thư mục test đã bỏ từ 23/10).
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

### Khôi phục dải "OUT OF SPACE!" (người dùng chọn theo game gốc)

**Đo** (video 1, giây 1181,3–1183,6, đọc tuần tự từng khung hình)

- Game gốc có dải hồng "OUT OF SPACE!" ngang bàn chơi: hiện ngay, đứng yên khoảng 1,75 giây, tắt; bàn chơi trống
  khoảng 0,5 giây; rồi mới hiện popup Hết chỗ.
- Lần quét đầu tưởng dải nhấp nháy, nhưng đó là do tua video không khớp khung.

**Sửa** (`PopupScreens`)

- Lấy lại `BuildOutOfSpaceBanner` từ commit trước (màu, viền, chữ đã đo ở level 261).
- `BannerTime` 1,75, `LosePause` 2,3.
- Bỏ hiệu ứng trượt mở cũ.

**Kiểm tra**

- Dải ở 658–875 px, bản gốc 666–862 px; màu (200,86,116) so với (200,87,117).
- Luồng thua / chơi tiếp / bỏ cuộc: đúng.
- Run all checks qua 8/8.

### Popup Level Failed theo video 1

**Đo** (video 1, giây 1187, level 266)

- Thẻ đỏ bản gốc 758×880 px, cạnh trên ở 414 px; bản mình 782×914 px, cạnh trên ở 434 px.
- Chữ tiêu đề bản gốc cao 61 px; bản mình 56 px.
- Nút Try Again bản mình thấp hơn 60 px.
- Bản gốc **không có nút ✕**.
- Ô xu bị làm tối.
- "Keep Going!" màu trắng; bản mình xám tím.

**Sửa** (`PopupScreens`)

- `FitCard(failed, .965, 37)`.
- Tiêu đề phóng 1,12 và hạ 10 px.
- Nút Try Again nâng 17 px.
- Chữ mô tả màu trắng.
- `HideCoinBar`.
- Ẩn nút ✕ khi còn tim; bản hết tim vẫn giữ nút ✕ và nút Home.

**Kiểm tra**

- Thẻ 754×883 px ở 415 px.
- Tiêu đề và nút ở đúng 493 / 1084 px.
- Chụp bản hết tim: không vỡ.
- Luồng thua đúng.
- Run all checks qua 8/8.

### Màn SAND JAM có câu mẹo sau Try Again

**Đo** (video 1, giây 714–722)

- Bấm Try Again: bản gốc hiện màn SAND JAM có câu mẹo hai dòng ở khoảng 73% chiều cao, không có thanh loading, trong
  khoảng 1,5 giây, rồi vào màn chơi.
- Bản mình vào thẳng màn chơi.

**Sửa**

- Thêm cờ `Campaign.RetrySplash`, đặt khi người chơi bấm `LevelManager.Retry`.
- `GameplayFeedbackController.Retry` gọi `Levels.Retry()` thay cho `Reload(true)`.
- `VideoScreen.RetrySplash()`:
  - dùng lại `QuickSplash`, ẩn thanh loading;
  - câu mẹo phóng 2,5, căn giữa, ngắt 36 ký tự mỗi dòng;
  - chờ 1,3 giây rồi gọi `Play`.
- Cờ `playAfterSplash` trong vòng Loading có sẵn.

**Kiểm tra**

- Ghi hình: Level Failed → SAND JAM + câu mẹo trong khoảng 1,2 giây → màn chơi.
- Luồng thua đúng.
- Run all checks qua 8/8.

### Nút Try Again nằm gọn trong khung kính (theo yêu cầu người dùng)

**Lý do:** người dùng muốn nút Try Again nằm gọn trong khung; trước đó nút sát mép dưới của khung kính.

**Sửa** (`PopupScreens`)

- Hằng `TryAgainScale` 0,9 và `TryAgainLiftPx` 32: nút nhỏ lại và nâng lên, cách mép dưới khung kính rõ ràng.
- Vùng bấm thu nhỏ theo nút.

**Kiểm tra**

- Chụp ở 3 tỉ lệ màn hình (16:9, iPhone, 3:4): nút đều nằm trong khung.
- Luồng bỏ cuộc → Try Again chạy đúng.
- Run all checks qua 8/8.

### Hộp tim / thời gian / "Keep Going!" nằm gọn trong khung kính (người dùng chỉnh lại)

**Lý do**

- Người dùng: ý là hộp tim / thời gian / Keep Going phải nằm gọn trong khung xanh, không lộ phần nào. Không phải
  nút Try Again, nên lần sửa nút trước được hoàn tác.

**Đo** (video 1, giây 1187)

- Hộp bản gốc cách mép trên khung kính khoảng 22 px, cao 413 px.
- Hộp bản mình (dựng từ hộp thưởng của Level Complete) cao 441 px, mép trên sát khung kính.
- Ở góc khung kính còn lộ viền xanh lá của ảnh kính.

**Sửa**

- `PopupScreens.FitFailedBox`: mép trên hạ `FailedBoxTopPx` 8 px, mép dưới cùng dải "Keep Going!" nâng
  `FailedBoxBottomPx` 22 px. Chỉ áp dụng cho popup Level Failed.
- `ResultFx`: ảnh kính vẽ to hơn vùng cắt (`ClipX` 0,9, `ClipY` 0,92), nên viền xanh lá luôn nằm ngoài vùng cắt, kể
  cả ở góc.
- Hoàn tác thay đổi nút Try Again.

**Kiểm tra**

- Ảnh ghép cạnh bản gốc: hộp nằm trong khung với lề đều.
- Bản hết tim và Level Complete: không vỡ.
- Luồng thua đúng.
- Run all checks qua 8/8.

### Ảnh ô chờ trong popup Hết chỗ: nền tối như bản gốc

**Đo** (video 1, giây 1184, level 266)

- Trong ảnh chụp ô chờ của bản gốc, khối giữ màu tươi, còn tranh, làn, sàn chuyển xám tối: tranh 192 → khoảng 155,
  sàn 140 → khoảng 61.
- Ảnh bản mình giữ nguyên màu sáng của bàn chơi.
- Chữ "Free Extra 1 Slot" của bản gốc màu trắng.

**Sửa** (`PopupScreens`)

- `MuteBackground`: pixel ít bão hòa (nền) chuyển xám và tối theo độ sáng^`MuteGamma` 1,7. Pixel tươi (khối) giữ
  nguyên màu; từ `VividFrom` 0,45 có chuyển tiếp mềm.
- Áp dụng ngay sau khi chụp trong `CaptureStash`.
- Chữ mô tả màu trắng.

**Kiểm tra**

- Ảnh ghép cạnh bản gốc: tông màu tương tự.
- Luồng chơi tiếp chạy đúng: ô thứ 6 có, trạng thái Playing.
- Run all checks qua 8/8.

### Khung ảnh ô chờ trong popup Hết chỗ không còn viền tím nhạt

- **Đo** (video 1, giây 1184): khung ảnh bản gốc là viền xanh tím than tối, không có viền tím nhạt như hộp thưởng
  của Level Complete.
- **Sửa** (`PopupScreens`): nhuộm ảnh khung của popup Hết chỗ bằng `PictureFrameTint`. Chỉ áp dụng cho popup này.
- **Kiểm tra:** ảnh ghép cạnh bản gốc; Run all checks qua 8/8.

### Nút "Claim All Cards" màu xanh thép như bản gốc

- **Đo** (video 2, giây 30): nút bản gốc màu (57,99,161); bản mình (40,81,179), xanh tím.
- Trước đây ghi là không nhuộm ra được. Thực ra ảnh nút có màu gốc (93,158,248), nên nhân với
  (0,61; 0,63; 0,65) là ra đúng màu.
- **Sửa:** `GalleryPopup`, màu nhuộm của nút.
- **Kiểm tra:** (59,102,161) so với (57,99,161); Run all checks qua 8/8.

### Băng chuyền ảnh bìa trong Gallery theo bản gốc

**Đo** (video 2, giây 30, cùng bộ MUSICIAN)

- Thẻ chính bản gốc: khung xanh 340×384 px, tâm ở 500 px.
- Bản mình: 284×374 px, tâm ở 523 px.
- Thẻ bên cạnh của bản gốc nằm xa hơn; mũi tên nằm sát ngoài mép thẻ.

**Sửa** (`GalleryPopup`)

- Các hằng `CardGrowX` 1,2, `CardGrowY` 1,03, `CarouselY` 2,7, `SideX` 2,72, áp dụng cho cả thẻ chính và thẻ bên.
- Mũi tên ra x 1,28, thấp hơn 0,1.

**Kiểm tra**

- Ảnh ghép cạnh bản gốc: thẻ chính cùng cỡ và vị trí.
- Run all checks qua 8/8.

### Ô xu trên màn CONGRATS bị làm tối

- **Đo** (video 2, giây 145): bản gốc vẫn hiện ô xu nhưng tối hẳn (độ sáng khoảng 0,38); bản mình sáng rõ.
- **Sửa:**
  - thêm `PopupScreens.DimCoinBar(p, brightness)`, dùng chung danh sách `CoinBar` với `HideCoinBar`;
  - `CongratsPopup` gọi `DimCoinBar` với độ sáng 0,38.
- **Kiểm tra:** Run all checks qua 8/8.

### Thẻ CONGRATS theo bản gốc

**Đo** (video 2, giây 145)

- Thẻ bản gốc ngắn hơn: mép dưới cao hơn khoảng 116 px.
- Dải ruy băng hẹp hơn 4% và thấp hơn 15 px; tên bộ sưu tập thấp hơn một chút.
- Chữ % và "Tap To Continue" của bản gốc cao hơn 10–28 px.
- Tia sáng của bản gốc vàng sáng, tỏa từ ruy băng lên; bản mình cam tối, tâm cao.

**Sửa** (`CongratsPopup`)

- `CardShortenPx` 116: co ảnh thẻ, giữ mép trên.
- Đặt lại vị trí và cỡ ruy băng, chữ CONGRATS, tên bộ sưu tập, chữ % và "Tap To Continue".
- Tia sáng hiện đủ màu của ảnh (`RaysColor`), tâm đặt tại ruy băng.

**Kiểm tra**

- Ruy băng, chữ % và "Tap To Continue" lệch ≤ 12 px so với bản gốc.
- Ảnh ghép cạnh bản gốc khớp khung.
- Run all checks qua 8/8.

### Viền chữ trên CONGRATS gần đen

- **Đo** (video 2, giây 145): viền chữ "Makeup", "% Completed" và "Tap To Continue" của bản gốc gần đen
  (12,9,13); bản mình tím (59,41,107).
- **Sửa:** `CongratsPopup.TextInk`, gán qua `OutlinedText.Add` có sẵn.
- **Kiểm tra:** viền đo được (12–25, 9–22, 13–18); Run all checks qua 8/8.

### Viền chữ toàn game gần đen, như bản gốc

**Đo** (video 1 và 2)

- Viền chữ của bản gốc ở mọi chỗ đều gần đen (#0B080C–#0D0A0D): thanh điều hướng, số Level, SAND SHOP, tiêu đề
  popup (Out of Space, Sand Quest, Level Failed), dải "OUT OF SPACE!", Level Complete, CONGRATS.
- Bản mình dùng tím (43,31,77), tím đậm hơn (59,41,107) trong `OutlinedLabel`, đỏ sẫm cho tiêu đề Level Failed
  và dải Hết chỗ.

**Sửa** (một chỗ dùng chung)

- `TextOutliner.Ink` = #0C090D.
- `PopupScreens.OutlinedLabel` dùng `TextOutliner.Ink` thay số riêng.
- Bỏ màu đỏ của tiêu đề Level Failed và dải "OUT OF SPACE!".
- `ReferenceTextOutline` chỉ giữ màu viền cũ trong scene khi đó là màu sáng / rõ màu; viền tím tối cũ đổi theo
  `Ink`.
- Bỏ `CongratsPopup.TextInk` vừa thêm, vì giờ trùng với màu mặc định.

**Kiểm tra**

- Viền đo được trên Home, Level Failed, CONGRATS: gần đen.
- Tự chơi thắng level 267; luồng thua đúng.
- Run all checks qua 8/8.

### Số đếm trên tranh: cỡ và viền mảnh như bản gốc

- **Đo** (video 2, giây 158, phóng to số "6"): chữ số bản gốc 16×25 px, viền tối mảnh. Sau khi đổi màu viền chung,
  viền của bản mình dày và đen đậm.
- **Sửa** (`SceneRegionView`):
  - `CounterScale` 0,77 (trước 0,8);
  - `CounterOutlineThickness` 0,045, gán một lần cho `OutlinedText` của số đếm.
- **Kiểm tra:**
  - số "6" đo được 16×25 px, bằng bản gốc; viền mảnh;
  - tự chơi thắng level 68;
  - Run all checks qua 8/8.

### Số trên khối trong làn

**Đo** (video 2, giây 150)

- Số ở các hàng sau của bản gốc: chữ nhạt pha màu khối, viền là màu khối đậm hơn (vàng đậm, xanh lá đậm), không
  đen.
- Số của bản gốc rộng hơn khoảng 15%.

**Sửa**

- `CharacterDepthStyle`:
  - hàng sau: chữ pha 30% màu khối (`BackTint`), viền = màu khối × 0,5 (`BackInkShade`);
  - viền mảnh `LabelOutline` 0,055.
- `CharacterSandVessel.LabelSize` 1,07.
- `OutlinedText.SetThickness`: đổi độ dày viền thì tự sắp lại viền ở khung sau. Dùng cho số trên khối và số đếm
  trên tranh.

**Còn khác:** phông chữ bản gốc tròn, giãn chữ hơn (font khác).

**Kiểm tra**

- Ảnh ghép cạnh bản gốc.
- Tự chơi thắng level 68.
- Run all checks qua 8/8.

### Thanh trên của màn chơi

**Đo** (video 2, giây 153)

- Nút cài đặt, ô Level và ô xu của bản gốc cao hơn khoảng 20 px.
- Nút cài đặt nhỏ hơn, lệch trái; ô Level rộng hơn 5%; ô xu lệch phải.

**Sửa**

- `VideoScreen.FitGameplayHud`, dùng lại helper `Place` có sẵn (thu phóng quanh tâm, dịch theo pixel video).

**Kiểm tra**

- Ô Level ở (336,104,206×49), bản gốc (337,104,206×50).
- Nút cài đặt ở (45,97,64×62), bản gốc (47,97,59×62).
- Ô xu lệch ≤ 5 px.
- Run all checks qua 8/8.

## 2026-10-22

### Huy hiệu số lượng booster

**Đo** (video 1, giây 720)

- Huy hiệu vàng của bản gốc khoảng 46 px, nằm thấp ở góc dưới-trái của nút; bản mình 37 px, cao hơn 17 px.
- Số trên huy hiệu của bản gốc màu trắng, viền tối; bản mình màu nâu.

**Sửa** (`BoosterController`)

- `BadgeSize` 0,3; `BadgeShiftPx` (6, 17).
- Số màu trắng, thêm viền `TextOutliner.Ink`. Vì huy hiệu rất nhỏ trong không gian thế giới, phải kéo dãn trục sâu
  của chữ để viền nằm sau chữ nhưng trước huy hiệu.

**Ghi chú môi trường:** cửa sổ Game của Unity bị đổi về "Free Aspect" (640×480, màn ngang), làm các nút booster
được đặt ra ngoài màn hình. Đã chuyển lại "9:16 Aspect". Code không lỗi.

**Kiểm tra**

- Huy hiệu 47×45 px ở đúng góc như bản gốc.
- Luồng thua đúng.
- Run all checks qua 8/8.

### Lối đi và nhân vật ở Home

**Đo** (video 2, giây 244)

- Lối đi bản gốc: mép sau ở 1176 px, khe giữa ở 1224 px, mép trước ở 1288 px. Bản mình 1144 / 1200 / 1272 px.
- Nhân vật đi trên lối đi của bản gốc nhỏ hơn và thấp hơn.

**Sửa** (`VideoScreen`)

- `WalkwayTopDropPx` 32 và `WalkwayBottomDropPx` 14. Sân khấu phía dưới co mép trên theo, để trời hiện xuống tới lối đi.
- `MascotScale` 0,8 và `MascotDropPx` 24.

**Kiểm tra**

- Lối đi 1176 / 1224 / 1288 px, trùng bản gốc.
- Ảnh ghép: nhân vật cùng cỡ, chân trên lối đi.
- Run all checks qua 8/8.

### Chữ trên bảng Level ở Home

**Đo** (video 2, giây 244)

- Số và chữ LEVEL của bản gốc màu trắng, viền đen mảnh. Bản mình viền xanh dày (đặt riêng trong `SoftSignInk`).
- Bản mình số to hơn 6% và thấp hơn 15 px; LEVEL to hơn 12% và thấp hơn 14 px.

**Sửa** (`VideoScreen`)

- `SoftSignInk` thay bằng `SignText`: dùng màu viền chung, độ dày `SignOutline` 0,03, chỉnh cỡ và vị trí bằng `Place`.

**Kiểm tra**

- Số (339,376,210×92) so với (343,373,211×97).
- LEVEL (383,563,122×33) so với (387,560,122×33).
- Run all checks qua 8/8.

### Nút PLAY ở Home

- **Đo** (video 2, giây 244): nút bản gốc 372×110 px, chữ PLAY 198×61 px. Bản mình 354×104 px, chữ khoảng
  180 px rộng.
- **Sửa** (`VideoScreen`): `PlayButtonScale` 1,05, `PlayButtonDropPx` 6, `PlayLabelGrow` 1,08, dùng lại `Place`.
- **Kiểm tra:** nút 370×109 px, chữ 195×60 px; bấm PLAY vẫn vào màn chơi; Run all checks qua 8/8.

### Sự kiện JOIN mở từ level 85; thắng / thử lại đều về Home (yêu cầu người dùng)

**Sửa**

- `SandQuest.StartLevel` 30 → 85. `CampaignUi` ẩn rương JOIN, nhãn và chữ JOIN ở Home cho tới khi mở khóa
  (`questParts`).
- `LevelManager`: `LoadNext` (sau thắng) và `Retry` (Try Again) cùng gọi `GoHome()`: màn SAND JAM ngắn rồi về Home,
  người chơi bấm PLAY để chơi tiếp.
- Bỏ code không còn dùng: `Campaign.HomeAfterLevel` (trước đây 10 level đầu chơi nối tiếp),
  `Campaign.RetrySplash`, `VideoScreen.RetrySplash` / `playAfterSplash` / `RetryHint*`.
- README cập nhật mốc 85.

**Kiểm tra**

- Level 68: không có rương; level 90: có rương.
- Thắng level 12 → CONGRATS → bấm tiếp → Home (level 13).
- Thua level 68 → Bỏ cuộc → Try Again → Home.
- Run all checks qua 8/8.


### Cảm giác chơi: cát đổ lên tranh giống bản gốc, khối không đi được thì lắc nhẹ (yêu cầu người dùng)

**Đo** (video 2, giây 57,5–61, phóng to từng pixel)

- Cát đã đổ: màu gốc, khoảng 1/5 hạt tối hơn (×0,85), không có hạt sáng. Cát vừa rơi xuống đã có màu cuối, không
  ửng trắng.
- Dòng cát rơi: vạch 1 px đúng màu cát (bản mình trước đây tối hơn).
- Tốc độ chảy đều, bản mình theo kịp nên giữ nguyên.

**Sửa**

- `SceneRegionView`:
  - `SandShade` còn 2 tông (`DarkGrainPercent` 20, `DarkGrainShade` 0,85), bỏ 2 tông phụ.
  - Dòng cát và hạt đang rơi dùng màu cát, không làm tối / sáng.
  - Ánh cát mới giảm còn ánh nhẹ: `FreshLift` 0,32 → 0,1, `FreshSteps` 180 → 120.
- `SceneActorView.Shake`: lắc trái phải nhẹ, 2 nhịp trong 0,34 s, biên độ 0,035, nhỏ dần
  (`ShakeSeconds`, `ShakeSwings`, `ShakeAmount`); trước đây rung nhanh 0,06. Dùng lại chỗ gọi sẵn có khi ô chờ đầy
  hoặc chạm khối không ở đầu làn.

**Kiểm tra:** chụp lại vùng xám level 267 đúng 2 tông như bản gốc; Run all checks qua 8/8.

### Cảm giác chơi: tốc độ đổ cát của khối bằng bản gốc

**Đo**

- Video 2, giây 152,5 (khối vàng) và giây 156 (khối tím, 20 → 1 trong 2,0 s): mỗi khối đếm xuống khoảng 1 đơn vị
  mỗi 0,1 s.
- Bản mình trước đây chỉ 5,25 đơn vị/s, chậm gần gấp đôi.
- Cách quay mới (chỉ dùng trên máy): dừng game rồi tự gọi `Advance(0,04)` và chụp từng bước, nên khoảng cách giữa
  các khung đúng tuyệt đối (game chạy theo thời gian thực nên `timeScale` không làm chậm được).

**Sửa** (`SandJamSceneController.Restart`)

- Bỏ hệ số 0,42 khó hiểu, thay bằng `PourUnitsPerSecond` 9,5 và `PourTick` 0,02 s.

**Kiểm tra**

- Quay lại level 267: khối đếm 20 → 10 trong 0,96 s, khớp bản gốc.
- Đo trực tiếp `Shake`: lệch ±0,03, 2 nhịp, tắt dần.
- Tự chơi level 2, 12, 68, 150, 267: thắng cả 5, không lỗi.
- Run all checks qua 8/8.

### Cảm giác chơi: hiệu ứng khi mở vùng mới giống bản gốc

**Đo** (video 2, giây 152,9–154,3)

- Vùng đổ đầy không có hiệu ứng riêng, số đếm chỉ biến mất.
- Các vùng vừa mở nổ cùng lúc. Mỗi vùng có 1 bong bóng ngay tại số đếm: gần như trong suốt, vành sáng mảnh, nhìn
  xuyên thấy vùng và số, tắt sau khoảng 0,3 s.
- Pháo giấy là các khối cát to, màu của vùng đó, kèm vài mảnh xanh lá.
- Bản mình trước đây ngược lại: nổ ở vùng vừa đầy, 5 bong bóng xanh đục che kín vùng, khối cát li ti nhiều màu.

**Sửa** (`SandFx`)

- `RegionBursts` theo dõi `InformationVisible`: vùng nào vừa hiện ra thì gọi `RegionOpens`. Vùng mở sẵn từ đầu
  level không nổ; vùng mở bằng Flare cũng nổ.
- `RegionOpens`: 1 bong bóng, `BurstGrains` 34 khối cát (cỡ 0,06–0,1), `ConfettiShare` 25% màu xanh lá,
  `BurstSparkles` 16.
- Bong bóng: thân gần trong suốt, quầng sáng chỉ lúc mới hiện, `BubbleLife` 0,8 → 0,45 s.
- Bỏ `palette` (màu cả tranh), không còn dùng.

**Kiểm tra**

- Quay lại level 267 từng bước 0,04 s: vùng mới nổ đúng màu, số đếm vẫn đọc được qua bong bóng.
- Tự chơi level 2, 68, 267: thắng cả 3. Run all checks qua 8/8.

### Cảm giác chơi: vùng mới mở sáng lên; số đếm trên vùng không nảy

**Đo** (video 2)

- Giây 153,1–153,5: vùng vừa mở sáng lên màu trắng xanh, khoảng 30–40% về phía trắng (đo màu: G 128 → 73), rồi phai
  về màu vùng trong 0,4 s.
- Giây 152,7–153,0: số đếm trên vùng giảm 6 → 5 → 4 mà không nảy (cao 26 px ở mọi khung). Bản mình nảy 28% mỗi lần
  giảm, và với tốc độ đổ mới (10 lần/s) thì số rung liên tục.

**Sửa** (`SceneRegionView`)

- Vùng mở giữa màn (trước đó bị ẩn): `revealGlow` 1 → 0 trong `RevealSeconds` 0,4. Màu nền vùng trộn với
  `RevealWash` theo `RevealStrength` 0,4, dùng lại `PaintBase`. Vùng mở sẵn từ đầu không sáng.
- Bỏ hẳn `counterPunch` (nảy số đếm) và biến `counterScale` chỉ dùng cho nó.

**Kiểm tra**

- Quay level 267 từng bước 0,04 s: vùng hồng sáng lên rồi phai; số đếm đứng yên.
- Run all checks qua 8/8.

### Cảm giác chơi: âm thanh theo đúng bản gốc

**Đo** (video 1 có tiếng; video 2 im lặng)

- Tách tiếng video 1, tìm 307 lần có tiếng, so dạng sóng từng lần với 7 file âm thanh gốc (khớp 0,95–1,0).
- `Input`: 258 lần, đúng mỗi lần chạm chọn khối.
- `pop`: khi bấm PLAY ở Home (giây 151,5, 313,6, 533,4).
- `Victory`, panel Level Complete, `claim_gift`: màn kết thúc.
- Lúc đổ cát, đầy vùng hay khối hết cát: im lặng hoàn toàn (-120 dB). File `audiomob-chime-soft` không lần nào phát.

**Sửa**

- `AudioManager`:
  - bỏ tiếng rè đổ cát tự tạo (`SandHiss`);
  - bỏ tiếng `pop` khi đầy vùng (kèm cao độ tăng dần) và khi khối hết cát;
  - nút PLAY phát `pop`.
- `SandJamSceneController`: bỏ `CompleteSound` và `CompletedRegions`; `BuildVideoScene` bỏ gán `CompleteSound`.

**Kiểm tra**

- Run all checks qua 8/8.
- Tự chơi level 2, 68: thắng. Luồng thua / chơi tiếp / bỏ cuộc / thử lại chạy không lỗi.

### Cảm giác chơi: tranh không nảy khi đầy vùng; so cú chạy lên ô chờ

**Đo** (video 2)

- Mép khung tranh ở giây 56,5–57,8, 60,3–61,3 và 153,3–154,4 (có vùng đầy và mở): đứng yên, lệch 0 px.
- Bản mình nảy cả tranh 3,5% mỗi lần đầy vùng (`BoardPunch`, tự thêm, bản gốc không có).
- Chạm khối → đáp xuống ô chờ: bản gốc khoảng 0,4 s (giây 155,56 → 155,95), bản mình khoảng 0,48 s. Dáng chạy và
  hàng sau dồn lên (~0,2 s) khớp, nên giữ nguyên.

**Sửa**

- Xóa `BoardPunch.cs` và dòng gắn nó trong `SandJamSceneController`.

**Kiểm tra**

- Run all checks qua 8/8.
- Tự chơi level 12, 267: thắng, màn phóng to tranh lúc thắng vẫn chạy.

### Cảm giác chơi: khối hết cát tự chuyển trắng; bỏ khói lúc đáp và lúc rời ô

**Đo** (video 2, phóng to)

- Giây 155,9–156,1: khối đáp xuống ô chờ, không có khói.
- Giây 158,3–158,7: khối hết cát chuyển trắng cả khối khoảng 0,17 s (158,38 → 158,55) và nhảy lên trong lúc trắng.
  Sau đó khối về màu cũ rồi chạy đi, không có khói màu.
- Bản mình trước đây: khói trắng lúc đáp; lúc rời ô có đốm sáng tròn đè lên khối và 22 hạt khói màu.

**Sửa**

- `SceneActorView.EmptyWhite`: 1 → 0 trong `EmptyWhiteSeconds` 0,18 s kể từ lúc `Leave()`.
- `CharacterSandVessel.Apply` trộn màu khối với trắng theo `EmptyWhite`; `CharacterDepthStyle` tô lại khi giá trị
  này đổi. Dùng lại đường tô màu sẵn có.
- `SandFx`: bỏ khói lúc đáp, đốm sáng và khói lúc rời ô (`EmptyFlashSize`), cùng các trường `ActorState` không
  còn dùng.

**Kiểm tra**

- Quay level 2 từng bước 0,04 s: khối xanh chuyển trắng rồi về xanh và chạy đi.
- Tự chơi level 2, 68, 150: thắng cả 3. Run all checks qua 8/8.

### Cảm giác chơi: bỏ bụi cát; khói khi khối "?" lộ màu rõ như bản gốc; sửa hiệu ứng hạt biến mất

**Đo**

- Video 2, giây 58,2–58,4, phóng to điểm cát rơi chạm mặt cát: không có bụi, cát chỉ đắp từng lớp.
- Video 1, giây 209,4–210,3 (level 259): khối "?" lên đầu làn thì một đám khói trắng to khoảng 1,3 lần khối bọc
  lấy khối (vẫn thấy khối bên trong), mỏng dần trong khoảng 0,8 s.
- Bản mình: khói gần như không thấy. Khói nổ ở chỗ khối bắt đầu trượt, nằm sau khối, và có lúc biến mất hẳn.

**Nguyên nhân khói biến mất:** hệ hạt chỉ phát bằng `Emit()` nên không có khung bao riêng. Culling mặc định của
Unity tưởng nó nằm ngoài màn hình và tạm dừng, nên đôi khi cả đợt hạt không hiện (ảnh hưởng mọi hiệu ứng hạt).

**Sửa**

- `SandFx.MakeSystem`: `cullingMode = AlwaysSimulate`.
- `SandFx.RevealSmoke`:
  - 18 cụm khói, cỡ 1,3, alpha 0,55, sống 1 s;
  - đặt ở `SceneActorView.Destination` (chỗ khối dừng ở đầu làn), nâng 0,4 và đưa ra trước mặt khối 0,8.
- Bỏ `PourDust` và `SceneRegionView.TryGetPourImpact` (chỉ dùng cho bụi).
- Công cụ quay mới (chỉ dùng trên máy): quay theo thời gian thực ở nửa độ phân giải. Cách dừng game rồi bước
  `Advance` không quay đúng được hạt và bong bóng, vì chúng chạy theo thời gian thực.

**Kiểm tra**

- Quay level 259 hai lần: khói hiện cả hai lần, bọc khối, tan sau ~0,6 s.
- Tự chơi level 2, 259, 267: thắng cả 3. Run all checks qua 8/8.

### Cảm giác chơi: khói mở khóa ô chờ dày như bản gốc

**Đo** (video 1, giây 1004,8–1005,1, ô 4 đếm về 0)

- Đám khói trắng dày phủ kín cả ô, rồi loang thành vòng ra ngoài và tan sau khoảng 0,3 s.
- Bản mình chỉ có vòng khói mỏng (22 cụm nhỏ), không có lớp khói phủ kín ô lúc đầu.

**Sửa:** `SandFx.SlotUnlocks` thêm `SlotCloudPuffs` = 12 cụm khói to giữa ô (sống 0,3–0,4 s), giữ nguyên vòng
khói cũ.

**Kiểm tra**

- Quay level 264 theo thời gian thực, tua nhanh tới lúc ô 4 còn 1: khói phủ kín ô, loang thành vòng, tan sau
  khoảng 0,6 s.
- Tự chơi level 264, 68: thắng. Run all checks qua 8/8.

### Cảm giác chơi: âm thanh lúc thắng đúng thời điểm như bản gốc

**Đo** (video 1, 3 lần thắng ở giây 88, 213, 372; so dạng sóng)

- Lúc tranh hoàn thành: phát `level completed panel` (khớp 0,47, đều cả 3 lần).
- "Superb!": không có tiếng.
- Bảng Level Complete hiện ra: phát `Victory` (khớp 0,98–0,99), muộn hơn lúc thắng khoảng 4,3 s.
- `claim_coin`: không lần nào (khớp 0,13).
- Bản mình trước đây phát `Victory` và tiếng xu ngay lúc thắng, không phát `LevelCompletePanel`.
- Quay lại bong bóng mở vùng theo thời gian thực (level 267): bong bóng sống khoảng 0,35–0,45 s, sát bản gốc.

**Sửa**

- `AudioManager`: lúc thắng phát `LevelCompletePanel`; tiếng xu chỉ còn khi nhân đôi thưởng (x2).
- `VideoScreen.Show(Page.Result)` phát `Victory` khi bảng Level Complete hiện ra.

**Kiểm tra**

- Tự chơi level 2, 68 tới bảng Level Complete: không lỗi; kích thước bảng không đổi.
- Run all checks qua 8/8.

### Cảm giác chơi: so cảnh thắng, lúc vào màn, lúc rời ô theo thời gian thực; khối pháo giấy mềm như bản gốc

**Đo (không cần sửa)**

- Cảnh thắng (video 1, giây 88,6–93,2; bản mình quay level 2 theo thời gian thực):

  | | Bản gốc | Bản mình |
  |---|---|---|
  | Tranh phóng to | ~0,6 s | ~0,7 s |
  | Từ lúc thắng đến "Superb!" | 2,0 s | 1,8 s |
  | Chữ "Superb!" | 1,9 s | 2,1 s |
  | Từ lúc thắng đến bảng Level Complete | 4,6 s | 4,4 s |

- Bấm PLAY (giây 151,55): bản gốc đứng ở Home ~0,6 s, chớp màn SAND JAM khi tải rồi mới vào màn. Bản mình vào
  ngay, giữ nguyên vì nhanh hơn.
- Khối rời ô (video 2, giây 158,5–159,0): nhảy lên rồi chạy ngang ra mép, ~0,45 s, khớp.
- Nút gốc không co khi bấm. "Next" chuyển sang xám trong lúc xu bay về. Bản mình giữ nhún 12% cho cảm giác bấm.
- Rung (haptics): mã gốc giải nén chỉ còn vỏ hàm, không đo được, giữ nguyên.

**Sửa:** texture khối pháo giấy (`SandFx` voxel) theo video 2 (giây 153,25): bỏ viền tối 0,62, đổ bóng nhẹ từ
trên trái (sáng) xuống dưới phải (0,78), bo góc, lọc mượt.

**Kiểm tra:** quay level 267: khối pháo giấy mềm, không viền tối. Tự chơi level 267 thắng. Run all checks qua 8/8.

### Cảm giác chơi: hạt cát tối đúng màu riêng của từng màu (bảng màu gốc)

**Đo**

- So cả quá trình tô level 267 với video 2 (giây 50–63,5, cùng bức tranh): viền vàng chữ U dâng đều hai nhánh ở
  cả hai bản (thứ tự lấp `SeepOrder` theo hàng), tốc độ và thứ tự mở vùng khớp.
- Hạt tối trong video: vàng (208,176,8), tím (72,0,160), xám (120,120,120). Tỉ lệ so với màu nền khác nhau theo
  từng màu (×0,84 / ×0,7 / ×0,94).
- Bảng màu gốc `OriginalConfig` có sẵn trường `dark` cho từng màu: vàng (214,172,0), tím (78,0,166), xám
  (125,125,125), khớp số đo. Bản mình dùng chung ×0,85 nên hạt tối của tím quá nhạt, của xám quá đậm.

**Sửa** (`SceneRegionView`)

- `DarkGrain` lấy màu `dark` của bảng màu gốc; màu nào không có thì dùng ×0,85.
- `SandShade(base, dark, x, y)` chọn giữa 2 màu, dùng cho cát đã đổ và hạt đang rơi.

**Kiểm tra**

- Quay level 267: hạt tối của tím ra (72,0,160), đúng bản gốc.
- Tự chơi level 2, 68: thắng. Run all checks qua 8/8.

### Cảm giác chơi: khối chạy đứng thẳng, hai chân ngắn cạnh nhau (người dùng báo chân xoay sai)

**Đo** (video 2, giây 155,6–156,15, phóng to; bản mình level 267 quay theo thời gian thực)

- Bản gốc: khối đứng thẳng, chỉ xoay nhẹ theo hướng chạy (gần như chỉ thấy mặt trước). Hai chân ngắn, mập, đứng
  cạnh nhau ngay dưới đáy khối, bước luân phiên, vẫn thấy chân khoảng 0,15 s sau khi đáp.
- Bản mình trước đây:
  - khối lật nghiêng sang bên 20–25°;
  - chân thành một que dài mảnh chĩa chéo ra sau;
  - chân biến mất ngay lúc đáp.

**Nguyên nhân:** khi chạy, khối và chân xoay quanh trục đứng của chính khối. Khối đang ngả về phía camera ~54°,
nên trên màn hình động tác xoay thành lật nghiêng. Chân xoay 30° theo hướng chạy nên hai chân che nhau thành một.
Lúc đổ cát đã sửa lỗi này trước đây, nhưng lúc chạy thì chưa.

**Sửa** (`SceneActorView`)

- `ApplyYaw`: xoay khi chạy (`yaw`) và khi đổ (`aimYaw`) cộng chung, cùng xoay quanh trục đứng thật đi qua tâm
  khối. Chân cũng xoay quanh trục đứng thật.
- `MaxYaw` 25 → 10°, `RunLegYaw` 30 → 0° (hai chân cạnh nhau), `RunLegStretch` 1,6 → 1,25.
- `LegLinger` 0,15 s: chân còn hiện một chút sau khi đáp.

**Kiểm tra**

- Quay level 267 cạnh video: khối thẳng, hai chân dưới đáy, bước luân phiên, còn chân lúc vừa đáp.
- Quay level 2: đổ cát và rời ô vẫn đúng.
- Tự chơi level 2, 68, 267: thắng cả 3. Run all checks qua 8/8.

### Cảm giác chơi: khối và chân quay về hướng chạy lên ô chờ (yêu cầu người dùng)

**Đo** (video 2, giây 48,6–49,35: khối cam chạy từ làn phải sang ô trái nhất)

- Khối quay hẳn về hướng chạy, khoảng 60°. Số nằm trên lưng nhân vật nên khi chạy sang trái, số lệch về bên
  phải, lộ mặt bên trái và mặt trên.
- Hai chân sải bước về hướng chạy. Khối vẫn đứng thẳng, gần tới ô thì quay lại mặt trước.
- Lượt trước mình chỉ so cú chạy gần như thẳng lên (giây 155,6) nên đặt góc quay 10°, quá nhỏ cho cú chạy chéo.

**Thử** (quay level 267, làn phải → ô trái nhất, phóng to)

- Xoay quanh trục đứng riêng của khung xương hoặc của `Visual`: khối chỉ xoay phẳng trên màn hình, vì các trục
  này gần trùng hướng nhìn camera.
- Xoay quanh trục đứng thật: lộ mặt bên như bản gốc, nhưng khối ngả nghiêng. Bù bằng cách nghiêng ngược quanh
  hướng nhìn camera 0,5° cho mỗi độ quay thì khối đứng thẳng.

**Sửa** (`SceneActorView`)

- Góc quay khi chạy = góc của hướng chạy so với thẳng lên (`Atan2(dx, dy)`), tối đa `MaxYaw` 65°. Chạy thẳng
  lên thì gần như không quay, chạy ngang thì quay mạnh.
- `ApplyYaw`: quay (chạy + đổ) quanh trục đứng thật qua tâm khối, rồi nghiêng `roll + yaw * RunUpright`
  (`RunUpright` = -0,5) quanh hướng nhìn camera.

**Kiểm tra**

- Chạy chéo dài: khối quay về hướng chạy, đứng thẳng, chân sải theo hướng, gần tới ô thì quay lại.
- Chạy thẳng (level 267, làn trái): quay rất nhẹ, hai chân dưới đáy như lượt trước.
- Tự chơi level 2, 68, 267: thắng cả 3. Run all checks qua 8/8.

### Cảm giác chơi: chân mọc từ giữa đáy khối khi chạy chéo, dài hơn (yêu cầu người dùng)

**Đo** (video 2, cú chạy làn phải → ô trái nhất, từng khung ~1/27 s)

- Bản gốc: hai chân dày mọc ra từ giữa đáy khối, dài ~0,6–0,7 chiều cao khối, đầu gối gập, sải bước rộng ~nửa
  bề ngang khối, chu kỳ ~0,2 s. Khối gần như không nhún.
- Bản mình: chân bị đẩy ra sau hoặc sang bên khối, dài ~0,4 chiều cao khối.

**Nguyên nhân:** động tác hạ chân (`legDrop`) xoay quanh trục ngang của khung xương. Từ khi khối quay về hướng
chạy (tới 65°), trục này quay theo, nên chân bị hất ra sau thay vì xuống dưới.

**Sửa** (`SceneActorView`)

- Hạ chân quanh trục ngang của màn hình (`dropAxis`, cùng chiều với trục ngang của khung xương lúc đứng yên).
- `RunLegStretch` 1,25 → 1,45.
- Đo góc và kích thước khối từng khung khi chạy: thay đổi mượt, không rung, nên giữ nguyên.

**Kiểm tra**

- Chạy chéo dài: chân dưới đáy khối, sải theo hướng chạy.
- Chạy thẳng: hai chân dưới đáy như trước.
- Tự chơi level 2, 267: thắng. Run all checks qua 8/8.

### Cảm giác chơi: khối chạy không nhún, nhịp bước như bản gốc; sửa lò xo co giãn bị giật khi khung hình dài

**Đo** (video 2, cú chạy dài giây 48,6–49,3, từng khung 54 fps)

- Đỉnh khối gốc chỉ lệch chậm ±5 px trên khối cao 137 px, không nhún theo bước. Bản mình nhún `StepBob` 0,07
  (~10% chiều cao khối).
- Chân gốc dạng/khép ~4–5 lần mỗi giây (~0,21–0,26 s một bước). Bản mình: clip đi bộ 0,875 s (2 bước) ở tốc độ
  1,6 → ~0,27 s một bước.
- Khi quay, khối của mình đổi dáng qua lại giữa các khung (lúc hẹp cao, lúc bè thấp). Ghi lại từng khung: góc
  quay giữ nguyên, nhưng khung bao đổi 0,75 ↔ 0,82. Đó là lò xo co giãn (`squash`) tính bằng Euler hiện, mất ổn
  định khi một khung dài (~0,1 s) và đổi dấu mỗi khung. Ở 60 fps không thấy, nhưng máy yếu hoặc lúc game khựng sẽ
  giật như vậy.

**Sửa** (`SceneActorView`)

- `StepBob` 0,07 → 0,012.
- `RunAnimSpeed` 1,9 (thay số 1,6 viết cứng). `StepsPerSecond` tính từ `WalkClipSeconds` và `RunAnimSpeed` nên
  nhịp nhún khớp nhịp bước.
- `ApplyJuice`: lò xo tính theo bước nhỏ tối đa `SpringStep` = 1/120 s.

**Kiểm tra**

- Quay lại cú chạy dài: khung bao đứng yên 0,75–0,77, khối thẳng, chân dưới đáy sải theo hướng chạy.
- Tự chơi level 2, 68, 267: thắng cả 3. Run all checks qua 8/8.

### Cảm giác chơi: số trên khối quay theo khối khi chạy

**Đo** (video 2, giây 48,6–49,3)

- Bản gốc: số in trên mặt khối nên khi khối quay về hướng chạy, số dạt về phía mép xa (chạy sang trái thì số
  nằm ~3/4 bề ngang về bên phải) và nghiêng theo mặt khối.
- Bản mình: số `Ammo` là đối tượng riêng nằm ngoài khung xương, nên đứng yên giữa khối, nhìn thẳng camera. Khối
  trông như không quay. Tăng góc quay 1,25 lần (tối đa 80°) cũng gần như không khác, vì người xem nhìn vào số.
- Gắn số theo đúng phép quay của khung xương: số gần như nằm cạnh và ra ngoài khối, vì mặt có số của khối được
  dựng ngả về phía camera.

**Sửa** (`SceneActorView.ApplyYaw`)

- Khi khối quay (chạy hoặc đổ), số trượt ngang `-sin(góc) × LabelSlide` (0,14 × cỡ khối) và quay
  `LabelTurnShare` (0,45) góc quanh trục đứng. Khi khối về thẳng thì số trả về chỗ cũ (`labelRest`, `labelMoved`).
- Giữ `RunTurnGain` 1,25 và `MaxYaw` 80° (quay mạnh hơn một chút như bản gốc).

**Kiểm tra**

- Chạy chéo dài: số nằm nửa phải khối, nghiêng theo.
- Đổ cát (level 2): số lệch nhẹ theo góc đổ, về giữa khi khối rời ô.
- Tự chơi level 2, 68, 259, 267: thắng cả 4. Run all checks qua 8/8.

### Cảm giác chơi: vùng đã đầy giữ đúng màu (bỏ sáng 12%); job tô màu biên dịch Burst đồng bộ

**Đo**

- Video 2, vùng xám giây 54–62: đầy lúc 55,5 s, sau đó giữ 134 tới cuối, không sáng lên.
- Bản mình: vùng nào đầy thì sáng thêm 12% về phía trắng mãi mãi (`completionGlow`). Xám 132 hiện thành 144,
  nên tranh đã tô xong nhạt hơn bản gốc.

**Sửa**

- `SceneRegionView`: bỏ `completionGlow` và trường `Glow` của `PaintRegionJob`; vùng đầy chỉ ẩn số đếm.
- `PaintRegionJob` thêm `[BurstCompile(CompileSynchronously=true)]` như `StepJob`. Sau khi bỏ một trường, editor
  vẫn chạy bản Burst cũ (biên dịch bất đồng bộ) và đọc các trường mới ở vị trí cũ; bài kiểm tra bắt được lỗi này.
- `BurstSandChecks`: kiểm tra ánh cát mới theo tuổi hạt (0, 45, 90, 180 bước) thay cho kiểm tra `Glow`.

**Kiểm tra**

- Level 267 tô xong: xám (128, hạt tối 120), cam (248,128,0), tím (104,0,232 / 72,0,160) khớp video.
- Tự chơi level 2, 68, 267: thắng cả 3. Run all checks qua 8/8.

### Cảm giác chơi: khối hết cát quay ngang, sải chân, chạy ra phía sau hàng ô

**Đo** (video 2, giây 158,35–158,9, khối tím rời ô 2)

- Chuyển trắng, nhảy lên, rồi quay hẳn 90° sang ngang (thấy mặt bên). Chân sải dài nhìn nghiêng ngay dưới khối.
- Chạy ngang ra mép trái phía sau khối vàng ở ô 1 (bị khối vàng che).
- Bản mình: khối không quay (`ExitYaw` = 0). Chân xoay riêng 85° và không hạ xuống, nên gần như không thấy.
  Khối chạy ngang đè lên khối ở ô bên cạnh.

**Sửa** (`SceneActorView`)

- `ExitYaw` 0 → 90°, cùng chiều với cách quay khi chạy lên ô (`Sign(dx)`). Thân và chân cùng quay, chân hạ dưới
  đáy như lúc chạy lên. Bỏ `ExitLegYaw`.
- `ApplyDepth`: khi đang chạy ra, khối lùi `ExitBehind` (0,5) về phía sau nên đi sau các khối còn trên ô.

**Kiểm tra**

- Quay level 267 (khối rời ô 3): khối quay ngang, có chân, đi sau khối cam ở ô 1.
- Tự chơi level 2, 68, 267: thắng cả 3. Luồng thua / chơi tiếp / thử lại không lỗi. Run all checks qua 8/8.

### Cảm giác chơi: hàng khối dồn lên chậm–nhanh–chậm như bản gốc

**Đo** (video 2, giây 48,3–49,0, làn phải; đỉnh khối xám từng khung 54 fps)

- Khối đầu rời đi lúc ~0,31 s. Khối xám đi 296 → 286 → 282 → 264 → 246 → 232 → 220 → 212 → 206 → 202 → 200 px
  tới ~0,70 s: bắt đầu chậm, nhanh ở giữa, chậm ở cuối, ~0,37 s, không nảy lên.
- Bản mình: chậm dần mạnh (cubic ease-out) trong 0,32 s, nảy lên 0,05. Đi gần hết quãng chỉ trong ~0,15 s rồi
  đứng chờ, nên nhìn giật.

**Sửa** (`SceneActorView`): trượt hàng dùng `smoothstep`, `SlideSeconds` 0,32 → 0,38, bỏ nảy lên. Giữ cú nhún
nhẹ khi dừng.

**Kiểm tra:** quay level 68 làn trái: khối vàng dồn lên đều trong ~0,38 s. Tự chơi level 2, 68, 267 thắng cả 3.
Run all checks qua 8/8.

### Cảm giác chơi: đo độ nhạy chạm và cú đáp; thêm rung khi khối rời đi và khi chạm khối không đi được

**Đo (không cần sửa)**

- Chạm: nhận ngay lúc chạm xuống (`GetMouseButtonDown`), khối chạy trong cùng khung hình.
- Vùng chạm (level 68): collider rộng hơn khối ~10% mỗi bên. Khe giữa làn/hàng ~10–20 px. Collider gốc (prefab
  `Character`: 0,75 × 1,15) cao hơn khối. Không kéo cao collider của mình, vì collider khối hàng sau (gần camera
  hơn) sẽ đè lên nửa dưới khối hàng đầu.
- Cú đáp: bản gốc (video 2, giây 155,9–156,2) hạ êm, không bẹp. Bản mình chỉ thấp đi ~4% (cao 189 → 182 px),
  đủ gần.
- Mã gốc giải nén chỉ còn tên hàm (`SelectionController.CharacterShake`, `_inputTimerCounter`), không đọc được
  thân hàm.

**Sửa** (rung, chỉ có tác dụng trên điện thoại)

- `HapticsManager`: khối hết cát rời đi → `Pulse(8)`. `Refused()` → `Pulse(30)`.
- `SceneActorView.Shake` gọi `HapticsManager.Refused()`: chạm vào khối không đi được thì vừa lắc vừa rung.

**Kiểm tra:** gọi `Shake` trong Play mode không lỗi, khối vẫn lắc ±0,03. Tự chơi level 2, 68 thắng. Run all
checks qua 8/8.

### Cảm giác chơi: nhịp bắn viên cát đúng số đo

**Đo** (video 2, giây 57,6–58,6, khối vàng đổ; dò vệt sáng trong dải giữa khối và tranh)

- 7 viên ở 57,64 / 57,76 / 57,89 / 58,01 / 58,12 / 58,23 / 58,33 s: cách nhau 0,11–0,12 s.
- Comment cũ ghi "~0,07 s", sai. Bản mình bắn 0,1 s.

**Sửa:** `ProjectileManager.FireInterval` 0,1 → 0,115 s, comment ghi lại số đo.

**Kiểm tra:** tự chơi level 2, 267 thắng. Run all checks qua 8/8.

### Popup "NO MORE LIVES!" đúng cỡ, nút đóng ở góc, thanh xu tối như bản gốc

**Đo** (video 1, giây 1330,6–1334,2: hết tim, về Home)

- Popup bật lên trong ~0,3 s, lố rất ít (~3%) rồi đứng yên. Khung rộng 779 px, mép đỏ từ y 258 đến 1161.
- Nút đóng ~96 px, đè lên góc trên phải khung. Thanh xu tối theo nền (độ sáng 175 → 63).
- ~1,5 s sau, bảng "Special Refill Pack" bật ra bên dưới. Đây là gói mua bằng tiền thật (`bundle_refillpack`
  trong `RC_Shop.asset`), không làm (clone không có thanh toán).
- Bản mình: rộng 851 px, thấp hơn ~30 px, nút đóng nhỏ (~66 px) nằm lệch lên trên, thanh xu vẫn sáng.

**Sửa** (`LivesPopup`)

- `FitCard(noLives, 0,915, 53)` → rộng 779 px, mép đỏ từ y 240 đến 1171.
- Nút đóng 145 × 168 đơn vị canvas tại (511, 556), vùng chạm 0,7.
- `DimCoinBar(noLives, 0,38)` → thanh xu 68 (gốc 63).

**Kiểm tra**

- Mở popup: collider nút đóng có (0,64), đóng được.
- Luồng thua / bỏ cuộc / về Home không lỗi. Run all checks qua 8/8.

### Home: rương Sand Quest và chữ JOIN đúng cỡ, đúng chỗ

**Đo** (video 1, giây 1334,6, Home level 267)

- Rương + bảng: rộng 165 px, x 680–845, y 670–847. Bản mình nhỏ hơn 5%, lệch trái 11 px, thấp 10 px.
- Chữ JOIN: 72 × 22 px, viền vàng sẫm mảnh (139,106,4). Bản mình 85 × 28 px, viền đen.

**Sửa**

- `VideoScreen`: dùng lại `NavPlace` / `Place` cho rương, bảng và chữ JOIN (`ChestGrow` 1,05,
  `ChestShiftPx`, `JoinTextScale` 0,82).
- `CampaignUi`: viền chữ JOIN đổi sang `VideoScreen.JoinInk` (đổi một lần khi viền đã có).

**Kiểm tra**

- Level 267: rương x 680–845, y 667–847; chữ 70 × 22 px.
- Level 68: rương, bảng, chữ vẫn ẩn.
- Tự chơi level 68 thắng. Run all checks qua 8/8.

### Độ mượt: bỏ cấp phát bộ nhớ mỗi khung của số đếm trên vùng; cảnh vào màn đã khớp

**Đo**

- Vào màn (video 1, giây 152,2): cắt cứng từ SAND JAM sang màn chơi, sau đó màn hình đứng yên hoàn toàn tới lần
  chạm đầu. Bản mình cũng đứng yên (khác biệt giữa các khung = 0), không sửa.
- Cấp phát bộ nhớ mỗi khung (ProfilerRecorder "GC Allocated In Frame", level 267): Home 8,3 KB (nền editor),
  màn chơi đứng yên 32 KB, đang chơi 26 KB.
- `Advance` và `SandFx` cấp phát 0 byte. Đọc cây Profiler một khung: 23,6 KB nằm ở
  `SceneRegionView.LateUpdate → GetComponentNullErrorMessage`. `GetComponent<OutlinedText>()` gọi mỗi khung
  cho các số đếm chưa có viền; trong editor mỗi lần không thấy, Unity tạo một chuỗi báo lỗi.

**Sửa:** `SceneRegionView.LateUpdate` dùng `TryGetComponent` (không cấp phát khi không có).

**Kiểm tra**

- Màn chơi đứng yên 8,3 KB (bằng nền editor); một khung đang chơi, phần của game chỉ ~0,5 KB.
- Tự chơi level 2, 68, 267: thắng cả 3. Run all checks qua 8/8.

### Bảng Level Complete: xu bay khi bấm Next, như bản gốc

**Đo** (video 1, giây 93,3–94,5 và 217,9–220,2)

- Bảng hiện ra rồi đứng yên, không có xu bay (người chơi chờ 2,3 s ở giây 218–220 cũng không có).
- Bấm Next: nút xám đi. Từ 93,78 đến 94,35 s xu bay thành dòng từ nút Next lên thanh xu, mỗi đồng ~0,2 s. Sau
  đó bảng tắt, CONGRATS bật ra. Từ lúc bấm tới lúc chuyển ~1 s.
- Bản mình: xu tự bay từ hộp thưởng ngay khi bảng hiện; bấm Next là chuyển ngay.

**Sửa**

- `ResultFx.Collect(done)`: xu bay từ nút Next (`CoinStart` 0,3, `Spread` 0,05, `Flight` 0,22, đường bay thấp
  hơn), nút mờ trong lúc bay, bay xong gọi `done`. Không bấm thì không bay. Bấm lại trong lúc bay không có tác dụng.
- `VideoScreen` "next-level": gọi `Collect(NextAfterResult)`; nếu không có hình xu thì chuyển ngay như cũ.

**Kiểm tra**

- Level 2: chờ 0,8 s không có xu; bấm Next → xu bay 1,2–1,7 s, số xu đếm lên, ~1 s sau sang CONGRATS.
- Tự chơi level 12, 68 thắng. Run all checks qua 8/8.

### CONGRATS bật ra nhanh như bản gốc

**Đo** (video 1, giây 774,6–774,9)

- Bảng Level Complete tắt ngay (màn tối ~0,1 s). Dải CONGRATS lớn từ 180 lên 807 px trong ~0,12 s, chậm dần ở
  cuối.
- Bản mình dùng chung nhịp 0,4 s đều của các popup khác.

**Sửa**

- `CongratsPopup.FastPopIn`.
- `PopupScreens.Update`: khi CONGRATS mở thì dùng `FastPopTime` 0,13 s với ease-out, giống cách `SlowPopIn` của
  thẻ Hard/Bonus.

**Kiểm tra**

- Level 2, bấm Next: CONGRATS 79 → 734 → 805 px trong 1,90–2,00 s.
- Luồng thua / thử lại không lỗi. Run all checks qua 8/8.

### Về Home sau CONGRATS: nền tối trước, lời mời Sand Quest bật sau; sửa nút Home sáng lên khi popup bật

**Đo** (video 2, giây 144,8–146,3)

- Chạm CONGRATS → màn SAND JAM ~0,1 s → Home đã tối sẵn → ~0,75 s sau thẻ Sand Quest mới bắt đầu lớn
  (145,75 → 146,2 s).
- Bản mình: thẻ bật ngay khi về Home. Lúc thẻ còn nhỏ (0,1 s đầu của mọi popup), tấm biển level, nút PLAY và
  thanh điều hướng sáng lên qua lớp tối.

**Nguyên nhân sáng lên:** popup co giãn đều cả 3 chiều khi bật, nên lớp nền tối (con của popup) bị kéo lùi về
phía sau các nút của Home lúc thẻ nhỏ.

**Sửa** (`PopupScreens`)

- `SetPopScale` gộp 3 chỗ đặt cỡ popup (bật, đóng, mỗi khung): chỉ co giãn ngang và dọc, giữ chiều sâu. Lớp
  tối bù lại ngang và dọc để vẫn phủ kín màn. Thay cho `FitBackdrop`.
- `HoldCard(giây)`: ẩn mọi thứ trừ nền tối và thanh xu, dời thời điểm bật. `Show` / `Close` / `Update` trả lại.
- `SandQuestPopup`: lời mời trên Home giữ `OfferDelay` 0,75 s.

**Kiểm tra**

- Level 90: về Home thì tối ngay, Home tối đều suốt; 0,75 s sau thẻ bật sáng rõ, đủ cỡ sau ~0,45 s.
- Luồng thua / chơi tiếp (ô 6) / bỏ cuộc / thử lại / về Home không lỗi. Tự chơi level 2 thắng.
- Run all checks qua 8/8.

### Lớp tối của popup mờ dần vào như bản gốc

**Đo** (video 1)

- Hết chỗ (giây 1327,9–1328,15): dải "OUT OF SPACE!" biến mất, màn mờ tối dần, độ sáng trung bình
  136 → 112 → 91 → 69 → 49 trong ~0,25 s. Sau đó thẻ mới bật.
- Hết tim (giây 1331,24–1331,45): Home tối dần 168 → 77.
- Bản mình: lớp tối hiện đủ ngay trong một khung.

**Sửa** (`PopupScreens`)

- `StartDimFade` (gọi trong `Show`), `TickDimFade` (mỗi khung), `EndDimFade` (`Close`): lớp tối trong suốt mờ dần
  từ 0 tới độ đậm riêng của nó trong `DimFade` 0,25 s.
- Lớp nền đặc (CONGRATS) và các trang (Shop, Gallery) vẫn cắt cứng như bản gốc.

**Kiểm tra**

- Popup hết tim: độ sáng 164 → 123 → 92 → 69 trong 0,29 s.
- Luồng thua / chơi tiếp / bỏ cuộc / thử lại / về Home không lỗi. Run all checks qua 8/8.
- Script kiểm tra cục bộ `flow.sh` chờ thêm khi đã thua nhưng popup chưa mở (dải OUT OF SPACE! 2,3 s). Trước đó
  thỉnh thoảng nó bấm "chơi tiếp" quá sớm và báo `False`.

### Đóng popup: không còn ô tối sót lại giữa màn

**Đo**

- Video 2 (giây 20,1–20,55): thẻ thu nhỏ ~0,35 s, lớp tối giữ nguyên rồi biến mất một lần (độ sáng 78 → 177
  trong một khung).
- Bản mình: giống vậy, nhưng ~0,05 s cuối lớp tối co lại thành một ô chữ nhật tối nhỏ giữa màn (thấy rõ ở khung
  1,217 s khi đóng popup hết tim). Hệ số bù kích thước bị chặn ở 0,05, trong khi thẻ thu tới 0,001.

**Sửa:** `PopupScreens.SetPopScale` chặn ở 0,001, bằng ngưỡng của thẻ.

**Kiểm tra:** đóng popup hết tim hai lần: không còn ô tối. Luồng thua / chơi tiếp không lỗi. Run all checks qua
8/8.

### Shop / Gallery: trang cuộn có quán tính như bản gốc

**Đo** (video 2, Shop giây 4,0–10,7; dịch chuyển nội dung giữa hai khung liên tiếp)

- Lướt rồi thả: trang trượt tiếp, vận tốc giảm ~e^-2t (ví dụ 8,4 → 8,9 s: 42,6 → 15,7 px/khung). Đây là mức hãm
  mặc định 0,135/s của ScrollRect. Chạm mép thì dừng ngay, không nảy (8,92 s: 15 → 0 px/khung).
- Bản mình (`DragScroller`): thả tay là trang dừng ngay, không có quán tính.

**Sửa** (`DragScroller`, dùng chung cho Shop và Gallery)

- Trong lúc kéo: đo tốc độ ngón tay, làm mượt như ScrollRect (`Lerp(v, mới, dt × 10)`).
- Sau khi thả: trượt tiếp, `velocity *= 0,135^dt`, dừng khi < `StopSpeed` hoặc chạm mép. Chạm lại là dừng ngay.
  Chạm không kéo thì không trượt.
- Bước thời gian tối đa 0,05 s, để một khung khựng không làm trang vọt đi. Phần trượt chạy cả khi editor không
  có con trỏ.

**Kiểm tra**

- Shop, gán vận tốc thả tay -6: vị trí 23,84 → 20,76, vận tốc 5,43 → 3,41 sau 0,23 s (≈ e^-2t), dừng sau ~2,4 s.
- Gán từ vị trí 1: dừng đúng ở 0. Mở Gallery không lỗi. Run all checks qua 8/8.

### Gallery: băng thẻ trượt khi lật trang; trang dựng lại không còn chớp hình cũ

**Đo** (video 2, giây 26,3–26,5 và 36,9–37,2; so khớp mẫu từng khung)

- Lật trang: băng thẻ trượt ~455 px (một bước thẻ) trong ~0,25 s, nhanh nhất ngay lúc đầu (58 px/khung) rồi giảm
  đều tới 0 (chậm dần bậc hai). Tên bộ sưu tập đổi ngay.
- Bản mình: dựng trang mới tại chỗ, không trượt.

**Sửa**

- `GalleryPopup`: ghi lại các mảnh của băng thẻ khi dựng (`carousel`, `carouselX`). Khi lật trang, cả băng lệch
  một bước `SideX` về phía cũ rồi về 0 trong `SlideSeconds` 0,25 s với `(1 - t)²`. Thẻ cũ chính là thẻ bên cạnh
  của trang mới, nên trượt ra như bản gốc.
- `PopupModule.DestroyAll`: ẩn ngay trước khi `Destroy`. Trước đây mảnh cũ còn vẽ thêm một khung (thấy hai tên
  bộ sưu tập chồng nhau). Áp dụng cho mọi trang dựng lại (Gallery, Shop, Tasks…).

**Kiểm tra**

- Level 268, lật trái: tên đổi ngay, thẻ Musician trượt vào từ trái, Accessories trượt sang phải, dừng sau
  ~0,25 s; không có khung chồng hình.
- Luồng thua không lỗi. Run all checks qua 8/8.

### Cài đặt: công tắc trượt khi bật/tắt

**Đo** (video 2, giây 14,95–15,9: tắt rồi bật Vibration)

- Bấm: nút đổi ngay hình (xanh "ON" ↔ xám "OFF"), rồi trượt 111 px trong ~0,33 s, nhanh lúc đầu, chậm dần
  (623 → 605 → 589 → … → 512 px).
- Bản mình: nút nhảy ngay sang chỗ mới.

**Sửa** (`SettingsPopup`)

- `SetToggle(…, animate)`: hình và chữ đổi ngay. Vị trí trượt trong `KnobSlideSeconds` 0,33 s với
  `1 - (1 - t)²` (`KnobSlide`, `SlideKnob` trong `Tick`). Khi mở popup thì đặt thẳng, không trượt.
- Đích trượt tính cả hệ số thu nhỏ 0,95 của popup ở Home (`fitScale`). Công tắc đứng yên thì `Tick` không động
  vào. Trước đây công thức vị trí bỏ qua hệ số này, nên bấm một lần là nút lệch ~5%.

**Kiểm tra**

- Home: tắt → 1,251 → 0,628 trong ~0,33 s, chậm dần; bật lại → 1,251.
- Run all checks qua 8/8. Save demo đặt lại (rung, âm thanh bật).

### Shop / Gallery trượt vào theo hướng tab; Shop mở ở đầu trang

**Đo** (video 2, giây 3,2–6,5)

- Bấm SHOP ở Home: trang Shop trượt vào từ trái, đè lên Home. Đoạn cuối lệch −150 → −95 → −60 → −34 → −15 →
  −3 → 0 px theo từng khung, tốc độ giảm đều; ngoại suy cả màn (888 px) trong ~0,25 s. Thanh trên và thanh điều
  hướng đứng yên.
- Shop mở ở **đầu trang** (3,45 s: SAND SHOP, Remove Ads); người chơi cuộn xuống tới BOOSTERS lúc 6,2 s. Kết luận cũ
  "mở ở BOOSTERS" dựa trên khung 6 s và 11 s, lúc đó người chơi đã cuộn, nên sai.

**Sửa**

- `PopupScreens.Pages`: `StartPageSlide` (gọi trong `Show` cho trang Shop/Gallery), `TickPageSlide`,
  `EndPageSlide`, `LeavePage` (`Close`). Nội dung trang, trừ thanh trên, thanh điều hướng và vùng chạm của
  thanh điều hướng, lệch một bề ngang màn về phía tab (`Sign(tab mới − tab cũ)`; Shop 0, Home 1, Gallery 2),
  rồi về 0 trong `PageSlideSeconds` 0,25 s với `(1 − t)²`.
- `ShopPopup.OpenShop`: mở ở đầu trang (`scroll.Reset()`, nội dung ở 0).
- Bảng tiến độ: dòng Sand Shop cập nhật.

**Kiểm tra**

- Level 268: Home → Shop trượt từ trái (Home còn thấy bên phải lúc 0,26 s); Shop → Gallery trượt từ phải; thanh
  điều hướng đứng yên. Shop mở đúng như video (SAND SHOP, Remove Ads, No Ads Pack, Starter Pack).
- Luồng thua không lỗi. Run all checks qua 8/8.

## Dọn code (yêu cầu người dùng, 23/10): bỏ code thừa, trùng lặp; bỏ thư mục test; giữ tài nguyên game gốc

### Đợt 1: bỏ phần test và các scene demo cũ

- Xoá `Scripts/Testing` (AutoPlayer, LevelStructureViewer), `Editor/Tests` (8 bài kiểm tra, Run all checks),
  `TestResults/`, scene `LevelStructure.unity` và menu mở nó.
- `LevelReplaySolver` (bộ giải level) chuyển sang `Editor/Tools`: công cụ tạo level và "Check custom levels"
  vẫn cần nó để bảo đảm level thắng được.
- `BuildVideoScene`: bỏ các biến thể demo không còn dùng (scene khối nối xích, khối đóng băng, Video201, scene
  VideoUI đơn lẻ, `CreateAndBuild`, `ConfigureChainDemo`, `ConfigureFreezeDemo`, `AddFreezeView`,
  `ConfigureVideo201`, khối "?" dựng sẵn). Công cụ chỉ còn dựng prefab mẫu cho catalog (`CreateLevelPack`),
  đúng đường game đang dùng. Xoá `BuildVideoScene.Video201.cs` và `BuildVideoScene.Freeze.cs`.
- Bỏ sinh file "replay" chỉ dùng cho test (`LevelCatalog.TestReplay`, `LevelReplay`).
- Code chết tìm bằng máy quét (tên chỉ xuất hiện ở chỗ khai báo, không có trong scene/prefab):
  `SandGame.HintLane`/`Useful`, `BurstSandSimulation.UsedBurst` (+ `MarkManaged`), `LevelBootstrap.SelectedIndex`.
- Tài liệu: bỏ phần nói về test và scene cấu trúc level.

**Kiểm tra:** biên dịch không lỗi. Tự chơi (script tạm trên máy, dùng bộ giải) level 2, 68, 267: thắng cả 3.

### Đợt 2: gộp code trùng lặp, bỏ lớp chuyển tiếp và code chết còn lại

Tìm bằng máy quét: dòng dài giống hệt nhau ở nhiều file, hàm cùng tên ở nhiều file, thành viên `static` không
ai gọi (tính cả lời gọi `Lớp.Tên`).

- **Một hàm `Hex`:** `ColorPalette.Hex(string)` / `Hex(int)` thay cho 3 bản riêng (`OriginalLookStyler`,
  `OriginalThemes`, `BuildVideoScene`).
- **Một hàm tắt đổ bóng:** `RendererExtensions.NoShadows()` thay 6 chỗ lặp lại 2 dòng.
- **Ẩn thanh xu:** Gallery, Sand Quest, Shop dùng `PopupScreens.HideCoinBar` thay vì tự lặp danh sách tên.
- **Cập nhật số xu một chỗ:** `Show` gọi `RefreshPopupCoins`; `RefreshPageLives` không cập nhật xu nữa
  (`Show` đã làm).
- **Bỏ lớp chuyển tiếp:** `Campaign.Coins` / `TrySpend` / `WinReward` chỉ gọi sang `EconomyManager`; nơi dùng
  gọi thẳng `EconomyManager.Coins`.
- **Mô phỏng cát:** xoá `SandPourSimulation` (lớp bọc cũ cho bài test); struct `Grain` chuyển vào
  `BurstSandSimulation` (bỏ trường `Dir` không dùng); bỏ chế độ chạy không Burst (`managed`).
- **Code chết:** `OriginalThemes.ForLevel`, `LevelBootstrap.SceneName`.

**Kiểm tra:** biên dịch không lỗi. Tự chơi level 2, 68, 267 thắng cả 3. Booster, luồng thua / chơi tiếp (ô 6) /
bỏ cuộc / thử lại / về Home không lỗi. Ảnh Home và Shop không đổi.

### Đợt 3: sắp xếp lại thư mục và tài liệu

Chỉ di chuyển file (giữ nguyên GUID trong `.meta`, scene/prefab không mất liên kết):

- `Scripts/Runtime/Meta/` (mới): EconomyManager, LivesManager, DailyRewards, TaskBoard, SandQuest,
  GalleryCollections, Collections. `Managers/` chỉ còn dịch vụ chung (Save, Audio, Haptics, GameEvents,
  Campaign, Level…).
- `Controllers/UI/Popups/` (mới): PopupScreens (+Kit, +Pages), PopupModule, Popup, 9 popup/trang, DragScroller.
  `Controllers/UI/` còn VideoScreen, CampaignUi, BoosterController, GameplayFeedbackController.
- `ProjectileManager` → `Views/Fx/` (vẽ viên cát bay). `ReferenceQueueCover` → `Views/Characters/`.
- `Editor/SceneBuilders/ReferenceUI/*` → `Editor/SceneBuilders/` (bỏ tầng thừa).
- Tài liệu: 14 ghi chú theo mốc ngày / scene demo cũ chuyển vào `Documentation/LuuTru/` (không xoá).
  README tiếng Anh trong `Assets/SandJam/` trùng nội dung README gốc nên bỏ; README gốc có sơ đồ thư mục mới
  và mục "Mẫu thiết kế và helper dùng chung".

**Kiểm tra:** biên dịch không lỗi. Tự chơi level 2, 68, 267 thắng cả 3. Booster, luồng thua / chơi tiếp (ô 6) /
bỏ cuộc / thử lại / về Home không lỗi.

### Đợt 4: cấu trúc Assets / Prefabs / ScriptableObjects / Scripts (yêu cầu người dùng)

- `Assets/SandJam/` giờ chỉ có 4 thư mục:
  - **Assets/**: Scenes, Resources (ảnh, âm thanh, level, shader, font), Generated, Levels, Fonts;
  - **Prefabs/**: Gameplay (thêm SandProjectile chuyển từ Generated), Characters, Resources (LevelPack/Prefabs,
    Original/CharacterVisual);
  - **ScriptableObjects/**: Resources (LevelPack/Catalog, OriginalGifs);
  - **Scripts/**: Runtime, Editor (chuyển từ `SandJam/Editor`).
- Di chuyển bằng `AssetDatabase.MoveAsset` (giữ GUID, tham chiếu trong scene/prefab còn nguyên); Build Settings
  tự cập nhật. Prefab và ScriptableObject nạp bằng `Resources.Load` nằm trong thư mục `Resources/` của loại đó,
  nên đường dẫn nạp trong code không đổi.
- `Scripts/Editor/Tools/ProjectPaths.cs`: mọi đường dẫn file mà công cụ editor dùng nằm ở một chỗ (thay 17 chuỗi
  viết cứng ở 6 file).
- `.gitignore` cập nhật theo đường dẫn mới; `Prefabs/Resources` và `ScriptableObjects/Resources` (dựng từ nội
  dung gốc) chỉ để trên máy. Git ghi nhận 266 file đổi chỗ, không file nào bị xoá.
- README (sơ đồ thư mục) và `THIET_KE_LEVEL_VI.md` cập nhật đường dẫn.

**Kiểm tra:** biên dịch không lỗi. `Resources.Load` tìm thấy catalog, prefab Level001, 14 ảnh động, CharacterVisual,
Campaign.json, shader. Tự chơi level 2, 68, 267 thắng cả 3. Booster, luồng thua / chơi tiếp (ô 6) / bỏ cuộc /
thử lại / về Home không lỗi. Màn chơi level 259 (khối "?", ô khóa) hiển thị đúng.

### Gallery: vuốt ngang để lật băng thẻ

**Đo:** bản gốc lật băng thẻ bằng vuốt (video 2, giây 26,3 và 36,9). Bản mình chỉ có hai nút mũi tên.

**Sửa**

- `DragScroller`: lần kéo bắt đầu theo chiều ngang thì không cuộn trang, mà báo là vuốt (`SwipeX` khi đang giữ,
  `SwipeSpeed`, `SwipeReleased` khi thả). Nút đang giữ bị huỷ khi đã vuốt, như khi cuộn dọc.
- `GalleryPopup.Swipe`: vuốt bắt đầu trên băng thẻ thì băng thẻ đi theo ngón tay (đi một nửa khi không còn bộ ở
  phía đó). Thả ra: kéo quá 1/3 bước thẻ hoặc vuốt nhanh (> 4 đơn vị/s) thì lật trang, trượt tiếp từ đúng vị trí
  ngón tay; không thì bật về. Dùng lại `Slide` (tách `MoveCarousel`) và `RunGalleryAction`.

**Kiểm tra:** mô phỏng vuốt ở level 268 (gán độ vuốt qua từng khung): vuốt 1,4 thì lật sang bộ trước, trượt liền
mạch; vuốt 0,5 thì bật về; vuốt về phía không còn bộ thì không lật. Cuộn dọc Shop vẫn trượt có quán tính. Tự chơi
level 68 thắng; luồng thua không lỗi.

### Home: nhân vật trên lối đi đi nhanh như bản gốc

**Đo**

- Video 2, giây 0–3,4: trên Home chỉ có nhân vật đi qua lối đi chuyển động (nút PLAY, rương, đèn đứng yên).
  Nhân vật đi từ x 852 tới x 40 trong 1,7 s ≈ 480 px/s.
- Bản mình (đo vị trí trên màn hình từng khung): ≈ 390 px/s, chậm hơn 1,24 lần.

**Sửa** (`VideoScreen`): `MascotWalk` 3 → 2,42 s, `MascotLap` 3,7 → 3,12 s (giữ ~0,7 s ngoài màn giữa hai lượt),
`MascotAnimSpeed` 1,8 → 2,23 (bước chân nhanh cùng tỉ lệ, không bị trượt chân).

**Kiểm tra:** đo lại: x −84 → 879 trong 1,97 s ≈ 490 px/s.

### Video 3 (23/10, level 341, có tiếng): Gallery lật thẻ khi nhận; màn thưởng mốc bộ sưu tập

Video mới: `ScreenRecording_10-23-2026 13-59-00_1.mp4` (275 s, 50 fps, có tiếng). Có các cảnh chưa từng có: Claim
All Cards, nhận mốc thưởng Gallery (40, 60 xu, hộp quà), level 341 hết chỗ hai lần.

**Đo**

- Claim All (giây 53,45–54,1): cả lưới thẻ "?" co ngang về 0 (~0,4 s, kèm lấp lánh), rồi mặt tranh giãn ra
  (~0,15 s). Bản mình đổi thẳng sang tranh.
- Nhận mốc (giây 58,1–62): nền tối, Gallery vẫn thấy phía dưới; quầng sáng xoay; dải CONGRATS ở y 490; chồng xu
  ~400 px ở y 960 kèm số (y 1080); nút "Get Rewards" (x 242) và "x2" (x 655) ở y 1355; mốc cuối là hộp quà.
  Bản mình chỉ cộng xu, không có màn nào.
- Dựng lại lưới thẻ luôn đưa thanh cuộn về đầu (nhận một thẻ phía dưới là nhảy lên trên).

**Sửa**

- `GalleryPopup`:
  - lật thẻ khi nhận một thẻ hoặc Claim All: `KeepCard` ghi các mảnh của mỗi thẻ; `FoldCards` co / giãn ngang
    quanh tâm thẻ (`FoldSeconds` 0,4, `UnfoldSeconds` 0,15); trong lúc lật không nhận thao tác khác;
  - cuộn chỉ về đầu khi mở Gallery hoặc lật trang, không về đầu khi nhận thẻ;
  - nhận mốc mở màn thưởng; `ResumeGallery` quay lại đúng bộ, đúng vị trí cuộn.
- `CongratsPopup`: màn thưởng mốc (`BuildReward`, `ShowGalleryReward`), dùng chung dải CONGRATS (`Ribbon`) với
  thẻ CONGRATS sau màn; quầng sáng `SpinningLight`, chồng xu `icon-coin-reward` hoặc `icon-gift`. Nút x2 nhân đôi
  xu như ở Level Complete (bản gốc là xem quảng cáo).
- `PopupScreens.ShowGalleryReward`: giữ trang Gallery hiện bên dưới, đưa màn thưởng lên trước (`OverPageZ`), trả
  lại khi quay về Gallery.

**Kiểm tra**

- Level 268, Claim All: thẻ co về 0 rồi tranh mở ra.
- Nhận mốc 0: màn 40 xu đè lên Gallery tối; x2 thì xu 200 → 240 → 280; Get Rewards thì về đúng bộ (dấu tích).
  Mốc 2: hộp quà, +100 xu và 3 booster.
- Popup khác giữ nguyên chiều sâu (z −5). Tự chơi level 2, 68 thắng; luồng thua / chơi tiếp / bỏ cuộc không lỗi.

### Booster Select: bảng "SELECT ANY SHOOTER", khối chọn được lắc lư (video 3)

**Đo** (video 3, giây 209,6–216,3, level 361)

- Bấm Select: bảng tím trong suốt hiện đè lên hàng ô chờ (x 158–730, y 1108–1295 px; màu đo 168,160,208 trên
  nền ô 168,167,186). Trên bảng: "SELECT ANY SHOOTER", đường kẻ, "Tap on any shooter you want", nút X đỏ ở bên
  phải. Nút booster không nhấp nháy.
- Khối chọn được lắc qua lại ±3,5°, chu kỳ ~0,53 s.
- Chạm khối ở hàng sau: khối nhảy ra khỏi làn, chạy vòng ra ngoài rồi lên ô chờ. Số booster giảm 6 → 5.
- Bản mình: bật Select không hiện gì (chỉ nút booster nhấp nháy), khối không lắc. Lý do khối không động đậy:
  `SandJamSceneController.Update` dừng cả game khi booster đang chờ thao tác (`BoosterInputBlocked`).
- Hai lần lóe sáng trên tranh (giây 217, 229) là hiệu ứng mở vùng, không phải booster.
- Nút booster có badge số vàng ở góc dưới trái: khớp sẵn.

**Sửa**

- `BoosterController`: `BuildSelectCard` (dựng một lần, theo số đo) và `ShowSelectCard`; `RockCubes` cho các
  khối `CanSelectPriority` lắc; bỏ nhấp nháy nút. Trong lúc chờ chọn, gọi `controller.Advance(0)` để khối được
  dựng dáng mà game vẫn đứng (không đổ cát). Chạm ra ngoài khối hoặc nút X thì huỷ như trước.
- `SceneActorView.Rock`: độ lắc (độ) cộng vào phép nghiêng quanh hướng nhìn camera đang có trong `ApplyYaw`.

**Kiểm tra**

- Level 361: bảng hiện đúng chỗ, nhìn xuyên thấy ô chờ; khối chọn được lắc; chọn khối hồng ở hàng sau thì
  khối chạy lên ô chờ, bảng tắt.
- Luồng booster (swap, rocket, select) và thua / chơi tiếp / bỏ cuộc không lỗi. Tự chơi level 68 thắng.
  Level 361 bộ giải không thắng được với 5 ô (cần booster, như trong video).

### Âm thanh: mọi nút giao diện phát "pop" (video 3)

**Đo** (video 3 có tiếng; so dạng sóng với các file âm thanh gốc, khớp 0,98–1,0)

- `pop` ở 10,1 s (đóng Sand Quest), 25,9 / 44,4 / 45,3 / 70,3 s (chuyển tab SHOP / HOME / GALLERY), 53,7 s
  (Claim All), 58,0 / 60,2 / 62,5 s (nhận mốc Gallery), 140,3 / 172,2 s (nút Home), 272,4 s.
- `Input` chỉ khi chạm khối (48 lần), kể cả chạm khối bằng Select (215,9 s, một tiếng). Bấm nút booster: im
  lặng. Thắng: `level completed panel` (240,6 s) rồi `Victory` khi bảng Level Complete hiện (245 s): khớp sẵn.
- Bấm "Get Rewards" ở màn mốc thưởng có một tiếng không khớp file nào đã có (không có trong dữ liệu đã giải nén).
- Bản mình: nút giao diện phát `Button.ogg`, thực ra là bản sao của tiếng chạm khối (`Select.ogg`); chỉ PLAY
  phát pop.

**Sửa** (`AudioManager`): `OnButton` luôn phát `Pop`; bỏ `Sfx.Button` (trùng tiếng chạm khối).

**Kiểm tra:** biên dịch không lỗi; luồng booster / thua không lỗi; tự chơi level 2 thắng.

## 23/10/2026 — Popup NEW ART STYLE và đủ 57 bộ sưu tập

**So với video 3 (75 s, level 361):** gốc ghi tên bộ sưu tập "Infinite Canvas" với ảnh phẳng Superman/Batman
lấp đầy ô bo góc; thẻ to hơn ~8% và cao hơn ~60 px. Bản mình ghi "Celebs" với ảnh render cát tràn khỏi ô, vì
bảng bộ sưu tập chỉ có 17 bộ.

**Sửa:**
- `GalleryCollections`: ba mảng Names / Counts / Covers gộp thành một bảng `Styles` (tên, số level, ảnh bìa)
  đủ 57 bộ như game gốc. Bộ 1–17 dùng `Gallery/cover-N`; bộ 18–57 dùng ảnh phẳng của level đại diện
  (`GalleryCovers/<levelId>`, chép từ dữ liệu gốc, chỉ để local, không đẩy lên git).
- `PopupScreens.CoverPair` (dùng chung cho Gallery, CONGRATS, NEW ART STYLE): ảnh màu và ảnh xám của một bộ.
  Chỉ bìa `Gallery/` mới cắt lề; thiếu file `-bw` thì tự làm xám; không có bìa thì dùng render của level.
  Bỏ ba đoạn code trùng nhau ở ba popup.
- `RoundedCopy` chuyển từ `CongratsPopup` sang `PopupScreens` để dùng chung.
- NEW ART STYLE: thẻ phóng 1,08 và nâng 60 px (`FitCard`); ảnh bo góc lấp kín phần nhìn thấy của ô
  (ảnh nền ô có lề trống phía trên).

**Kiểm tra:** biên dịch không lỗi; 57/57 bộ có đủ ảnh màu + xám; ảnh chụp popup ở level 361 khớp video;
tự chơi level 2, 18, 68 thắng; luồng booster / thua không lỗi.

## 23/10/2026 — Luật chọn của booster Select

**Yêu cầu người dùng:** Select chỉ chọn được khối đơn đã hiện rõ màu.

**Sửa** (`SandGame.Boosters`): `CanSelectPriority` nhận mọi khối đơn còn trong hàng, ở bất kỳ vị trí nào,
miễn là màu đang hiện (khối "?" chờ sau khối khác thì không), không bị đóng băng và còn ô trống. Bỏ điều kiện
"phải đổ được ngay". Khối nối cặp không chọn được nữa nên bỏ `PairUnblocked` / `Find` và phần di chuyển cặp
trong `SelectPriority`.

**Kiểm tra:** level 361: mọi khối "?" ẩn bị loại, khối hiện màu chọn được; luồng booster (Select chọn được
khối) không lỗi; tự chơi level 2, 68 thắng.

**Dọn local:** xoá `Build/` và `Builds/` (~650 MB, bản build Windows cũ ngày 7/10, git đã bỏ qua, không code
nào dùng; build lại được từ Unity).

## 23/10/2026 — Rương Sand Quest (JOIN) khi chưa mở khoá

**Người dùng báo** (kèm ảnh gốc ở level 362): gốc luôn có rương ở Home; trước khi mở khoá rương xám và ghi
level mở khoá. Bản mình ẩn hẳn rương trước level 85.

**Dữ liệu gốc** (`MainMenu_Basic`, `SandQuestUWidgetSlot`): nút rương bị tắt dùng `Chest_Disable`, bảng dùng
`Chest_Label_Disable`, thêm `Locked` (icon-lock, 65x90 trên bảng 262x111, lệch trái 87) và `NeedLevel`
("Lv. N", lệch phải 12).

**Sửa** (`CampaignUi.BuildQuestLock` / `ShowQuestLock`): rương luôn hiện. Khi chưa mở khoá: rương và bảng xám,
icon khoá bên trái, chữ "Lv. 85" viền xám thay JOIN, rương không nhận chạm. Mở khoá thì về lại art vàng và
JOIN. Art xám và icon khoá chép từ dữ liệu gốc vào `Resources/VideoUI` (chỉ local).

**Kiểm tra:** ảnh chụp Home ở level 68 (khoá) và 362 (JOIN) khớp ảnh gốc; luồng booster / thua không lỗi;
tự chơi level 2, 68 thắng.

## 23/10/2026 — Hộp quà mốc cuối Gallery mở ra như gốc; quà 150 xu

**So với video 3 (63,6–67 s):** thanh xu 3695 → 3735 → 3795 → 3945: ba mốc là 40, 60 và 150 xu; hộp quà chỉ có
xu. Bản mình cho 100 xu + 3 booster (đoán, không có nguồn) và hiện icon hộp quà đứng yên.

**Dữ liệu gốc:** prefab `popup_gallery_reward_gift` (thân hộp `Group 10082`, nắp `Group 10083`) và clip
`gift_open` (1 s): hộp nén còn 48% chiều cao và rung ±4° (0,17–0,73 s), bật lên 135% (0,83 s) rồi về 100%; nắp
bay lên trái (x 7 → −241, y 316 → 417 → 46 đơn vị canvas) và xoay 64° (0,83–1,12 s).

**Sửa:**
- `CongratsPopup.BuildGift` / `TickGift`: dựng hộp + nắp bằng art gốc (chép vào `Resources/GalleryPage`, chỉ
  local) và chạy đúng các đường cong trên; đồng xu kèm số trồi từ miệng hộp (y 920 → 712, 1,3–1,65 s); nút
  "Get Rewards" / x2 hiện sau 1,9 s.
- `GalleryCollections`: mốc cuối 150 xu, bỏ 3 booster.

**Kiểm tra:** chụp từng mốc thời gian của hoạt cảnh; khung cuối khớp vị trí hộp, nắp, xu với video; luồng
booster / thua không lỗi; tự chơi level 2, 68 thắng.

**Còn khác:** gốc 4% ở thẻ CONGRATS sau level 361, mình 6% (dữ liệu gốc ghi 15 level / bộ; có thể bản live đã
đổi). Chưa sửa vì chưa đủ bằng chứng.

## 23/10/2026 — Trang Gallery theo gốc: bìa màu, khung vàng, bộ kế bị khoá, thẻ "?" viền vàng

**So với video 3 (69,5 s, level 361) và video 2 (28–34 s):**
- Bìa các bộ đã mở luôn có màu. Bộ chưa xong có khung vàng, bộ đã xong khung xanh. Bản mình hiện bìa xám với
  phần màu dâng dần (không có trong video nào) và luôn khung xanh.
- Bên phải bộ đang chơi là bộ kế bị khoá: khung vàng, ảnh tối ~40%, ổ khoá trắng mờ; có mũi tên phải. Bản mình
  không có.
- Thẻ "?" chưa mở có viền vàng mảnh, bo tròn; bản mình là ô tối không viền. Hàng thứ 3 lộ một phần sau thanh
  điều hướng; bản mình ẩn.
- Có chữ "Gift" dưới hộp quà; nền ca-rô chỉ sau carousel, dưới thanh mốc là nền xanh trơn.
- Trên trang Gallery không có badge "!"; bản mình lọt chữ "!" của Home (đè lên mọi lớp, `sortingOrder` 42).

**Sửa:**
- `GalleryCollections.NextLocked` + `Info.Locked`: bộ kế được thêm vào cuối carousel.
- `GalleryPopup`: `CardFrame` (vàng nếu chưa xong hoặc bị khoá, art gốc `bg-cards-top-carCollection - gold`
  chép local), `CoverPicture` (bìa màu; bộ khoá tối + `VideoUI/icon-lock`); bỏ ảnh xám dâng màu và
  `GalleryPicture` không còn dùng. `GoldCard` dùng chung cho thẻ "?" và thẻ chờ nhận (viền ~6 px, bo tròn). Chữ
  "Gift", hộp quà to hơn. Nền dưới bắt đầu ngay dưới thanh mốc; hàng thẻ kế tiếp hiện sau thanh điều hướng.
- `CampaignUi.AlertBadge`: chữ "!" cùng thứ tự vẽ với đĩa đỏ nên trang che được.

**Kiểm tra:** chụp Gallery ở level 361 so với video (khớp bố cục); bấm sang bộ khoá "Iconic Realms" hiện ảnh
tối + ổ khoá; badge Home vẫn đủ đĩa đỏ và "!"; luồng booster / thua không lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Ổn định trước khi làm mechanic / booster mới

**Tự chơi diện rộng:** 30 level rải từ 2 đến 498: 28 thắng ngay, không exception. Level 31 và 222 thua vì script
tự chơi (cục bộ) chọn lane mỗi 0,25 s khi khối trước còn đang đổ, trong khi bộ giải chờ đổ xong; sửa script chờ
đổ xong thì cả hai thắng. Chạy lời giải trên luật thuần (`SandGame`) cũng thắng: không phải lỗi game. Level 361
cần booster (đã biết).

**Bảng booster (`BoosterCatalog`):** id và giá của mọi booster ở một chỗ. Trước đó chuỗi "rocket" / "swap" /
"select" viết tay ở 10 file (giá, số lượng, save, HUD, shop, nhiệm vụ, quà). Giờ thêm booster = một dòng trong bảng
+ hiệu ứng trong `BoosterController`.
- Save v3: `boosters` là danh sách (id, số lượng); save v2 (`flares`, `swaps`, `selects`) tự chuyển khi đọc.
- `EconomyManager` đọc giá và số lượng theo bảng; id lạ bị bỏ qua kèm cảnh báo.
- `OriginalConfig` có trường `Boosters` riêng, nên bảng đặt tên `BoosterCatalog` để khỏi nhầm (cả
  `VideoScreen.Boosters`).

**Bớt trùng lặp:**
- `Campaign.IdAt(level)` và `Campaign.PictureId(id)` thay 5 chỗ tự tính id level và tự bỏ hậu tố `_Dupe` bằng
  Regex (Gallery, CONGRATS / bộ sưu tập, NEW ART STYLE).
- Bỏ hàm bọc `Hex` trong trình dựng scene (gọi thẳng `ColorPalette.Hex`).

**Tài liệu:** `Documentation/THEM_MECHANIC_BOOSTER_VI.md`: các chỗ phải chạm khi thêm mechanic hoặc booster.

**Kiểm tra:** biên dịch không lỗi; save v2 (10 / 3 / 7) chuyển đúng sang v3, cộng / dùng / đọc lại đúng; id bộ
sưu tập level 361 không đổi; luồng booster (Swap, Flare, Select) và thua / chơi tiếp / bỏ cuộc không lỗi; tự chơi
level 2, 68, 120 thắng.

## 26/10/2026 — Chân khi chạy (người dùng: "phần di chuyển chân trong gameplay khá xấu")

**Đo** (video 3, 84,3–84,6 s, từng khung 50 fps; bản mình quay từng bước 0,02 s, giữ controller và gọi
`Advance(0.02)` để chân và chuyển động cùng nhịp):
- Gốc: chân to (mỗi chân ~1/5 bề ngang khối), thò dưới đáy ~0,7–0,8 chiều cao khối, gối gập rõ, sải rộng; khối rỗng
  chạy ra khỏi ô đều ~570 px/s, thấy trọn một chu kỳ bước (~0,35 s tới mép màn).
- Bản mình: chân mảnh (chỉ kéo dài 1,45 theo chiều dọc), xoay đùi 35° ra sau; hông rig nằm trong hộp nên khối rỗng
  lộ chân gập trong kính; khối rỗng lướt qua phần nhìn thấy trong ~0,1 s (thời lượng cố định 0,6 s cho quãng tới
  x = ±4,8, bắt đầu nhanh).
- Lỗi: vị trí đùi không được Animator đặt lại mỗi khung, nên phép xoay / hạ đùi bị cộng dồn qua từng khung (chân
  trôi xa khỏi khối).

**Sửa** (`SceneActorView`):
- Nhớ vị trí nghỉ của đùi, đặt lại mỗi khung trước khi xoay / hạ.
- Chạy: hạ khớp háng 0,2 xuống đáy hộp (`RunLegLower`, theo trục dọc thế giới vì rig nghiêng về camera); chân to
  1,7 lần, dài 1,3 lần (`RunLegThick`, `RunLegLength`); đùi chỉ xoay 12° (`RunLegDrop`).
- Rời ô: chạy đều `ExitSpeed` 3,2 đơn vị/s; cú nhảy khỏi ô tính theo thời gian (`ExitHopSeconds` 0,3 s).

**Script tự chơi (cục bộ):** chờ 0,8 s sau lần đổ cuối mới chọn tiếp, vì vùng mới chỉ mở khi cát đã lắng (bộ giải mở
ngay); level 222 trước đó lúc thắng lúc thua, giờ thắng 3/3.

**Kiểm tra:** quay chạy lên ô và rời ô so với video (chân dưới đáy hộp, sải rộng, thấy trọn chu kỳ); luồng booster /
thua không lỗi; tự chơi level 2, 68 thắng, 222 thắng 3/3.

## 26/10/2026 — Chân thon lại (người dùng: "chân nó to quá rồi để nhỏ lại đi")

`SceneActorView.RunLegThick` 1,7 → 1,3 (chiều dài giữ 1,3, hông vẫn hạ xuống đáy hộp). Quay lại cảnh chạy lên ô:
chân thon, vẫn sải bước và gập gối.

## 26/10/2026 — Chân to vừa với khối, hết mảng tối dưới đáy (người dùng: "chân có độ to vừa phải, phù hợp với khối")

**So sánh** (cùng bề ngang khối, gốc video 3 84,4 s cạnh bản mình ở độ dày 1,3 và 1,5): đùi gốc rộng ~1/4 khối;
1,5 gần gốc hơn 1,3. Cái làm chân trông thô là một mảng tối hình chữ V ngay dưới đáy khối: mặt cắt hở ở eo của
mesh chân (tách khỏi thân nhân vật gốc), lộ ra khi hông bị hạ 0,2 xuống dưới đáy hộp; chỉ hạ hai đùi còn kéo
giãn phần hông.

**Sửa** (`SceneActorView`): độ dày `RunLegThick` 1,5; hạ cả khớp hông (không chỉ hai đùi), chỉ 0,08
(`RunLegLower`) để mặt cắt nằm ngay trên đáy hộp; chân dài 1,3 nên vẫn thò ra đủ. Vị trí hông: Animator có thể
không ghi lại mỗi khung, nên phần hạ của khung trước được trả lại trước khi hạ tiếp (không cộng dồn).

**Kiểm tra:** quay từng khung chạy lên ô và rời ô: chân liền dưới khối, không còn mảng tối; luồng booster / thua
không lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Chân nhỏ nhắn (người dùng: "muốn nó nhỏ nhắn, to như hiện tại gây khó chịu")

`SceneActorView`: độ dày khi chạy `RunLegThick` 1,5 → 0,9 (mảnh hơn chân gốc của rig), chiều dài `RunLegLength`
1,3 → 1,15; hông vẫn hạ 0,08 (mặt cắt eo khuất trong hộp). Quay từng khung chạy lên ô và rời ô: chân thanh, vẫn
sải bước, gập gối; tự chơi level 2, 68 thắng.

## 26/10/2026 — Tối ưu cho mobile: bớt cấp phát bộ nhớ, bỏ việc thừa của khối đứng yên

**Luật mới (người dùng):** luôn tối ưu để game chạy ổn định, mượt, không giật lag trên mobile (đã thêm vào phần
quy tắc code ở đầu file).

**Đo:** Profiler của Unity khi tự chơi level 68 (300 khung, gom theo hàm; script đo cục bộ). Cấp phát của game
~1 070 B/khung, thủ phạm: `ChainLinkView` 384 B (đọc / ghi `sharedMaterials`, mảng `new[]`, đọc `vertices` /
`normals` mỗi lần uốn xích khi cặp đang chạy, `SetInQueue` tạo mảng mỗi khung), `CharacterQueueController` 192 B
(cũng do `SetInQueue`), `TextOutliner` 199 B (quét mọi TextMesh 0,4 s một lần bằng mảng mới), `OutlinedText` 154 B
(đọc `TextMesh.text` mỗi 8 khung để dò chữ đổi), `SceneRegionView.Apply` 131 B (`GetComponentsInChildren` mỗi
lần số đếm đổi), `SceneActorView` 28 B. Thời gian: `characterQueue.Synchronize` 1,64 ms/khung: mỗi khối (~30)
ghi lại vị trí / scale / góc hai lần mỗi khung kể cả khi đứng yên (mỗi lần ghi cập nhật cả rig ~50 xương).

**Sửa:**
- `SceneActorView`: khối đã ghi xong tư thế nghỉ thì bỏ qua phần ghi (`restWritten`, `Still`) cho tới khi có gì
  làm nó động (chạy, nảy, lắc, đổ, cát đổi, lắc Select, rời, gộp). Hai đùi đặt bằng `PoseThigh` (không mảng).
- `CubeContactShadow`, `ChainLinkView`: theo `Still`, không tính lại bóng / vẽ lại xích khi khối đứng yên.
- `ChainLinkView`: vật liệu chỉ đặt khi thứ tự đổi; `MeshFilter` của vòng dưới giữ lại; mảng đỉnh đọc một lần, uốn
  vào mảng dùng lại; `Hide` thay mảng trong `SetInQueue`.
- `OutlinedText.SetText`: số trên khối và số đếm vùng ghi qua đây, viền theo ngay, nhãn đó thôi bị dò định kỳ;
  renderer của 8 bản viền giữ lại.
- `TextOutliner`: quét bằng danh sách dùng lại. `SceneRegionView`: tấm nền số đếm tìm một lần; `SortCounter` dùng
  danh sách dùng lại; số trên ổ khoá và vùng xong không ghi lại mỗi khung. `CharacterFreezeView`: số trên băng lấy
  từ bảng chuỗi, chỉ ghi khi đổi. `CharacterDepthStyle`: ID thuộc tính `_Color`.

**Kết quả** (A/B liền nhau cùng điều kiện, mỗi bản 2 lần, trong Editor):

| | Trước | Sau |
|---|---|---|
| Tổng thời gian script của game | 3,5–3,9 ms/khung | 2,5 ms/khung (−33%) |
| Controller gameplay | 1,7–1,8 ms | 1,2 ms (−30%) |
| Cấp phát của game | ~1 070 B/khung | ~250 B/khung (−76%) |

**Kiểm tra:** biên dịch không lỗi; quay cảnh chọn khối (hàng dồn lên, xích, đổ cát đúng); luồng booster (Swap,
Flare, Select lắc khối) và thua không lỗi; tự chơi level 2, 31, 68, 120, 222 thắng.

## 26/10/2026 — Khung khối rỗng giữ màu khi chạy ra; tối ưu phần vẽ hình

**Người dùng báo:** khối thả hết cát chạy ra thì khung như mất màu, phá cảm giác mượt.

**Đo** (video 3, 84,04–84,6 s, so cạnh bản mình quay theo thời gian thực): gốc trắng hẳn khi còn đứng trên ô
(~0,24 s), nhảy đi là đỏ lại ngay, cả kính lẫn khung. Bản mình: trắng mờ dần trong 0,18 s ngay lúc bắt đầu chạy, kính
pha 50% trắng, nên khối chạy đi nhạt màu.

**Sửa:** `SceneActorView.EmptyWhite`: trắng suốt lúc dừng trên ô (`ExitPause` 0,12 → 0,18 s), về màu trong 0,05 s
(`EmptyWhiteSnap`) khi nhảy đi. `CharacterSandVessel`: kính pha `GlassWhite` 0,15 (trước 0,5).

**Tối ưu vẽ hình (đo bằng `UnityStats` ở level 68):**
- Không còn bản sao vật liệu: bệ chờ, thanh ray, khung tranh, chân đế đổi màu qua `renderer.material` (mỗi cái một
  bản sao, mỗi bản sao một lần đổi trạng thái vẽ). Helper mới `RendererExtensions.SetBlockColor` / `SetBlockFloat` /
  `SharedColor` (MaterialPropertyBlock) dùng ở `GameplayFeedbackController`, `OriginalLookStyler`, `VideoScreen`.
  SetPass 73 → 61.
- Khối cát bo góc: lưới mỗi mặt 15 → 11 mốc (bo góc 5, giữa 1). 2 352 → 1 200 tam giác mỗi khối; cả màn 68 500 →
  40 900 tam giác. Ảnh chụp trước / sau giống hệt (sai khác trung bình 0,014 / 255).
- `SandToonDepth`: lượt viền đưa đỉnh ra ngoài khung nhìn khi không có viền (trước tô cả vỏ rồi `clip` từng điểm ảnh,
  trên GPU mobile còn tắt loại sớm theo độ sâu).
- Camera tô nền viền đen: tắt HDR, MSAA (chỉ xoá màn hình).

**Kết quả:** script của game ~1,85 ms/khung (đầu ngày 3,7 ms), cấp phát ~225 B/khung; camera ~0,14 ms.

**Kiểm tra:** chụp Home và màn chơi (màu không đổi); quay khối rỗng chạy ra so với video; luồng booster / thua không
lỗi; tự chơi level 2, 31, 120, 222 thắng.

## 26/10/2026 — Tối ưu viền chữ và viền khối cát

**Đo trước** (`UnityStats`, level 68): 144 renderer chữ, trong đó 128 là bản sao viền (mỗi nhãn 8 TextMesh); 175 lệnh
vẽ phải gộp động mỗi khung; khối cát luôn gửi 2 lượt vẽ (viền + thân) dù phần lớn không có viền.

**Sửa:**
- `OutlinedText`: 8 bản sao viền giờ là MỘT lưới phía sau chữ, dựng bằng `TextGenerator` (cùng font, cỡ, neo; 1
  pixel font = `characterSize` / 10 đơn vị như TextMesh), dịch theo 8 hướng, tô màu viền bằng màu đỉnh. Dựng lại khi
  chữ / cỡ đổi và khi atlas font được xếp lại (`Font.textureRebuilt`). `ShowInk` đổi màu viền tạm thời mà giữ `Ink`
  (số hàng sau viền theo màu khối, `CharacterDepthStyle`); `GameplayFeedbackController` dùng `SetInk`.
- Khối cát không viền (hàng sau, cát trong kính) dùng vật liệu sinh đôi với shader mới `SandToonDepthPlain` (chỉ
  `UsePass` lượt TOON của `SandToonDepth`, không chép code): 1 lượt vẽ thay vì 2. Khối hàng đầu giữ viền.

**Kết quả:**

| | Trước | Sau |
|---|---|---|
| Renderer chữ trên màn | 144 | 16 (+16 lưới viền) |
| Lệnh vẽ gộp động mỗi khung | 175 | 62 |
| Batch / SetPass | 81 / 61 | 74 / 57 |
| Tam giác | 40 900 | 32 500 |
| Cấp phát của game | ~225 B/khung | ~155 B/khung |

**Kiểm tra:** chụp Home, màn chơi, Settings, Sand Quest so với trước (chữ và viền trùng, số hàng sau viền theo màu
khối); luồng booster / thua không lỗi; tự chơi level 2, 31, 120 thắng.

## 26/10/2026 — Khối rỗng không còn chớp khung trắng khi rời ô

**Người dùng báo:** khối xả hết cát đi ra có một khoảng rất ngắn bị trắng viền.

**Đo** (từng khung 0,02 s, cạnh video 3, 84,26–84,6 s): gốc cũng trắng ~0,18 s trước khi nhảy đi, nhưng cả khối đọc
là một hộp trắng (kính mờ, có bóng xám, cục cát trắng bên trong) và đứng yên quay mặt ra trước. Bản mình: kính gần
trong suốt (`GlassPane` mặt chỉ đục 0,26), trên nền và bệ gần trắng chỉ còn KHUNG trắng mảnh, lại xoay ngang ngay
lúc đó, nên trông như chớp viền lỗi. Thử đục kính tới 0,9: vẫn gần như vô hình trên nền sáng.

**Sửa:**
- `SceneActorView`: nhịp trắng chỉ ngả 60% về trắng (`EmptyWhitePeak`), khối giữ sắc màu; khối đứng yên trong nhịp
  này, quay ra hướng thoát khi nhảy đi.
- `CharacterSandVessel`: kính mờ dần tới 0,7 (`WhiteGlass`, qua `_Base` của `GlassPane`) trong nhịp đó, nên đọc là một
  hộp kính màu nhạt, không phải khung trơ.

**Kiểm tra:** quay từng khung khối xanh rời ô; luồng booster / thua không lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Khối không đổi màu khi vẫn còn cát (người dùng)

Nhịp sáng màu bắt đầu ngay khi lượng cát về 0 trong dữ liệu, trong khi phần cát hiển thị (`DisplayedFill`) còn vơi
thêm ~0,18 s: khối nhạt màu lúc vẫn thấy cát bên trong. `SceneActorView`: `EmptyWhite` bằng 0 tới khi cát hiển thị
hết hẳn; thời gian dừng (`exitDelay`) và nhịp sáng chỉ tính từ lúc đó. Quay từng khung: giữ màu suốt lúc cát vơi,
sáng nhạt sau khi hết, rồi nhảy đi. Luồng booster / thua không lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Bỏ hẳn nhịp trắng của khối rỗng (người dùng: "không muốn khung trắng, muốn có màu")

Khối rỗng giữ màu của nó từ lúc hết cát tới khi chạy khỏi màn (khác gốc, gốc trắng ~0,18 s; theo lựa chọn của người
dùng). Bỏ `EmptyWhite` và mọi chỗ dùng (`SceneActorView`, `CharacterSandVessel`, `CharacterDepthStyle`, ghi chú
`SandFx`); vẫn dừng trên ô tới khi cát hiển thị hết rồi mới nhảy đi. Quay từng khung: khung xanh suốt; luồng booster /
thua không lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Khối rỗng chạy ra: đứng thẳng, quay mặt trước, chân sải nghiêng

**Đo** (từng khung 0,02 s cạnh video 3, 84,40–84,70 s): khối gốc chạy ra vẫn gần như quay mặt trước (mặt trước
vuông), đứng thẳng, chân nhìn nghiêng sải về phía thoát, chạy trước gờ tím của khung tranh. Bản mình xoay hẳn 90° nên
thấy cạnh lồng, lúc nhảy thì như lồng nằm nghiêng; khối nghiêng ~15° (bù nghiêng `RunUpright` tính cho góc lớn); và bị
đẩy ra sau 0,5 nên đỉnh khối bị gờ tím che.

**Sửa** (`SceneActorView`): `ExitYaw` 90° → 30°; chân xoay nốt phần còn lại (`ExitLegYaw` 60°) nên sải nghiêng; khối
rời đi không bù nghiêng; lùi sau `ExitBehind` 0,5 → 0,2 (vẫn sau khối đang đứng trên ô, trước chân đế khung tranh).

**Kiểm tra:** quay khối rời ô một mình và chạy ngang sau một khối khác đang đứng trên ô; luồng booster / thua không
lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Khối đang chạy cho thấy mặt trước; đổi tư thế mượt

**Đo** (video 3, 84,3–84,7 s): khối gốc đang chạy dựng đứng, mặt trước vuông, gần như không thấy mặt trên. Bản mình
khi chạy vẫn giữ góc nghiêng của ô chờ (16°) nên thấy nhiều mặt trên.

**Sửa:**
- `CharacterSandVessel`: góc riêng khi chạy `RunPitch` 32° (lên ô và rời đi); góc chuyển dần 160°/s (`PitchSpeed`)
  giữa chạy / ô / hàng thay vì nhảy cóc lúc đáp; `Settling` báo `CharacterDepthStyle` cập nhật tiếp tới khi xong.
- `SceneActorView.Moving`: đang thật sự di chuyển (khối rỗng còn chờ trên ô thì không) — khối không đổi góc trước khi
  nhảy đi.

**Kiểm tra:** quay chạy lên ô, đáp, rời ô; luồng booster / thua không lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Đường chạy lên ô theo gốc: bật đi, chạy đều, vượt nhẹ rồi về

**Đo** (vị trí khối từng khung, video 3, 90,30–90,90 s, chọn khối đen hàng trái; bản mình quay 0,02 s / khung): gốc đi
19% quãng đường ngay khung đầu, sau đó đều ~4% mỗi khung, vượt đích 7% rồi lùi về trong ~0,1 s. Bản mình bắt đầu từ
từ rồi chậm dần vào ô (đường cong giảm tốc), trông lừ đừ.

**Sửa** (`SceneActorView.RunPath`): bật `RunLeap` 19% trong 5% đầu thời gian, chạy đều tới 107% ở 82% thời gian
(`RunOvershoot`), về đúng ô bằng smoothstep. Khối rỗng rời đi vẫn chạy đều.

**Kiểm tra:** đo lại quỹ đạo (đều, vượt 7%, về 100%); luồng booster / thua không lỗi; tự chơi level 2, 68, 222 thắng.

## 26/10/2026 — Hàng dồn lên nhanh hơn, theo đo

**Đo** (video 3, 90,30–90,60 s, từng khung): khối sau dồn lên chỗ trống xong trong ~0,27 s, bắt đầu cùng lúc khối trước
rời hàng. Bản mình mất ~0,36 s (`SlideSeconds` 0,38), trông chậm, hàng như bị trễ.

**Sửa:** `SceneActorView.SlideSeconds` 0,38 → 0,28. Đo lại: dồn xong trong ~0,26 s.

**Kiểm tra:** luồng booster / thua không lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Hiệu ứng mở vùng: bong bóng to hơn, hạt lơ lửng

**Đo** (video 3, 83,84–84,50 s, từng khung; bản mình quay theo giờ thực vì hạt chạy theo giờ thực — quay từng bước
0,02 s làm hạt trông như biến mất sau 0,16 s): gốc nở bong bóng ~1,85 đơn vị (gấp đôi vùng) trong ~0,2 s; ~70 khối cát
nhỏ bung ra rồi lơ lửng, trôi chậm hơn nửa giây. Bản mình: bong bóng 1,2; 34 hạt rơi nhanh (trọng lực 0,9).

**Sửa** (`SandFx`): `BubbleScale` 1,6 → 2,5; `BurstGrains` 34 → 70, bung nhanh hơn (0,8–3,2), có lực cản
(`VoxelDrag` 3, `limitVelocityOverLifetime`) và trọng lực nhẹ (`VoxelGravity` 0,25) nên chậm lại rồi lơ lửng; hạt to
hơn chút (0,07–0,12), sống 0,7–1,1 s.

**Kiểm tra:** quay giờ thực cảnh mở vùng ở level 2; đo thời gian (script vẫn ~1,8 ms/khung); luồng booster / thua
không lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Nhịp thắng level: phóng tranh nhanh, ngắm tranh lâu hơn

**Đo** (video 3, đo bề ngang khung tranh từng khung): phóng to bắt đầu 239,98 s (khối cuối vừa hết cát), xong 240,30 s
(0,32 s); tranh giữ nguyên tới 242,15 s mới hiện "Superb!" (~2,15 s từ lúc bắt đầu phóng). Bản mình: phóng 0,9 s,
"Superb!" sau 1,65 s — phóng lừ đừ mà ngắm tranh hoàn chỉnh quá ngắn.

**Sửa:** `WinZoom.Duration` 0,9 → 0,32; `VideoScreen.SuperbDelay` 2,15 (thay số 1,65 viết thẳng trong code).

**Kiểm tra:** quay giờ thực đoạn thắng ở level 2 (phóng xong ~0,3 s, Superb! ở ~2,2 s); luồng booster / thua không lỗi;
tự chơi level 2, 68 thắng.

## 26/10/2026 — "Superb!" kết thúc theo nhịp gốc

**Đo** (video 3, 242,15–244,80 s, từng khung): chữ co lại biến mất từ ~1,93 s trong ~0,25 s; pháo giấy còn rơi một mình
~0,35 s; bảng Level Complete bật ra ở ~2,53 s. Bản mình: co từ 2,1 s trong 0,4 s, bảng hiện ngay sau 0,05 s (không có
quãng chỉ pháo giấy, chuyển cảnh bị dồn).

**Sửa** (`SuperbFx`): `OutroStart` 1,93, `OutroTime` 0,25, thêm `ConfettiOnly` 0,35 (tổng `Duration` 2,53, gần như cũ).

**Kiểm tra:** quay giờ thực từng 0,1 s đoạn Superb! → Level Complete; luồng booster / thua không lỗi; tự chơi level 2, 68
thắng.

## 26/10/2026 — Bảng Level Complete nở ra như gốc

**Đo** (video 3, bề ngang bảng từng khung 244,76–244,90 s): bảng nở từ ~1/5 lên đủ cỡ trong ~0,14 s, nhanh rồi chậm dần,
không vượt. Bản mình: bảng hiện tức thì (trang Result bật thẳng).

**Sửa** (`ResultFx.Pop`): khi trang hiện, phóng gốc trang 0,2 → 1 trong 0,14 s (ease-out); nền tối, thanh xu
(`PopupScreens.CoinBar`, nay `internal` để dùng chung) và pháo giấy được bù lại để giữ nguyên chỗ / phủ kín màn. Các popup
nhân bản từ trang này đã bỏ `ResultFx` nên không bị ảnh hưởng.

**Kiểm tra:** quay giờ thực từng khung lúc bảng hiện; luồng booster / thua không lỗi; tự chơi level 2, 68 thắng.

## 26/10/2026 — Mặt cát trong khối kính thành gò, không phẳng

**Đo** (video 3, phóng to 90,70 s và 83,30 s): khối vàng còn ~một nửa có mặt cát nhấp nhô; khối đỏ gần hết có một gò cát
ở giữa. Bản mình: mặt cát phẳng như cắt (khối cát chỉ co theo chiều cao).

**Sửa** (`CharacterSandVessel`): lưới "gò cát" dùng chung (`Mound`: vòm thấp trên mặt khối, gợn nhẹ, mép bằng mặt cát),
một renderer mỗi khối đặt trên đỉnh khối cát, cùng vật liệu không viền và màu; cao `MoundHeight` 0,15, thấp dần khi
gần hết (< 20%) và gần đầy (> 88%); lật ngẫu nhiên trục x/z để các khối không giống hệt nhau. Chỉ hiện khi khối là kính.

**Kiểm tra:** quay từng khung khối xanh đang đổ (gò hiện rõ, cát vẫn vơi đúng); hiệu năng không đổi đáng kể; luồng
booster / thua không lỗi; tự chơi level 2, 68 thắng.

## 27/10/2026 — Hoàn thiện hình khối băng và ổ khoá theo ảnh gốc

**Ảnh gốc tìm lại** (Texture2D, chỉ để local, không lên git): `ice_new.png` (= `Mechanics/Ice.png`), `IceMatCap.png`,
`snowflake.png` cho khối băng; `Lock_Color.png` + mesh `Lock` cho ổ khoá (chép vào `Resources/Mechanics/Lock.asset`,
`LockColor.png` giới hạn 512 px — thư mục đã gitignore). So hình theo 2 GIF giới thiệu gốc (Freeze 40 khung, Unlocker 38 khung).

**Sửa:**
- `SandIce.shader`: băng xanh lơ sáng, vân băng mờ trên mặt bên, mặt trên gần trắng, mép sáng (trước: xanh lam đục, tối).
- Số trên khối băng (`AddFreezeShell`): chữ trắng viền xanh như gốc. `OutlinedText.Awake` huỷ lưới viền bị nhân bản theo
  nhãn gốc; bỏ `ReferenceTextOutline` cũ (nó bỏ qua màu viền tối < 0,35).
- Ổ khoá trên tranh (`SceneRegionView.BuildPadlock`): ổ khoá 3D vàng (mesh + texture gốc) nghiêng nhẹ, số trong bong bóng
  trắng ở góc trên phải, chữ nâu tối viền trắng; kích thước tính theo bề ngang bảng. Thiếu mesh (repo không có file gốc)
  thì chỉ hiện bong bóng số.
- `SandToonDepth` / `SandToonDepthPlain`: thêm `_Albedo` (mặc định 0) để tô theo màu texture — dùng lại shader khối, không
  thêm shader mới; vẫn chỉ 1 lần lấy mẫu texture.

**Màn có mechanic** (không có trong Campaign.json): băng 19 màn (AnimalPortrait 1,2,3,5,6,9,10,12,14,15; Dance
2,3,6,8,9,10,12,14,15); khoá/chìa 20 màn (Halloween 1,2,3,5,6,7,10,11,12,14; ModernPosters 1,2,4,5,8,9,10,12,14,15).

**Kiểm tra:** nạp tạm từng màn vào ô level 68 (chỉ trong bộ nhớ), tự chơi cả 39 màn: **39/39 thắng**, không exception;
luồng booster / thua không lỗi; tự chơi level 5, 31, 68 thắng.

## 27/10/2026 — Băng vỡ dần và ổ khoá mở như gốc

**Tìm trong game gốc:** cả khối băng (`CharacterFreeze`) lẫn ổ khoá (`PaintablePart.LockMeshRenderer`) đều là
`SkinnedMeshRenderer` có 1 blend shape: mesh `Ice_Cube` mòn từ trên xuống (9 khung), mesh `Lock` bật quai và xoay mở
(4 khung). Mesh + `snowflake.png` chép vào `Resources/Mechanics` (local, gitignore; bật Read/Write để bake).

**Đo theo GIF** (khung 0,14 s):
- Băng: mỗi lần đếm lùi loé trắng, tia nứt trắng bắn ra, băng mòn bớt (4/5 chỉ sứt đỉnh, 2/5 còn nửa, 1/5 còn vành); về
  0 tan hết, bông tuyết bay quanh khối ~1 s.
- Khoá: mỗi khối chìa nhận vào làm khoá nảy to ~20%; về 0: bong bóng + hạt màu vùng + confetti, khoá trắng bừng trong lúc
  quai bật mở, vàng lại, rồi thu nhỏ biến mất cùng số "0" (~0,7 s).

**Sửa:**
- `BlendShapeCache` (mới, Core/Rendering): bake blend shape đầu tiên ở 20 mức, mỗi mức 1 lần, dùng chung — chỉ đổi
  `sharedMesh`, không dùng SkinnedMeshRenderer (đỡ skin mỗi khung trên mobile). Mesh không Read/Write thì bỏ qua.
- `CharacterFreezeView`: vỏ băng là mesh gốc, mòn theo `(1 - còn/ban đầu)^1,5` (khớp GIF); `_Flash` mới trong
  `SandIce.shader` loé trắng mỗi nấc. Thiếu mesh gốc (repo) thì vẫn là vỏ bo tròn mờ dần như cũ.
- `AddFreezeShell`: khớp mesh gốc theo khung bao đỉnh (`mesh.bounds` gồm cả phạm vi blend shape nên nhỏ sai), đáy đặt tại gốc.
- `PadlockView` (mới): nảy khi nhận khối, mở / loé / thu nhỏ khi xong; chỉ chạy khi đang có animation. Số đếm của vùng
  vừa mở (`SceneRegionView.Unlocks`) chờ khoá biến mất mới hiện (chữ GUI/Text luôn vẽ đè lên khoá).
- `SandFx`: vụ nổ mở vùng dùng lại cho ổ khoá (tại ổ khoá); `IceCracks` (loé + tia nứt bằng hệ hạt `spray` sẵn có) và
  bông tuyết khi băng vỡ.

**Kiểm tra:** quay từng khung lúc băng giảm nấc, lúc băng vỡ, lúc khoá mở (so với GIF); tự chơi 8 màn băng / khoá + luồng
booster / thua + level 2, 68.

## 27/10/2026 — Chân thu ngay khi khối lên ô

**Đo** (video 3, 179,60–180,52 s từng 0,02 s, so với bản mình chụp bước cố định 0,02 s): cú chạy từ hàng lên ô khớp
(tốc độ trên màn ~0,21 màn/s, to lên ~1,18 lần khi bay, xoay đổ 30° đã có). Khác: gốc tới ô ở 180,22 s thì chân biến mất
ở ~180,32 s (~0,1 s); bản mình chân còn ~0,3 s (đoạn lùi về sau khi vượt 7% + `LegLinger` 0,15 s) — khối như đứng
lơ lửng trên ô.

**Sửa** (`SceneActorView`): bỏ `LegLinger` / `landedAge`; chân tắt ngay khi đường chạy kết thúc (còn ~0,12 s gồm đoạn
lùi về, khớp gốc).

**Kiểm tra:** quay từng khung lúc lên ô; luồng booster / thua không lỗi; tự chơi level 2, 31, 68 thắng.

## 27/10/2026 — Khối ngồi xuống ô rồi mới đổ

**Đo:** từ lúc khối chạy tới ô đến phát cát đầu tiên (số trên khối giảm): video 2 155,88 → 156,04 s (0,16 s), video 3
180,22 → 180,44 s (0,22 s) — khối thu chân, ngồi xuống, xoay về vùng rồi mới đổ. Bản mình bắn ngay khi hết đoạn lùi về
(~0,11 s sau khi tới), cảm giác vội.

**Sửa:** `SceneActorView.ReadyToPour` = đứng yên + (sau khi chạy) đã ngồi thêm `PourSettle` 0,08 s;
`CharacterQueueController.CanShoot` và tư thế nghỉ (`Resting`) dùng nó. Phát đầu nay ~0,19 s sau khi tới ô.

**Kiểm tra:** đo bước cố định 0,02 s (tới ô 0,49 s, phát đầu 0,68 s); luồng booster / thua; tự chơi level 2, 31, 68, 222
và 2 màn băng / khoá.

## 27/10/2026 — Mỗi khối đổ hết trong ~2,1 s, khối to đổ nhanh hơn

**Đo:** video 2 khối 20: 20 → 0 trong 156,0 → 158,05 s (~2,05 s); video 3 (màn 361) khối 40: 40 → 0 trong
83,9 → 86,1 s (~2,2 s), khối đỏ 24 → 1 trong 1,4 s. Gốc đổ mỗi khối trong thời gian gần như cố định, không theo tốc độ
cố định. Bản mình đổ đều 9,5 đơn vị/s nên khối 40 mất ~4,2 s — chậm gấp đôi gốc.

**Sửa:** `SandGame.Tick(share)` — mỗi tick khối đang đổ cho `share` lượng cát ban đầu của nó, phần lẻ cộng dồn
(`Shooter.PourCarry`) nên khối nhỏ vẫn đổ đều. `SandJamSceneController`: `PourSeconds` 2,1 s, tick 0,02 s; bỏ
`AmmoPerShot` / `ShotInterval` (và các dòng gán trong scene builder). `LevelReplaySolver` tick cùng kiểu (`.01`).

**Kiểm tra:** đo bước cố định 0,02 s ở level 68 (khối 40): đổ 0,68 → 2,76 s (2,08 s); luồng booster / thua; tự chơi.

## 27/10/2026 — Khối băng theo đúng material gốc, có vệt băng loang dưới chân

**So:** GIF Freeze gốc — băng xanh nhạt trong, mặt trên gần trắng, mép có sương giá, nhìn xuyên thấy màu khối bên trong
(khối xám thấy xám, khối tím thấy tím), dưới chân có vệt băng loang. Bản mình: băng gần như đục (alpha 0,9), không thấy
khối, giữa mặt có vệt chữ thập trắng (UV dịch +0,5 làm mép sương giá của texture rơi vào giữa mặt).

**Nguồn gốc:** prefab Character gốc — `IceCube` (SkinnedMeshRenderer, mesh Ice_Cube) dùng material `IceCube_0`
(Toony Colors, premultiplied): `ice_new` × (0,87; 0,94; 1), emission (0; 0,34; 0,87), rim (0; 0,39; 1) 0,28–1,01; alpha
lấy từ texture (~0,7 giữa mặt, ~1 ở mép). Vệt loang: `Splash.png`, màu của material `Splash 1` (0,89; 0,97; 1).

**Sửa:**
- `SandIce.shader`: viết lại theo công thức trên, blend `One OneMinusSrcAlpha`, mặt trên nhạt gần trắng, bỏ dịch UV
  (UV mesh 0..1). Vẫn giữ `_Flash` / `_Opacity`.
- `SandJamSceneController.Dynamic.AddIceSplash`: SpriteRenderer nằm trên nền dưới khối băng (xoay ngẫu nhiên, sprite và
  material dùng chung → batch được); `CharacterFreezeView.Splash` ẩn ngay khi băng vỡ (lúc đó có loé sáng + bông tuyết, khối
  chạy đi ngay).
- Local (gitignore): `Resources/Mechanics/Splash.png`.

**Kiểm tra:** chụp khối băng đứng yên, quay cảnh số giảm / băng vỡ; luồng booster / thua; tự chơi màn băng / khoá.

## 27/10/2026 — Ổ khoá giữ số 0 một nhịp rồi mới bung

**So:** UnlockerGif gốc (khung 0,14 s): khung 21 số 1, khung 22 số 0 nhưng ổ khoá vẫn đóng, khung 23 mới bong bóng + vụn +
ổ khoá trắng. Bản mình bung ngay lúc số về 0 (phát cát cuối còn đang bay), và phát cuối không làm ổ khoá nảy.

**Sửa:** `PadlockView`: `Hold` 0,12 s trước khi loé / mở / thu nhỏ; phát cuối cũng nảy (chạy tiếp trong lúc giữ, tách
`Bounce`); `Burst` báo đã qua lúc giữ. `SceneRegionView.IsSettled` (chế độ khoá) chờ `Burst` nên bong bóng của `SandFx`
nổ cùng lúc với ổ khoá. Logic mở vùng (`SettledCount`) không đổi.

**Kiểm tra:** quay cảnh mở khoá; luồng booster / thua; tự chơi các màn khoá.

## 27/10/2026 — Băng loé lan từ đầu hàng xuống cuối hàng

**So:** GIF Freeze gốc khung 9, 15, 23 (ngay sau mỗi lần chạm): khối băng cuối hàng còn trắng sáng nhất, khối đầu hàng đã
loé xong — loé chạy dọc hàng, không đồng loạt. Bản mình loé tất cả cùng lúc.

**Sửa:** `CharacterFreezeView.Front` (= `LaneStarts` của hàng, gán trong `AddFreezeShell`): mỗi khối chờ
`FlashStagger` 0,07 s cho mỗi đơn vị cách đầu hàng (tối đa 0,3 s) rồi mới loé; băng vẫn tan ngay.

**Kiểm tra:** quay Dance_12 (12 khối băng): hàng đầu loé 0 s, hàng giữa ~0,07 s, hàng cuối ~0,13 s; luồng booster / thua;
tự chơi các màn băng.

## 27/10/2026 — Ổn định cho mobile: cấu hình build, nướng mesh sẵn, bớt việc lặp mỗi khung

**Đo (editor, Dance_12 — 12 khối băng, autoplay; profiler self time):** script game sinh ~110 B rác/khung (8 KB/khung
trong số tổng là của editor + công cụ đo), không khung nào > 2 KB; GC incremental đã bật; không có đèn → không có lượt
shadow; 70–90 draw call. Tốn nhất: `SandJamSceneController.Update` 677 µs, `OutlinedText` 268, `CharacterDepthStyle` 211,
`SandFx` 141, `CharacterFreezeView` 124. Đo trực tiếp `Advance`: ~200 µs, trong đó `Synchronize` ~51 µs × 2 lần/khung
(gần hết là đồng bộ hàng chờ), mô phỏng cát (Burst) ~9 µs, tick 3 µs.

**Sửa:**
- Cấu hình build (qua API editor): Android IL2CPP + ARM64 (trước Mono, chỉ ARMv7 32-bit — chậm hơn và Google Play bắt buộc
  64-bit), iOS IL2CPP, tắt accelerometer (đang đọc 60 lần/s dù game không dùng).
- `BlendShapeCache.Prewarm`: nướng sẵn 20 bước của mesh băng / ổ khoá lúc dựng màn, không nướng giữa lúc chơi.
- `CharacterFreezeView`: chỉ gửi property block khi độ mờ / loé đổi, `SetActive` chỉ khi đổi, thoát sớm khi băng đã tan;
  số trên băng gán qua `OutlinedText.SetText`. 124 → 27 µs/khung.
- `RendererExtensions.SetShown` (helper dùng chung: `SetActive` chỉ khi đổi; dùng thêm ở `CongratsPopup`).
- `OutlinedText.SetText` bỏ qua khi chữ không đổi (so với chuỗi đã lưu, không đọc `TextMesh.text`); số trên ổ khoá và các nhãn
  HUD (`CampaignUi`: level, xu, mạng, JOIN) gán qua đó → 30/33 nhãn không còn bị dò định kỳ (trước 16/33).
- `SandJamSceneController.Advance`: lần `Synchronize(0)` sau tick chỉ chạy khi `Game.Revision` đổi (~40 µs/khung lúc không
  bắn).

**Kiểm tra:** luồng booster / thua; tự chơi level 2, 31, 68, 222 và 4 màn băng / khoá; đo lại nhịp đổ (level 68).

## 27/10/2026 — Mobile: nén texture ASTC, giới hạn độ phân giải render, mesh nướng sẵn không giữ bản CPU

**Đo:** texture không có thiết lập riêng cho Android / iOS và mặc định `textureCompression: 0` → build mobile để ảnh không
nén (RGBA32 / RGB24: logo 2000×1500, nền 2048×2048, nền popup ~900×1200). Ước tính toàn bộ 236 texture thường: ~301 MB
không nén. Mesh nướng sẵn của ổ khoá / băng: 40 mesh × 280–500 KB (~15 MB), vẫn giữ bản sao CPU. Âm thanh: 8 SFX ngắn
(< 90 KB, giải nén khi tải — đúng cho SFX), không sửa.

**Sửa:**
- `Editor/Tools/MobileTextureImport` (AssetPostprocessor, áp cho mọi texture trong `Assets/SandJam`): Android + iOS dùng
  ASTC 4×4 (giữ gradient UI sạch, ~1/4 dung lượng) → ước tính ~75 MB. Bỏ qua ảnh pixel-art (lọc Point: bìa gallery, ảnh level)
  và texture Read/Write; không đè thiết lập đã đặt tay. Bản PC không đổi. Các `.meta` texture của dự án được import lại có
  thêm phần Android / iPhone; `.meta` trong Resources (gitignore) được tạo lại khi import trên máy.
- `BlendShapeCache.Bake`: `UploadMeshData(true)` — mesh chỉ để vẽ, bỏ bản CPU (giảm ~một nửa).
- `Core/Rendering/MobileDisplay.Configure` (thay dòng `targetFrameRate` trong `SandJamSceneController.Awake`): 60 fps; trên
  điện thoại cạnh ngắn render tối đa 1080 px (giữ tỉ lệ) — màn 1440×3200 bớt ~44% pixel phải vẽ; camera / UI tự căn lại khi
  kích thước màn đổi.

**Kiểm tra:** import lại logo → Android / iOS ASTC_4x4, Standalone giữ nguyên; quay băng tan (mesh nướng sẵn vẫn vẽ đúng);
luồng booster / thua; tự chơi level 2, 68 và 3 màn băng / khoá. Giới hạn độ phân giải chỉ chạy trên điện thoại (chưa thử máy thật).

## 27/10/2026 — Mobile: tắt mô phỏng vật lý thừa, tắt camera nền khi không có viền

**Đo:** lúc chơi có ~57 triệu px texture trong bộ nhớ (~55 MB sau ASTC), ~38 triệu px thuộc popup chưa mở (Shop, SandQuest,
Gallery, Congrats, Tasks, Lives…). Giữ dựng sẵn: dựng khi mở sẽ khựng lần đầu mở popup. 3 camera / khung: camera nền
letterbox (mask 0) chỉ xoá màn hình trong khi Main Camera cũng tự xoá. Không có rigidbody nào, collider chỉ để dò chạm,
nhưng vật lý 3D + 2D vẫn mô phỏng 50 lần/s và đồng bộ collider của mọi khối đang chạy.

**Sửa:**
- `OriginalLookStyler.LateUpdate`: camera nền chỉ bật khi Main Camera chừa viền (`rect` không phủ kín) — trên điện thoại
  (1170×2532) tắt, bớt một lượt xoá toàn màn hình mỗi khung.
- Vật lý 3D + 2D: `Simulation Mode = Script` (Project Settings). `Views/UI/ScreenTap` (helper dùng chung): `SyncTransforms` rồi
  raycast một lần mỗi lần chạm; 3 chỗ dò chạm (khối, booster chọn khối, nút UI) dùng nó thay vì lặp code.

**Kiểm tra:** camera nền tắt ở 1170×2532, ảnh không đổi; thử chạm: khối vừa dịch lên — không đồng bộ thì trượt, qua
`ScreenTap` (có đồng bộ) thì trúng; luồng booster / thua; tự chơi level 2, 31, 68 và 2 màn băng / khoá.

## 27/10/2026 — Mobile: shader khối không còn `clip`

**Đo:** `SandToonDepth` (shader đục của khối, ray, đế, ổ khoá…) có `clip` trong cả hai pass cho hiệu ứng "fill" — nhưng
`_FillEnabled` không bao giờ bật (chỉ `CharacterDepthStyle` gán 0, mọi material cũng 0). Trên GPU tile (iPhone, đa số
Android) chỉ cần shader đục có `clip` là mất loại bỏ pixel bị che (HSR / early-Z) cho mọi vật vẽ bằng nó.

**Sửa:** bỏ toàn bộ phần fill (thuộc tính, nội suy độ cao, các `clip`) khỏi `SandToonDepth`; bỏ dòng gán `_FillEnabled` /
`_FillAmount` trong `CharacterDepthStyle`. Hình không đổi.

**Kiểm tra:** so ảnh Dance_12 trước / sau — giống hệt (chỉ khác vệt băng loang xoay ngẫu nhiên); luồng booster / thua; tự chơi.

## 27/10/2026 — Mobile: vào màn lần đầu bớt khựng (so sánh chuỗi Ordinal)

**Đo:** `Resources` ~106 MB (Levels 37 MB = 556 file JSON, chỉ nạp khi chơi tới; không có `LoadAll`). Vào màn lần đầu:
`VideoScreen.Play` 62 ms, khung đầu sau đó 145 ms — `SceneActorView.LateUpdate` 54 ms: `FindRig` duyệt 55 transform mỗi khối
với `EndsWith("Hips")` kiểu so theo culture (~2 ms/khối × 26). Còn lại: JIT của Mono (~20 ms, IL2CPP không có), editor,
dựng phần bình cát cho từng khối (~0,3 ms/khối, mesh đã dùng chung).

**Sửa:** mọi `StartsWith` / `EndsWith` với chuỗi cố định trong code runtime (34 chỗ, 17 file) dùng
`StringComparison.Ordinal` — kết quả giống hệt (tên, action ASCII), nhanh hơn nhiều và không lệ thuộc ngôn ngữ máy (ví dụ máy
cài tiếng Thổ Nhĩ Kỳ). `FindRig` 2,05 → 0,10 ms/khối; khung đầu khi vào màn 145 → 86 ms trong editor (phần game ~15 ms).

**Kiểm tra:** luồng booster / thua (đi qua action nút); tự chơi level 2, 31, 68 và 2 màn băng / khoá.

## 27/10/2026 — Mobile: nền gradient vẽ đục, bớt blend toàn màn hình

**Đo:** rà các hàm chạy mỗi khung: `TextOutliner` quét UI mỗi 0,4 s chỉ tốn 0,08 ms (27 nhãn) — không sửa;
`CubeContactShadow` đã cache; `SuperbFx` gọi `GetComponent<SpriteRenderer>()` mỗi khung khi đang ăn mừng. Ước lượng overdraw
(khung bao renderer chiếu lên màn hình): lớp đục ~1 màn, lớp trong suốt ~2,1 màn — trong đó "Backdrop gradient" phủ kín 1 màn
bằng `Sprites/Default` (blend toàn màn hình mỗi khung dù nền vốn đục).

**Sửa:**
- `Resources/BackdropGradient.shader`: màu đỉnh, đục, hàng đợi `Background`, không ghi depth; `OriginalLookStyler.BuildGradient`
  dùng nó. Màu nền giống hệt từng pixel. Lớp trong suốt còn ~1,3 màn (tấm nền bảng + bảng cát cần alpha).
- `SuperbFx`: giữ sẵn `SpriteRenderer` của glow thay vì `GetComponent` mỗi khung.
- `SandToonDepthPlain`: bỏ các thuộc tính fill còn sót.

**Kiểm tra:** so ảnh Dance_12 — nền trùng từng pixel; luồng booster / thua; tự chơi level 2, 222 và 2 màn băng / khoá.

## 27/10/2026 — Dọn repo: gỡ công cụ ngoài, tài liệu về nội dung trích từ game gốc và log test

**Rà `git ls-files` (1.304 file):** 736 file là package nhúng `com.coplaydev.unity-mcp` (cầu nối MCP dùng khi phát triển,
của bên thứ ba, không thuộc game — `manifest.json` không khai báo); trong `Documentation` có các chỉ mục / ghi chú về nội dung
trích từ game gốc: danh sách 1.120 script dịch ngược, bản đồ 5.413 asset đã trích, danh sách 556 level gốc, hash các file đã
chép, bảng màu gốc, kiến trúc lớp gốc; cùng ~40 file log test thô (`checks.txt`, `benchmark.txt`…).

**Sửa:** gỡ khỏi git 782 file trên (vẫn giữ nguyên trên máy) và thêm vào `.gitignore`; 4 dòng tài liệu nhắc tới các file này
ghi chú "chỉ lưu trên máy". Giữ: scripts, shader, font, scene, prefab, `Generated` (scene tham chiếu), level tự thiết kế,
`ProjectSettings`, `Packages/manifest.json`, tài liệu thiết kế / nhật ký.

**Lưu ý:** các file này vẫn còn trong lịch sử các commit cũ; xoá khỏi lịch sử cần viết lại lịch sử và force-push (chưa làm).

## 28/10/2026 — Chốt bản: khoá màn dọc, rà cảnh báo / code chết

**Rà:** biên dịch lại cả `Assembly-CSharp` và `Assembly-CSharp-Editor` bằng Roslyn của Unity với đúng file `.rsp` của dự án —
0 cảnh báo, 0 lỗi. Quét type / member / static không dùng và code trùng: chỉ còn báo nhầm (menu item, callback Unity, lớp
extension). Cấu hình build: 1 scene (`SandJamGame`); hướng màn hình đang tự xoay, cho cả ngang.

**Sửa:** khoá màn dọc (`defaultScreenOrientation` Portrait, tắt xoay ngược / ngang) — game thiết kế dọc, xoay ngang thì bố cục bị
ép vào giữa với hai dải viền.

**Còn để bạn quyết:** tên app (`productName` đang là `sandjamproject`) và bundle ID (chưa đặt → Unity dùng
`com.GameplayLab.sandjamproject`).

## 28/10/2026 — Khối đổ cát không còn giật khi xoay

**Đo (level 68, bước cố định 0,02 s, góc `aimYaw` mỗi bước):** khối đổ cát xoay 30° về phía ngược vùng nhận, chiều xoay theo
điểm đích bên trái / phải. Hai cú giật: (1) lúc bắt đầu đổ, khối xoay theo điểm đích cũ còn sót từ lần dùng trước (+24°) rồi
phát đầu tiên đổi đích → quay ngoắt về −30° (ô 1, 5,22–5,48 s); (2) đổi sang vùng ở phía bên kia → quay 60° đều 300°/s trong
0,2 s (ô 3, 10,14–10,34 s). Bản thân khối không rung (vị trí, góc đứng yên khi chụp từng bước).

**Sửa (`SceneActorView`):** chiều xoay chỉ lấy từ các phát của lần đổ này (`aimed`, xoá khi chạy / ngừng bắn); chỉ đổi chiều khi
đích lệch quá `SideDeadZone` 0,3 sang phía kia; xoay bằng `SmoothDamp` (0,09 s) thay cho tốc độ đều.

**Kiểm tra:** đo lại — không còn lần đảo chiều nào ở cả 4 ô, đổi vùng tối đa 10°/bước có tăng / giảm tốc; luồng booster / thua;
tự chơi level 2, 68, 250, 361 và 2 màn băng / khoá.

## 28/10/2026 — Dòng cát bắn lên chảy đều, không còn chớp giật

**Đo (level 68, bước 1/60 s, đếm viên đang bay):** khoảng cách giữa các viên xen kẽ 0,12 / 0,14 s (mốc bắn lấy theo tick 0,02 s
thay vì nhịp 0,115 s); viên nhắm lại miệng rót mỗi khung, mà miệng rót chạy theo cột cát → viên bị kéo ngang giữa đường; bay 0,08 s
đều tốc trong nhịp 0,115 s → 26/96 khung đang đổ không có viên nào (chớp tắt), mỗi khung nhảy ~1 đơn vị.

**Sửa (`ProjectileManager`):** nhịp riêng cộng dồn đúng 0,115 s (lần đổ mới bắt đầu nhịp mới); tuổi viên tính từ nhịp (bù vài ms
trễ của tick); giữ miệng rót lúc bắn (`To`); bay 0,13 s giảm tốc vào miệng rót (ease-out) — viên sau đã ra trước khi viên trước tới.
Kèm: `LevelReplaySolver` (editor) tick `DrainShare` 0,05 — tick 0,01 làm solver chậm ~5 lần, level 250 quá 60 s không giải xong.

**Kiểm tra:** đo lại — khoảng cách đều 0,115 s, khung trống 26 → 3 /96 (chỉ lúc khối vừa tới ô); ảnh từng bước: viên trên đường
bay ở mọi khung; tự chơi level 2, 68, 250 (musicians_10) và 2 màn băng / khoá — chạy bằng script test mới chờ game sẵn sàng và đúng màn (script cũ ngủ cố định nên đôi khi nạp nhầm màn).

## 28/10/2026 — Khối đang đổ không còn nhảy ngang (Animator ghi đè vị trí sau Update)

**Đo theo thời gian thực** (ép vòng lặp game chạy mỗi khung editor bằng `QueuePlayerLoopUpdate`, tự chơi ở tốc độ thường, ghi
vị trí / góc phần thân `pose` của mọi khối đang đổ mỗi khung): khối giữ góc 30° nhưng cứ ~0,12–0,16 s lại có một khung `pose` về
vị trí chưa xoay (x −1,3460 → −1,4650 → −1,3460), tức nhảy ngang ~0,12 đơn vị rồi về — đúng cái "giật giật". Các lần đo trước gọi
`Advance` theo bước cố định, không chạy Animator, nên không thấy.

**Nguyên nhân:** `pose` là object mang Animator; clip gốc có khoá vị trí của nó. Thứ tự trong khung: `Update` (`ApplyYaw` xoay
`pose` quanh tâm khối) → Animator → `LateUpdate`. Khung nào Animator đánh giá lại clip thì vị trí bị đặt về gốc, góc giữ nguyên.

**Sửa (`SceneActorView.LateUpdate`):** khi khối đang xoay (`yaw` / `aimYaw` / `roll` / `Rock` ≠ 0) áp lại `ApplyYaw` sau Animator.

**Kiểm tra:** đo lại thời gian thực — 0 lần nhảy (còn 3 bước > 0,03 là lúc khối quay mềm sang vùng phía bên kia, khung dài 30 ms);
tự chơi level 2, 68, 250 (musicians_10) và 2 màn băng / khoá — chạy bằng script test mới chờ game sẵn sàng và đúng màn (script cũ ngủ cố định nên đôi khi nạp nhầm màn).
