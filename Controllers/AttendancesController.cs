using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SportsCenterAPI.Services.Interface;
using SportsCenterAPI.DTOs.Attendances;
using SportsCenterAPI.Helpers;

namespace SportsCenterAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttendancesController(IAttendanceService attendanceService) : ControllerBase
    {
        /// <summary>
        /// Điểm danh hội viên (Dành cho lễ tân hoặc Huấn luyện viên)
        /// </summary>
        [HttpPost("mark")]
        [Authorize(Roles = UserRoles.FrontDesk + "," + UserRoles.Coach)] //Chỉ nhân viên mới được điểm danh
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await attendanceService.MarkAttendanceAsync(request);
                return Ok(new { message = "Điểm danh thành công", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here if needed
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi điểm danh hội viên", error = ex.Message });
            }
            
        }

        /// <summary>
        /// Lấy danh sách những hội viên đã điểm danh trong một buổi học cụ thể (Dành cho lễ tân hoặc Huấn luyện viên)
        /// </summary>
        [HttpGet("session/{sessionId}")]
        [Authorize(Roles = UserRoles.FrontDesk + "," + UserRoles.Coach)]
        public async Task<IActionResult> GetSessionAttendances(int sessionId)
        {
            try
            {
                var result = await attendanceService.GetSessionAttendancesAsync(sessionId);
                return Ok(new { message = "Lấy danh sách điểm danh thành công", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here if needed
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách điểm danh", error = ex.Message });
            }
        }
    }
}
