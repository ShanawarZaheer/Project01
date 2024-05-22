namespace Project01.DTOs
{
    public class AppResponse
    {
        public int ResCode { get; set; }
        public string ResMsg { get; set; }
        public dynamic ResBody { get; set; }
    }

    public class ResponseResult<T> where T : class
    {
        public bool IsStatus { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
        public T Data { get; set; }
    }
}
