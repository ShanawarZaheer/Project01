namespace Project01.Middlewares
{
    public class FileValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public FileValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            if (context.Request.HasFormContentType && context.Request.Form.Files.Any())
            {
                foreach (var file in context.Request.Form.Files)
                {
                    if (!IsValidFileExtension(file.FileName))
                    {
                        context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                        await context.Response.WriteAsync("Unsupported file type.");
                        return;
                    }
                }
            }

            await _next(context);
        }
        private bool IsValidFileExtension(string fileName)
        {
            //string[] allowedExtensions = { ".jpg", ".jpeg", ".pdf", ".png", ".xls", ".xlsx" };
            string[] allowedExtensions = {".xls", ".xlsx" };

            var extension = Path.GetExtension(fileName);
            return allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}
