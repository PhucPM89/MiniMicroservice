using FileService.Common;
using FileService.Interfaces.Services;
using FileService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;

namespace FileService.Controllers;

[ApiController]
[Authorize]
[Route("api/files")]
public sealed class FilesController(IFileService fileService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Shared.Pagination.PagedResult<FileResponse>>> GetFiles(
        [FromQuery] FileQueryParameters query,
        CancellationToken cancellationToken)
    {
        var actingUserId = User.GetRequiredUserId();
        var canViewAll = User.IsInRole(RoleConstants.Admin);
        var response = await fileService.GetAsync(query, actingUserId, canViewAll, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{fileId:guid}")]
    public async Task<ActionResult<FileResponse>> GetById(Guid fileId, CancellationToken cancellationToken)
    {
        var actingUserId = User.GetRequiredUserId();
        var canViewAll = User.IsInRole(RoleConstants.Admin);
        var response = await fileService.GetByIdAsync(fileId, actingUserId, canViewAll, cancellationToken);
        return Ok(response);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FileResponse>> Upload([FromForm] UploadFileRequest request, CancellationToken cancellationToken)
    {
        var uploadedByUserId = User.GetRequiredUserId();
        var response = await fileService.UploadAsync(request.File, uploadedByUserId, cancellationToken);
        return Ok(response);
    }
}
