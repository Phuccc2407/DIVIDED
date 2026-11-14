**Cập nhật:** Điểm trở về hub được gán tại `LevelSelectionManager > Hub First Player / Hub Second Player`, lấy hai điểm con của `Spawnpoint Hub`. Không dùng Level Index 0 để về hub. Các trường Hub Level Index cũ đã bỏ.

# HƯỚNG DẪN SETUP MÀN 3 — CẦU KÍNH

Dự án: DIVIDED / PHUCZZLE  
Scene: `Assets/Scenes/SampleScene.unity`  
Công nghệ: Unity 6, URP, TextMeshPro, PurrNet  
Tài liệu đối chiếu với script hiện tại ngày 11/11/2026.

## 1. Màn chơi hoạt động như thế nào?

1. Từ hub, tương tác cửa màn 3 để dịch chuyển cả hai người tới sàn đầu cầu.
2. Cặp kính đầu tiên ghi START là điểm chờ an toàn, chưa có câu hỏi.
3. Khi cả hai cùng đứng trên cặp START, đồng hồ bắt đầu chạy. Hai người có thể đứng chung một tấm START; không bắt buộc mỗi người một bên.
4. Màn hình lớn hiện câu hỏi cho cặp kính tiếp theo. Hai tấm trái/phải có hai đáp án bên dưới; chỉ một bên đúng.
5. Cả hai cùng đứng trên tấm đúng của cặp hiện tại thì chuyển sang câu hỏi tiếp theo. Một người tới trước phải chờ người còn lại. Đồng hồ không được cộng lại ở mỗi câu.
6. Đứng lên kính sai: tấm đó bị ẩn và tắt collider. Nếu một người rơi dưới ngưỡng Fall Boundary thì cả lượt thất bại.
7. Hết giờ: tất cả kính, kể cả START, vỡ; chờ Fall Seconds rồi đưa cả hai về sàn đầu cầu, phục hồi kính và thay bộ câu hỏi.
8. Sau khi giải hết các cặp, cả hai vào vùng đích thì hiện thông báo thành công và trở về hub.

Mẫu có 6 cặp câu hỏi, cộng 1 cặp START, tổng cộng 14 tấm kính. Thời gian mặc định là 60 giây cho toàn cầu, thời gian chờ reset là 3 giây. Không trừ máu hoặc despawn nhân vật khi rơi.

## 2. Các file cần có

| File | Vai trò |
| --- | --- |
| `Assets/Scripts/Puzzles/GlassBridge/GlassBridgePuzzleManager.cs` | Điều khiển câu hỏi, thời gian, thắng/thua và đồng bộ |
| `Assets/Scripts/Puzzles/GlassBridge/GlassBridgeQuestionBank.cs` | Ngân hàng mặc định 120 câu tiếng Anh |
| `Assets/Scripts/Puzzles/GlassBridge/GlassBridgePanel.cs` | Quản lý một tấm kính, chữ đáp án và hiệu ứng vỡ |
| `Assets/Scripts/Puzzles/GlassBridge/Editor/GlassBridgeBuilder.cs` | Tạo màn mẫu từ menu Unity |
| `Assets/Scripts/Levels/LevelSelectionManager.cs` | Dịch chuyển, khởi động và reset màn từ hub |
| `Assets/Scripts/Levels/LevelDoor.cs` | Cửa tương tác chọn màn |
| `Assets/Scripts/Player/PlayerController.cs` | Di chuyển nhân vật; xóa vận tốc rơi khi teleport |

Scene hiện đã có model tên `squid_game_glass_bridge_level`. Bạn có thể dùng công cụ tạo mẫu để thử luật trước, rồi làm theo phần 7 để dùng model này.

## 3. Chuẩn bị trước khi setup

1. Mở `SampleScene` trong Unity, thoát Play Mode.
2. Lưu scene trước khi thay đổi.
3. Mở Window > General > Console. Xử lý lỗi đỏ nếu có, chờ Unity compile xong.
4. Kiểm tra `LevelSelectionManager` của hub đang bật và đang dùng cho các cửa hiện có.
5. Giữ nguyên hệ thống NetworkManager, transport, spawn player và NetworkTransform đang dùng cho hai màn trước.
6. Nếu Unity yêu cầu TMP Essentials, chọn Import TMP Essentials. Chữ tiếng Việt cần font TMP có đủ dấu.

Chọn một trong hai cách: phần 4 tạo mẫu tự động; phần 5–9 giải thích đầy đủ để chỉnh mẫu hoặc gắn vào model riêng.

## 4. Cách nhanh: tạo màn mẫu tự động

### 4.1. Tạo cầu

1. Trên thanh menu chọn **DIVIDED > Create Glass Bridge Level**.
2. Unity tạo object `GlassBridge_Level_N`. N là index mới trong danh sách spawn của hub.
3. Công cụ tự tạo và gán:
   - Sàn đầu cầu và sàn đích.
   - Hai spawn đầu cầu.
   - Cặp START.
   - Sáu cặp kính có chữ đáp án.
   - Màn hình câu hỏi và chữ thời gian.
   - FinishZone và FallBoundary.
   - GlassBridgePuzzleManager.
   - Phần tử spawn mới và phần tử Glass Bridge Puzzles trong hub.
4. Mở Console, tìm dòng `Created glass bridge. Set a hub LevelDoor's Level Index to N...` và ghi lại N.
5. Chọn object vừa tạo, nhấn F trong Scene View để nhìn cầu.

Vị trí gốc mẫu là World Position (150, 20, 0). Muốn chuyển vị trí, di chuyển cả object `GlassBridge_Level_N`. Giữ scale gốc (1, 1, 1), chỉ xoay quanh trục Y nếu cần; kính phải nằm ngang.

Chỉ bấm menu một lần cho một cầu. Mỗi lần bấm sẽ tạo cầu mới và thêm phần tử mới vào hub. Không dùng số thứ tự màn trong tên game để tự đoán Level Index.

### 4.2. Nối cửa từ hub

1. Chọn object cửa muốn dùng để vào màn 3.
2. Nếu chưa có, Add Component > `LevelDoor`.
3. Trên `LevelDoor`:

| Field | Gán gì? |
| --- | --- |
| Level Index | N vừa được ghi trong Console |
| Level Selection Manager | Object hub có component LevelSelectionManager |

4. Cửa phải có Collider. Gắn LevelDoor trên cùng GameObject với collider mà raycast của player chạm vào.
5. Layer của cửa phải được bật trong `PlayerInteractor > Interactable Layer` trên prefab player.
6. Đứng trong phạm vi tương tác, mặc định 3 mét, nhìn vào cửa và nhấn E.

Không cần nối OnClick hoặc UnityEvent: PlayerInteractor gọi `LevelDoor.Interact()` trực tiếp.

### 4.3. Kiểm tra trước khi chạy

1. Trên manager của cầu, kiểm tra `Bridge Level Index = N`.
2. `Hub Level Index` phải trỏ tới phần tử spawn thật sự của hub; công cụ mặc định điền 0, bạn cần xác nhận.
3. Chọn hub > Glass Bridge Puzzles: phải chứa manager cầu vừa tạo.
4. Lưu scene bằng Ctrl+S. Tiếp tục phần 10 để test hai máy/host-client.

## 5. Cấu trúc object khi setup thủ công

Tạo một Empty làm object gốc, ví dụ `GlassBridge_Level3`. Dưới object này, tạo các nhóm/object sau bằng Hierarchy (tên chỉ để dễ quản lý):

| Object/nhóm | Thành phần |
| --- | --- |
| GlassBridge_Level3 | GlassBridgePuzzleManager |
| Start platform | Mesh và collider sàn đầu, không bị vỡ |
| Spawn_Player1, Spawn_Player2 | Hai Empty đặt trên sàn đầu |
| Start_Left, Start_Right | Mỗi object có GlassBridgePanel và các object con riêng |
| Row_1_Left, Row_1_Right | Cặp câu hỏi 1 |
| Row_2_Left, Row_2_Right | Cặp câu hỏi 2; tiếp tục tới số cặp mong muốn |
| Finish platform | Mesh và collider sàn đích, không bị vỡ |
| FinishZone | BoxCollider bật Is Trigger |
| FallBoundary | Empty đánh dấu ngưỡng rơi bằng tọa độ Y |
| Screen backdrop | Mặt nền tối cho màn hình |
| QuestionScreen | TextMeshPro 3D hiển thị câu hỏi |
| Timer | TextMeshPro 3D hiển thị đồng hồ |

Giữ object manager bật trong scene suốt lượt. Không bật/tắt toàn bộ root cầu để mô phỏng kính vỡ.

### 5.1. Setup một tấm kính

1. Tạo Empty tên `Row_1_Left`, scale (1, 1, 1).
2. Add Component > `GlassBridgePanel` vào Empty đó.
3. Tạo object con `Glass`, chứa MeshRenderer/MeshFilter và BoxCollider hoặc MeshCollider thích hợp.
4. Collider đỡ chân phải **tắt Is Trigger**, khớp mặt kính và được bật ban đầu.
5. Tạo object con `Answer` bằng GameObject > 3D Object > Text - TextMeshPro.
6. Đặt chữ dưới mặt kính một chút, hướng chữ lên trên. Với kính ngang, bắt đầu thử Rotation X = 90, Y = 0, Z = 0.
7. Gán Inspector của GlassBridgePanel theo bảng sau.

| Field | Object/component cần kéo |
| --- | --- |
| Surface | Collider đỡ chân trên object con Glass |
| Visual | GameObject con chứa phần mesh kính sẽ biến mất |
| Answer Label | Component TextMeshPro của chữ Answer |
| Break Effect | ParticleSystem đặt riêng trong scene, có thể để trống |

Không kéo object đang chứa GlassBridgePanel vào Visual. Nếu tắt chính object đó, việc quản lý trạng thái trở nên khó kiểm soát. Nếu một tấm có nhiều mesh, gom các mesh vào một object con rồi kéo object con đó vào Visual; Surface vẫn phải là collider đỡ chân chính.

Nhân bản thành kính bên phải và các cặp tiếp theo. Mỗi field Left/Right của manager phải dùng một GlassBridgePanel khác nhau; không gán lặp cùng một tấm cho nhiều cặp.

### 5.2. Kích thước mẫu để tham khảo

Các tọa độ dưới đây là local so với root cầu, chỉ dùng khi dựng mới. Nếu dùng model có sẵn, đặt theo kích thước thực tế của model.

| Hạng mục | Giá trị mẫu |
| --- | --- |
| Tâm kính trái/phải theo X | -1.4 / +1.4 |
| Kích thước mỗi kính | X 2.4, Y 0.18, Z 3 |
| Tâm cặp START | Z = 0 |
| Tâm cặp câu hỏi thứ i, i bắt đầu từ 1 | Z = i × 4.2 |
| Khe nhảy giữa hai cặp liên tiếp | Khoảng 1.2 đơn vị |
| Chữ dưới kính | Y khoảng -0.14 so với tâm kính |
| Sàn đầu | Tâm (0, -0.3, -5), scale (7, 0.6, 6) |
| Spawn 1 / Spawn 2 | (-1.4, 0.2, -5) / (1.4, 0.2, -5) |
| Sàn đích cho 6 cặp | Tâm (0, -0.3, 30.2), scale (7, 0.6, 5) |
| FinishZone | Tâm (0, 1.5, 30.2), size (7, 3.2, 4.5) |
| FallBoundary | Y = -6 |

Kiểm tra khoảng nhảy bằng nhân vật thật. Mỗi kính đúng phải đủ rộng cho hai CharacterController đứng cùng lúc.

## 6. Gán GlassBridgePuzzleManager đầy đủ

Chọn root chứa manager và gán tất cả field sau:

| Nhóm / Field | Gán gì hoặc giá trị nào? |
| --- | --- |
| Hub / Level Selection Manager | Manager chọn màn đang có ở hub |
| Hub / Hub Level Index | Index spawn quay về hub |
| Hub / Bridge Level Index | Index spawn vào cầu; phải giống cửa LevelDoor |
| Start Left | GlassBridgePanel của kính START bên trái |
| Start Right | GlassBridgePanel của kính START bên phải |
| Rows / Size | Số cặp có câu hỏi, không tính cặp START |
| Rows / Element 0 / Left, Right | Hai component GlassBridgePanel của cặp câu hỏi đầu tiên |
| Rows / Element 1… | Các cặp tiếp theo theo đúng thứ tự từ đầu tới đích |
| Finish Zone | BoxCollider trigger tại sàn cuối |
| Questions | Để Size = 0 dùng câu hỏi mặc định, hoặc cấu hình ở phần 9 |
| Question Display | TMP_Text của màn hình câu hỏi |
| Timer Display | TMP_Text của chữ thời gian |
| Required Players | 1 để test một mình, 2 để chơi co-op; đặt trước khi vào cầu |
| Round Seconds | 60 để thử mẫu, có thể tăng khi test |
| Fall Seconds | 3, là thời gian chờ sau thất bại trước khi teleport |
| Fall Boundary | Transform của Empty đánh dấu ngưỡng rơi |

### 6.1. Hai field dễ gán nhầm

**Finish Zone:** Tạo BoxCollider riêng, bật Is Trigger, phủ vùng đi bộ trên sàn cuối và phần không gian phía trên. Sàn cuối vẫn phải có collider vật lý riêng. Code kiểm tra vị trí chân của cả hai nằm trong vùng, không cần viết OnTriggerEnter hoặc gắn Rigidbody vào vùng.

**Fall Boundary:** Chỉ cần Empty. Manager dùng tọa độ Y thế giới của nó làm ngưỡng. Đặt thấp hơn toàn cầu nhưng cao hơn đáy vực/death zone của hệ thống khác. Không cần collider. Nếu đặt quá cao, vừa vào màn đã thất bại; quá thấp thì player rơi rất lâu mới reset.

### 6.2. Nối hub khi làm thủ công

1. Chọn object có LevelSelectionManager.
2. Mở Level Spawns, giữ nguyên các phần tử của màn cũ.
3. Tăng Size thêm 1 và gán First Player = Spawn_Player1, Second Player = Spawn_Player2 của cầu.
4. Ghi lại index của phần tử vừa thêm: đó là N.
5. Mở Glass Bridge Puzzles, tăng Size thêm 1, kéo manager cầu vào phần tử mới.
6. Trên manager cầu, đặt Bridge Level Index = N.
7. Trên cửa vào màn 3, đặt LevelDoor > Level Index = N.
8. Xác nhận Hub Level Index trỏ tới hai spawn trong hub.

Ví dụ nếu Level Spawns có Element 0 là hub, Element 1 là đồng hồ, Element 2 là pha thuốc, thì thêm cầu ở Element 3 và dùng index 3. Đây là ví dụ; hãy xem danh sách thật trong Inspector, không tự đổi index các màn cũ.

Danh sách Players của hub được code tự tìm khi dịch chuyển, không cần kéo tay.

## 7. Gắn vào model cầu kính đã có trong scene

1. Chọn `squid_game_glass_bridge_level`, mở các object con để xác định mesh kính, khung cầu và sàn.
2. Nếu các tấm kính đã tách riêng, dùng mỗi tấm để tạo một GlassBridgePanel theo phần 5.1.
3. Nếu nhiều tấm nằm chung một mesh, tách chúng trong phần mềm dựng hình rồi import lại, hoặc tạm ẩn mesh kính chung và dùng các tấm riêng do công cụ dựng mẫu tạo ra. Không để mesh chung vẫn hiện nguyên cầu sau khi một tấm vỡ.
4. Giữ collider khung/sàn đầu/sàn đích phù hợp. Xóa hoặc chỉnh collider nào vô tình phủ kín toàn bộ đường kính/khe hở: collider đó sẽ làm player không rơi dù Surface đã tắt.
5. Với kính có MeshCollider, không đặt collider của toàn bộ cầu vào Surface của từng tấm. Mỗi tấm cần collider riêng.
6. Đặt chữ đáp án dưới từng tấm, chỉnh kích thước và hướng nhìn. Gán Answer Label tương ứng.
7. Tạo cặp START riêng ở đầu cầu, hoặc dùng cặp kính đầu của model làm START rồi bắt đầu Rows từ cặp tiếp theo.
8. Gán đủ Start Left/Right và Rows lên manager theo đúng thứ tự.
9. Dời hai spawn lên sàn đầu của model; dời FinishZone lên sàn đích; dời FallBoundary xuống dưới cầu.
10. Nếu thay cầu mẫu bằng model, kiểm tra không còn kính mẫu/collider mẫu chồng bên dưới. Thử một tấm sai để xác nhận nhân vật rơi thật.

Không cần gắn GlassBridgePanel lên phần khung sắt hoặc sàn đầu/đích. Chỉ gắn lên các tấm kính có thể vỡ, bao gồm hai tấm START.

## 8. Màn hình, kính trong suốt và hiệu ứng

### 8.1. Màn hình câu hỏi

1. Tạo một Cube/Plane làm nền tối đặt ở nơi cả hai người nhìn được.
2. Tạo TextMeshPro 3D ở phía trước nền, đặt tên QuestionScreen.
3. Chọn màu chữ sáng, Alignment = Center; chỉnh Width/Height đủ cho câu dài.
4. Tạo chữ Timer riêng, lớn và dễ đọc.
5. Kéo hai TMP_Text vào Question Display và Timer Display của manager.
6. Nếu chữ bị nền che hoặc nhấp nháy, đẩy chữ ra trước nền một khoảng nhỏ. Xoay mặt chữ về phía đầu cầu.

Có thể dùng Canvas World Space với TextMeshProUGUI vì các field nhận TMP_Text. Không dùng Screen Space Overlay nếu muốn màn hình nằm trong thế giới 3D.

### 8.2. Chữ đáp án dưới kính

1. Material kính dùng shader URP phù hợp, Surface Type = Transparent.
2. Giảm Alpha đủ để đọc chữ dưới kính; mẫu tự tạo dùng alpha khoảng 0.22.
3. Không để một mesh kính đục khác chồng lên trên.
4. Tắt collider trên object chữ, nếu đã lỡ thêm.
5. Chữ dưới kính cần đủ tương phản với môi trường; dùng TMP font hỗ trợ ký tự bạn nhập.

Trước khi bắt đầu lượt, mẫu hiện START và dấu hỏi. Code tự thay text đáp án khi vào màn; nội dung bạn gõ sẵn lên Answer không phải ngân hàng câu hỏi.

### 8.3. Hiệu ứng kính vỡ tùy chọn

1. Tạo ParticleSystem cho từng tấm hoặc nhân bản một hiệu ứng kính vỡ đã có.
2. Đặt hiệu ứng tại vị trí tấm kính trong scene.
3. Tắt Play On Awake và Looping.
4. Để hiệu ứng bên ngoài object Visual bị tắt, ví dụ làm object con khác dưới root tấm kính.
5. Kéo ParticleSystem vào Break Effect trên đúng GlassBridgePanel.
6. Khi kính chuyển từ nguyên sang vỡ, script gọi Play; khi reset, script dừng và xóa particle cũ.

Để trống Break Effect vẫn chơi được: mesh biến mất và collider tắt. Script hiện tại chưa tạo mảnh kính vật lý và chưa có field âm thanh vỡ riêng.

## 9. Tạo ngân hàng câu hỏi

### Dùng sẵn

Đặt Questions > Size = 0. Manager sử dụng 120 câu tiếng Anh mặc định, gồm toán và kiến thức phổ thông. Mẫu 6 cặp sẽ chọn 6 câu trong số đó cho mỗi lượt. Bộ rút loại các câu đã dùng, đủ 20 lượt khác nhau trước khi xáo lại ngân hàng; mỗi lượt tính cả các câu chưa kịp tới khi chết.

### Tự nhập

1. Trên manager, tăng Questions > Size.
2. Trong từng phần tử, nhập đủ:

| Field | Ví dụ |
| --- | --- |
| Question | What is 7 x 8? |
| Correct Answer | 56 |
| Wrong Answer | 54 |

3. Cần ít nhất bằng số cặp Rows và ít nhất 2 câu. Có 6 cặp thì nhập tối thiểu 6 câu; nên có 30 câu trở lên để đa dạng.
4. Không để trống câu hỏi/đáp án, không để Correct Answer giống Wrong Answer.
5. Không tự viết hai lựa chọn vào Question nếu đã muốn chúng chỉ xuất hiện dưới kính.
6. Không cố định bên trái là đúng: server tự xáo vị trí đáp án cho từng cặp.

Trong một lượt, code chọn các phần tử câu hỏi không lặp. Giữa các lượt, code rút tiếp từ phần ngân hàng chưa dùng; khi không đủ cho một lượt hoàn chỉnh thì xáo lại. Với ngân hàng đủ lớn, lượt đầu sau khi xáo cũng không dùng câu của lượt ngay trước. Khi reset, mỗi cặp nhận phần tử câu hỏi khác lượt trước; vị trí đúng trái/phải vẫn ngẫu nhiên nên có thể trùng bên. Các câu được dùng ở cặp khác trong lượt trước vẫn có thể xuất hiện lại. Muốn nội dung không trùng, cũng không nhập nhiều phần tử có cùng câu.

## 10. Setup và kiểm tra PurrNet

1. Giữ NetworkManager và cơ chế spawn scene object đang dùng của dự án. Manager cầu phải được PurrNet spawn/nhận diện như các NetworkBehaviour puzzle trong scene.
2. GlassBridgePuzzleManager kế thừa PuzzleBase và NetworkBehaviour. Không tự thêm một loại NetworkObject từ framework mạng khác.
3. Các tấm kính và chữ chỉ cần tồn tại trong cùng scene trên cả host và client; không cần NetworkTransform riêng cho từng kính.
4. Prefab player phải đồng bộ vị trí về server bằng cấu hình NetworkTransform hiện tại. Server dựa trên vị trí chân để nhận diện tấm đang đứng.
5. Giữ scale nhân vật (1,1,1), giữ kính ngang để phép kiểm tra chân hoạt động đúng.
6. Màn này cần số PlayerController đang được spawn khớp Required Players (1 hoặc 2). Không để thêm player thử nghiệm trong scene.
7. Không thêm manager cầu vào danh sách puzzles tuần tự của AllPuzzleManager. Màn cầu khởi động qua LevelSelectionManager khi chọn đúng index ở cửa hub.
8. Khi build, bảo đảm SampleScene nằm trong danh sách scene của Build Profiles và scene mới nhất đã được lưu.

Để test: chạy một instance làm host và một instance làm client bằng cách kết nối hiện tại của dự án. Vào màn qua cửa hub, không chỉ kéo player tới cầu bằng Scene View: teleport thủ công không gọi phần khởi động lượt.

## 11. Quy trình test hoàn chỉnh

Có thể tạm đặt Round Seconds = 15–20 khi test hết giờ, rồi trả lại giá trị mong muốn.

| Bước kiểm tra | Kết quả cần thấy |
| --- | --- |
| Host hoặc client nhấn E tại cửa | Cả hai tới sàn đầu cầu, không bị rơi ngay |
| Chỉ một người lên START | Chữ READY, đồng hồ chưa chạy |
| Cả hai cùng đứng trên START | Đồng hồ bắt đầu; cùng thấy câu 1 |
| Một người lên kính đúng cặp 1 | Kính còn nguyên, chưa chuyển câu |
| Người còn lại lên cùng kính đúng | Hiện câu 2, đồng hồ tiếp tục |
| Một người đứng kính sai | Mesh/collider tấm sai tắt trên cả hai máy |
| Một người rơi dưới FallBoundary | Cả lượt thất bại; sau thời gian chờ, cả hai về đầu |
| Đợi hết giờ | Tất cả kính kể cả START vỡ, hai người được reset |
| Sau reset | Kính phục hồi; phải cùng lên START để đếm lại; câu hỏi đổi |
| Giải hết các cặp, chỉ một người tới đích | Chưa hoàn thành |
| Cả hai vào FinishZone | Hiện PuzzleSolvedView và về đúng hub |
| Vào cầu lần nữa từ hub | Một lượt mới bắt đầu ở trạng thái chờ |
| Kiểm tra ở cả host và client | Không có kính vỡ chỉ trên một máy hoặc chữ khác nhau |

Test thêm khi hai người đang ở hai vị trí khác nhau lúc hết giờ. Trước khi chốt, thử lại với thời gian và kích thước cầu cuối cùng.

## 12. Lỗi thường gặp và cách xử lý

| Hiện tượng | Kiểm tra / cách sửa |
| --- | --- |
| Không có menu DIVIDED | Thoát Play Mode; chờ compile; sửa lỗi đỏ; GlassBridgeBuilder.cs phải nằm trong thư mục Editor |
| Menu báo không tìm thấy LevelSelectionManager | Mở đúng scene có hub và bảo đảm object hub đang bật |
| Nhấn E không vào màn | Collider và LevelDoor cùng object; layer có trong Interactable Layer; khoảng cách tối đa; gán hub manager |
| Console báo no matching spawn points | Level Index ngoài phạm vi Level Spawns; dùng đúng index N |
| Teleport tới cầu nhưng không có câu hỏi/trạng thái READY | Gán Glass Bridge Puzzles ở hub; Bridge Level Index khớp cửa; kiểm tra manager được network spawn |
| Vừa vào đã reset liên tục | Spawn bị rơi/sàn thiếu collider; FallBoundary quá cao; spawn đặt sai vùng |
| Cả hai trên START mà không chạy | Gán đúng hai GlassBridgePanel; Surface không phải trigger; chân sát mặt kính; server nhận được vị trí remote player |
| Một người tới kính đúng nhưng không chuyển câu | Cần cả hai đứng cùng tấm đúng của cặp hiện tại, không bỏ cặp |
| Không bắt đầu, báo requires exactly two spawned players | Cần đúng hai PlayerController; bỏ player thử nghiệm thừa hoặc kiểm tra spawn |
| Đứng kính sai nhưng không rơi | Collider chung hoặc kính khác còn đỡ phía dưới; Surface gán nhầm; collider kính sai chưa nằm sát chân |
| Kính ẩn nhưng particle không hiện | Break Effect không được nằm trong object Visual bị tắt; kiểm tra ParticleSystem được gán đúng |
| Không đọc được đáp án | Kính phải trong suốt; chỉnh hướng chữ/font/kích thước; tránh mesh chồng; font có glyph cần dùng |
| Câu hỏi trống hoặc không đổi | Kiểm tra Questions, Rows và màn TMP; đọc lỗi validation trong Console |
| Tới cuối không thắng | Cả hai đã qua đủ cặp đúng; FinishZone phủ vị trí chân; gán đúng BoxCollider trigger |
| Thắng nhưng không về hub | Hub Level Index hoặc spawn hub chưa đúng; Level Selection Manager chưa gán |
| Có lỗi liên quan GameViewManager/PuzzleSolvedView | Kiểm tra manager UI và đăng ký PuzzleSolvedView như các màn trước |
| Host đúng nhưng client sai | Dùng cùng scene/build, manager được PurrNet spawn, kiểm tra đồng bộ transform player và lỗi RPC |
| Reset rồi vẫn rơi xuyên sàn | Kiểm tra collider sàn và spawn; không đặt spawn thấp hơn mặt sàn; xác nhận PlayerController đang có TeleportTo xóa vận tốc |

## 13. Checklist trước khi lưu và build

- [ ] Chỉ có một bộ cầu mong muốn, không có kính/collider trùng bên dưới.
- [ ] Hai spawn nằm trên sàn đầu, hướng về cầu.
- [ ] Cặp START không nằm trong Rows.
- [ ] Các cặp Rows đủ Left/Right, theo đúng thứ tự.
- [ ] Mỗi Surface là collider riêng, không bật Is Trigger.
- [ ] Visual không trỏ vào object chứa GlassBridgePanel.
- [ ] Chữ đáp án nằm đúng dưới kính và đọc được.
- [ ] Question Display, Timer Display được gán.
- [ ] FinishZone là trigger riêng, sàn đích vẫn có collider vật lý.
- [ ] FallBoundary thấp hơn cầu, cao hơn vùng chết nếu có.
- [ ] Ngân hàng câu hỏi hợp lệ hoặc để trống để dùng mặc định.
- [ ] Hub có spawn cầu và tham chiếu manager trong Glass Bridge Puzzles.
- [ ] LevelDoor và Bridge Level Index cùng một index.
- [ ] Hub Level Index quay về đúng hai spawn hub.
- [ ] Đã test chọn sai, hết giờ, reset và hoàn thành trên host/client.
- [ ] Đã lưu scene ngoài Play Mode.

## 14. Phạm vi hiện tại

Tài liệu hướng dẫn setup đúng hành vi của code hiện tại. Thông số kích thước là điểm bắt đầu để thử, chưa phải kết quả playtest. Kính vỡ mặc định bằng cách tắt mesh/collider; particle là tùy chọn. Màn cầu hoàn thành và về hub độc lập, chưa bổ sung hệ thống tổng kết hoàn thành cả bốn màn hoặc thiết kế màn 4.

## 15. Ngân hàng 120 câu tiếng Anh

File CSV tham khảo: GlassBridge_QuestionBank_EN.csv trong cùng thư mục. CSV có câu hỏi, đáp án đúng và đáp án sai; không dùng file này làm manh mối trong game vì có lời giải. Game đọc ngân hàng từ GlassBridgeQuestionBank.cs, không tự import CSV. Để dùng ngân hàng mới, đặt Questions > Size = 0 trên manager ngoài Play Mode.

Phân bổ: 30 câu toán, 20 câu hình học/đơn vị/thời gian, 20 câu logic, 30 câu khoa học/tự nhiên, 20 câu tiếng Anh/đồ vật. Mục tiêu thiết kế: khoảng 2–5 giây suy nghĩ cho đa số câu, dành phần thời gian còn lại cho giao tiếp và nhảy. Đây là mục tiêu, chưa phải thời gian đo qua playtest. Với 6 cặp và 60 giây, hãy thử với hai người thật trước khi chốt; có thể tăng Round Seconds lên 75 nếu việc đọc tiếng Anh mất nhiều thời gian.


## 16. Test một người bằng Inspector

1. Thoát Play Mode, chọn object có GlassBridgePuzzleManager.
2. Trong Rules, đặt Required Players = 1. Mặc định 2 là chế độ co-op.
3. Nếu game còn chờ người thứ hai trước khi spawn: chọn object có WaitForPlayersState trong hệ thống GameStates, đặt Min Players = 1. Đây là field sẵn có, độc lập với luật cầu kính.
4. Lưu scene, chạy host và đi vào cầu qua cửa hub như bình thường.
5. Với chế độ 1: một mình đứng START sẽ bắt đầu đếm, đứng lên kính đúng sẽ sang câu tiếp, vào đích sẽ hoàn thành. Kính sai, hết giờ và reset giữ nguyên cơ chế.
6. Khi quay lại chơi hai người, đặt Required Players = 2 và WaitForPlayersState > Min Players = 2 trước khi chạy.

Required Players được chốt khi vào cầu. Nếu đổi trong Play Mode, phải rời cầu rồi vào lại; thay đổi trong Play Mode không được Unity lưu lâu dài. Số player đang spawn phải đúng bằng thiết lập: không để player test thừa trong scene. Lời nhắc trên màn hình được gửi từ server theo số người của lượt.
