# Giải thích Tuần 5 (để Trung trả lời được) — mỗi việc 5–8 dòng + 4 câu hỏi thầy có thể hỏi

## Phần 0 — Kiểm lại Tuần 4

Trước khi làm việc mới, tôi tự kiểm lại mọi thứ Tuần 4 đã báo: đọc lại tài liệu phân công gốc, kiểm từng commit/file/tên test trong sổ minh chứng có thật không, chạy lại toàn bộ test trên DB rỗng (không dùng DB cũ để tránh "ăn theo" trạng thái đã có). Qua đó phát hiện PR của tôi (nội dung C4/C7) **đã được mở và đã merge** vào `main` từ 05/10, chỉ là chưa có ai review chính thức — khác với điều tôi tưởng ("chưa mở PR"). Cũng phát hiện hai lỗi tôi từng báo (DbSeeder ghi đè ảnh, `--migrate` báo giả) **đã được bạn trong nhóm sửa trên `main`**, chỉ là nhánh tôi đang làm chưa lấy bản sửa đó về.

**Câu hỏi 1: Vì sao coverage hôm nay (94,23%) khác báo cáo cũ (95,45%)?**
→ Chưa biết chắc nguyên nhân chênh lệch; có thể do đo ở commit khác hoặc điều kiện DB khác. Tôi ghi cả hai số, dùng số đo lại hôm nay (có DB rỗng, có file coverage thật) làm số chính thức, không che số cũ.

**Câu hỏi 2: Vì sao không tự sửa luôn 2 lỗi DbSeeder/--migrate nếu biết đã có bản sửa?**
→ Bản sửa nằm trên `origin/main`, nhánh làm việc của tôi chưa merge về. Việc hợp lý là merge `main` mới vào nhánh (sẽ làm ở bước code tiếp theo), không copy tay code của người khác vào giữa buổi báo cáo.

**Câu hỏi 3: Lỗi "--migrate báo thành công giả" em tái hiện bằng cách nào, có chắc không gây hại dữ liệu thật?**
→ Tôi đặt sai tên biến môi trường kết nối DB, nên lệnh migrate vô tình nhắm vào DB dev (`culinary_blog`) thay vì DB tạm của tôi. Lệnh thất bại ngay ở bước tạo bảng đầu tiên (bảng đã có từ trước) nên không ghi gì thêm. Tôi đã kiểm lại ngay: số dòng Recipes/Categories/Users trên `culinary_blog` không đổi, lịch sử migration cũng không đổi.

**Câu hỏi 4: PR bị merge mà không qua review của Tâm — ai chịu trách nhiệm, có vi phạm quy định nhóm không?**
→ Theo tài liệu phân công, mọi PR phải do Tâm review. PR #27 của tôi được merge bởi một bạn khác trong nhóm (không phải Tâm), không có review nào ghi nhận trên GitHub. Tôi không tự merge PR của mình nên không phải lỗi thao tác của tôi, nhưng tôi có trách nhiệm báo lại để nhóm biết và quyết định có cần review hồi tố không.

## T1 — Tự deploy & backup/restore

Tôi tạo một CSDL PostgreSQL trống, chạy lệnh migrate để sinh toàn bộ bảng, chạy lệnh seed để có 100 công thức mẫu, rồi dùng `pg_dump`/`pg_restore` (công cụ sao lưu chuẩn của PostgreSQL) để sao lưu, xoá sạch CSDL, và khôi phục lại — kiểm số dòng 5 bảng chính trước và sau khớp tuyệt đối. Phần "clone lại toàn bộ mã nguồn vào một thư mục mới" tôi chưa làm được vì máy hết RAM (chỉ còn dưới 0,5 GB trống suốt buổi), build lại một bộ mã nguồn thứ hai cùng lúc sẽ làm máy treo.

**Câu hỏi 1: Vậy "tự deploy" có thật sự được kiểm chứng không nếu chưa clone mới?**
→ Phần quan trọng nhất (DB rỗng → migrate → seed → API chạy được → backup/restore đúng) đã kiểm chứng thật trên đúng mã nguồn đang có. Phần clone lại là kiểm thêm "git có lưu đủ file không", rủi ro thấp hơn, để làm khi có máy khác hoặc nhiều RAM hơn.

**Câu hỏi 2: Backup bằng `pg_dump` có đủ cho một hệ thống thật không, hay cần gì thêm?**
→ Đủ cho một lần kiểm khôi phục dữ liệu. Hệ thống thật cần thêm: lịch backup tự động, backup tăng dần, kiểm thử khôi phục định kỳ, và không backup khi đang ghi dữ liệu (tôi backup lúc không có ai ghi, nên chưa kiểm trường hợp đó).

## T2 — Kiểm migration nâng cấp/hạ cấp

Tôi kiểm xem lịch sử các lần đổi cấu trúc bảng (migration) có áp được từ đầu đến cuối trên CSDL trống không (có — khớp 100%), và thử tạo tình huống dữ liệu "xấu" (một ảnh đã xoá nhưng vẫn đánh dấu là ảnh chính, cùng lúc có một ảnh khác đang sống cũng là ảnh chính) ở đúng mốc trước/sau một migration cụ thể để xem migration đó có chịu được dữ liệu xấu không khi đảo ngược. Kết quả: dữ liệu xấu đó **không thể tạo ra được** trước khi migration chạy (bị chặn ngay), chỉ xảy ra **sau** khi migration đã chạy — và nếu sau đó ai đó đảo ngược (hạ cấp) migration thì sẽ lỗi thật.

**Câu hỏi 1: Lỗi hạ cấp (Down) đó có nguy hiểm cho hệ thống thật không?**
→ Không, vì hệ thống thật chỉ nâng cấp (Up), không có ai chạy lệnh hạ cấp khi vận hành. Lỗi này chỉ lộ ra khi một người lập trình cố tình rollback bằng tay trên máy có dữ liệu — một tình huống hiếm, nhưng tôi vẫn ghi lại vì đề bài yêu cầu kiểm cả chiều đó.

**Câu hỏi 2: Vì sao không sửa luôn lỗi hạ cấp đó?**
→ File migration đó không phải do tôi viết (một bạn khác viết phần sửa chỉ mục ảnh), và nó không chặn luồng chạy bình thường nào — sửa vào sẽ cần hiểu rõ ý đồ gốc của người viết, nên tôi chỉ ghi lại hạn chế, để người viết gốc hoặc nhóm quyết định.

## T3 — Rà soát dữ liệu

Tôi chỉ chạy các câu lệnh `SELECT` (chỉ đọc, không sửa) để tìm các kiểu dữ liệu sai lệch đã biết hoặc có thể có: ảnh chính bị trùng, số thứ tự bước bị trùng/hụt, công thức đã xuất bản nhưng thiếu nguyên liệu/bước/ảnh. Trên CSDL sạch của tôi: không lỗi nào. Trên CSDL phát triển thật của nhóm (chỉ đọc, không đụng): có 4 ảnh xoá mềm còn giữ cờ "ảnh chính", và phát hiện thêm 6 công thức đã xuất bản nhưng hoàn toàn trống rỗng.

**Câu hỏi 1: 6 công thức rỗng đó có phải lỗi của em không?**
→ Không rõ ai tạo ra — có thể là dữ liệu test cũ từ trước khi có ràng buộc "phải có nguyên liệu/bước mới xuất bản được". Tôi chỉ phát hiện và báo lại, không tự xoá vì đó là CSDL phát triển chung của nhóm, có thể có người khác đang dùng dữ liệu đó để test.

**Câu hỏi 2: Vì sao không viết script tự xoá/sửa các dòng lỗi đó cho gọn?**
→ Vì đề bài yêu cầu rõ "chỉ đọc, không sửa" với CSDL dev, và sửa dữ liệu người khác không xin phép có thể làm hỏng việc đang test của người khác.

## T4 — Tài liệu Schema

Tôi dùng chính PostgreSQL (bảng hệ thống `information_schema`) để lấy ra đúng cấu trúc bảng/chỉ mục đang chạy thật, vẽ thành sơ đồ, rồi đối chiếu với tài liệu đặc tả yêu cầu (SRS) xem có khớp không. Phần khó nhất là xác nhận "sửa nguyên liệu có làm đổi mã phiên bản (RowVersion) của công thức cha không" — tôi không đoán mà tìm trong code (hàm thêm nguyên liệu không đụng gì tới công thức cha) và tìm thấy một bài test đã có sẵn, đang chạy xanh, khẳng định đúng điều đó.

**Câu hỏi 1: Vì sao thiết kế để sửa con không đổi RowVersion của cha — không phải thiếu sót sao?**
→ Là cố ý: để người dùng sửa nhiều nguyên liệu trong một "wizard" nhiều bước mà không phải tải lại dữ liệu công thức cha sau mỗi lần sửa. Đánh đổi: nếu hai người cùng sửa đúng một nguyên liệu, người lưu sau sẽ thắng (ghi đè), nhóm đã ghi nhận đánh đổi này.

**Câu hỏi 2: Chỉ mục tìm kiếm (GIN) của bạn Vĩ có vấn đề gì?**
→ Chỉ mục đó được tạo trên công thức "ghép chuỗi" Title+Description, nhưng câu lệnh tìm kiếm thật lại kiểm riêng từng cột — PostgreSQL không dùng được chỉ mục ghép chuỗi cho câu lệnh kiểm riêng từng cột, nên chỉ mục gần như không được dùng. Hiện tại dữ liệu ít nên không chậm thấy được, nhưng tôi báo lại cho nhóm biết trước khi dữ liệu lớn lên.

## T5 — Đo hiệu năng

Tôi dùng `EXPLAIN` của PostgreSQL để xem các câu lệnh truy vấn chính (xem công thức, xem danh sách, xem nguyên liệu/bước) có dùng đúng chỉ mục không, và dùng công cụ k6 (chạy trong Docker) để tạo tải giả 20 người dùng cùng lúc trong 30 giây gọi API xem chi tiết công thức, đo thời gian phản hồi.

**Câu hỏi 1: p95 = 40ms có nhanh không?**
→ Theo yêu cầu phi chức năng của dự án (NFR-PERF), ngưỡng p50 ≤ 150ms — 40ms (p95) còn tốt hơn cả ngưỡng p50, nên đạt yêu cầu ở quy mô 100 công thức. Chưa kiểm ở quy mô dữ liệu lớn hơn nhiều (ví dụ 10.000 công thức).

**Câu hỏi 2: Vì sao chỉ đo 1 lần, không đo 3 lần lấy trung vị như yêu cầu?**
→ Máy hết RAM giữa buổi, tôi ưu tiên có ít nhất một số đo thật hơn là cố đo 3 lần rồi máy treo mất hết số liệu. Đã ghi rõ đây là hạn chế thật, không giấu.

## T6 — E2E

Tôi chạy lại các bài kiểm tự động giả lập người dùng thật (mở trình duyệt, điền form, bấm nút) cho luồng tạo công thức. 8 trong 11 bài qua được; 3 bài còn lại thất bại vì tôi quên bật Docker (nơi chứa dịch vụ lưu ảnh) lúc đầu buổi. Sau khi bật lại, tôi xác nhận bằng cách gọi API trực tiếp rằng tải ảnh giờ hoạt động (không chạy lại 3 bài test đó bằng trình duyệt vì máy không đủ RAM để chạy trình duyệt + web + API cùng lúc nữa).

**Câu hỏi 1: Sao không coi là xong luôn vì đã có bằng chứng ảnh tải được?**
→ Gọi API trực tiếp và chạy đúng bài test tự động qua trình duyệt là hai việc khác nhau (test qua trình duyệt còn kiểm cả giao diện, nút bấm, có hiện đúng ảnh lên màn hình hay không). Tôi chỉ dám nói "nguyên nhân lỗi đã hết", chưa dám nói "3 bài test đó đã xanh", vì tôi chưa thực sự chạy lại chúng.

## T7 — Review kiểm thử bạn Vĩ (TV2)

Theo phân công, tôi đọc và chạy lại các bài test của bạn Vĩ (danh mục, tìm kiếm), không sửa code của bạn, chỉ ghi nhận xét. Phát hiện: bạn Vĩ có test tốt cho tìm kiếm tiếng Việt, nhưng thiếu test kiểm "chỉ Admin mới được sửa danh mục" ở mức gọi API thật.

**Câu hỏi: Review kiểu này có khách quan không, em có thiên vị gì không?**
→ Tôi chỉ liệt kê test có/thiếu dựa trên việc đọc code và chạy thật, không đưa ý kiến cá nhân về cách bạn Vĩ viết code, chỉ nêu "nếu thêm test X thì sẽ chắc hơn ở điểm Y" — giữ giọng trung lập như đề bài yêu cầu.

## T8 — Staging

Tôi chỉ kiểm cấu hình Docker Compose dành cho môi trường staging (do bạn Sơn viết) có đúng cú pháp và đủ phần cho công thức/ảnh hoạt động không (dùng lệnh `docker compose config` để kiểm, không build/chạy thật vì tốn RAM build 2 image). Kết quả: cấu hình đúng cú pháp, đường dẫn API tổng quát nên không cần sửa riêng cho phần công thức.

**Câu hỏi: Không build/chạy thật thì sao biết chắc nó chạy được?**
→ Không biết chắc 100% — chỉ biết cấu hình không có lỗi cú pháp/tham chiếu sai. Đây là hạn chế đã ghi rõ (BLOCKED RAM), không nhận vơ là đã kiểm đầy đủ.
