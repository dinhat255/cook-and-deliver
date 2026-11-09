# Delivery độc lập

Mini game giao hàng cho Unity 6000.3.16f1: xe tự chạy trên đường ba làn, người chơi vuốt để né vật cản và vào làn nhận hàng. Module có scene, config, UI, audio và save riêng; chưa nối với Cooking.

## Chạy trong Editor

1. Mở `Assets/Scenes/Delivery.unity` rồi bấm Play.
2. Chọn **LÊN ĐƯỜNG**, đợi đếm ngược.
3. Kéo chuột trái hoặc vuốt ngang trên vùng đường để đổi một làn. Mỗi lần nhấn–kéo–thả nhận tối đa một lệnh; có thể xếp một lệnh khi xe đang chuyển làn.
4. Theo chỉ dẫn trái/phải và đứng đúng làn khi vạch giao đi qua xe. Giao tự động; bỏ lỡ thì tiếp tục đơn sau.

Nút pause mở **Tiếp tục / Chơi lại / Về đầu**. Mất focus hoặc đưa app ra nền cũng pause; quay lại cần bấm tiếp tục. Chơi lại/về đầu giữa chuyến không ghi thành tích.

Mặc định có 3 đơn, nguyên vẹn 100, mỗi va chạm mất 25. Hết hàng nguyên vẹn kết thúc chuyến. Điểm của đơn giao thành công bằng thưởng cơ bản × nguyên vẹn / 100; điểm đã nhận được giữ nguyên sau va chạm về sau.

## Chỉnh gameplay

Chọn `Assets/Data/Delivery/DeliveryConfig.asset` trong Inspector. Thay đổi trước khi vào Play; `DeliverySimulation` sao chép settings lúc khởi tạo.

| Trường | Mặc định | Ý nghĩa |
|---|---:|---|
| `settings.OrderCount` | 3 | Số đơn cho nút Bắt đầu/Chơi lại, tối thiểu 1 |
| `settings.LegLength` | 4320 | Khoảng cách giữa hai điểm giao, theo pixel logic |
| `settings.Speeds` | 252, 300, 348 | Tốc độ tối đa theo chặng; các chặng sau dùng phần tử cuối |
| `settings.WaveIntervals` | 0.80, 0.70, 0.62 | Khoảng cách thời gian mục tiêu giữa các đợt; tự nới nếu cần chỗ đổi làn |
| `settings.TrafficChances` | 0.70, 0.80, 0.85 | Xác suất chọn ô tô cho từng vật cản theo chặng |
| `settings.DoubleWaveChances` | 0.55, 0.70, 0.80 | Xác suất chặn hai làn, ngoài các đợt đơn bắt buộc |
| `settings.Acceleration` | 300 | Pixel logic/giây²; lên tốc đầu chuyến và giữa các chặng |
| `settings.SlowdownSeconds` | 0.45 | Thời gian hồi tốc mượt sau va chạm |
| `settings.SlowdownMultiplier` | 0.65 | Tỷ lệ tốc độ ngay khi va chạm, rồi hồi về 1 |
| `settings.InitialClearSeconds` | 2 | Vùng an toàn đầu chuyến tính theo tốc độ tối đa chặng |
| `settings.LaneChangeSeconds` | 0.12 | Thời gian đổi một làn kiểu arcade |
| `settings.EnableHazards` | true | Tắt để kiểm tra giao hàng/UI mà không có vật cản |
| `settings.WarningSeconds` | 3 | Thời gian báo trước điểm giao và vùng an toàn trước vạch |
| `settings.CollisionDamage` | 25 | Thiệt hại mỗi va chạm hợp lệ |
| `settings.InvulnerabilitySeconds` | 1.25 | Miễn thiệt hại sau va chạm |
| `swipeWidthFraction` | 0.06 | Ngưỡng kéo theo chiều rộng vùng đường |
| `swipeHorizontalRatio` | 1.25 | Độ dịch ngang tối thiểu so với độ dịch dọc |
| `sampleOrderNames`, `baseReward` | 5 tên, 100 | Tên đơn tuần hoàn và thưởng cơ bản |

Kích thước gameplay cố định 270 × 360 pixel logic, PPU 32; tâm làn là 71 / 135 / 199. `Delivery Camera` render vào `DeliveryRoad.renderTexture` với Point filtering; `Delivery Display Camera` xuất nền lên Display 1 để Game View có camera, còn UI `Screen Space Overlay` hiển thị đường qua RawImage. UI giữ tỷ lệ 3:4 cho vùng đường và dùng safe area. Khi sửa hình học đường, cần đồng bộ constants trong `DeliverySettings` và `DeliveryEnvironmentBuilder`.

## Cấu trúc

| Đường dẫn | Nội dung |
|---|---|
| `Assets/Scripts/Delivery/Core` | C# thuần: state, chuyển làn, va chạm, giao đơn, kết quả, sinh tuyến theo seed |
| `Assets/Scripts/Delivery/Runtime` | Controller, input uGUI/Input System, world/UI, safe area và save JSON |
| `Assets/Scripts/Delivery/Editor` | Dựng scene, kiểm tra tham chiếu và build Android riêng |
| `Assets/Data/Delivery` | Config, font TMP tiếng Việt, material và RenderTexture |
| `Assets/Prefabs/Delivery` | Xe/rider, đoạn đường, sáu mẫu cây, vật cản pool, impact VFX và dòng kết quả |
| `Assets/Art/Delivery/Environment/KenneyModernCity` | 7 tile gốc: đường, vỉa hè, vạch làn, vạch giao |
| `Assets/Art/Delivery/Sourced` | Xe IanPeter, vật cản GrafxKid, cây Roboden; khách và icon Kenney |
| `Assets/Audio/Delivery` | 4 OGG gốc từ Kenney UI Pack, thay WAV tổng hợp |
| `docs/asset-sources/Delivery` (chỉ local) | Ghi nguồn, giấy phép gốc và manifest kiểm chứng asset |

`DeliveryController` sở hữu mô phỏng và các view trong scene. Mô phỏng bước cố định 0,02 giây; HUD cập nhật tối đa 10 Hz. World view tái sử dụng đường và pool vật cản. Bộ sinh tuyến kiểm tra khả năng chuyển làn, vùng an toàn đầu chuyến và quanh điểm giao; cùng settings/request/seed tạo cùng tuyến.

Controller truyền thời gian frame cho mô phỏng; chỉ mô phỏng giữ bộ tích lũy bước cố định. World view nội suy `PreviousDistance`/`Distance` và `PreviousRiderX`/`RiderX` theo `InterpolationAlpha` để cuộn đều ở 60 FPS dù logic chạy 50 Hz. Đường, cây, ô tô, điểm giao và particle cùng dùng một khoảng cách hiển thị. Nội suy trễ tối đa một bước 0,02s; va chạm, thưởng và sinh tuyến vẫn dùng trạng thái logic.

### Nhịp chạy nhanh / thử thách

Xe khởi hành ở 60% tốc độ chặng đầu, đạt 252 sau khoảng 0,34s; tăng lên 300 rồi 348 qua hai điểm giao. Tốc độ cuộn tăng 50% / 56% / 61% so với cấu hình 168/192/216. Chặng dài 4320px để giữ đủ đoạn né vật cản: chuyến 3 đơn khoảng 44,06s nếu không va chạm, chưa tính đếm ngược. Ô tô xuất hiện ngay từ chặng đầu, xác suất tăng 70% → 80% → 85%. Vẫn giữ 3s báo điểm giao, 1s vùng trống sau điểm giao, đổi làn nhanh hơn ở 0,12s và một lệnh chờ.

`DeliveryRoutePlanner` tăng mật độ theo các nguyên tắc:

- Mỗi đợt chặn tối đa hai làn. Hai đợt đầu mỗi chặng là đợt đơn; sau đó mỗi đợt thứ tư là đợt đơn bắt buộc để giảm áp lực.
- Chọn một làn trống cho đợt kế tiếp mà **mọi làn trống của đợt trước** đều tới được bằng tối đa một lần đổi làn. Người chơi chọn một trong hai lối trống ở đợt đơn không bị dồn vào ngõ cụt ở đợt sau.
- Khi cần chuyển lối, khoảng trống tính từ hết vùng chiếm chỗ của đợt trước đến đầu vùng chiếm chỗ của đợt sau ít nhất `LaneChangeSeconds + 0.25 + 0.10`, mặc định 0,47s. Vùng chiếm chỗ tính cả chiều dài xe/người chơi và biên 8px; không chỉ tính khoảng cách tâm xe.
- Bộ kiểm tra đường đi vẫn xác nhận toàn bộ chặng và làn giao. Khoảng trống được nới tự động nếu nhịp đợt mục tiêu quá sát. Xe cùng lối né có thể tạo chuỗi ngắn dày hơn.
- Ở tốc độ 348, từ lúc trọn sprite ô tô vào màn hình đến lần tiếp xúc sớm nhất còn khoảng 0,59s. Đây là giới hạn hình học ở tốc độ tối đa; tốc độ khởi hành/hồi sau va chạm thấp hơn cho thêm thời gian. Mức này cần phản ứng nhanh hơn, không yêu cầu đổi hai làn liên tiếp giữa các đợt vật cản.

Va chạm giảm tốc ngay còn 65%, hồi theo đường cong smoothstep trong 0,45s để giữ nhịp chạy. Pause đóng băng cả tiến độ hồi tốc. Tốc độ thực không vượt tốc độ tối đa dùng trong bộ kiểm tra tuyến. Làn giao sáng từ đầu khoảng báo 3s, kể cả khi vạch giao còn phía trên màn hình.

Làn giao và mũi tên trái/phải dùng chung `DeliveryWorldView.DeliveryPulse`, nhịp cosine **3 lần/giây**, khởi đầu sáng khi báo đơn mới. Alpha làn từ 0,09 đến 0,27; mũi tên đổi màu mực/cam, alpha 0,35–1 và scale 0,92–1,12. Controller gọi `AnimateDeliveryWarning` mỗi frame để không bị giới hạn bởi HUD chữ cập nhật 10 Hz. Pause giữ pha; restart/abort reset.

Gợi ý phát triển tiếp (chưa triển khai): thưởng tip theo chuỗi giao hàng không va chạm, dùng icon/nhịp sáng ngắn để báo combo; giữ khoảng nghỉ quanh điểm giao để nhịp chơi có đoạn căng và đoạn thả.

State: `Ready → Countdown → Running → Results`; `Paused` nhớ trạng thái trước đó. Pause không thay đổi `Time.timeScale` chung. Module không gọi service, save, đơn hàng hoặc ví xu của Cooking.

## API cho tích hợp sau này

Namespace runtime: `CookAndDeliver.Delivery`; model/mô phỏng: `CookAndDeliver.Delivery.Core`.

```csharp
var request = new DeliveryRunRequest(12345, new[]
{
    new DeliveryOrder("order-01", "Cơm gà", 100),
    new DeliveryOrder("order-02", "Trà sữa", 100)
});
controller.StartRun(request);
// Đăng ký sau Awake, khi controller.Simulation đã có giá trị.
controller.Simulation.RunCompleted += OnDeliveryCompleted;
```

Request cần ít nhất một đơn; ID phải duy nhất và tên không trống. Danh sách trong request quyết định số đơn của chuyến này. `StartDefaultRun(seed)` dùng số đơn/tên từ config, hữu ích để tái hiện một tuyến khi debug.

- Controller: `StartRun`, `StartDefaultRun`, `RequestLaneChange(-1 hoặc 1)`, `Pause`, `Resume`, `AbortRun`.
- Sự kiện mô phỏng: `StateChanged`, `Damaged` (nguyên vẹn còn lại), `OrderResolved`, `RunCompleted`. Thành phần đăng ký tự hủy đăng ký khi hết vòng đời.
- `DeliveryRunResult`: lý do `RouteCompleted`/`CargoLost`, seed, tổng điểm, số va chạm, số đơn giao và kết quả từng đơn.
- `OrderResult`: đơn, `Delivered`/`Missed`/`CargoLost`, nguyên vẹn lúc xử lý và thưởng đã chốt.

Adapter chuyển đơn/kết quả giữa Cooking và Delivery chưa triển khai. “Điểm chuyến” hiện là thành tích riêng.

## Save

File tại `Application.persistentDataPath/Delivery/progress.json`, version 1; lưu `totalDelivered` và điểm cao nhất theo số đơn/chuyến. Chỉ kết thúc tự nhiên hoặc hết nguyên vẹn mới ghi. `DeliveryProgressStore` chống ghi trùng cùng đối tượng kết quả trong vòng đời store; ghi lỗi có thể thử lại mà không cộng thành tích lần nữa.

Store ghi file `.tmp`, giữ bản `.bak` hợp lệ rồi thay file chính. Khi đọc, ưu tiên file chính hợp lệ → backup → dữ liệu mặc định. Lỗi ghi trả `false`, chi tiết ở `LastError`; UI kết quả thông báo chưa lưu thành tích.

## Công cụ dựng scene và build

| Menu Unity | Chức năng |
|---|---|
| `Tools/Delivery/Build standalone scene` | Dựng lại Delivery scene và prefab, tạo config nếu thiếu, chuẩn hóa sprite Point/PPU32 |
| `Tools/Delivery/Validate loaded Delivery` | Kiểm tra controller, config và tham chiếu bắt buộc trong Delivery đang mở |
| `Tools/Delivery/Build Development Android` | Build riêng `Delivery.unity` thành `Builds/Delivery/Delivery-Development.apk` |

Builder thay nội dung scene Delivery hiện có; lưu các thay đổi muốn giữ vào mã builder trước khi dựng lại. Builder từ chối scene Delivery còn thay đổi chưa lưu. Config có sẵn được giữ. Build Android yêu cầu module Android Build Support, SDK/NDK/JDK; dùng danh sách scene riêng và phục hồi thiết lập orientation sau build.

## UI theo Cooking

`DeliveryUIBuilder` dùng phong cách từ scene Cooking: nền nâu, HUD xám, thẻ kem/cam, viền tối và vàng nhấn; Liberation Sans đậm. Nền/viền/thanh dùng `Assets/Art/UI/white.png`, icon món dùng Food có sẵn. Module vẫn độc lập với Cooking.

- HUD: icon + điểm, số đơn, phần trăm nguyên vẹn + thanh màu, pause.
- Thẻ dưới: icon món, tên một dòng, thứ tự đơn, khoảng cách; gần điểm giao hiện mũi tên và “Giao”.
- Ready/Pause/Results chỉ giữ tiêu đề và nút ngắn. Toast “−25%”, “+100”, “Lỡ đơn” nằm ngoài đường, không che vật cản.
- Popup chặn input nền, có focus mặc định và navigation bàn phím; danh sách kết quả cuộn. Cụm HUD/đường/thẻ món co theo safe area và tỷ lệ màn hình.

## Art và audio có nguồn

Toàn bộ thư mục `Generated`, bốn WAV tổng hợp và script `generate_assets.py` đã được xóa theo yêu cầu. Không dùng code/AI để vẽ asset mới.

| Bộ nguồn | Sử dụng |
|---|---|
| [Roguelike Modern City — Kenney, CC0](https://kenney.nl/assets/roguelike-modern-city) | 7 tile đường/vỉa hè/vạch làn/vạch giao |
| [Cars top down view — IanPeter, CC0](https://opengameart.org/content/cars-top-down-view) | Xe đỏ/xanh nhìn từ trên xuống, đầu hướng lên |
| [Mini Pixel Pack 2 — GrafxKid, CC0](https://grafxkid.itch.io/mini-pixel-pack-2) | Cọc tiêu cam, ổ gà, rào công trường |
| [Roboden — Iskander/quasilyte, public domain](https://quasilyte.itch.io/roboden-tileset) | Sáu ảnh cây khác nhau cùng forest sheet, kiểu top-down cách điệu có thân ngắn |
| [RPG Urban Pack — Kenney, CC0](https://kenney.nl/assets/rpg-urban-pack) | Khách chờ |
| [Game Icons — Kenney, CC0](https://kenney.nl/assets/game-icons) | 9 icon HUD/nút/trạng thái |
| [UI Pack — Kenney, CC0](https://kenney.nl/assets/ui-pack) | 4 OGG tap/click/switch |

Nguồn, giấy phép, crop và SHA-256 được ghi trong `docs/asset-sources/Delivery` trên máy phát triển; thư mục này được ignore cục bộ và không nằm trong commit. Bảng nguồn ở trên ghi các trang gốc và loại giấy phép. Ba vật cản và sáu cây được cắt vùng nguyên pixel bằng Aseprite CLI, RGBA khớp nguồn. Hai xe sao chép nguyên PNG24×48, không xoay/recolor/resample. Import Point/PPU32/Full Rect, không mipmap/nén. Rider/bike gốc được giữ.

`DeliveryEnvironmentBuilder`: đường lặp128px, lòng đường x=39..231, vạch làn x=103/167, vỉa hè39px. Cây có12 instance và6 sprite gốc khác nhau, canvas32×32 scale1, tán khoảng26px nằm trọn trên vỉa hè; so le mỗi80px, vòng480px. Không tint/flip một sprite để giả nhiều mẫu cây.

`DeliveryWorldView` chừa vùng đứng cho người nhận trên đúng vỉa hè. Khoảng cách giữ trống tính từ bounds tán cây, bóng cây và người nhận, cộng 8px. Cây trùng vùng này được ẩn trước khi đi vào khung hình và giữ ẩn đến lần lặp ngoài màn hình, kể cả sau khi giao xong; tránh hiện cây đột ngột khi chuyển đơn. Reset khôi phục đủ 12 cây. Người nhận vẫn cuộn cùng mặt đường tại vị trí cố định gần vạch giao.

Ô tô scale1.5 →36×72px, hitbox30×62, thân dọc đường/đầu hướng lên. Xe máy và rider cùng parent scale1, bike18×43px, hitbox12×30. Một hazard Cone là hàng4 chiếc scale2 → mỗi chiếc12×14px, tâm x lệch −21/−7/7/21px; một hazard Barrier là hàng2 chiếc scale2.6 → mỗi chiếc26×15.6px, tâm x lệch −14/14px. Cả hai cụm có mép ngoài cách vạch làn5px và hitbox54px ngang. Ổ gà scale3 →24×24px, hitbox20×18. Scale chỉ trên transform, không sửa pixel PNG.

Ô tô là vật cản đỗ cố định: `Y = PlayerY + CrossDistance - Distance`, cuộn1:1 với đường, cây và props. Đã bỏ `TrafficSpeedFraction`; slowdown/pause tác động đồng bộ toàn cảnh. Runtime reset rotation0/flip của ô tô khi render. Enum vẫn là `Cone`, `Pothole`, `Barrier`, `TrafficGreen`, `TrafficRed` để giữ thứ tự số.

Khi lách làn, rider luôn dùng pose ngồi `Rider Green.png`; chuyển một làn trong0.12s với ease-out, cả cụm nghiêng tối đa9° và hồi nhanh về0. StepLeft/StepRight không còn reference trong scene/prefab/runtime, nhưng file nguồn được giữ để dùng cho đi bộ sau này.

### Bóng từ lớp ảnh

Đã đọc `Shadow` trong scene Cooking: một `Image` màu đen, alpha 0,4667 đặt dưới nội dung. Delivery áp dụng cùng cách xếp lớp ảnh bằng `SpriteRenderer`: sao lại sprite gốc, tô đen trong suốt rồi đặt dưới vật thể; không tạo PNG, shader hoặc đèn mới.

- Sáu prefab cây: mỗi mẫu dùng chính sprite của nó, lệch (+3, −3)px, alpha 0,4667.
- Xe máy: dùng sprite Bike, lệch (+2, −2)px, alpha 0,42; cùng parent nên theo vị trí/góc nghiêng của xe và theo alpha nhấp nháy khi miễn va chạm.
- Hai ô tô: dùng đúng sprite xe hiện tại, scale 1,5, lệch (+3, −3)px, alpha 0,4. Pool đổi sprite/scale và ẩn bóng khi chuyển sang Cone/Barrier/Pothole.
- Tất cả bóng ở sorting order −6: trên mặt đường/vạch làn, dưới cây, xe và chỉ dẫn giao hàng. Builder giữ các thiết lập này khi dựng lại scene.

### Feeling va chạm

`DeliveryImpact.prefab` có10 tia màu kem/vàng (5.5–6.5px) và6 hạt bụi xám ấm (9–10px), dùng texture trắng có sẵn của Cooking, không tạo ảnh mới. Hai ParticleSystem được tái sử dụng; hạt sống0.18–0.39s và cuộn theo điểm chạm trên đường. `DeliveryImpactFeedback` bước mô phỏng thủ công để pause đóng băng chính xác và resume tiếp tục.

Rung vùng đường khoảng1.5px/0.24s, recoil2px, roll bổ sung4° và tintấm0.09s. HUD đứng yên. Không thay Time.timeScale. Abort/restart clear hạt, tint, góc nghiêng và vị trí camera; kết thúc chuyến cho hiệu ứng tắt dần.

## Kiểm tra

Lượt tăng nhịp chạy và sửa điểm giao mới nhất ngày 2026-11-09: cấu hình 252/300/348, chặng 4320, 400 tuyến / 1.900 chặng / 29.456 vật cản / 22.940 ô tô đều qua kiểm tra đường đi, vùng giao và chuyển lối. Khoảng chuyển lối nhỏ nhất 0,469996s. Trung bình 3 đơn có 48,44 vật cản, 36,72 ô tô. 120 lượt lái phản ứng 0,25s, chỉ chọn khi thấy trọn đợt xe: 3.672 lần đổi làn, 0 va chạm, 0 lỡ đơn; thời gian 3 đơn 44,06s.

Kiểm tra PlayMode 30 điểm giao ở chiều dài chặng 4320 và 4345 (đổi pha cây): 2.050 frame người nhận hiện, không giao nhau với bounds cây/bóng, không có cây bật/tắt trong vùng nhìn thấy. Đã xem `Builds/DeliveryEvidence/fast-delivery-left-stop.png` và `fast-delivery-right-stop.png`. Nhịp báo đo ở 60 FPS có đáy tại frame 10/30/50, đỉnh tại 20/40/60; alpha/scale mũi tên đồng bộ làn giao. Pause giữ pha, abort trả đủ 12 cây và reset icon. Không tạo test assembly, không build Android; kiểm tra trực tiếp trong Editor, chưa đo trải nghiệm thiết bị thật.

Lượt tăng ô tô tiếp theo ngày 2026-11-09: 400 tuyến (1/3/5/10 đơn × 100 seed), 1.900 chặng, 25.397 vật cản và 19.778 ô tô. Mọi tuyến giữ đường đi, vùng trống quanh điểm giao và lối chuyển từ mọi làn trống của đợt trước; khoảng chuyển nhỏ nhất đo được 0,489999s. Trung bình trên 100 chuyến 3 đơn: 41,27 vật cản, 31,03 ô tô. Tỷ lệ ô tô thực tế theo chặng khoảng 66,8% / 76,4% / 83,1%; khác xác suất đặt vì kiểm tra chỗ trống và ngẫu nhiên.

Kiểm tra lái phản ứng chạy trực tiếp mô phỏng: 120 chuyến (100 chuyến 3 đơn, 20 chuyến 10 đơn), 3.224 lần đổi làn, 0 va chạm, giao đủ mọi đơn. Bộ điều khiển kiểm tra chỉ quyết định khi thấy trọn đợt xe, đợi phản ứng 0,25s, chọn ngẫu nhiên một lối trống kề bên và đợi đợt trước đi qua trước khi chuyển. Seed42 trong PlayMode giao đủ 3 đơn sau 45,62s; pool về đủ 48 view. Đã xem ảnh thực tế của tuyến này tại `Builds/DeliveryEvidence/dense-traffic-seed42.png` (khoảng cách 3.570,61; ba ô tô đang hiện). Không có lỗi gameplay/compile; có một cảnh báo kết nối MCP chống chạy trùng lệnh. Các kiểm tra mô phỏng không thay cho đánh giá cảm giác chơi trên điện thoại.

Lượt chỉnh tốc độ/bóng ngày 2026-11-09: kiểm tra trực tiếp trong Unity, không tạo lại thư mục test. 80 tuyến (1/3/5/10 đơn × 20 seed), 380 chặng và 4.915 vật cản giữ đường đi hợp lệ và vùng trống quanh điểm giao. Chuyến không vật cản giao đủ 3/3 đơn trong 48,72s. Va chạm thật giảm 156 xuống 85,8 rồi hồi đều về 156; pause giữ nguyên trạng thái và 16 hạt va chạm. Sáu lần đổi loại trong một view xác nhận bóng xe không bị sót khi pool tái sử dụng. Đã xem ảnh `Builds/DeliveryEvidence/speed-shadows-layout.png` để kiểm tra bóng cây và hai ô tô; đây là bố cục kiểm tra, không phải một tuyến ngẫu nhiên.

Nội suy đã kiểm tra ở 30/50/60/120 FPS: cùng seed và thời lượng cho cùng khoảng cách logic/va chạm. Khi chạy 156 ở 60 FPS, bước hiển thị nằm trong 2,59998–2,60004px/frame. Kiểm tra trong PlayMode xác nhận cây và ô tô không trôi tương đối khi cuộn/nội suy, pause giữ vị trí, làn giao sáng khi điểm giao còn cách khoảng 449,5px và chưa vào màn hình. Scene/prefab đã kiểm tra tham chiếu sau khi nạp lại; Console không có lỗi/cảnh báo. Chưa đo FPS hay cảm giác vuốt trên điện thoại thật.

Bộ `Assets/Tests/Delivery` đã được xóa theo yêu cầu ngày 2026-11-09. Các kết quả dưới đây là lịch sử trước khi xóa test; hiện không còn assembly test Delivery trong Unity Test Runner.

Lượt kiểm tra cuối trước khi xóa là 14/14 PlayMode đạt sau sửa camera Display 1. Unity đã nạp lại dự án sau khi dọn file: không có assembly test Delivery và Console không có lỗi/cảnh báo.

Sau chỉnh cụm vật cản và arcade: Unity xác nhận prefab có4 Cone và2 Barrier, mép ngoài ở27px so với tâm làn (vạch ở32px), hitbox ngang54px. Cụm đổi loại đúng khi tái sử dụng. Trong PlayMode, va chạm phát10 tia và6 hạt bụi với kích thước5.5px/9px ở hạt đầu. Kiểm tra thủ công20 seed, tổng1246 vật cản trên3 chặng mỗi seed, không có chặng mất đường đi; chuyển làn kết thúc sau0.14s. Bộ test Delivery vẫn đã bị xóa nên không có kết quả Test Runner mới cho đợt này.

### Kết quả kiểm tra ngày 2026-11-09

- Lượt mới nhất sau chỉnh tỷ lệ/feeling: Unity Test Runner **47/47 đạt** — Core26, save8, PlayMode13.
- Scene đã validate tham chiếu; lần kiểm tra Console gần nhất không có error/warning.
- UI mới đã xem ở 9:16, 9:20 và tablet 3:4; Results với 10 đơn/tên dài và cuộn đến cuối, gameplay/pause với safe inset mô phỏng. Đã sửa dòng kết quả co chiều rộng theo viewport để điểm không bị cắt trên tablet. Ảnh ở `Builds/DeliveryEvidence/redesign-*`.
- Đã đưa trạng thái chuột ảo qua `InputSystem.QueueStateEvent` và `InputSystemUIInputModule`; xe chuyển từ làn giữa sang trái trước khi thả chuột.
- Build Android đã dừng và hoãn theo yêu cầu người dùng ngày 2026-11-09; chưa có APK. Menu build được giữ để dùng sau. Cấu hình chung và các file tạm phát sinh đã được hoàn nguyên.

Đợt UI trước đạt 32/32 EditMode + 10/10 PlayMode. Đợt sửa asset top-down mới nhất đạt **32/32 EditMode + 11/11 PlayMode**, gồm bài kiểm tra cố ý xoay/flip/scale sai các renderer trong pool rồi xác nhận lần dùng tiếp theo khôi phục đúng hướng và kích thước. Các tuyến 1/3/5/10 đơn qua 20 seed mỗi cấu hình vẫn đi được và giữ vùng an toàn quanh điểm giao với hitbox xe dài hơn.

Ảnh hiện tại: `Builds/DeliveryEvidence/topdown-asset-layout.png` là bố cục QA cố định để xem cả hai xe và ba vật cản; `topdown-running-seed42.png` là trạng thái mô phỏng với config thật, seed42, khoảng cách6938.65, đã giao2/3 và nguyên vẹn75%, không ghi save. 22 file trong manifest khớp SHA-256; 5 crop PNG khớp RGBA nguồn, 2 xe khớp nguyên file. Không build Android. Không còn tham chiếu Generated/WAV hoặc sprite xe/vật cản/cây cũ trong scene, prefab, script Delivery.

Đợt tỷ lệ/feeling mới nhất:34/34 EditMode +13/13 PlayMode. Kiểm tra mới xác nhận cả hai ô tô giữ vị trí tương đối với đường khi slowdown/pause, lách trái/phải giữ pose ngồi và bank hồi đúng, particle/rung đóng băng qua nhiều frame thật rồi clear khi restart. Va chạm thật seed42 phát16 hạt khi nguyên vẹn100→75; hết vòng đời hạt trở về0. 26 file manifest khớp SHA-256,6 cây có6 nội dung pixel khác nhau.

Ảnh mới nhất `Builds/DeliveryEvidence/scale-feel-layout.png`; `scale-feel-impact.png` là va chạm thật; `scale-feel-impact.gif` gồm55 frame camera trong1.1s, từ trước tiếp xúc tới khi hiệu ứng tắt. Các ảnh `topdown-*`, `redesign-*`, `kenney-*` là bằng chứng lịch sử. Vòng cây hiện tại480px.

Chưa kiểm tra touch, vòng đời app và 60 FPS trên thiết bị Android thật. Các kiểm tra input/safe area trong Editor chưa thay cho bước này.

