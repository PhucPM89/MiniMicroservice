namespace FileService.Models;

public sealed record StoredFileResult(
    string OriginalFileName,
    string StoredFileName,
    string RelativePath,
    string ContentType,
    string FileExtension,
    long SizeInBytes);
