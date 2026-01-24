using Microsoft.AspNetCore.SignalR;

namespace LastCallMotorAuctions.API.Hubs
{
    public class BiddingHub : Hub
    {
        //Method for clients to join an auction room
        public async Task JoinAuctionRoom(string auctionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
            await Clients.Group($"auction-{auctionId}").SendAsync("UserJoined", Context.ConnectionId);
        }

        // Method for clients to leave an auction room
        public async Task LeaveAuctionRoom(string auctionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
            await Clients.Group($"auction-{auctionId}").SendAsync("UserLeft", Context.ConnectionId);
        }

        // Method to broadcast a new bid to all clients in an auction room
        public async Task BroadcastBid(string auctionId, object binData)
        {
            await Clients.Group($"auction-{auctionId}").SendAsync("NewBid", binData);
        }

        // Override OnDisconnectedAsync to clean up when clients disconnect
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

}
