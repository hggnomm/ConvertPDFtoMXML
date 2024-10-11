using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Diagnostics;

namespace ConvertPDFtoMXML.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(ILogger<FileUploadController> logger)
        {
            _logger = logger;
        }

        [HttpPost("upload")]
        public IActionResult UploadPDF(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Không có file nào được upload.");
            }

            // Lưu file PDF tạm thời
            var filePath = Path.Combine(Path.GetTempPath(), file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Đường dẫn tới công cụ PDFtoMusic Pro
            string pdfToMusicProFolder = @"C:\Program Files\PDFtoMusic Pro"; // Đảm bảo đường dẫn chính xác
            string p2mpCommand = Path.Combine(pdfToMusicProFolder, "p2mp.exe");
            string outputFilePath = Path.ChangeExtension(filePath, ".xml");

            var processInfo = new ProcessStartInfo
            {
                FileName = p2mpCommand,
                Arguments = $"\"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = processInfo;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            _logger.LogInformation($"Output: {e.Data}");
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            _logger.LogError($"Error: {e.Data}");
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }

                // Kiểm tra xem file MusicXML có tồn tại không
                if (System.IO.File.Exists(outputFilePath))
                {
                    // Đọc toàn bộ nội dung của file MusicXML vào bộ nhớ
                    byte[] fileBytes = System.IO.File.ReadAllBytes(outputFilePath);

                    // Xóa các file tạm sau khi đã đọc nội dung
                    System.IO.File.Delete(filePath);         // Xóa file PDF
                    System.IO.File.Delete(outputFilePath);   // Xóa file MusicXML

                    // Trả về nội dung file MusicXML dưới dạng byte[]
                    return File(fileBytes, "application/xml", Path.GetFileName(outputFilePath));
                }
                else
                {
                    return StatusCode(500, "Chuyển đổi thất bại.");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình chuyển đổi file.");
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
    }
}
