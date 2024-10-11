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

            // Đường dẫn tới thư mục cài đặt PDFtoMusic Pro và lệnh p2mp
            string pdfToMusicProFolder = @"C:\Program Files\PDFtoMusic Pro"; 
            string p2mpCommand = Path.Combine(pdfToMusicProFolder, "p2mp.exe");
            string outputFilePath = Path.ChangeExtension(filePath, ".xml");

            // Thực thi lệnh chuyển đổi PDF sang MusicXML bằng cách chạy p2mp
            var processInfo = new ProcessStartInfo
            {
                FileName = p2mpCommand,
                Arguments = $"\"{filePath}\"",  // Truyền đường dẫn file PDF
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

                    // Bắt sự kiện khi có output hoặc error
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

                    // Khởi động quá trình chuyển đổi
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Đợi cho quá trình hoàn thành
                    process.WaitForExit();

                    // Kiểm tra xem file .musicxml có tồn tại không
                    if (System.IO.File.Exists(outputFilePath))
                    {
                        // Trả về file MusicXML đã chuyển đổi
                        var musicXmlFile = System.IO.File.OpenRead(outputFilePath);
                        return File(musicXmlFile, "application/xml", Path.GetFileName(outputFilePath));
                    }
                    else
                    {
                        return StatusCode(500, "Chuyển đổi thất bại.");
                    }
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
