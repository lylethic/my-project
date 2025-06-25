using System;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Play.Application.DTOs;
using Play.Application.Enums;
using Play.Infrastructure.Common.Abstracts;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Services;
using Play.Infrastructure.Repository;

namespace Play.Infrastructure.Services;

public class AuthService(IServiceProvider services, IDbConnection connection, IHttpContextAccessor httpContextAccessor, GmailService mailService, IMemoryCache memoryCache) : BaseService(services), IScoped
{
    private readonly AuthRepository _repo = new(connection, httpContextAccessor, memoryCache, mailService);

    public async Task<AuthResponse> LoginAsync(AuthenticateDto model)
    {
        return await _repo.Login(model);
    }

    public AuthResponse RefreshTokenAsync(string token)
    {
        return _repo.RefreshToken(token);
    }

    public ResponseData<UserDto> LogoutAsync()
    {
        return _repo.Logout();
    }

    public async Task<ResponseData<string>> SendResetCodeAsync(string userEmail)
    {
        return await _repo.SendResetCode(userEmail);
    }

    public async Task<ResponseData<string>> ConfirmResetPasswordAsync(ResetPasswordRequest request)
    {
        return await _repo.ConfirmResetPassword(request);
    }
}
