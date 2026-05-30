using ChatociCupidSNUS.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ChatociCupidSNUS
{

    public class CupidonMessageHub : Hub
    {
        private static readonly ConcurrentDictionary<string, UserSession> _sessions = new();
        private static readonly ConcurrentDictionary<string, string> _connectionToUserMap = new();

        private static readonly string[] _cupidMessages = {
            "Excited to see you",
            "I wanna meet you",
            "Im not interested"
        };

        public async Task InitSinglePerson(Person person)
        {
            var session = new UserSession
            {
                Profile = person,
                ConnectionId = Context.ConnectionId,
                HasPending = false
            };

            if (!_sessions.TryAdd(person.Username, session))
            {
                await Clients.Caller.SendAsync("Error", $"Username '{person.Username}' already exists");
                Console.WriteLine($"[SERVER] username {person.Username} already exist");
                return;
            }

            _connectionToUserMap[Context.ConnectionId] = person.Username;

            Console.WriteLine($"[SERVER] '{person.Username}' registered");
            await Clients.Caller.SendAsync("Registered", "Successfully registered");
        }

        public async Task TriggerCupidRound()
        {
            Console.WriteLine("[SERVER] Started sending messages");

            var activeSessions = _sessions.Values.ToList();
            if (activeSessions.Count < 2)
            {
                Console.WriteLine("[SERVER] Need more pepol");
                return;
            }

            foreach (var senderSession in activeSessions)
            {
                var matchSession = FindBestMatch(senderSession, activeSessions);
                if (matchSession == null) continue;

                if (matchSession.HasPending)
                {
                    Console.WriteLine($"[SERVER] '{matchSession.Profile.Username}' has unread letter");
                    continue;
                }

                if (matchSession.BlockedUsers.ContainsKey(senderSession.Profile.Username))
                {
                    Console.WriteLine($"[SERVER] '{matchSession.Profile.Username}' blocked '{senderSession.Profile.Username}'");
                    continue;
                }

                if (senderSession.BlockedUsers.ContainsKey(matchSession.Profile.Username))
                {
                    Console.WriteLine($"[SERVER] '{senderSession.Profile.Username}' blocked '{matchSession.Profile.Username}'");
                    continue;
                }

                matchSession.HasPending = true;

                int msgIndex = CryptoRandInt(0, _cupidMessages.Length);
                string message = _cupidMessages[msgIndex];
                bool disinterested = message == "Im not interested";

                var letter = new Letter
                {
                    FromUsername = senderSession.Profile.Username,
                    FromCity = senderSession.Profile.City,
                    FromAge = senderSession.Profile.Age,
                    FromPhone = disinterested ? "hidden" : senderSession.Profile.PhoneNumber,
                    Message = message
                };

                await Clients.Client(matchSession.ConnectionId).SendAsync("LetterArrived", letter);
                Console.WriteLine($"[SERVER] Letter: '{senderSession.Profile.Username}' to '{matchSession.Profile.Username}' | \"{message}\"");
            }
        }

        public Task ConfirmRead(string username)
        {
            if (_sessions.TryGetValue(username, out var session))
            {
                session.HasPending = false;
                Console.WriteLine($"[SERVER] '{username}' confirmed letter");
            }
            return Task.CompletedTask;
        }

        public async Task BlockUser(string requester, string target)
        {
            if (!_sessions.ContainsKey(target))
            {
                await Clients.Caller.SendAsync("Error", $"User '{target}' doesnt exist.");
                return;
            }

            if (_sessions.TryGetValue(requester, out var session))
            {
                session.BlockedUsers.TryAdd(target, 0);
                await Clients.Caller.SendAsync("Blocked", $"'{target}' was blocked");
                Console.WriteLine($"[SERVER] '{requester}' blocked '{target}'.");
            }
        }

        private static UserSession? FindBestMatch(UserSession sender, List<UserSession> all)
        {
            UserSession? best = null;
            int bestScore = -1;

            foreach (var candidate in all)
            {
                if (candidate.Profile.Username == sender.Profile.Username) continue;

                int score = 0;

                if (string.Equals(candidate.Profile.City, sender.Profile.City, StringComparison.OrdinalIgnoreCase))
                    score += 30;

                if (Math.Abs(candidate.Profile.Age - sender.Profile.Age) <= 2)
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
            return RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionToUserMap.TryRemove(Context.ConnectionId, out var username))
            {
                _sessions.TryRemove(username, out _);
                Console.WriteLine($"[SERVER] '{username}' disconnected.");
            }
            return base.OnDisconnectedAsync(exception);
        }

        private static int CryptoRandIntObs(int minInclusive, int maxExclusive)
        {
            using var rng = new RNGCryptoServiceProvider();

            var bytes = new byte[4];
            rng.GetBytes(bytes);
            uint value = BitConverter.ToUInt32(bytes, 0);
            uint range = (uint)(maxExclusive - minInclusive);

            return minInclusive + (int)(value % range);
        }
    }
}