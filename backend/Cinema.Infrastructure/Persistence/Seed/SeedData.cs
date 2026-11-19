using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Persistence.Seed;

public static class SeedData
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0);

    // Quy mô phòng theo rạp thương mại: 84–160 ghế (phòng 2D tiêu chuẩn ~100–110 ghế).
    // StandardRows hàng thường -> VipRows hàng VIP -> hàng cuối còn lại là ghế đôi.
    private readonly record struct RoomLayout(int Rows, int Cols, int StandardRows, int VipRows);

    private static readonly RoomLayout[] Layouts =
    [
        new(Rows: 8, Cols: 12, StandardRows: 3, VipRows: 4), // Hall 01 (2D): 96 ghế, 6 cặp đôi
        new(Rows: 8, Cols: 14, StandardRows: 3, VipRows: 4), // Hall 02 (2D): 112 ghế, 7 cặp đôi
        new(Rows: 7, Cols: 12, StandardRows: 3, VipRows: 3), // Hall 03 (3D): 84 ghế, 6 cặp đôi
        new(Rows: 10, Cols: 16, StandardRows: 4, VipRows: 5), // Hall 04 (IMAX): 160 ghế, 8 cặp đôi
    ];

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Role>()
            .HasData(
                new Role { RoleId = 1, RoleName = Roles.Customer },
                new Role { RoleId = 2, RoleName = Roles.Admin },
                new Role { RoleId = 3, RoleName = Roles.Employee }
            );

        modelBuilder
            .Entity<Position>()
            .HasData(
                new Position { PositionId = 1, Name = "Quản lý rạp" },
                new Position { PositionId = 2, Name = "Trưởng ca" },
                new Position { PositionId = 3, Name = "Nhân viên bán vé" }
            );

        modelBuilder
            .Entity<User>()
            .HasData(
                new User
                {
                    UserId = 1,
                    FullName = "System Admin",
                    Email = "admin@cinema.local",
                    PasswordHash =
                        "AQAAAAEAAYagAAAAENS3DOHbkUObJNrLWeaXshnI43hVryVZB/xVqXl+RRCO+rT1ek+EVigjK5BS5n9leQ==",
                    RoleId = 2,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = SeedDate,
                }
            );

        modelBuilder
            .Entity<Employee>()
            .HasData(
                new Employee
                {
                    EmployeeId = 1,
                    UserId = 1,
                    PositionId = 1,
                    HireDate = DateOnly.FromDateTime(SeedDate),
                    IsActive = true,
                }
            );

        modelBuilder
            .Entity<ScreenType>()
            .HasData(
                new ScreenType { ScreenTypeId = 1, Name = "2D" },
                new ScreenType { ScreenTypeId = 2, Name = "3D" },
                new ScreenType { ScreenTypeId = 3, Name = "IMAX" }
            );

        modelBuilder
            .Entity<SeatType>()
            .HasData(
                new SeatType
                {
                    SeatTypeId = 1,
                    Name = "Standard",
                    PriceMultiplier = 1.0m,
                },
                new SeatType
                {
                    SeatTypeId = 2,
                    Name = "VIP",
                    PriceMultiplier = 1.15m,
                },
                new SeatType
                {
                    SeatTypeId = 3,
                    // Hệ số áp cho TỪNG ghế; 1 cặp (2 ghế) ≈ 2,5 × vé thường, sát mức sweetbox thực tế.
                    Name = "Couple",
                    PriceMultiplier = 1.25m,
                    IsCouple = true,
                }
            );

        modelBuilder
            .Entity<Genre>()
            .HasData(
                new Genre { GenreId = 1, Name = "Hành động" },
                new Genre { GenreId = 2, Name = "Hài" },
                new Genre { GenreId = 3, Name = "Chính kịch" },
                new Genre { GenreId = 4, Name = "Kinh dị" },
                new Genre { GenreId = 5, Name = "Khoa học viễn tưởng" },
                new Genre { GenreId = 6, Name = "Hoạt hình" },
                new Genre { GenreId = 7, Name = "Lãng mạn" },
                new Genre { GenreId = 8, Name = "Tâm lý" },
                new Genre { GenreId = 9, Name = "Phiêu lưu" },
                new Genre { GenreId = 10, Name = "Bí ẩn" },
                new Genre { GenreId = 11, Name = "Gia đình" },
                new Genre { GenreId = 12, Name = "Tâm linh" }
            );

        modelBuilder
            .Entity<Language>()
            .HasData(
                new Language { LanguageId = 1, Name = "Tiếng Việt" },
                new Language { LanguageId = 2, Name = "Tiếng Anh" },
                new Language { LanguageId = 3, Name = "Tiếng Hàn" },
                new Language { LanguageId = 4, Name = "Tiếng Nhật" },
                new Language { LanguageId = 5, Name = "Tiếng Thái" }
            );

        modelBuilder
            .Entity<AgeRating>()
            .HasData(
                new AgeRating
                {
                    AgeRatingId = 1,
                    Code = "P",
                    Description = "Phổ biến, không hạn chế đối tượng xem",
                },
                new AgeRating
                {
                    AgeRatingId = 2,
                    Code = "K",
                    Description = "Phổ biến, khuyến cáo trẻ dưới 13 tuổi xem cùng cha mẹ",
                },
                new AgeRating
                {
                    AgeRatingId = 3,
                    Code = "T13",
                    Description = "Cấm phổ biến với khán giả dưới 13 tuổi",
                },
                new AgeRating
                {
                    AgeRatingId = 4,
                    Code = "T16",
                    Description = "Cấm phổ biến với khán giả dưới 16 tuổi",
                },
                new AgeRating
                {
                    AgeRatingId = 5,
                    Code = "T18",
                    Description = "Cấm phổ biến với khán giả dưới 18 tuổi",
                },
                new AgeRating
                {
                    AgeRatingId = 6,
                    Code = "C",
                    Description = "Không được phép phổ biến",
                }
            );

        modelBuilder
            .Entity<MembershipTier>()
            .HasData(
                new MembershipTier
                {
                    TierId = 1,
                    TierName = "Thành viên",
                    MinPoints = 0,
                    DiscountPercent = 0m,
                },
                new MembershipTier
                {
                    TierId = 2,
                    TierName = "Bạc",
                    MinPoints = 500,
                    DiscountPercent = 5m,
                },
                new MembershipTier
                {
                    TierId = 3,
                    TierName = "Vàng",
                    MinPoints = 2000,
                    DiscountPercent = 10m,
                },
                new MembershipTier
                {
                    TierId = 4,
                    TierName = "Kim Cương",
                    MinPoints = 5000,
                    DiscountPercent = 15m,
                }
            );

        modelBuilder
            .Entity<CinemaSettings>()
            .HasData(
                new CinemaSettings
                {
                    Id = 1,
                    Name = "Perfo Cinema",
                    Address = "Tầng 5, TTTM Perfo Plaza, 120 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                    Phone = "02838221234",
                    Email = "cskh@perfocinema.vn",
                    TaxCode = "0312345678",
                    // Suất đầu mở bán từ 9h, suất cuối kết thúc trước nửa đêm.
                    OpeningTime = new TimeOnly(8, 0),
                    ClosingTime = new TimeOnly(23, 59),
                    // 10.000đ = 1 điểm (CGV/Lotte: ~1% giá trị giao dịch).
                    PointsPerAmount = 10000m,
                    // Ngừng bán vé online khi suất chiếu còn 10 phút.
                    StopSellingMinutesBefore = 10,
                    DefaultEmployeePassword = CinemaSettings.DefaultEmployeePasswordValue,
                }
            );

        modelBuilder
            .Entity<Combo>()
            .HasData(
                new Combo
                {
                    ComboId = 1,
                    Name = "Combo Solo",
                    Description = "1 Bắp ngọt (L) + 1 Nước ngọt (L)",
                    Price = 89000m,
                    ImageUrl = "https://images.unsplash.com/photo-1585647347483-22b66260dfff?w=500",
                    IsActive = true,
                },
                new Combo
                {
                    ComboId = 2,
                    Name = "Combo Couple",
                    Description = "1 Bắp ngọt (L) + 2 Nước ngọt (L)",
                    Price = 119000m,
                    ImageUrl = "https://images.unsplash.com/photo-1572177812156-58036aae439c?w=500",
                    IsActive = true,
                },
                new Combo
                {
                    ComboId = 3,
                    Name = "Combo Premium",
                    Description = "1 Bắp phô mai (L) + 2 Nước ngọt (L) + 1 Snack",
                    Price = 149000m,
                    ImageUrl = "https://images.unsplash.com/photo-1512149673953-1e251807ec7c?w=500",
                    IsActive = true,
                },
                new Combo
                {
                    ComboId = 4,
                    Name = "Combo Gia Đình",
                    Description = "2 Bắp ngọt (L) + 4 Nước ngọt (L) + 2 Snack",
                    Price = 249000m,
                    ImageUrl = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?w=500",
                    IsActive = true,
                },
                new Combo
                {
                    ComboId = 5,
                    Name = "Combo Caramel",
                    Description = "1 Bắp caramel (L) + 2 Nước ngọt (L)",
                    Price = 135000m,
                    ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?w=500",
                    IsActive = true,
                }
            );

        modelBuilder
            .Entity<Director>()
            .HasData(
                new Director
                {
                    DirectorId = 1,
                    FullName = "Christopher Nolan",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Christopher+Nolan&background=1a1a2e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 2,
                    FullName = "Denis Villeneuve",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Denis+Villeneuve&background=c19a49&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 3,
                    FullName = "Trấn Thành",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Tran+Thanh&background=b83b5e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 4,
                    FullName = "Victor Vũ",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Victor+Vu&background=1b1b2f&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 5,
                    FullName = "Lý Hải",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Ly+Hai&background=d35400&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 6,
                    FullName = "James Cameron",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=James+Cameron&background=0f4c75&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 7,
                    FullName = "Bong Joon-ho",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Bong+Joon+Ho&background=2d3436&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 8,
                    FullName = "Phan Gia Nhật Linh",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Phan+Gia+Nhat+Linh&background=6c5b7b&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 9,
                    FullName = "Shawn Levy",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Shawn+Levy&background=8b0000&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 10,
                    FullName = "Kelsey Mann",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Kelsey+Mann&background=f39c12&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 11,
                    FullName = "Anthony & Joe Russo",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Russo+Brothers&background=34495e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 12,
                    FullName = "Jang Jae-hyun",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Jang+Jae+Hyun&background=2c3e50&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 13,
                    FullName = "Joaquim Dos Santos",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Joaquim+Dos+Santos&background=e74c3c&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 14,
                    FullName = "Mike Mitchell",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Mike+Mitchell&background=27ae60&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 15,
                    FullName = "Makoto Shinkai",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Makoto+Shinkai&background=16a085&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 16,
                    FullName = "Takashi Yamazaki",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Takashi+Yamazaki&background=b03a2e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 17,
                    FullName = "Alessandro Carloni",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Alessandro+Carloni&background=e67e22&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 18,
                    FullName = "Nguyễn Phạm Thành Đạt",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Nguyễn+Phạm+Thành+Đạt&background=2e86ab&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 19,
                    FullName = "Vũ Ngọc Đãng",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Vũ+Ngọc+Đãng&background=7d3c98&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 20,
                    FullName = "Narit Yuvaboon",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Narit+Yuvaboon&background=1e8449&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 21,
                    FullName = "Kitao Sakurai",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Kitao+Sakurai&background=922b21&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 22,
                    FullName = "Nguyễn Hữu Hoàng",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Nguyễn+Hữu+Hoàng&background=5d6d7e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 23,
                    FullName = "Trần Hữu Tấn",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Trần+Hữu+Tấn&background=212f3d&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 24,
                    FullName = "Francis Lawrence",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Francis+Lawrence&background=b7950b&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 25,
                    FullName = "Nguyễn Nhất Trung",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Nguyễn+Nhất+Trung&background=ca6f1e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Director
                {
                    DirectorId = 26,
                    FullName = "John Hamburg",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=John+Hamburg&background=117a65&color=fff&size=500&bold=true&font-size=0.4",
                }
            );

        modelBuilder
            .Entity<Actor>()
            .HasData(
                new Actor
                {
                    ActorId = 1,
                    FullName = "Cillian Murphy",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Cillian+Murphy&background=34495e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 2,
                    FullName = "Robert Downey Jr.",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Robert+Downey+Jr&background=8b0000&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 3,
                    FullName = "Timothée Chalamet",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Timothee+Chalamet&background=c19a49&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 4,
                    FullName = "Zendaya",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Zendaya&background=9b59b6&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 5,
                    FullName = "Trấn Thành",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Tran+Thanh&background=b83b5e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 6,
                    FullName = "Tuấn Trần",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Tuan+Tran&background=2980b9&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 7,
                    FullName = "Uyển Ân",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Uyen+An&background=e67e22&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 8,
                    FullName = "Phương Anh Đào",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Phuong+Anh+Dao&background=d63384&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 9,
                    FullName = "Song Kang-ho",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Song+Kang+Ho&background=2d3436&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 10,
                    FullName = "Choi Woo-shik",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Choi+Woo+Shik&background=16a085&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 11,
                    FullName = "Sam Worthington",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Sam+Worthington&background=0f4c75&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 12,
                    FullName = "Zoe Saldana",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Zoe+Saldana&background=1abc9c&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 13,
                    FullName = "Ryan Reynolds",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Ryan+Reynolds&background=8b0000&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 14,
                    FullName = "Hugh Jackman",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Hugh+Jackman&background=34495e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 15,
                    FullName = "Thái Hòa",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Thai+Hoa&background=d35400&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 16,
                    FullName = "Kaity Nguyễn",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Kaity+Nguyen&background=e84393&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 17,
                    FullName = "Ninh Dương Lan Ngọc",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Ninh+Duong+Lan+Ngoc&background=6c5b7b&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 18,
                    FullName = "Quách Ngọc Ngoan",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Quach+Ngoc+Ngoan&background=1b1b2f&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 19,
                    FullName = "Avin Lu",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Avin+Lu&background=3498db&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 20,
                    FullName = "Hoàng Hà",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Hoang+Ha&background=e67e22&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 21,
                    FullName = "Amy Poehler",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Amy+Poehler&background=f39c12&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 22,
                    FullName = "Maya Hawke",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Maya+Hawke&background=9b59b6&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 23,
                    FullName = "Matthew McConaughey",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Matthew+McConaughey&background=2c3e50&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 24,
                    FullName = "Anne Hathaway",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Anne+Hathaway&background=8e44ad&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 25,
                    FullName = "Trần Nghĩa",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Tran+Nghia&background=16a085&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 26,
                    FullName = "Trúc Anh",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Truc+Anh&background=e91e63&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 27,
                    FullName = "Choi Min-sik",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Choi+Min+Sik&background=34495e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 28,
                    FullName = "Kim Go-eun",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Kim+Go+Eun&background=9c27b0&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 29,
                    FullName = "Lee Do-hyun",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Lee+Do+Hyun&background=3f51b5&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 30,
                    FullName = "Shameik Moore",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Shameik+Moore&background=d32f2f&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 31,
                    FullName = "Hailee Steinfeld",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Hailee+Steinfeld&background=0288d1&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 32,
                    FullName = "Chris Evans",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Chris+Evans&background=1976d2&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 33,
                    FullName = "Scarlett Johansson",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Scarlett+Johansson&background=c2185b&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 34,
                    FullName = "Jack Black",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Jack+Black&background=f57c00&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 35,
                    FullName = "Song Luân",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Song+Luan&background=00796b&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 36,
                    FullName = "Lê Giang",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Le+Giang&background=7b1fa2&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 37,
                    FullName = "Nanoka Hara",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Nanoka+Hara&background=0097a7&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 38,
                    FullName = "Hokuto Matsumura",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Hokuto+Matsumura&background=455a64&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 39,
                    FullName = "Ryunosuke Kamiki",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Ryunosuke+Kamiki&background=34495e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 40,
                    FullName = "Minami Hamabe",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Minami+Hamabe&background=c0392b&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 41,
                    FullName = "Bill Hader",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Bill+Hader&background=2874a6&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 42,
                    FullName = "Xochitl Gomez",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Xochitl+Gomez&background=af601a&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 43,
                    FullName = "Nguyễn Hùng",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Nguyễn+Hùng&background=148f77&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 44,
                    FullName = "Trần Tiểu Vy",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Trần+Tiểu+Vy&background=a93226&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 45,
                    FullName = "Ngọc Xuân",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Ngọc+Xuân&background=6c3483&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 46,
                    FullName = "Võ Điền Gia Huy",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Võ+Điền+Gia+Huy&background=1f618d&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 47,
                    FullName = "Lãnh Thanh",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Lãnh+Thanh&background=7e5109&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 48,
                    FullName = "Hồng Đào",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Hồng+Đào&background=b03a2e&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 49,
                    FullName = "Lê Thiện",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Lê+Thiện&background=2c3e50&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 50,
                    FullName = "Ma Ran Đô",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Ma+Ran+Đô&background=7b241c&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 51,
                    FullName = "Quỳnh Lương",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Quỳnh+Lương&background=6e2c00&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 52,
                    FullName = "Nadech Kugimiya",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Nadech+Kugimiya&background=117864&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 53,
                    FullName = "Ong-art Cheamcharoenpornkul",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Ong-art+Cheamcharoenpornkul&background=273746&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 54,
                    FullName = "Andrew Koji",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Andrew+Koji&background=1a5276&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 55,
                    FullName = "Noah Centineo",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Noah+Centineo&background=af7ac5&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 56,
                    FullName = "Jason Momoa",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Jason+Momoa&background=7b7d7d&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 57,
                    FullName = "Nguyễn Cao Kỳ Duyên",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Nguyễn+Cao+Kỳ+Duyên&background=cb4335&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 58,
                    FullName = "Steven Nguyễn",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Steven+Nguyễn&background=2471a3&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 59,
                    FullName = "Joseph Zada",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Joseph+Zada&background=7d6608&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 60,
                    FullName = "Whitney Peak",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Whitney+Peak&background=884ea0&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 61,
                    FullName = "Mckenna Grace",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Mckenna+Grace&background=148f77&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 62,
                    FullName = "Jesse Plemons",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Jesse+Plemons&background=5f6a6a&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 63,
                    FullName = "Lương Thế Thành",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Lương+Thế+Thành&background=a04000&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 64,
                    FullName = "Sally Hồng Anh",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Sally+Hồng+Anh&background=c2185b&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 65,
                    FullName = "Robert De Niro",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Robert+De+Niro&background=424949&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 66,
                    FullName = "Ben Stiller",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Ben+Stiller&background=2e4053&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 67,
                    FullName = "Ariana Grande",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Ariana+Grande&background=d98880&color=fff&size=500&bold=true&font-size=0.4",
                },
                new Actor
                {
                    ActorId = 68,
                    FullName = "Owen Wilson",
                    AvatarUrl =
                        "https://ui-avatars.com/api/?name=Owen+Wilson&background=b9770e&color=fff&size=500&bold=true&font-size=0.4",
                }
            );

        modelBuilder
            .Entity<CinemaRoom>()
            .HasData(
                new CinemaRoom
                {
                    RoomId = 1,
                    RoomName = "Cinema Hall 01",
                    ScreenTypeId = 1,
                    Rows = Layouts[0].Rows,
                    Cols = Layouts[0].Cols,
                    TotalSeats = Layouts[0].Rows * Layouts[0].Cols,
                    IsActive = true,
                },
                new CinemaRoom
                {
                    RoomId = 2,
                    RoomName = "Cinema Hall 02",
                    ScreenTypeId = 1,
                    Rows = Layouts[1].Rows,
                    Cols = Layouts[1].Cols,
                    TotalSeats = Layouts[1].Rows * Layouts[1].Cols,
                    IsActive = true,
                },
                new CinemaRoom
                {
                    RoomId = 3,
                    RoomName = "3D Experience Hall 03",
                    ScreenTypeId = 2,
                    Rows = Layouts[2].Rows,
                    Cols = Layouts[2].Cols,
                    TotalSeats = Layouts[2].Rows * Layouts[2].Cols,
                    IsActive = true,
                },
                new CinemaRoom
                {
                    RoomId = 4,
                    RoomName = "IMAX Grand Theatre 04",
                    ScreenTypeId = 3,
                    Rows = Layouts[3].Rows,
                    Cols = Layouts[3].Cols,
                    TotalSeats = Layouts[3].Rows * Layouts[3].Cols,
                    IsActive = true,
                }
            );

        // Sơ đồ ghế theo kiểu rạp thật: các hàng đầu là ghế thường, hàng giữa là VIP,
        // hàng CUỐI là ghế đôi (sweetbox). Số cột chẵn để ghép cặp liền kề 1-2, 3-4, ...
        var seats = new List<Seat>();
        var seatIdCounter = 1;

        // Helper tạo CoupleId cố định, duy nhất theo từng cặp ghế liền kề cùng hàng
        static Guid? GetCoupleId(int roomId, int rowIdx, int seatNumber, int seatTypeId)
        {
            if (seatTypeId != 3)
                return null;

            var pairIndex = (seatNumber + 1) / 2; // 1-2 => cặp 1, 3-4 => cặp 2, ...
            return Guid.Parse(
                $"c0000000-0000-0000-{roomId:D4}-{((rowIdx + 1) * 1000 + pairIndex):D12}"
            );
        }

        for (var roomIdx = 0; roomIdx < Layouts.Length; roomIdx++)
        {
            var layout = Layouts[roomIdx];
            var roomId = roomIdx + 1;

            for (var rowIdx = 0; rowIdx < layout.Rows; rowIdx++)
            {
                var row = ((char)('A' + rowIdx)).ToString();
                var seatTypeId =
                    rowIdx < layout.StandardRows ? 1
                    : rowIdx < layout.StandardRows + layout.VipRows ? 2
                    : 3;

                for (var num = 1; num <= layout.Cols; num++)
                {
                    seats.Add(
                        new Seat
                        {
                            SeatId = seatIdCounter++,
                            RoomId = roomId,
                            SeatRow = row,
                            SeatNumber = num,
                            SeatTypeId = seatTypeId,
                            CoupleId = GetCoupleId(roomId, rowIdx, num, seatTypeId),
                            IsActive = true,
                        }
                    );
                }
            }
        }

        modelBuilder.Entity<Seat>().HasData(seats);

        modelBuilder
            .Entity<Movie>()
            .HasData(
                new Movie
                {
                    MovieId = 1,
                    Title = "Oppenheimer",
                    OriginalTitle = "Oppenheimer",
                    Description =
                        "Câu chuyện về J. Robert Oppenheimer, nhà vật lý lý thuyết người Mỹ lãnh đạo Dự án Manhattan phát triển vũ khí nguyên tử đầu tiên trên thế giới trong Thế chiến thứ hai.",
                    DirectorId = 1, // Christopher Nolan
                    DurationMinutes = 180,
                    ReleaseDate = new DateOnly(2023, 8, 11),
                    LanguageId = 2, // Tiếng Anh
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 5, // T18
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/4/4a/Oppenheimer_%28film%29.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=keydELfWuMQ",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.8m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 2,
                    Title = "Dune: Hành Tinh Cát - Phần Hai",
                    OriginalTitle = "Dune: Part Two",
                    Description =
                        "Paul Atreides hợp lực cùng Chani và người Fremen để trả thù những kẻ đã hủy diệt gia đình anh, đồng thời đối mặt với lựa chọn nghiệt ngã giữa tình yêu và vận mệnh của vũ trụ.",
                    DirectorId = 2, // Denis Villeneuve
                    DurationMinutes = 166,
                    ReleaseDate = new DateOnly(2024, 3, 1),
                    LanguageId = 2, // Tiếng Anh
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 4, // T16
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/5/52/Dune_Part_Two_poster.jpeg",
                    TrailerUrl = "https://www.youtube.com/watch?v=Way9Dexny3w",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.7m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 3,
                    Title = "Mai",
                    OriginalTitle = "Mai",
                    Description =
                        "Mai - người phụ nữ gần 40 tuổi làm nghề mát-xa trị liệu mang nhiều vết thương tâm hồn, gặp Dương - chàng trai đào hoa trẻ tuổi quyết tâm theo đuổi cô. Một câu chuyện tình yêu đầy thử thách và định kiến xã hội.",
                    DirectorId = 3, // Trấn Thành
                    DurationMinutes = 131,
                    ReleaseDate = new DateOnly(2024, 2, 10),
                    LanguageId = 1, // Tiếng Việt
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 5, // T18
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/vi/3/36/Mai_2024_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=EX6clvId19s",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.5m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 4,
                    Title = "Ký Sinh Trùng",
                    OriginalTitle = "Parasite",
                    Description =
                        "Gia đình ông Kim nghèo khó tìm cách từng người một thâm nhập vào làm việc cho gia đình ngài Park giàu sang phú quý, mở ra chuỗi bi kịch tầng lớp châm biếm sâu cay.",
                    DirectorId = 7, // Bong Joon-ho
                    DurationMinutes = 132,
                    ReleaseDate = new DateOnly(2019, 6, 21),
                    LanguageId = 3, // Tiếng Hàn
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 5, // T18
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/5/53/Parasite_%282019_film%29.png",
                    TrailerUrl = "https://www.youtube.com/watch?v=isOGD_7hNIY",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.9m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 5,
                    Title = "Avatar: Dòng Chảy Của Nước",
                    OriginalTitle = "Avatar: The Way of Water",
                    Description =
                        "Jake Sully cùng Neytiri và các con phải rời bỏ quê hương, tìm đến nương náu ở vùng biển của tộc Metkayina khi loài người trở lại đe dọa sự sống của Pandora.",
                    DirectorId = 6, // James Cameron
                    DurationMinutes = 192,
                    ReleaseDate = new DateOnly(2022, 12, 16),
                    LanguageId = 2, // Tiếng Anh
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 3, // T13
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/5/54/Avatar_The_Way_of_Water_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=6AvFHlKS6OE",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.6m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 6,
                    Title = "Lật Mặt 7: Một Điều Ước",
                    OriginalTitle = "Face Off 7: One Wish",
                    Description =
                        "Bộ phim gia đình xúc động về bà Hai 73 tuổi neo đơn ở vùng cao Lâm Đồng và những câu chuyện trăn trở của 5 người con đã trưởng thành khi mẹ gặp nạn.",
                    DirectorId = 5, // Lý Hải
                    DurationMinutes = 138,
                    ReleaseDate = new DateOnly(2024, 4, 26),
                    LanguageId = 1, // Tiếng Việt
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 2, // K
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/vi/d/d4/%C3%81p_ph%C3%ADch_ch%C3%ADnh_th%E1%BB%A9c_L%E1%BA%ADt_m%E1%BA%B7t_7.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=d1ZHdosjNX8",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.4m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 7,
                    Title = "Deadpool & Wolverine",
                    OriginalTitle = "Deadpool & Wolverine",
                    Description =
                        "Cơ quan Phương sai Thời gian (TVA) lôi kéo Deadpool vào một nhiệm vụ bảo vệ đa vũ trụ, buộc anh phải hợp tác cùng một biến thể bất đắc dĩ của Wolverine.",
                    DirectorId = 9, // Shawn Levy
                    DurationMinutes = 128,
                    ReleaseDate = new DateOnly(2024, 7, 26),
                    LanguageId = 2, // Tiếng Anh
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 5, // T18
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/vi/4/4c/Deadpool_%26_Wolverine_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=73_1biulkYk",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.7m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 8,
                    Title = "Những Mảnh Ghép Cảm Xúc 2",
                    OriginalTitle = "Inside Out 2",
                    Description =
                        "Cô bé Riley bước vào tuổi dậy thì, kéo theo sự xuất hiện của những cảm xúc mới tinh vi như Lo Âu, Ganh Tị, Xấu Hổ và Chán Nản bên cạnh nhóm Vui Vẻ quen thuộc.",
                    DirectorId = 10, // Kelsey Mann
                    DurationMinutes = 96,
                    ReleaseDate = new DateOnly(2024, 6, 14),
                    LanguageId = 2, // Tiếng Anh
                    Subtitle = "Lồng tiếng & Phụ đề",
                    AgeRatingId = 1, // P
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/f/f7/Inside_Out_2_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=8rjvKzYeSp0",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.8m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 9,
                    Title = "Em Và Trịnh",
                    OriginalTitle = "Em và Trịnh",
                    Description =
                        "Tác phẩm tái hiện cuộc đời tài hoa, lãng tử của cố nhạc sĩ Trịnh Công Sơn qua những giai đoạn gắn liền với các 'nàng thơ' Dao Ánh, Bích Diễm, Khánh Ly, Michiko.",
                    DirectorId = 8, // Phan Gia Nhật Linh
                    DurationMinutes = 136,
                    ReleaseDate = new DateOnly(2022, 6, 17),
                    LanguageId = 1, // Tiếng Việt
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 3, // T13
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/vi/b/b3/Poster_gioi_thieu_phim_Em_va_Trinh.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=zzik4JB9D1Q",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.2m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 10,
                    Title = "Người Bất Tử",
                    OriginalTitle = "The Immortal",
                    Description =
                        "Bộ phim kỳ ảo tâm linh về Hùng - người đàn ông nắm giữ bí thuật ngải bất tử vượt qua 3 thế kỷ đầy tham vọng, thù hận và sự trừng phạt nghiệt ngã của số phận.",
                    DirectorId = 4, // Victor Vũ
                    DurationMinutes = 130,
                    ReleaseDate = new DateOnly(2018, 10, 26),
                    LanguageId = 1, // Tiếng Việt
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 5, // T18
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/b/b9/The_Immortal_%282018_film%29_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=6VEVPtFDN_U",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.1m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 11,
                    Title = "Hố Đen Tử Thần",
                    OriginalTitle = "Interstellar",
                    Description =
                        "Khi Trái Đất bên bờ diệt vong, một nhóm nhà thám hiểm vũ trụ dấn thân vào hố sâu không gian qua một lỗ sâu mới xuất hiện để tìm kiếm hành tinh mới cho sự sống còn của nhân loại.",
                    DirectorId = 1, // Christopher Nolan
                    DurationMinutes = 169,
                    ReleaseDate = new DateOnly(2014, 11, 7),
                    LanguageId = 2, // Tiếng Anh
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 3, // T13
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/b/bc/Interstellar_film_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=zSWdZVtXT7E",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.9m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 12,
                    Title = "Mắt Biếc",
                    OriginalTitle = "Dreamy Eyes",
                    Description =
                        "Chuyển thể từ truyện dài cùng tên của nhà văn Nguyễn Nhật Ánh, kể về mối tình đơn phương vừa tha thiết vừa đượm buồn của Ngạn dành cho cô bạn thanh mai trúc mã Hà Lan có đôi mắt biếc.",
                    DirectorId = 4, // Victor Vũ
                    DurationMinutes = 117,
                    ReleaseDate = new DateOnly(2019, 12, 20),
                    LanguageId = 1, // Tiếng Việt
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 3, // T13
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/vi/4/42/%C3%81p_ph%C3%ADch_phim_M%E1%BA%AFt_bi%E1%BA%BFc.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=ITlQ0oU7tDA",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.5m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 13,
                    Title = "Bố Già",
                    OriginalTitle = "Dad, I'm Sorry",
                    Description =
                        "Tác phẩm xoay quanh đại gia đình Giàu - Sang - Phú - Quý tại một con hẻm nghèo ở Sài Gòn, đặc biệt là mối quan hệ mâu thuẫn thế hệ đầy day dứt và tình yêu thương sâu sắc giữa Ba Sang và Quắn.",
                    DirectorId = 3, // Trấn Thành
                    DurationMinutes = 128,
                    ReleaseDate = new DateOnly(2021, 3, 5),
                    LanguageId = 1, // Tiếng Việt
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 2, // K
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/vi/7/72/Bo_gia_2021_ap_phich.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=jluSu8Rw6YE",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.4m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 14,
                    Title = "Quật Mộ Trùng Ma",
                    OriginalTitle = "Exhuma",
                    Description =
                        "Một nhóm pháp sư, thầy phong thủy và chuyên gia tang lễ hàng đầu Hàn Quốc nhận lời khai quật một ngôi mộ bí ẩn với mức thù lao khổng lồ, vô tình giải thoát một thế lực hắc ám cổ xưa rùng rợn.",
                    DirectorId = 12, // Jang Jae-hyun
                    DurationMinutes = 134,
                    ReleaseDate = new DateOnly(2024, 3, 15),
                    LanguageId = 3, // Tiếng Hàn
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 5, // T18
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/1/13/Exhuma_film_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=nO3_h3fJ3-s",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.6m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 15,
                    Title = "Người Nhện: Du Hành Vũ Trụ Nhện",
                    OriginalTitle = "Spider-Man: Across the Spider-Verse",
                    Description =
                        "Miles Morales tái ngộ cùng Gwen Stacy và bị kéo vào đa vũ trụ đầy hiểm họa. Tại đây, Miles phải đối đầu với Liên minh Người Nhện để bảo vệ định mệnh và người thân yêu nhất của mình.",
                    DirectorId = 13, // Joaquim Dos Santos
                    DurationMinutes = 140,
                    ReleaseDate = new DateOnly(2023, 6, 2),
                    LanguageId = 2, // Tiếng Anh
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 2, // K
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/b/b4/Spider-Man-_Across_the_Spider-Verse_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=cqGjhVJWtEg",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.9m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 16,
                    Title = "Avengers: Hồi Kết",
                    OriginalTitle = "Avengers: Endgame",
                    Description =
                        "Sau cú búng tay hủy diệt của Thanos, các Avengers còn sống sót tập hợp lực lượng một lần cuối cùng, thực hiện kế hoạch du hành thời gian đầy táo bạo nhằm khôi phục vũ trụ.",
                    DirectorId = 11, // Anthony & Joe Russo
                    DurationMinutes = 181,
                    ReleaseDate = new DateOnly(2019, 4, 26),
                    LanguageId = 2, // Tiếng Anh
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 3, // T13
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/0/0d/Avengers_Endgame_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=TcMBFSGVi1c",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.9m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 17,
                    Title = "Kung Fu Panda 4",
                    OriginalTitle = "Kung Fu Panda 4",
                    Description =
                        "Po chuẩn bị trở thành Thủ lĩnh Tinh thần của Thung lũng Bình Yên và phải huấn luyện một chiến binh rồng mới, đồng thời đối đầu với mụ phù thủy Tắc Kè Hoa có khả năng biến hình nguy hiểm.",
                    DirectorId = 14, // Mike Mitchell
                    DurationMinutes = 94,
                    ReleaseDate = new DateOnly(2024, 3, 8),
                    LanguageId = 2, // Tiếng Anh
                    Subtitle = "Lồng tiếng & Phụ đề",
                    AgeRatingId = 1, // P
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/7/7f/Kung_Fu_Panda_4_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=_inKs4eeHiI",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.3m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 18,
                    Title = "Nhà Bà Nữ",
                    OriginalTitle = "The House of No Man",
                    Description =
                        "Câu chuyện xoay quanh tiệm bánh canh cua của bà Ngọc Nữ và những rắc rối, mâu thuẫn tình cảm trong một gia đình nhiều thế hệ toàn phụ nữ nơi sự kiểm soát thái quá dẫn đến đổ vỡ.",
                    DirectorId = 3, // Trấn Thành
                    DurationMinutes = 102,
                    ReleaseDate = new DateOnly(2023, 1, 22),
                    LanguageId = 1, // Tiếng Việt
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 4, // T16
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/vi/6/6f/%C3%81p_ph%C3%ADch_phim_Nh%C3%A0_b%C3%A0_N%E1%BB%AF.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=684y48YgqjA",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.3m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 19,
                    Title = "Khóa Chặt Cửa Nào Suzume",
                    OriginalTitle = "Suzume",
                    Description =
                        "Cô nữ sinh 17 tuổi Suzume gặp gỡ chàng trai kỳ lạ Souta và bắt đầu hành trình băng qua khắp nước Nhật để đóng lại những cánh cửa giải phóng tai ương thảm họa bảo vệ sự bình yên nhân loại.",
                    DirectorId = 15, // Makoto Shinkai
                    DurationMinutes = 122,
                    ReleaseDate = new DateOnly(2023, 3, 10),
                    LanguageId = 4, // Tiếng Nhật
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 2, // K
                    PosterUrl =
                        "https://upload.wikimedia.org/wikipedia/en/7/7f/Suzume_no_Tojimari_poster.jpg",
                    TrailerUrl = "https://www.youtube.com/watch?v=5pY4qVz9rEQ",
                    Status = MovieStatus.Ended,
                    AverageRating = 4.7m,
                    CreatedAt = SeedDate,
                },
                new Movie
                {
                    MovieId = 20,
                    Title = "Godzilla Trừ Không",
                    OriginalTitle = "Godzilla Minus Zero",
                    Description =
                        "Phần tiếp nối Godzilla Minus One do Takashi Yamazaki thực hiện, đưa khán giả trở lại nước Nhật hậu chiến trước sự đe dọa của quái vật huyền thoại Godzilla.",
                    DirectorId = 16,
                    DurationMinutes = 135,
                    ReleaseDate = new DateOnly(2026, 11, 6),
                    LanguageId = 4,
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 3,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.NowShowing,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 11, 6, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 21,
                    Title = "Chàng Mèo Mang Mũ",
                    OriginalTitle = "The Cat in the Hat",
                    Description =
                        "Phim hoạt hình phiêu lưu hài hước dựa trên tác phẩm kinh điển của Dr. Seuss: chú mèo tinh nghịch đội chiếc mũ sọc đỏ trắng biến một ngày mưa buồn tẻ thành cuộc phiêu lưu hỗn loạn.",
                    DirectorId = 17,
                    DurationMinutes = 114,
                    ReleaseDate = new DateOnly(2026, 11, 6),
                    LanguageId = 2,
                    Subtitle = "Lồng tiếng & Phụ đề",
                    AgeRatingId = 1,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.NowShowing,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 11, 6, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 22,
                    Title = "Bò Sữa Bay",
                    OriginalTitle = "Bò Sữa Bay",
                    Description =
                        "Phim hài hoài niệm pha yếu tố khoa học viễn tưởng, lấy bối cảnh một nông trại ở Mộc Châu những năm 1990. Tác phẩm điện ảnh đầu tay của đạo diễn Nguyễn Phạm Thành Đạt.",
                    DirectorId = 18,
                    DurationMinutes = 120,
                    ReleaseDate = new DateOnly(2026, 11, 6),
                    LanguageId = 1,
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 3,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.NowShowing,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 11, 6, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 23,
                    Title = "Ngày Con Còn Mẹ",
                    OriginalTitle = "Ngày Con Còn Mẹ",
                    Description =
                        "Phim tâm lý gia đình của đạo diễn Vũ Ngọc Đãng, khai thác tình mẫu tử và những rạn nứt, hàn gắn giữa các thế hệ trong một gia đình.",
                    DirectorId = 19,
                    DurationMinutes = 110,
                    ReleaseDate = new DateOnly(2026, 11, 11),
                    LanguageId = 1,
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 3,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.NowShowing,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 11, 11, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 24,
                    Title = "Quỷ Ăn Tạng 4: Hổ Tinh",
                    OriginalTitle = null,
                    Description =
                        "Phần thứ tư của loạt phim kinh dị siêu nhiên Thái Lan Quỷ Ăn Tạng, kết hợp yếu tố hành động.",
                    DirectorId = 20,
                    DurationMinutes = 111,
                    ReleaseDate = new DateOnly(2026, 10, 9),
                    LanguageId = 5,
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 5,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.NowShowing,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 10, 9, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 25,
                    Title = "Street Fighter",
                    OriginalTitle = "Street Fighter",
                    Description =
                        "Chuyển thể từ series game đối kháng huyền thoại của Capcom, quy tụ các võ sĩ từ khắp thế giới trong giải đấu Street Fighter.",
                    DirectorId = 21,
                    DurationMinutes = 125,
                    ReleaseDate = new DateOnly(2026, 10, 16),
                    LanguageId = 2,
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 4,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.NowShowing,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 10, 16, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 26,
                    Title = "Chị Chị Em Em 3",
                    OriginalTitle = "Chị Chị Em Em 3",
                    Description =
                        "Phần ba của loạt phim chính kịch cổ trang Việt Nam về những mối quan hệ chị em đầy ganh ghét và toan tính.",
                    DirectorId = 22,
                    DurationMinutes = 115,
                    ReleaseDate = new DateOnly(2026, 10, 16),
                    LanguageId = 1,
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 5,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.NowShowing,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 10, 16, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 27,
                    Title = "Trấn Yểm",
                    OriginalTitle = "Trấn Yểm",
                    Description =
                        "Phim kinh dị tâm linh Việt Nam của đạo diễn Trần Hữu Tấn.",
                    DirectorId = 23,
                    DurationMinutes = 96,
                    ReleaseDate = new DateOnly(2026, 10, 30),
                    LanguageId = 1,
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 5,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.NowShowing,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 10, 30, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 28,
                    Title = "Đấu Trường Sinh Tử: Bình Minh Ngày Chiêu Quân",
                    OriginalTitle = "The Hunger Games: Sunrise on the Reaping",
                    Description =
                        "Phần tiền truyện mới của loạt phim Đấu Trường Sinh Tử do Francis Lawrence đạo diễn, quay về thời điểm Hội Chiêu Quân diễn ra.",
                    DirectorId = 24,
                    DurationMinutes = 158,
                    ReleaseDate = new DateOnly(2026, 11, 20),
                    LanguageId = 2,
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 4,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.ComingSoon,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 11, 20, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 29,
                    Title = "Trại Giam Hạnh Phúc",
                    OriginalTitle = "Trại Giam Hạnh Phúc",
                    Description =
                        "Phim hài – gia đình – tâm lý của đạo diễn Nguyễn Nhất Trung (Trung Lùn).",
                    DirectorId = 25,
                    DurationMinutes = 120,
                    ReleaseDate = new DateOnly(2026, 11, 20),
                    LanguageId = 1,
                    Subtitle = "Tiếng Anh",
                    AgeRatingId = 3,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.ComingSoon,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 11, 20, 0, 0, 0).AddDays(-14),
                },
                new Movie
                {
                    MovieId = 30,
                    Title = "Gặp Gỡ Thông Gia: Dâu Mới Trình Làng",
                    OriginalTitle = "Focker-in-Law",
                    Description =
                        "Phần tiếp theo của loạt phim hài Meet the Parents với dàn diễn viên Robert De Niro, Ben Stiller, Ariana Grande và Owen Wilson.",
                    DirectorId = 26,
                    DurationMinutes = 90,
                    ReleaseDate = new DateOnly(2026, 11, 27),
                    LanguageId = 2,
                    Subtitle = "Tiếng Việt",
                    AgeRatingId = 3,
                    // Poster/Trailer: cập nhật từ trang quản trị sau khi nhà phát hành công bố.
                    PosterUrl = null,
                    TrailerUrl = null,
                    Status = MovieStatus.ComingSoon,
                    // Chưa có đánh giá: điểm trung bình sẽ được tính từ bảng Review.
                    AverageRating = 0m,
                    CreatedAt = new DateTime(2026, 11, 27, 0, 0, 0).AddDays(-14),
                }
            );

        modelBuilder
            .Entity<MovieGenre>()
            .HasData(
                // Oppenheimer: Chính kịch (3), Tâm lý (8)
                new MovieGenre { MovieId = 1, GenreId = 3 },
                new MovieGenre { MovieId = 1, GenreId = 8 },
                // Dune 2: Hành động (1), Khoa học viễn tưởng (5), Chính kịch (3)
                new MovieGenre { MovieId = 2, GenreId = 1 },
                new MovieGenre { MovieId = 2, GenreId = 5 },
                new MovieGenre { MovieId = 2, GenreId = 3 },
                // Mai: Chính kịch (3), Lãng mạn (7), Tâm lý (8)
                new MovieGenre { MovieId = 3, GenreId = 3 },
                new MovieGenre { MovieId = 3, GenreId = 7 },
                new MovieGenre { MovieId = 3, GenreId = 8 },
                // Parasite: Chính kịch (3), Kinh dị (4), Tâm lý (8)
                new MovieGenre { MovieId = 4, GenreId = 3 },
                new MovieGenre { MovieId = 4, GenreId = 4 },
                new MovieGenre { MovieId = 4, GenreId = 8 },
                // Avatar 2: Hành động (1), Khoa học viễn tưởng (5)
                new MovieGenre { MovieId = 5, GenreId = 1 },
                new MovieGenre { MovieId = 5, GenreId = 5 },
                // Lật Mặt 7: Chính kịch (3), Hài (2)
                new MovieGenre { MovieId = 6, GenreId = 3 },
                new MovieGenre { MovieId = 6, GenreId = 2 },
                // Deadpool & Wolverine: Hành động (1), Hài (2), Khoa học viễn tưởng (5)
                new MovieGenre { MovieId = 7, GenreId = 1 },
                new MovieGenre { MovieId = 7, GenreId = 2 },
                new MovieGenre { MovieId = 7, GenreId = 5 },
                // Inside Out 2: Hoạt hình (6), Hài (2), Tâm lý (8)
                new MovieGenre { MovieId = 8, GenreId = 6 },
                new MovieGenre { MovieId = 8, GenreId = 2 },
                new MovieGenre { MovieId = 8, GenreId = 8 },
                // Em Và Trịnh: Chính kịch (3), Lãng mạn (7)
                new MovieGenre { MovieId = 9, GenreId = 3 },
                new MovieGenre { MovieId = 9, GenreId = 7 },
                // Người Bất Tử: Kinh dị (4), Tâm lý (8)
                new MovieGenre { MovieId = 10, GenreId = 4 },
                new MovieGenre { MovieId = 10, GenreId = 8 },
                // Interstellar: Khoa học viễn tưởng (5), Phiêu lưu (9), Chính kịch (3)
                new MovieGenre { MovieId = 11, GenreId = 5 },
                new MovieGenre { MovieId = 11, GenreId = 9 },
                new MovieGenre { MovieId = 11, GenreId = 3 },
                // Mắt Biếc: Lãng mạn (7), Chính kịch (3)
                new MovieGenre { MovieId = 12, GenreId = 7 },
                new MovieGenre { MovieId = 12, GenreId = 3 },
                // Bố Già: Hài (2), Chính kịch (3), Tâm lý (8)
                new MovieGenre { MovieId = 13, GenreId = 2 },
                new MovieGenre { MovieId = 13, GenreId = 3 },
                new MovieGenre { MovieId = 13, GenreId = 8 },
                // Quật Mộ Trùng Ma: Kinh dị (4), Bí ẩn (10), Tâm lý (8)
                new MovieGenre { MovieId = 14, GenreId = 4 },
                new MovieGenre { MovieId = 14, GenreId = 10 },
                new MovieGenre { MovieId = 14, GenreId = 8 },
                // Spider-Man Across the Spider-Verse: Hoạt hình (6), Hành động (1), Khoa học viễn tưởng (5)
                new MovieGenre { MovieId = 15, GenreId = 6 },
                new MovieGenre { MovieId = 15, GenreId = 1 },
                new MovieGenre { MovieId = 15, GenreId = 5 },
                // Avengers: Endgame: Hành động (1), Khoa học viễn tưởng (5), Phiêu lưu (9)
                new MovieGenre { MovieId = 16, GenreId = 1 },
                new MovieGenre { MovieId = 16, GenreId = 5 },
                new MovieGenre { MovieId = 16, GenreId = 9 },
                // Kung Fu Panda 4: Hoạt hình (6), Hành động (1), Hài (2)
                new MovieGenre { MovieId = 17, GenreId = 6 },
                new MovieGenre { MovieId = 17, GenreId = 1 },
                new MovieGenre { MovieId = 17, GenreId = 2 },
                // Nhà Bà Nữ: Hài (2), Chính kịch (3), Tâm lý (8)
                new MovieGenre { MovieId = 18, GenreId = 2 },
                new MovieGenre { MovieId = 18, GenreId = 3 },
                new MovieGenre { MovieId = 18, GenreId = 8 },
                // Suzume: Hoạt hình (6), Phiêu lưu (9), Bí ẩn (10)
                new MovieGenre { MovieId = 19, GenreId = 6 },
                new MovieGenre { MovieId = 19, GenreId = 9 },
                new MovieGenre { MovieId = 19, GenreId = 10 },
                // Phim mới 10-11/2026
                new MovieGenre { MovieId = 20, GenreId = 1 },
                new MovieGenre { MovieId = 20, GenreId = 5 },
                new MovieGenre { MovieId = 21, GenreId = 6 },
                new MovieGenre { MovieId = 21, GenreId = 11 },
                new MovieGenre { MovieId = 21, GenreId = 9 },
                new MovieGenre { MovieId = 22, GenreId = 2 },
                new MovieGenre { MovieId = 22, GenreId = 1 },
                new MovieGenre { MovieId = 22, GenreId = 8 },
                new MovieGenre { MovieId = 23, GenreId = 3 },
                new MovieGenre { MovieId = 23, GenreId = 8 },
                new MovieGenre { MovieId = 23, GenreId = 11 },
                new MovieGenre { MovieId = 24, GenreId = 4 },
                new MovieGenre { MovieId = 24, GenreId = 12 },
                new MovieGenre { MovieId = 25, GenreId = 1 },
                new MovieGenre { MovieId = 26, GenreId = 3 },
                new MovieGenre { MovieId = 27, GenreId = 4 },
                new MovieGenre { MovieId = 27, GenreId = 12 },
                new MovieGenre { MovieId = 28, GenreId = 1 },
                new MovieGenre { MovieId = 28, GenreId = 9 },
                new MovieGenre { MovieId = 28, GenreId = 5 },
                new MovieGenre { MovieId = 29, GenreId = 2 },
                new MovieGenre { MovieId = 29, GenreId = 11 },
                new MovieGenre { MovieId = 29, GenreId = 8 },
                new MovieGenre { MovieId = 30, GenreId = 2 },
                new MovieGenre { MovieId = 30, GenreId = 11 }
            );

        modelBuilder
            .Entity<MovieCast>()
            .HasData(
                // Oppenheimer (1)
                new MovieCast
                {
                    MovieId = 1,
                    ActorId = 1, // Cillian Murphy
                    CharacterName = "J. Robert Oppenheimer",
                },
                new MovieCast
                {
                    MovieId = 1,
                    ActorId = 2, // Robert Downey Jr.
                    CharacterName = "Lewis Strauss",
                },
                // Dune 2 (2)
                new MovieCast
                {
                    MovieId = 2,
                    ActorId = 3, // Timothée Chalamet
                    CharacterName = "Paul Atreides",
                },
                new MovieCast
                {
                    MovieId = 2,
                    ActorId = 4, // Zendaya
                    CharacterName = "Chani",
                },
                // Mai (3)
                new MovieCast
                {
                    MovieId = 3,
                    ActorId = 8, // Phương Anh Đào
                    CharacterName = "Mai",
                },
                new MovieCast
                {
                    MovieId = 3,
                    ActorId = 6, // Tuấn Trần
                    CharacterName = "Trùng Dương (Sâu)",
                },
                new MovieCast
                {
                    MovieId = 3,
                    ActorId = 5, // Trấn Thành
                    CharacterName = "Ông Hoàng",
                },
                new MovieCast
                {
                    MovieId = 3,
                    ActorId = 7, // Uyển Ân
                    CharacterName = "Bình Minh",
                },
                // Parasite (4)
                new MovieCast
                {
                    MovieId = 4,
                    ActorId = 9, // Song Kang-ho
                    CharacterName = "Kim Ki-taek",
                },
                new MovieCast
                {
                    MovieId = 4,
                    ActorId = 10, // Choi Woo-shik
                    CharacterName = "Kim Ki-woo",
                },
                // Avatar 2 (5)
                new MovieCast
                {
                    MovieId = 5,
                    ActorId = 11, // Sam Worthington
                    CharacterName = "Jake Sully",
                },
                new MovieCast
                {
                    MovieId = 5,
                    ActorId = 12, // Zoe Saldana
                    CharacterName = "Neytiri",
                },
                // Lật Mặt 7 (6)
                new MovieCast
                {
                    MovieId = 6,
                    ActorId = 15, // Thái Hòa
                    CharacterName = "Ba Tràng",
                },
                new MovieCast
                {
                    MovieId = 6,
                    ActorId = 18, // Quách Ngọc Ngoan
                    CharacterName = "Tư Hậu",
                },
                // Deadpool & Wolverine (7)
                new MovieCast
                {
                    MovieId = 7,
                    ActorId = 13, // Ryan Reynolds
                    CharacterName = "Wade Wilson / Deadpool",
                },
                new MovieCast
                {
                    MovieId = 7,
                    ActorId = 14, // Hugh Jackman
                    CharacterName = "Logan / Wolverine",
                },
                // Inside Out 2 (8)
                new MovieCast
                {
                    MovieId = 8,
                    ActorId = 21, // Amy Poehler
                    CharacterName = "Joy (Vui Vẻ)",
                },
                new MovieCast
                {
                    MovieId = 8,
                    ActorId = 22, // Maya Hawke
                    CharacterName = "Anxiety (Lo Âu)",
                },
                // Em Và Trịnh (9)
                new MovieCast
                {
                    MovieId = 9,
                    ActorId = 19, // Avin Lu
                    CharacterName = "Trịnh Công Sơn (thời trẻ)",
                },
                new MovieCast
                {
                    MovieId = 9,
                    ActorId = 20, // Hoàng Hà
                    CharacterName = "Dao Ánh",
                },
                // Người Bất Tử (10)
                new MovieCast
                {
                    MovieId = 10,
                    ActorId = 18, // Quách Ngọc Ngoan
                    CharacterName = "Hùng",
                },
                new MovieCast
                {
                    MovieId = 10,
                    ActorId = 17, // Ninh Dương Lan Ngọc
                    CharacterName = "Liên",
                },
                // Interstellar (11)
                new MovieCast
                {
                    MovieId = 11,
                    ActorId = 23, // Matthew McConaughey
                    CharacterName = "Joseph Cooper",
                },
                new MovieCast
                {
                    MovieId = 11,
                    ActorId = 24, // Anne Hathaway
                    CharacterName = "Dr. Amelia Brand",
                },
                // Mắt Biếc (12)
                new MovieCast
                {
                    MovieId = 12,
                    ActorId = 25, // Trần Nghĩa
                    CharacterName = "Ngạn",
                },
                new MovieCast
                {
                    MovieId = 12,
                    ActorId = 26, // Trúc Anh
                    CharacterName = "Hà Lan",
                },
                // Bố Già (13)
                new MovieCast
                {
                    MovieId = 13,
                    ActorId = 5, // Trấn Thành
                    CharacterName = "Ba Sang",
                },
                new MovieCast
                {
                    MovieId = 13,
                    ActorId = 6, // Tuấn Trần
                    CharacterName = "Quắn",
                },
                new MovieCast
                {
                    MovieId = 13,
                    ActorId = 36, // Lê Giang
                    CharacterName = "Cẩm Lệ",
                },
                // Quật Mộ Trùng Ma (14)
                new MovieCast
                {
                    MovieId = 14,
                    ActorId = 27, // Choi Min-sik
                    CharacterName = "Kim Sang-deok",
                },
                new MovieCast
                {
                    MovieId = 14,
                    ActorId = 28, // Kim Go-eun
                    CharacterName = "Lee Hwa-rim",
                },
                new MovieCast
                {
                    MovieId = 14,
                    ActorId = 29, // Lee Do-hyun
                    CharacterName = "Yoon Bong-gil",
                },
                // Spider-Man: Across the Spider-Verse (15)
                new MovieCast
                {
                    MovieId = 15,
                    ActorId = 30, // Shameik Moore
                    CharacterName = "Miles Morales / Spider-Man",
                },
                new MovieCast
                {
                    MovieId = 15,
                    ActorId = 31, // Hailee Steinfeld
                    CharacterName = "Gwen Stacy / Spider-Woman",
                },
                // Avengers: Endgame (16)
                new MovieCast
                {
                    MovieId = 16,
                    ActorId = 2, // Robert Downey Jr.
                    CharacterName = "Tony Stark / Iron Man",
                },
                new MovieCast
                {
                    MovieId = 16,
                    ActorId = 32, // Chris Evans
                    CharacterName = "Steve Rogers / Captain America",
                },
                new MovieCast
                {
                    MovieId = 16,
                    ActorId = 33, // Scarlett Johansson
                    CharacterName = "Natasha Romanoff / Black Widow",
                },
                // Kung Fu Panda 4 (17)
                new MovieCast
                {
                    MovieId = 17,
                    ActorId = 34, // Jack Black
                    CharacterName = "Po",
                },
                // Nhà Bà Nữ (18)
                new MovieCast
                {
                    MovieId = 18,
                    ActorId = 36, // Lê Giang
                    CharacterName = "Bà Ngọc Nữ",
                },
                new MovieCast
                {
                    MovieId = 18,
                    ActorId = 7, // Uyển Ân
                    CharacterName = "Ngọc Nhi",
                },
                new MovieCast
                {
                    MovieId = 18,
                    ActorId = 35, // Song Luân
                    CharacterName = "John",
                },
                new MovieCast
                {
                    MovieId = 18,
                    ActorId = 5, // Trấn Thành
                    CharacterName = "Phú Nhuận",
                },
                // Suzume (19)
                new MovieCast
                {
                    MovieId = 19,
                    ActorId = 37, // Nanoka Hara
                    CharacterName = "Suzume Iwato",
                },
                new MovieCast
                {
                    MovieId = 19,
                    ActorId = 38, // Hokuto Matsumura
                    CharacterName = "Souta Munakata",
                },
                // Phim mới 10-11/2026
                new MovieCast
                {
                    MovieId = 20,
                    ActorId = 39, // Ryunosuke Kamiki
                },
                new MovieCast
                {
                    MovieId = 20,
                    ActorId = 40, // Minami Hamabe
                },
                new MovieCast
                {
                    MovieId = 21,
                    ActorId = 41, // Bill Hader
                },
                new MovieCast
                {
                    MovieId = 21,
                    ActorId = 42, // Xochitl Gomez
                },
                new MovieCast
                {
                    MovieId = 22,
                    ActorId = 43, // Nguyễn Hùng
                },
                new MovieCast
                {
                    MovieId = 22,
                    ActorId = 44, // Trần Tiểu Vy
                },
                new MovieCast
                {
                    MovieId = 22,
                    ActorId = 45, // Ngọc Xuân
                },
                new MovieCast
                {
                    MovieId = 22,
                    ActorId = 46, // Võ Điền Gia Huy
                },
                new MovieCast
                {
                    MovieId = 22,
                    ActorId = 47, // Lãnh Thanh
                },
                new MovieCast
                {
                    MovieId = 23,
                    ActorId = 48, // Hồng Đào
                },
                new MovieCast
                {
                    MovieId = 23,
                    ActorId = 8, // Phương Anh Đào
                },
                new MovieCast
                {
                    MovieId = 23,
                    ActorId = 49, // Lê Thiện
                },
                new MovieCast
                {
                    MovieId = 23,
                    ActorId = 50, // Ma Ran Đô
                },
                new MovieCast
                {
                    MovieId = 23,
                    ActorId = 51, // Quỳnh Lương
                },
                new MovieCast
                {
                    MovieId = 24,
                    ActorId = 52, // Nadech Kugimiya
                },
                new MovieCast
                {
                    MovieId = 24,
                    ActorId = 53, // Ong-art Cheamcharoenpornkul
                },
                new MovieCast
                {
                    MovieId = 25,
                    ActorId = 54, // Andrew Koji
                },
                new MovieCast
                {
                    MovieId = 25,
                    ActorId = 55, // Noah Centineo
                },
                new MovieCast
                {
                    MovieId = 25,
                    ActorId = 56, // Jason Momoa
                },
                new MovieCast
                {
                    MovieId = 26,
                    ActorId = 57, // Nguyễn Cao Kỳ Duyên
                },
                new MovieCast
                {
                    MovieId = 26,
                    ActorId = 58, // Steven Nguyễn
                },
                new MovieCast
                {
                    MovieId = 28,
                    ActorId = 59, // Joseph Zada
                },
                new MovieCast
                {
                    MovieId = 28,
                    ActorId = 60, // Whitney Peak
                },
                new MovieCast
                {
                    MovieId = 28,
                    ActorId = 61, // Mckenna Grace
                },
                new MovieCast
                {
                    MovieId = 28,
                    ActorId = 62, // Jesse Plemons
                },
                new MovieCast
                {
                    MovieId = 29,
                    ActorId = 6, // Tuấn Trần
                },
                new MovieCast
                {
                    MovieId = 29,
                    ActorId = 50, // Ma Ran Đô
                },
                new MovieCast
                {
                    MovieId = 29,
                    ActorId = 63, // Lương Thế Thành
                },
                new MovieCast
                {
                    MovieId = 29,
                    ActorId = 64, // Sally Hồng Anh
                },
                new MovieCast
                {
                    MovieId = 30,
                    ActorId = 65, // Robert De Niro
                },
                new MovieCast
                {
                    MovieId = 30,
                    ActorId = 66, // Ben Stiller
                },
                new MovieCast
                {
                    MovieId = 30,
                    ActorId = 67, // Ariana Grande
                },
                new MovieCast
                {
                    MovieId = 30,
                    ActorId = 68, // Owen Wilson
                }
            );
    }
}
