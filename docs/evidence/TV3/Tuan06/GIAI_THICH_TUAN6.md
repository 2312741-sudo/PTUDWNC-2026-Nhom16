# Giải thích K18 + Tuần 6 (để Trung trả lời được) — mỗi việc 5–8 dòng + 4 câu hỏi kèm đáp án

## Phần A — K18

Tôi không tin vào log NVDA cũ của Trung mà tự kiểm lại bằng cách viết một bài test Playwright MỚI, cho trình duyệt thật đọc tên các phần tử (không phải tôi tự đoán tên dựa trên code). Kết quả: cả 3 lỗi NVDA từng ghi nhận (ô Mô tả đọc lẫn số đếm, nút Sửa dính chữ, liên kết Sửa không kèm tên) đều **đã đúng** trong code hiện tại — rất có thể log cũ của Trung chụp ở bản build trước khi 3 lỗi đó được sửa. Tôi cũng nhân dịp đo thêm phần chưa ai đo: chụp ảnh 320/768/1200 cho cả 5 bước VÀ trang dashboard — phát hiện dashboard thật sự cuộn ngang ở điện thoại nhỏ (320px), lỗi nằm ngoài các file của tôi.

**Câu hỏi 1: Sao không tự đánh dấu K18 xong nếu 5/5 test mới đều xanh?**
→ Test tự động chỉ chứng minh được "tên phần tử đúng theo chuẩn kỹ thuật" (accessible name API), không chứng minh được NVDA (trình đọc màn hình thật) đọc ra đúng như vậy bằng giọng nói — hai thứ gần nhau nhưng không phải lúc nào cũng giống 100% (ví dụ tốc độ đọc, ngắt câu). Tiêu chí gốc yêu cầu nghe NVDA thật, nên tôi giữ đúng yêu cầu, không tự nới tiêu chí.

**Câu hỏi 2: Lỗi cuộn ngang dashboard ở 320px nghiêm trọng tới đâu?**
→ Chỉ ảnh hưởng người dùng điện thoại rất nhỏ (320px, ví dụ iPhone SE cỡ cũ) xem trang quản lý công thức của mình — không ảnh hưởng trang công khai, không mất dữ liệu, chỉ gây khó dùng (phải cuộn ngang để thấy hết bảng). Tôi đã thử sửa trong đúng phạm vi file của mình trước (không hiệu quả) rồi mới xác định lỗi nằm ở phần cấu trúc trang chung (layout) do bạn khác viết, nên không tự sửa tiếp.

## Phần B — Tuần 6

### B1-B2: Đọc phân công + chọn nhánh

Tôi đọc lại đúng tài liệu phân công gốc trên nhánh `main` (không dùng bản tôi nhớ hoặc bản cũ) để chắc mục tuần 6 của tôi là gì: làm hồi quy (regression), gom lại toàn bộ bằng chứng (evidence) đã làm, chuẩn bị để tự giải thích khi demo, và bàn giao tài liệu test/DB cho người khác dùng lại được. Vì mọi việc này cần dùng đúng những gì tôi vừa sửa ở K18 (chưa merge vào `main`), tôi tạo một nhánh mới nối tiếp từ nhánh đang làm, không tạo từ `main` (sẽ thiếu các sửa đổi mới).

**Câu hỏi: Vì sao không merge thẳng vào `main` rồi làm tuần 6 trên `main` luôn cho đỡ rối nhánh?**
→ Vì PR của tuần 5 (nhánh trước) chưa có ai review/merge — tôi không tự merge PR của mình (đúng luật của nhóm, PR phải qua Tâm), nên phải tạo nhánh tiếp nối thay vì merge tay.

### W3: Regression

Tôi chạy lại TOÀN BỘ các loại test đã có (backend .NET, kiểm định dạng code, kiểm kiểu TypeScript, test giao diện Jest, test tự động hoá trình duyệt Playwright) trên đúng bản code mới nhất (đã có sửa K18), xem có việc sửa nào vô tình làm hỏng việc khác không. Kết quả: không có gì hỏng — mọi con số khớp với trước khi sửa (421 + 5 backend, 85 Jest, thêm 6 test Playwright mới của K18 đều xanh).

**Câu hỏi: "Regression" khác "test thường" ở điểm nào?**
→ Test thường chỉ kiểm tính năng VỪA viết. Regression là chạy lại HẾT mọi test cũ + mới cùng lúc, để chắc chắn tính năng vừa sửa không âm thầm làm hỏng một tính năng khác đã từng chạy đúng trước đó — đây là lý do phải chạy lại toàn bộ, không chỉ chạy riêng phần K18.

### W5: Tài liệu tự giải thích

Tôi viết lại bằng lời đơn giản 5 khái niệm kỹ thuật hay bị hỏi (aggregate, owned entity, Unit of Work, RowVersion, phiên bản schema) kèm sẵn 4 câu hỏi+đáp hay gặp, để khi thầy hoặc Tâm hỏi bất ngờ lúc demo, tôi có sẵn câu trả lời ngắn gọn thay vì phải đọc lại tài liệu kỹ thuật dài (ADR) ngay lúc đó.

**Câu hỏi: Nếu thầy hỏi một câu không có trong tài liệu này thì sao?**
→ Tài liệu này chỉ là phần "hay hỏi nhất", không phải toàn bộ kiến thức — phần chi tiết đầy đủ vẫn nằm ở hai file ADR (`ADR-0001`, `ADR-0002`) và `SCHEMA_RECIPE.md`, tôi đọc lại ba file đó trước khi demo để chuẩn bị kỹ hơn.

### W6: Bàn giao test + DB docs

Tôi viết một trang tổng hợp "nếu ai đó (Tâm, thầy, hoặc tôi 3 tháng sau) muốn chạy lại mọi test và hiểu cấu trúc database của tôi thì bắt đầu từ đâu" — gồm đúng lệnh chạy lại từng loại test kèm số thật gần nhất, và danh sách mọi việc còn vướng mắc với người khác (6 handoff) để không ai phải lục lại toàn bộ lịch sử chat/commit mới hiểu.

**Câu hỏi: Vì sao không gộp luôn vào README chính của dự án cho dễ tìm?**
→ README chính do nhóm dùng chung, nhiều phần của nhiều người — tài liệu bàn giao cá nhân nên để trong thư mục evidence riêng của tôi, tránh nhóm phải merge xung đột khi nhiều người cùng sửa README.
