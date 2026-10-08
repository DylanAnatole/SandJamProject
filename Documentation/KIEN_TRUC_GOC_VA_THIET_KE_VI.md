# Kiến trúc game gốc Sand Jam và thiết kế cho bản clone

Nguồn: code dịch ngược trong `D:\Unity\testsand\Assets\Scripts\Assembly-CSharp` (bản sao: `sandjamproject/SourceData/AssetRipper`).
Thân hàm đã mất, nhưng **tên lớp, trường, chữ ký hàm, enum và quan hệ giữa các lớp còn nguyên** — đủ để đọc thiết kế.
Code gameplay gốc: 248 file ở thư mục gốc + 80 file trong `__PROJECT/Dev/Scripts`. Phần còn lại (~800 file) là SDK
(Voodoo Sauce 609, ConsentManagement 78, AlmostEngine 52, PaperPlaneTools, EpicToonFX…) — không cần cho bản clone.

## 1. Kiến trúc gốc theo tầng

```
Khởi động     VoodooLoaderScene → SplashScreen (Canvas_FakeLoading) → MainMenu_Basic → Game
Điều phối     GameManager (State, CanClickState) · Scene_Manager / GenericSceneManager · InteractManager
Level         LevelManager (index, adjust events) · LevelController (Win, ShootingCharacters, UiDivider)
              RemoteLevelOrderConfig (levelOrder / levelOrder2 A/B) · LevelPoolHolder (pool ngẫu nhiên)
Bảng tranh    PaintableGrid (CompressedLevel → PaintablePart + PaintableGridSlot) · PaintableController
              PaintablePart.ExecutePaint / PlaceSand / Prerequests / UnlockPartRef / LockImage
Hàng khối     LaneController (LaneData, 3 lane, khoảng cách 1.2) · Lane (IterateLane, ManuelClickSwapLane) · LaneSlot
Nhân vật      Character (IsChain/IsSecret/IsFreeze/IsHalf/IsUnlocker/IsReservable, ChainCharacter, FreezeCount)
              CharacterVisual · Character3D_View · CharacterAnimator · CharacterFreeze · Bullet (IThrowable)
Chọn & đi     SelectionController (CheckStashAndGo, MoveToStash, MoveToReserve, SelectAny, Fail)
Ô chờ         StashController (Stash: List<ISlot>, SlotPrice, extra slot, GetHalfFullSlot, expand preview)
              GridSlot (UnlockLevel, UnlockPrice, NeedAmmo/NeedAmmoCount, IsNeedOtherHalf, HalfFinalPos)
              GridNeedAmmo · CharacterReserveHandler (khu "dự trữ") · ReserveButton3D · UiButtonAddSpace
Sự kiện       EventManager.GetEvent<T>() + GuruEvent: OnCharacterMoved, OnCharacterMoveToStash, OnCharacterShoot,
              OnPainted, OnCoinChanged, OnGameStateChanged, OnIsSoundOnChanged, OnLevelWinEffect, OnFeatureDoneLevel
Lưu           SaveManager + ISaveSystem/JsonSaveSystem + SaveState · Saver · GalleryPrefs
Kinh tế       EconomyConfig, ReserveConfig, ExtraBoosterConfig, CooldownRvConfig, DoubleRvConfig (Remote Config Voodoo)
Meta          HealthSystem/HealthManager (tim) · ShopManager + Reward_* · GalleryManager/GalleryConfig
              DailyRewardsController · DailyTaskController/DailyQuestConfig (ngày/tuần) · SandQuestController (sự kiện đua bot)
UI            UiCanvasController, UiCanvas (Main/Level/Settings/Tutorial), UiPanel, UiSlot, UIBoosterController,
              UiNewFeature/NewBooster managers, FailUICanvas, SuccessUICanvas, UiEasyHardLevelManager, Tutorial*
Âm thanh      AudioManager · MusicManager · SFXManager · VibrationManager
```

Giao diện (interface) cốt lõi: `ISlot` (chỗ chứa: LaneSlot, GridSlot), `ISlotable` (vật đặt vào: Character, Pipe, Crete),
`ISelectable`, `IColorable`, `IPaintable`, `IThrower/IThrowable/IThrowerUser` (bắn đạn cát).

## 2. Những điều code gốc cho biết (mới)

| Phát hiện | Bằng chứng | Ý nghĩa cho bản clone |
|---|---|---|
| Có màn **Boss** và **Bonus** với thưởng riêng | `EconomyConfig.rewardBoss/rewardBonus`, `SaveState.IsLastLevelBoss` | Làm BONUS LEVEL (video 2) + màn Boss (khả năng là EasyHard "hard") |
| Tim bị trừ **khi bắt đầu màn**, hoàn lại khi thắng; thoát giữa màn cũng mất | `HealthSystem.HandleLevelStart/HandleWin/HandleOutOfFocus` | Đổi LivesManager theo đúng luật gốc |
| Có giá **hồi đầy tim** | `EconomyConfig.priceRefillHealth`, `Placement.NO_LIVES_POPUP(_FREE)` | Thêm nút "Hồi tim" (xu / miễn phí 1 lần) |
| "Play on" có biến thể **thêm 2 ô** và **cuối màn** | `Placement.PLAY_ON_2SPACES`, `PLAY_ON_ENDGAME`, `priceExtraSlotEGP` | Hiện đã có thêm 1 ô; mở rộng 2 ô |
| Booster thứ 4 **QuadLaneSlot** | `BoosterType.QuadLaneSlot`, `SaveState.BoosterQuadLaneSlotCount` | Chưa có (có thể là thêm 1 ô vào hàng / lane thứ 4) |
| Khu **dự trữ (Reserve)** mua bằng xu, 3 mức giá | `CharacterReserveHandler`, `ReserveConfig.priceReserve0..2`, `Character.IsReservable` | Chưa có |
| Ô chờ mở bằng **cấp độ / xu** ngoài mở bằng cát | `GridSlot.UnlockLevel/UnlockPrice` | Có mở bằng cát; thiếu mở bằng xu |
| Nửa khối **ghép vào chung một ô, cộng cát** | `StashController.GetHalfFullSlot`, `SetInsideSlotable(…, isCombineAmmo)`, `GridSlot.HalfFinalPos` | Đã làm đúng |
| Sau khi hết danh sách màn: **màn ngẫu nhiên** | `SaveState.IsRandomLevelOn`, `LevelPoolHolder` | Hiện lặp lại danh sách; có thể đổi sang ngẫu nhiên |
| Thứ tự màn **A/B** | `RemoteLevelOrderConfig.levelOrder2/isLevelOrder2`, file `_TEST_AB` | Tùy chọn |
| Gallery: thẻ + mốc thưởng theo bộ | `GalleryManager.SetCardClaimed/SetRewardClaimed`, `View_Gallery_Milestone` | Có % bộ; thiếu màn Gallery + thưởng mốc |
| Nhiệm vụ ngày/tuần, quà hằng ngày, sự kiện Sand Quest đua bot | `DailyTaskController`, `DailyQuestType`, `DailyRewardsController`, `SandQuestController/BotController` | Chưa có |
| Shop: xu, booster, bundle, no-ads, premium, tim vô hạn | `Reward_Coin/Booster/Bundle/Premium/InfHealth/Health/Gift` | Chưa có (không IAP thật) |
| Cơ chế cũ không dùng | `Pipe`, `Crete`, `GridAStar`, `MoveToGridExit` (chế độ lưới) — không level nào dùng | Bỏ qua |

### Bản đồ tính năng từ `Placement` (mọi điểm phát sinh xu/thưởng)
LEVEL_WIN, LEVEL_FAIL, MULTIPLY_REWARD, PLAY_ON, PLAY_ON_2SPACES, PLAY_ON_ENDGAME, EXTRA_SLOT, GRID_SLOT,
BOOSTER_* (mua/dùng FunFlare, Swap, SelectAll, QuadLaneSlot, Reserve, bundle, initial, extra), SHOP_PURCHASE/REWARD,
IAP_BUNDLE, RESTORE, GALLERY(_DOUBLE), NO_LIVES_POPUP(_FREE), SANDQUEST_JOIN/REWARD, DAILY_TASK(_DOUBLE),
WEEKLY_TASK(_DOUBLE), DAILY_REWARD(_DOUBLE).

## 3. Đối chiếu gốc → bản clone

| Hệ thống | Lớp gốc | Bản clone (`Assets/SandJam/Scripts`) | Trạng thái |
|---|---|---|---|
| Luật chơi | LevelController, SelectionController, StashController, Character flags | `Core/Domain/SandGame*.cs` (thuần, không phụ thuộc scene) | Đủ 7 cơ chế |
| Bảng tranh | PaintableGrid/Part/GridSlot | `Views/Board/SceneRegionView`, `SandBoardTextureView`, `Core/Sand` (Burst) | Đủ (+ ổ khóa sọc) |
| Hàng/nhân vật | LaneController, Lane, Character* | `Controllers/Characters/CharacterQueueController`, `Views/Characters/*` | Đủ |
| Sự kiện | EventManager + GuruEvent | `Managers/GameEvents` (static event) | Đủ cho nhu cầu hiện tại |
| Lưu | SaveManager/SaveState/JsonSaveSystem | `Managers/SaveManager` (JSON + .bak + version) | Đủ |
| Kinh tế | EconomyConfig/ReserveConfig (remote) | `Managers/EconomyManager` (hằng số) | Thiếu config ngoài |
| Tim | HealthSystem | `Managers/LivesManager` | Khác luật trừ tim |
| Booster | UIBoosterController, BoosterType | `Controllers/UI/BoosterController` | Thiếu QuadLaneSlot |
| Tính năng mới | UiNewFeature/NewBooster managers | `PopupScreens` + `OriginalConfig` | Đủ |
| Gallery | GalleryManager + Canvas_Gallery | `Managers/Collections` + thẻ Chúc mừng | Thiếu màn Gallery |
| Shop, nhiệm vụ, quà, Sand Quest, Reserve | nhiều lớp | — | Chưa có |
| Âm thanh | Audio/Music/SFX/Vibration | `Managers/AudioManager` | Thiếu nhạc nền, rung |

## 4. Đề xuất nâng cấp (theo thứ tự ưu tiên)

1. **`GameConfig` dạng ScriptableObject** (`Resources/Config/Economy.asset`) thay hằng số trong EconomyManager/LivesManager:
   starterCoin, giá booster, mở khóa booster, `priceRefillHealth`, `priceExtraSlot`, `rewardSuccess/Boss/Bonus`,
   giá Reserve. Mô phỏng Remote Config của gốc, chỉnh trong Inspector không cần sửa code.
2. **Loại màn (LevelKind: Normal / Hard(Boss) / Bonus)**: thưởng theo `rewardBoss/rewardBonus`, popup BONUS LEVEL,
   badge HARD (đã có màu). Quy luật bonus cần thêm video để xác nhận.
3. **Luật tim theo gốc**: trừ khi bắt đầu màn, hoàn khi thắng, mất khi thoát giữa màn; nút hồi tim bằng xu.
4. **Placement cho EconomyManager**: thay chuỗi `reason` bằng enum giống `Placement` → thống kê/analytics rõ ràng.
5. **Booster QuadLaneSlot + khu Reserve**: thêm vào `SandGame` như các mutation thuần (giống Swap/SelectPriority).
6. **Màn Gallery** (`View_Gallery_Category/Card/Milestone`): dùng `Collections` + `FeatureConfig` + ảnh `GeneratedLevelRenders`.
7. **Chế độ sự kiện** cho 6 gói chưa vào campaign (AnimalPortrait, Dance, EventFigures, FoodAndDrinks, Halloween, ModernPosters).
8. **Quà hằng ngày / nhiệm vụ ngày-tuần**: dữ liệu `OriginalReference/Resources/rewards/reward_daily.asset` có sẵn.
9. **Hết danh sách màn → màn ngẫu nhiên** từ pool thay vì lặp lại.

Nguyên tắc giữ cho bản clone: luật chơi ở `Core` (thuần, test được bằng solver), hiển thị ở `Views`, điều phối ở
`Controllers`, dữ liệu/tiến trình ở `Managers`, giao tiếp qua `GameEvents` — tương ứng với tách lớp của gốc
nhưng không dùng Singleton MonoBehaviour cho luật chơi.
