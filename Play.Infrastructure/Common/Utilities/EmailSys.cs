using System;
using System.Net;
using System.Net.Mail;

namespace Play.Infrastructure.Common.Utilities;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}

public class EmailSys : IEmailService
{
    private readonly string smtpHost;
    private readonly int smtpPort;
    private readonly string smtpUser;
    private readonly string smtpPass;
    private readonly bool enableSsl;
    public EmailSys(string host, int port, string user, string pass, bool ssl = true)
    {
        this.smtpHost = host;
        this.smtpPort = port;
        this.smtpUser = user;
        this.smtpPass = pass;
        this.enableSsl = ssl;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            using (var smtp = new SmtpClient(smtpHost, smtpPort))
            {
                smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);
                smtp.EnableSsl = enableSsl;
                smtp.TargetName = "STARTTLS/smtp.gmail.com"; // Optional but may help
                
                var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                mail.To.Add(toEmail);
                await smtp.SendMailAsync(mail);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            return false;
        }
    }

}

