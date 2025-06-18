using System;

namespace Play.Infrastructure.Common.Mail;

public class GmailOptions
{
    public const string GmailOptionsKey = "GmailOptions";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
