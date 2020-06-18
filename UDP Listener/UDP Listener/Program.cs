using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UDP_Listener
{
    class Program
    {
        static int localPort;

        static string remoteAddress;
        static int remotePort;

        static Socket listeningSocket;

        static async Task Main(string[] args)
        {
            localPort = 7777;

            remoteAddress = "37.17.16.42";
            //remoteAddress = "192.168.0.107";
            remotePort = 7777;

            //Console.Write("Введите порт для приема сообщений: ");
            //localPort = Int32.Parse(Console.ReadLine());
            //Console.Write("Введите порт для отправки сообщений: ");
            //remotePort = Int32.Parse(Console.ReadLine());
            //Console.WriteLine("Для отправки сообщений введите сообщение и нажмите Enter");
            //Console.WriteLine();


            try
            {
                listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                //Прослушиваем по адресу
                IPEndPoint localIP = new IPEndPoint(IPAddress.Any, localPort);
                listeningSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listeningSocket.Bind(localIP);

                Task listeningTask = new Task(Listen);
                listeningTask.Start();

                //await listeningTask;

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint bindEndPoint = new IPEndPoint(IPAddress.Any, 7777);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Bind(bindEndPoint);

                while (true)
                {
                    string message = Console.ReadLine();



                    byte[] data = Encoding.Unicode.GetBytes(message);

                    socket.SendTo(data, new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort));


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Close();
            }




            //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //IPEndPoint localIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5555);
            //socket.Bind(localIP);

            //byte[] data = new byte[4096];

            //EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
            //int bytes = socket.ReceiveFrom(data, ref remoteIp);
        }

        private static void Listen()
        {
            try
            {

                while (true)
                {
                    // получаем сообщение
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0; // количество полученных байтов
                    byte[] data = new byte[4096]; // буфер для получаемых данных

                    //адрес, с которого пришли данные
                    EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);

                    do
                    {
                        bytes = listeningSocket.ReceiveFrom(data, ref remoteIp);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (listeningSocket.Available > 0);
                    // получаем данные о подключении
                    IPEndPoint remoteFullIp = remoteIp as IPEndPoint;

                    // выводим сообщение
                    Console.WriteLine("{0}:{1} - {2}", remoteFullIp.Address.ToString(),
                                                    remoteFullIp.Port, builder.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Close();
            }
        }
        // закрытие сокета
        private static void Close()
        {
            if (listeningSocket != null)
            {
                listeningSocket.Shutdown(SocketShutdown.Both);
                listeningSocket.Close();
                listeningSocket = null;
            }
        }
    }
}
