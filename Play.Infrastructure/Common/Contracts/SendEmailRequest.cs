using System;

namespace Play.Infrastructure.Common.Contracts;

public record SendEmailRequest(string Recipient, string Subject, string Body);
