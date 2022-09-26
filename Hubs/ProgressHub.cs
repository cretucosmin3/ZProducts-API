using Microsoft.AspNetCore.SignalR;

namespace ProductAPI.SinglarRHubs;

public class ProgressHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
