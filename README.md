# Sand Jam (clone) — sandjamproject

Unity 2022.3.62f3 · Built-in render pipeline · Burst. Main scene: `Assets/SandJam/Scenes/SandJamGame.unity`.

## Nội dung không có trong repo

Repo chỉ chứa code, scene, editor tool, shader và tài liệu tự viết. Mọi thứ lấy từ game gốc Sand Jam (Voodoo)
đều để lại trên máy và không đưa lên: `SourceData/` (code dịch ngược), `OriginalReference/`,
`Assets/RecoveredOriginal/`, các thư mục con trong `Assets/SandJam/Resources/` (sprite, âm thanh, level,
cấu hình), mesh/ảnh xem trước sinh từ level gốc và ảnh chụp màn hình. Vì vậy project tải từ repo sẽ thiếu
asset và không chạy ngay được.

## Thư mục

```
Assets/
├─ SandJam/                       Toàn bộ game
│  ├─ Scenes/                     SandJamGame (chính, duy nhất trong Build Settings) + các scene test
│  ├─ Scripts/
│  │  ├─ Runtime/
│  │  │  ├─ Core/                 Logic thuần, không phụ thuộc scene
│  │  │  │  ├─ Domain/            Luật chơi: SandGame (+Boosters, Freeze), ChainPairing, thực thể
│  │  │  │  ├─ Data/              LevelData, LevelCatalog, ColorPalette, OriginalConfig, SpriteSequence
│  │  │  │  └─ Sand/              Mô phỏng cát rơi (Burst Job)
│  │  │  ├─ Managers/             Campaign, SaveManager, EconomyManager, AudioManager, GameEvents,
│  │  │  │                        GameServices, LevelBootstrap, LevelManager, LevelDataManager…
│  │  │  ├─ Controllers/          Gameplay (SandJamSceneController), Characters (hàng chờ), UI (màn hình, popup, booster)
│  │  │  └─ Views/                Board (vùng màu), Characters (khối, chân, hộp kính), Fx (hiệu ứng), UI
│  │  ├─ Development/             Smoke test chạy bằng tham số dòng lệnh, AutoPlayer
│  │  └─ Legacy/                  Prototype 2D cũ (SandJamDemo) — không dùng trong game
│  ├─ Editor/
│  │  ├─ Tools/                   Game Manager, CampaignBuilder, OriginalDataImporter, ViewCapture
│  │  ├─ SceneBuilders/           Dựng prefab/scene mẫu từ code (Level001 là template)
│  │  └─ Validation/              Kiểm tra cát, Burst, shader, booster, freeze, chain…
│  ├─ Resources/                  Dữ liệu load lúc chạy (đường dẫn Resources không đổi)
│  │  ├─ Levels/                  Data/*.json (556 level gốc), LevelOrder.json, Campaign.json
│  │  ├─ OriginalConfig/          colors / intros / difficulty (sinh từ dữ liệu gốc)
│  │  ├─ OriginalGifs/            Ảnh động hướng dẫn (SpriteSequence)
│  │  ├─ Original/                Âm thanh gốc
│  │  ├─ LevelPack/Prefabs/       Level001 = template cảnh chơi
│  │  └─ VideoUI, ReferenceUI…    Sprite UI
│  └─ Generated/                  Mesh/material sinh bởi SceneBuilders
└─ RecoveredOriginal/             Asset gốc lấy từ game (texture, sprite, mesh, audio, font…)

OriginalReference/                (ngoài Assets) cấu hình/prefab/scene gốc để tra cứu, Unity không import
Builds/                           Bản build (ngoài Assets)
TestResults/                      Ảnh và báo cáo kiểm tra
```

## Luồng chạy

`LevelBootstrap` → `GameServices.Ensure()` (Save + Audio + thống kê) → nạp level hiện tại của `Campaign`
(bỏ qua level lỗi) → prefab template `Level001` → `SandJamSceneController` dựng vùng/nhân vật từ JSON.
Màn hình: Loading → Home → Gameplay → Superb! → Level Complete → Next (tải lại scene, vào thẳng màn sau).

## Manager

| | |
|---|---|
| `GameEvents` | Sự kiện chung: LevelStarted/Won/Lost, RegionCompleted, CharacterSelected/Emptied, BoosterUsed, CoinsChanged, ButtonClicked |
| `SaveManager` | `persistentDataPath/sandjam_save.json` (+ `.bak`), có version, tự chuyển dữ liệu PlayerPrefs cũ |
| `EconomyManager` | Xu, kho booster, giá booster 150/500/700, thưởng thắng 10 |
| `AudioManager` | Âm thanh gốc theo sự kiện, tôn trọng cài đặt âm thanh |
| `GameServices` | Gốc DontDestroyOnLoad, thống kê chơi |
| `LivesManager` | 5 tim, bỏ cuộc mất 1 tim, hồi 1 tim / 30 phút (tính cả lúc tắt game) |
| `Collections` | Bộ sưu tập theo tên level (Accessories, Pop Art…), % hoàn thành |

## Popup (PopupScreens, dựng từ art popup gốc)

Hết chỗ (Chơi tiếp 400 xu → thêm ô chờ thứ 6 / ✕ bỏ cuộc) → Thua rồi (tim, đồng hồ hồi tim, Chơi lại) ·
Hết tim · Chúc mừng (ảnh bức tranh vừa tô + % bộ sưu tập) · Tính năng / Booster mới (tặng 1 booster) · Cài đặt.
**Thứ tự màn** = đúng 915 màn của `Default_LevelOrderConfig` gốc (màn N ở đây = màn N trong game thật).
**Theme sân chơi**: nhóm = màn / 15, xoay vòng lavender → tối → xanh (`OriginalThemes`).
**Màn Bonus** (danh sách "easy" của EasyHardLevelConfig: 25, 35, 55…): popup BONUS LEVEL xanh lá, HUD xanh + xu, thưởng +30.
**Cài đặt**: dựng lại theo UiSettingsPanel gốc (Rung, Âm thanh, Chơi lại, Trang chủ) — `PopupScreens.Settings.cs`.
**SAND SHOP** (nút SHOP ở Home, `PopupScreens.Shop.cs`): nội dung theo RC_Shop/reward_bundle gốc; mua booster bằng xu
chạy thật, gói tiền thật chỉ hiển thị (không có thanh toán).
**NEW ART STYLE** khi gặp bộ tranh mới (ảnh render gốc trong `Resources/LevelRenders`).
**CONGRATS** = thẻ Gallery (`GalleryCollections`, theo FeatureConfig gốc): ảnh bìa đen trắng được tô màu dần, mỗi màn thắng thêm 1 phần.
**HẾT CHỖ!** dải đỏ ~1 giây trước popup Hết chỗ. **Hết tim**: nút Hồi tim 600 xu. Màn khó thưởng +20.
**Sand Quest** (`SandQuest` + `PopupScreens.SandQuest.cs`): từ màn 30, bấm rương JOIN ở Home; thắng 10 màn liên tiếp trong 24 giờ,
số người chơi giảm theo `botProgressionList` gốc, về đích chia giải 1000 xu; thua 1 màn hoặc hết giờ là trượt.
**Home**: nút QUÀ (quà 7 ngày, `DailyRewards`), NGÀY / TUẦN (nhiệm vụ, `TaskBoard`, dữ liệu theo
Default_RemoteDailyQuestConfig gốc: mỗi kỳ chọn 1 nhiệm vụ mỗi bậc, hoàn thành nhận sao, đủ mốc sao mở rương),
GALLERY (bộ sưu tập: ảnh bìa tô dần, 3 mốc thưởng, lưới thẻ tranh), NO MORE LIVES khi bấm PLAY lúc hết tim.
Giao diện: `PopupScreens.Tasks.cs`, `PopupScreens.Gallery.cs`, `PopupScreens.Lives.cs`.
**Màn khó** (`OriginalConfig.IsHard`: 20, 30, 40…): popup HARD LEVEL (art gốc trong `Resources/HardLevel`) trước khi chơi,
badge HUD đỏ có đầu lâu, nền tối (`OriginalLookStyler.Hard*`).

## Công cụ (menu **Sand Jam**)

- **Game Manager** — xem/sửa save, nhảy màn, cộng xu/booster, reset.
- **Levels → Build campaign from original order** — lọc level chạy được và giải được → `Campaign.json`.
- **Original data → Import configs and tutorial gifs** — sinh `OriginalConfig` và `OriginalGifs`.

## Cơ chế (suy ra từ ảnh động gốc + dữ liệu level, kiểm chứng bằng solver)

| Cơ chế | Luật | Level thật giải được |
|---|---|---|
| Khối nối (IsChain) | Cặp nối lên ô chờ cùng lúc, cần 2 ô liền kề; dọc = cùng hàng, ngang = hai hàng cạnh nhau | có sẵn |
| Nửa khối (IsHalf) | Hình nêm; lên ô chờ thì ngủ (zZ), không bắn; 2 nửa cùng màu trong ô chờ ghép thành 1 khối đầy | 19/21 |
| Đóng băng (IsFreeze) | Số trên băng giảm 1 mỗi khối khác được đưa lên ô chờ (cặp nối: 2); về 0 thì vỡ | 17/19 |
| Ổ khóa (IsUnlocker / IsUnlockerPart → IsUnlockedPart) | Vùng sọc + ổ khóa; chỉ khối có chìa khóa cùng màu bắn vào ổ khóa; về 0 thì vùng bị khóa mở cho khối thường | 20/20 |

Scene test: `Assets/SandJam/Scenes/Tests/MechanicTest_ChainHalf.unity` và `MechanicTest_FreezeLock.unity`
(menu **Sand Jam → Test scenes → Create mechanic test scenes** để tạo lại). Bảng điều khiển: đổi màn, chơi lại,
**Tự giải** (chạy theo lời giải của solver), trạng thái cơ chế. Không ảnh hưởng save/campaign.

## Lưu ý

- Muốn reset tiến độ: Game Manager → *Reset save* (không còn dùng PlayerPrefs).
- Đường dẫn trong code Editor dùng `Assets/SandJam/...`; đường dẫn `Resources.Load` không có tiền tố thư mục.
- `LevelPackSmoke` là smoke test của luồng 3 level cũ, đã lỗi thời.
