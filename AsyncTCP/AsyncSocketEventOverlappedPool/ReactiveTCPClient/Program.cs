using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Reactive.Subjects;

namespace ReactiveTCPClient
{
    class Program
    {
        public static async Task ReadLoop(Stream stream)
        {
            var buffer = new byte[4096];
            var count = -1;
            while (count != 0)
            {
                count = await stream.ReadAsync(buffer, 0, buffer.Length);

                //Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, count) +"->Responsed");
                _packetReceived++;
            }
        }

        static int _packetReceived;
        static int _count;
        static int _fail;
        static IDisposable sub;
        static Subject<long> timer;
        static void Main(string[] args)
        {
            if (args.Length!=2)
            {
                Console.WriteLine("params: [Server] [Port]");
                Console.WriteLine("Press any key...");
                Console.ReadLine();
                return;
            }
            var server = args[0];
            var port = int.Parse(args[1]);

            timer = new Subject<long>();
            Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)).Subscribe(timer);
            string input;

            if (args.Length > 0)
            {
                input = args[0];
            }
            Console.WriteLine("Input count:");
            input = Console.ReadLine();
            int count = 0;
            while (!(Int32.TryParse(input, out count) && count > 0))
            {
                Console.WriteLine("Input error, reinput count :");
                input = Console.ReadLine();
            }
            sub = timer.Subscribe(_ => Console.WriteLine("PacketReceived{0},Connections{1},Fail{2}", _packetReceived, _count, _fail));
            //try
            //{

            var pid = Process.GetCurrentProcess().Id;
            var returnList = Enumerable.Range(0, count)

                         .Select(clientindex =>
                         {
                             try
                             {
                                 _count++;


                                 TcpClient tc = new TcpClient(server, port);
                                 var stream = tc.GetStream();
                                 var t = ReadLoop(stream);
                                 //t.Start();
                                 return new
                                 {

                                     SendSequence = Observable.Timer(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(25))
                                        .Select(
                                            seqindex =>
                                                new { TcpClient = tc, StreamWriter = new StreamWriter(stream), ClientId = clientindex, SequenceIndex = seqindex })

                                        .Subscribe(
                                            item =>
                                            {
                                                var message = string.Format("Client PID{0} Client{1} Squence{2}", pid, item.ClientId, item.SequenceIndex);
                                                item.StreamWriter.WriteLine(message + "|Send");
                                                item.StreamWriter.Flush();
                                            }



                                        ),
                                     Loop = t


                                 };
                             }
                             catch (Exception)
                             {

                                 _fail++;
                             }

                             return null;

                         })

                         .ToList();




            //}
            //catch (Exception)
            //{

            //    throw;
            //}

            Console.ReadLine();
        }
    }
}
