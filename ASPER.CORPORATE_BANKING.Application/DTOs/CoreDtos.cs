namespace ASPER.CORPORATE_BANKING.Application.DTOs
{
    public class ResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        
        public static ResultDto Success(string message = "Operation successful") => new ResultDto { IsSuccess = true, Message = message };
        public static ResultDto Failure(string message) => new ResultDto { IsSuccess = false, Message = message };
    }

    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }
}
