using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using System.Data;
using Npgsql;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.DTOs.Rooms;
using SportsCenterAPI.Models;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Services.Implement;

public class RoomService : IRoomService
{
    private readonly AppDbContext _context;

    public RoomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RoomResponse>> GetAllRoomsAsync(bool? isActive = null)
    {
        var query = _context.Rooms.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        return await query.OrderBy(r => r.Name)
            .Select(r => MapToResponse(r))
            .ToListAsync();
    }

    public async Task<RoomResponse> GetRoomByIdAsync(int id)
    {
        var room = await _context.Rooms.FindAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy phòng tập.");

        return MapToResponse(room);
    }

    public async Task<RoomResponse> CreateRoomAsync(CreateRoomRequest request)
    {
        var exists = await _context.Rooms.AnyAsync(r => r.Name.ToLower() == request.Name.Trim().ToLower());
        if (exists)
            throw new InvalidOperationException($"Phòng với tên '{request.Name}' đã tồn tại.");

        var room = new Room
        {
            Name = request.Name.Trim(),
            RoomType = request.RoomType,
            Capacity = request.Capacity,
            Location = request.Location,
            IsActive = true
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return MapToResponse(room);
    }

    public async Task<RoomResponse> UpdateRoomAsync(int id, UpdateRoomRequest request)
    {
        return await Transaction(async () =>
        {
            var room = await _context.Rooms.SingleOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            var duplicateName = await _context.Rooms
                .AnyAsync(r => r.Id != id && r.Name.ToLower() == request.Name.Trim().ToLower());
            if (duplicateName)
                throw new InvalidOperationException($"Tên phòng '{request.Name}' đã được sử dụng bởi phòng khác.");

            await EnsureRoomCanChangeAsync(id, request.IsActive, request.Capacity);

            room.Name = request.Name.Trim();
            room.RoomType = request.RoomType;
            room.Capacity = request.Capacity;
            room.Location = request.Location;
            room.IsActive = request.IsActive;

            return MapToResponse(room);
        });
    }

    public async Task DeleteRoomAsync(int id)
    {
        await Transaction(async () =>
        {
            var room = await _context.Rooms.SingleOrDefaultAsync(r => r.Id == id)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng tập.");

            await EnsureRoomCanChangeAsync(id, false, room.Capacity);

            // Giữ dữ liệu phòng để tra cứu lịch sử.
            room.IsActive = false;
            return true;
        });
    }

    private async Task EnsureRoomCanChangeAsync(int roomId, bool isActive, int capacity)
    {
        var now = DateTime.UtcNow;
        var sessions = _context.ClassSessions.Where(s =>
            s.RoomId == roomId && s.Status == "Scheduled" && s.EndsAt > now);

        if (!isActive && await sessions.AnyAsync())
            throw new BusinessException(409, "Không thể ngừng hoạt động phòng đang có buổi học chưa kết thúc.");

        if (await sessions.AnyAsync(s => s.Capacity > capacity))
            throw new BusinessException(409, "Sức chứa phòng không thể thấp hơn số chỗ của buổi học chưa kết thúc.");
    }

    private async Task<T> Transaction<T>(Func<Task<T>> action)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var result = await action();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex) when (SqlConflict(ex))
        {
            throw new BusinessException(409, "Dữ liệu vừa thay đổi bởi yêu cầu khác. Vui lòng tải lại và thử lại.");
        }
    }

    private static bool SqlConflict(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
            if (current is PostgresException { SqlState: "40001" or "40P01" }) return true;
        return exception is DbUpdateConcurrencyException;
    }

    // Thuật toán kiểm tra chống trùng lịch học tại cùng 1 phòng
    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime startsAt, DateTime endsAt, int? excludeSessionId = null)
    {
        // 2 khung giờ trùng nhau khi: startsAt < s.EndsAt && endsAt > s.StartsAt
        var isOccupied = await _context.ClassSessions
            .AnyAsync(s => s.RoomId == roomId
                        && s.Status != "Cancelled"
                        && (excludeSessionId == null || s.Id != excludeSessionId)
                        && startsAt < s.EndsAt
                        && endsAt > s.StartsAt);

        return !isOccupied; // Trả về true nếu phòng còn trống
    }

    private static RoomResponse MapToResponse(Room r)
    {
        return new RoomResponse
        {
            Id = r.Id,
            Name = r.Name,
            RoomType = r.RoomType,
            Capacity = r.Capacity,
            Location = r.Location,
            IsActive = r.IsActive
        };
    }
}
