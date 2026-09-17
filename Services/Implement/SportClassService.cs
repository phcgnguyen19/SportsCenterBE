using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Models.DTOs.Class;
using SportsCenterAPI.Services.Interface;
using AutoMapper;

namespace SportsCenterAPI.Services.Implement
{
    public class SportClassService : ISportClassService
    {
        private readonly AppDbContext _context;

        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public SportClassService(AppDbContext context, IConfiguration configuration, IMapper mapper)
        {
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<SportClassResponse?> GetClassByIdAsync(int classId)
        {
            return await _context.SportClasses
          .AsNoTracking()
          .Where(x => x.Id == classId && x.IsActive)
          .Select(x => new SportClassResponse
          {
              Id = x.Id,
              ClassName = x.ClassName,
              SportId = x.SportId,
              SportName = x.Sport.Name,
              CoachId = x.CoachId,
              Price = x.Price,
              MaxCapacity = x.MaxCapacity,
              Schedule = x.Schedule,
              StartDate = x.StartDate,
              EndDate = x.EndDate,
              IsActive = x.IsActive
          })
          .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<SportClassResponse>> GetClassesAsync(
    SportClassSearchRequest request)
        {
            if (request.FromDate.HasValue &&
                request.ToDate.HasValue &&
                request.FromDate.Value > request.ToDate.Value)
            {
                throw new ArgumentException(
                    "Ngày bắt đầu tìm kiếm không được sau ngày kết thúc.");
            }

            var query = _context.SportClasses
                .AsNoTracking()
                .Include(x => x.Sport)
                .Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();

                query = query.Where(x => x.ClassName.Contains(keyword));
            }

            if (request.SportId.HasValue)
            {
                query = query.Where(x => x.SportId == request.SportId.Value);
            }

            if (request.CoachId.HasValue)
            {
                query = query.Where(x => x.CoachId == request.CoachId.Value);
            }

            // Lớp có khoảng hoạt động giao với khoảng tìm kiếm.
            if (request.FromDate.HasValue)
            {
                query = query.Where(x => x.EndDate >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x => x.StartDate <= request.ToDate.Value);
            }

            var sportClasses = await query
                .OrderBy(x => x.StartDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SportClassResponse>>(sportClasses);
        }

        public Task<IEnumerable<SportClassMemberResponse>> GetClassMembersAsync(int classId, int coachId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SportClassResponse>> GetCoachClassesAsync(int coachId)
        {
            var sportClasses = await _context.SportClasses
                .AsNoTracking()
                .Include(x => x.Sport)
                .Where(x => x.CoachId == coachId)
                .OrderByDescending(x => x.StartDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SportClassResponse>>(sportClasses);
        }
        Task<SportClassResponse> ISportClassService.CreateClassAsync(SportClassCreateRequestDTO sportClassCreateRequestdto)
        {
            throw new NotImplementedException();
        }

        Task<SportClassResponse?> ISportClassService.UpdateClassAsync(int classId, SportClassUpdateRequestDTO sportClassUpdateRequestdto)
        {
            throw new NotImplementedException();
        }

        Task<bool> ISportClassService.DeleteClassAsync(int classId)
        {
            throw new NotImplementedException();

        }
    }
};
