using ChatociCupidSNUS.Models;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;

namespace ChatociCupidSNUS
{
    public class CupidonMessageHub : Hub
    {
        private static readonly Dictionary<string, Person> _subs = new();
        private static readonly Dictionary<string, string> _connections = new();
        private static readonly Dictionary<string, HashSet<string>> _blocked = new();
        private static readonly Dictionary<string, bool> _hasPending = new();

        private static readonly string[] _cupidMessages = {
            "Excited to see you",
            "I wanna meet you",
            "Im not interested"
        };

        public async Task InitSinglePerson(Person person)
        {
            if (_subs.ContainsKey(person.Username))
            {
                await Clients.Caller.SendAsync("Error", $"Username '{person.Username}' already exists.");
                Console.WriteLine($"[SERVER] username {person.Username} already exists!");
                return;
            }

            _subs[person.Username] = person;
            _connections[person.Username] = Context.ConnectionId;
            _blocked[person.Username] = new HashSet<string>();
            _hasPending[person.Username] = false;

            Console.WriteLine($"[SERVER] '{person.Username}' registered");
            await Clients.Caller.SendAsync("Registered", $"Successfully registered");
        }

        public async Task TriggerCupidRound()
        {
            Console.WriteLine($"[SERVER] Started sending messagse");

            var people = _subs.Values.ToList();
            if (people.Count < 2)
            {
                Console.WriteLine("[SERVER] Need more pepol");
                return;
            }

            foreach (var sender in people)
            {
                var match = FindBestMatch(sender, people);
                if (match == null) continue;

                if (_hasPending.TryGetValue(match.Username, out bool pending) && pending)
                {
                    Console.WriteLine($"[SERVER] '{match.Username}' has unread letter");
                    continue;
                }

                if (_blocked.TryGetValue(match.Username, out var blockedSet)
                    && blockedSet.Contains(sender.Username))
                {
                    Console.WriteLine($"[SERVER] '{match.Username}' blocked '{sender.Username}'");
                    continue;
                }

                if (_blocked.TryGetValue(sender.Username, out var blockedSetSender)
                    && blockedSetSender.Contains(match.Username))
                {
                    Console.WriteLine($"[SERVER] '{sender.Username}' blocked '{match.Username}'");
                    continue;
                }

                int msgIndex = CryptoRandInt(0, _cupidMessages.Length);
                string message = _cupidMessages[msgIndex];
                bool disinterested = message == "Im not interested";

                var letter = new Letter
                {
                    FromUsername = sender.Username,
                    FromCity = sender.City,
                    FromAge = sender.Age,
                    FromPhone = disinterested ? "hidden" : sender.PhoneNumber,
                    Message = message
                };

                if (_connections.TryGetValue(match.Username, out var connId))
                {
                    _hasPending[match.Username] = true;
                    await Clients.Client(connId).SendAsync("LetterArrived", letter);
                    Console.WriteLine($"[SERVER] Letter: '{sender.Username}' to '{match.Username}' | \"{message}\"");
                }
            }
        }

        public Task ConfirmRead(string username)
        {
            if (_hasPending.ContainsKey(username))
                _hasPending[username] = false;

            Console.WriteLine($"[SERVER] '{username}' confirmed letter");
            return Task.CompletedTask;
        }

        public async Task BlockUser(string requester, string target)
        {
            if (!_subs.ContainsKey(target))
            {
                await Clients.Caller.SendAsync("Error", $"User '{target}' doesnt exist.");
                return;
            }

            _blocked[requester].Add(target);
            await Clients.Caller.SendAsync("Blocked", $"'{target}' was blokced.");
            Console.WriteLine($"[SERVER] '{requester}' blocked '{target}'.");
        }

        private static Person? FindBestMatch(Person sender, List<Person> all)
        {
            Person? best = null;
            int bestScore = -1;

            foreach (var candidate in all)
            {
                if (candidate.Username == sender.Username) continue;

                int score = 0;

                if (string.Equals(candidate.City, sender.City, StringComparison.OrdinalIgnoreCase))
                    score += 30;

                if (Math.Abs(candidate.Age - sender.Age) <= 2)
                    score += 20;

                score += CryptoRandInt(0, 101);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static int CryptoRandInt(int minInclusive, int maxExclusive)
        {
            using var rng = new RNGCryptoServiceProvider();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            uint value = BitConverter.ToUInt32(bytes, 0);
            return minInclusive + (int)(value % (uint)(maxExclusive - minInclusive));
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var entry = _connections.FirstOrDefault(kv => kv.Value == Context.ConnectionId);
            if (entry.Key != null)
            {
                _subs.Remove(entry.Key);
                _connections.Remove(entry.Key);
                _blocked.Remove(entry.Key);
                _hasPending.Remove(entry.Key);
                Console.WriteLine($"[SERVER] '{entry.Key}' disconnected.");
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}