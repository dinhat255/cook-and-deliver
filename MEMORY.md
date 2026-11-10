# Project memory: Bếp Giao Hòa (cook-and-deliver)

## Ràng buộc bất biến
- Unity 6000.3.16f1, URP 2D, màn dọc 9:16 (1080x1920), Android/iOS.
- Kiến trúc Pillarbox/Safe Framing & Safe Area:
  1. `PillarboxBackground`: Neo tràn viền (0,0 đến 1,1) màu nền tối ấm (#141211), ngăn hở viền xanh của Camera.
  2. `SafeAreaContainer`: Tự động co giãn theo `Screen.safeArea` qua `SafeAreaFitter.cs`, cách ly hoàn toàn khỏi tai thỏ, notch và thanh cử chỉ đáy.
  3. `GameFrame9x16`: Nằm trong `SafeAreaContainer`, gắn `AspectRatioFitter` (FitInParent, 9:16 = 0.5625). Bảo toàn 100% tỷ lệ và khoảng cách thiết kế trên mọi màn hình từ 20:9 đến tablet 4:3.
  4. `DynamicCanvasScaler.cs`: Tự động chuyển đổi `matchWidthOrHeight` linh hoạt giữa 0 (Phone dài) và 1 (Tablet vuông).
- Cấu trúc các vùng chơi (nằm trọn trong `GameFrame9x16`):
  1. `TopZone` (Top-Stretch, cao 416px): Chứa `ZoneHud` background mat, `HudView`, KDS rail 2 vé (`Ticket0`, `Ticket1`), nút huy hiệu hàng chờ (`QueueBadgeButton`).
  2. `CookingZone` (Middle-Stretch, cao 990px): Chứa `ZoneStations` mat, `ZoneCounter` mat, `HeatSlotView` (Stations), `BoardView` (Boards).
  3. `BottomZone` (Bottom-Stretch, cao 400px): Chứa `ZoneTray` mat, `TrayLine`, `TrayButtonView` (Tray 6x2).
  4. `FlyLayer`: Nằm trong `GameFrame9x16` (0,0 đến 1,1) để tọa độ bay parabol `InverseTransformPoint` đồng nhất 1-1 với thớt và khay.
  5. `ShipFullOverlay`, `OrderQueuePopup`, `CulinaryCodexPopup`: Giữ ở cấp Canvas ngoài cùng để dimmer làm mờ toàn bộ màn hình khi mở modal.
- Asset pixel art: 100% 11 IngredientSO và 8 RecipeSO sử dụng bộ sprite tinh chỉnh sạch viền tại `Assets/Art/Cooking/clean/`. Hạn chế asset: không vẽ sprite trạng thái trung gian.
- Người dùng phụ trách phần Cooking. Phần Delivery do người khác làm (scene `Delivery`).

## Quyết định kiến trúc (phần Cooking)
- Toàn bộ mã nguồn, định danh (identifiers), comments và log được chuẩn hóa hoàn toàn bằng tiếng Anh.
- Áp dụng Clean Architecture và Design Patterns phân tách rành mạch:
  1. Data Layer (`CookAndDeliver.Cooking.Data`): `IngredientRequirement`, `IngredientSO`, `RecipeSO`, `CookingConfigSO`, `RecipeDatabaseSO`, `CookingSession`.
  2. Core Domain & Models Layer (`CookAndDeliver.Cooking.Core.Models`): `OrderModel`, `BoardModel`, `HeatSlotModel`, `HeatSlotState`.
  3. Core Events Layer (`CookAndDeliver.Cooking.Core.Events`): Event Channel SO pattern gồm `OrderEventChannelSO`, `StationEventChannelSO`, `WorkstationEventChannelSO`, `EconomyEventChannelSO`.
  4. Core Services Layer (`CookAndDeliver.Cooking.Core.Services`): Tách biệt hoàn toàn thành các GameObject độc lập (`Services/PoolService`, `Services/VfxService`, `Services/EconomyService`, `Services/AudioService`) và kết nối thông qua interfaces:
     - `IPoolService`: Quản lý Object Pool cho bay item, toast, puff, spark.
     - `IVfxService`: Điều khiển tweening, bay parabol, rung lắc, pháo hoa.
     - `IEconomyService`: Quản lý tính thưởng, combo, số lượng ship, trừ phạt.
     - `IAudioService` & `AudioService`: Tích hợp với kiến trúc `AudioManager` và `AudioClipSO` (`SingleAudioClipSO`, `RandomAudioClipSO`, `SequenceAudioClipSO`), hỗ trợ BGM loop, fade in/out qua AudioMixer snapshot và hiệu ứng SFX tương tác bếp.
     - `CookingServiceLocator`: Đăng ký và truy xuất service qua interface marker `IService`.
  5. UI / View Layer (`CookAndDeliver.Cooking.UI`): 100% TextMeshProUGUI. Gồm `HudView`, `TicketView`, `TicketSlotView`, `BoardView`, `ChecklistSlotView`, `HeatSlotView`, `TrayButtonView`, `ShipOverlayView`, `OrderQueuePopupView`, `TMPKit`.
  6. Presenters Layer (`CookAndDeliver.Cooking.Presenters`): MVP pattern gồm `HudPresenter`, `OrderPresenter`, `HeatStationPresenter`, `WorkstationPresenter`, `CookingMasterPresenter`.
  7. Editor Layer (`CookAndDeliver.Cooking.Editor`): `CookingAssetDatabaseBuilder`, `CookingSceneBuilder`, `CookingSpriteImporter`.
  8. SaveLoad Layer (`CookAndDeliver.SaveLoad.Core`, `CookAndDeliver.SaveLoad.Objects`):
     - `ISaveLoadHandler`, `JsonSaveLoadHandler`, `SaveLoadManager`, `SaveTree`, `EncryptUtil` (AES-256).
     - Đường dẫn lưu: `Application.persistentDataPath/SaveData/savedata.json`.
     - Generic `LoadAsync<T>` type-safe, I/O bất đồng bộ hoàn toàn qua `UniTask`.
     - Hỗ trợ mã hóa AES linh hoạt qua cờ `UseEncryption`.
     - `GameProgressSLO` lưu trữ tiến trình mở khóa công thức, coin, combo, liên kết đồng bộ với `RecipeProgressionService`.
     - `GeneralSettingsSLO` lưu cấu hình người chơi, âm lượng BGM/SFX, Mute.
- Hệ thống KDS 2 làn độc lập song song (Dedicated Dual-Lane KDS):
  - Khóa chặt liên kết song song 1-1: `Ticket0` (Trái) gắn chặt với `Board0` (Thớt Trái); `Ticket1` (Phải) gắn chặt với `Board1` (Thớt Phải). Nhìn từ trên xuống luôn thẳng hàng 1-1, không chéo cánh X.
  - Màu sắc vé: Trắng ngà (`#F7F5E6` chữ đậm `#262626`) cho Tại bàn, Cam đậm (`#FFA847` chữ nâu đen `#1F1405`) cho Giao ship.
  - Hoạt ảnh 2 làn độc lập:
    - Ô Trái: Vào từ mép trái (`x - 260`), xong đơn trượt ra mép trái (`x - 260`) rồi rút đơn mới từ hàng chờ vào đúng ô Trái.
    - Ô Phải: Vào từ mép phải (`x + 260`), xong đơn trượt ra mép phải (`x + 260`) rồi rút đơn mới từ hàng chờ vào đúng ô Phải.
    - Hai làn hoạt động độc lập, không dồn chéo ô làm xáo trộn thớt.
  - Hàng chờ đơn (Queue Badge & Popup): Nút `QueueBadgeButton` hiển thị số lượng đơn chờ ngoài 2 vé chính (`+N`). Bấm vào mở popup `OrderQueuePopup` modal.
  - Highlight liên kết hai chiều: Chạm vào vé nào thì kích hoạt chọn thớt đó; chạm vào thớt nào thì vé tương ứng sáng viền vàng.
  - Smart-tap Phương án B: Ưu tiên thớt đang chọn. Nếu thớt đang chọn không cần, nguyên liệu được phép bay sang thớt còn lại để tránh lãng phí, nhưng TUYỆT ĐỐI KHÔNG tự ý chuyển `ActiveBoard`. Quyền chọn thớt thuộc về người chơi.
- Cơ chế số lượng nguyên liệu (Ingredient Quantities) & Tọa độ bay chính xác:
  - Thay vì 1 nguyên liệu đơn lẻ, mỗi công thức quy định số lượng cụ thể (ví dụ: Thịt bò x2 cho Phở, Thịt heo x3 cho Bánh mì lấp đầy 3 slot vỉ nướng, Bánh gạo x3 cho Tteokbokki).
  - Tọa độ bay (`FlyArc`): Nguyên liệu bay từ khay/bếp đến thớt đáp chính xác vào ô checklist (`GetSlotWorldPos`) tương ứng thay vì rơi chung vào tâm thớt.
  - Tọa độ thiết bị gia nhiệt chuẩn visual:
    - `Grill`: 3 slot nướng gom trọn vào lò than đỏ (`ImgGrill`, X: 0.40 - 0.92, Y: 0.22 - 0.88), không trải ra mặt bàn tủ inox bên trái.
    - `Oven`: `IconBox` nằm gọn trong khoang kính trên vỉ sắt (Y: 0.44 - 0.78), thanh tiến trình nằm ở gờ dưới cửa lò.
    - `Pan`: `IconBox` nằm giữa lòng chảo sốt (X: 0.24 - 0.76, Y: 0.44 - 0.78), thanh tiến trình đặt trên thân bếp gas.
  - Phân bổ KDS và hàng chờ: `Ticket0` (0.02 - 0.44), `Ticket1` (0.46 - 0.88), `QueueBadgeButton` (0.90 - 0.98) đảm bảo lọt lòng chuẩn tỷ lệ và không đè lấn nhau.
  - Nước lèo (Broth Station UX): Sử dụng sprite `pot.png` (chiếc nồi inox) làm icon cho `Ingredient_broth` để tương đồng 1-1 với Nồi Nước Lèo trên bếp. Nồi tự động nhịp to nhỏ (pulse hint) khi thớt đã có bánh phở và đang chờ nước lèo. Toast phân biệt rõ: "Cần bánh phở trước" khi thiếu phở và "Không cần nước lèo" khi đơn không yêu cầu.
- Menu Editor: `Cooking > Build or rebuild Cooking scene (Clean Architecture)` thực hiện một cú click để sinh dữ liệu và nối dây trong scene.
- Chín/cháy bằng code: đổi màu sprite + thanh tiến trình + khói UI. Vỉ 3s/6s/8s, lò ép 2s (không cháy), chảo 2.5s/4.5s/6s.
- Smart-tap: chạm 1 lần, sprite bay cung parabol 0.18s. Thớt Active ưu tiên, nếu không nhận thì thử thớt còn lại.
- Chọn phương án thân thiện: nguyên liệu sai bị bật lại, không hỏng món, không phạt. Chỉ phạt khi để cháy (-15 xu, mất combo) hoặc dọn thớt (-5 xu).
- Đủ 3 hộp ship: Deep Freeze bếp, hiện overlay (Giao hàng -> scene Delivery, hoặc tiếp tục để test).
- Chế độ Thùng rác đa mục tiêu (Toggle Trash Mode): Nút Thùng rác hoạt động theo dạng bật/tắt (Toggle). Khi bật (nút pulsing nhịp nhàng, chữ 'HỦY MÓN', dấu X vàng), chạm vào Thớt sẽ dọn thớt (-5 xu), chạm vào Thiết bị gia nhiệt (Vỉ nướng, Lò ép, Chảo) sẽ dọn sạch ô bếp (-5 xu dọn dẹp, hoặc -15 xu nếu cháy). Tự động tắt chế độ sau khi dọn xong hoặc khi người chơi chạm lấy nguyên liệu khác. Giải quyết triệt để bài toán kẹt nguyên liệu không cháy (như bánh mì trong lò ép).
- Hệ thống mở khóa công thức và Sổ tay ẩm thực (Recipe Progression & Culinary Codex):
  - `IRecipeProgressionService` & `RecipeProgressionService`: Quản lý mở khóa món ăn lưu bền vững kết hợp đa tầng: đọc/ghi bất đồng bộ qua `SaveLoadManager` (`GameProgressSLO`) và đồng bộ `PlayerPrefs` (`RECIPE_UNLOCKED_{id}`) để bảo toàn tương thích ngược.
  - `RecipeSO` mở rộng: Lưu trữ mô tả (`dishDescription`), nguồn gốc lịch sử văn hóa (`culturalStory`), đặc trưng vị giác (`flavorProfile`), mẹo đầu bếp (`chefTip`), trạng thái mở khóa mặc định (`isUnlockedByDefault`) và học phí mở khóa (`unlockCostCoins`).
  - `OrderPresenter.SpawnOrder`: Rút ngẫu nhiên đơn hàng từ tập các món đã học (`RecipeDatabaseSO.GetRandomUnlockedRecipe`).
  - `CulinaryCodexPopupView`: Sổ tay ẩm thực và học viện nấu ăn, hiển thị chi tiết văn hóa ẩm thực Việt - Hàn - Fusion và cho phép trả phí học mở khóa công thức.
  - Danh mục mở rộng lên 8 món: Phở Bò Tái, Bánh Mì Thịt Nướng, Tteokbokki Cay (mở mặc định); K-Bánh Mì Bulgogi, Phở Kim Chi Bulgogi, Tteokbokki Cung Đình, Bánh Mì Thập Cẩm Giao Hòa, Mì Xào Kim Chi Thịt Heo (mở qua học nấu ăn).

- Hệ thống màu sắc khay nguyên liệu (Category-based Color Palette): Màu nền ô nguyên liệu được cấu hình Data-driven trực tiếp qua trường `slotBackgroundColor` trong `IngredientSO` theo nhóm dinh dưỡng/ẩm thực (Rau củ: xanh lá tươi `#2E7D32`, Bánh mì: vàng hổ phách `#C27D28`, Bánh phở/gạo: xám bạc khói `#4A5568`, Thịt heo: hồng tươi `#8C3B4A`, Thịt bò: đỏ nâu steak `#6B2D2B`, Sốt cay: đỏ ớt `#A83226`, Nước tương: nâu sô-cô-la `#3E2723`, Thùng rác: đỏ cờ `#C72929`), tự động đồng bộ cả trong Scene View (`OnValidate`) và Runtime (`Awake`).
- Hệ thống cầu nối Cooking - Delivery và Gameplay loop theo ngày (Day-based Loop):
  - `CookingSession`: Lưu trữ bền vững xuyên scene danh sách món ship vừa nấu (`CookedDeliveryOrders`), bộ đệm kết quả giao hàng (`LastDeliveryEarned`, `LastDeliveryPenalty`, `LastDeliveryCargoLost`), và tiến trình ca kinh doanh (`CurrentDay`, `DayTargetDineIn`, `DayTargetDelivery`, `DayDineInCompleted`, `DayDeliveryCompleted`).
  - Cầu nối Assembly độc lập (Decoupled Bridge): `DeliveryController` mở các static hooks (`ExternalRequestProvider`, `OnExternalRunCompleted`, `OnExternalReturnRequested`, `ExternalReturnButtonLabel`, `PendingOrderCount`) giúp Cooking liên kết 2 chiều mà không tạo phụ thuộc vòng (circular reference) hay ép sửa asmdef của Delivery.
  - Xử lý kinh tế khi ship hỏng: Trường hợp `CargoLost` (độ nguyên vẹn về 0%), người chơi bảo lưu điểm các đơn đã giao trước đó, bị phạt phí bồi thường cho khách (-50 xu/món chưa giao). Không gây Game Over văng app, cho phép bấm "Về quán" tiếp tục ca kinh doanh.
  - Đóng ca và chuyển ngày: Quán hoàn thành cả đơn tại bàn lẫn chuyến giao hàng thì kích hoạt hoàn thành ca và chuyển sang ngày kế tiếp (`AdvanceToNextDay`).
  - Màn hình chính (MainMenu Scene): `Assets/Scenes/MainMenu.unity` là scene khởi động đầu tiên (Build Index 0). `MainMenuController` điều khiển nút "Vào bếp mở quán" (hoặc "Tiếp tục ngày X"), hiển thị ví xu, tiến trình quán và bật/tắt âm thanh.
  - Popup tổng kết ca (DaySummaryPopupView): Modal hóa đơn (Daily Receipt) nằm ngay trong `CookingCanvas` của `Cooking.unity`. Hiển thị chi tiết doanh thu đơn tại bàn, đơn ship, phí bồi thường, lãi ròng và sao đánh giá. Cho phép mở Sổ tay ẩm thực học món mới hoặc bấm "Mở cửa ngày mới" để bắt đầu ca tiếp theo ngay tại chỗ (zero-loading).
  - Tuân thủ Unity MCP: Toàn bộ việc quản lý, chỉnh sửa Scene, Component và GameObjects thực hiện qua các công cụ `unityMCP`; đã xóa toàn bộ 5 file setup/builder trong `Assets/Scripts/Delivery/Editor` (chỉ giữ công cụ debug `Assets/Scripts/Cooking/Editor/`).
  - Đồng bộ UI và asset phần Delivery (`Delivery.unity`, `DeliveryResultRow.prefab`, `DeliveryConfig.asset`):
    - Dùng `DeliveryCanvasScaler` + `PillarboxBackground (#181513)` + `SafeAreaContainer` (`DeliverySafeArea`) + `GameFrame9x16` (`AspectRatioFitter FitInParent 0.5625`), các modal (`Ready`, `Countdown`, `Pause`, `Results`) đặt cấp Canvas ngoài cùng có dimmer `#000000E0` và `SafeOverlayArea`, font `LiberationSans SDF`, card kem `#F7F5E6`, viền 9-slice `pixil-frame-0 (5)/(6)/(7)` đồng bộ với `Cooking`.
    - Bộ 8 sprite `Assets/Art/Cooking/clean/Recipe_*.png` nối vào `DeliveryUIView.dishCatalog` và `readyFoodIcons`, khớp động theo prefix `recipeId` và tên món từ `CookingSession.CookedDeliveryOrders`.
    - `DeliveryController`: tự tắt `AudioListener` cục bộ khi đi từ `Cooking` đã có `AudioManager` (`DontDestroyOnLoad`), nhãn nút về `"Về quán"` khi chạy từ Cooking và `"Về đầu"` khi standalone.
  - Kết thúc ca (`CookingMasterPresenter.BeginDayEndSequence` & `ShowDaySummary`): đóng `shipOverlay` và `queuePopup` trước khi pause, xóa toàn bộ vé/hàng chờ (`OrderPresenter.ClearAllOrders` -> badge bell về `0`), dọn bếp/thớt và giữ `SHIP: [3/3]` tới khi `OnNextDayTransition()`. `GameProgressStore.EnsureLoadedAsync` cấp `UniTaskCompletionSource` riêng cho mỗi caller đồng thời (tránh lỗi `Already continuation registered`).
  - Hệ thống chuyển cảnh mượt (SceneLoader): `SceneLoader.unity` (Build Index 3) làm scene Additive trung gian. `LoadSceneHandler` hỗ trợ nạp scene qua cả build index lẫn tên scene (`ResolveBuildIndex`), loại bỏ lỗi index -1. `FadeableLoader` dùng UniTask và DOTween độc lập timescale (`SetUpdate(true)`), tự động bật `raycastTarget` ngăn chạm nút lúc chuyển cảnh, màu fader chuẩn tông ấm `#141211`. Toàn bộ chuyển scene giữa MainMenu, Cooking và Delivery đều qua `LoadSceneHandler.LoadSceneByName`.
- Hệ thống Tutorial ngày 1 tách rời gameplay (Decoupled Day 1 Tutorial) và công cụ test Save/Load:
  - Lưu trữ thống nhất trong `GameProgressSLO` qua `GameProgressStore`: bổ sung `CurrentDay`, `IsCookingTutorialCompleted`, `IsDeliveryTutorialCompleted` (đồng bộ với `DeliveryProgressStore` ở Delivery qua static hooks trên `DeliveryController`).
  - Phần Cooking (`CookAndDeliver.Cooking.Tutorial`): điều phối bằng `CookingTutorialDirector` + `Day1CookingTutorial.asset` (`TutorialSequenceSO`, 13 bước + 3 mẹo mềm ngày 1), dùng `TutorialAnchor` / `TutorialAnchorRegistry`, `TutorialSpotlightMask` và `CookingTutorialOverlayView` (`FloatingLayer` Sub-Canvas `sortingOrder = 50`). Với bước `BoardIngredientCount` nhắm nhiều bếp (`station.*`), `ResolveStepTargets` lọc động các slot còn nguyên liệu (`MatchesSlotTransform` kiểm tra cả child `"Hit"`) ưu tiên `Perfect`/`Warning` và `CookingMasterPresenter` luôn dọn qua `ClearHeatSlot`. `BoardDine` mang `isDeliveryBoard = false` (`RA MÓN`) tách biệt `BoardShip` (`ĐÓNG HỘP`), `OrderPresenter.UpdateViews` ẩn cả placeholder scene khi `order == null`.
  - Phần Delivery (`CookAndDeliver.Delivery.DeliveryTutorialController`): lắng nghe `DeliverySimulation` + `SwipeInput`, dùng seam `SetSimulationFrozen` hướng dẫn 2 bước né chướng ngại vật và đổi làn vào vạch vàng giao hàng.
  - Công cụ kiểm thử Editor (`TutorialSaveDebugEditor`): menu `Tools/Tutorial/Reset All Save Data (Test Tutorial)`, `Reset Tutorial Flags Only (Day 1)`, `Show Dev Skip Controls` (mặc định tắt, lưu `EditorPrefs` key `CookAndDeliver.Dev.ShowTutorialSkipControls`, ẩn hoàn toàn nút `Bỏ qua bước`, `Qua Ngày 2` và `Bỏ qua` Delivery khi tắt hoặc trong bản build), và `Open Save & Tutorial Debugger`.

## Ngõ cụt / không làm
- Không dùng Legacy Text (`UnityEngine.UI.Text`), toàn bộ chuyển sang TextMeshPro (`TextMeshProUGUI`).
- Không đặt tên biến/comment bằng tiếng Việt trong code nguồn, 100% dùng tiếng Anh chuẩn.
- Không dùng công thức nguyên liệu 1-1 đơn điệu; bắt buộc hỗ trợ số lượng stack (x2, x3) để tạo độ sâu nhịp bấm cho game cooking.
- Không hardcode công thức trong code hoặc enum gộp, toàn bộ chuyển sang ScriptableObject riêng lẻ (`IngredientSO`, `RecipeSO`).
- Không dựng UI bằng code thuần lúc Awake/Start vì không thể inspect hoặc kéo thả tinh chỉnh trên Scene View.
- Không di chuyển vé từ trái sang phải ngược trực giác FIFO KDS bếp công nghiệp.
- Không dùng nồi kín "bỏ hết vào là ra món": mất bản sắc món nướng/bánh mì.
- Khay 4x4 hoặc 3x6 quá nhỏ cho ngón tay: dùng lưới 6 cột x 2 hàng.
