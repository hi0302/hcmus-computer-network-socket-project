// file: Shared.Dtos/ProcessModel.cs (Tạo mới)

namespace YourApplicationName.Dtos
{
    // Class chứa dữ liệu Process để gửi đi (DTO)
    public class ProcessModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long MemoryMB { get; set; }
        public string WindowTitle { get; set; } // Dùng để phân biệt App vs Process ngầm
    }
}
