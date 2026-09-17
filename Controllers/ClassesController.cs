using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.API;
using SportsCenterAPI.Models.DTOs.Class;
using SportsCenterAPI.Services.Interface;
using SportsCenterAPI.Services.Interface;
using System.Collections.Generic;

namespace SportsCenterAPI.Controllers
{
    [ApiController]
    [Route("api/classes")]
    public class ClassesController : ControllerBase
    {
        private readonly ISportClassService _classService;
        private readonly IMapper _mapper;

        public ClassesController(ISportClassService classService, IMapper mapper)
        {
            _classService = classService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SportClassResponse>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<SportClassResponse>>> GetClasses(
            [FromQuery] SportClassSearchRequest request)
        {
            var classes = await _classService.GetClassesAsync(request);

            var response = ApiResponse<IEnumerable<SportClassResponse>>.Ok(
                classes,
                "Get Classes successful");

            return Ok(response);
        }

        // GET: api/classes/5
        [HttpGet("{classId:int}")]
        public async Task<ActionResult<ApiResponse<SportClassResponse>>>
            GetClassById(int classId)
        {
            if (classId <= 0)
            {
                return BadRequest(
                    ApiResponse<object>.BadRequest("ID lớp không hợp lệ."));
            }

            var result = await _classService.GetClassByIdAsync(classId);

            if (result == null)
            {
                return NotFound(
                    ApiResponse<object>.NotFound("Không tìm thấy lớp học."));
            }

            return Ok(ApiResponse<SportClassResponse>.Ok(
                result, "Lấy thông tin lớp thành công."));
        }

        // POST: api/classes
        [Authorize(Roles = "Manager")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SportClassResponse>>>
            CreateClass([FromBody] SportClassCreateRequestDTO sportClassCreateRequestDTO)
        {
            if (sportClassCreateRequestDTO.EndDate <= sportClassCreateRequestDTO.StartDate)
            {
                return BadRequest(ApiResponse<object>.BadRequest(
                    "Ngày kết thúc phải sau ngày bắt đầu."));
            }

            var result = await _classService.CreateClassAsync(sportClassCreateRequestDTO);

            var response = ApiResponse<SportClassResponse>.Ok(
                result, "Tạo lớp thành công.");

            return CreatedAtAction(
                nameof(GetClassById),
                new { classId = result.Id },
                response);
        }

        // PUT: api/classes/5
        [Authorize(Roles = "Manager")]
        [HttpPut("{classId:int}")]
        public async Task<ActionResult<ApiResponse<SportClassResponse>>>
            UpdateClass(
                int classId,
                [FromBody] SportClassUpdateRequestDTO sportClassUpdateRequestDTO)
        {
            if (classId <= 0)
            {
                return BadRequest(
                    ApiResponse<object>.BadRequest("ID lớp không hợp lệ."));
            }

            if (sportClassUpdateRequestDTO.EndDate <= sportClassUpdateRequestDTO.StartDate)
            {
                return BadRequest(ApiResponse<object>.BadRequest(
                    "Ngày kết thúc phải sau ngày bắt đầu."));
            }

            var result = await _classService.UpdateClassAsync(
                classId, sportClassUpdateRequestDTO);

            if (result == null)
            {
                return NotFound(
                    ApiResponse<object>.NotFound("Không tìm thấy lớp học."));
            }

            return Ok(ApiResponse<SportClassResponse>.Ok(
                result, "Cập nhật lớp thành công."));
        }

        // DELETE: api/classes/5
        [Authorize(Roles = "Manager")]
        [HttpDelete("{classId:int}")]
        public async Task<IActionResult> DeleteClass(int classId)
        {
            if (classId <= 0)
            {
                return BadRequest(
                    ApiResponse<object>.BadRequest("ID lớp không hợp lệ."));
            }

            var deleted = await _classService.DeleteClassAsync(classId);

            if (!deleted)
            {
                return NotFound(
                    ApiResponse<object>.NotFound("Không tìm thấy lớp học."));
            }

            return NoContent();
        }

        // Giữ các method GET danh sách, lịch dạy và học viên tại đây.
    }
}
  