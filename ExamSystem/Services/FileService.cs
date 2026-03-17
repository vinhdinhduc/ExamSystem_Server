using System;
using System.IO;
using System.Threading.Tasks;
using ExamSystem.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ExamSystem.Services;

public class FileService : IFileService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private readonly string _avatarDirectory;

    public FileService(IWebHostEnvironment env)
    {
        _avatarDirectory = Path.Combine(env.WebRootPath, "avatars");
        Directory.CreateDirectory(_avatarDirectory);
    }

    public async Task<string> SaveAvatarAsync(IFormFile file)
    {
        if (file.Length == 0)
            throw new ArgumentException("File không được rỗng");

        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException("File không được vượt quá 5MB");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Array.Exists(AllowedExtensions, e => e == ext))
            throw new ArgumentException("Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif, .webp)");

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(_avatarDirectory, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return fileName;
    }

    public void DeleteAvatar(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;
        var filePath = Path.Combine(_avatarDirectory, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
