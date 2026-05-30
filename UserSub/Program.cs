using ChatociCupidSNUS.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Sub
{
    internal class Program
    {
        private static readonly ConcurrentQueue<Letter> _letterQueue = new();
        private static bool _isViewingLetter = false;

        static async Task Main(string[] args)
        {
            bool registered = false;
            var connection = new HubConnectionBuilder().WithUrl("https://localhost:7095/cupidonMessageHub").Build();
            string username = "";

            connection.On<Letter>("LetterArrived", (letter) =>
            {
                _letterQueue.Enqueue(letter);
            });

            connection.On<string>("Registered", msg => Console.WriteLine($"[INFO] {msg}"));
            connection.On<string>("Blocked", msg => Console.WriteLine($"[INFO] {msg}"));
            connection.On<string>("Error", msg => Console.WriteLine($"[ERROR] {msg}"));

            await connection.StartAsync();

            while (!registered)
            {
                Console.WriteLine("Enter username: ");
                username = Console.ReadLine()?.Trim() ?? "";
                bool hasNonEnglishAlphabet = username.Any(c => !((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')));
                if (hasNonEnglishAlphabet)
                {
                    Console.WriteLine("Username must contain only English alphabet characters. Please try again.");
                    continue;
                }
                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Username cannot be empty. Please try again.");
                    continue;
                }

                Console.WriteLine("Enter city: ");
                string city = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(city))
                {
                    Console.WriteLine("City cannot be empty. Please try again.");
                    continue;
                }

                Console.WriteLine("Enter age: ");
                string ageInput = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(ageInput))
                {
                    Console.WriteLine("Age cannot be empty. Please try again.");
                    continue;
                }
                if (!int.TryParse(ageInput, out int age))
                {
                    Console.WriteLine("Age must be a valid number. Please try again.");
                    continue;
                }
                Console.WriteLine("Enter phone number: ");
                string phoneNumber = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    Console.WriteLine("Phone cant be empty. Try again");
                    continue;
                }

                if (!IsValidPhoneNumber(phoneNumber))
                {
                    Console.WriteLine("Must be valid phone number");
                    continue;
                }

                Person singlePerson = new Person(username, age, city, phoneNumber);
                await connection.InvokeAsync("InitSinglePerson", singlePerson);

                Console.WriteLine("[SUB] Successfully subscribed to MessageEvent");
                registered = true;
            }

            Console.WriteLine("\nCommands: /block <username> | /exit");
            Console.WriteLine("If a letter pops up, press Enter with no text to confirm reading it.\n");

            while (true)
            {

                string input = "nah";

                if (!_isViewingLetter && _letterQueue.TryDequeue(out var letter))
                {
                    _isViewingLetter = true;
                    Console.WriteLine("\nNEW LETTER");
                    Console.WriteLine($"From: {letter.FromUsername}");
                    Console.WriteLine($"City: {letter.FromCity}");
                    Console.WriteLine($"Age: {letter.FromAge}");
                    Console.WriteLine($"Phone: {letter.FromPhone}");
                    Console.WriteLine($"Message: \"{letter.Message}\"");
                    Console.WriteLine("Press ENTER to confirm, /block <username> | /exit ");
                    input = Console.ReadLine()?.Trim() ?? "";
                }

                if (_isViewingLetter && !"nah".Equals(input))
                {
                    await connection.InvokeAsync("ConfirmRead", username);
                    _isViewingLetter = false;
                    Console.WriteLine("Letter read confirmed.");
                }

                if (input.StartsWith("/block "))
                {
                    string target = input[7..].Trim();
                    if (!string.IsNullOrEmpty(target))
                        await connection.InvokeAsync("BlockUser", username, target);
                    else
                        Console.WriteLine("Usage: /block <username>");
                }
                else if (input == "/exit")
                {
                    await connection.StopAsync();
                    break;
                }
            }
        }

        static bool IsValidPhoneNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number)) return false;
            string pattern = @"^\+?[0-9]+$";
            return Regex.IsMatch(number, pattern);
        }
    }
}