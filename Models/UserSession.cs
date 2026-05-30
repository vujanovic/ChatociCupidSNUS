using System.Collections.Concurrent;

namespace ChatociCupidSNUS.Models
{
    public class UserSession
    {
        public Person Profile { get; set; } = null!;
        public string ConnectionId { get; set; } = null!;
        public bool HasPending { get; set; }
        public ConcurrentDictionary<string, byte> BlockedUsers { get; } = new();
    }
}