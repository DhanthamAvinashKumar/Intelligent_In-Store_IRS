using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        [Authorize(Roles = "manager")]
        [HttpGet("manager-dashboard")]
        public IActionResult GetManagerDashboard() =>
            Ok("Visible to managers only");

        [Authorize(Roles = "staff")]
        [HttpGet("staff-tasks")]
        public IActionResult GetStaffTasks() =>
            Ok("Visible to staff only");

        [Authorize(Roles = "manager,staff")]
        [HttpGet("shared-tasks")]
        public IActionResult GetSharedTasks() =>
            Ok("Visible to both managers and staff");
    }
}
