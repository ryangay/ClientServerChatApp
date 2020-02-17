using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        private const int PORT = 11000;

        static void Main(string[] args)
        {
            var addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToList();
            for (var i = 0; i < addresses.Count; i++)
            {
                var ipAddressinList = addresses[i];
                Console.WriteLine($"[{i}] {ipAddressinList}");
            }

            bool gotAddr = false;
            int num = 0;
            while (!gotAddr)
            {
                Console.WriteLine("Choose Address: ");
                var key = Console.ReadKey(true).KeyChar;
                if (int.TryParse(key.ToString(), out num) && num < addresses.Count)
                    gotAddr = true;
                else
                    Console.WriteLine($"Address number invalid. Please try again.{Environment.NewLine}");
            }
            int portNo = PORT;
            bool gotPort = false;

            while (!gotPort)
            {
                Console.WriteLine();
                Console.Write("Enter port number [11000]: ");
                var port = Console.ReadLine();
                if (string.IsNullOrEmpty(port)) gotPort = true;
                else if (int.TryParse(port, out portNo)) gotPort = true;
                else Console.WriteLine($"Port number contains invlid data. Please try again{Environment.NewLine}");
            }
            using (var server = new Server(addresses[num], portNo))
            {
                server.Start();
            }
        }
    }
}
