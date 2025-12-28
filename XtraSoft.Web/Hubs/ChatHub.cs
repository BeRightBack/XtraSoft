using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace XtraSoft.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        public ChatHub(ILogger<ChatHub> logger, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }
        public async Task SendMessage(string message)
        {
            var userName = Context.User?.Identity?.Name ?? "Unknown";
            _logger.LogInformation("User {User} sent message: {Message}", userName, message);
            await Clients.All.SendAsync("ReceiveMessage", userName, message);
        }
    }
}
