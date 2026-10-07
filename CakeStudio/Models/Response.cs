namespace CakeStudio.Models
{
    public class Response
    {
        public bool Status { get; set; }

        public string Message { get; set; } = string.Empty;

        public string MessageCode { get; set; } = string.Empty;

        public object? Data { get; set; }
    }
}