using System;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Play.Application.DTOs;
using Play.Infrastructure.Common.Abstracts;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Services;
using Play.Infrastructure.Repository;

namespace Play.Infrastructure.Services;

public class AuthService(IServiceProvider services, IDbConnection connection, IHttpContextAccessor httpContextAccessor, GmailService mailService, IMemoryCache memoryCache) : BaseService(services), IScoped
{
    private readonly AuthRepository _repo = new(connection, httpContextAccessor, memoryCache, mailService);

    public async Task<ResponseData<AuthResponse>> LoginAsync(AuthenticateDto model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model), "Authentication model cannot be null.");

        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            return new ResponseData<AuthResponse>
            {
                StatusCode = 400,
                Message = "Email and password are required."
            };

        return await _repo.Login(model);
    }

    public ResponseData<AuthResponse> RefreshTokenAsync(TokenApiDto model)
    {
        return _repo.RefreshToken(model);
    }

    public Task<ResponseData<UserDto>> LogoutAsync()
    {
        return _repo.Logout();
    }

    public async Task<string> SendResetCodeAsync(string userEmail)
    {
        return await _repo.SendResetCode(userEmail);
    }

    public async Task<string> ConfirmResetPasswordAsync(ResetPasswordRequest request)
    {
        return await _repo.ConfirmResetPassword(request);
    }
}
