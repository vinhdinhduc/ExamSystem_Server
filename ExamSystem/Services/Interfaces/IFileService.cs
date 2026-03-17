using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ExamSystem.Services.Interfaces;

public interface IFileService
{
    /// <summary>
    /// Lưu file upload vào thư mục avatars, trả về tên file ngẫu nhiên (không có path).
    /// </summary>
    Task<string> SaveAvatarAsync(IFormFile file);

    /// <summary>
    /// Xóa file avatar cũ nếu tồn tại.
    /// </summary>
    void DeleteAvatar(string? fileName);
}
