using ExamSystem.Common;
using ExamSystem.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ExamSystem.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string toName, string verificationLink)
    {
        var subject = "[Exam System] Xác thực tài khoản của bạn";
        var body = $@"
        <div style='font-family:sans-serif;max-width:600px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:8px'>
            <h2 style='color:#0f6cbd'>Xác thực tài khoản</h2>
            <p>Xin chào <strong>{toName}</strong>,</p>
            <p>Cảm ơn bạn đã đăng ký tài khoản tại <strong>Exam System</strong>.</p>
            <p>Vui lòng nhấn vào nút bên dưới để xác thực email và kích hoạt tài khoản:</p>
            <div style='text-align:center;margin:32px 0'>
                <a href='{verificationLink}'
                   style='background:#0f6cbd;color:#fff;padding:12px 32px;border-radius:6px;text-decoration:none;font-weight:bold;display:inline-block'>
                    Xác thực tài khoản
                </a>
            </div>
            <p style='color:#6b7280;font-size:14px'>Link có hiệu lực trong <strong>15 phút</strong>.</p>
            <p style='color:#6b7280;font-size:14px'>Nếu bạn không thực hiện đăng ký, hãy bỏ qua email này.</p>
            <hr style='border:none;border-top:1px solid #e5e7eb;margin:24px 0'/>
            <p style='color:#9ca3af;font-size:12px'>Exam System — Hệ thống thi trắc nghiệm trực tuyến</p>
        </div>";

        await SendAsync(toEmail, toName, subject, body);
    }

    public async Task SendPasswordResetOtpAsync(string toEmail, string toName, string otp)
    {
        var subject = "[Exam System] Mã OTP đặt lại mật khẩu";
        var body = $@"
        <div style='font-family:sans-serif;max-width:600px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:8px'>
            <h2 style='color:#0f6cbd'>Đặt lại mật khẩu</h2>
            <p>Xin chào <strong>{toName}</strong>,</p>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
            <p>Mã OTP của bạn là:</p>
            <div style='text-align:center;margin:32px 0'>
                <span style='font-size:36px;font-weight:bold;letter-spacing:8px;color:#0f6cbd;background:#f0f7ff;padding:16px 32px;border-radius:8px;display:inline-block'>
                    {otp}
                </span>
            </div>
            <p style='color:#6b7280;font-size:14px'>Mã có hiệu lực trong <strong>10 phút</strong>.</p>
            <p style='color:#6b7280;font-size:14px'>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
            <hr style='border:none;border-top:1px solid #e5e7eb;margin:24px 0'/>
            <p style='color:#9ca3af;font-size:12px'>Exam System — Hệ thống thi trắc nghiệm trực tuyến</p>
        </div>";

        await SendAsync(toEmail, toName, subject, body);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}