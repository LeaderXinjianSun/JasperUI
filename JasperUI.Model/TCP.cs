using BingLibrary.hjb.tools;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BingLibrary.hjb.net
{
    public class TcpIpClient
    {
        private TcpClient client = new TcpClient();
        private NetworkStream stream;
        public bool tcpConnected { set; get; }

        public async Task<bool> Connect(string ip, int port)
        {
            bool r = false;

            Task taskDelay = Task.Delay(6000);
            var completedTask = await Task.WhenAny(((Func<Task>)(() =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        client.SendTimeout = 600;
                        IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);
                        client.Connect(ipe);
                        tcpConnected = true;
                        r = true;
                    }
                    catch {; client.Close(); client = new TcpClient(); tcpConnected = false; r = false; }
                });
            }))(), taskDelay);
            if (completedTask == taskDelay)
            {
                client.Close(); client = new TcpClient();
                r = false;
            }

            return r;
        }

        public void IsOnline()
        {
            try
            {
                tcpConnected = !((client.Client.Poll(1000, SelectMode.SelectRead) && (client.Client.Available == 0)) || !client.Client.Connected);
            }
            catch { tcpConnected = false; }
        }

        public async Task<string> ReceiveAsync()
        {
            string tempS = "error";
            try
            {
                IsOnline();
                byte[] data = new Byte[256];
                string responseData = string.Empty;
                stream = client.GetStream();
                stream.ReadTimeout = 200;
                //Int32 bytes = stream.Read(data, 0, data.Length);
                Int32 bytes = await stream.ReadAsync(data, 0, data.Length);
                tempS = System.Text.Encoding.GetEncoding("GBK").GetString(data, 0, bytes);
                return tempS;
            }
            catch
            {
                return "error";
            }
        }

        public async Task<string> SendAsync(string message)
        {
            try
            {
                stream = client.GetStream();
                await stream.WriteAsync(Encoding.ASCII.GetBytes(message + "\r\n"), 0, Encoding.ASCII.GetBytes(message + "\r\n").Length);

                return "";
            }
            catch { tcpConnected = false; return "error"; }
        }

        public async Task<string> SendAndReceive(string msg)
        {
            string tempS = "error";
            byte[] data = new Byte[256];
            string responseData = string.Empty;
            try
            {
                stream = client.GetStream();
                stream.ReadTimeout = 200;
                await stream.WriteAsync(Encoding.ASCII.GetBytes(msg + "\r\n"), 0, Encoding.ASCII.GetBytes(msg + "\r\n").Length);
                await stream.FlushAsync();
                IsOnline();
                //Int32 bytes = stream.Read(data, 0, data.Length);
                Int32 bytes = await stream.ReadAsync(data, 0, data.Length);
                tempS = Encoding.ASCII.GetString(data, 0, bytes);
                return tempS;
            }
            catch
            {
                IsOnline();
                return "error";
            }
        }
    }

    public class TcpIpServer : TcpListener
    {
        public TcpIpServer(IPAddress LocalAddr, Int32 Port) : base(LocalAddr, Port)
        {
            Async.RunFuncAsync(ServerStart, null);
        }

        ~TcpIpServer()
        {
            Stop();
            Connected = false;
        }

        public bool Connected { set; get; }
        private TcpClient tcpClient = null;
        private NetworkStream stream;

        public async Task<string> SendAsync(string message)
        {
            try
            {
                stream = tcpClient.GetStream();
                await stream.WriteAsync(Encoding.ASCII.GetBytes(message + "\r\n"), 0, Encoding.ASCII.GetBytes(message + "\r\n").Length);
                return "";
            }
            catch { return "error"; }
        }

        public async Task<string> ReceiveAsync()
        {
            string tempS = "error";
            await ((Func<Task>)(() =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        byte[] data = new Byte[256];
                        string responseData = string.Empty;
                        stream = tcpClient.GetStream();
                        stream.ReadTimeout = 200;
                        Int32 bytes = stream.Read(data, 0, data.Length);
                        // Int32 bytes = await stream.ReadAsync(data, 0, data.Length);
                        tempS = Encoding.ASCII.GetString(data, 0, bytes);
                        return tempS;
                    }
                    catch
                    {
                        return "error";
                    }
                });
            }))();
            return tempS;
        }

        private void ServerStart()
        {
            Start();

            try
            {
                tcpClient = AcceptTcpClient();
                Connected = tcpClient.Connected;
            }
            catch { }
        }

        public new void Stop()
        {
            if (Active)
            {
                base.Stop();
                tcpClient = null;
                Connected = false;
            }
        }
    }
}