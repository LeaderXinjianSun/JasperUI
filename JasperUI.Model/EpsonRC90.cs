using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BingLibrary.hjb.net;
using BingLibrary.hjb.file;
using BingLibrary.hjb.tools;
using BingLibrary.Net.net;
using System.Data;
using ViewROI;
using BingLibrary.HVision;

namespace JasperUI.Model
{
    public class EpsonRC90
    {
        #region 变量
        public TcpIpClient CtrlNet = new TcpIpClient();
        public TcpIpClient IOReceiveNet = new TcpIpClient();
        public TcpIpClient TestSentNet = new TcpIpClient();
        public TcpIpClient TestReceiveNet = new TcpIpClient();
        public UDPClient udp = new UDPClient();
        string ip = "192.168.1.2";
        private bool isLogined = false;
        public bool[] Rc90In = new bool[100];
        public bool[] Rc90Out = new bool[100];
        public bool CtrlStatus = false, IOReceiveStatus = false, TestSendStatus = false, TestReceiveStatus = false;
        private string iniParameterPath = System.Environment.CurrentDirectory + "\\Parameter.ini";

        public int[,] BordIndexA = new int[12, 8];
        public int[,] BordIndexB = new int[12, 8];
        public int BordSW = 0;
        //条码 板条码 索引 产品状态 日期 时间
        public ProducInfo[] BarInfo = new ProducInfo[96];
        public string BordBarcode = "Null";
        
        #endregion
        #region 事件
        public delegate void PrintEventHandler(string ModelMessageStr);
        public event PrintEventHandler ModelPrint;
        public event PrintEventHandler EpsonStatusUpdate;
        #endregion
        #region 构造函数
        public EpsonRC90()
        {
            ip = Inifile.INIGetStringValue(iniParameterPath, "Epson", "EpsonIp", "192.168.0.30");
            string MCUIp = Inifile.INIGetStringValue(iniParameterPath, "MCU", "MCUIp", "192.168.0.130");
            udp.Connect(10999, 13800, MCUIp);
            for (int i = 0; i < 96; i++)
            {
                BarInfo[i] = new ProducInfo();
                BarInfo[i].Barcode = "FAIL";
                BarInfo[i].BordBarcode = "Null";
                BarInfo[i].Status = 0;
                BarInfo[i].TDate = DateTime.Now.ToString("yyyyMMdd");
                BarInfo[i].TTime = DateTime.Now.ToString("HHmmss");
            }
            Async.RunFuncAsync(Run, null);
        }
        #endregion
        #region 功能
        void Run()
        {
            checkCtrlNet();
            GetStatus();
            checkIOReceiveNet();
            IORevAnalysis();
            checkTestSentNet();
            checkTestReceiveNet();
            TestRevAnalysis();
        }
        public async void checkCtrlNet()
        {
            while (true)
            {

                if (!CtrlNet.tcpConnected)
                {
                    await Task.Delay(1000);
                    if (!CtrlNet.tcpConnected)
                    {
                        isLogined = false;
                        bool r1 = await CtrlNet.Connect(ip, 5000);
                        if (r1)
                        {
                            CtrlStatus = true;
                            ModelPrint("机械手CtrlNet连接");
                        }
                        else
                            CtrlStatus = false;
                    }
                }
                if (!isLogined && CtrlStatus)
                {
                    await CtrlNet.SendAsync("$login,123");
                    string s = await CtrlNet.ReceiveAsync();
                    if (s.Contains("#login,0"))
                        isLogined = true;
                    await Task.Delay(400);
                }
                else
                {
                    await Task.Delay(3000);
                }
            }
        }
        private async void GetStatus()
        {
            string status = "";
            while (true)
            {
                if (isLogined == true)
                {
                    try
                    {
                        status = await CtrlNet.SendAndReceive("$getstatus");
                        string[] statuss = status.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        if (statuss[0] == "#getstatus")
                        {
                            if (statuss[1].Length == 11)
                            {
                                EpsonStatusUpdate(statuss[1]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log.Default.Error("EpsonRC90.GetStatus", ex.Message);
                    }
                }
                await Task.Delay(1000);
            }
        }
        public async void checkIOReceiveNet()
        {
            while (true)
            {
                await Task.Delay(400);
                if (!IOReceiveNet.tcpConnected)
                {
                    await Task.Delay(1000);
                    if (!IOReceiveNet.tcpConnected)
                    {
                        bool r1 = await IOReceiveNet.Connect(ip, 2007);
                        if (r1)
                        {
                            IOReceiveStatus = true;
                            ModelPrint("机械手IOReceiveNet连接");

                        }
                        else
                            IOReceiveStatus = false;
                    }
                }
                else
                { await Task.Delay(15000); }
            }
        }
        private async void IORevAnalysis()
        {
            while (true)
            {
                //await Task.Delay(100);
                if (IOReceiveStatus == true)
                {
                    string s = await IOReceiveNet.ReceiveAsync();

                    string[] ss = s.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    try
                    {
                        s = ss[0];

                    }
                    catch
                    {
                        s = "error";
                    }

                    if (s == "error")
                    {
                        IOReceiveNet.tcpConnected = false;
                        IOReceiveStatus = false;
                        ModelPrint("机械手IOReceiveNet断开");
                    }
                    else
                    {
                        string[] strs = s.Split(',');
                        if (strs[0] == "IOCMD" && strs[1].Length == 100)
                        {
                            for (int i = 0; i < 100; i++)
                            {
                                Rc90Out[i] = strs[1][i] == '1' ? true : false;
                            }
                            string RsedStr = "";
                            for (int i = 0; i < 100; i++)
                            {
                                RsedStr += Rc90In[i] ? "1" : "0";
                            }
                            await IOReceiveNet.SendAsync(RsedStr);
                            //ModelPrint("IOSend " + RsedStr);
                            //await Task.Delay(1);
                        }
                        //ModelPrint("IORev: " + s);
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }
        public async void checkTestSentNet()
        {
            while (true)
            {
                await Task.Delay(400);
                if (!TestSentNet.tcpConnected)
                {
                    await Task.Delay(1000);
                    if (!TestSentNet.tcpConnected)
                    {
                        bool r1 = await TestSentNet.Connect(ip, 2000);
                        if (r1)
                        {
                            TestSendStatus = true;
                            ModelPrint("机械手TestSentNet连接");

                        }
                        else
                            TestSendStatus = false;
                    }
                }
                else
                { await Task.Delay(15000); }
            }
        }
        public async void checkTestReceiveNet()
        {
            while (true)
            {
                await Task.Delay(400);
                if (!TestReceiveNet.tcpConnected)
                {
                    await Task.Delay(1000);
                    if (!TestReceiveNet.tcpConnected)
                    {
                        bool r1 = await TestReceiveNet.Connect(ip, 2001);
                        if (r1)
                        {
                            TestReceiveStatus = true;
                            ModelPrint("机械手TestReceiveNet连接");

                        }
                        else
                            TestReceiveStatus = false;
                    }
                }
                else
                { await Task.Delay(15000); }
            }
        }
        private async void TestRevAnalysis()
        {
            while (true)
            {
                if (TestReceiveStatus == true)
                {
                    string s = await TestReceiveNet.ReceiveAsync();

                    string[] ss = s.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    try
                    {
                        s = ss[0];
                    }
                    catch
                    {
                        s = "error";
                    }

                    if (s == "error")
                    {
                        TestReceiveNet.tcpConnected = false;
                        TestReceiveStatus = false;
                        ModelPrint("机械手TestReceiveNet断开");
                    }
                    else
                    {
                        ModelPrint("TestRev: " + s);
                        try
                        {
                            string[] strs = s.Split(';');
                            switch (strs[0])
                            {
                                case "ScanBarcode":
                                    GlobalVars.GetImage();
                                    String[] Barcodes = GlobalVars.GetBarcode();
                                    if (strs.Length > 1)
                                    {
                                        BottomScanGetBarCodeCallback(Barcodes, strs);
                                    }
                                    else
                                    {
                                        string barcode;
                                        if (Barcodes[0] == "error")
                                        {
                                            barcode = Barcodes[1];
                                        }
                                        else
                                        {
                                            barcode = Barcodes[0];
                                        }
                                        BottomScanGetBarCodeCallback(barcode);
                                    }

                                    break;
                                case "TestStart":
                                    SendBarcode(int.Parse(strs[1]));
                                    break;
                                case "TestResult":
                                    if (strs.Length == 10)
                                    {
                                        SaveResult(strs);
                                    }
                                    break;
                                case "RobotGetFinish":
                                    GlobalVars.Fx5u.SetM("M2508", true);
                                    break;
                                default:
                                    ModelPrint("无效指令： " + s);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelPrint(ex.Message);
                        }
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }
        public async void BottomScanGetBarCodeCallback(string barcode)
        {
            //barcode = "G5Y936600AZP2CQ1S";
            ModelPrint(barcode);
            await Task.Run(async () =>
            {
                try
                {
                    Oracle oraDB = new Oracle("zdtbind", "sfcabar", "sfcabar*168");
                    if (oraDB.isConnect())
                    {
                        string sqlstr = "select * from sfcdata.barautbind where SCBARCODE = '" + barcode + "'";
                        DataSet ds = oraDB.executeQuery(sqlstr);
                        DataTable dt = ds.Tables[0];
                        if (dt.Rows.Count > 0)
                        {
                            int nowindex = int.Parse((string)dt.Rows[0]["PCSSER"]);

                            for (int i = 0; i < 96; i++)
                            {
                                if (BordIndexA[i / 8, i % 8] == nowindex)
                                {
                                    BordSW = 0;
                                    break;
                                }
                                if (BordIndexB[i / 8, i % 8] == nowindex)
                                {
                                    BordSW = 1;
                                    break;
                                }
                            }

                            string PNLBarcode = (string)dt.Rows[0]["SCPNLBAR"];
                            sqlstr = "select * from sfcdata.barautbind where SCPNLBAR = '" + PNLBarcode + "'";
                            ds = oraDB.executeQuery(sqlstr);
                            dt = ds.Tables[0];
                            if (dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < 96; i++)
                                {
                                    int pcser;
                                    if (BordSW == 0)
                                    {
                                        pcser = BordIndexA[i / 8, i % 8];
                                    }
                                    else
                                    {
                                        pcser = BordIndexB[i / 8, i % 8];
                                    }


                                    try
                                    {
                                        DataRow[] drs = dt.Select(string.Format("PCSSER = '{0}'", pcser.ToString()));
                                        if (drs.Length > 0)
                                        {
                                            DataRow dr = drs[0];

                                            bool isAoi = false;
                                            sqlstr = "select to_char(sfcdata.GETCK_posaoi_t1('" + (string)dr["SCBARCODE"] + "', 'A')) from dual";
                                            ds = oraDB.executeQuery(sqlstr);
                                            DataTable dt1 = ds.Tables[0];
                                            if ((string)dt1.Rows[0][0] == "0")
                                            {
                                                isAoi = true;
                                            }
                                            else
                                            {
                                                //sqlstr = "select to_char(sfcdata.GETCK_posaoi_t1('" + (string)dr["SCBARCODE"] + "', 'B')) from dual";
                                                //ds = oraDB.executeQuery(sqlstr);
                                                //dt1 = ds.Tables[0];
                                                //if ((string)dt1.Rows[0][0] == "0")
                                                //{
                                                //    isAoi = true;
                                                //}
                                            }
                                            //条码 板条码 产品状态 日期 时间
                                            BarInfo[i].Barcode = (string)dr["SCBARCODE"];
                                            BarInfo[i].BordBarcode = BordBarcode;
                                            BarInfo[i].Status = isAoi ? 1 : 0;
                                            BarInfo[i].TDate = DateTime.Now.ToString("yyyyMMdd");
                                            BarInfo[i].TTime = DateTime.Now.ToString("HHmmss");
                                        }
                                        else
                                        {
                                            BarInfo[i].Barcode = "FAIL";
                                            BarInfo[i].BordBarcode = BordBarcode;
                                            BarInfo[i].Status = 2;
                                            BarInfo[i].TDate = DateTime.Now.ToString("yyyyMMdd");
                                            BarInfo[i].TTime = DateTime.Now.ToString("HHmmss");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        BarInfo[i].Barcode = "FAIL";
                                        BarInfo[i].BordBarcode = BordBarcode;
                                        BarInfo[i].Status = 2;
                                        BarInfo[i].TDate = DateTime.Now.ToString("yyyyMMdd");
                                        BarInfo[i].TTime = DateTime.Now.ToString("HHmmss");
                                    }
                                }
                                string retstr = "";
                                string machinestr = Inifile.INIGetStringValue(iniParameterPath, "System", "MachineID", "Jasper01");
                                Mysql mysql = new Mysql();
                                if (mysql.Connect())
                                {
                                    for (int i = 0; i < 96; i++)
                                    {
                                        string stm = "INSERT INTO BARBIND (MACHINE,SCBARCODE,SCPNLBAR,SCBODBAR,SDATE,STIME,PCSSER,RESULT) VALUES ('" + machinestr + "','" + BarInfo[i].Barcode + "','"
                                        + PNLBarcode + "','" + BordBarcode + "','" + BarInfo[i].TDate + "','" + BarInfo[i].TTime + "','" + (i + 1).ToString() + "','" + BarInfo[i].Status.ToString() + "')";
                                        mysql.executeQuery(stm);
                                    }                                    
                                }
                                mysql.DisConnect();
                                for (int i = 0; i < 96; i++)
                                {
                                    retstr += BarInfo[i].Status.ToString() + ";";
                                }
                                retstr = retstr.Substring(0, retstr.Length - 1);

                                await TestSentNet.SendAsync("BarcodeInfo;OK;" + retstr);
                                ModelPrint("BarcodeInfo;OK;" + retstr);
                            }
                            else
                            {
                                ModelPrint("条码" + PNLBarcode + "查询失败");
                                if (TestSendStatus)
                                {
                                    await TestSentNet.SendAsync("BarcodeInfo;NG");
                                }
                            }
                        }
                        else
                        {
                            ModelPrint("条码" + barcode + "查询失败");
                            if (TestSendStatus)
                            {
                                await TestSentNet.SendAsync("BarcodeInfo;NG");
                            }
                        }
                    }
                    else
                    {
                        ModelPrint("数据库未连接");
                        if (TestSendStatus)
                        {
                            await TestSentNet.SendAsync("BarcodeInfo;NG");
                        }
                    }
                    oraDB.disconnect();
                }
                catch (Exception ex)
                {
                    ModelPrint(ex.Message);
                    if (TestSendStatus)
                    {
                        await TestSentNet.SendAsync("BarcodeInfo;NG");
                    }
                }
            });

        }
        public async void BottomScanGetBarCodeCallback(string[] barcode,string[] mes)
        {
            ModelPrint(barcode[0] + "," + barcode[1]);
            await Task.Run(async()=> {
                int index = int.Parse(mes[1]);
                int index1 = int.Parse(mes[2]);
                int[] barindex = new int[2];
                barindex[0] = index1 + index * 8;
                barindex[1] = index1 + 4 + index * 8;
                try
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (barcode[i] != "error")
                        {
                            bool isAoi = false;
                            Oracle oraDB = new Oracle("zdtbind", "sfcabar", "sfcabar*168");
                            string sqlstr = "select to_char(sfcdata.GETCK_posaoi_t1('" + barcode[i] + "', 'A')) from dual";
                            DataSet ds = oraDB.executeQuery(sqlstr);
                            DataTable dt1 = ds.Tables[0];
                            if ((string)dt1.Rows[0][0] == "0")
                            {
                                isAoi = true;
                            }
                            //条码 板条码 产品状态 日期 时间
                            BarInfo[barindex[i]].Barcode = barcode[i];
                            BarInfo[barindex[i]].BordBarcode = BordBarcode;
                            BarInfo[barindex[i]].Status = isAoi ? 1 : 0;
                            BarInfo[barindex[i]].TDate = DateTime.Now.ToString("yyyyMMdd");
                            BarInfo[barindex[i]].TTime = DateTime.Now.ToString("HHmmss");
                        }
                        else
                        {
                            BarInfo[barindex[i]].Barcode = "FAIL";
                            BarInfo[barindex[i]].BordBarcode = BordBarcode;
                            BarInfo[barindex[i]].Status = 2;
                            BarInfo[barindex[i]].TDate = DateTime.Now.ToString("yyyyMMdd");
                            BarInfo[barindex[i]].TTime = DateTime.Now.ToString("HHmmss");
                        }

                        string machinestr = Inifile.INIGetStringValue(iniParameterPath, "System", "MachineID", "Jasper01");
                        Mysql mysql = new Mysql();
                        if (mysql.Connect())
                        {
                            string stm = "INSERT INTO BARBIND (MACHINE,SCBARCODE,SCBODBAR,SDATE,STIME,PCSSER,RESULT) VALUES ('" + machinestr + "','" + BarInfo[barindex[i]].Barcode + "','"
                            + BordBarcode + "','" + BarInfo[barindex[i]].TDate + "','" + BarInfo[barindex[i]].TTime + "','" + (barindex[i] + 1).ToString() + "','" + BarInfo[barindex[i]].Status.ToString() + "')";
                            mysql.executeQuery(stm);
                        }
                        mysql.DisConnect();
                    }
                    string retstr = mes[2] + ";" + BarInfo[barindex[0]].Status.ToString() + ";" + BarInfo[barindex[1]].Status.ToString();
                    await TestSentNet.SendAsync("BarcodeInfo1;" + retstr);
                    ModelPrint("BarcodeInfo1;" + retstr);
                }
                catch (Exception ex)
                {
                    ModelPrint(ex.Message);

                }
                
            });
        }
        async void SendBarcode(int index)
        {
            string str = "";
            for (int i = 0; i < 8; i++)
            {
                str += BarInfo[i + index * 8].Barcode + "|";
            }
            str += "0";
            ModelPrint(str);
            bool r = await udp.SendAsync(str);
        }
        async void SaveResult(string[] rststr)
        {
            await Task.Run(()=> {
                int index = int.Parse(rststr[1]);
                Mysql mysql = new Mysql();
                if (mysql.Connect())
                {
                    for (int i = 0; i < 8; i++)
                    {
                        string stm = "UPDATE BARBIND SET RESULT = '" + rststr[2 + i] + "' WHERE SCBARCODE = '" + BarInfo[index * 8 + i].Barcode + "' AND SCBODBAR = '" + BarInfo[index * 8 + i].BordBarcode
                        + "' AND SDATE = '" + BarInfo[index * 8 + i].TDate + "' AND STIME = '" + BarInfo[index * 8 + i].TTime + "'";
                        mysql.executeQuery(stm);
                    }
                    mysql.DisConnect();                   
                }

            });

        }
        #endregion

    }
    public class ProducInfo
    {
        //条码 板条码 产品状态 日期 时间
        public string Barcode { set; get; }
        public string BordBarcode { set; get; }
        public int Status { set; get; }
        public string TDate { set; get; }
        public string TTime { set; get; }
    }
}
