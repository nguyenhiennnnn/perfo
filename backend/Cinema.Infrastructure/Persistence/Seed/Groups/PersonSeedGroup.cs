using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Persistence.Seed.Groups;

internal sealed class PersonSeedGroup : ISeedGroup
{
    private sealed record PersonSeed(string FullName, string AvatarUrl);

    private static readonly PersonSeed[] Directors =
    [
        new("Christopher Nolan", "https://ui-avatars.com/api/?name=Christopher+Nolan&background=1a1a2e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Denis Villeneuve", "https://ui-avatars.com/api/?name=Denis+Villeneuve&background=c19a49&color=fff&size=500&bold=true&font-size=0.4"),
        new("Trấn Thành", "https://ui-avatars.com/api/?name=Tran+Thanh&background=b83b5e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Victor Vũ", "https://ui-avatars.com/api/?name=Victor+Vu&background=1b1b2f&color=fff&size=500&bold=true&font-size=0.4"),
        new("Lý Hải", "https://ui-avatars.com/api/?name=Ly+Hai&background=d35400&color=fff&size=500&bold=true&font-size=0.4"),
        new("James Cameron", "https://ui-avatars.com/api/?name=James+Cameron&background=0f4c75&color=fff&size=500&bold=true&font-size=0.4"),
        new("Bong Joon-ho", "https://ui-avatars.com/api/?name=Bong+Joon+Ho&background=2d3436&color=fff&size=500&bold=true&font-size=0.4"),
        new("Phan Gia Nhật Linh", "https://ui-avatars.com/api/?name=Phan+Gia+Nhat+Linh&background=6c5b7b&color=fff&size=500&bold=true&font-size=0.4"),
        new("Shawn Levy", "https://ui-avatars.com/api/?name=Shawn+Levy&background=8b0000&color=fff&size=500&bold=true&font-size=0.4"),
        new("Kelsey Mann", "https://ui-avatars.com/api/?name=Kelsey+Mann&background=f39c12&color=fff&size=500&bold=true&font-size=0.4"),
        new("Anthony & Joe Russo", "https://ui-avatars.com/api/?name=Russo+Brothers&background=34495e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Jang Jae-hyun", "https://ui-avatars.com/api/?name=Jang+Jae+Hyun&background=2c3e50&color=fff&size=500&bold=true&font-size=0.4"),
        new("Joaquim Dos Santos", "https://ui-avatars.com/api/?name=Joaquim+Dos+Santos&background=e74c3c&color=fff&size=500&bold=true&font-size=0.4"),
        new("Mike Mitchell", "https://ui-avatars.com/api/?name=Mike+Mitchell&background=27ae60&color=fff&size=500&bold=true&font-size=0.4"),
        new("Makoto Shinkai", "https://ui-avatars.com/api/?name=Makoto+Shinkai&background=16a085&color=fff&size=500&bold=true&font-size=0.4"),
        new("Takashi Yamazaki", "https://ui-avatars.com/api/?name=Takashi+Yamazaki&background=b03a2e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Alessandro Carloni", "https://ui-avatars.com/api/?name=Alessandro+Carloni&background=e67e22&color=fff&size=500&bold=true&font-size=0.4"),
        new("Nguyễn Phạm Thành Đạt", "https://ui-avatars.com/api/?name=Nguyễn+Phạm+Thành+Đạt&background=2e86ab&color=fff&size=500&bold=true&font-size=0.4"),
        new("Vũ Ngọc Đãng", "https://ui-avatars.com/api/?name=Vũ+Ngọc+Đãng&background=7d3c98&color=fff&size=500&bold=true&font-size=0.4"),
        new("Narit Yuvaboon", "https://ui-avatars.com/api/?name=Narit+Yuvaboon&background=1e8449&color=fff&size=500&bold=true&font-size=0.4"),
        new("Kitao Sakurai", "https://ui-avatars.com/api/?name=Kitao+Sakurai&background=922b21&color=fff&size=500&bold=true&font-size=0.4"),
        new("Nguyễn Hữu Hoàng", "https://ui-avatars.com/api/?name=Nguyễn+Hữu+Hoàng&background=5d6d7e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Trần Hữu Tấn", "https://ui-avatars.com/api/?name=Trần+Hữu+Tấn&background=212f3d&color=fff&size=500&bold=true&font-size=0.4"),
        new("Francis Lawrence", "https://ui-avatars.com/api/?name=Francis+Lawrence&background=b7950b&color=fff&size=500&bold=true&font-size=0.4"),
        new("Nguyễn Nhất Trung", "https://ui-avatars.com/api/?name=Nguyễn+Nhất+Trung&background=ca6f1e&color=fff&size=500&bold=true&font-size=0.4"),
        new("John Hamburg", "https://ui-avatars.com/api/?name=John+Hamburg&background=117a65&color=fff&size=500&bold=true&font-size=0.4"),
    ];

    private static readonly PersonSeed[] Actors =
    [
        new("Cillian Murphy", "https://ui-avatars.com/api/?name=Cillian+Murphy&background=34495e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Robert Downey Jr.", "https://ui-avatars.com/api/?name=Robert+Downey+Jr&background=8b0000&color=fff&size=500&bold=true&font-size=0.4"),
        new("Timothée Chalamet", "https://ui-avatars.com/api/?name=Timothee+Chalamet&background=c19a49&color=fff&size=500&bold=true&font-size=0.4"),
        new("Zendaya", "https://ui-avatars.com/api/?name=Zendaya&background=9b59b6&color=fff&size=500&bold=true&font-size=0.4"),
        new("Trấn Thành", "https://ui-avatars.com/api/?name=Tran+Thanh&background=b83b5e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Tuấn Trần", "https://ui-avatars.com/api/?name=Tuan+Tran&background=2980b9&color=fff&size=500&bold=true&font-size=0.4"),
        new("Uyển Ân", "https://ui-avatars.com/api/?name=Uyen+An&background=e67e22&color=fff&size=500&bold=true&font-size=0.4"),
        new("Phương Anh Đào", "https://ui-avatars.com/api/?name=Phuong+Anh+Dao&background=d63384&color=fff&size=500&bold=true&font-size=0.4"),
        new("Song Kang-ho", "https://ui-avatars.com/api/?name=Song+Kang+Ho&background=2d3436&color=fff&size=500&bold=true&font-size=0.4"),
        new("Choi Woo-shik", "https://ui-avatars.com/api/?name=Choi+Woo+Shik&background=16a085&color=fff&size=500&bold=true&font-size=0.4"),
        new("Sam Worthington", "https://ui-avatars.com/api/?name=Sam+Worthington&background=0f4c75&color=fff&size=500&bold=true&font-size=0.4"),
        new("Zoe Saldana", "https://ui-avatars.com/api/?name=Zoe+Saldana&background=1abc9c&color=fff&size=500&bold=true&font-size=0.4"),
        new("Ryan Reynolds", "https://ui-avatars.com/api/?name=Ryan+Reynolds&background=8b0000&color=fff&size=500&bold=true&font-size=0.4"),
        new("Hugh Jackman", "https://ui-avatars.com/api/?name=Hugh+Jackman&background=34495e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Thái Hòa", "https://ui-avatars.com/api/?name=Thai+Hoa&background=d35400&color=fff&size=500&bold=true&font-size=0.4"),
        new("Kaity Nguyễn", "https://ui-avatars.com/api/?name=Kaity+Nguyen&background=e84393&color=fff&size=500&bold=true&font-size=0.4"),
        new("Ninh Dương Lan Ngọc", "https://ui-avatars.com/api/?name=Ninh+Duong+Lan+Ngoc&background=6c5b7b&color=fff&size=500&bold=true&font-size=0.4"),
        new("Quách Ngọc Ngoan", "https://ui-avatars.com/api/?name=Quach+Ngoc+Ngoan&background=1b1b2f&color=fff&size=500&bold=true&font-size=0.4"),
        new("Avin Lu", "https://ui-avatars.com/api/?name=Avin+Lu&background=3498db&color=fff&size=500&bold=true&font-size=0.4"),
        new("Hoàng Hà", "https://ui-avatars.com/api/?name=Hoang+Ha&background=e67e22&color=fff&size=500&bold=true&font-size=0.4"),
        new("Amy Poehler", "https://ui-avatars.com/api/?name=Amy+Poehler&background=f39c12&color=fff&size=500&bold=true&font-size=0.4"),
        new("Maya Hawke", "https://ui-avatars.com/api/?name=Maya+Hawke&background=9b59b6&color=fff&size=500&bold=true&font-size=0.4"),
        new("Matthew McConaughey", "https://ui-avatars.com/api/?name=Matthew+McConaughey&background=2c3e50&color=fff&size=500&bold=true&font-size=0.4"),
        new("Anne Hathaway", "https://ui-avatars.com/api/?name=Anne+Hathaway&background=8e44ad&color=fff&size=500&bold=true&font-size=0.4"),
        new("Trần Nghĩa", "https://ui-avatars.com/api/?name=Tran+Nghia&background=16a085&color=fff&size=500&bold=true&font-size=0.4"),
        new("Trúc Anh", "https://ui-avatars.com/api/?name=Truc+Anh&background=e91e63&color=fff&size=500&bold=true&font-size=0.4"),
        new("Choi Min-sik", "https://ui-avatars.com/api/?name=Choi+Min+Sik&background=34495e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Kim Go-eun", "https://ui-avatars.com/api/?name=Kim+Go+Eun&background=9c27b0&color=fff&size=500&bold=true&font-size=0.4"),
        new("Lee Do-hyun", "https://ui-avatars.com/api/?name=Lee+Do+Hyun&background=3f51b5&color=fff&size=500&bold=true&font-size=0.4"),
        new("Shameik Moore", "https://ui-avatars.com/api/?name=Shameik+Moore&background=d32f2f&color=fff&size=500&bold=true&font-size=0.4"),
        new("Hailee Steinfeld", "https://ui-avatars.com/api/?name=Hailee+Steinfeld&background=0288d1&color=fff&size=500&bold=true&font-size=0.4"),
        new("Chris Evans", "https://ui-avatars.com/api/?name=Chris+Evans&background=1976d2&color=fff&size=500&bold=true&font-size=0.4"),
        new("Scarlett Johansson", "https://ui-avatars.com/api/?name=Scarlett+Johansson&background=c2185b&color=fff&size=500&bold=true&font-size=0.4"),
        new("Jack Black", "https://ui-avatars.com/api/?name=Jack+Black&background=f57c00&color=fff&size=500&bold=true&font-size=0.4"),
        new("Song Luân", "https://ui-avatars.com/api/?name=Song+Luan&background=00796b&color=fff&size=500&bold=true&font-size=0.4"),
        new("Lê Giang", "https://ui-avatars.com/api/?name=Le+Giang&background=7b1fa2&color=fff&size=500&bold=true&font-size=0.4"),
        new("Nanoka Hara", "https://ui-avatars.com/api/?name=Nanoka+Hara&background=0097a7&color=fff&size=500&bold=true&font-size=0.4"),
        new("Hokuto Matsumura", "https://ui-avatars.com/api/?name=Hokuto+Matsumura&background=455a64&color=fff&size=500&bold=true&font-size=0.4"),
        new("Ryunosuke Kamiki", "https://ui-avatars.com/api/?name=Ryunosuke+Kamiki&background=34495e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Minami Hamabe", "https://ui-avatars.com/api/?name=Minami+Hamabe&background=c0392b&color=fff&size=500&bold=true&font-size=0.4"),
        new("Bill Hader", "https://ui-avatars.com/api/?name=Bill+Hader&background=2874a6&color=fff&size=500&bold=true&font-size=0.4"),
        new("Xochitl Gomez", "https://ui-avatars.com/api/?name=Xochitl+Gomez&background=af601a&color=fff&size=500&bold=true&font-size=0.4"),
        new("Nguyễn Hùng", "https://ui-avatars.com/api/?name=Nguyễn+Hùng&background=148f77&color=fff&size=500&bold=true&font-size=0.4"),
        new("Trần Tiểu Vy", "https://ui-avatars.com/api/?name=Trần+Tiểu+Vy&background=a93226&color=fff&size=500&bold=true&font-size=0.4"),
        new("Ngọc Xuân", "https://ui-avatars.com/api/?name=Ngọc+Xuân&background=6c3483&color=fff&size=500&bold=true&font-size=0.4"),
        new("Võ Điền Gia Huy", "https://ui-avatars.com/api/?name=Võ+Điền+Gia+Huy&background=1f618d&color=fff&size=500&bold=true&font-size=0.4"),
        new("Lãnh Thanh", "https://ui-avatars.com/api/?name=Lãnh+Thanh&background=7e5109&color=fff&size=500&bold=true&font-size=0.4"),
        new("Hồng Đào", "https://ui-avatars.com/api/?name=Hồng+Đào&background=b03a2e&color=fff&size=500&bold=true&font-size=0.4"),
        new("Lê Thiện", "https://ui-avatars.com/api/?name=Lê+Thiện&background=2c3e50&color=fff&size=500&bold=true&font-size=0.4"),
        new("Ma Ran Đô", "https://ui-avatars.com/api/?name=Ma+Ran+Đô&background=7b241c&color=fff&size=500&bold=true&font-size=0.4"),
        new("Quỳnh Lương", "https://ui-avatars.com/api/?name=Quỳnh+Lương&background=6e2c00&color=fff&size=500&bold=true&font-size=0.4"),
        new("Nadech Kugimiya", "https://ui-avatars.com/api/?name=Nadech+Kugimiya&background=117864&color=fff&size=500&bold=true&font-size=0.4"),
        new("Ong-art Cheamcharoenpornkul", "https://ui-avatars.com/api/?name=Ong-art+Cheamcharoenpornkul&background=273746&color=fff&size=500&bold=true&font-size=0.4"),
        new("Andrew Koji", "https://ui-avatars.com/api/?name=Andrew+Koji&background=1a5276&color=fff&size=500&bold=true&font-size=0.4"),
        new("Noah Centineo", "https://ui-avatars.com/api/?name=Noah+Centineo&background=af7ac5&color=fff&size=500&bold=true&font-size=0.4"),
        new("Jason Momoa", "https://ui-avatars.com/api/?name=Jason+Momoa&background=7b7d7d&color=fff&size=500&bold=true&font-size=0.4"),
        new("Nguyễn Cao Kỳ Duyên", "https://ui-avatars.com/api/?name=Nguyễn+Cao+Kỳ+Duyên&background=cb4335&color=fff&size=500&bold=true&font-size=0.4"),
        new("Steven Nguyễn", "https://ui-avatars.com/api/?name=Steven+Nguyễn&background=2471a3&color=fff&size=500&bold=true&font-size=0.4"),
        new("Joseph Zada", "https://ui-avatars.com/api/?name=Joseph+Zada&background=7d6608&color=fff&size=500&bold=true&font-size=0.4"),
        new("Whitney Peak", "https://ui-avatars.com/api/?name=Whitney+Peak&background=884ea0&color=fff&size=500&bold=true&font-size=0.4"),
        new("Mckenna Grace", "https://ui-avatars.com/api/?name=Mckenna+Grace&background=148f77&color=fff&size=500&bold=true&font-size=0.4"),
        new("Jesse Plemons", "https://ui-avatars.com/api/?name=Jesse+Plemons&background=5f6a6a&color=fff&size=500&bold=true&font-size=0.4"),
        new("Lương Thế Thành", "https://ui-avatars.com/api/?name=Lương+Thế+Thành&background=a04000&color=fff&size=500&bold=true&font-size=0.4"),
        new("Sally Hồng Anh", "https://ui-avatars.com/api/?name=Sally+Hồng+Anh&background=c2185b&color=fff&size=500&bold=true&font-size=0.4"),
        new("Robert De Niro", "https://ui-avatars.com/api/?name=Robert+De+Niro&background=424949&color=fff&size=500&bold=true&font-size=0.4"),
        new("Ben Stiller", "https://ui-avatars.com/api/?name=Ben+Stiller&background=2e4053&color=fff&size=500&bold=true&font-size=0.4"),
        new("Ariana Grande", "https://ui-avatars.com/api/?name=Ariana+Grande&background=d98880&color=fff&size=500&bold=true&font-size=0.4"),
        new("Owen Wilson", "https://ui-avatars.com/api/?name=Owen+Wilson&background=b9770e&color=fff&size=500&bold=true&font-size=0.4"),
    ];

    public string Name => "people";

    public async Task SeedAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existingDirectors = (
            await db.Directors.Select(x => x.FullName).ToListAsync(cancellationToken)
        ).ToHashSet();
        var newDirectors = Directors
            .Where(seed => !existingDirectors.Contains(seed.FullName))
            .Select(seed => new Director { FullName = seed.FullName, AvatarUrl = seed.AvatarUrl })
            .ToList();

        var existingActors = (
            await db.Actors.Select(x => x.FullName).ToListAsync(cancellationToken)
        ).ToHashSet();
        var newActors = Actors
            .Where(seed => !existingActors.Contains(seed.FullName))
            .Select(seed => new Actor { FullName = seed.FullName, AvatarUrl = seed.AvatarUrl })
            .ToList();

        if (newDirectors.Count == 0 && newActors.Count == 0)
            return;

        db.Directors.AddRange(newDirectors);
        db.Actors.AddRange(newActors);
        await db.SaveChangesAsync(cancellationToken);

        context.Logger.LogInformation(
            "Seeded {Directors} directors, {Actors} actors.",
            newDirectors.Count,
            newActors.Count
        );
    }
}
