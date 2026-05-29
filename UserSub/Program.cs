using ChatociCupidSNUS.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.RegularExpressions;

namespace Sub
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            bool registered = false;
            var connection = new HubConnectionBuilder().WithUrl("https://localhost:7095/cupidonMessageHub").Build();
            await connection.StartAsync();
            string username = "";


            while (!registered)
                {
                    Console.WriteLine("Enter username: ");
                    username = Console.ReadLine();
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
                    string city = Console.ReadLine();
                    if (string.IsNullOrEmpty(city))
                    {
                        Console.WriteLine("City cannot be empty. Please try again.");
                        continue;
                    }

                    Console.WriteLine("Enter age: ");
                    string ageInput = Console.ReadLine();
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
                    string phoneNumber = Console.ReadLine();
                    if (string.IsNullOrEmpty(phoneNumber))
                    {
                        Console.WriteLine("Phone cant be empty. Try again");
                        continue;
                    }

                    if (!IsValidPhoneNumber(phoneNumber))
                    {
                        Console.WriteLine("Must be valid phone number.");
                        continue;
                    }

                    Person singlePerson = new Person(username, age, city, phoneNumber);
                    await connection.InvokeAsync("InitSinglePerson", singlePerson);

                    Console.WriteLine("[SUB] Successfully subscribed to MessageEvent");
                    registered = true;

                }

            connection.On<Letter>("LetterArrived", async (letter) =>
            {
                Console.WriteLine("NEW LETTER");
                Console.WriteLine($"From: {letter.FromUsername}");
                Console.WriteLine($"City: {letter.FromCity}");
                Console.WriteLine($"Age: {letter.FromAge}");
                Console.WriteLine($"Phone: {letter.FromPhone}");
                Console.WriteLine($"Message: \"{letter.Message}\"");
                Console.WriteLine("Press enter to confirm");

                Console.ReadLine();
                await connection.InvokeAsync("ConfirmRead", username);
            });

            connection.On<string>("Registered", msg => Console.WriteLine($"[INFO] {msg}"));
            connection.On<string>("Blocked", msg => Console.WriteLine($"[INFO] {msg}"));
            connection.On<string>("Error", msg => Console.WriteLine($"[ERROR] {msg}"));

            Console.WriteLine("Commands: /block <username> | /exit");
            while (true)
            {
                var input = Console.ReadLine().Trim() ?? "";

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
