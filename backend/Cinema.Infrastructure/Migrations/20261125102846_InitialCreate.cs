using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cinema.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actors",
                columns: table => new
                {
                    ActorId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actors", x => x.ActorId);
                });

            migrationBuilder.CreateTable(
                name: "AgeRatings",
                columns: table => new
                {
                    AgeRatingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgeRatings", x => x.AgeRatingId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "CinemaSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OpeningTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ClosingTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DefaultEmployeePassword = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Employee@123"),
                    PointsPerAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 10000m),
                    CancelRefundPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 100m),
                    CancelDeadlineHours = table.Column<int>(type: "integer", nullable: false),
                    StopSellingMinutesBefore = table.Column<int>(type: "integer", nullable: false),
                    HoldDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    CheckInOpenMinutes = table.Column<int>(type: "integer", nullable: false),
                    SeatGapRuleMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Strict"),
                    RoomBufferMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    MaxSeatsPerBooking = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    MaxComboQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    LoyaltyWindowMonths = table.Column<int>(type: "integer", nullable: false, defaultValue: 12),
                    PointsExpiryMonths = table.Column<int>(type: "integer", nullable: false, defaultValue: 12),
                    MovieEndGraceDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ShowtimeLookaheadDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 7),
                    SchedulerStepMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    SchedulerMinSameMovieGapMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    SchedulerHeatLookbackDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 7),
                    SchedulerHeatSalesWeight = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.69999999999999996),
                    SchedulerHeatRatingWeight = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.29999999999999999),
                    SchedulerMaxRating = table.Column<double>(type: "double precision", nullable: false, defaultValue: 5.0),
                    TicketPriceRoundingStep = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 1000m),
                    DynamicPricingMaxIncreasePercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 30m),
                    DynamicPricingMaxDecreasePercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 30m),
                    MinTicketPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 45000m),
                    SalesPaceCustomMarkMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    SuggestionHighOccupancyPercent = table.Column<int>(type: "integer", nullable: false, defaultValue: 80),
                    SuggestionLowOccupancyPercent = table.Column<int>(type: "integer", nullable: false, defaultValue: 35),
                    SuggestionMinShowtimes = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    SuggestionLookbackDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 14),
                    DailyClosingAllowOverride = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TicketVatPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 10m),
                    ComboVatPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 10m),
                    GiftCardVatPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 10m),
                    MembershipVatPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 10m),
                    InvoiceRequestWindowDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    GiftCardDenominations = table.Column<decimal[]>(type: "numeric(18,2)[]", nullable: false, defaultValueSql: "ARRAY[100000,200000,500000,1000000]::numeric(18,2)[]"),
                    GiftCardMinAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 50000m),
                    GiftCardMaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 5000000m),
                    GiftCardValidityMonths = table.Column<int>(type: "integer", nullable: false, defaultValue: 12),
                    GiftCardRefundExtensionDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    GiftCardMaxFailedAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    GiftCardLockoutMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    ExchangeDeadlineHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    MaxExchangesPerOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ExchangeCreditValidityDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 90),
                    PreorderComboDiscountPercent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ReminderHoursBefore = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    PointsExpiryReminderDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 14),
                    ReferralReferrerPoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    ReferralReferredPoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    ReferralMonthlyRewardLimit = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    SupportRequestDueHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 48),
                    ExceptionalRefundApprovalThreshold = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 500000m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CinemaSettings", x => x.Id);
                    table.CheckConstraint("CK_CinemaSettings_Id", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "Combos",
                columns: table => new
                {
                    ComboId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SuggestedTicketCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combos", x => x.ComboId);
                    table.CheckConstraint("CK_Combos_Price", "\"Price\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Directors",
                columns: table => new
                {
                    DirectorId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Directors", x => x.DirectorId);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    GenreId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.GenreId);
                });

            migrationBuilder.CreateTable(
                name: "GiftCardAttemptLimit",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    WindowStartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCardAttemptLimit", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    InventoryItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,3)", nullable: false, defaultValue: 0m),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false, defaultValue: 0m),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    LowStockThreshold = table.Column<decimal>(type: "numeric(18,3)", nullable: false, defaultValue: 0m),
                    LowStockAlertedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.InventoryItemId);
                    table.CheckConstraint("CK_InventoryItems_LowStockThreshold", "\"LowStockThreshold\" >= 0");
                    table.CheckConstraint("CK_InventoryItems_QuantityOnHand", "\"QuantityOnHand\" >= 0");
                    table.CheckConstraint("CK_InventoryItems_ReservedQuantity", "\"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"QuantityOnHand\"");
                    table.CheckConstraint("CK_InventoryItems_UnitCost", "\"UnitCost\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    LanguageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.LanguageId);
                });

            migrationBuilder.CreateTable(
                name: "MembershipTiers",
                columns: table => new
                {
                    TierId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TierName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MinPoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipTiers", x => x.TierId);
                    table.CheckConstraint("CK_MembershipTiers_Discount", "\"DiscountPercent\" BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    OutboxMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.OutboxMessageId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentDailyStats",
                columns: table => new
                {
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    CollectedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GatewayFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDailyStats", x => new { x.BusinessDate, x.Channel, x.Method });
                    table.CheckConstraint("CK_PaymentDailyStats_Amounts", "\"TransactionCount\" >= 0 AND \"CollectedAmount\" >= 0 AND \"RefundedAmount\" >= 0 AND \"GatewayFee\" >= 0");
                    table.CheckConstraint("CK_PaymentDailyStats_Channel", "\"Channel\" IN ('Online','Counter')");
                    table.CheckConstraint("CK_PaymentDailyStats_Method", "\"Method\" IN ('VNPay','Momo','Cash','Stripe','GiftCard','BankTransfer','Card')");
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    PositionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.PositionId);
                });

            migrationBuilder.CreateTable(
                name: "PricingRules",
                columns: table => new
                {
                    PricingRuleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RuleType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConditionJson = table.Column<string>(type: "jsonb", nullable: false),
                    AdjustmentMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdjustmentValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RoomIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    ScreenTypeIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    MovieIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRules", x => x.PricingRuleId);
                    table.CheckConstraint("CK_PricingRules_AdjustmentValue", "(\"RuleType\" = 'Occupancy' AND \"AdjustmentValue\" IS NULL) OR (\"RuleType\" <> 'Occupancy' AND \"AdjustmentValue\" IS NOT NULL)");
                    table.CheckConstraint("CK_PricingRules_ValidRange", "\"ValidFrom\" IS NULL OR \"ValidTo\" IS NULL OR \"ValidTo\" >= \"ValidFrom\"");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "ScreenTypes",
                columns: table => new
                {
                    ScreenTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreenTypes", x => x.ScreenTypeId);
                });

            migrationBuilder.CreateTable(
                name: "SeatHoldEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    SeatId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    MovieId = table.Column<int>(type: "integer", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatHoldEvents", x => x.Id);
                    table.CheckConstraint("CK_SeatHoldEvents_Type", "\"Type\" IN ('Held','Released','Expired')");
                });

            migrationBuilder.CreateTable(
                name: "SeatTypes",
                columns: table => new
                {
                    SeatTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriceMultiplier = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 1.0m),
                    IsCouple = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatTypes", x => x.SeatTypeId);
                    table.CheckConstraint("CK_SeatTypes_PriceMultiplier", "\"PriceMultiplier\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "SpecialDays",
                columns: table => new
                {
                    SpecialDayId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialDays", x => x.SpecialDayId);
                    table.CheckConstraint("CK_SpecialDays_Range", "\"EndDate\" >= \"StartDate\"");
                });

            migrationBuilder.CreateTable(
                name: "StockReceipts",
                columns: table => new
                {
                    StockReceiptId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ReceivedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReceipts", x => x.StockReceiptId);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeSheets",
                columns: table => new
                {
                    StocktakeSheetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StocktakeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeSheets", x => x.StocktakeSheetId);
                });

            migrationBuilder.CreateTable(
                name: "TicketTypes",
                columns: table => new
                {
                    TicketTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriceMultiplier = table.Column<decimal>(type: "numeric(6,4)", nullable: false, defaultValue: 1m),
                    RequiresCardVerification = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowedAgeRatingIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    AllowedFromTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    AllowedToTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    AllowedDaysOfWeek = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTypes", x => x.TicketTypeId);
                    table.CheckConstraint("CK_TicketTypes_PriceMultiplier", "\"PriceMultiplier\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "WasteSheets",
                columns: table => new
                {
                    WasteSheetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WasteDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WasteSheets", x => x.WasteSheetId);
                    table.CheckConstraint("CK_WasteSheets_OtherNote", "\"Reason\" <> 'Other' OR (\"Note\" IS NOT NULL AND btrim(\"Note\") <> '')");
                    table.CheckConstraint("CK_WasteSheets_Reason", "\"Reason\" IN ('Expired','Damaged','Spilled','Other')");
                });

            migrationBuilder.CreateTable(
                name: "ComboRecipes",
                columns: table => new
                {
                    ComboRecipeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComboId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboRecipes", x => x.ComboRecipeId);
                    table.ForeignKey(
                        name: "FK_ComboRecipes_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "ComboId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyRewards",
                columns: table => new
                {
                    RewardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PointsCost = table.Column<int>(type: "integer", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CodeValidityDays = table.Column<int>(type: "integer", nullable: false),
                    DiscountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaxDiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ComboId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRewards", x => x.RewardId);
                    table.CheckConstraint("CK_LoyaltyRewards_CodeValidityDays", "\"CodeValidityDays\" > 0");
                    table.CheckConstraint("CK_LoyaltyRewards_Combo", "(\"Type\" = 'FreeCombo') = (\"ComboId\" IS NOT NULL)");
                    table.CheckConstraint("CK_LoyaltyRewards_Discount", "(\"Type\" = 'Discount' AND \"DiscountType\" IS NOT NULL AND \"DiscountValue\" > 0) OR (\"Type\" <> 'Discount' AND \"DiscountType\" IS NULL AND \"DiscountValue\" IS NULL AND \"MaxDiscountAmount\" IS NULL)");
                    table.CheckConstraint("CK_LoyaltyRewards_MaxDiscount", "(\"Type\" = 'FreeTicket' AND \"MaxDiscountAmount\" > 0) OR (\"Type\" = 'FreeCombo' AND \"MaxDiscountAmount\" IS NULL) OR \"Type\" = 'Discount'");
                    table.CheckConstraint("CK_LoyaltyRewards_PointsCost", "\"PointsCost\" > 0");
                    table.CheckConstraint("CK_LoyaltyRewards_RemainingQuantity", "\"RemainingQuantity\" >= 0");
                    table.CheckConstraint("CK_LoyaltyRewards_Type", "\"Type\" IN ('FreeTicket','FreeCombo','Discount')");
                    table.CheckConstraint("CK_LoyaltyRewards_ValidRange", "\"ValidFrom\" IS NULL OR \"ValidTo\" IS NULL OR \"ValidTo\" > \"ValidFrom\"");
                    table.ForeignKey(
                        name: "FK_LoyaltyRewards_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "ComboId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockLedger",
                columns: table => new
                {
                    StockLedgerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryItemId = table.Column<int>(type: "integer", nullable: false),
                    MovementType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QuantityChange = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLedger", x => x.StockLedgerId);
                    table.CheckConstraint("CK_StockLedger_BalanceAfter", "\"BalanceAfter\" >= 0");
                    table.CheckConstraint("CK_StockLedger_MovementType", "\"MovementType\" IN ('Receipt','Sale','Return','StocktakeAdjustment','Waste')");
                    table.CheckConstraint("CK_StockLedger_QuantityChange", "\"QuantityChange\" <> 0");
                    table.ForeignKey(
                        name: "FK_StockLedger_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "InventoryItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    MovieId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginalTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DirectorId = table.Column<int>(type: "integer", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LanguageId = table.Column<int>(type: "integer", nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AgeRatingId = table.Column<int>(type: "integer", nullable: true),
                    PosterUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TrailerUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ComingSoon"),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.MovieId);
                    table.CheckConstraint("CK_Movies_AverageRating", "\"AverageRating\" BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_Movies_Duration", "\"DurationMinutes\" > 0");
                    table.CheckConstraint("CK_Movies_Status", "\"Status\" IN ('ComingSoon','NowShowing','Ended')");
                    table.ForeignKey(
                        name: "FK_Movies_AgeRatings_AgeRatingId",
                        column: x => x.AgeRatingId,
                        principalTable: "AgeRatings",
                        principalColumn: "AgeRatingId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movies_Directors_DirectorId",
                        column: x => x.DirectorId,
                        principalTable: "Directors",
                        principalColumn: "DirectorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movies_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "LanguageId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxDeliveries",
                columns: table => new
                {
                    OutboxDeliveryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OutboxMessageId = table.Column<long>(type: "bigint", nullable: false),
                    HandlerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxDeliveries", x => x.OutboxDeliveryId);
                    table.CheckConstraint("CK_OutboxDeliveries_Attempts", "\"Attempts\" >= 0");
                    table.CheckConstraint("CK_OutboxDeliveries_Status", "\"Status\" IN ('Pending','Processing','Processed','Failed')");
                    table.ForeignKey(
                        name: "FK_OutboxDeliveries_OutboxMessages_OutboxMessageId",
                        column: x => x.OutboxMessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "OutboxMessageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Avatar = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    GoogleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Gender", "\"Gender\" IN ('Male','Female','Other')");
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CinemaRooms",
                columns: table => new
                {
                    RoomId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScreenTypeId = table.Column<int>(type: "integer", nullable: true),
                    TotalSeats = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Rows = table.Column<int>(type: "integer", nullable: false),
                    Cols = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CinemaRooms", x => x.RoomId);
                    table.CheckConstraint("CK_CinemaRooms_TotalSeats", "\"TotalSeats\" >= 0");
                    table.ForeignKey(
                        name: "FK_CinemaRooms_ScreenTypes_ScreenTypeId",
                        column: x => x.ScreenTypeId,
                        principalTable: "ScreenTypes",
                        principalColumn: "ScreenTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockReceiptLines",
                columns: table => new
                {
                    StockReceiptLineId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockReceiptId = table.Column<int>(type: "integer", nullable: false),
                    InventoryItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReceiptLines", x => x.StockReceiptLineId);
                    table.CheckConstraint("CK_StockReceiptLines_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_StockReceiptLines_UnitCost", "\"UnitCost\" >= 0");
                    table.ForeignKey(
                        name: "FK_StockReceiptLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "InventoryItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReceiptLines_StockReceipts_StockReceiptId",
                        column: x => x.StockReceiptId,
                        principalTable: "StockReceipts",
                        principalColumn: "StockReceiptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StocktakeSheetLines",
                columns: table => new
                {
                    StocktakeSheetLineId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StocktakeSheetId = table.Column<int>(type: "integer", nullable: false),
                    InventoryItemId = table.Column<int>(type: "integer", nullable: false),
                    BookQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Difference = table.Column<decimal>(type: "numeric(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StocktakeSheetLines", x => x.StocktakeSheetLineId);
                    table.CheckConstraint("CK_StocktakeSheetLines_BookQuantity", "\"BookQuantity\" >= 0");
                    table.CheckConstraint("CK_StocktakeSheetLines_CountedQuantity", "\"CountedQuantity\" >= 0");
                    table.CheckConstraint("CK_StocktakeSheetLines_Difference", "\"Difference\" = \"CountedQuantity\" - \"BookQuantity\"");
                    table.ForeignKey(
                        name: "FK_StocktakeSheetLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "InventoryItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StocktakeSheetLines_StocktakeSheets_StocktakeSheetId",
                        column: x => x.StocktakeSheetId,
                        principalTable: "StocktakeSheets",
                        principalColumn: "StocktakeSheetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WasteSheetLines",
                columns: table => new
                {
                    WasteSheetLineId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WasteSheetId = table.Column<int>(type: "integer", nullable: false),
                    InventoryItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WasteSheetLines", x => x.WasteSheetLineId);
                    table.CheckConstraint("CK_WasteSheetLines_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_WasteSheetLines_UnitCost", "\"UnitCost\" >= 0");
                    table.ForeignKey(
                        name: "FK_WasteSheetLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "InventoryItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WasteSheetLines_WasteSheets_WasteSheetId",
                        column: x => x.WasteSheetId,
                        principalTable: "WasteSheets",
                        principalColumn: "WasteSheetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComboRecipeLines",
                columns: table => new
                {
                    ComboRecipeLineId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComboRecipeId = table.Column<int>(type: "integer", nullable: false),
                    InventoryItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboRecipeLines", x => x.ComboRecipeLineId);
                    table.CheckConstraint("CK_ComboRecipeLines_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_ComboRecipeLines_ComboRecipes_ComboRecipeId",
                        column: x => x.ComboRecipeId,
                        principalTable: "ComboRecipes",
                        principalColumn: "ComboRecipeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComboRecipeLines_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "InventoryItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyChallenges",
                columns: table => new
                {
                    ChallengeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ConditionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetCount = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RewardPoints = table.Column<int>(type: "integer", nullable: true),
                    RewardGiftId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyChallenges", x => x.ChallengeId);
                    table.CheckConstraint("CK_LoyaltyChallenges_ConditionType", "\"ConditionType\" IN ('DistinctMovies','Screenings')");
                    table.CheckConstraint("CK_LoyaltyChallenges_Range", "\"EndsOn\" >= \"StartsOn\"");
                    table.CheckConstraint("CK_LoyaltyChallenges_Reward", "(\"RewardType\" = 'Points' AND \"RewardPoints\" > 0 AND \"RewardGiftId\" IS NULL) OR (\"RewardType\" = 'Gift' AND \"RewardGiftId\" IS NOT NULL AND \"RewardPoints\" IS NULL)");
                    table.CheckConstraint("CK_LoyaltyChallenges_RewardType", "\"RewardType\" IN ('Points','Gift')");
                    table.CheckConstraint("CK_LoyaltyChallenges_Target", "\"TargetCount\" > 0");
                    table.ForeignKey(
                        name: "FK_LoyaltyChallenges_LoyaltyRewards_RewardGiftId",
                        column: x => x.RewardGiftId,
                        principalTable: "LoyaltyRewards",
                        principalColumn: "RewardId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovieCast",
                columns: table => new
                {
                    MovieId = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<int>(type: "integer", nullable: false),
                    CharacterName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieCast", x => new { x.MovieId, x.ActorId });
                    table.ForeignKey(
                        name: "FK_MovieCast_Actors_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Actors",
                        principalColumn: "ActorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovieCast_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "MovieId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovieGenres",
                columns: table => new
                {
                    MovieId = table.Column<int>(type: "integer", nullable: false),
                    GenreId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieGenres", x => new { x.MovieId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_MovieGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "GenreId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovieGenres_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "MovieId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PointDebt = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TierId = table.Column<int>(type: "integer", nullable: true),
                    TotalSpent = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    MarketingOptIn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PersonalizedRecommendations = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ReminderAndReviewInvites = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ReferralCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_CustomerProfiles_MembershipTiers_TierId",
                        column: x => x.TierId,
                        principalTable: "MembershipTiers",
                        principalColumn: "TierId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyClosings",
                columns: table => new
                {
                    DailyClosingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CountedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Variance = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ClosedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReportEmailedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyClosings", x => x.DailyClosingId);
                    table.CheckConstraint("CK_DailyClosings_Closure", "(\"Status\" = 'Draft' AND \"ClosedAt\" IS NULL AND \"ClosedByUserId\" IS NULL AND \"CountedCash\" IS NULL AND \"Variance\" IS NULL) OR (\"Status\" = 'Closed' AND \"SnapshotJson\" IS NOT NULL AND \"ClosedAt\" IS NOT NULL AND \"ClosedByUserId\" IS NOT NULL AND \"ExpectedCash\" IS NOT NULL AND \"CountedCash\" IS NOT NULL AND \"CountedCash\" >= 0 AND \"Variance\" IS NOT NULL AND \"Variance\" = \"CountedCash\" - \"ExpectedCash\")");
                    table.CheckConstraint("CK_DailyClosings_Status", "\"Status\" IN ('Draft','Closed')");
                    table.ForeignKey(
                        name: "FK_DailyClosings_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PositionId = table.Column<int>(type: "integer", nullable: true),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_Employees_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "PositionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiftCards",
                columns: table => new
                {
                    GiftCardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Gift"),
                    FaceValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReservedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    PurchaserUserId = table.Column<int>(type: "integer", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RecipientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SendAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PendingSend"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCards", x => x.GiftCardId);
                    table.CheckConstraint("CK_GiftCards_Balance", "\"Balance\" >= 0 AND \"Balance\" <= \"FaceValue\"");
                    table.CheckConstraint("CK_GiftCards_FaceValue", "\"FaceValue\" > 0");
                    table.CheckConstraint("CK_GiftCards_ReservedAmount", "\"ReservedAmount\" >= 0 AND \"ReservedAmount\" <= \"Balance\"");
                    table.CheckConstraint("CK_GiftCards_Status", "\"Status\" IN ('PendingSend','Sent','InUse','Depleted','Expired','Cancelled')");
                    table.CheckConstraint("CK_GiftCards_Type", "\"Type\" IN ('Gift','ExchangeCredit')");
                    table.ForeignKey(
                        name: "FK_GiftCards_Users_PurchaserUserId",
                        column: x => x.PurchaserUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportExportJobs",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Queued"),
                    Progress = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalRows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SuccessRows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailedRows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Parameters = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsPiiMasked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportExportJobs", x => x.JobId);
                    table.CheckConstraint("CK_ImportExportJobs_Progress", "\"Progress\" >= 0 AND \"Progress\" <= 100");
                    table.CheckConstraint("CK_ImportExportJobs_Rows", "\"TotalRows\" >= 0 AND \"SuccessRows\" >= 0 AND \"FailedRows\" >= 0");
                    table.CheckConstraint("CK_ImportExportJobs_Status", "\"Status\" IN ('Queued','Running','Completed','Failed')");
                    table.CheckConstraint("CK_ImportExportJobs_Type", "\"Type\" IN ('Import','Export','Rebuild')");
                    table.ForeignKey(
                        name: "FK_ImportExportJobs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceProfiles",
                columns: table => new
                {
                    InvoiceProfileId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceProfiles", x => x.InvoiceProfileId);
                    table.ForeignKey(
                        name: "FK_InvoiceProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    PromotionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DiscountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MovieIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    ShowtimeIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    ScreenTypeIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    DaysOfWeek = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    MembershipTierIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    NewCustomersOnly = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TotalUsageLimit = table.Column<int>(type: "integer", nullable: true),
                    PerCustomerLimit = table.Column<int>(type: "integer", nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ConsumedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ConsumedBudget = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsExclusive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AssignedUserId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.PromotionId);
                    table.CheckConstraint("CK_Promotions_Assignment", "(\"Type\" = 'PersonalCode') = (\"AssignedUserId\" IS NOT NULL)");
                    table.CheckConstraint("CK_Promotions_Channel", "\"Channel\" IS NULL OR \"Channel\" IN ('Online','Counter')");
                    table.CheckConstraint("CK_Promotions_Consumed", "\"ConsumedCount\" >= 0 AND \"ConsumedBudget\" >= 0");
                    table.CheckConstraint("CK_Promotions_DiscountType", "\"DiscountType\" IN ('Percent','FixedAmount')");
                    table.CheckConstraint("CK_Promotions_DiscountValue", "(\"DiscountType\" = 'Percent' AND \"DiscountValue\" > 0 AND \"DiscountValue\" <= 100) OR (\"DiscountType\" = 'FixedAmount' AND \"DiscountValue\" > 0)");
                    table.CheckConstraint("CK_Promotions_Limits", "(\"TotalUsageLimit\" IS NULL OR \"TotalUsageLimit\" > 0) AND (\"PerCustomerLimit\" IS NULL OR \"PerCustomerLimit\" > 0) AND (\"BudgetAmount\" IS NULL OR \"BudgetAmount\" > 0)");
                    table.CheckConstraint("CK_Promotions_MaxDiscount", "\"MaxDiscountAmount\" IS NULL OR (\"DiscountType\" = 'Percent' AND \"MaxDiscountAmount\" > 0)");
                    table.CheckConstraint("CK_Promotions_MinOrder", "\"MinOrderAmount\" IS NULL OR \"MinOrderAmount\" >= 0");
                    table.CheckConstraint("CK_Promotions_Priority", "\"Priority\" >= 0");
                    table.CheckConstraint("CK_Promotions_Scope", "\"Scope\" IN ('TicketsOnly','CombosOnly','WholeOrder')");
                    table.CheckConstraint("CK_Promotions_Type", "\"Type\" IN ('ManualCode','AutoApply','PersonalCode')");
                    table.CheckConstraint("CK_Promotions_ValidRange", "\"StartsAt\" IS NULL OR \"EndsAt\" IS NULL OR \"EndsAt\" > \"StartsAt\"");
                    table.ForeignKey(
                        name: "FK_Promotions_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationSessions",
                columns: table => new
                {
                    ReconciliationSessionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Gateway = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PeriodFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Running"),
                    TotalRows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MatchedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MissingInSystemCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MissingAtGatewayCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AmountMismatchCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StatusMismatchCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PaidAtGatewayOrderUnconfirmedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    GatewayTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    SystemTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationSessions", x => x.ReconciliationSessionId);
                    table.CheckConstraint("CK_ReconciliationSessions_Amounts", "\"GatewayTotalAmount\" >= 0 AND \"SystemTotalAmount\" >= 0");
                    table.CheckConstraint("CK_ReconciliationSessions_Completed", "\"Status\" = 'Running' OR \"CompletedAt\" IS NOT NULL");
                    table.CheckConstraint("CK_ReconciliationSessions_Counts", "\"TotalRows\" >= 0 AND \"MatchedCount\" >= 0 AND \"MissingInSystemCount\" >= 0 AND \"MissingAtGatewayCount\" >= 0 AND \"AmountMismatchCount\" >= 0 AND \"StatusMismatchCount\" >= 0 AND \"PaidAtGatewayOrderUnconfirmedCount\" >= 0");
                    table.CheckConstraint("CK_ReconciliationSessions_FileHash", "\"Source\" <> 'File' OR \"FileHash\" IS NOT NULL");
                    table.CheckConstraint("CK_ReconciliationSessions_Gateway", "\"Gateway\" IN ('VNPay','Momo','Stripe')");
                    table.CheckConstraint("CK_ReconciliationSessions_Period", "\"PeriodFrom\" IS NULL OR \"PeriodTo\" IS NULL OR \"PeriodFrom\" <= \"PeriodTo\"");
                    table.CheckConstraint("CK_ReconciliationSessions_Source", "\"Source\" IN ('File','Api')");
                    table.CheckConstraint("CK_ReconciliationSessions_Status", "\"Status\" IN ('Running','Completed','Failed')");
                    table.ForeignKey(
                        name: "FK_ReconciliationSessions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReferralRelations",
                columns: table => new
                {
                    ReferralId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferrerUserId = table.Column<int>(type: "integer", nullable: false),
                    ReferredUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RewardGrantedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RewardBlockedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RewardBlockedReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralRelations", x => x.ReferralId);
                    table.CheckConstraint("CK_ReferralRelations_NotSelf", "\"ReferrerUserId\" <> \"ReferredUserId\"");
                    table.ForeignKey(
                        name: "FK_ReferralRelations_Users_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReferralRelations_Users_ReferrerUserId",
                        column: x => x.ReferrerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    RefreshTokenId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    RevokedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReplacedByTokenId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.RefreshTokenId);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "RefreshTokenId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    ReviewId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MovieId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.ReviewId);
                    table.CheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_Reviews_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "MovieId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserVerificationTokens",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVerificationTokens", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK_UserVerificationTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomClosures",
                columns: table => new
                {
                    RoomClosureId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomClosures", x => x.RoomClosureId);
                    table.CheckConstraint("CK_RoomClosures_Time", "\"EndTime\" > \"StartTime\"");
                    table.ForeignKey(
                        name: "FK_RoomClosures_CinemaRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "CinemaRooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Seats",
                columns: table => new
                {
                    SeatId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    SeatRow = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    SeatNumber = table.Column<int>(type: "integer", nullable: false),
                    SeatTypeId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsAisle = table.Column<bool>(type: "boolean", nullable: false),
                    CoupleId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.SeatId);
                    table.CheckConstraint("CK_Seats_SeatNumber", "\"SeatNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_Seats_CinemaRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "CinemaRooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Seats_SeatTypes_SeatTypeId",
                        column: x => x.SeatTypeId,
                        principalTable: "SeatTypes",
                        principalColumn: "SeatTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Showtimes",
                columns: table => new
                {
                    ShowtimeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MovieId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Scheduled"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Showtimes", x => x.ShowtimeId);
                    table.CheckConstraint("CK_Showtime_Time", "\"EndTime\" > \"StartTime\"");
                    table.CheckConstraint("CK_Showtimes_BasePrice", "\"BasePrice\" >= 0");
                    table.CheckConstraint("CK_Showtimes_Status", "\"Status\" IN ('Scheduled','Cancelled','Completed')");
                    table.ForeignKey(
                        name: "FK_Showtimes_CinemaRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "CinemaRooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Showtimes_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "MovieId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyChallengeProgresses",
                columns: table => new
                {
                    ProgressId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CurrentCount = table.Column<int>(type: "integer", nullable: false),
                    CountedMovieIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    CountedShowtimeIds = table.Column<List<int>>(type: "integer[]", nullable: false, defaultValueSql: "'{}'"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RewardGrantedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyChallengeProgresses", x => x.ProgressId);
                    table.CheckConstraint("CK_LoyaltyChallengeProgresses_Count", "\"CurrentCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_LoyaltyChallengeProgresses_LoyaltyChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "LoyaltyChallenges",
                        principalColumn: "ChallengeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyChallengeProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CounterShifts",
                columns: table => new
                {
                    CounterShiftId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    OpeningCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CountedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Variance = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnclosedAlertedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounterShifts", x => x.CounterShiftId);
                    table.CheckConstraint("CK_CounterShifts_Closure", "(\"Status\" = 'Open' AND \"ClosedAt\" IS NULL AND \"CountedCash\" IS NULL AND \"ExpectedCash\" IS NULL AND \"Variance\" IS NULL) OR (\"Status\" = 'Closed' AND \"ClosedAt\" IS NOT NULL AND \"ClosedAt\" >= \"OpenedAt\" AND \"CountedCash\" IS NOT NULL AND \"CountedCash\" >= 0 AND \"ExpectedCash\" IS NOT NULL AND \"Variance\" IS NOT NULL AND \"Variance\" = \"CountedCash\" - \"ExpectedCash\")");
                    table.CheckConstraint("CK_CounterShifts_OpeningCash", "\"OpeningCash\" >= 0");
                    table.CheckConstraint("CK_CounterShifts_Status", "\"Status\" IN ('Open','Closed')");
                    table.ForeignKey(
                        name: "FK_CounterShifts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionCustomerUsages",
                columns: table => new
                {
                    PromotionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ConsumedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCustomerUsages", x => new { x.PromotionId, x.UserId });
                    table.CheckConstraint("CK_PromotionCustomerUsages_Consumed", "\"ConsumedCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_PromotionCustomerUsages_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionCustomerUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShowtimeDailyStats",
                columns: table => new
                {
                    ShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Regular"),
                    SellableSeats = table.Column<int>(type: "integer", nullable: false),
                    TicketsSold = table.Column<int>(type: "integer", nullable: false),
                    GrossTicketRevenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ComboRevenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CancelledOrders = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowtimeDailyStats", x => x.ShowtimeId);
                    table.CheckConstraint("CK_ShowtimeDailyStats_Amounts", "\"GrossTicketRevenue\" >= 0 AND \"DiscountAmount\" >= 0 AND \"RefundAmount\" >= 0");
                    table.CheckConstraint("CK_ShowtimeDailyStats_Counts", "\"SellableSeats\" >= 0 AND \"TicketsSold\" >= 0 AND \"CancelledOrders\" >= 0");
                    table.CheckConstraint("CK_ShowtimeDailyStats_Kind", "\"Kind\" IN ('Regular','Private','Event')");
                    table.ForeignKey(
                        name: "FK_ShowtimeDailyStats_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "ShowtimeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowtimeRoomChanges",
                columns: table => new
                {
                    ShowtimeRoomChangeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    FromRoomId = table.Column<int>(type: "integer", nullable: false),
                    ToRoomId = table.Column<int>(type: "integer", nullable: false),
                    SeatsMoved = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowtimeRoomChanges", x => x.ShowtimeRoomChangeId);
                    table.CheckConstraint("CK_ShowtimeRoomChanges_Rooms", "\"FromRoomId\" <> \"ToRoomId\"");
                    table.CheckConstraint("CK_ShowtimeRoomChanges_SeatsMoved", "\"SeatsMoved\" >= 0");
                    table.ForeignKey(
                        name: "FK_ShowtimeRoomChanges_CinemaRooms_FromRoomId",
                        column: x => x.FromRoomId,
                        principalTable: "CinemaRooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowtimeRoomChanges_CinemaRooms_ToRoomId",
                        column: x => x.ToRoomId,
                        principalTable: "CinemaRooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowtimeRoomChanges_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "ShowtimeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowtimeSalesPaces",
                columns: table => new
                {
                    ShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    MarkMinutesBeforeStart = table.Column<int>(type: "integer", nullable: false),
                    TicketsSold = table.Column<int>(type: "integer", nullable: false),
                    SellableSeats = table.Column<int>(type: "integer", nullable: false),
                    SoldPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowtimeSalesPaces", x => new { x.ShowtimeId, x.MarkMinutesBeforeStart });
                    table.CheckConstraint("CK_ShowtimeSalesPaces_Counts", "\"TicketsSold\" >= 0 AND \"SellableSeats\" >= 0");
                    table.CheckConstraint("CK_ShowtimeSalesPaces_Mark", "\"MarkMinutesBeforeStart\" > 0");
                    table.CheckConstraint("CK_ShowtimeSalesPaces_Percent", "\"SoldPercent\" >= 0 AND \"SoldPercent\" <= 100");
                    table.ForeignKey(
                        name: "FK_ShowtimeSalesPaces_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "ShowtimeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Ticket"),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Online"),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedByEmployeeId = table.Column<int>(type: "integer", nullable: true),
                    CounterShiftId = table.Column<int>(type: "integer", nullable: true),
                    OriginalOrderId = table.Column<int>(type: "integer", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    FinalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LoyaltyReversedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    CancelReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CancelledBy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.CheckConstraint("CK_Orders_Amounts", "\"DiscountAmount\" <= \"TotalAmount\" AND \"FinalAmount\" = \"TotalAmount\" - \"DiscountAmount\"");
                    table.CheckConstraint("CK_Orders_Cancellation", "(\"Status\" = 'Cancelled' AND \"CancelReason\" IS NOT NULL AND \"CancelledBy\" IS NOT NULL AND \"CancelledAt\" IS NOT NULL) OR (\"Status\" <> 'Cancelled' AND \"CancelReason\" IS NULL AND \"CancelledBy\" IS NULL AND \"CancelledAt\" IS NULL)");
                    table.CheckConstraint("CK_Orders_CancelledBy", "\"CancelledBy\" IS NULL OR \"CancelledBy\" IN ('Customer','Staff','Admin','System')");
                    table.CheckConstraint("CK_Orders_CancelReason", "\"CancelReason\" IS NULL OR \"CancelReason\" IN ('CustomerChangedMind','DuplicateOrder','PaymentError','ShowtimeCancelled','RoomClosed','Other')");
                    table.CheckConstraint("CK_Orders_Channel", "\"Channel\" IN ('Online','Counter')");
                    table.CheckConstraint("CK_Orders_Channel_Employee", "(\"Channel\" = 'Counter' AND \"CreatedByEmployeeId\" IS NOT NULL) OR (\"Channel\" = 'Online' AND \"CreatedByEmployeeId\" IS NULL)");
                    table.CheckConstraint("CK_Orders_Customer_Identity", "\"UserId\" IS NOT NULL OR \"CustomerName\" IS NOT NULL");
                    table.CheckConstraint("CK_Orders_DiscountAmount", "\"DiscountAmount\" >= 0");
                    table.CheckConstraint("CK_Orders_FinalAmount", "\"FinalAmount\" >= 0");
                    table.CheckConstraint("CK_Orders_Status", "\"Status\" IN ('Pending','Confirmed','Cancelled','Expired')");
                    table.CheckConstraint("CK_Orders_Supplement_Original", "(\"Type\" IN ('Supplement','Exchange')) = (\"OriginalOrderId\" IS NOT NULL)");
                    table.CheckConstraint("CK_Orders_TotalAmount", "\"TotalAmount\" >= 0");
                    table.CheckConstraint("CK_Orders_Type", "\"Type\" IN ('Ticket','Combo','GiftCard','Membership','RoomRental','Supplement','Exchange')");
                    table.ForeignKey(
                        name: "FK_Orders_CounterShifts_CounterShiftId",
                        column: x => x.CounterShiftId,
                        principalTable: "CounterShifts",
                        principalColumn: "CounterShiftId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Orders_OriginalOrderId",
                        column: x => x.OriginalOrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiftCardLedger",
                columns: table => new
                {
                    GiftCardLedgerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GiftCardId = table.Column<int>(type: "integer", nullable: false),
                    EntryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BalanceChange = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReservedChange = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReservedAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCardLedger", x => x.GiftCardLedgerId);
                    table.CheckConstraint("CK_GiftCardLedger_After", "\"BalanceAfter\" >= 0 AND \"ReservedAfter\" >= 0 AND \"ReservedAfter\" <= \"BalanceAfter\"");
                    table.CheckConstraint("CK_GiftCardLedger_EntryType", "\"EntryType\" IN ('Issue','Reserve','Release','Redeem','Refund','Expire','Cancel')");
                    table.CheckConstraint("CK_GiftCardLedger_Signs", "(\"EntryType\" = 'Issue' AND \"BalanceChange\" > 0 AND \"ReservedChange\" = 0) OR (\"EntryType\" = 'Reserve' AND \"BalanceChange\" = 0 AND \"ReservedChange\" > 0) OR (\"EntryType\" = 'Release' AND \"BalanceChange\" = 0 AND \"ReservedChange\" < 0) OR (\"EntryType\" = 'Redeem' AND \"BalanceChange\" < 0 AND \"ReservedChange\" <= 0) OR (\"EntryType\" = 'Refund' AND \"BalanceChange\" > 0 AND \"ReservedChange\" = 0) OR (\"EntryType\" IN ('Expire','Cancel') AND \"BalanceChange\" <= 0 AND \"ReservedChange\" <= 0 AND (\"BalanceChange\" < 0 OR \"ReservedChange\" < 0))");
                    table.ForeignKey(
                        name: "FK_GiftCardLedger_GiftCards_GiftCardId",
                        column: x => x.GiftCardId,
                        principalTable: "GiftCards",
                        principalColumn: "GiftCardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCardLedger_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceRequests",
                columns: table => new
                {
                    InvoiceRequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    InvoiceProfileId = table.Column<int>(type: "integer", nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RejectReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FilePublicId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    NeedsAdjustment = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AdjustmentFlaggedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AdjustmentRefundTransactionId = table.Column<int>(type: "integer", nullable: true),
                    AdjustmentRefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    AdjustmentResolvedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AdjustmentResolvedByUserId = table.Column<int>(type: "integer", nullable: true),
                    AdjustmentNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IssuedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceRequests", x => x.InvoiceRequestId);
                    table.ForeignKey(
                        name: "FK_InvoiceRequests_InvoiceProfiles_InvoiceProfileId",
                        column: x => x.InvoiceProfileId,
                        principalTable: "InvoiceProfiles",
                        principalColumn: "InvoiceProfileId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InvoiceRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceRequests_Users_IssuedByUserId",
                        column: x => x.IssuedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InvoiceRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPointEntries",
                columns: table => new
                {
                    EntryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    RevokedPoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RemainingPoints = table.Column<int>(type: "integer", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiryReminderSentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointEntries", x => x.EntryId);
                    table.CheckConstraint("CK_LoyaltyPointEntries_Points", "\"Points\" >= 0");
                    table.CheckConstraint("CK_LoyaltyPointEntries_Remaining", "\"RemainingPoints\" >= 0 AND \"RemainingPoints\" <= \"Points\"");
                    table.CheckConstraint("CK_LoyaltyPointEntries_Revoked", "\"RevokedPoints\" >= 0 AND \"RevokedPoints\" <= \"Points\"");
                    table.ForeignKey(
                        name: "FK_LoyaltyPointEntries_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderPriceAdjustments",
                columns: table => new
                {
                    AdjustmentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenceId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPriceAdjustments", x => x.AdjustmentId);
                    table.CheckConstraint("CK_OrderPriceAdjustments_Amount", "\"Amount\" >= 0");
                    table.CheckConstraint("CK_OrderPriceAdjustments_Reference", "(\"ReferenceType\" IS NULL) = (\"ReferenceId\" IS NULL)");
                    table.CheckConstraint("CK_OrderPriceAdjustments_Type", "\"Type\" IN ('TierDiscount','MembershipBenefit','Promotion','PackagePrice')");
                    table.ForeignKey(
                        name: "FK_OrderPriceAdjustments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderResourceHolds",
                columns: table => new
                {
                    ResourceHoldId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResourceId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Reserved"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderResourceHolds", x => x.ResourceHoldId);
                    table.CheckConstraint("CK_OrderResourceHolds_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_OrderResourceHolds_Resolved", "(\"Status\" = 'Reserved') = (\"ResolvedAt\" IS NULL)");
                    table.CheckConstraint("CK_OrderResourceHolds_Status", "\"Status\" IN ('Reserved','Committed','Released')");
                    table.ForeignKey(
                        name: "FK_OrderResourceHolds_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    PaymentTransactionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GatewayFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    TransactionCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    GatewayTransactionId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    GatewayResponseCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PerformedByUserId = table.Column<int>(type: "integer", nullable: true),
                    AmountReceived = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ChangeGiven = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    OriginalTransactionId = table.Column<int>(type: "integer", nullable: true),
                    CounterShiftId = table.Column<int>(type: "integer", nullable: true),
                    GiftCardId = table.Column<int>(type: "integer", nullable: true),
                    NeedsManualRefund = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.PaymentTransactionId);
                    table.CheckConstraint("CK_PaymentTransactions_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_PaymentTransactions_Cash", "(\"AmountReceived\" IS NULL AND \"ChangeGiven\" IS NULL) OR (\"AmountReceived\" IS NOT NULL AND \"ChangeGiven\" IS NOT NULL AND \"Method\" = 'Cash' AND \"Type\" = 'Charge' AND \"AmountReceived\" >= \"Amount\" AND \"ChangeGiven\" = \"AmountReceived\" - \"Amount\")");
                    table.CheckConstraint("CK_PaymentTransactions_Completed", "\"Status\" <> 'Success' OR \"CompletedAt\" IS NOT NULL");
                    table.CheckConstraint("CK_PaymentTransactions_GatewayFee", "\"GatewayFee\" >= 0");
                    table.CheckConstraint("CK_PaymentTransactions_ManualRefund", "NOT \"NeedsManualRefund\" OR \"Type\" = 'Charge'");
                    table.CheckConstraint("CK_PaymentTransactions_Method", "\"Method\" IN ('VNPay','Momo','Cash','Stripe','GiftCard','BankTransfer','Card')");
                    table.CheckConstraint("CK_PaymentTransactions_Original", "(\"Type\" = 'Refund') = (\"OriginalTransactionId\" IS NOT NULL)");
                    table.CheckConstraint("CK_PaymentTransactions_Status", "\"Status\" IN ('Pending','Success','Failed')");
                    table.CheckConstraint("CK_PaymentTransactions_Type", "\"Type\" IN ('Charge','Refund')");
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_CounterShifts_CounterShiftId",
                        column: x => x.CounterShiftId,
                        principalTable: "CounterShifts",
                        principalColumn: "CounterShiftId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_GiftCards_GiftCardId",
                        column: x => x.GiftCardId,
                        principalTable: "GiftCards",
                        principalColumn: "GiftCardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_PaymentTransactions_OriginalTransaction~",
                        column: x => x.OriginalTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "PaymentTransactionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionRedemptions",
                columns: table => new
                {
                    PromotionRedemptionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PromotionId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRedemptions", x => x.PromotionRedemptionId);
                    table.CheckConstraint("CK_PromotionRedemptions_DiscountAmount", "\"DiscountAmount\" >= 0");
                    table.CheckConstraint("CK_PromotionRedemptions_Status", "\"Status\" IN ('Reserved','Redeemed','Released')");
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShowtimeRoomChangeNotices",
                columns: table => new
                {
                    ShowtimeRoomChangeId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowtimeRoomChangeNotices", x => new { x.ShowtimeRoomChangeId, x.OrderId });
                    table.ForeignKey(
                        name: "FK_ShowtimeRoomChangeNotices_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShowtimeRoomChangeNotices_ShowtimeRoomChanges_ShowtimeRoomC~",
                        column: x => x.ShowtimeRoomChangeId,
                        principalTable: "ShowtimeRoomChanges",
                        principalColumn: "ShowtimeRoomChangeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShowtimeSeats",
                columns: table => new
                {
                    ShowtimeSeatId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    SeatId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Available"),
                    HeldByUserId = table.Column<int>(type: "integer", nullable: true),
                    HeldByOrderId = table.Column<int>(type: "integer", nullable: true),
                    HeldUntil = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowtimeSeats", x => x.ShowtimeSeatId);
                    table.CheckConstraint("CK_ShowtimeSeats_Price", "\"Price\" >= 0");
                    table.CheckConstraint("CK_ShowtimeSeats_Status", "\"Status\" IN ('Available','Held','Booked','Blocked')");
                    table.ForeignKey(
                        name: "FK_ShowtimeSeats_Orders_HeldByOrderId",
                        column: x => x.HeldByOrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShowtimeSeats_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "SeatId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowtimeSeats_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "ShowtimeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShowtimeSeats_Users_HeldByUserId",
                        column: x => x.HeldByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPointTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<int>(type: "integer", nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointTransactions", x => x.TransactionId);
                    table.CheckConstraint("CK_LoyaltyPointTransactions_Points", "\"Points\" <> 0");
                    table.ForeignKey(
                        name: "FK_LoyaltyPointTransactions_LoyaltyPointEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "LoyaltyPointEntries",
                        principalColumn: "EntryId");
                    table.ForeignKey(
                        name: "FK_LoyaltyPointTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiftCardRevenueEntries",
                columns: table => new
                {
                    GiftCardRevenueEntryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GiftCardId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    PaymentTransactionId = table.Column<int>(type: "integer", nullable: false),
                    RecognizedEntryId = table.Column<long>(type: "bigint", nullable: true),
                    EntryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCardRevenueEntries", x => x.GiftCardRevenueEntryId);
                    table.CheckConstraint("CK_GiftCardRevenueEntries_EntryType", "\"EntryType\" IN ('Recognized','Reversed')");
                    table.CheckConstraint("CK_GiftCardRevenueEntries_Signs", "(\"EntryType\" = 'Recognized' AND \"Amount\" > 0 AND \"NetAmount\" > 0 AND \"TaxAmount\" >= 0 AND \"RecognizedEntryId\" IS NULL) OR (\"EntryType\" = 'Reversed' AND \"Amount\" < 0 AND \"NetAmount\" <= 0 AND \"TaxAmount\" <= 0 AND \"RecognizedEntryId\" IS NOT NULL)");
                    table.CheckConstraint("CK_GiftCardRevenueEntries_Total", "\"Amount\" = \"NetAmount\" + \"TaxAmount\"");
                    table.ForeignKey(
                        name: "FK_GiftCardRevenueEntries_GiftCardRevenueEntries_RecognizedEnt~",
                        column: x => x.RecognizedEntryId,
                        principalTable: "GiftCardRevenueEntries",
                        principalColumn: "GiftCardRevenueEntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCardRevenueEntries_GiftCards_GiftCardId",
                        column: x => x.GiftCardId,
                        principalTable: "GiftCards",
                        principalColumn: "GiftCardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCardRevenueEntries_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCardRevenueEntries_PaymentTransactions_PaymentTransacti~",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "PaymentTransactionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LedgerAdjustments",
                columns: table => new
                {
                    LedgerAdjustmentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OriginalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TicketRevenueDelta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ComboRevenueDelta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GiftCardSoldDelta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountDelta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundAmountDelta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CancelledOrdersDelta = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    PaymentTransactionId = table.Column<int>(type: "integer", nullable: true),
                    SourceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerAdjustments", x => x.LedgerAdjustmentId);
                    table.CheckConstraint("CK_LedgerAdjustments_Dates", "\"PostingDate\" > \"OriginalDate\"");
                    table.CheckConstraint("CK_LedgerAdjustments_NotEmpty", "\"TicketRevenueDelta\" <> 0 OR \"ComboRevenueDelta\" <> 0 OR \"GiftCardSoldDelta\" <> 0 OR \"DiscountDelta\" <> 0 OR \"RefundAmountDelta\" <> 0 OR \"CancelledOrdersDelta\" <> 0");
                    table.CheckConstraint("CK_LedgerAdjustments_Type", "\"Type\" IN ('Refund','Cancellation','ShowtimeChange','Manual')");
                    table.ForeignKey(
                        name: "FK_LedgerAdjustments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerAdjustments_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "PaymentTransactionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerAdjustments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationItems",
                columns: table => new
                {
                    ReconciliationItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReconciliationSessionId = table.Column<int>(type: "integer", nullable: false),
                    Gateway = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GatewayTransactionCode = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    GatewayFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    GatewayStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GatewayTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PaymentTransactionId = table.Column<int>(type: "integer", nullable: true),
                    Result = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Resolution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AssignedToUserId = table.Column<int>(type: "integer", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationItems", x => x.ReconciliationItemId);
                    table.CheckConstraint("CK_ReconciliationItems_Amounts", "(\"Amount\" IS NULL OR \"Amount\" >= 0) AND \"GatewayFee\" >= 0");
                    table.CheckConstraint("CK_ReconciliationItems_Gateway", "\"Gateway\" IN ('VNPay','Momo','Stripe')");
                    table.CheckConstraint("CK_ReconciliationItems_PaymentTransaction", "(\"Result\" = 'MissingInSystem') = (\"PaymentTransactionId\" IS NULL)");
                    table.CheckConstraint("CK_ReconciliationItems_Resolution", "\"Resolution\" IN ('Open','Resolved','Ignored')");
                    table.CheckConstraint("CK_ReconciliationItems_Resolved", "(\"Resolution\" = 'Open' AND \"ResolvedAt\" IS NULL AND \"ResolvedByUserId\" IS NULL) OR (\"Resolution\" <> 'Open' AND \"ResolvedAt\" IS NOT NULL AND \"ResolvedByUserId\" IS NOT NULL)");
                    table.CheckConstraint("CK_ReconciliationItems_Result", "\"Result\" IN ('Matched','MissingInSystem','MissingAtGateway','AmountMismatch','StatusMismatch','PaidAtGatewayOrderUnconfirmed')");
                    table.ForeignKey(
                        name: "FK_ReconciliationItems_PaymentTransactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "PaymentTransactionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationItems_ReconciliationSessions_ReconciliationSe~",
                        column: x => x.ReconciliationSessionId,
                        principalTable: "ReconciliationSessions",
                        principalColumn: "ReconciliationSessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReconciliationItems_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationItems_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportRequests",
                columns: table => new
                {
                    SupportRequestId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Subject = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssignedToUserId = table.Column<int>(type: "integer", nullable: true),
                    ApologyPromotionId = table.Column<int>(type: "integer", nullable: true),
                    SourcePaymentTransactionId = table.Column<int>(type: "integer", nullable: true),
                    DueAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportRequests", x => x.SupportRequestId);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportRequests_PaymentTransactions_SourcePaymentTransactio~",
                        column: x => x.SourcePaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "PaymentTransactionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Promotions_ApologyPromotionId",
                        column: x => x.ApologyPromotionId,
                        principalTable: "Promotions",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                columns: table => new
                {
                    OrderLineId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    LineType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShowtimeId = table.Column<int>(type: "integer", nullable: true),
                    ShowtimeSeatId = table.Column<int>(type: "integer", nullable: true),
                    TicketTypeId = table.Column<int>(type: "integer", nullable: true),
                    RequiresCardPresentation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CardCheckStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NotRequired"),
                    CardCheckRecordedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CardCheckRecordedByUserId = table.Column<int>(type: "integer", nullable: true),
                    SupplementedLineId = table.Column<int>(type: "integer", nullable: true),
                    ExchangeFromLineId = table.Column<int>(type: "integer", nullable: true),
                    ComboId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    QuantityFulfilled = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    UnitListPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LockedUnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    RefundedQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    QrCode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsCheckedIn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GiftCardId = table.Column<int>(type: "integer", nullable: true),
                    GiftRecipientEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    GiftRecipientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    GiftMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GiftSendAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.OrderLineId);
                    table.CheckConstraint("CK_OrderLines_CardCheck", "\"CardCheckStatus\" = 'NotRequired' OR (\"LineType\" = 'Ticket' AND \"RequiresCardPresentation\")");
                    table.CheckConstraint("CK_OrderLines_CardCheckRecord", "((\"CardCheckStatus\" IN ('NotPresented','Supplemented')) = (\"CardCheckRecordedAt\" IS NOT NULL)) AND ((\"CardCheckRecordedAt\" IS NULL) = (\"CardCheckRecordedByUserId\" IS NULL))");
                    table.CheckConstraint("CK_OrderLines_CardCheckStatus", "\"CardCheckStatus\" IN ('NotRequired','Pending','NotPresented','Supplemented')");
                    table.CheckConstraint("CK_OrderLines_CardPresentation", "(NOT \"RequiresCardPresentation\") OR \"LineType\" = 'Ticket'");
                    table.CheckConstraint("CK_OrderLines_CheckIn", "(NOT \"IsCheckedIn\") OR \"LineType\" = 'Ticket'");
                    table.CheckConstraint("CK_OrderLines_ExchangeFromLine", "\"ExchangeFromLineId\" IS NULL OR \"LineType\" = 'Ticket'");
                    table.CheckConstraint("CK_OrderLines_GiftCardData", "\"LineType\" = 'GiftCard' OR (\"GiftCardId\" IS NULL AND \"GiftRecipientEmail\" IS NULL AND \"GiftRecipientName\" IS NULL AND \"GiftMessage\" IS NULL AND \"GiftSendAt\" IS NULL)");
                    table.CheckConstraint("CK_OrderLines_LineType", "\"LineType\" IN ('Ticket','Combo','GiftCard','Fee')");
                    table.CheckConstraint("CK_OrderLines_LockedUnitPrice", "\"LockedUnitPrice\" >= 0");
                    table.CheckConstraint("CK_OrderLines_PaidAmount", "\"PaidAmount\" >= 0 AND \"PaidAmount\" <= \"LockedUnitPrice\" * \"Quantity\"");
                    table.CheckConstraint("CK_OrderLines_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_OrderLines_QuantityFulfilled", "\"QuantityFulfilled\" >= 0 AND \"QuantityFulfilled\" <= \"Quantity\"");
                    table.CheckConstraint("CK_OrderLines_RefundedAmount", "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"PaidAmount\"");
                    table.CheckConstraint("CK_OrderLines_RefundedQuantity", "\"RefundedQuantity\" >= 0 AND \"RefundedQuantity\" <= \"Quantity\"");
                    table.CheckConstraint("CK_OrderLines_Shape", "(\"LineType\" = 'Ticket' AND \"ShowtimeId\" IS NOT NULL AND \"ShowtimeSeatId\" IS NOT NULL AND \"ComboId\" IS NULL AND \"Quantity\" = 1) OR (\"LineType\" = 'Combo' AND \"ComboId\" IS NOT NULL AND \"ShowtimeSeatId\" IS NULL AND \"TicketTypeId\" IS NULL) OR (\"LineType\" IN ('GiftCard','Fee') AND \"ComboId\" IS NULL AND \"ShowtimeId\" IS NULL AND \"ShowtimeSeatId\" IS NULL AND \"TicketTypeId\" IS NULL)");
                    table.CheckConstraint("CK_OrderLines_SupplementedLine", "\"SupplementedLineId\" IS NULL OR \"LineType\" = 'Fee'");
                    table.CheckConstraint("CK_OrderLines_UnitCost", "\"UnitCost\" >= 0");
                    table.CheckConstraint("CK_OrderLines_UnitListPrice", "\"UnitListPrice\" >= 0");
                    table.ForeignKey(
                        name: "FK_OrderLines_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "ComboId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_GiftCards_GiftCardId",
                        column: x => x.GiftCardId,
                        principalTable: "GiftCards",
                        principalColumn: "GiftCardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_OrderLines_ExchangeFromLineId",
                        column: x => x.ExchangeFromLineId,
                        principalTable: "OrderLines",
                        principalColumn: "OrderLineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_OrderLines_SupplementedLineId",
                        column: x => x.SupplementedLineId,
                        principalTable: "OrderLines",
                        principalColumn: "OrderLineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderLines_ShowtimeSeats_ShowtimeSeatId",
                        column: x => x.ShowtimeSeatId,
                        principalTable: "ShowtimeSeats",
                        principalColumn: "ShowtimeSeatId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "ShowtimeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "TicketTypes",
                        principalColumn: "TicketTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Users_CardCheckRecordedByUserId",
                        column: x => x.CardCheckRecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeatBlocks",
                columns: table => new
                {
                    SeatBlockId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    ShowtimeSeatId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatBlocks", x => x.SeatBlockId);
                    table.CheckConstraint("CK_SeatBlocks_Type", "\"Type\" IN ('Maintenance','Broken','RoomRental','WaitlistReserve')");
                    table.ForeignKey(
                        name: "FK_SeatBlocks_ShowtimeSeats_ShowtimeSeatId",
                        column: x => x.ShowtimeSeatId,
                        principalTable: "ShowtimeSeats",
                        principalColumn: "ShowtimeSeatId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeatBlocks_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "ShowtimeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportMessages",
                columns: table => new
                {
                    SupportMessageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupportRequestId = table.Column<int>(type: "integer", nullable: false),
                    AuthorType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AuthorUserId = table.Column<int>(type: "integer", nullable: true),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportMessages", x => x.SupportMessageId);
                    table.ForeignKey(
                        name: "FK_SupportMessages_SupportRequests_SupportRequestId",
                        column: x => x.SupportRequestId,
                        principalTable: "SupportRequests",
                        principalColumn: "SupportRequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupportMessages_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComboPickups",
                columns: table => new
                {
                    ComboPickupId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    OrderLineId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PickedUpByUserId = table.Column<int>(type: "integer", nullable: false),
                    PickedUpAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboPickups", x => x.ComboPickupId);
                    table.CheckConstraint("CK_ComboPickups_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_ComboPickups_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalTable: "OrderLines",
                        principalColumn: "OrderLineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComboPickups_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComboPickups_Users_PickedUpByUserId",
                        column: x => x.PickedUpByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExceptionalRefundProposals",
                columns: table => new
                {
                    ExceptionalRefundProposalId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    OrderLineId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequiresAdminApproval = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalThreshold = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProposedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ProposedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionalRefundProposals", x => x.ExceptionalRefundProposalId);
                    table.ForeignKey(
                        name: "FK_ExceptionalRefundProposals_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalTable: "OrderLines",
                        principalColumn: "OrderLineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionalRefundProposals_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionalRefundProposals_Users_ProposedByUserId",
                        column: x => x.ProposedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExceptionalRefundProposals_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InventoryReservations",
                columns: table => new
                {
                    InventoryReservationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    OrderLineId = table.Column<int>(type: "integer", nullable: false),
                    InventoryItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Reserved"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReservations", x => x.InventoryReservationId);
                    table.CheckConstraint("CK_InventoryReservations_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_InventoryReservations_Resolved", "(\"Status\" = 'Reserved') = (\"ResolvedAt\" IS NULL)");
                    table.CheckConstraint("CK_InventoryReservations_Status", "\"Status\" IN ('Reserved','Committed','Released')");
                    table.ForeignKey(
                        name: "FK_InventoryReservations_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "InventoryItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalTable: "OrderLines",
                        principalColumn: "OrderLineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryReservations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderExchanges",
                columns: table => new
                {
                    OrderExchangeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ExchangeNumber = table.Column<int>(type: "integer", nullable: false),
                    OldOrderLineId = table.Column<int>(type: "integer", nullable: false),
                    NewOrderLineId = table.Column<int>(type: "integer", nullable: false),
                    OldShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    NewShowtimeId = table.Column<int>(type: "integer", nullable: false),
                    PriceDifference = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExchangedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    PerformedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderExchanges", x => x.OrderExchangeId);
                    table.CheckConstraint("CK_OrderExchanges_Lines", "\"OldOrderLineId\" <> \"NewOrderLineId\"");
                    table.CheckConstraint("CK_OrderExchanges_Number", "\"ExchangeNumber\" >= 1");
                    table.CheckConstraint("CK_OrderExchanges_Showtimes", "\"OldShowtimeId\" <> \"NewShowtimeId\"");
                    table.ForeignKey(
                        name: "FK_OrderExchanges_OrderLines_NewOrderLineId",
                        column: x => x.NewOrderLineId,
                        principalTable: "OrderLines",
                        principalColumn: "OrderLineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderExchanges_OrderLines_OldOrderLineId",
                        column: x => x.OldOrderLineId,
                        principalTable: "OrderLines",
                        principalColumn: "OrderLineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderExchanges_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderExchanges_Showtimes_NewShowtimeId",
                        column: x => x.NewShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "ShowtimeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderExchanges_Showtimes_OldShowtimeId",
                        column: x => x.OldShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "ShowtimeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderExchanges_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderPriceAdjustmentAllocations",
                columns: table => new
                {
                    AllocationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdjustmentId = table.Column<int>(type: "integer", nullable: false),
                    OrderLineId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPriceAdjustmentAllocations", x => x.AllocationId);
                    table.CheckConstraint("CK_OrderPriceAdjustmentAllocations_Amount", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_OrderPriceAdjustmentAllocations_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalTable: "OrderLines",
                        principalColumn: "OrderLineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderPriceAdjustmentAllocations_OrderPriceAdjustments_Adjus~",
                        column: x => x.AdjustmentId,
                        principalTable: "OrderPriceAdjustments",
                        principalColumn: "AdjustmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomChangeRefundDues",
                columns: table => new
                {
                    RoomChangeRefundDueId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShowtimeRoomChangeId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    OrderLineId = table.Column<int>(type: "integer", nullable: false),
                    FromSeatId = table.Column<int>(type: "integer", nullable: false),
                    ToSeatId = table.Column<int>(type: "integer", nullable: false),
                    AmountDue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomChangeRefundDues", x => x.RoomChangeRefundDueId);
                    table.CheckConstraint("CK_RoomChangeRefundDues_Amount", "\"AmountDue\" > 0");
                    table.CheckConstraint("CK_RoomChangeRefundDues_Status", "\"Status\" IN ('Pending','Refunded','Voided')");
                    table.ForeignKey(
                        name: "FK_RoomChangeRefundDues_OrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalTable: "OrderLines",
                        principalColumn: "OrderLineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomChangeRefundDues_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomChangeRefundDues_Seats_FromSeatId",
                        column: x => x.FromSeatId,
                        principalTable: "Seats",
                        principalColumn: "SeatId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomChangeRefundDues_Seats_ToSeatId",
                        column: x => x.ToSeatId,
                        principalTable: "Seats",
                        principalColumn: "SeatId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomChangeRefundDues_ShowtimeRoomChanges_ShowtimeRoomChange~",
                        column: x => x.ShowtimeRoomChangeId,
                        principalTable: "ShowtimeRoomChanges",
                        principalColumn: "ShowtimeRoomChangeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Positions",
                columns: new[] { "PositionId", "Name" },
                values: new object[,]
                {
                    { 1, "Quản lý rạp" },
                    { 2, "Trưởng ca" },
                    { 3, "Nhân viên bán vé" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 1, "Customer" },
                    { 2, "Admin" },
                    { 3, "Employee" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "Avatar", "CreatedAt", "DateOfBirth", "Email", "EmailConfirmed", "FullName", "Gender", "GoogleId", "IsActive", "PasswordHash", "Phone", "RoleId", "UpdatedAt" },
                values: new object[] { 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "admin@cinema.local", true, "System Admin", null, null, true, "AQAAAAEAAYagAAAAENS3DOHbkUObJNrLWeaXshnI43hVryVZB/xVqXl+RRCO+rT1ek+EVigjK5BS5n9leQ==", null, 2, null });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "EmployeeId", "HireDate", "IsActive", "PositionId", "UserId" },
                values: new object[] { 1, new DateOnly(2026, 1, 1), true, 1, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_AgeRatings_Code",
                table: "AgeRatings",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Actor",
                table: "AuditLogs",
                columns: new[] { "ActorUserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAt",
                table: "AuditLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_CinemaRooms_RoomName",
                table: "CinemaRooms",
                column: "RoomName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CinemaRooms_ScreenTypeId",
                table: "CinemaRooms",
                column: "ScreenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboPickups_Order",
                table: "ComboPickups",
                columns: new[] { "OrderId", "PickedUpAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ComboPickups_OrderLine",
                table: "ComboPickups",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboPickups_PickedUpBy",
                table: "ComboPickups",
                column: "PickedUpByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboRecipeLines_InventoryItemId",
                table: "ComboRecipeLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "UX_ComboRecipeLines_Recipe_Item",
                table: "ComboRecipeLines",
                columns: new[] { "ComboRecipeId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ComboRecipes_ComboId",
                table: "ComboRecipes",
                column: "ComboId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CounterShifts_Employee_OpenedAt",
                table: "CounterShifts",
                columns: new[] { "EmployeeId", "OpenedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_CounterShifts_Open",
                table: "CounterShifts",
                column: "Status",
                filter: "\"Status\" = 'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_CounterShifts_OpenedAt",
                table: "CounterShifts",
                column: "OpenedAt");

            migrationBuilder.CreateIndex(
                name: "UX_CounterShifts_Employee_Open",
                table: "CounterShifts",
                column: "EmployeeId",
                unique: true,
                filter: "\"Status\" = 'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_Tier",
                table: "CustomerProfiles",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerProfiles_ReferralCode",
                table: "CustomerProfiles",
                column: "ReferralCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyClosings_ClosedByUserId",
                table: "DailyClosings",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_DailyClosings_BusinessDate",
                table: "DailyClosings",
                column: "BusinessDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Position",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionalRefundProposals_Order_Status",
                table: "ExceptionalRefundProposals",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionalRefundProposals_OrderLineId",
                table: "ExceptionalRefundProposals",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionalRefundProposals_ProposedByUserId",
                table: "ExceptionalRefundProposals",
                column: "ProposedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionalRefundProposals_ReviewedByUserId",
                table: "ExceptionalRefundProposals",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionalRefundProposals_Status_ProposedAt",
                table: "ExceptionalRefundProposals",
                columns: new[] { "Status", "ProposedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardLedger_Card",
                table: "GiftCardLedger",
                columns: new[] { "GiftCardId", "GiftCardLedgerId" });

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardLedger_Order",
                table: "GiftCardLedger",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardRevenueEntries_Card",
                table: "GiftCardRevenueEntries",
                column: "GiftCardId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardRevenueEntries_Date",
                table: "GiftCardRevenueEntries",
                columns: new[] { "BusinessDate", "EntryType" });

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardRevenueEntries_Order",
                table: "GiftCardRevenueEntries",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardRevenueEntries_Recognized",
                table: "GiftCardRevenueEntries",
                column: "RecognizedEntryId",
                filter: "\"RecognizedEntryId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_GiftCardRevenueEntries_Transaction",
                table: "GiftCardRevenueEntries",
                column: "PaymentTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_Code",
                table: "GiftCards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_Purchaser",
                table: "GiftCards",
                column: "PurchaserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_RecipientEmail",
                table: "GiftCards",
                column: "RecipientEmail");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_Status_ExpiresAt",
                table: "GiftCards",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_Status_SendAt",
                table: "GiftCards",
                columns: new[] { "Status", "SendAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportExportJobs_ExpiresAt_HasFile",
                table: "ImportExportJobs",
                column: "ExpiresAt",
                filter: "\"StoragePath\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportExportJobs_Type_Module_Status",
                table: "ImportExportJobs",
                columns: new[] { "Type", "Module", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportExportJobs_User_CreatedAt",
                table: "ImportExportJobs",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportExportJobs_User_Status",
                table: "ImportExportJobs",
                columns: new[] { "CreatedByUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_InventoryItems_Code",
                table: "InventoryItems",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_Order",
                table: "InventoryReservations",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_Reserved",
                table: "InventoryReservations",
                columns: new[] { "InventoryItemId", "Status" },
                filter: "\"Status\" = 'Reserved'");

            migrationBuilder.CreateIndex(
                name: "UX_InventoryReservations_Line_Item",
                table: "InventoryReservations",
                columns: new[] { "OrderLineId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_InvoiceProfiles_User_TaxCode",
                table: "InvoiceProfiles",
                columns: new[] { "UserId", "TaxCode" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceRequests_InvoiceProfileId",
                table: "InvoiceRequests",
                column: "InvoiceProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceRequests_IssuedByUserId",
                table: "InvoiceRequests",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceRequests_NeedsAdjustment",
                table: "InvoiceRequests",
                column: "AdjustmentFlaggedAt",
                filter: "\"NeedsAdjustment\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceRequests_RequestedByUserId",
                table: "InvoiceRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceRequests_Status_CreatedAt",
                table: "InvoiceRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_InvoiceRequests_Order_Active",
                table: "InvoiceRequests",
                column: "OrderId",
                unique: true,
                filter: "\"Status\" <> 'Rejected'");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Name",
                table: "Languages",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAdjustments_CreatedByUserId",
                table: "LedgerAdjustments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAdjustments_OrderId",
                table: "LedgerAdjustments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAdjustments_OriginalDate",
                table: "LedgerAdjustments",
                column: "OriginalDate");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAdjustments_PaymentTransactionId",
                table: "LedgerAdjustments",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAdjustments_PostingDate",
                table: "LedgerAdjustments",
                column: "PostingDate");

            migrationBuilder.CreateIndex(
                name: "UX_LedgerAdjustments_Source_OriginalDate",
                table: "LedgerAdjustments",
                columns: new[] { "SourceKey", "OriginalDate" },
                unique: true,
                filter: "\"SourceKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyChallengeProgresses_User",
                table: "LoyaltyChallengeProgresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyChallengeProgresses_Challenge_User",
                table: "LoyaltyChallengeProgresses",
                columns: new[] { "ChallengeId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyChallenges_Range",
                table: "LoyaltyChallenges",
                columns: new[] { "StartsOn", "EndsOn" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyChallenges_RewardGiftId",
                table: "LoyaltyChallenges",
                column: "RewardGiftId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointEntries_User_EarnedAt",
                table: "LoyaltyPointEntries",
                columns: new[] { "UserId", "EarnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointEntries_User_Lots",
                table: "LoyaltyPointEntries",
                columns: new[] { "UserId", "ExpiresAt" },
                filter: "\"RemainingPoints\" > 0");

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyPointEntries_Order",
                table: "LoyaltyPointEntries",
                column: "OrderId",
                unique: true,
                filter: "\"OrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointTransactions_Entry",
                table: "LoyaltyPointTransactions",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointTransactions_OrderId",
                table: "LoyaltyPointTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointTransactions_User_CreatedAt",
                table: "LoyaltyPointTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewards_Active",
                table: "LoyaltyRewards",
                columns: new[] { "IsActive", "PointsCost" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewards_ComboId",
                table: "LoyaltyRewards",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipTiers_TierName",
                table: "MembershipTiers",
                column: "TierName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovieCast_Actor",
                table: "MovieCast",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieGenres_GenreId",
                table: "MovieGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_AgeRating",
                table: "Movies",
                column: "AgeRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Director",
                table: "Movies",
                column: "DirectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Language",
                table: "Movies",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ReleaseDate",
                table: "Movies",
                column: "ReleaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Status",
                table: "Movies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderExchanges_NewShowtime",
                table: "OrderExchanges",
                column: "NewShowtimeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderExchanges_OldShowtime",
                table: "OrderExchanges",
                column: "OldShowtimeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderExchanges_Order",
                table: "OrderExchanges",
                columns: new[] { "OrderId", "ExchangeNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderExchanges_PerformedByUserId",
                table: "OrderExchanges",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_OrderExchanges_NewOrderLine",
                table: "OrderExchanges",
                column: "NewOrderLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_OrderExchanges_OldOrderLine",
                table: "OrderExchanges",
                column: "OldOrderLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_CardCheckRecordedByUserId",
                table: "OrderLines",
                column: "CardCheckRecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_Combo",
                table: "OrderLines",
                column: "ComboId",
                filter: "\"ComboId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ExchangeFromLine",
                table: "OrderLines",
                column: "ExchangeFromLineId",
                filter: "\"ExchangeFromLineId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_Order",
                table: "OrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_PendingReminder",
                table: "OrderLines",
                column: "ShowtimeId",
                filter: "\"LineType\" = 'Ticket' AND \"ReminderSentAt\" IS NULL AND \"QrCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ShowtimeSeat",
                table: "OrderLines",
                column: "ShowtimeSeatId",
                filter: "\"ShowtimeSeatId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_SupplementedLine",
                table: "OrderLines",
                column: "SupplementedLineId",
                filter: "\"SupplementedLineId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_TicketType",
                table: "OrderLines",
                column: "TicketTypeId",
                filter: "\"TicketTypeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_OrderLines_GiftCard",
                table: "OrderLines",
                column: "GiftCardId",
                unique: true,
                filter: "\"GiftCardId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_OrderLines_QrCode",
                table: "OrderLines",
                column: "QrCode",
                unique: true,
                filter: "\"QrCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPriceAdjustmentAllocations_Line",
                table: "OrderPriceAdjustmentAllocations",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "UQ_OrderPriceAdjustmentAllocations_Adjustment_Line",
                table: "OrderPriceAdjustmentAllocations",
                columns: new[] { "AdjustmentId", "OrderLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPriceAdjustments_Order",
                table: "OrderPriceAdjustments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPriceAdjustments_Reference",
                table: "OrderPriceAdjustments",
                columns: new[] { "Type", "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderResourceHolds_Order",
                table: "OrderResourceHolds",
                columns: new[] { "OrderId", "ResourceType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderResourceHolds_Reserved",
                table: "OrderResourceHolds",
                columns: new[] { "ResourceType", "ResourceId", "Status" },
                filter: "\"Status\" = 'Reserved'");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Channel",
                table: "Orders",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CounterShift",
                table: "Orders",
                column: "CounterShiftId",
                filter: "\"CounterShiftId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Employee",
                table: "Orders",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderCode",
                table: "Orders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OriginalOrder",
                table: "Orders",
                column: "OriginalOrderId",
                filter: "\"OriginalOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Pending_ExpiresAt",
                table: "Orders",
                column: "ExpiresAt",
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Type",
                table: "Orders",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_User",
                table: "Orders",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeliveries_Due",
                table: "OutboxDeliveries",
                columns: new[] { "Status", "NextAttemptAt" },
                filter: "\"Status\" IN ('Pending','Processing')");

            migrationBuilder.CreateIndex(
                name: "UX_OutboxDeliveries_Message_Handler",
                table: "OutboxDeliveries",
                columns: new[] { "OutboxMessageId", "HandlerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CreatedAt",
                table: "OutboxMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CounterShift",
                table: "PaymentTransactions",
                column: "CounterShiftId",
                filter: "\"CounterShiftId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CreatedAt",
                table: "PaymentTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_GiftCard",
                table: "PaymentTransactions",
                column: "GiftCardId",
                filter: "\"GiftCardId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_NeedsManualRefund",
                table: "PaymentTransactions",
                column: "NeedsManualRefund",
                filter: "\"NeedsManualRefund\"");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Order",
                table: "PaymentTransactions",
                columns: new[] { "OrderId", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Original",
                table: "PaymentTransactions",
                column: "OriginalTransactionId",
                filter: "\"OriginalTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PerformedByUserId",
                table: "PaymentTransactions",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Status",
                table: "PaymentTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentTransactions_Method_Gateway",
                table: "PaymentTransactions",
                columns: new[] { "Method", "GatewayTransactionId" },
                unique: true,
                filter: "\"GatewayTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentTransactions_TransactionCode",
                table: "PaymentTransactions",
                column: "TransactionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Name",
                table: "Positions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_Type_Active",
                table: "PricingRules",
                columns: new[] { "RuleType", "IsActive" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "UX_PricingRules_Name",
                table: "PricingRules",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCustomerUsages_User",
                table: "PromotionCustomerUsages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_Order",
                table: "PromotionRedemptions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_Promotion_Status",
                table: "PromotionRedemptions",
                columns: new[] { "PromotionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_User_Promotion",
                table: "PromotionRedemptions",
                columns: new[] { "UserId", "PromotionId" },
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_PromotionRedemptions_Promotion_Order_Active",
                table: "PromotionRedemptions",
                columns: new[] { "PromotionId", "OrderId" },
                unique: true,
                filter: "\"Status\" <> 'Released'");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_AssignedUser",
                table: "Promotions",
                column: "AssignedUserId",
                filter: "\"AssignedUserId\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Type_Active",
                table: "Promotions",
                columns: new[] { "Type", "IsActive" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "UX_Promotions_Code",
                table: "Promotions",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_AssignedTo",
                table: "ReconciliationItems",
                column: "AssignedToUserId",
                filter: "\"AssignedToUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_PaymentTransaction",
                table: "ReconciliationItems",
                column: "PaymentTransactionId",
                filter: "\"PaymentTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_Resolution_Result",
                table: "ReconciliationItems",
                columns: new[] { "Resolution", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationItems_ResolvedByUserId",
                table: "ReconciliationItems",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_ReconciliationItems_Session_Code",
                table: "ReconciliationItems",
                columns: new[] { "ReconciliationSessionId", "GatewayTransactionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_CreatedByUserId",
                table: "ReconciliationSessions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_Gateway_CreatedAt",
                table: "ReconciliationSessions",
                columns: new[] { "Gateway", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_ReconciliationSessions_Gateway_FileHash",
                table: "ReconciliationSessions",
                columns: new[] { "Gateway", "FileHash" },
                unique: true,
                filter: "\"Source\" = 'File'");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralRelations_Referrer",
                table: "ReferralRelations",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "UX_ReferralRelations_Referred",
                table: "ReferralRelations",
                column: "ReferredUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                table: "RefreshTokens",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_User",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Movie",
                table: "Reviews",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "UQ_User_Movie_Review",
                table: "Reviews",
                columns: new[] { "UserId", "MovieId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomChangeRefundDues_FromSeatId",
                table: "RoomChangeRefundDues",
                column: "FromSeatId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChangeRefundDues_Order",
                table: "RoomChangeRefundDues",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChangeRefundDues_OrderLineId",
                table: "RoomChangeRefundDues",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChangeRefundDues_Pending",
                table: "RoomChangeRefundDues",
                column: "Status",
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChangeRefundDues_ShowtimeRoomChangeId",
                table: "RoomChangeRefundDues",
                column: "ShowtimeRoomChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChangeRefundDues_ToSeatId",
                table: "RoomChangeRefundDues",
                column: "ToSeatId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomClosures_Room_Time",
                table: "RoomClosures",
                columns: new[] { "RoomId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ScreenTypes_Name",
                table: "ScreenTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatBlocks_ExpiresAt",
                table: "SeatBlocks",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SeatBlocks_Showtime_Type",
                table: "SeatBlocks",
                columns: new[] { "ShowtimeId", "Type" });

            migrationBuilder.CreateIndex(
                name: "UQ_SeatBlocks_ShowtimeSeat",
                table: "SeatBlocks",
                column: "ShowtimeSeatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatHoldEvents_Date_Movie",
                table: "SeatHoldEvents",
                columns: new[] { "BusinessDate", "MovieId" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatHoldEvents_Room_Date",
                table: "SeatHoldEvents",
                columns: new[] { "RoomId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Seats_SeatType",
                table: "Seats",
                column: "SeatTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_Room_Seat",
                table: "Seats",
                columns: new[] { "RoomId", "SeatRow", "SeatNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatTypes_Name",
                table: "SeatTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeDailyStats_Date_Kind",
                table: "ShowtimeDailyStats",
                columns: new[] { "BusinessDate", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeRoomChangeNotices_Order",
                table: "ShowtimeRoomChangeNotices",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeRoomChanges_FromRoomId",
                table: "ShowtimeRoomChanges",
                column: "FromRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeRoomChanges_Showtime",
                table: "ShowtimeRoomChanges",
                column: "ShowtimeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeRoomChanges_ToRoomId",
                table: "ShowtimeRoomChanges",
                column: "ToRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Showtime_Movie",
                table: "Showtimes",
                columns: new[] { "MovieId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Showtime_Room_Time",
                table: "Showtimes",
                columns: new[] { "RoomId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeSalesPaces_Mark",
                table: "ShowtimeSalesPaces",
                column: "MarkMinutesBeforeStart");

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeSeats_HeldByOrderId",
                table: "ShowtimeSeats",
                column: "HeldByOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeSeats_HeldByUserId",
                table: "ShowtimeSeats",
                column: "HeldByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeSeats_SeatId",
                table: "ShowtimeSeats",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowtimeSeats_Status",
                table: "ShowtimeSeats",
                columns: new[] { "ShowtimeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_Showtime_Seat",
                table: "ShowtimeSeats",
                columns: new[] { "ShowtimeId", "SeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDays_Range",
                table: "SpecialDays",
                columns: new[] { "StartDate", "EndDate" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "UX_SpecialDays_Name_StartDate",
                table: "SpecialDays",
                columns: new[] { "Name", "StartDate" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_Item_OccurredAt",
                table: "StockLedger",
                columns: new[] { "InventoryItemId", "OccurredAt", "StockLedgerId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_Reference",
                table: "StockLedger",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptLines_InventoryItemId",
                table: "StockReceiptLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptLines_StockReceiptId",
                table: "StockReceiptLines",
                column: "StockReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_ReceivedDate",
                table: "StockReceipts",
                column: "ReceivedDate");

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSheetLines_InventoryItemId",
                table: "StocktakeSheetLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "UX_StocktakeSheetLines_Sheet_Item",
                table: "StocktakeSheetLines",
                columns: new[] { "StocktakeSheetId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StocktakeSheets_StocktakeDate",
                table: "StocktakeSheets",
                column: "StocktakeDate");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_AuthorUserId",
                table: "SupportMessages",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_Request_CreatedAt",
                table: "SupportMessages",
                columns: new[] { "SupportRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_ApologyPromotionId",
                table: "SupportRequests",
                column: "ApologyPromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_AssignedToUserId",
                table: "SupportRequests",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_OrderId",
                table: "SupportRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_Status_DueAt",
                table: "SupportRequests",
                columns: new[] { "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_User_LastMessageAt",
                table: "SupportRequests",
                columns: new[] { "UserId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "UX_SupportRequests_SourcePaymentTransactionId",
                table: "SupportRequests",
                column: "SourcePaymentTransactionId",
                unique: true,
                filter: "\"SourcePaymentTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_TicketTypes_Name",
                table: "TicketTypes",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleId",
                table: "Users",
                column: "GoogleId",
                unique: true,
                filter: "\"GoogleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVerificationTokens_User_Purpose",
                table: "UserVerificationTokens",
                columns: new[] { "UserId", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "UQ_UserVerificationTokens_TokenHash",
                table: "UserVerificationTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WasteSheetLines_InventoryItemId",
                table: "WasteSheetLines",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "UX_WasteSheetLines_Sheet_Item",
                table: "WasteSheetLines",
                columns: new[] { "WasteSheetId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WasteSheets_WasteDate",
                table: "WasteSheets",
                column: "WasteDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CinemaSettings");

            migrationBuilder.DropTable(
                name: "ComboPickups");

            migrationBuilder.DropTable(
                name: "ComboRecipeLines");

            migrationBuilder.DropTable(
                name: "CustomerProfiles");

            migrationBuilder.DropTable(
                name: "DailyClosings");

            migrationBuilder.DropTable(
                name: "ExceptionalRefundProposals");

            migrationBuilder.DropTable(
                name: "GiftCardAttemptLimit");

            migrationBuilder.DropTable(
                name: "GiftCardLedger");

            migrationBuilder.DropTable(
                name: "GiftCardRevenueEntries");

            migrationBuilder.DropTable(
                name: "ImportExportJobs");

            migrationBuilder.DropTable(
                name: "InventoryReservations");

            migrationBuilder.DropTable(
                name: "InvoiceRequests");

            migrationBuilder.DropTable(
                name: "LedgerAdjustments");

            migrationBuilder.DropTable(
                name: "LoyaltyChallengeProgresses");

            migrationBuilder.DropTable(
                name: "LoyaltyPointTransactions");

            migrationBuilder.DropTable(
                name: "MovieCast");

            migrationBuilder.DropTable(
                name: "MovieGenres");

            migrationBuilder.DropTable(
                name: "OrderExchanges");

            migrationBuilder.DropTable(
                name: "OrderPriceAdjustmentAllocations");

            migrationBuilder.DropTable(
                name: "OrderResourceHolds");

            migrationBuilder.DropTable(
                name: "OutboxDeliveries");

            migrationBuilder.DropTable(
                name: "PaymentDailyStats");

            migrationBuilder.DropTable(
                name: "PricingRules");

            migrationBuilder.DropTable(
                name: "PromotionCustomerUsages");

            migrationBuilder.DropTable(
                name: "PromotionRedemptions");

            migrationBuilder.DropTable(
                name: "ReconciliationItems");

            migrationBuilder.DropTable(
                name: "ReferralRelations");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "RoomChangeRefundDues");

            migrationBuilder.DropTable(
                name: "RoomClosures");

            migrationBuilder.DropTable(
                name: "SeatBlocks");

            migrationBuilder.DropTable(
                name: "SeatHoldEvents");

            migrationBuilder.DropTable(
                name: "ShowtimeDailyStats");

            migrationBuilder.DropTable(
                name: "ShowtimeRoomChangeNotices");

            migrationBuilder.DropTable(
                name: "ShowtimeSalesPaces");

            migrationBuilder.DropTable(
                name: "SpecialDays");

            migrationBuilder.DropTable(
                name: "StockLedger");

            migrationBuilder.DropTable(
                name: "StockReceiptLines");

            migrationBuilder.DropTable(
                name: "StocktakeSheetLines");

            migrationBuilder.DropTable(
                name: "SupportMessages");

            migrationBuilder.DropTable(
                name: "UserVerificationTokens");

            migrationBuilder.DropTable(
                name: "WasteSheetLines");

            migrationBuilder.DropTable(
                name: "ComboRecipes");

            migrationBuilder.DropTable(
                name: "MembershipTiers");

            migrationBuilder.DropTable(
                name: "InvoiceProfiles");

            migrationBuilder.DropTable(
                name: "LoyaltyChallenges");

            migrationBuilder.DropTable(
                name: "LoyaltyPointEntries");

            migrationBuilder.DropTable(
                name: "Actors");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "OrderPriceAdjustments");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ReconciliationSessions");

            migrationBuilder.DropTable(
                name: "OrderLines");

            migrationBuilder.DropTable(
                name: "ShowtimeRoomChanges");

            migrationBuilder.DropTable(
                name: "StockReceipts");

            migrationBuilder.DropTable(
                name: "StocktakeSheets");

            migrationBuilder.DropTable(
                name: "SupportRequests");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "WasteSheets");

            migrationBuilder.DropTable(
                name: "LoyaltyRewards");

            migrationBuilder.DropTable(
                name: "ShowtimeSeats");

            migrationBuilder.DropTable(
                name: "TicketTypes");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "Combos");

            migrationBuilder.DropTable(
                name: "Seats");

            migrationBuilder.DropTable(
                name: "Showtimes");

            migrationBuilder.DropTable(
                name: "GiftCards");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "SeatTypes");

            migrationBuilder.DropTable(
                name: "CinemaRooms");

            migrationBuilder.DropTable(
                name: "Movies");

            migrationBuilder.DropTable(
                name: "CounterShifts");

            migrationBuilder.DropTable(
                name: "ScreenTypes");

            migrationBuilder.DropTable(
                name: "AgeRatings");

            migrationBuilder.DropTable(
                name: "Directors");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
