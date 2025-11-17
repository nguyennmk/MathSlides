using MailKit.Net.Smtp;
using MailKit.Security;
using MathSlides.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;

namespace MathSlides.Service.Services
{
    public class MailKitEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MailKitEmailService> _logger;
        private readonly SmtpSettings _smtpSettings;

        public MailKitEmailService(IConfiguration config, ILogger<MailKitEmailService> logger)
        {
            _config = config;
            _logger = logger;
            _smtpSettings = _config.GetSection("SmtpSettings").Get<SmtpSettings>()
                ?? throw new InvalidOperationException("SmtpSettings không được cấu hình.");
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string code)
        {
            var subject = "Yêu cầu khôi phục mật khẩu MathSlides";
            var body = $@"
                <p>Chào bạn,</p>
                <p>Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn.</p>
                <p>Mã khôi phục của bạn là:</p>
                <h2 style='font-family: monospace; color: #d9534f;'>{code}</h2>
                <p>Mã này có hiệu lực trong <b>5 phút</b>.</p>
                <p>Nếu bạn không yêu cầu điều này, vui lòng bỏ qua email này.</p>
                <p>Trân trọng,<br>Đội ngũ MathSlides</p>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var smtp = new SmtpClient();
                _logger.LogInformation($"Connecting to SMTP server: {_smtpSettings.SmtpServer} on port {_smtpSettings.Port}");

                await smtp.ConnectAsync(_smtpSettings.SmtpServer, _smtpSettings.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                var result = await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation($"Email sent to {to}, response: {result}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email tới {to}");
            }
        }
    }

    internal class SmtpSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
