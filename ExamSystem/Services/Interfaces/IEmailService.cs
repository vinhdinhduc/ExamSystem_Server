namespace ExamSystem.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string toName, string verificationLink);
    Task SendPasswordResetOtpAsync(string toEmail, string toName, string otp);
}