using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Profinet.Melsec;
using BingLibrary.hjb.file;
using BingLibrary.hjb.tools;
using System.Diagnostics;

namespace JasperUI.Model
{
    public class Fx5u
    {
        public event EventHandler<bool> ConnectStateChanged;
        private bool mConnect = false;
        private bool _Connect
        {
            get { return mConnect; }
            set
            {
                if (mConnect != value)
                {
                    mConnect = value;
                    ConnectStateChanged?.Invoke(null, mConnect);
                }
            }
        }
        public bool Connect { get { return mConnect; } }
        string IP;
        public MelsecMcNet melsec_net = null;
        //private string iniParameterPath = System.Environment.CurrentDirectory + "\\Parameter.ini";
        public bool[] FX5UOUT;
        public bool[] FX5UIN;
        public long SWms = 0;
        public Fx5u(string ip,int port)
        {
            IP = ip;
            melsec_net = new MelsecMcNet(ip, port);
            melsec_net.ConnectTimeOut = 2000;
            melsec_net.ReceiveTimeOut = 200;
            OperateResult connect = melsec_net.ConnectServer();
            FX5UOUT = new bool[64];
            if (connect.IsSuccess)
            {
                _Connect = true;
                Console.WriteLine("连接成功");
            }
            else
            {
                _Connect = false;
                Console.WriteLine("连接失败");
            }
            //Async.RunFuncAsync(Run, null);
        }
        /// <summary>
        /// 读双字
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public int ReadW(string address)
        {
            OperateResult<byte[]> read = melsec_net.Read(address, 1);
            if (read.IsSuccess)
            {
                _Connect = true;
            }
            else
            {
                _Connect = false;
            }
            if (read.Content.Length >= 2)
            {
                return (read.Content[1] << 8) + read.Content[0];
            }
            else
            {
                return -1;
            }
        }
        /// <summary>
        /// 读多个双字
        /// </summary>
        /// <param name="address"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public int[] ReadMultiW(string address,ushort length)
        {
            OperateResult<Int32[]> read = melsec_net.ReadInt32(address, length);
            if(read.IsSuccess)
            {
                _Connect = true;
                return read.Content;
            }
            else
            {
                _Connect = false;
                return null;
            }
        }
        public bool ReadM(string address)
        {
            OperateResult<bool> read = melsec_net.ReadBool(address);
            if (read.IsSuccess)
            {
                _Connect = true;
                return read.Content;
            }
            else
            {
                _Connect = false;
                return false;
            }
        }
        public bool[] ReadMultiM(string address, ushort length)
        {
            OperateResult<bool[]> read = melsec_net.ReadBool(address, length);
            if (read.IsSuccess)
            {
                _Connect = true;
                return read.Content;
            }
            else
            {
                _Connect = false;
                return null;
            }
        }
        public void SetM(string address,bool value)
        {
            OperateResult write = melsec_net.Write(address, value);
            if (write.IsSuccess)
            {
                _Connect = true;
            }
            else
            {
                _Connect = false;
            }
        }
        public void SetMultiM(string address, bool[] value)
        {
            OperateResult write = melsec_net.Write(address, value);
            if (write.IsSuccess)
            {
                _Connect = true;
            }
            else
            {
                _Connect = false;
            }
        }
        /// <summary>
        /// 写单字
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        public void WriteD(string address, short value)
        {
            OperateResult write = melsec_net.Write(address, value);
            if (write.IsSuccess)
            {
                _Connect = true;
            }
            else
            {
                _Connect = false;
            }
        }
        /// <summary>
        /// 写双字
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        public void WriteW(string address, int value)
        {
            OperateResult write = melsec_net.Write(address, value);
            if (write.IsSuccess)
            {
                _Connect = true;
            }
            else
            {
                _Connect = false;
            }
        }
        /// <summary>
        /// 写多个单字
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        public void WriteMultD(string address, short[] value)
        {
            OperateResult write = melsec_net.Write(address, value);
            if (write.IsSuccess)
            {
                _Connect = true;
            }
            else
            {
                _Connect = false;
            }
        }
        private void Run()
        {
            Stopwatch sw = new Stopwatch();
            while (true)
            {
                sw.Restart();
                OperateResult<bool[]> read = melsec_net.ReadBool("M2000", 100);
                if (read.IsSuccess)
                {
                    _Connect = true;
                    FX5UIN = read.Content;
                    OperateResult write = melsec_net.Write("M2500", FX5UOUT);
                    if (write.IsSuccess)
                    {
                        _Connect = true;
                    }
                    else
                    {
                        _Connect = false;
                    }
                }
                else
                {
                    _Connect = false;
                }
                System.Threading.Thread.Sleep(20);
                SWms = sw.ElapsedMilliseconds;
            }
        }
    }
}
