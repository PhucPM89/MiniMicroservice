using System.ComponentModel.DataAnnotations;

namespace FileService.Models;

public sealed class UploadFileRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
