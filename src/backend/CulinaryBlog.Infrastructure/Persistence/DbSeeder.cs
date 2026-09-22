using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>
/// Bộ sinh dữ liệu mẫu đầy đủ phục vụ Lab 2 và kiểm thử hệ thống:
/// Đảm bảo tối thiểu: 25 Categories, 100 Recipes (mỗi recipe >= 10 nguyên liệu, >= 5 bước chế biến).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        // Kiểm tra nếu đã có đủ 100 recipes thì bỏ qua
        if (await db.Recipes.IgnoreQueryFilters().CountAsync(ct) >= 100) return;

        // 1. Roles
        foreach (var role in new[] { "Guest", "Author", "Admin" })
        {
            if (!await db.Roles.AnyAsync(r => r.Name == role, ct))
                db.Roles.Add(new IdentityRole(role) { NormalizedName = role.ToUpperInvariant() });
        }

        // 2. Authors (5 tác giả ẩm thực)
        var authors = new List<RecipeAuthorUser>();
        var authorInfo = new[]
        {
            ("tam.nguyen@culinary.local", "Nguyễn Thanh Tâm"),
            ("vi.ngo@culinary.local", "Ngô Quốc Trường Vĩ"),
            ("trung.huynh@culinary.local", "Huỳnh Quốc Trung"),
            ("son.nguyen@culinary.local", "Nguyễn Hữu Trung Sơn"),
            ("masterchef@culinary.local", "Bếp Trưởng Culinary")
        };

        foreach (var (email, name) in authorInfo)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user == null)
            {
                user = NewUser(email, name);
                db.Users.Add(user);
            }
            authors.Add(user);
        }
        await db.SaveChangesAsync(ct);

        // 3. Categories (25 danh mục)
        var catDefs = new (string Name, string Slug, string Desc)[]
        {
            ("Món khai vị", "mon-khai-vi", "Các món khai vị nhẹ nhàng, kích thích vị giác đầu bữa ăn."),
            ("Món chính", "mon-chinh", "Các món ăn chính giàu dinh dưỡng cho bữa cơm gia đình."),
            ("Món canh & súp", "mon-canh-sup", "Các món canh, súp thanh mát, đậm đà hương vị truyền thống."),
            ("Món xào", "mon-xao", "Các món xào thơm ngon, giữ trọn độ giòn ngọt của rau củ và thịt."),
            ("Món kho", "mon-kho", "Các món kho đậm đà, màu sắc bắt mắt, hao cơm."),
            ("Món nướng", "mon-nuong", "Các món nướng thơm lừng với nước sốt ướp đặc trưng."),
            ("Món lẩu", "mon-lau", "Các món lẩu nghi ngút khói, thích hợp cho tụ họp bạn bè, gia đình."),
            ("Món chiên & rán", "mon-chien-ran", "Các món chiên giòn rụm bên ngoài, mềm ngọt bên trong."),
            ("Món hấp", "mon-hap", "Các món hấp thanh đạm, lưu giữ nguyên vẹn dưỡng chất."),
            ("Món gỏi & nộm", "mon-goi-nom", "Các món gỏi chua ngọt, cay nhẹ giòn mát."),
            ("Món cuốn", "mon-cuon", "Các món cuốn tươi mát chấm kèm nước chấm pha chuẩn vị."),
            ("Món bún, phở & mì", "mon-bun-pho-mi", "Các món nước, bún phở đặc sản ba miền Việt Nam."),
            ("Món cháo", "mon-chao", "Các món cháo dinh dưỡng, thơm bùi, dễ tiêu hóa."),
            ("Món chay thanh tịnh", "mon-chay-thanh-tinh", "Các món chay tốt cho sức khỏe từ nấm và rau củ tươi."),
            ("Món bánh truyền thống", "mon-banh-truyen-thong", "Các loại bánh dân gian đậm đà bản sắc quê hương."),
            ("Bánh ngọt & tráng miệng", "banh-ngot-trang-mieng", "Bánh ngọt phương Tây và món tráng miệng hấp dẫn."),
            ("Món chè", "mon-che", "Các món chè ngọt mát, giải nhiệt ngày hè."),
            ("Đồ uống & trà", "do-uong-tra", "Trà trái cây, trà thảo mộc và thức uống pha chế."),
            ("Sinh tố & nước ép", "sinh-to-nuoc-ep", "Nước ép và sinh tố tươi giàu vitamin."),
            ("Hải sản tươi sống", "hai-san-tuoi-song", "Các món hải sản tôm, cua, cá, mực chế biến đa dạng."),
            ("Món thịt bò", "mon-thit-bo", "Các món ngon hảo hạng từ thịt bò tươi."),
            ("Món thịt gà", "mon-thit-ga", "Các món ăn quen thuộc từ thịt gà thả vườn."),
            ("Món thịt heo", "mon-thit-heo", "Các món chế biến từ thịt heo thơm ngon mỗi ngày."),
            ("Món ăn sáng", "mon-an-sang", "Các món ăn sáng nhanh gọn, cung cấp năng lượng ngày mới."),
            ("Món ăn vặt đường phố", "mon-an-vat-duong-pho", "Các món ăn vặt được giới trẻ yêu thích."),
        };

        var catList = new List<Category>();
        int order = 1;
        foreach (var (name, slug, desc) in catDefs)
        {
            var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Slug == slug, ct);
            if (cat == null)
            {
                cat = Category.Create(name, slug, desc, $"https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&auto=format&fit=crop", orderIndex: order++);
                db.Categories.Add(cat);
            }
            catList.Add(cat);
        }
        await db.SaveChangesAsync(ct);

        // 4. Recipes (100 công thức chi tiết, mỗi recipe >= 10 nguyên liệu, >= 5 bước)
        var recipeDefs = new (string Title, string Slug, int CatIdx, string Desc)[]
        {
            ("Phở Bò Tái Nạm Hà Nội", "pho-bo-tai-nam-ha-noi", 11, "Phở bò Hà Nội với nước dùng trong veo, thơm nức mùi hoa hồi quế, thịt bò mềm ngọt."),
            ("Bún Bò Huế Cố Đô", "bun-bo-hue-co-do", 11, "Bún bò Huế đậm đà thơm mùi sả ruốc, thịt bắp hoa giòn ngon và chả cua béo ngậy."),
            ("Cơm Tấm Sườn Bì Chả", "com-tam-suon-bi-cha", 1, "Cơm tấm hạt dẻo thơm, sườn nướng mỡ hành đậm đà, bì thơm và chả trứng hấp mềm."),
            ("Bánh Xèo Tôm Nhảy Miền Tây", "banh-xeo-tom-nhay-mien-tay", 7, "Vỏ bánh xèo vàng ươm giòn rụm, nhân tôm đất tươi sống, giá đỗ và thịt ba rọi."),
            ("Gỏi Cuốn Tôm Thịt", "goi-cuon-tom-thit", 10, "Gỏi cuốn bánh tráng thanh mát, nhân tôm tươi, thịt ba rọi luộc và rau thơm chấm tương đen."),
            ("Bánh Mì Thịt Nướng Sốt Tiêu", "banh-mi-thit-nuong-sot-tieu", 23, "Bánh mì vỏ giòn tan, nhân thịt nướng thơm lừng sốt tiêu đen đậm đà kèm đồ chua giòn ngọt."),
            ("Canh Chua Cá Lóc Đồng", "canh-chua-ca-loc-dong", 2, "Canh chua cá lóc nấu bông điên điển, bắp chuối, me chua dịu và ngò gai thơm ngát."),
            ("Thịt Kho Tàu Trứng Vịt", "thit-kho-tau-trung-vit", 4, "Món thịt kho rục nước dừa xiêm, miếng thịt trong veo mềm tan ăn cùng cơm nóng."),
            ("Gà Nướng Muối Ớt Tây Bắc", "ga-nuong-muoi-ot-tay-bac", 5, "Thịt gà đồi nướng than hoa da giòn rụm, cay nồng ớt xiêm rừng và mắc khén đậm vị."),
            ("Cá Hồi Áp Chảo Sốt Bơ Chanh", "ca-hoi-ap-chao-sot-bo-chanh", 1, "Cá hồi phi lê áp chảo béo ngậy, sốt bơ chanh vàng óng chua nhẹ cùng măng tây giòn."),
            ("Bún Chả Hà Nội Truyền Thống", "bun-cha-ha-noi-truyen-thong", 11, "Chả viên và chả miếng nướng than hoa thơm lừng, chấm nước mắm đu đủ chua ngọt thanh tao."),
            ("Bò Kho Bánh Mì Đậm Đà", "bo-kho-banh-mi-dam-da", 20, "Bò kho gân mềm dẻo, nước sốt sánh mịn đậm đà hương hoa hồi, sả cây chấm bánh mì nóng giòn."),
            ("Chả Cá Lã Vọng", "cha-ca-la-vong", 19, "Chả cá lăng tẩm ướp nghệ tây chiên trên chảo nóng cùng thì là, hành hoa chấm mắm tôm."),
            ("Lẩu Thái Hải Sản Chua Cay", "lau-thai-hai-san-chua-cay", 6, "Nước lẩu Tom Yum chua cay tê tái ngập tràn tôm sú, mực tươi, nghêu và nấm kim châm."),
            ("Lẩu Gà Lá É Phú Yên", "lau-ga-la-e-phu-yen", 6, "Thịt gà ta giòn ngọt nấu cùng măng chua và lá é tươi cay nồng ấm bụng ngày mưa."),
            ("Mực Xào Sa Tế Cay Nồng", "muc-xao-sa-te-cay-nong", 3, "Mực ống tươi giòn sần sật xào cùng sa tế tôm, hành tây và ớt chuông đậm đà bắt mắt."),
            ("Tôm Rim Nước Cốt Dừa", "tom-rim-nuoc-cot-dua", 4, "Tôm đất bóc vỏ rim cùng nước cốt dừa sánh mịn, vị béo ngọt mặn mà ăn cùng cơm trắng."),
            ("Cơm Chiên Dương Châu Hải Sản", "com-chien-duong-chau-hai-san", 1, "Hạt cơm tơi vàng óng chiên cùng tôm, lạp xưởng, đậu Hà Lan và trứng béo ngậy."),
            ("Sườn Xào Chua Ngọt", "suon-xao-chua-ngot", 3, "Sườn heo non mềm thấm đẫm sốt chua ngọt cà chua, giấm bỗng và ớt chuông."),
            ("Bò Lúc Lắc Khoai Tây Chiên", "bo-luc-lac-khoai-tay-chien", 20, "Thịt thăn bò mềm ngọt xào lửa lớn cùng bơ tỏi, hành tây và khoai tây chiên giòn rụm."),
            ("Nem Rán Hà Nội Giòn Rụm", "nem-ran-ha-noi-gion-rum", 7, "Nem rán truyền thống nhân thịt băm, mộc nhĩ, miến dong, vỏ giòn rụm chấm nước mắm tỏi ớt."),
            ("Canh Cua Rau Đay Mướp", "canh-cua-rau-day-muop", 2, "Canh cua đồng gạch nổi váng vàng ươm nấu cùng rau đay mồng tơi và mướp hương thanh mát."),
            ("Thịt Ba Chỉ Luộc Chấm Mắm Tôm", "thit-ba-chi-luoc-cham-mam-tom", 22, "Thịt ba chỉ giòn bì luộc vừa chín tới, thái mỏng chấm mắm tôm chanh ớt sủi bọt."),
            ("Cá Kho Tộ Miền Tây", "ca-kho-to-mien-tay", 4, "Cá lóc hoặc cá basa kho tộ đất, nước màu đường thốt nốt sánh đặc, tiêu sọ cay nồng."),
            ("Gà Hấp Lá Chanh", "ga-hap-la-chanh", 8, "Gà ta hấp cách thủy da vàng óng mượt mà, thơm lừng hương lá chanh thái chỉ chấm muối tiêu chanh."),
            ("Vịt Om Sấu Hà Nội", "vit-om-sau-ha-noi", 2, "Thịt vịt mềm ngọt nấu cùng quả sấu xanh chua thanh, khoai sọ bùi béo và rau muống giòn."),
            ("Bún Đậu Mắm Tôm Thập Cẩm", "bun-dau-mam-tom-thap-cam", 11, "Mẹt bún lá, đậu mơ chiên phồng giòn rụm, chả cốm, nem rán và mắm tôm chuẩn vị Thanh Hóa."),
            ("Bánh Canh Cua Giò Heo", "banh-canh-cua-gio-heo", 11, "Sợi bánh canh bột lọc dai dẻo, thịt cua biển ngọt lịm và giò heo hầm mềm trong nước dùng sệt."),
            ("Bún Riêu Cua Đồng", "bun-rieu-cua-dong", 11, "Nước dùng bún riêu chua dịu từ giấm bỗng, gạch cua béo ngậy ăn kèm đậu hũ chiên và huyết luộc."),
            ("Mì Quảng Tôm Thịt Đà Nẵng", "mi-quang-tom-thit-da-nang", 11, "Sợi mì vàng ươm chan xăm xắp nước nhưn tôm thịt, rắc đậu phộng rang và bánh tráng mè nướng."),
            ("Cao Lầu Hội An", "cao-lau-hoi-an", 11, "Đặc sản phố cổ với sợi cao lầu dai giòn nước tro, xá xíu đậm đà, tóp mỡ chiên và rau sống Trà Quế."),
            ("Hủ Tiếu Nam Vang", "hu-tieu-nam-vang", 11, "Hủ tiếu sợi dai nước dùng xương ngọt thanh, tôm sú, thịt băm, gan heo và trứng cút bùi béo."),
            ("Bánh Cuốn Nóng Cà Cuống", "banh-cuon-nong-ca-cuong", 14, "Vỏ bánh tráng mỏng tang mướt mát, nhân thịt mộc nhĩ thơm phức thoang thoảng giọt tinh dầu cà cuống."),
            ("Bánh Bèo Chén Miền Trung", "banh-beo-chen-mien-trung", 14, "Bánh bèo đúc chén nhỏ xinh, nhân tôm chấy đỏ cam, mỡ hành xanh mướt và tóp mỡ giòn rụm."),
            ("Bánh Nậm Huế", "banh-nam-hue", 14, "Bánh nậm gói lá chuối xanh ngắt, bột gạo mềm mịn ôm lấy nhân tôm thịt đậm đà tan ngay trong miệng."),
            ("Bánh Bột Lọc Tôm Thịt", "banh-bot-loc-tom-thit", 14, "Vỏ bột lọc trong veo thấy rõ con tôm đỏ au và miếng thịt mỡ bên trong, chấm nước mắm ớt cay xé."),
            ("Gỏi Ngó Sen Tôm Thịt", "goi-ngo-sen-tom-thit", 9, "Ngó sen giòn sần sật trộn cùng tôm sú luộc, thịt ba rọi thái mỏng và nước mắm chua ngọt."),
            ("Nộm Hoa Chuối Tai Heo", "nom-hoa-chuoi-tai-heo", 9, "Hoa chuối thái mỏng ngâm trắng giòn, tai heo luộc thái sợi sần sật trộn rau răm và đậu phộng."),
            ("Gỏi Bò Bóp Thấu", "goi-bo-bop-thau", 9, "Thịt bắp bò tái chanh chua ngọt trộn cùng chuối chát, khế chua, hành tây và mè rang thơm."),
            ("Chả Giò Hải Sản Sốt Mayonnaise", "cha-gio-hai-san-sot-mayonnaise", 7, "Vỏ rế chiên vàng giòn rụm, nhân tôm mực ngọt lịm trộn sốt kem béo ngậy tan chảy."),
            ("Lẩu Nấm Chay Thanh Đạm", "lau-nam-chay-thanh-dam", 13, "Nồi lẩu thanh mát từ nước dùng rau củ quả ngập tràn các loại nấm tươi linh chi, nấm đùi gà, nấm rơm."),
            ("Đậu Hũ Tứ Xuyên", "dau-hu-tu-xuyen", 1, "Đậu hũ non mềm mượt sốt thịt băm cay tê sa tế dầu ớt và tiêu Tứ Xuyên trứ danh."),
            ("Cà Tím Nướng Mỡ Hành", "ca-tim-nuong-mo-hanh", 5, "Cà tím nướng than hoa thơm nức mũi, xối mỡ hành béo ngậy và rưới nước mắm tỏi ớt chua cay."),
            ("Nấm Đùi Gà Kho Tiêu", "nam-dui-ga-kho-tieu", 13, "Nấm đùi gà dai ngọt kho đậm đà nước màu dừa và hạt tiêu xanh cay nồng đưa cơm."),
            ("Rau Muống Xào Tỏi", "rau-muong-xao-toi", 3, "Rau muống ngọn non xanh mướt xào lửa lớn cùng tỏi đập dập thơm lừng giòn sần sật."),
            ("Bông Bí Xào Thịt Bò", "bong-bi-xao-thit-bo", 3, "Bông bí vàng ươm ngọt lịm xào vừa chín tới cùng thịt bò thăn mềm ngọt đậm đà."),
            ("Canh Rong Biển Thịt Băm", "canh-rong-bien-thit-bam", 2, "Canh rong biển đậu hũ non thịt băm thanh nhẹ mát lành chuẩn phong cách ẩm thực gia đình."),
            ("Canh Kim Chi Thịt Heo", "canh-kim-chi-thit-heo", 2, "Kim chi muối cay nồng nấu cùng thịt ba chỉ béo ngọt và đậu hũ trắng mềm thơm cay ấm bụng."),
            ("Cháo Sườn Quẩy Nóng", "chao-suon-quay-nong", 12, "Bát cháo sườn xay mịn như kem, sườn sụn giòn sần sật ăn kèm ruốc thịt và quẩy nóng giòn."),
            ("Cháo Cá Lóc Rau Đắng", "chao-ca-loc-rau-dang", 12, "Cháo cá lóc miền Tây hạt gạo rang thơm, cá ngọt thịt ăn cùng đĩa rau đắng tươi mát."),
            ("Cháo Gà Hạt Sen", "chao-ga-hat-sen", 12, "Cháo gà ta hầm cùng hạt sen Huế bùi béo, nấm hương thơm ngát tẩm bổ cho cả gia đình."),
            ("Cháo Yến Mạch Tôm Thịt", "chao-yen-mach-tom-thit", 12, "Cháo yến mạch nguyên cám nấu cùng tôm tươi và thịt băm giàu chất xơ và đạm cho người ăn kiêng."),
            ("Bánh Flan Caramel Béo Ngậy", "banh-flan-caramel-beo-ngay", 15, "Bánh flan trứng sữa mềm mịn mượt mà không chút rỗ khí, sốt caramel đắng nhẹ thơm cà phê."),
            ("Chè Bưởi An Giang", "che-buoi-an-giang", 16, "Cùi bưởi giòn sần sật khử hết đắng nấu cùng đỗ xanh bùi bở và nước cốt dừa béo ngậy."),
            ("Chè Khúc Bạch Hạnh Nhân", "che-khuc-bach-hanh-nhan", 16, "Từng viên khúc bạch phô mai dẻo mềm tan trong miệng cùng nước đường phèn nhãn ngọt thanh và hạnh nhân lát."),
            ("Chè Hạt Sen Long Nhãn", "che-hat-sen-long-nhan", 16, "Món chè cung đình thanh nhã, hạt sen bùi lồng khéo léo trong cùi nhãn ngọt giòn nước đường phèn."),
            ("Chè Sương Sa Hạt Lựu", "che-suong-sa-hat-luu", 16, "Chè ba màu rực rỡ với hạt lựu củ năng giòn sần sật, sương sa mát lạnh và nước cốt dừa thơm béo."),
            ("Sữa Chua Nếp Cẩm Mộc Châu", "sua-chua-nep-cam-moc-chau", 15, "Sữa chua lên men tự nhiên mát lạnh ăn cùng nếp cẩm dẻo bùi thơm mùi men rượu nếp."),
            ("Rau Câu Dừa Trái Cây", "rau-cau-dua-trai-cay", 15, "Thạch rau câu giòn mát làm từ nước dừa tươi nguyên chất ôm trọn các loại trái cây nhiệt đới thanh mát."),
            ("Trà Đào Cam Sả", "tra-dao-cam-sa", 17, "Trà đen ủ thơm lừng hòa quyện cùng vị ngọt thơm của miếng đào ngâm giòn và cam tươi sả ấm áp."),
            ("Trà Vải Hoa Hồng", "tra-vai-hoa-hong", 17, "Trà lài ngát hương quyện cùng quả vải ngâm ngọt mọng và siro hoa hồng thanh tao quyến rũ."),
            ("Trà Mãng Cầu Xiêm", "tra-mang-cau-xiem", 17, "Món trà hot hit với thịt mãng cầu tươi dầm ngọt chua thanh dịu kết hợp trà olong thơm ngát."),
            ("Sinh Tố Bơ Đắk Lắk", "sinh-to-bo-dak-lak", 18, "Trái bơ sáp Đắk Lắk dẻo quánh xay mịn cùng sữa tươi và sữa đặc béo ngậy thơm ngon bổ dưỡng."),
            ("Nước Ép Cần Tây Táo Xanh", "nuoc-ep-can-tay-tao-xanh", 18, "Nước ép detox thanh lọc cơ thể từ cần tây tươi giòn, táo xanh chua dịu và chút gừng ấm áp."),
            ("Bánh Mì Chảo Thập Cẩm", "banh-mi-chao-thap-cam", 23, "Chảo gang xèo xèo pate rán thơm, trứng ốp la lòng đào, xúc xích và sốt cà chua sánh mịn ăn kèm bánh mì."),
            ("Xôi Xéo Gà Xé", "xoi-xeo-ga-xe", 23, "Hạt xôi nếp cái hoa vàng óng ả xối mỡ hành rắc đậu xanh thái mỏng và thịt gà đồi xé phay giòn dai."),
            ("Bánh Bao Nhân Thịt Trứng Cút", "banh-bao-nhan-thit-trung-cut", 14, "Vỏ bánh bao trắng ngần bông xốp thơm mùi sữa, nhân thịt nạc băm mộc nhĩ và trứng cút bùi béo."),
            ("Bánh Giò Nóng Hà Nội", "banh-gio-nong-ha-noi", 14, "Bánh giò mềm mịn núng nính nóng hổi trong lớp lá chuối, nhân thịt mộc nhĩ thơm ngậy ăn cùng giò chả."),
            ("Xôi Bắp Mỡ Hành", "xoi-bap-mo-hanh", 23, "Xôi bắp hầm dẻo bùi hạt nếp dẻo thơm rưới mỡ hành xanh mướt và rắc hành phi giòn rụm."),
            ("Cơm Rang Dưa Bò", "com-rang-dua-bo", 1, "Cơm rang vàng giòn đảo đều cùng dưa cải chua giòn sần sật và thịt bắp bò xào đậm vị tỏi."),
            ("Mì Xào Bò Rau Cải", "mi-xao-bo-rau-cai", 3, "Sợi mì trứng dai vàng xào lửa lớn cùng thịt thăn bò ướp dầu hào và rau cải ngọt xanh giòn."),
            ("Bún Thịt Nướng Chả Giò", "bun-thit-nuong-cha-gio", 11, "Tô bún tươi mát với thịt nướng mè thơm ngậy, chả giò giòn rụm, đồ chua và nước mắm ớt pha tỏi."),
            ("Bánh Khọt Vũng Tàu", "banh-khot-vung-tau", 7, "Chiếc bánh khọt tròn xoe giòn rụm viền bánh, nhân tôm tươi nguyên con xối mỡ hành rắc bột tôm đỏ."),
            ("Bánh Căn Phan Thiết", "banh-can-phan-thiet", 7, "Bánh căn nướng khuôn đất xốp mềm, nhân mực tôm tươi rói chấm ngập bát nước mắm cá kho đậm đà."),
            ("Ốc Hương Xào Bơ Tỏi", "oc-huong-xao-bo-toi", 19, "Ốc hương biển tươi giòn ngọt xào đẫm sốt bơ tỏi thơm lừng chấm bánh mì đặc ruột."),
            ("Càng Ghẹ Rang Muối Kéo Chỉ", "cang-ghe-rang-muoi-keo-chi", 19, "Càng ghẹ chắc nịch phủ lớp muối ớt cay xè kéo chỉ đỏ au hấp dẫn đầu ngón tay."),
            ("Nghêu Hấp Sả Gừng", "ngheu-hap-sa-gung", 8, "Nghêu tươi sống hấp lửa lớn cùng sả cây đập dập, gừng tươi thơm nức ngọt trọn từng giọt nước."),
            ("Sò Huyết Xào Tỏi", "so-huyet-xao-toi", 19, "Sò huyết đầm ngọt thịt xào vừa chín tới cùng tóp mỡ béo ngậy và tỏi phi giòn tan."),
            ("Hàu Nướng Phô Mai", "hau-nuong-pho-mai", 5, "Hàu sữa tươi béo múp nướng trên than hoa phủ ngập sốt phô mai kéo sợi thơm phức."),
            ("Tôm Nướng Muối Ớt", "tom-nuong-muoi-ot", 5, "Tôm sú biển tẩm ướp muối hột ớt hiểm nướng vàng rực vỏ giòn thịt ngọt săn chắc."),
            ("Cá Tai Tượng Chiên Xù", "ca-tai-tuong-chien-xu", 7, "Cá tai tượng chiên xù vảy dựng đứng giòn tan, cuốn bánh tráng rau rừng chấm mắm nêm đậm đà."),
            ("Bò Né Hoa Tuyết", "bo-ne-hoa-tuyet", 20, "Thịt bò phi lê mềm ướp bơ thơm lừng ăn kèm trứng ốp la, pate béo ngậy và bánh mì nóng giòn."),
            ("Gà Chiên Nước Mắm", "ga-chien-nuoc-mam", 7, "Cánh gà chiên vàng ươm đảo đều sốt nước mắm tỏi ớt kẹo dẻo thơm mặn ngọt hài hòa."),
            ("Sườn Nướng Cơm Lam", "suon-nuong-com-lam", 5, "Sườn heo tẩm ướp mật ong rừng nướng than hoa ăn cùng ống cơm lam dẻo thơm mùi tre nứa."),
            ("Vịt Quay Bắc Kinh", "vit-quay-bac-kinh", 5, "Lớp da vịt quay mỏng tang màu cánh gián giòn rụm cuốn bánh tráng hành hoa sốt tương ngọt."),
            ("Bánh Tiramisu Ý", "banh-tiramisu-y", 15, "Bánh tráng miệng nước Ý thơm nức hương cà phê espresso, rượu rum và lớp kem mascarpone mềm mượt."),
            ("Bánh Mousse Chanh Leo", "banh-mousse-chanh-leo", 15, "Bánh mousse ba tầng chua dịu vị chanh leo nhiệt đới, kem tươi béo ngậy mát lạnh ngày hè."),
            ("Bánh Crepe Sầu Riêng", "banh-crepe-sau-rieng", 15, "Lớp vỏ crepe mềm mỏng tang ôm trọn lớp kem tươi bông mịn và thịt sầu riêng Ri6 thơm lừng."),
            ("Pancake Mật Ong Chuối", "pancake-mat-ong-chuoi", 15, "Bánh pancake tròn xốp mềm mịn ăn kèm chuối tiêu thái lát và mật ong rừng nguyên chất ngọt ngào."),
            ("Bánh Waffle Bỉ Giòn Xốp", "banh-waffle-bi-gion-xop", 15, "Bánh quế tổ ong nướng giòn rụm bên ngoài, xốp thơm bên trong ăn kèm dâu tây và kem tươi."),
            ("Chè Thái Sầu Riêng", "che-thai-sau-rieng", 16, "Bát chè Thái đầy đặn các loại thạch giòn, mít chín vàng, nhãn lồng ngập trong nước cốt dừa sầu riêng."),
            ("Chè Ba Màu Nam Bộ", "che-ba-mau-nam-bo", 16, "Món chè dân dã ba tầng đậu xanh vàng mịn, đậu đỏ bùi thơm, thạch lá dứa xanh ngắt nước cốt dừa béo."),
            ("Trà Sữa Trân Châu Đường Đen", "tra-sua-tran-chau-duong-den", 17, "Sữa tươi thanh trùng béo ngậy kết hợp dòng siro đường đen ngọt ấm và hạt trân châu dai mềm."),
            ("Sinh Tố Mãng Cầu Bơ", "sinh-to-mang-cau-bo", 18, "Sự kết hợp hoàn hảo giữa vị chua nhẹ của mãng cầu xiêm và vị béo ngậy thơm dẻo của trái bơ sáp."),
            ("Bánh Tráng Nướng Đà Lạt", "banh-trang-nuong-da-lat", 24, "Chiếc 'pizza Đà Lạt' nướng than hoa giòn rụm với trứng cút, hành hoa, xúc xích và phô mai béo ngậy."),
            ("Bánh Tráng Trộn Long An", "banh-trang-tron-long-an", 24, "Bánh tráng cắt sợi trộn muối tôm Tây Ninh, xoài xanh chua giòn, rau răm, khô bò và trứng cút."),
            ("Cút Lộn Xào Me", "cut-lon-xao-me", 24, "Trứng cút lộn chiên sơ xào ngập trong sốt me chua ngọt cay nồng rắc lạc rang và rau răm."),
            ("Bắp Xào Tôm Bơ", "bap-xao-tom-bo", 24, "Hạt bắp nếp ngọt dẻo xào thơm lừng bơ lạt cùng ruốc tôm đỏ au và mỡ hành xanh bóng."),
            ("Khoai Lang Lắc Phô Mai", "khoai-lang-lac-pho-mai", 24, "Khoai lang vàng chiên giòn tan lắc đều lớp bột phô mai mằn mặn thơm nức ngọt ngào."),
            ("Gà Lắc Phô Mai Cay", "ga-lac-pho-mai-cay", 24, "Miếng gà rút xương chiên vàng giòn rụm lắc đẫm bột phô mai cay kích thích mọi giác quan."),
        };

        var random = new Random(42);
        var ingredientPool = new[]
        {
            ("Thịt chính / Hải sản", 400m, "g", "Thái lát hoặc cắt khối vừa ăn"),
            ("Hành tây", 2m, "củ", "Thái múi cau"),
            ("Hành lá & Ngò rí", 50m, "g", "Rửa sạch, cắt khúc nhỏ"),
            ("Tỏi khô", 1m, "củ", "Bóc vỏ, băm nhuyễn"),
            ("Gừng tươi", 30m, "g", "Gọt vỏ, đập dập hoặc thái sợi"),
            ("Ớt sừng", 2m, "quả", "Bỏ hạt, thái lát chéo"),
            ("Nước mắm truyền thống", 3m, "muỗng canh", "Nêm theo khẩu vị"),
            ("Hạt nêm thịt thăn", 2m, "muỗng cà phê", "Nêm đậm đà"),
            ("Tiêu đen xay", 1m, "muỗng cà phê", "Rắc thơm khi hoàn thiện"),
            ("Đường thốt nốt", 1.5m, "muỗng canh", "Tạo vị ngọt thanh tự nhiên"),
            ("Dầu ăn thực vật", 2m, "muỗng canh", "Dùng để phi thơm và chiên xào"),
            ("Nước dùng hầm xương", 500m, "ml", "Nấu sôi liu riu lấy vị ngọt")
        };

        for (int i = 0; i < recipeDefs.Length; i++)
        {
            var def = recipeDefs[i];
            if (await db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Slug == def.Slug, ct)) continue;

            var author = authors[i % authors.Count];
            var category = catList[def.CatIdx % catList.Count];

            int prepTime = 15 + (i % 6) * 5;
            int cookTime = 20 + (i % 8) * 10;
            int servings = 2 + (i % 5);
            var difficulty = (RecipeDifficulty)(i % 3);

            var recipe = Recipe.CreateDraft(
                def.Title,
                def.Slug,
                def.Desc,
                $"Hướng dẫn chi tiết cách chế biến món {def.Title} chuẩn vị truyền thống.",
                prepTime,
                cookTime,
                servings,
                difficulty,
                category.Id,
                author.Id);

            // Dinh dưỡng
            int calories = 300 + (i * 7) % 450;
            decimal protein = 18m + (i % 25);
            decimal carbs = 30m + (i % 40);
            decimal fat = 10m + (i % 18);
            decimal fiber = 2.5m + (i % 5);
            decimal sodium = 450m + (i * 9) % 500;
            recipe.SetNutrition(RecipeNutrition.Create(calories, protein, carbs, fat, fiber, sodium));

            // Thêm 10-12 nguyên liệu (đảm bảo >= 10 nguyên liệu)
            int numIngredients = 10 + (i % 3); // 10, 11 hoặc 12
            for (int ingIdx = 0; ingIdx < numIngredients; ingIdx++)
            {
                var baseIng = ingredientPool[ingIdx % ingredientPool.Length];
                string ingName = ingIdx == 0 ? $"Nguyên liệu chính ({def.Title.Split(' ')[0]})" : baseIng.Item1;
                decimal qty = baseIng.Item2 * (1m + (i % 3) * 0.2m);
                recipe.AddIngredient(ingName, Math.Round(qty, 1), baseIng.Item3, baseIng.Item4);
            }

            // Thêm 5-6 bước chế biến (đảm bảo >= 5 bước)
            int numSteps = 5 + (i % 2); // 5 hoặc 6 bước
            recipe.AddStep("Sơ chế nguyên liệu", $"Rửa sạch các nguyên liệu tươi cho món {def.Title}, để ráo nước và cắt thái vừa ăn.", 15, "Rửa qua nước muối loãng để khử mùi tanh.");
            recipe.AddStep("Tẩm ướp gia vị", "Cho hành, tỏi, nước mắm, tiêu, hạt nêm và đường vào trộn đều cùng nguyên liệu chính trong 20 phút.", 20, "Ướp trong ngăn mát tủ lạnh để thấm vị nhanh hơn.");
            recipe.AddStep("Phi thơm và tao sơ", "Bắc chảo lên bếp, cho dầu ăn vào phi thơm hành tỏi băm, sau đó cho nguyên liệu vào đảo săn trên lửa lớn.", 10, "Đảo nhanh tay để nguyên liệu giữ được độ ngọt tự nhiên.");
            recipe.AddStep("Nấu chính và gia nhiệt", "Hạ nhỏ lửa, đậy nắp ninh liu riu hoặc đun sôi cho đến khi các nguyên liệu chín mềm và nước sốt sánh lại.", cookTime > 30 ? cookTime - 15 : 20, "Hớt bọt thường xuyên để nước dùng luôn trong và đẹp mắt.");
            recipe.AddStep("Cân chỉnh gia vị", "Nếm lại nước sốt và điều chỉnh thêm chút tiêu, ớt và nước mắm cho thật hài hòa, chuẩn vị.", 5, null);
            if (numSteps >= 6)
            {
                recipe.AddStep("Hoàn thiện và thưởng thức", $"Trình bày món {def.Title} ra đĩa hoặc tô nóng, rắc hành ngò thái nhỏ và thưởng thức cùng cơm hoặc bún.", 5, "Ngon nhất khi thưởng thức ngay lúc còn nóng sốt.");
            }

            // Ảnh đại diện
            recipe.AddImage($"https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=800&auto=format&fit=crop", $"Ảnh món {def.Title}");

            // 85% món được xuất bản (Published), 15% để Draft
            if (i % 7 != 0)
            {
                recipe.Publish();
            }

            db.Recipes.Add(recipe);
        }

        await db.SaveChangesAsync(ct);
    }

    private static RecipeAuthorUser NewUser(string email, string displayName)
    {
        var id = Guid.NewGuid().ToString();
        return new RecipeAuthorUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }
}
