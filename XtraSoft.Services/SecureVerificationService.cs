using IptvWebProject.Data;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Security.Cryptography;
using System.Text;
using XtraSoft.Core;
using XtraSoft.Infrastructure;

namespace XtraSoft.Services
{
    public class SecureVerificationService
    {
        private readonly IConfiguration _configuration;
        private readonly IVerificationCodeRepository _repository;
        private readonly ILogger<SecureVerificationService> _logger;
        private readonly string _encryptionKey;

        public SecureVerificationService(
            IConfiguration configuration,
            IVerificationCodeRepository repository,
            ILogger<SecureVerificationService> logger)
        {
            _configuration = configuration;
            _repository = repository;
            _logger = logger;
            _encryptionKey = _configuration["Encryption:Key"] ?? throw new ArgumentNullException("Encryption key not configured");
        }

        public async Task<VerificationResult> SendVerificationCodeAsync(string userEmail, string userId, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                _logger.LogInformation("Sending verification code for user {UserId} to {Email}", userId, userEmail);

                // Generate verification code
                var verificationCode = GenerateVerificationCode();
                _logger.LogInformation("Generated verification code: {Code}", verificationCode);

                // Encrypt the code using a more robust method
                var encryptedCode = EncryptCode(verificationCode, userId);
                _logger.LogInformation("Encrypted code generated successfully");

                // Create database record
                var record = new VerificationCodeRecord
                {
                    UserId = userId,
                    Email = userEmail,
                    EncryptedCode = encryptedCode,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                await _repository.CreateAsync(record);
                _logger.LogInformation("Verification code record created with ID: {RecordId}", record.Id);

                // Send email with plain code (user can see it)
                await SendSecureEmailAsync(userEmail, verificationCode, userId);

                return new VerificationResult
                {
                    EncryptedCode = encryptedCode,
                    Code = verificationCode,
                    Expiry = record.ExpiresAt,
                    RecordId = record.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification code for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> VerifyCodeAsync(string userId, string inputCode, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                _logger.LogInformation("Verifying code for user {UserId}", userId);

                // Get the active verification record
                var record = await _repository.GetActiveByUserIdAsync(userId);

                if (record == null)
                {
                    _logger.LogWarning("No active verification record found for user {UserId}", userId);
                    return false;
                }

                _logger.LogInformation("Found verification record ID: {RecordId}", record.Id);

                // Check if expired
                if (record.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Verification code expired for user {UserId}. Expired at: {ExpiresAt}", userId, record.ExpiresAt);
                    return false;
                }

                // Decrypt stored code and compare
                var decryptedCode = DecryptCode(record.EncryptedCode, userId);
                _logger.LogInformation("Decrypted code: {DecryptedCode}", decryptedCode);
                _logger.LogInformation("Input code: {InputCode}", inputCode);

                if (decryptedCode == inputCode)
                {
                    // Mark as used
                    await _repository.MarkAsUsedAsync(record.Id, ipAddress, userAgent);
                    _logger.LogInformation("Verification successful for user {UserId}", userId);
                    return true;
                }

                _logger.LogWarning("Invalid verification code attempt for user {UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verification failed for user {UserId}", userId);
                return false;
            }
        }

        private string GenerateVerificationCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            return (BitConverter.ToUInt32(bytes, 0) % 1000000).ToString("D6");
        }

        private string EncryptCode(string code, string userId)
        {
            try
            {
                // Use a more deterministic approach for key derivation
                var key = GenerateKeyFromUserId(userId);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Generate a random IV for each encryption
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var plainBytes = Encoding.UTF8.GetBytes(code);
                var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                // Combine IV and encrypted data
                var result = new byte[aes.IV.Length + encryptedBytes.Length];
                Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
                Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encryption failed for user {UserId}", userId);
                throw;
            }
        }

        private string DecryptCode(string encryptedCode, string userId)
        {
            try
            {
                var key = GenerateKeyFromUserId(userId);

                var encryptedBytes = Convert.FromBase64String(encryptedCode);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV (first 16 bytes for AES)
                var iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.IV = iv;

                // Extract encrypted data (after IV)
                var data = new byte[encryptedBytes.Length - 16];
                Array.Copy(encryptedBytes, 16, data, 0, data.Length);

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(data, 0, data.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Decryption failed for user {UserId}. Encrypted code length: {Length}", userId, encryptedCode?.Length ?? 0);
                throw;
            }
        }

        private byte[] GenerateKeyFromUserId(string userId)
        {
            try
            {
                // Create a more robust key derivation using salt
                var salt = Encoding.UTF8.GetBytes("verification_salt_2024");
                var userIdBytes = Encoding.UTF8.GetBytes(userId);

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_encryptionKey));
                var key = hmac.ComputeHash(userIdBytes);
                return key.Take(32).ToArray(); // 256-bit key
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Key generation failed for user {UserId}", userId);
                throw;
            }
        }

        private async Task SendSecureEmailAsync(string userEmail, string plainCode, string userId)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Your App", _configuration["SmtpSettings:From"]));
                email.To.Add(new MailboxAddress("", userEmail));
                email.Subject = "Secure Verification Code";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = GenerateSecureEmailBody(plainCode, userId);
                email.Body = bodyBuilder.ToMessageBody();

                // Add security headers
                email.Headers.Add("X-Content-Type-Options", "nosniff");
                email.Headers.Add("X-Frame-Options", "DENY");
                email.Headers.Add("X-XSS-Protection", "1; mode=block");

                using var client = new SmtpClient();

                await client.ConnectAsync(
                    _configuration["SmtpSettings:Host"],
                    int.Parse(_configuration["SmtpSettings:Port"]!),
                    MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);

                await client.AuthenticateAsync(
                    _configuration["SmtpSettings:Username"],
                    _configuration["SmtpSettings:Password"]);

                await client.SendAsync(email);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Verification code sent securely to {UserEmail}", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send secure email to {UserEmail}", userEmail);
                throw;
            }
        }

        private string GenerateSecureEmailBody(string code, string userId)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Secure Verification</title>
                <style>
                    body {{ font-family: Arial, sans-serif; background: #f5f5f5; padding: 20px; }}
                    .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                    .code {{ font-size: 32px; font-weight: bold; color: #007bff; text-align: center; letter-spacing: 5px; padding: 20px; background: #f8f9fa; border-radius: 8px; margin: 20px 0; }}
                    .warning {{ color: #dc3545; font-size: 14px; margin-top: 20px; }}
                    .footer {{ font-size: 12px; color: #666; margin-top: 30px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Secure Verification Required</h2>
                    <p>Hello,</p>
                    <p>Your verification code is:</p>
                    <div class='code'>{code}</div>
                    <p>This code will expire in 10 minutes.</p>
                    <p class='warning'>⚠️ This code is for your eyes only. Do not share it with anyone.</p>
                    <p class='footer'>
                        Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}
                    </p>
                </div>
            </body>
            </html>";
        }
    }

    public class VerificationResult
    {
        public required string EncryptedCode { get; set; }
        public required string Code { get; set; }
        public DateTime Expiry { get; set; }
        public string? RecordId { get; set; } // Add this property to fix CS0117
    }
}
