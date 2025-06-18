using System;
using Play.Infrastructure.Common.Contracts;

namespace Play.Infrastructure.Common.Abstracts;

public interface IMailService
{
    Task SendEmailAsync(SendEmailRequest request);
}
