# Trả lời câu hỏi thảo luận — Chương 1

## 1. Ưu, nhược điểm Monolithic và khi nào nên chọn?

Monolithic đóng gói ứng dụng trong một đơn vị triển khai. Ưu điểm: khởi tạo nhanh, debug và kiểm thử luồng đầu cuối thuận tiện, giao dịch nội bộ đơn giản, ít chi phí vận hành. Nhược điểm: thay đổi nhỏ vẫn thường triển khai cả ứng dụng; các module dễ phụ thuộc chặt nếu tổ chức kém; phải nhân bản cả đơn vị triển khai khi scale; sự cố có thể ảnh hưởng nhiều tính năng.

Trong bối cảnh câu hỏi năm 2025, vẫn nên chọn cho MVP, nhóm nhỏ, ứng dụng nghiệp vụ quy mô vừa và miền nghiệp vụ chưa ổn định. Ví dụ Culinary Blog mới chỉ có nhóm 2–3 người: một modular monolith có ranh giới module rõ ràng thường hợp lý hơn microservices. Monolithic không đồng nghĩa code lộn xộn; vẫn có thể áp dụng Clean Architecture và Vertical Slice. API-Driven cũng không tự động làm backend scale theo service: cần tách đơn vị triển khai mới có đặc tính đó.

## 2. Dependency Rule và vì sao Application không import Infrastructure?

Phụ thuộc mã nguồn hướng từ ngoài vào trong: Domain độc lập; Application sử dụng Domain; Infrastructure hiện thực interface của Application; Presentation gọi use case. Nếu Application import DbContext cụ thể, gửi email cụ thể hoặc dịch vụ lưu file từ Infrastructure, nghiệp vụ bị gắn với chi tiết triển khai; thay provider, kiểm thử hoặc tái sử dụng đều khó hơn, thậm chí tạo vòng tham chiếu.

DIP giải quyết bằng abstraction: Application định nghĩa hợp đồng, Infrastructure hiện thực, DI nối hai bên lúc khởi chạy. Program.cs được phép làm composition root và biết implementation để đăng ký; endpoint không dùng DbContext cụ thể. Chính đề có điểm chưa nhất quán: trang 6 nói Presentation không phụ thuộc Infrastructure, nhưng trang 21–22 lại thêm reference này. Bài làm theo cách composition root của Case Study. Tương tự DbSet trong IApplicationDbContext vẫn là phụ thuộc EF Core; interface này chỉ loại bỏ phụ thuộc implementation cụ thể, không loại bỏ phụ thuộc framework.

## 3. CQRS so với Service pattern

Service pattern gom các thao tác theo đối tượng, ví dụ RecipeService.Create/Get/Update. Dễ học, ít file, phù hợp CRUD đơn giản. Khi số use case tăng, service có thể trở thành lớp quá lớn và nhiều phụ thuộc.

CQRS tách command thay đổi dữ liệu và query đọc dữ liệu; mỗi use case có request/handler riêng. MediatR định tuyến yêu cầu tới handler. CreateRecipe xử lý điều kiện tạo; GetRecipes tối ưu projection, filter và paging. CQRS thuận lợi khi đọc/ghi khác nhau nhiều, nghiệp vụ phức tạp hoặc có nhiều nhóm cùng phát triển. Với vài bảng CRUD và ít logic, chi phí file, dispatch và cấu hình có thể thành over-engineering. CQRS không bắt buộc hai database, microservices hay event sourcing; bài này dùng chung PostgreSQL.

## 4. Khi nào PUT, khi nào PATCH?

PUT dùng khi gửi toàn bộ trạng thái có thể chỉnh sửa của resource. Ví dụ PUT /api/v1/recipes/{id} gửi title, description, instructions, thời gian, servings, difficulty, categoryId và authorId. ID và timestamp do server quản lý. Gửi lại cùng payload phải cho cùng trạng thái mong muốn.

PATCH dùng khi chỉ muốn chỉnh một phần, ví dụ đổi servings thành 4 mà giữ nguyên các trường khác. Có thể dùng JSON Merge Patch `{"servings":4}` hoặc JSON Patch với operation replace. Phải quy định rõ content type, xử lý null và field được phép sửa. PATCH không được đảm bảo idempotent nói chung; phép gán cố định có thể idempotent, phép tăng số lượt xem thì không. Binding [FromBody] không tự đảm bảo tính idempotent; logic ứng dụng quyết định.

## 5. Cursor-based so với Offset-based pagination

Offset dùng page/size, hỗ trợ nhảy tới trang bất kỳ nhưng OFFSET lớn tốn công bỏ qua nhiều dòng. Khi thêm/xóa dòng giữa hai lần gọi, vị trí dịch chuyển có thể làm bỏ sót hoặc lặp dữ liệu.

Cursor dùng vị trí của phần tử cuối, chẳng hạn cặp (CreatedAt, Id), và truy vấn keyset với thứ tự tương ứng. Có index phù hợp thì hiệu quả trên tập lớn, ít bị ảnh hưởng bởi bản ghi mới chèn trước vị trí hiện tại. Phù hợp feed liên tục, infinite scroll, lịch sử giao dịch lớn. Không có yêu cầu kiến trúc nào bắt buộc mọi real-time feed phải dùng cursor; đây là lựa chọn phù hợp khi cần hiệu năng và hạn chế dịch chuyển trang. Cần thứ tự duy nhất, tiêu chí ổn định, cursor gắn với bộ lọc; cursor không tự bảo đảm snapshot khi bản ghi bị sửa/xóa. Bài tập này triển khai offset theo đề.

## 6. Scalar và Swagger UI trong .NET 10

Cả hai đều đọc tài liệu OpenAPI và cho phép gửi request. Có thể so sánh ba điểm cụ thể:

1. Tích hợp .NET: Scalar dùng Scalar.AspNetCore/MapScalarApiReference; Swagger UI thường qua Swashbuckle.AspNetCore.SwaggerUI/UseSwaggerUI. Cả hai đều dùng được tài liệu do AddOpenApi/MapOpenApi tạo.
2. Trải nghiệm hiển thị: Scalar ưu tiên bố cục tài liệu nhiều cột với ví dụ mã và API client; Swagger UI thường trình bày endpoint dạng danh sách mở rộng theo tag.
3. Tùy biến: Scalar cung cấp các theme và cấu hình client/target qua API riêng; Swagger UI có cấu hình layout, plugin và CSS riêng. Việc chọn phụ thuộc trải nghiệm mong muốn và hệ công cụ hiện có.

Scalar hợp với bài này vì giáo trình yêu cầu và tích hợp gọn với OpenAPI của ASP.NET Core. Không nên khẳng định .NET 10 bắt buộc/mặc định luôn có Scalar, hay Swagger UI bị loại bỏ: middleware UI được cài riêng. Việc một công cụ được ưa chuộng hơn không phải định luật kỹ thuật.

Nguồn chính thức: [Microsoft — OpenAPI overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0), [Scalar — ASP.NET Core integration](https://github.com/scalar/scalar/blob/main/documentation/integrations/aspnetcore/integration.md).

## 7. Vì sao DTO quan trọng?

DTO định nghĩa contract độc lập với cách lưu trữ. Client chỉ nhận các trường cần thiết; server có thể đổi entity mà vẫn giữ API cũ, hạn chế rò thông tin nội bộ và tránh vòng lặp serialization qua navigation properties. Request DTO còn ngăn client gán trường hệ thống như CreatedAt hoặc trường đặc quyền ngoài ý muốn.

Trả entity trực tiếp làm client phụ thuộc schema nội bộ, dễ lộ trường không nên công khai và có thể tải quá nhiều dữ liệu. RecipeDto cho danh sách không chứa Instructions/Category đầy đủ; RecipeDetailDto có Instructions và CategoryDto để phục vụ màn hình chi tiết. Mapster projection chọn các cột cần thiết trong query EF. Mapster mapping không thay thế validation hoặc authorization.

## 8. Thiết kế URI cho Recipe, Category, Comment, User, RecipeImage

| Resource | Collection / item | Quan hệ và lý do |
|---|---|---|
| Recipe | /api/v1/recipes ; /api/v1/recipes/{recipeId} | Công thức có danh tính riêng, lọc danh mục bằng query |
| Category | /api/v1/categories ; /api/v1/categories/{categoryId} | Danh mục là tài nguyên độc lập |
| Recipe thuộc Category | /api/v1/categories/{categoryId}/recipes | Thể hiện tập công thức của danh mục, có phân trang |
| Comment | /api/v1/recipes/{recipeId}/comments ; /api/v1/recipes/{recipeId}/comments/{commentId} | Bình luận thuộc một công thức; server kiểm tra quan hệ cha/con |
| User | /api/v1/users ; /api/v1/users/{userId} | Người dùng độc lập; quyền xem/sửa cần kiểm soát |
| Recipe của User | /api/v1/users/{userId}/recipes | Danh sách công thức theo tác giả |
| RecipeImage | /api/v1/recipes/{recipeId}/images ; /api/v1/recipes/{recipeId}/images/{imageId} | Ảnh phụ thuộc công thức; metadata và upload thống nhất theo resource |

Collection dùng GET để liệt kê, POST để tạo; item dùng GET/PUT/PATCH/DELETE theo nghiệp vụ và quyền được cấp. Không dùng URI như /createRecipe hoặc /deleteComment. Nếu ảnh lưu bên ngoài, response có URL ảnh, còn URI images quản lý tài nguyên metadata/upload. Tránh lồng quá sâu như categories/.../recipes/.../comments vì Recipe đã có ID riêng. Chương 1 hiện thực Category, Recipe và nested GET; Comment/User/RecipeImage là thiết kế cho câu hỏi, chưa phải chức năng được yêu cầu trong phần thực hành.
