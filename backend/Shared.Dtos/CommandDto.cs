namespace YourApplicationName.Dtos
{
    public class CommandDto
    {
        // Tên lệnh Agent cần thực thi (ví dụ: "LIST_PROCESS", "START_APP", "SHUTDOWN")
        public string Command { get; set; }

        // Tham số tùy chọn cho lệnh (ví dụ: tên ứng dụng, ID process, số giây record)
        public string? Target { get; set; }

        // Tham số số (ví dụ: số giây record webcam)
        public int? DurationSeconds { get; set; }
    }
}
