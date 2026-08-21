using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.DTOs.Auth;

namespace InvoiceTrackingSystemBackend.Interfaces.Auth;

public interface IUserService
{
    Task<PagedResult<UserListDto>> GetListAsync(int actorUserId, int page = 1, int pageSize = 20);
    Task<UserListDto?> GetByIdAsync(int actorUserId, int id);
    Task<UserMeDto> GetMeAsync(int userId);
    Task<UserMeDto> UpdateMeAsync(int userId, UpdateMeRequestDto request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
    Task<UserMeDto> UpdateOutOfOfficeAsync(int userId, UpdateOutOfOfficeRequestDto request);
    Task<UserFileMetaDto> UploadMyFileAsync(int userId, UserFileKind fileKind, IFormFile file);
    Task<UserFileMetaDto> GetMyFileMetaAsync(int userId, UserFileKind fileKind);
    Task<(Stream Content, string ContentType, string FileName)> OpenMyFileAsync(int userId, UserFileKind fileKind);
}
