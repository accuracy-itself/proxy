using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Proxy
{
    class Program
    {
        static IPEndPoint? IPEndPointLocal;
        static IPAddress? IPAddressLocal;
        const int LocalPort = 8005;
        const int BufferSize = 65536;
        static string URL;
        static void Main()
        {
            Console.WriteLine("hey, my dear proxy user!");

            IPAddressLocal = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(IPAddressLocal, LocalPort);
            try
            {
                listener.Start();
                while (true)
                {
                    //receiving requests from browser and creating threads for processing them
                    TcpClient handler = listener.AcceptTcpClient();
                    Thread ProcessRequestThread = new(() => ProcessRequest(handler));
                    ProcessRequestThread.Start();
                    Console.WriteLine("--------------------------------------------------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static byte[] GetPath(byte[] data)
        {
            string buffer = Encoding.UTF8.GetString(data);
            Regex headerRegex = new Regex(@"http:\/\/[a-z0-9а-яё\:\.]*");
            MatchCollection headers = headerRegex.Matches(buffer);
            buffer = buffer.Replace(headers[0].Value, "");
            data = Encoding.UTF8.GetBytes(buffer);
            return data;
        }

        public static void ProcessRequest(TcpClient handler)
        {
            NetworkStream browserStream = handler.GetStream();
            byte[] browserData = new byte[BufferSize];

            try
            {
                {
                        try
                        {
                            //read request and get server URL and port
                            int readbytes = browserStream.Read(browserData);
                            if (readbytes == 0)
                                return;

                            string bData = Encoding.ASCII.GetString(browserData, 0, readbytes);
                            string[] data = bData.Trim().Split(new char[] { '\r', '\n' });
                            string hostString = data.FirstOrDefault(s => s.Contains("Host"));

                            if (hostString == null)
                                return;

                            hostString = hostString.Substring(hostString.IndexOf(":") + 2);
                            string[] host = hostString.Trim().Split(':');
                            URL = data[0].Split()[1];
                            //Console.WriteLine("URL: " + URL);

                            int port;
                            if (host.Length == 2)
                                port = Convert.ToInt32(host[1]);
                            else
                                port = 80;

                            //create socket and stream for interacting with server
                            TcpClient serverSender = new TcpClient(host[0], port);

                            NetworkStream serverStream = serverSender.GetStream();

                            try
                            {
                                bData = Encoding.UTF8.GetString(browserData);
                                //Console.WriteLine("first" + bData);
                                Regex hostRegex = new Regex(@"http:\/\/[a-z0-9а-яё\:\.]*");
                                MatchCollection hosts = hostRegex.Matches(bData);
                                bData = bData.Replace(hosts[0].Value, "");
                                //Console.WriteLine("second:" + bData);
                                browserData = Encoding.UTF8.GetBytes(bData);
                                serverStream.Write(browserData, 0, readbytes);

                                //get response code and copy response to browser stream
                                byte[] responseBuffer = new byte[BufferSize];

                                int len = serverStream.Read(responseBuffer);
                                browserStream.Write(responseBuffer, 0, len);

                                string responseString = Encoding.UTF8.GetString(responseBuffer, 0, len);
                                string[] response = responseString.Trim().Split(new char[] { '\r', '\n' });

                                response = response[0].Trim().Split(' ');
                                int answerCode = Convert.ToInt32(response[1]);
                                Console.WriteLine(URL + '-' + answerCode + " " + response[2]);

                                serverStream.CopyTo(browserStream);
                                //serverStream.Close();
                            }
                            finally
                            {
                                //serverStream.Close();
                                serverSender.Close();
                            }
                        }
                        catch
                        {
                        }
                }
            }
            finally
            {
                handler.Close();
            }
        }
    }
}