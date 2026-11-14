# DIVIDED — Thông báo puzzle, ending và credit

Đã gắn component **PuzzlePresentation** lên object **LevelSelectionManager** trong SampleScene. UI tự được tạo khi Play, không cần tạo Canvas bằng tay.

## Tên bốn màn

| Cửa / Level Index | Số puzzle | Tên |
| --- | --- | --- |
| 0 | 1 — Đồng hồ | THE THIRTEENTH HOUR |
| 1 | 2 — Pha thuốc | THE ALCHEMIST'S OATH |
| 2 | 3 — Cầu kính | A STEP FROM OBLIVION |
| 3 | 4 — Sân khấu | THE SILENT ENCORE |

Trên LevelSelectionManager, nhóm **Puzzle Announcements And Ending / Chapters** có từng số puzzle, Level Index, tiêu đề và tham chiếu manager. Chỉnh tên ở đây nếu muốn.

Thông báo vào màn chỉ chạy khi chọn cửa, không chạy lại mỗi lần chết/reset cầu kính hoặc quay về hub.

## Cấu hình hiệu ứng

Chọn LevelSelectionManager > PuzzlePresentation:

| Field | Mặc định |
| --- | --- |
| Letter Seconds | 0.035 giây/ký tự |
| Hold Seconds | 1.8 giây |
| Fade Seconds | 0.7 giây |
| Shake Amount | 1.2 |
| Credits Pixels Per Second | 65 |

Vào màn: PUZZLE N rồi tên màn xuất hiện từng ký tự, chữ rung nhẹ, giữ lại rồi mờ dần.

Hoàn thành: PUZZLE N — CLEARED và tên màn, kèm âm thanh chúc mừng. Thông báo này không ẩn HUD bằng GameViewManager cũ.

Các ô Banner, Chapter Label, Chapter Title, Ending, Credits... có thể để None: code tự dựng UI. Nếu muốn dùng UI riêng, phải gán đủ cả bộ tham chiếu; UI tự tạo có Canvas sorting order 300.

## Âm thanh

- **Celebration Clip** đang gán `Assets/Audio/Presentation/PuzzleCleared.wav`: fanfare ngắn được tạo cho project.
- **Credit Music** tạm gán bài `Color Your Night.mp3` đang có trong project.
- Thay hai ô này bằng clip của bạn bất cứ lúc nào.
- Effects và Music có thể để None: tự tạo hai AudioSource 2D riêng.
- Âm lượng dùng kênh SFX và Music trong Settings hiện có.
- Khi bắt đầu ending, nhạc nền gameplay được pause để không chồng lên nhạc credit. Khi về hub, nhạc nền được khôi phục/chuyển theo cấu hình level.

## Nội dung credit

Sửa **Ending Heading**, **Ending Message** và **Credits Text** trên PuzzlePresentation.

Credits Text hiện có các chỗ trống [Your name / team], [Your credits], [Asset creators], [Music and sound credits]. Thay bằng tên nhóm, vai trò và thông tin tác giả asset/nhạc thực tế của bạn. Dùng dòng trống để chia các mục.

## Luồng ending

Server ghi nhận bốn phòng riêng biệt, không phụ thuộc thứ tự. Hoàn thành lại cùng một phòng vẫn hiện thông báo và âm thanh chúc mừng, nhưng không tăng tiến độ.

Sau khi phòng thứ tư hoàn thành:
1. Chạy xong thông báo hoàn thành của phòng.
2. Hiện DIVIDED / TOGETHER AT LAST.
3. Khóa điều khiển nhân vật, mở con trỏ chuột và phát nhạc credit.
4. Credit cuộn từ dưới lên trong vùng cắt.
5. Skip credits bỏ qua phần cuộn và tới màn THANK YOU FOR PLAYING.
6. Return to hub đưa cả phiên chơi về hub.

Tiến độ được giữ trong phiên hiện tại, không lưu ra ổ đĩa. Về hub sau ending vẫn giữ bốn phòng đã hoàn thành; bắt đầu phiên mạng mới sẽ reset. Người mới kết nối khi phiên đang ending cũng nhận được trạng thái ending.

## Kiểm tra

- Vào lần lượt bốn cửa, kiểm tra đúng số và tên.
- Giải xong từng phòng: đúng thông báo và âm thanh, HUD vẫn hoạt động.
- Chết trên cầu kính: không hiện lại thông báo vào màn.
- Chơi lại một phòng đã giải: không được tính thành phòng mới.
- Giải đủ bốn phòng theo thứ tự bất kỳ: ending chỉ bắt đầu sau phòng cuối cùng.
- Kiểm tra Skip credits và Return to hub trên host/client.

Đã kiểm tra biên dịch C#, tham chiếu scene và logic tiến độ với thứ tự đảo, giải trùng, số puzzle không hợp lệ và reset phiên. Cần kiểm tra hình ảnh và đồng bộ thực tế trong Unity Play Mode.
