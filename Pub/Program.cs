using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pub
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var connection = new HubConnectionBuilder().WithUrl("https://localhost:7095/cupidonMessageHub").Build();

            await connection.StartAsync();
            Console.WriteLine("[CUPID] Sending messages every minute");

            while (true)
            {
                await connection.InvokeAsync("TriggerCupidRound");
                Console.WriteLine($"[CUPID] Round triggered: {DateTime.Now:HH:mm:ss}");
                await Task.Delay(60000);
            }

        }
    }
}
