public class ResponseDto
{
    public string AgentId { get; set; }        // ID của Agent gửi phản hồi
    public string DataType { get; set; }       // Loại dữ liệu: "PROCESS_LIST", "SCREENSHOT", "KEYLOG_CHUNK"
    public object Data { get; set; }           // Dữ liệu thực tế (List<ProcessInfo>, string Base64, string KeyPresses)
    public bool Success { get; set; }          // Trạng thái thực thi lệnh
    public string? ErrorMessage { get; set; }  // Thông báo lỗi nếu có
}
