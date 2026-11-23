using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class MovieSeedGroup : ISeedGroup
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed record CastSeed(string Actor, string? Character);

    private sealed class MovieSeed
    {
        public required string Title { get; init; }
        public required string? OriginalTitle { get; init; }
        public required string Description { get; init; }
        public required string Director { get; init; }
        public required int DurationMinutes { get; init; }
        public required DateOnly ReleaseDate { get; init; }
        public required string Language { get; init; }
        public required string Subtitle { get; init; }
        public required string AgeRating { get; init; }
        public required string PosterUrl { get; init; }
        public required string? TrailerUrl { get; init; }
        public required MovieStatus Status { get; init; }
        public required decimal AverageRating { get; init; }
        public required string[] Genres { get; init; }
        public required CastSeed[] Cast { get; init; }
    }

    private static readonly MovieSeed[] Movies =
    [
        new()
        {
            Title = "Oppenheimer",
            OriginalTitle = "Oppenheimer",
            Description =
                "Câu chuyện về J. Robert Oppenheimer, nhà vật lý lý thuyết người Mỹ lãnh đạo Dự án Manhattan phát triển vũ khí nguyên tử đầu tiên trên thế giới trong Thế chiến thứ hai.",
            Director = "Christopher Nolan",
            DurationMinutes = 180,
            ReleaseDate = new DateOnly(2023, 8, 11),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "T18",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/4/4a/Oppenheimer_%28film%29.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=keydELfWuMQ",
            Status = MovieStatus.Ended,
            AverageRating = 4.8m,
            Genres = ["Chính kịch", "Tâm lý"],
            Cast =
            [
                new("Cillian Murphy", "J. Robert Oppenheimer"),
                new("Robert Downey Jr.", "Lewis Strauss"),
            ],
        },
        new()
        {
            Title = "Dune: Hành Tinh Cát - Phần Hai",
            OriginalTitle = "Dune: Part Two",
            Description =
                "Paul Atreides hợp lực cùng Chani và người Fremen để trả thù những kẻ đã hủy diệt gia đình anh, đồng thời đối mặt với lựa chọn nghiệt ngã giữa tình yêu và vận mệnh của vũ trụ.",
            Director = "Denis Villeneuve",
            DurationMinutes = 166,
            ReleaseDate = new DateOnly(2024, 3, 1),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "T16",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/5/52/Dune_Part_Two_poster.jpeg",
            TrailerUrl = "https://www.youtube.com/watch?v=Way9Dexny3w",
            Status = MovieStatus.Ended,
            AverageRating = 4.7m,
            Genres = ["Hành động", "Khoa học viễn tưởng", "Chính kịch"],
            Cast =
            [
                new("Timothée Chalamet", "Paul Atreides"),
                new("Zendaya", "Chani"),
            ],
        },
        new()
        {
            Title = "Mai",
            OriginalTitle = "Mai",
            Description =
                "Mai - người phụ nữ gần 40 tuổi làm nghề mát-xa trị liệu mang nhiều vết thương tâm hồn, gặp Dương - chàng trai đào hoa trẻ tuổi quyết tâm theo đuổi cô. Một câu chuyện tình yêu đầy thử thách và định kiến xã hội.",
            Director = "Trấn Thành",
            DurationMinutes = 131,
            ReleaseDate = new DateOnly(2024, 2, 10),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T18",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/vi/3/36/Mai_2024_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=EX6clvId19s",
            Status = MovieStatus.Ended,
            AverageRating = 4.5m,
            Genres = ["Chính kịch", "Lãng mạn", "Tâm lý"],
            Cast =
            [
                new("Phương Anh Đào", "Mai"),
                new("Tuấn Trần", "Trùng Dương (Sâu)"),
                new("Trấn Thành", "Ông Hoàng"),
                new("Uyển Ân", "Bình Minh"),
            ],
        },
        new()
        {
            Title = "Ký Sinh Trùng",
            OriginalTitle = "Parasite",
            Description =
                "Gia đình ông Kim nghèo khó tìm cách từng người một thâm nhập vào làm việc cho gia đình ngài Park giàu sang phú quý, mở ra chuỗi bi kịch tầng lớp châm biếm sâu cay.",
            Director = "Bong Joon-ho",
            DurationMinutes = 132,
            ReleaseDate = new DateOnly(2019, 6, 21),
            Language = "Tiếng Hàn",
            Subtitle = "Tiếng Việt",
            AgeRating = "T18",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/5/53/Parasite_%282019_film%29.png",
            TrailerUrl = "https://www.youtube.com/watch?v=isOGD_7hNIY",
            Status = MovieStatus.Ended,
            AverageRating = 4.9m,
            Genres = ["Chính kịch", "Kinh dị", "Tâm lý"],
            Cast =
            [
                new("Song Kang-ho", "Kim Ki-taek"),
                new("Choi Woo-shik", "Kim Ki-woo"),
            ],
        },
        new()
        {
            Title = "Avatar: Dòng Chảy Của Nước",
            OriginalTitle = "Avatar: The Way of Water",
            Description =
                "Jake Sully cùng Neytiri và các con phải rời bỏ quê hương, tìm đến nương náu ở vùng biển của tộc Metkayina khi loài người trở lại đe dọa sự sống của Pandora.",
            Director = "James Cameron",
            DurationMinutes = 192,
            ReleaseDate = new DateOnly(2022, 12, 16),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "T13",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/5/54/Avatar_The_Way_of_Water_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=6AvFHlKS6OE",
            Status = MovieStatus.Ended,
            AverageRating = 4.6m,
            Genres = ["Hành động", "Khoa học viễn tưởng"],
            Cast =
            [
                new("Sam Worthington", "Jake Sully"),
                new("Zoe Saldana", "Neytiri"),
            ],
        },
        new()
        {
            Title = "Lật Mặt 7: Một Điều Ước",
            OriginalTitle = "Face Off 7: One Wish",
            Description =
                "Bộ phim gia đình xúc động về bà Hai 73 tuổi neo đơn ở vùng cao Lâm Đồng và những câu chuyện trăn trở của 5 người con đã trưởng thành khi mẹ gặp nạn.",
            Director = "Lý Hải",
            DurationMinutes = 138,
            ReleaseDate = new DateOnly(2024, 4, 26),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "K",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/vi/d/d4/%C3%81p_ph%C3%ADch_ch%C3%ADnh_th%E1%BB%A9c_L%E1%BA%ADt_m%E1%BA%B7t_7.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=d1ZHdosjNX8",
            Status = MovieStatus.Ended,
            AverageRating = 4.4m,
            Genres = ["Chính kịch", "Hài"],
            Cast =
            [
                new("Thái Hòa", "Ba Tràng"),
                new("Quách Ngọc Ngoan", "Tư Hậu"),
            ],
        },
        new()
        {
            Title = "Deadpool & Wolverine",
            OriginalTitle = "Deadpool & Wolverine",
            Description =
                "Cơ quan Phương sai Thời gian (TVA) lôi kéo Deadpool vào một nhiệm vụ bảo vệ đa vũ trụ, buộc anh phải hợp tác cùng một biến thể bất đắc dĩ của Wolverine.",
            Director = "Shawn Levy",
            DurationMinutes = 128,
            ReleaseDate = new DateOnly(2024, 7, 26),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "T18",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/vi/4/4c/Deadpool_%26_Wolverine_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=73_1biulkYk",
            Status = MovieStatus.Ended,
            AverageRating = 4.7m,
            Genres = ["Hành động", "Hài", "Khoa học viễn tưởng"],
            Cast =
            [
                new("Ryan Reynolds", "Wade Wilson / Deadpool"),
                new("Hugh Jackman", "Logan / Wolverine"),
            ],
        },
        new()
        {
            Title = "Những Mảnh Ghép Cảm Xúc 2",
            OriginalTitle = "Inside Out 2",
            Description =
                "Cô bé Riley bước vào tuổi dậy thì, kéo theo sự xuất hiện của những cảm xúc mới tinh vi như Lo Âu, Ganh Tị, Xấu Hổ và Chán Nản bên cạnh nhóm Vui Vẻ quen thuộc.",
            Director = "Kelsey Mann",
            DurationMinutes = 96,
            ReleaseDate = new DateOnly(2024, 6, 14),
            Language = "Tiếng Anh",
            Subtitle = "Lồng tiếng & Phụ đề",
            AgeRating = "P",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/f/f7/Inside_Out_2_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=8rjvKzYeSp0",
            Status = MovieStatus.Ended,
            AverageRating = 4.8m,
            Genres = ["Hoạt hình", "Hài", "Tâm lý"],
            Cast =
            [
                new("Amy Poehler", "Joy (Vui Vẻ)"),
                new("Maya Hawke", "Anxiety (Lo Âu)"),
            ],
        },
        new()
        {
            Title = "Em Và Trịnh",
            OriginalTitle = "Em và Trịnh",
            Description =
                "Tác phẩm tái hiện cuộc đời tài hoa, lãng tử của cố nhạc sĩ Trịnh Công Sơn qua những giai đoạn gắn liền với các 'nàng thơ' Dao Ánh, Bích Diễm, Khánh Ly, Michiko.",
            Director = "Phan Gia Nhật Linh",
            DurationMinutes = 136,
            ReleaseDate = new DateOnly(2022, 6, 17),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T13",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/vi/b/b3/Poster_gioi_thieu_phim_Em_va_Trinh.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=zzik4JB9D1Q",
            Status = MovieStatus.Ended,
            AverageRating = 4.2m,
            Genres = ["Chính kịch", "Lãng mạn"],
            Cast =
            [
                new("Avin Lu", "Trịnh Công Sơn (thời trẻ)"),
                new("Hoàng Hà", "Dao Ánh"),
            ],
        },
        new()
        {
            Title = "Người Bất Tử",
            OriginalTitle = "The Immortal",
            Description =
                "Bộ phim kỳ ảo tâm linh về Hùng - người đàn ông nắm giữ bí thuật ngải bất tử vượt qua 3 thế kỷ đầy tham vọng, thù hận và sự trừng phạt nghiệt ngã của số phận.",
            Director = "Victor Vũ",
            DurationMinutes = 130,
            ReleaseDate = new DateOnly(2018, 10, 26),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T18",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/b/b9/The_Immortal_%282018_film%29_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=6VEVPtFDN_U",
            Status = MovieStatus.Ended,
            AverageRating = 4.1m,
            Genres = ["Kinh dị", "Tâm lý"],
            Cast =
            [
                new("Quách Ngọc Ngoan", "Hùng"),
                new("Ninh Dương Lan Ngọc", "Liên"),
            ],
        },
        new()
        {
            Title = "Hố Đen Tử Thần",
            OriginalTitle = "Interstellar",
            Description =
                "Khi Trái Đất bên bờ diệt vong, một nhóm nhà thám hiểm vũ trụ dấn thân vào hố sâu không gian qua một lỗ sâu mới xuất hiện để tìm kiếm hành tinh mới cho sự sống còn của nhân loại.",
            Director = "Christopher Nolan",
            DurationMinutes = 169,
            ReleaseDate = new DateOnly(2014, 11, 7),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "T13",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/b/bc/Interstellar_film_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=zSWdZVtXT7E",
            Status = MovieStatus.Ended,
            AverageRating = 4.9m,
            Genres = ["Khoa học viễn tưởng", "Phiêu lưu", "Chính kịch"],
            Cast =
            [
                new("Matthew McConaughey", "Joseph Cooper"),
                new("Anne Hathaway", "Dr. Amelia Brand"),
            ],
        },
        new()
        {
            Title = "Mắt Biếc",
            OriginalTitle = "Dreamy Eyes",
            Description =
                "Chuyển thể từ truyện dài cùng tên của nhà văn Nguyễn Nhật Ánh, kể về mối tình đơn phương vừa tha thiết vừa đượm buồn của Ngạn dành cho cô bạn thanh mai trúc mã Hà Lan có đôi mắt biếc.",
            Director = "Victor Vũ",
            DurationMinutes = 117,
            ReleaseDate = new DateOnly(2019, 12, 20),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T13",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/vi/4/42/%C3%81p_ph%C3%ADch_phim_M%E1%BA%AFt_bi%E1%BA%BFc.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=ITlQ0oU7tDA",
            Status = MovieStatus.Ended,
            AverageRating = 4.5m,
            Genres = ["Lãng mạn", "Chính kịch"],
            Cast =
            [
                new("Trần Nghĩa", "Ngạn"),
                new("Trúc Anh", "Hà Lan"),
            ],
        },
        new()
        {
            Title = "Bố Già",
            OriginalTitle = "Dad, I'm Sorry",
            Description =
                "Tác phẩm xoay quanh đại gia đình Giàu - Sang - Phú - Quý tại một con hẻm nghèo ở Sài Gòn, đặc biệt là mối quan hệ mâu thuẫn thế hệ đầy day dứt và tình yêu thương sâu sắc giữa Ba Sang và Quắn.",
            Director = "Trấn Thành",
            DurationMinutes = 128,
            ReleaseDate = new DateOnly(2021, 3, 5),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "K",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/vi/7/72/Bo_gia_2021_ap_phich.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=jluSu8Rw6YE",
            Status = MovieStatus.Ended,
            AverageRating = 4.4m,
            Genres = ["Hài", "Chính kịch", "Tâm lý"],
            Cast =
            [
                new("Trấn Thành", "Ba Sang"),
                new("Tuấn Trần", "Quắn"),
                new("Lê Giang", "Cẩm Lệ"),
            ],
        },
        new()
        {
            Title = "Quật Mộ Trùng Ma",
            OriginalTitle = "Exhuma",
            Description =
                "Một nhóm pháp sư, thầy phong thủy và chuyên gia tang lễ hàng đầu Hàn Quốc nhận lời khai quật một ngôi mộ bí ẩn với mức thù lao khổng lồ, vô tình giải thoát một thế lực hắc ám cổ xưa rùng rợn.",
            Director = "Jang Jae-hyun",
            DurationMinutes = 134,
            ReleaseDate = new DateOnly(2024, 3, 15),
            Language = "Tiếng Hàn",
            Subtitle = "Tiếng Việt",
            AgeRating = "T18",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/1/13/Exhuma_film_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=nO3_h3fJ3-s",
            Status = MovieStatus.Ended,
            AverageRating = 4.6m,
            Genres = ["Kinh dị", "Bí ẩn", "Tâm lý"],
            Cast =
            [
                new("Choi Min-sik", "Kim Sang-deok"),
                new("Kim Go-eun", "Lee Hwa-rim"),
                new("Lee Do-hyun", "Yoon Bong-gil"),
            ],
        },
        new()
        {
            Title = "Người Nhện: Du Hành Vũ Trụ Nhện",
            OriginalTitle = "Spider-Man: Across the Spider-Verse",
            Description =
                "Miles Morales tái ngộ cùng Gwen Stacy và bị kéo vào đa vũ trụ đầy hiểm họa. Tại đây, Miles phải đối đầu với Liên minh Người Nhện để bảo vệ định mệnh và người thân yêu nhất của mình.",
            Director = "Joaquim Dos Santos",
            DurationMinutes = 140,
            ReleaseDate = new DateOnly(2023, 6, 2),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "K",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/b/b4/Spider-Man-_Across_the_Spider-Verse_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=cqGjhVJWtEg",
            Status = MovieStatus.Ended,
            AverageRating = 4.9m,
            Genres = ["Hoạt hình", "Hành động", "Khoa học viễn tưởng"],
            Cast =
            [
                new("Shameik Moore", "Miles Morales / Spider-Man"),
                new("Hailee Steinfeld", "Gwen Stacy / Spider-Woman"),
            ],
        },
        new()
        {
            Title = "Avengers: Hồi Kết",
            OriginalTitle = "Avengers: Endgame",
            Description =
                "Sau cú búng tay hủy diệt của Thanos, các Avengers còn sống sót tập hợp lực lượng một lần cuối cùng, thực hiện kế hoạch du hành thời gian đầy táo bạo nhằm khôi phục vũ trụ.",
            Director = "Anthony & Joe Russo",
            DurationMinutes = 181,
            ReleaseDate = new DateOnly(2019, 4, 26),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "T13",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/0/0d/Avengers_Endgame_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=TcMBFSGVi1c",
            Status = MovieStatus.Ended,
            AverageRating = 4.9m,
            Genres = ["Hành động", "Khoa học viễn tưởng", "Phiêu lưu"],
            Cast =
            [
                new("Robert Downey Jr.", "Tony Stark / Iron Man"),
                new("Chris Evans", "Steve Rogers / Captain America"),
                new("Scarlett Johansson", "Natasha Romanoff / Black Widow"),
            ],
        },
        new()
        {
            Title = "Kung Fu Panda 4",
            OriginalTitle = "Kung Fu Panda 4",
            Description =
                "Po chuẩn bị trở thành Thủ lĩnh Tinh thần của Thung lũng Bình Yên và phải huấn luyện một chiến binh rồng mới, đồng thời đối đầu với mụ phù thủy Tắc Kè Hoa có khả năng biến hình nguy hiểm.",
            Director = "Mike Mitchell",
            DurationMinutes = 94,
            ReleaseDate = new DateOnly(2024, 3, 8),
            Language = "Tiếng Anh",
            Subtitle = "Lồng tiếng & Phụ đề",
            AgeRating = "P",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/7/7f/Kung_Fu_Panda_4_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=_inKs4eeHiI",
            Status = MovieStatus.Ended,
            AverageRating = 4.3m,
            Genres = ["Hoạt hình", "Hành động", "Hài"],
            Cast =
            [
                new("Jack Black", "Po"),
            ],
        },
        new()
        {
            Title = "Nhà Bà Nữ",
            OriginalTitle = "The House of No Man",
            Description =
                "Câu chuyện xoay quanh tiệm bánh canh cua của bà Ngọc Nữ và những rắc rối, mâu thuẫn tình cảm trong một gia đình nhiều thế hệ toàn phụ nữ nơi sự kiểm soát thái quá dẫn đến đổ vỡ.",
            Director = "Trấn Thành",
            DurationMinutes = 102,
            ReleaseDate = new DateOnly(2023, 1, 22),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T16",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/vi/6/6f/%C3%81p_ph%C3%ADch_phim_Nh%C3%A0_b%C3%A0_N%E1%BB%AF.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=684y48YgqjA",
            Status = MovieStatus.Ended,
            AverageRating = 4.3m,
            Genres = ["Hài", "Chính kịch", "Tâm lý"],
            Cast =
            [
                new("Lê Giang", "Bà Ngọc Nữ"),
                new("Uyển Ân", "Ngọc Nhi"),
                new("Song Luân", "John"),
                new("Trấn Thành", "Phú Nhuận"),
            ],
        },
        new()
        {
            Title = "Khóa Chặt Cửa Nào Suzume",
            OriginalTitle = "Suzume",
            Description =
                "Cô nữ sinh 17 tuổi Suzume gặp gỡ chàng trai kỳ lạ Souta và bắt đầu hành trình băng qua khắp nước Nhật để đóng lại những cánh cửa giải phóng tai ương thảm họa bảo vệ sự bình yên nhân loại.",
            Director = "Makoto Shinkai",
            DurationMinutes = 122,
            ReleaseDate = new DateOnly(2023, 3, 10),
            Language = "Tiếng Nhật",
            Subtitle = "Tiếng Việt",
            AgeRating = "K",
            PosterUrl =
                "https://upload.wikimedia.org/wikipedia/en/7/7f/Suzume_no_Tojimari_poster.jpg",
            TrailerUrl = "https://www.youtube.com/watch?v=5pY4qVz9rEQ",
            Status = MovieStatus.Ended,
            AverageRating = 4.7m,
            Genres = ["Hoạt hình", "Phiêu lưu", "Bí ẩn"],
            Cast =
            [
                new("Nanoka Hara", "Suzume Iwato"),
                new("Hokuto Matsumura", "Souta Munakata"),
            ],
        },
        new()
        {
            Title = "Godzilla Trừ Không",
            OriginalTitle = "Godzilla Minus Zero",
            Description =
                "Phần tiếp nối Godzilla Minus One do Takashi Yamazaki thực hiện, đưa khán giả trở lại nước Nhật hậu chiến trước sự đe dọa của quái vật huyền thoại Godzilla.",
            Director = "Takashi Yamazaki",
            DurationMinutes = 135,
            ReleaseDate = new DateOnly(2026, 11, 6),
            Language = "Tiếng Nhật",
            Subtitle = "Tiếng Việt",
            AgeRating = "T13",
            PosterUrl =
                "https://cinema.momocdn.net/convert-webp/img/2404819473541948-godzzzzz.jpg?size=L",
            TrailerUrl = null,
            Status = MovieStatus.NowShowing,
            AverageRating = 0m,
            Genres = ["Hành động", "Khoa học viễn tưởng"],
            Cast =
            [
                new("Ryunosuke Kamiki", null),
                new("Minami Hamabe", null),
            ],
        },
        new()
        {
            Title = "Chàng Mèo Mang Mũ",
            OriginalTitle = "The Cat in the Hat",
            Description =
                "Phim hoạt hình phiêu lưu hài hước dựa trên tác phẩm kinh điển của Dr. Seuss: chú mèo tinh nghịch đội chiếc mũ sọc đỏ trắng biến một ngày mưa buồn tẻ thành cuộc phiêu lưu hỗn loạn.",
            Director = "Alessandro Carloni",
            DurationMinutes = 114,
            ReleaseDate = new DateOnly(2026, 11, 6),
            Language = "Tiếng Anh",
            Subtitle = "Lồng tiếng & Phụ đề",
            AgeRating = "P",
            PosterUrl =
                "https://image-fworker.momocdn.net/momo-external/baaf9e8cdc307bc2a22024cc1b58e3c932bb24e0085aec89bc5c12c2236a7aa9?size=L",
            TrailerUrl = null,
            Status = MovieStatus.NowShowing,
            AverageRating = 0m,
            Genres = ["Hoạt hình", "Gia đình", "Phiêu lưu"],
            Cast =
            [
                new("Bill Hader", null),
                new("Xochitl Gomez", null),
            ],
        },
        new()
        {
            Title = "Bò Sữa Bay",
            OriginalTitle = "Bò Sữa Bay",
            Description =
                "Phim hài hoài niệm pha yếu tố khoa học viễn tưởng, lấy bối cảnh một nông trại ở Mộc Châu những năm 1990. Tác phẩm điện ảnh đầu tay của đạo diễn Nguyễn Phạm Thành Đạt.",
            Director = "Nguyễn Phạm Thành Đạt",
            DurationMinutes = 120,
            ReleaseDate = new DateOnly(2026, 11, 6),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T13",
            PosterUrl =
                "https://cinema.momocdn.net/convert-webp/img/4904564921820363-bsb_1200x1600.png?size=L",
            TrailerUrl = null,
            Status = MovieStatus.NowShowing,
            AverageRating = 0m,
            Genres = ["Hài", "Hành động", "Tâm lý"],
            Cast =
            [
                new("Nguyễn Hùng", null),
                new("Trần Tiểu Vy", null),
                new("Ngọc Xuân", null),
                new("Võ Điền Gia Huy", null),
                new("Lãnh Thanh", null),
            ],
        },
        new()
        {
            Title = "Ngày Con Còn Mẹ",
            OriginalTitle = "Ngày Con Còn Mẹ",
            Description =
                "Phim tâm lý gia đình của đạo diễn Vũ Ngọc Đãng, khai thác tình mẫu tử và những rạn nứt, hàn gắn giữa các thế hệ trong một gia đình.",
            Director = "Vũ Ngọc Đãng",
            DurationMinutes = 110,
            ReleaseDate = new DateOnly(2026, 11, 11),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T13",
            PosterUrl =
                "https://cinema.momocdn.net/convert-webp/img/4986547019998252-1200wx1600h.jpg?size=L",
            TrailerUrl = null,
            Status = MovieStatus.NowShowing,
            AverageRating = 0m,
            Genres = ["Chính kịch", "Tâm lý", "Gia đình"],
            Cast =
            [
                new("Hồng Đào", null),
                new("Phương Anh Đào", null),
                new("Lê Thiện", null),
                new("Ma Ran Đô", null),
                new("Quỳnh Lương", null),
            ],
        },
        new()
        {
            Title = "Quỷ Ăn Tạng 4: Hổ Tinh",
            OriginalTitle = null,
            Description =
                "Phần thứ tư của loạt phim kinh dị siêu nhiên Thái Lan Quỷ Ăn Tạng, kết hợp yếu tố hành động.",
            Director = "Narit Yuvaboon",
            DurationMinutes = 111,
            ReleaseDate = new DateOnly(2026, 10, 9),
            Language = "Tiếng Thái",
            Subtitle = "Tiếng Việt",
            AgeRating = "T18",
            PosterUrl =
                "https://homepage.momocdn.net/img/quy-an-tang-4-260929095104.webp",
            TrailerUrl = null,
            Status = MovieStatus.NowShowing,
            AverageRating = 0m,
            Genres = ["Kinh dị", "Tâm linh"],
            Cast =
            [
                new("Nadech Kugimiya", null),
                new("Ong-art Cheamcharoenpornkul", null),
            ],
        },
        new()
        {
            Title = "Street Fighter",
            OriginalTitle = "Street Fighter",
            Description =
                "Chuyển thể từ series game đối kháng huyền thoại của Capcom, quy tụ các võ sĩ từ khắp thế giới trong giải đấu Street Fighter.",
            Director = "Kitao Sakurai",
            DurationMinutes = 125,
            ReleaseDate = new DateOnly(2026, 10, 16),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "T16",
            PosterUrl =
                "https://image-fworker.momocdn.net/momo-external/828b56d93d2f00be168fb17e06cdf8ab0051a3b59762b65d45d6eb9eba6df8dd?size=L",
            TrailerUrl = null,
            Status = MovieStatus.NowShowing,
            AverageRating = 0m,
            Genres = ["Hành động"],
            Cast =
            [
                new("Andrew Koji", null),
                new("Noah Centineo", null),
                new("Jason Momoa", null),
            ],
        },
        new()
        {
            Title = "Chị Chị Em Em 3",
            OriginalTitle = "Chị Chị Em Em 3",
            Description =
                "Phần ba của loạt phim chính kịch cổ trang Việt Nam về những mối quan hệ chị em đầy ganh ghét và toan tính.",
            Director = "Nguyễn Hữu Hoàng",
            DurationMinutes = 115,
            ReleaseDate = new DateOnly(2026, 10, 16),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T18",
            PosterUrl =
                "https://cinema.momocdn.net/convert-webp/img/4463292240117827-cc33.jpg?size=L",
            TrailerUrl = null,
            Status = MovieStatus.NowShowing,
            AverageRating = 0m,
            Genres = ["Chính kịch"],
            Cast =
            [
                new("Nguyễn Cao Kỳ Duyên", null),
                new("Steven Nguyễn", null),
            ],
        },
        new()
        {
            Title = "Trấn Yểm",
            OriginalTitle = "Trấn Yểm",
            Description =
                "Phim kinh dị tâm linh Việt Nam của đạo diễn Trần Hữu Tấn.",
            Director = "Trần Hữu Tấn",
            DurationMinutes = 96,
            ReleaseDate = new DateOnly(2026, 10, 30),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T18",
            PosterUrl =
                "https://cinema.momocdn.net/convert-webp/img/169449770605007316-tra_n-ye_m-poster-co_ng-bo_-du_-a_n.jpg?size=L",
            TrailerUrl = null,
            Status = MovieStatus.NowShowing,
            AverageRating = 0m,
            Genres = ["Kinh dị", "Tâm linh"],
            Cast =
            [

            ],
        },
        new()
        {
            Title = "Đấu Trường Sinh Tử: Bình Minh Ngày Chiêu Quân",
            OriginalTitle = "The Hunger Games: Sunrise on the Reaping",
            Description =
                "Phần tiền truyện mới của loạt phim Đấu Trường Sinh Tử do Francis Lawrence đạo diễn, quay về thời điểm Hội Chiêu Quân diễn ra.",
            Director = "Francis Lawrence",
            DurationMinutes = 158,
            ReleaseDate = new DateOnly(2026, 11, 20),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "T16",
            PosterUrl =
                "https://cinema.momocdn.net/convert-webp/img/3864033921859582-thehungergamesunrise.jpg?size=L",
            TrailerUrl = null,
            Status = MovieStatus.ComingSoon,
            AverageRating = 0m,
            Genres = ["Hành động", "Phiêu lưu", "Khoa học viễn tưởng"],
            Cast =
            [
                new("Joseph Zada", null),
                new("Whitney Peak", null),
                new("Mckenna Grace", null),
                new("Jesse Plemons", null),
            ],
        },
        new()
        {
            Title = "Trại Giam Hạnh Phúc",
            OriginalTitle = "Trại Giam Hạnh Phúc",
            Description =
                "Phim hài – gia đình – tâm lý của đạo diễn Nguyễn Nhất Trung (Trung Lùn).",
            Director = "Nguyễn Nhất Trung",
            DurationMinutes = 120,
            ReleaseDate = new DateOnly(2026, 11, 20),
            Language = "Tiếng Việt",
            Subtitle = "Tiếng Anh",
            AgeRating = "T13",
            PosterUrl =
                "https://cinema.momocdn.net/convert-webp/img/3867294930842850-tghp.jpg?size=L",
            TrailerUrl = null,
            Status = MovieStatus.ComingSoon,
            AverageRating = 0m,
            Genres = ["Hài", "Gia đình", "Tâm lý"],
            Cast =
            [
                new("Tuấn Trần", null),
                new("Ma Ran Đô", null),
                new("Lương Thế Thành", null),
                new("Sally Hồng Anh", null),
            ],
        },
        new()
        {
            Title = "Gặp Gỡ Thông Gia: Dâu Mới Trình Làng",
            OriginalTitle = "Focker-in-Law",
            Description =
                "Phần tiếp theo của loạt phim hài Meet the Parents với dàn diễn viên Robert De Niro, Ben Stiller, Ariana Grande và Owen Wilson.",
            Director = "John Hamburg",
            DurationMinutes = 90,
            ReleaseDate = new DateOnly(2026, 11, 27),
            Language = "Tiếng Anh",
            Subtitle = "Tiếng Việt",
            AgeRating = "T13",
            PosterUrl =
                "https://image-fworker.momocdn.net/momo-external/52e1271dc9f3d6ba6c7f38e060ab0692254826dd1bee7e0cae963248e10d35e0?size=L",
            TrailerUrl = null,
            Status = MovieStatus.ComingSoon,
            AverageRating = 0m,
            Genres = ["Hài", "Gia đình"],
            Cast =
            [
                new("Robert De Niro", null),
                new("Ben Stiller", null),
                new("Ariana Grande", null),
                new("Owen Wilson", null),
            ],
        },
    ];

    public string Name => "movies";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existingTitles = (
            await db.Movies.Select(m => m.Title).ToListAsync(cancellationToken)
        ).ToHashSet();
        var pending = Movies.Where(m => !existingTitles.Contains(m.Title)).ToList();
        if (pending.Count == 0)
            return;

        var directorIds = await db.Directors.ToDictionaryAsync(
            x => x.FullName,
            x => x.DirectorId,
            cancellationToken
        );
        var actorIds = await db.Actors.ToDictionaryAsync(
            x => x.FullName,
            x => x.ActorId,
            cancellationToken
        );
        var languageIds = await db.Languages.ToDictionaryAsync(
            x => x.Name,
            x => x.LanguageId,
            cancellationToken
        );
        var ratingIds = await db.AgeRatings.ToDictionaryAsync(
            x => x.Code,
            x => x.AgeRatingId,
            cancellationToken
        );
        var genreIds = await db.Genres.ToDictionaryAsync(
            x => x.Name,
            x => x.GenreId,
            cancellationToken
        );

        foreach (var seed in pending)
        {
            db.Movies.Add(
                new Movie
                {
                    Title = seed.Title,
                    OriginalTitle = seed.OriginalTitle,
                    Description = seed.Description,
                    DirectorId = directorIds[seed.Director],
                    DurationMinutes = seed.DurationMinutes,
                    ReleaseDate = seed.ReleaseDate,
                    LanguageId = languageIds[seed.Language],
                    Subtitle = seed.Subtitle,
                    AgeRatingId = ratingIds[seed.AgeRating],
                    PosterUrl = seed.PosterUrl,
                    TrailerUrl = seed.TrailerUrl,
                    Status = seed.Status,
                    AverageRating = seed.AverageRating,
                    CreatedAt = CreatedAt,
                    MovieGenres = seed
                        .Genres.Select(g => new MovieGenre { GenreId = genreIds[g] })
                        .ToList(),
                    MovieCast = seed
                        .Cast.Select(c => new MovieCast
                        {
                            ActorId = actorIds[c.Actor],
                            CharacterName = c.Character,
                        })
                        .ToList(),
                }
            );
            await db.SaveChangesAsync(cancellationToken);
        }

        context.Logger.LogInformation("Seeded {Count} movies.", pending.Count);
    }
}
