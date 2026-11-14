**Cập nhật:** Điểm trở về hub được gán tại `LevelSelectionManager > Hub First Player / Hub Second Player`, lấy hai điểm con của `Spawnpoint Hub`. Không dùng Level Index 0 để về hub. Các trường Hub Level Index cũ đã bỏ.

# DIVIDED — Màn sân khấu đã setup trong scene

Scene: `Assets/Scenes/SampleScene.unity`
Root mới: `Theatre_Level_4_Setup`
Vị trí: nằm trong khu vực hai phòng `Theater Room 1` và `Theater Room 2` hiện có.

## 1. Mở và chạy thử

1. Nếu Unity đang mở scene cũ, mở lại SampleScene từ Project để nạp bản đã sửa trên đĩa. Đừng lưu đè bản scene cũ đang giữ trong bộ nhớ.
2. Đợi Unity import font và compile script.
3. Chọn **DIVIDED > Theatre > Select Configured Stage** để tìm root mới trong Hierarchy và Scene View.
4. Chọn **DIVIDED > Theatre > Validate Open Scene**. Console sẽ báo nếu còn field thiếu.
5. Chạy Host theo cách hiện tại của project.
6. Dùng cửa sân khấu hiện có: `LevelDoor > Level Index = 3`.
7. Hai người được đưa tới hai phòng. Nhìn về sân khấu, tương tác nút START phòng A bằng E.
8. Khi thử một mình, tăng `TheatrePuzzleManager > Answer Seconds` từ 10 lên 60. Game không bắt buộc đủ hai người để bắt đầu, nhưng bạn cần di chuyển giữa hai phòng để đối chiếu.

**Lưu ý:** tên gọi “màn 4” là thứ tự thiết kế; index của cửa sân khấu trong scene hiện tại là **3**. Không đổi index thành 4 khi danh sách spawn chưa có phần tử tương ứng.

## 2. Những gì đã gán

| Object / Field | Cấu hình hiện tại |
| --- | --- |
| Theatre_Level_4_Setup/TheatrePuzzle | TheatrePuzzleManager |
| Level Selection Manager | Manager hub hiện có |
| Theatre Level Index | 3, giữ cặp Spawn theater 1/2 hiện có |
| Hub Level Index | 0 |
| Sequential Mode | Tắt |
| Rooms | Room_A rồi Room_B |
| Use Built In Gestures | Bật |
| Answer Seconds | 10 |
| Curtain Seconds | 1.5 |
| Lift Seconds | 1 |
| Intermission Seconds | 1 |
| Completion Seconds | 2 |
| Font ký hiệu | Hai font nguồn, tạo TMP font động khi bắt đầu Play |
| AudioSource | Có SFX và Music riêng ở mỗi phòng; chưa gán clip âm thanh |

Hai điểm spawn đã quay về hướng sân khấu. Không thay đổi các phần tử spawn khác hoặc logic màn cầu kính.

## 3. Cấu trúc mới trong mỗi phòng

- `Moving`: rèm, Mystery, Reference_0..3, StartButton, Answer_0..3.
- `Markers`: các điểm Hidden/Shown riêng cho từng vật.
- `StageFloor`: sàn bệ có BoxCollider.
- `Backdrop`: nền phía sau ma nơ canh.
- `Stage_PointLight`: một Point Light realtime, tắt ban đầu.
- `RoundStatus`: bảng trạng thái và timer.
- `SFX`, `Music`: AudioSource để bạn gán âm thanh.

Rèm hiện là hai mảng màu đỏ sẫm đơn giản để thử chuyển động. Ma nơ canh là bộ hình khối có khớp tay/chân. Đây là model mẫu để kiểm tra luật chơi; bạn có thể thay bằng model và animation riêng sau.

Các object Puppet, button_the_mines, start và đèn cũ nằm trực tiếp trong hai Theater Room đã được **tắt**, không bị xóa. Hai nhóm rèm cũ Left Curtain/Right Curtain của phòng B cũng được tắt để không che bộ sân khấu mới. Không bật lại đồng thời nếu chưa thay các tham chiếu của bộ mới.

## 4. Luật hoạt động

| Lượt | Room_A | Room_B |
| --- | --- | --- |
| 1–5 | Mystery dấu ? và bốn nút ký hiệu | Bốn ma nơ canh có ký hiệu |
| 6–10 | Bốn ma nơ canh có ký hiệu | Mystery dấu ? và bốn nút ký hiệu |

Người xem Mystery mô tả động tác. Người xem bốn ma nơ canh tìm động tác khớp rồi đọc ký hiệu. Người ở phòng Mystery bấm nút có ký hiệu đó.

Mỗi lượt có 4 động tác khác nhau. Tổng 40 ký hiệu không trùng trong một lần chơi. Động tác, ký hiệu và thứ tự nút được chọn độc lập. Sai hoặc hết giờ: reset toàn bộ, đóng sân khấu rồi đưa START phòng A lên lại. Sau lượt 5 tự đổi vai. Đúng lượt 10: hoàn thành và về hub.

Đèn bật ngay khi bắt đầu mở lượt, giữ bật trong lúc diễn và đóng rèm, chỉ tắt khi rèm đóng hoàn toàn. Nhạc trong lượt dừng ngay khi bắt đầu đóng rèm.

## 5. Bộ 20 động tác mẫu

Vẫy tay phải; vẫy tay trái; giơ hai tay; dang ngang hai tay; vỗ tay; cúi chào; gật đầu; lắc đầu; chỉ trái; chỉ phải; chào quân đội; chống hông; khoanh tay; nhảy; đá chân trái; đá chân phải; đấm luân phiên; lắc người; xoay tròn; che mặt.

Các động tác này do `TheatreGestureRig` điều khiển trực tiếp các khớp, không cần Animator hoặc AnimationClip. Mục tiêu và ma nơ canh đối chiếu được lấy mẫu theo cùng thời gian của lượt. Chúng là phương án mẫu vì project hiện chưa có bộ khoảng 20 AnimationClip riêng biệt.

## 6. Chỉnh vị trí nâng/hạ

Trên mỗi TheatreActor/TheatreButton có nhóm **Lift**:

| Field | Ý nghĩa |
| --- | --- |
| Moving | Root được nâng/hạ, chứa cả model và bảng ký hiệu |
| Hidden Pose | Điểm ẩn dưới sàn |
| Shown Pose | Điểm xuất hiện trên sân khấu |

Chỉnh marker trong `Markers` để thay vị trí. Không đặt marker làm con của Moving. Script nội suy world position và rotation; không nội suy scale.

Với rèm trên TheatreRoom:
- Hidden Pose là vị trí **đóng**.
- Shown Pose là vị trí **mở**.
- Moving là root cánh rèm.

Nếu thay model rèm, giữ các marker và gán Moving sang root model mới. Nếu root model có pivot khác, chỉnh lại cả hai marker trước khi Play.

## 7. Thay bằng model có Animator và clip thật

1. Mở `Moving/Mystery`, giữ TheatreActor, Lift và object Symbol.
2. Tắt hoặc xóa phần Body của ma nơ canh hình khối.
3. Đặt model có Animator làm con của root này.
4. Gán Animator của model vào TheatreActor > Animator.
5. Gỡ hoặc bỏ tham chiếu TheatreActor > Gesture Rig nếu không dùng bộ mẫu nữa.
6. Tắt Apply Root Motion trên Animator; dùng Avatar hợp lệ với clip Humanoid hoặc skeleton khớp với clip Generic.
7. Không cần Animator Controller. Script phát AnimationClip bằng Playables.
8. Làm tương tự cho cả 10 ma nơ canh.
9. Trên TheatrePuzzleManager, **tắt Use Built In Gestures**.
10. Gán Animations ít nhất 4 clip khác nhau để test, nên dùng khoảng 20 clip. Không gán clip Legacy, clip rỗng hoặc cùng clip lặp lại.
11. Chọn Validate Open Scene rồi kiểm tra lại trong Play Mode.

Không tắt Use Built In Gestures khi các ma nơ canh vẫn chỉ có bộ hình khối và chưa được gán Animator/clip phù hợp.

## 8. Thay model nút

Giữ TheatreButton, BoxCollider, Lift và Symbol. Thay phần ButtonHousing bằng model của bạn.

Collider và TheatreButton cần ở **cùng GameObject**, layer **Interactable**. Root nút hiện đã đặt layer này. Nếu model mới có collider con, tránh để chúng chặn raycast trước collider của root.

Answers có 4 phần tử. Thứ tự mảng là bốn vị trí vật lý. Không tự cố định ký hiệu vì script đổi nội dung mỗi lượt. START có Answer Index = -1. START phòng B luôn ẩn và khóa.

## 9. Font ký hiệu

Root có TheatreSymbolFonts, dùng hai font nguồn trong `Assets/TheatreSetup/Fonts`. Font TMP được tạo trong Awake và áp dụng cho text dưới root. Đã kiểm tra bảng glyph của hai font chứa đủ 40 ký hiệu.

Nếu edit mode vẫn hiện ? hoặc font cũ, kiểm tra lại khi Play: việc gán font động chỉ chạy lúc đó. Nếu thay font, gán font nguồn có đầy đủ glyph hoặc thêm nguồn fallback trong Source Fonts.

Không chỉnh mảng Symbols khác nhau giữa host và client.

## 10. Gán âm thanh

Trên TheatreRoom của mỗi phòng:

| Field | Gán |
| --- | --- |
| Effects | AudioSource SFX |
| Music | AudioSource Music |
| Round Music | Nhạc lặp trong lượt |
| Open Sound / Close Sound | Mở / đóng rèm |
| Lift Sound | Nâng / hạ |
| Press Sound | Bấm nút |
| Correct Sound / Wrong Sound | Đúng / sai |
| Timeout Sound | Hết giờ |
| Swap Sound | Đổi vai |
| Completed Sound | Hoàn thành |

Không dùng cùng AudioSource cho Effects và Music. Có thể đặt Spatial Blend = 1, Max Distance khoảng 15 để hai phòng không nghe chồng lên nhau. Các clip hiện để trống; phần luật vẫn hoạt động khi không có âm thanh.

## 11. Kiểm tra trong Unity

1. Vào từ cửa sân khấu: đèn tắt, rèm đóng, START A lên.
2. Bấm START: đèn bật, mở rèm, nâng đúng nhóm vật.
3. Mở xong: timer bắt đầu, các ma nơ canh diễn.
4. Đáp án đúng: nhạc dừng, rèm đóng, đèn tắt khi kín, tự sang lượt.
5. Đáp án sai hoặc hết giờ: START A xuất hiện lại; bấm lại bắt đầu lượt 1 với bộ ngẫu nhiên mới.
6. Qua lượt 5: đổi vai.
7. Qua lượt 10: về hub.
8. Vào lại màn: có thể bắt đầu lần chơi mới.
9. Test host/client với cùng phiên bản scene và script.

Đã kiểm tra biên dịch C#, các liên kết fileID mới trong scene, số lượng object, nối hub và độ phủ 40 glyph. Chưa xác nhận bằng Play Mode hoặc hai máy: Unity batchmode trong môi trường tự động dừng khi khởi tạo với lỗi WMI. Cần mở scene bằng Unity của bạn để kiểm tra hình ảnh, va chạm và mạng thực tế.
