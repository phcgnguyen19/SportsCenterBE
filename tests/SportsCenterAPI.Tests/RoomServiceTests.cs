using SportsCenterAPI.DTOs.Rooms;
using SportsCenterAPI.Models;
using SportsCenterAPI.Services.Implement;

namespace SportsCenterAPI.Tests;

public class RoomServiceTests
{
    [Fact]
    public async Task CreateRoomAsync_ValidRequest_CreatesActiveRoom()
    {
        using var db = TestDbHelper.CreateDbContext();
        var service = new RoomService(db);

        var request = new CreateRoomRequest
        {
            Name = "  Studio Yoga 01  ",
            RoomType = "Yoga",
            Capacity = 25,
            Location = "Tầng 2 - Khu A"
        };

        var result = await service.CreateRoomAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Studio Yoga 01", result.Name);
        Assert.Equal("Yoga", result.RoomType);
        Assert.Equal(25, result.Capacity);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateRoomAsync_DuplicateNameCaseInsensitive_ThrowsInvalidOperationException()
    {
        using var db = TestDbHelper.CreateDbContext();
        db.Rooms.Add(new Room
        {
            Id = 1,
            Name = "Studio Yoga 01",
            RoomType = "Yoga",
            Capacity = 25,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new RoomService(db);
        var request = new CreateRoomRequest
        {
            Name = "studio yoga 01",
            RoomType = "Yoga",
            Capacity = 30
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateRoomAsync(request));
    }

    [Fact]
    public async Task UpdateRoomAsync_DuplicateNameWithAnotherRoom_ThrowsInvalidOperationException()
    {
        using var db = TestDbHelper.CreateDbContext();
        db.Rooms.AddRange(
            new Room { Id = 1, Name = "Phòng Gym Khu A", RoomType = "Gym", Capacity = 50, IsActive = true },
            new Room { Id = 2, Name = "Phòng Boxing Khu B", RoomType = "Boxing", Capacity = 20, IsActive = true }
        );
        await db.SaveChangesAsync();

        var service = new RoomService(db);
        var updateRequest = new UpdateRoomRequest
        {
            Name = "Phòng Gym Khu A", // Trùng tên với phòng Id = 1
            RoomType = "Boxing",
            Capacity = 25,
            IsActive = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRoomAsync(2, updateRequest));
    }

    [Fact]
    public async Task DeleteRoomAsync_NoFutureSessions_SoftDeletesRoom()
    {
        using var db = TestDbHelper.CreateDbContext();
        db.Rooms.Add(new Room
        {
            Id = 10,
            Name = "Phòng Tập Cũ",
            RoomType = "General",
            Capacity = 20,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new RoomService(db);

        await service.DeleteRoomAsync(10);

        var roomInDb = await db.Rooms.FindAsync(10);
        Assert.NotNull(roomInDb);
        Assert.False(roomInDb!.IsActive);
    }

    [Fact]
    public async Task DeleteRoomAsync_WithFutureActiveSession_ThrowsInvalidOperationException()
    {
        using var db = TestDbHelper.CreateDbContext();
        db.Rooms.Add(new Room
        {
            Id = 11,
            Name = "Sân Đa Năng 01",
            RoomType = "General",
            Capacity = 40,
            IsActive = true
        });

        db.ClassSessions.Add(new ClassSession
        {
            Id = 100,
            ClassId = 1,
            CoachId = 1,
            CancellationPolicyId = 1,
            RoomId = 11,
            StartsAt = DateTime.UtcNow.AddDays(2),
            EndsAt = DateTime.UtcNow.AddDays(2).AddHours(1),
            Capacity = 30,
            Status = "Scheduled"
        });
        await db.SaveChangesAsync();

        var service = new RoomService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteRoomAsync(11));
    }

    [Fact]
    public async Task IsRoomAvailableAsync_DetectsOverlappingAndNonOverlappingSchedulesCorrectly()
    {
        using var db = TestDbHelper.CreateDbContext();
        var baseTime = DateTime.UtcNow.Date.AddDays(1).AddHours(18); // 18:00 ngày mai

        db.Rooms.Add(new Room { Id = 5, Name = "Studio 05", RoomType = "Yoga", Capacity = 20, IsActive = true });
        db.ClassSessions.Add(new ClassSession
        {
            Id = 200,
            ClassId = 1,
            CoachId = 1,
            CancellationPolicyId = 1,
            RoomId = 5,
            StartsAt = baseTime,                 // 18:00
            EndsAt = baseTime.AddHours(2),       // 20:00
            Capacity = 20,
            Status = "Scheduled"
        });
        await db.SaveChangesAsync();

        var service = new RoomService(db);

        // 1. Khung giờ 19:00 - 21:00 (Đè lên ca 18:00 - 20:00) -> Phòng BẬN (false)
        var isAvailableOverlap = await service.IsRoomAvailableAsync(5, baseTime.AddHours(1), baseTime.AddHours(3));
        Assert.False(isAvailableOverlap);

        // 2. Khung giờ 20:00 - 21:30 (Ngay sau khi ca trước kết thúc) -> Phòng TRỐNG (true)
        var isAvailableAfter = await service.IsRoomAvailableAsync(5, baseTime.AddHours(2), baseTime.AddHours(3.5));
        Assert.True(isAvailableAfter);
    }
}