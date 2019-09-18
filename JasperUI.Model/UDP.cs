using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;

namespace BingLibrary.Net.net
{
    public class UDPClient
    {
        public UdpClient udp;

        //public string IPAddress = "LocalHost";
        public IPEndPoint RemoteIpEndPoint;

        public bool Connect(int localPort, int targetPort)
        {
            try
            {
                udp = new UdpClient(localPort);
                udp.Client.SendTimeout = 1000;
                udp.Client.ReceiveTimeout = 1000;
                RemoteIpEndPoint = new IPEndPoint(System.Net.IPAddress.Loopback, targetPort);
                return true;
            }
            catch { return false; }
        }

        public bool Connect(int localPort, int targetPort, string targetIP)
        {
            try
            {
                udp = new UdpClient(localPort);
                udp.Client.SendTimeout = 1000;
                udp.Client.ReceiveTimeout = 1000;
                RemoteIpEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
                return true;
            }
            catch { return false; }
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
                        //UdpReceiveResult x = await udp.ReceiveAsync();
                        //tempS = Encoding.UTF8.GetString(x.Buffer);
                        tempS = Encoding.Default.GetString(udp.Receive(ref RemoteIpEndPoint));
                    }
                    catch
                    { tempS = "error"; }
                });
            }))();


            return tempS;
        }

        public async Task<bool> SendAsync(string mStrToSend, bool wait = true)
        {
            try
            {
                while (udp.Available > 0)
                {
                    await udp.ReceiveAsync();
                }

                byte[] ByteToSend = System.Text.Encoding.Default.GetBytes(mStrToSend);
                udp.Send(ByteToSend, ByteToSend.Length, RemoteIpEndPoint);
                return true;
            }
            catch { return false; }
        }
    }
}