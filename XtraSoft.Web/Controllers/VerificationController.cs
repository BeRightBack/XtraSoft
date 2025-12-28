using Microsoft.AspNetCore.Mvc;
using XtraSoft.Services;

namespace XtraSoft.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerificationController : ControllerBase
    {
        private readonly SecureVerificationService _verificationService;

        public VerificationController(SecureVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendVerification([FromBody] SendVerificationRequest request)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                var userAgent = Request.Headers["User-Agent"].ToString();

                var result = await _verificationService.SendVerificationCodeAsync(
                    request.Email,
                    request.UserId,
                    ipAddress,
                    userAgent);

                return Ok(new
                {
                    Success = true,
                    RecordId = result.RecordId,
                    Expiry = result.Expiry,
                    Message = "Verification code sent securely"
                });
            }
            catch (Exception )
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Failed to send verification code"
                });
            }
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var userAgent = Request.Headers["User-Agent"].ToString();

            bool isValid = await _verificationService.VerifyCodeAsync(
                request.UserId,
                request.Code,
                ipAddress,
                userAgent);

            return Ok(new
            {
                Success = isValid,
                Valid = isValid
            });
        }
    }

    public class SendVerificationRequest
    {
        public required string Email { get; set; }
        public required string UserId { get; set; }
    }

    public class VerifyCodeRequest
    {
        public required string Email { get; set; }
        public required string UserId { get; set; }
        public required string Code { get; set; }
    }
}
