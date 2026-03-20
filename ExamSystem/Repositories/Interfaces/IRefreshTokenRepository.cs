using System;
using System.Threading.Tasks;
using ExamSystem.Models;

namespace ExamSystem.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken> UpdateAsync(RefreshToken refreshToken);
    Task DeleteAsync(RefreshToken refreshToken);
    Task DeleteAllUserTokensAsync(Guid userId);
    Task RevokeAllUserTokensAsync(Guid userId);
}
