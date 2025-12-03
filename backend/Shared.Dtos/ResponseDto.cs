// file: Shared.Dtos/ResponseDto.cs 
namespace YourApplicationName.Dtos
{
    public class ResponseDto
    {
        public string AgentId { get; set; }
        public string DataType { get; set; }
        public object Data { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
