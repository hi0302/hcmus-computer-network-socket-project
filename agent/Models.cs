namespace RemoteControl.Agent.Models
{
    public class ResponseDto
    {
        public string AgentId { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty; // Ví dụ: "PROCESS_LIST", "SCREENSHOT", "KEYLOG"
        public object Data { get; set; } // Dữ liệu thực tế (List process, ảnh base64...)
    }

    // Class để hứng lệnh từ Server gửi xuống (parse từ commandJson)
    public class CommandDto
    {
        public string CommandType { get; set; } // Ví dụ: "GET_PROCESS", "SHUTDOWN"
        public string Payload { get; set; } // Tham số phụ (PID để kill,...)
    }
}