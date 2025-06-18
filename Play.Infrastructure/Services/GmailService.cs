using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Play.Infrastructure.Common.Abstracts;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Mail;

namespace Play.Infrastructure.Services;

public class GmailService : IMailService, IScoped
{
    private readonly GmailOptions _gmailOptions;

    public GmailService(IOptions<GmailOptions> gmailOptions)
    {
        this._gmailOptions = gmailOptions.Value;
    }
    public async Task SendEmailAsync(SendEmailRequest request)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_gmailOptions.Email),
            Subject = request.Subject,
            Body = request.Body
        };

        mailMessage.To.Add(new MailAddress(request.Recipient));

        using var smtpClient = new SmtpClient();
        smtpClient.Host = _gmailOptions.Host;
        smtpClient.Port = _gmailOptions.Port;
        smtpClient.Credentials = new NetworkCredential(
            _gmailOptions.Email, _gmailOptions.Password);
        smtpClient.EnableSsl = true;
        await smtpClient.SendMailAsync(mailMessage);
    }
}
