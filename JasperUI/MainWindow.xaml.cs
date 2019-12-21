using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JasperUI.Model;
using BingLibrary.hjb.file;
using BingLibrary.hjb.tools;
using OfficeOpenXml;
using System.IO;
using ViewROI;
using BingLibrary.HVision;
using HalconDotNet;
using System.Diagnostics;
using System.Data;
using System.Windows.Forms;
using System.Net;
using BingLibrary.hjb;
using System.Collections.ObjectModel;

namespace JasperUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 变量
        bool CameraState = false;
        bool PLCState = false;
        bool RobotState = false;
        bool EpsonStatusAuto = false;
        bool EpsonStatusWarning = false;
        bool EpsonStatusSError = false;
        bool EpsonStatusSafeGuard = false;
        bool EpsonStatusEStop = false;
        bool EpsonStatusError = false;
        bool EpsonStatusPaused = false;
        bool EpsonStatusRunning = false;
        bool EpsonStatusReady = false;

        bool CameraState2 = false;
        bool RobotState2 = false;
        bool EpsonStatusAuto2 = false;
        bool EpsonStatusWarning2 = false;
        bool EpsonStatusSError2 = false;
        bool EpsonStatusSafeGuard2 = false;
        bool EpsonStatusEStop2 = false;
        bool EpsonStatusError2 = false;
        bool EpsonStatusPaused2 = false;
        bool EpsonStatusRunning2 = false;
        bool EpsonStatusReady2 = false;
        private EpsonRC90 epsonRC90, epsonRC90_2;
        private string iniParameterPath = System.Environment.CurrentDirectory + "\\Parameter.ini";
        
        Leisai ls;
        List<int[]> ExIoIn, ExIoOut, ExIoIn2, ExIoOut2;
        long SWms = 0;
        DateTime lastSam1, lastSam2,lastClean1,lastClean2;
        DateTime SamStartDatetime1, SamDateBigin1, SamStartDatetime2, SamDateBigin2;
        bool IsInSampleMode1 = false, IsInSampleMode2 = false;
        string LastBanci = "";
        string alarmExcelPath = System.Environment.CurrentDirectory + "\\Jasper报警.xlsx";
        List<AlarmData> AlarmList = new List<AlarmData>();
        string CurrentAlarmStr = "";
        public DataTable Mdt = new DataTable();
        ExcelPackage MPackage;
        ExcelWorksheet MWorksheet;
        int MaterialStatus = 0;
        int haocaisavetimes = 0;
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            MaterialDataGrid.ItemsSource = Mdt.DefaultView;

            System.Diagnostics.Process[] myProcesses = System.Diagnostics.Process.GetProcessesByName("JasperUI");//获取指定的进程名   
            if (myProcesses.Length > 1) //如果可以获取到知道的进程名则说明已经启动
            {
                System.Windows.MessageBox.Show("不允许重复打开软件");
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                FileInfo existingFile = new FileInfo("C:\\耗材.xlsx");
                try
                {
                    MPackage = new ExcelPackage(existingFile);
                    MWorksheet = MPackage.Workbook.Worksheets[1];
                    
                    for (int i = 1; i <= MWorksheet.Dimension.End.Column; i++)
                    {
                        Mdt.Columns.Add((string)MWorksheet.Cells[2, i].Value);
                    }

                    ObservableCollection<string> myItemsSource = new ObservableCollection<string>();
                    for (int i = 3; i <= MWorksheet.Dimension.End.Row; i++)
                    {
                        DataRow dr = Mdt.NewRow();
                        for (int j = 1; j <= MWorksheet.Dimension.End.Column; j++)
                        {
                            dr[j - 1] = MWorksheet.Cells[i, j].Value;
                        }
                        myItemsSource.Add((string)MWorksheet.Cells[i , 1].Value);
                        Mdt.Rows.Add(dr);
                    }
                    MaterialComboxBox.ItemsSource = myItemsSource;
                    MaterialComboxBox.SelectedIndex = 0;

                    string ip = Inifile.INIGetStringValue(iniParameterPath, "Epson", "EpsonIp", "192.168.0.30");
                    string mcuip = Inifile.INIGetStringValue(iniParameterPath, "MCU", "MCUIp", "192.168.0.130");
                    int mcuport = int.Parse(Inifile.INIGetStringValue(iniParameterPath, "MCU", "MCUPort", "11099"));
                    epsonRC90 = new EpsonRC90(ip, mcuip, mcuport, "第1台");
                    ip = Inifile.INIGetStringValue(iniParameterPath, "Epson", "EpsonIp2", "192.168.0.40");
                    mcuip = Inifile.INIGetStringValue(iniParameterPath, "MCU", "MCUIp2", "192.168.0.130");
                    mcuport = int.Parse(Inifile.INIGetStringValue(iniParameterPath, "MCU", "MCUPort2", "11099"));
                    epsonRC90_2 = new EpsonRC90(ip, mcuip, mcuport, "第2台");

                    GlobalVars.viewController1 = new HWndCtrl(ImageWindow1);
                    GlobalVars.roiController1 = new ROIController();
                    GlobalVars.viewController1.useROIController(GlobalVars.roiController1);
                    GlobalVars.viewController1.setViewState(HWndCtrl.MODE_VIEW_MOVE);

                    GlobalVars.viewController2 = new HWndCtrl(ImageWindow2);
                    GlobalVars.roiController2 = new ROIController();
                    GlobalVars.viewController2.useROIController(GlobalVars.roiController2);
                    GlobalVars.viewController2.setViewState(HWndCtrl.MODE_VIEW_MOVE);


                    epsonRC90.ModelPrint += ModelPrintEventProcess;
                    epsonRC90.EpsonStatusUpdate += EpsonStatusUpdateProcess;

                    epsonRC90_2.ModelPrint += ModelPrintEventProcess;
                    epsonRC90_2.EpsonStatusUpdate += EpsonStatusUpdateProcess2;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "耗材表格文件异常");
                    System.Windows.Application.Current.Shutdown();
                }
                
            }


            

        }
        #region 功能函数
        void AddMessage(string str)
        {
            this.Dispatcher.Invoke(new Action(() => {

                string[] s = MsgTextBox.Text.Split('\n');
                if (s.Length > 1000)
                {
                    MsgTextBox.Text = "";
                }
                if (MsgTextBox.Text != "")
                {
                    MsgTextBox.Text += "\r\n";
                }
                MsgTextBox.Text += DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + str;

            }));
        }
        void LoadedCallback()
        {
            Init();
            AddMessage("软件加载完成");
        }
        void Init()
        {
            try
            {
                ls = new Leisai();
                if (ls.Connect() < 3)
                {
                    throw new Exception("轴卡异常");
                }
                else
                {
                    string ExIoExcelPath = System.Environment.CurrentDirectory + "\\RobotExIo.xlsx";

                    if (File.Exists(ExIoExcelPath))
                    {
                        FileInfo existingFile = new FileInfo(ExIoExcelPath);
                        using (ExcelPackage package = new ExcelPackage(existingFile))
                        {
                            // get the first worksheet in the workbook
                            ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                            ExIoIn = new List<int[]>();//雷赛卡的输入
                            ExIoOut = new List<int[]>();//雷赛卡的输出
                            for (int i = 2; i <= worksheet.Dimension.End.Row; i++)
                            {
                                if (worksheet.Cells["A" + i.ToString()].Value != null && worksheet.Cells["B" + i.ToString()].Value != null && worksheet.Cells["C" + i.ToString()].Value != null)
                                {
                                    ExIoIn.Add(new int[3] { int.Parse(worksheet.Cells["A" + i.ToString()].Value.ToString()), int.Parse(worksheet.Cells["B" + i.ToString()].Value.ToString()), int.Parse(worksheet.Cells["C" + i.ToString()].Value.ToString()) });
                                }
                                if (worksheet.Cells["D" + i.ToString()].Value != null && worksheet.Cells["E" + i.ToString()].Value != null && worksheet.Cells["F" + i.ToString()].Value != null)
                                {
                                    ExIoOut.Add(new int[3] { int.Parse(worksheet.Cells["D" + i.ToString()].Value.ToString()), int.Parse(worksheet.Cells["E" + i.ToString()].Value.ToString()), int.Parse(worksheet.Cells["F" + i.ToString()].Value.ToString()) });
                                }
                            }

                            // get the first worksheet in the workbook
                            worksheet = package.Workbook.Worksheets[2];
                            ExIoIn2 = new List<int[]>();//雷赛卡的输入
                            ExIoOut2 = new List<int[]>();//雷赛卡的输出
                            for (int i = 2; i <= worksheet.Dimension.End.Row; i++)
                            {
                                if (worksheet.Cells["A" + i.ToString()].Value != null && worksheet.Cells["B" + i.ToString()].Value != null && worksheet.Cells["C" + i.ToString()].Value != null)
                                {
                                    ExIoIn2.Add(new int[3] { int.Parse(worksheet.Cells["A" + i.ToString()].Value.ToString()), int.Parse(worksheet.Cells["B" + i.ToString()].Value.ToString()), int.Parse(worksheet.Cells["C" + i.ToString()].Value.ToString()) });
                                }
                                if (worksheet.Cells["D" + i.ToString()].Value != null && worksheet.Cells["E" + i.ToString()].Value != null && worksheet.Cells["F" + i.ToString()].Value != null)
                                {
                                    ExIoOut2.Add(new int[3] { int.Parse(worksheet.Cells["D" + i.ToString()].Value.ToString()), int.Parse(worksheet.Cells["E" + i.ToString()].Value.ToString()), int.Parse(worksheet.Cells["F" + i.ToString()].Value.ToString()) });
                                }
                            }
                        }

                        ExIoExcelPath = System.Environment.CurrentDirectory + "\\排版.xlsx";
                        if (File.Exists(ExIoExcelPath))
                        {
                            existingFile = new FileInfo(ExIoExcelPath);
                            using (ExcelPackage package = new ExcelPackage(existingFile))
                            {
                                // get the first worksheet in the workbook
                                ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                                for (int i = 0; i < 4; i++)
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        for (int k = 0; k < 4; k++)
                                        {
                                            epsonRC90.BordIndexA[i * 3 + j, k] = int.Parse(worksheet.Cells[13 + i * 2, 4 - k + j * 4].Value.ToString());
                                        }
                                        for (int k = 0; k < 4; k++)
                                        {
                                            epsonRC90.BordIndexA[i * 3 + j, k + 4] = int.Parse(worksheet.Cells[12 + i * 2, 4 - k + j * 4].Value.ToString());
                                        }
                                    }
                                }

                                for (int i = 0; i < 4; i++)
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        for (int k = 0; k < 4; k++)
                                        {
                                            epsonRC90.BordIndexB[i * 3 + j, k] = int.Parse(worksheet.Cells[22 + i * 2, 4 - k + j * 4].Value.ToString());
                                        }
                                        for (int k = 0; k < 4; k++)
                                        {
                                            epsonRC90.BordIndexB[i * 3 + j, k + 4] = int.Parse(worksheet.Cells[21 + i * 2, 4 - k + j * 4].Value.ToString());
                                        }
                                    }
                                }
                            }
                            if (File.Exists(alarmExcelPath))
                            {
                                existingFile = new FileInfo(alarmExcelPath);
                                using (ExcelPackage package = new ExcelPackage(existingFile))
                                {
                                    // get the first worksheet in the workbook
                                    ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                                    for (int i = 1; i <= worksheet.Dimension.End.Row; i++)
                                    {
                                        AlarmData ad = new AlarmData();
                                        ad.Code = worksheet.Cells["A" + i.ToString()].Value == null ? "Null" : worksheet.Cells["A" + i.ToString()].Value.ToString();
                                        ad.Content = worksheet.Cells["B" + i.ToString()].Value == null ? "Null" : worksheet.Cells["B" + i.ToString()].Value.ToString();
                                        ad.Start = DateTime.Now;
                                        ad.End = DateTime.Now;
                                        ad.State = false;
                                        AlarmList.Add(ad);
                                    }
                                }
                                ExIoExcelPath = System.Environment.CurrentDirectory + "\\样本规格.xlsx";
                                if (File.Exists(ExIoExcelPath))
                                {
                                    existingFile = new FileInfo(ExIoExcelPath);
                                    using (ExcelPackage package = new ExcelPackage(existingFile))
                                    {
                                        // get the first worksheet in the workbook
                                        ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                                        epsonRC90.SamOpenList = new List<string>();
                                        epsonRC90.SamShortList = new List<string>();
                                        epsonRC90_2.SamOpenList = new List<string>();
                                        epsonRC90_2.SamShortList = new List<string>();
                                        for (int i = 4; i <= worksheet.Dimension.End.Row; i++)
                                        {
                                            if (worksheet.Cells["C" + i.ToString()].Value != null)
                                            {
                                                epsonRC90.SamOpenList.Add(worksheet.Cells["C" + i.ToString()].Value.ToString());
                                                epsonRC90_2.SamOpenList.Add(worksheet.Cells["C" + i.ToString()].Value.ToString());
                                            }
                                            if (worksheet.Cells["D" + i.ToString()].Value != null)
                                            {
                                                epsonRC90.SamShortList.Add(worksheet.Cells["D" + i.ToString()].Value.ToString());
                                                epsonRC90_2.SamShortList.Add(worksheet.Cells["D" + i.ToString()].Value.ToString());
                                            }
                                        }
                                    }

                                    if (GlobalVars.Camera.OpenCamera("CAM1", "GigEVision"))
                                    {
                                        if (GlobalVars.Camera2.OpenCamera("CAM2", "GigEVision"))
                                        {
                                            try
                                            {
                                                Oracle oraDB = new Oracle("zdtbind", "sfcabar", "sfcabar*168");
                                                if (oraDB.isConnect())
                                                {
                                                    AddMessage("更新系统时间" + oraDB.OraclDateTime());
                                                    LastBanci = Inifile.INIGetStringValue(iniParameterPath, "Summary", "LastBanci", "null");

                                                    //SamItem1.Text = Inifile.INIGetStringValue(iniParameterPath, "Sample", "SamItem1", "OK");
                                                    //SamItem2.Text = Inifile.INIGetStringValue(iniParameterPath, "Sample", "SamItem2", "OK");
                                                    //SamItem3.Text = Inifile.INIGetStringValue(iniParameterPath, "Sample", "SamItem3", "OK");
                                                    //SamItem4.Text = Inifile.INIGetStringValue(iniParameterPath, "Sample", "SamItem4", "OK");
                                                    IsSam.IsChecked = Inifile.INIGetStringValue(iniParameterPath, "Sample", "IsSam", "1") == "1";
                                                    //IsSam.IsChecked = false;
                                                    IsClean.IsChecked = Inifile.INIGetStringValue(iniParameterPath, "Clean", "IsClean", "1") == "1";

                                                    lastSam1 = Convert.ToDateTime(Inifile.INIGetStringValue(iniParameterPath, "Sample", "LastSam1", "2019/1/1 00:00:00"));
                                                    LastSam1.Text = lastSam1.ToString();
                                                    lastSam2 = Convert.ToDateTime(Inifile.INIGetStringValue(iniParameterPath, "Sample", "LastSam2", "2019/1/1 00:00:00"));
                                                    LastSam2.Text = lastSam2.ToString();

                                                    lastClean1 = Convert.ToDateTime(Inifile.INIGetStringValue(iniParameterPath, "Clean", "LastClean1", "2019/1/1 00:00:00"));
                                                    LastClean1.Text = lastClean1.ToString();
                                                    lastClean2 = Convert.ToDateTime(Inifile.INIGetStringValue(iniParameterPath, "Clean", "LastClean2", "2019/1/1 00:00:00"));
                                                    LastClean2.Text = lastClean2.ToString();

                                                    MachineID.Text = Inifile.INIGetStringValue(iniParameterPath, "System", "MachineID", "Jasper01");
                                                    MachineID2.Text = Inifile.INIGetStringValue(iniParameterPath, "System", "MachineID2", "Jasper01");
                                                    线体.Text = Inifile.INIGetStringValue(iniParameterPath, "System", "线体", "null");
                                                    测试料号.Text = Inifile.INIGetStringValue(iniParameterPath, "System", "测试料号", "null");

                                                    SystemBarcodeModeCheckBox.IsChecked = Inifile.INIGetStringValue(iniParameterPath, "System", "SystemBarcodeMode", "0") == "1";
                                                    SingleBarcodeModeCheckBox.IsChecked = Inifile.INIGetStringValue(iniParameterPath, "System", "SingleBarcodeMode", "0") == "1";

                                                    SamLimitCount.Text = Inifile.INIGetStringValue(iniParameterPath, "System", "SamLimitCount", "999");

                                                    string COM = Inifile.INIGetStringValue(iniParameterPath, "Scan", "Scan1", "COM0");
                                                    GlobalVars.Scan1 = new Scan();
                                                    GlobalVars.Scan1.ini(COM);
                                                    COM = Inifile.INIGetStringValue(iniParameterPath, "Scan", "Scan2", "COM0");
                                                    GlobalVars.Scan2 = new Scan();
                                                    GlobalVars.Scan2.ini(COM);
                                                    Async.RunFuncAsync(ls.Run, null);//刷IO卡
                                                    string ip = Inifile.INIGetStringValue(iniParameterPath, "FX5U", "Ip", "192.168.0.20");
                                                    int port = int.Parse(Inifile.INIGetStringValue(iniParameterPath, "FX5U", "Port", "502"));
                                                    GlobalVars.Fx5u = new Fx5u(ip, port);
                                                    Async.RunFuncAsync(IORun, null);
                                                    Run();
                                                }
                                                oraDB.disconnect();
                                            }
                                            catch (Exception)
                                            {
                                                throw;
                                            }
                                        }
                                        else
                                        {
                                            throw new Exception("相机2打开失败");
                                        }




                                    }
                                    else
                                    {
                                        throw new Exception("相机1打开失败");
                                    }
                                }
                                else
                                {
                                    throw new Exception("样本规格.xlsx 文件不存在");
                                }
                                
                            }
                            else
                            {
                                throw new Exception("Jasper报警.xlsx 文件不存在");
                            }




                           




                        }
                        else
                        {
                            throw new Exception("排版文件不存在");
                        }



                    }
                    else
                    {
                        throw new Exception("IO文件不存在");
                    }


                }
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }
        void UpdateUI()
        {
            #region 机械手状态
            if (EpsonStatusAuto)
            {
                EllipseEpsonStatusAuto.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusAuto.Fill = Brushes.Gray;
            }
            if (EpsonStatusWarning)
            {
                EllipseEpsonStatusWarning.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusWarning.Fill = Brushes.Gray;
            }
            if (EpsonStatusSError)
            {
                EllipseEpsonStatusSError.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusSError.Fill = Brushes.Gray;
            }
            if (EpsonStatusSafeGuard)
            {
                EllipseEpsonStatusSafeGuard.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusSafeGuard.Fill = Brushes.Gray;
            }
            if (EpsonStatusEStop)
            {
                EllipseEpsonStatusEStop.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusEStop.Fill = Brushes.Gray;
            }
            if (EpsonStatusError)
            {
                EllipseEpsonStatusError.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusError.Fill = Brushes.Gray;
            }
            if (EpsonStatusPaused)
            {
                EllipseEpsonStatusPaused.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusPaused.Fill = Brushes.Gray;
            }
            if (EpsonStatusRunning)
            {
                EllipseEpsonStatusRunning.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusRunning.Fill = Brushes.Gray;
            }
            if (EpsonStatusReady)
            {
                EllipseEpsonStatusReady.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusReady.Fill = Brushes.Gray;
            }

            if (EpsonStatusAuto2)
            {
                EllipseEpsonStatusAuto2.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusAuto2.Fill = Brushes.Gray;
            }
            if (EpsonStatusWarning2)
            {
                EllipseEpsonStatusWarning2.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusWarning2.Fill = Brushes.Gray;
            }
            if (EpsonStatusSError2)
            {
                EllipseEpsonStatusSError2.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusSError2.Fill = Brushes.Gray;
            }
            if (EpsonStatusSafeGuard2)
            {
                EllipseEpsonStatusSafeGuard2.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusSafeGuard2.Fill = Brushes.Gray;
            }
            if (EpsonStatusEStop2)
            {
                EllipseEpsonStatusEStop2.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusEStop2.Fill = Brushes.Gray;
            }
            if (EpsonStatusError2)
            {
                EllipseEpsonStatusError2.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusError2.Fill = Brushes.Gray;
            }
            if (EpsonStatusPaused2)
            {
                EllipseEpsonStatusPaused2.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusPaused2.Fill = Brushes.Gray;
            }
            if (EpsonStatusRunning2)
            {
                EllipseEpsonStatusRunning2.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusRunning2.Fill = Brushes.Gray;
            }
            if (EpsonStatusReady2)
            {
                EllipseEpsonStatusReady2.Fill = Brushes.Yellow;
            }
            else
            {
                EllipseEpsonStatusReady2.Fill = Brushes.Gray;
            }
            #endregion
            #region 设备连接状态
            if (PLCState)
            {
                EllipsePLCState.Fill = Brushes.Green;
            }
            else
            {
                EllipsePLCState.Fill = Brushes.Red;
            }
            if (RobotState)
            {
                EllipseRobotState.Fill = Brushes.Green;
            }
            else
            {
                EllipseRobotState.Fill = Brushes.Red;
            }
            if (RobotState2)
            {
                EllipseRobotState2.Fill = Brushes.Green;
            }
            else
            {
                EllipseRobotState2.Fill = Brushes.Red;
            }
            if (CameraState)
            {
                EllipseCameraState.Fill = Brushes.Green;
            }
            else
            {
                EllipseCameraState.Fill = Brushes.Red;
            }
            if (CameraState2)
            {
                EllipseCameraState2.Fill = Brushes.Green;
            }
            else
            {
                EllipseCameraState2.Fill = Brushes.Red;
            }
            #endregion
            #region 其他
            CycleText.Text = SWms.ToString() + " ms";
            #endregion
        }
        async void Run()
        {

            Stopwatch sw = new Stopwatch();
            bool m2000 = false, m2004 = false, m2001 = false, m2005 = false, m2006 = false, m2007 = false; ;
            bool[] M2000, M1000; bool[] Y42; bool[] y42 = new bool[8] { false, false, false, false, false, false, false, false };
            bool first = true;
            string macid, linenum, productnum;
            macid = Inifile.INIGetStringValue(iniParameterPath, "System", "MachineID", "Jasper01");
            linenum = Inifile.INIGetStringValue(iniParameterPath, "System", "线体", "null");
            productnum = Inifile.INIGetStringValue(iniParameterPath, "System", "测试料号", "null");
            while (true)
            {
                sw.Restart();
                await Task.Delay(100);
                #region UpdateUI
                PLCState = GlobalVars.Fx5u.Connect;
                CameraState = GlobalVars.Camera.Connected;
                CameraState2 = GlobalVars.Camera2.Connected;
                RobotState = epsonRC90.CtrlStatus && epsonRC90.IOReceiveStatus && epsonRC90.TestReceiveStatus && epsonRC90.TestSendStatus;
                RobotState2 = epsonRC90_2.CtrlStatus && epsonRC90_2.IOReceiveStatus && epsonRC90_2.TestReceiveStatus && epsonRC90_2.TestSendStatus;
                UpdateUI();
                #endregion
                #region 清洁
                NextClean1.Text = lastClean1.AddHours(2).ToString();
                SpanClean1.Text = (lastClean1.AddHours(2) - DateTime.Now).ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries)[0];
                NextClean2.Text = lastClean2.AddHours(2).ToString();
                SpanClean2.Text = (lastClean2.AddHours(2) - DateTime.Now).ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries)[0];
                #endregion
                #region 样本
                SamDateBigin1 = lastSam1.AddHours(4);
                SamStartDatetime1 = lastSam1.AddHours(6);
                NextSam1.Text = SamStartDatetime1.ToString();
                SpanSam1.Text =(SamStartDatetime1 - DateTime.Now).ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries)[0];
                SampleGrid1.Visibility = (DateTime.Now - SamDateBigin1).TotalSeconds > 0 && IsSam.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
                if ((DateTime.Now - SamDateBigin1).TotalSeconds > 0 && IsSam.IsChecked.Value)
                {
                    if (IsInSampleMode1)
                    {
                        SampleTextBlock1.Text = "样本测试中";
                    }
                    else
                    {
                        if ((DateTime.Now - SamStartDatetime1).TotalSeconds < 0)
                        {
                            SampleTextBlock1.Text = "请测样本";
                        }
                        else
                        {
                            SampleTextBlock1.Text = "强制样本";
                        }
                    }
                }
                SamDateBigin2 = lastSam2.AddHours(4);
                SamStartDatetime2 = lastSam2.AddHours(6);
                NextSam2.Text = SamStartDatetime2.ToString();
                SpanSam2.Text = (SamStartDatetime2 - DateTime.Now).ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries)[0];
                SampleGrid2.Visibility = (DateTime.Now - SamDateBigin2).TotalSeconds > 0 && IsSam.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
                if ((DateTime.Now - SamDateBigin1).TotalSeconds > 0 && IsSam.IsChecked.Value)
                {
                    if (IsInSampleMode2)
                    {
                        SampleTextBlock2.Text = "样本测试中";
                    }
                    else
                    {
                        if ((DateTime.Now - SamStartDatetime2).TotalSeconds < 0)
                        {
                            SampleTextBlock2.Text = "请测样本";
                        }
                        else
                        {
                            SampleTextBlock2.Text = "强制样本";
                        }
                    }
                }
                #endregion
                #region 良率
                Result1_1.Result = epsonRC90.jasperTester.Result[0];
                Result1_2.Result = epsonRC90.jasperTester.Result[1];
                Result1_3.Result = epsonRC90.jasperTester.Result[2];
                Result1_4.Result = epsonRC90.jasperTester.Result[3];
                Result1_5.Result = epsonRC90.jasperTester.Result[4];
                Result1_6.Result = epsonRC90.jasperTester.Result[5];
                Result1_7.Result = epsonRC90.jasperTester.Result[6];
                Result1_8.Result = epsonRC90.jasperTester.Result[7];

                Result2_1.Result = epsonRC90_2.jasperTester.Result[0];
                Result2_2.Result = epsonRC90_2.jasperTester.Result[1];
                Result2_3.Result = epsonRC90_2.jasperTester.Result[2];
                Result2_4.Result = epsonRC90_2.jasperTester.Result[3];
                Result2_5.Result = epsonRC90_2.jasperTester.Result[4];
                Result2_6.Result = epsonRC90_2.jasperTester.Result[5];
                Result2_7.Result = epsonRC90_2.jasperTester.Result[6];
                Result2_8.Result = epsonRC90_2.jasperTester.Result[7];

                Yield1_1.PassCount = epsonRC90.jasperTester.PassCount[0];
                Yield1_2.PassCount = epsonRC90.jasperTester.PassCount[1];
                Yield1_3.PassCount = epsonRC90.jasperTester.PassCount[2];
                Yield1_4.PassCount = epsonRC90.jasperTester.PassCount[3];
                Yield1_5.PassCount = epsonRC90.jasperTester.PassCount[4];
                Yield1_6.PassCount = epsonRC90.jasperTester.PassCount[5];
                Yield1_7.PassCount = epsonRC90.jasperTester.PassCount[6];
                Yield1_8.PassCount = epsonRC90.jasperTester.PassCount[7];

                Yield2_1.PassCount = epsonRC90_2.jasperTester.PassCount[0];
                Yield2_2.PassCount = epsonRC90_2.jasperTester.PassCount[1];
                Yield2_3.PassCount = epsonRC90_2.jasperTester.PassCount[2];
                Yield2_4.PassCount = epsonRC90_2.jasperTester.PassCount[3];
                Yield2_5.PassCount = epsonRC90_2.jasperTester.PassCount[4];
                Yield2_6.PassCount = epsonRC90_2.jasperTester.PassCount[5];
                Yield2_7.PassCount = epsonRC90_2.jasperTester.PassCount[6];
                Yield2_8.PassCount = epsonRC90_2.jasperTester.PassCount[7];

                Yield1_1.Yield = epsonRC90.jasperTester.Yield[0];
                Yield1_2.Yield = epsonRC90.jasperTester.Yield[1];
                Yield1_3.Yield = epsonRC90.jasperTester.Yield[2];
                Yield1_4.Yield = epsonRC90.jasperTester.Yield[3];
                Yield1_5.Yield = epsonRC90.jasperTester.Yield[4];
                Yield1_6.Yield = epsonRC90.jasperTester.Yield[5];
                Yield1_7.Yield = epsonRC90.jasperTester.Yield[6];
                Yield1_8.Yield = epsonRC90.jasperTester.Yield[7];

                Yield2_1.Yield = epsonRC90_2.jasperTester.Yield[0];
                Yield2_2.Yield = epsonRC90_2.jasperTester.Yield[1];
                Yield2_3.Yield = epsonRC90_2.jasperTester.Yield[2];
                Yield2_4.Yield = epsonRC90_2.jasperTester.Yield[3];
                Yield2_5.Yield = epsonRC90_2.jasperTester.Yield[4];
                Yield2_6.Yield = epsonRC90_2.jasperTester.Yield[5];
                Yield2_7.Yield = epsonRC90_2.jasperTester.Yield[6];
                Yield2_8.Yield = epsonRC90_2.jasperTester.Yield[7];

                if (LastBanci != GetBanci())
                {
                    LastBanci = GetBanci();
                    Inifile.INIWriteValue(iniParameterPath, "Summary", "LastBanci", LastBanci);
                    WriteMachineData();
                    for (int i = 0; i < 8; i++)
                    {
                        epsonRC90.jasperTester.TestCount[i] = 0;
                        epsonRC90.jasperTester.PassCount[i] = 0;
                        Inifile.INIWriteValue(iniParameterPath, "Summary", "Tester1TestCount" + (i + 1).ToString(), "0");
                        Inifile.INIWriteValue(iniParameterPath, "Summary", "Tester1PassCount" + (i + 1).ToString(), "0");
                        epsonRC90_2.jasperTester.TestCount[i] = 0;
                        epsonRC90_2.jasperTester.PassCount[i] = 0;
                        Inifile.INIWriteValue(iniParameterPath, "Summary", "Tester2TestCount" + (i + 1).ToString(), "0");
                        Inifile.INIWriteValue(iniParameterPath, "Summary", "Tester2PassCount" + (i + 1).ToString(), "0");
                    }
                    AddMessage(LastBanci + " 换班数据清零");
                }
                #endregion
                #region work
                try
                {
                    M2000 = await Task.Run<bool[]>(() => {
                        return GlobalVars.Fx5u.ReadMultiM("M2000", 16);
                    });
                    if (M2000 != null)
                    {
                        if (first)
                        {
                            first = false;
                            m2000 = M2000[0];
                            m2004 = M2000[4];
                            m2001 = M2000[1];
                            m2005 = M2000[5];
                            m2006 = M2000[6];
                            m2007 = M2000[7];
                        }
                        #region 工位1
                        if (m2000 != M2000[0])
                        {
                            m2000 = M2000[0];
                            if (m2000)
                            {
                                GlobalVars.Fx5u.SetM("M2000", false);
                                GlobalVars.Fx5u.SetM("M2500", false);
                                GlobalVars.Fx5u.SetM("M2501", false);
                                GlobalVars.Fx5u.SetM("M2510", false);
                                GlobalVars.Fx5u.SetM("M2511", false);
                                GlobalVars.Scan1.GetBarCode((string barcode) =>
                                {
                                    AddMessage("测试机1轨道扫码:" + barcode);
                                    if (barcode != "Error")
                                    {
                                        Mysql mysql = new Mysql();
                                        if (mysql.Connect())
                                        {
                                            string stm = "SELECT * FROM BODMSG WHERE SCBODBAR = '" + barcode + "' ORDER BY SIDATE DESC LIMIT 0,5";
                                            DataSet ds = mysql.Select(stm);
                                            DataTable dt = ds.Tables["table0"];
                                            if (dt.Rows.Count > 0)
                                            {
                                                if (dt.Rows[0]["STATUS"] == DBNull.Value)
                                                {
                                                    stm = "INSERT INTO BODMSG (SCBODBAR, STATUS) VALUES('" + barcode + "','ON')";
                                                    mysql.executeQuery(stm);
                                                    epsonRC90.BordBarcode = barcode;
                                                    AddMessage("板 " + barcode + " 绑定");
                                                    GlobalVars.Fx5u.SetM("M2511", true);
                                                }
                                                else
                                                {
                                                    if ((string)dt.Rows[0]["STATUS"] == "OFF")
                                                    {
                                                        stm = "INSERT INTO BODMSG (SCBODBAR, STATUS) VALUES('" + barcode + "','ON')";
                                                        mysql.executeQuery(stm);
                                                        epsonRC90.BordBarcode = barcode;
                                                        AddMessage("板 " + barcode + " 绑定");
                                                        GlobalVars.Fx5u.SetM("M2511", true);
                                                    }
                                                    else
                                                    {
                                                        AddMessage("板 " + barcode + " 已测过");
                                                        GlobalVars.Fx5u.SetM("M2510", true);
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                stm = "INSERT INTO BODMSG (SCBODBAR, STATUS) VALUES('" + barcode + "','ON')";
                                                mysql.executeQuery(stm);
                                                epsonRC90.BordBarcode = barcode;
                                                AddMessage("板 " + barcode + " 绑定");
                                                GlobalVars.Fx5u.SetM("M2511", true);
                                            }
                                            GlobalVars.Fx5u.SetM("M2500", true);
                                        }
                                        else
                                        {
                                            AddMessage("Mysql数据库查询失败");
                                            GlobalVars.Fx5u.SetM("M2501", true);
                                        }
                                        mysql.DisConnect();

                                    }
                                    else
                                    {
                                        GlobalVars.Fx5u.SetM("M2501", true);
                                    }
                                });
                            }
                        }
                        if (m2004 != M2000[4])
                        {
                            m2004 = M2000[4];
                            if (m2004)
                            {
                                AddMessage("测试机1发送:RobotCanGet");
                                GlobalVars.Fx5u.SetM("M2004", false);
                                if (epsonRC90.TestSendStatus)
                                {                                    
                                    string scanmode = "0";
                                    if (Inifile.INIGetStringValue(iniParameterPath, "System", "SystemBarcodeMode", "0") == "1")
                                    {
                                        scanmode = "1";
                                    }
                                    await epsonRC90.TestSentNet.SendAsync("RobotCanGet;" + scanmode);
                                }
                            }
                        }
                        #endregion
                        #region 工位2
                        if (m2001 != M2000[1])
                        {
                            m2001 = M2000[1];
                            if (m2001)
                            {
                                GlobalVars.Fx5u.SetM("M2001", false);
                                GlobalVars.Fx5u.SetM("M2502", false);
                                GlobalVars.Fx5u.SetM("M2503", false);
                                GlobalVars.Fx5u.SetM("M2512", false);
                                GlobalVars.Fx5u.SetM("M2513", false);
                                GlobalVars.Scan2.GetBarCode((string barcode) =>
                                {
                                    AddMessage("测试机2轨道扫码:" + barcode);
                                    if (barcode != "Error")
                                    {
                                        Mysql mysql = new Mysql();
                                        if (mysql.Connect())
                                        {
                                            string stm = "SELECT * FROM BODMSG WHERE SCBODBAR = '" + barcode + "' ORDER BY SIDATE DESC LIMIT 0,5";
                                            DataSet ds = mysql.Select(stm);
                                            DataTable dt = ds.Tables["table0"];
                                            if (dt.Rows.Count > 0)
                                            {
                                                if (dt.Rows[0]["STATUS"] == DBNull.Value)
                                                {
                                                    stm = "INSERT INTO BODMSG (SCBODBAR, STATUS) VALUES('" + barcode + "','ON')";
                                                    mysql.executeQuery(stm);
                                                    epsonRC90_2.BordBarcode = barcode;
                                                    AddMessage("板 " + barcode + " 绑定");
                                                    GlobalVars.Fx5u.SetM("M2513", true);
                                                }
                                                else
                                                {
                                                    if ((string)dt.Rows[0]["STATUS"] == "OFF")
                                                    {
                                                        stm = "INSERT INTO BODMSG (SCBODBAR, STATUS) VALUES('" + barcode + "','ON')";
                                                        mysql.executeQuery(stm);
                                                        epsonRC90_2.BordBarcode = barcode;
                                                        AddMessage("板 " + barcode + " 绑定");
                                                        GlobalVars.Fx5u.SetM("M2513", true);
                                                    }
                                                    else
                                                    {
                                                        AddMessage("板 " + barcode + " 已测过");
                                                        GlobalVars.Fx5u.SetM("M2512", true);
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                stm = "INSERT INTO BODMSG (SCBODBAR, STATUS) VALUES('" + barcode + "','ON')";
                                                mysql.executeQuery(stm);
                                                epsonRC90_2.BordBarcode = barcode;
                                                AddMessage("板 " + barcode + " 绑定");
                                                GlobalVars.Fx5u.SetM("M2513", true);
                                            }
                                            GlobalVars.Fx5u.SetM("M2502", true);
                                        }
                                        else
                                        {
                                            AddMessage("Mysql数据库查询失败");
                                            GlobalVars.Fx5u.SetM("M2503", true);
                                        }
                                        mysql.DisConnect();

                                    }
                                    else
                                    {
                                        GlobalVars.Fx5u.SetM("M2503", true);
                                    }
                                });
                            }
                        }
                        if (m2005 != M2000[5])
                        {
                            m2005 = M2000[5];
                            if (m2005)
                            {
                                AddMessage("测试机2发送:RobotCanGet");
                                GlobalVars.Fx5u.SetM("M2005", false);
                                if (epsonRC90_2.TestSendStatus)
                                {
                                    await epsonRC90_2.TestSentNet.SendAsync("RobotCanGet");

                                }
                            }
                        }
                        #endregion
                        if (m2006 != M2000[6])
                        {
                            m2006 = M2000[6];
                            if (M2000[6])
                            {
                                if (epsonRC90.CtrlStatus)
                                {
                                    await epsonRC90.CtrlNet.SendAsync("$pause");
                                }
                                if (epsonRC90_2.CtrlStatus)
                                {
                                    await epsonRC90_2.CtrlNet.SendAsync("$pause");
                                }
                            }
                        }
                        if (m2007 != M2000[7])
                        {
                            m2007 = M2000[7];
                            if (M2000[7])
                            {
                                if (epsonRC90.CtrlStatus)
                                {
                                    await epsonRC90.CtrlNet.SendAsync("$continue");
                                }
                                if (epsonRC90_2.CtrlStatus)
                                {
                                    await epsonRC90_2.CtrlNet.SendAsync("$continue");
                                }
                            }
                        }
                        Y42 = await Task.Run<bool[]>(() => {
                            return GlobalVars.Fx5u.ReadMultiM("Y22", 8);
                        });
                        if (Y42 != null)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                if (y42[i] != Y42[i])
                                {
                                    y42[i] = Y42[i];
                                    if (Y42[i])
                                    {
                                        MWorksheet.Cells[5, 6].Value = Convert.ToInt32(MWorksheet.Cells[5, 6].Value) + 1;
                                    }
                                }
                            }
                        }
                    }
                    #region 报警
                    GlobalVars.Fx5u.SetMultiM("M2514",new bool[] { epsonRC90.Rc90Out[49], epsonRC90_2.Rc90Out[49], epsonRC90.Rc90Out[50], epsonRC90_2.Rc90Out[50], epsonRC90.Rc90Out[51], epsonRC90_2.Rc90Out[51], epsonRC90.Rc90Out[52], epsonRC90_2.Rc90Out[52] });
                    M1000 = await Task.Run<bool[]>(() => {
                        return GlobalVars.Fx5u.ReadMultiM("M1000", 96);
                    });
                    for (int i = 0; i < AlarmList.Count; i++)
                    {
                        if (M1000[i] != AlarmList[i].State)
                        {
                            AlarmList[i].State = M1000[i];
                            if (AlarmList[i].Content != "Null")
                            {
                                if (AlarmList[i].State)
                                {
                                    if (CurrentAlarmStr != AlarmList[i].Content)
                                    {
                                        CurrentAlarmStr = AlarmList[i].Content;
                                        AlarmList[i].Start = DateTime.Now;
                                        AddMessage(AlarmList[i].Code + AlarmList[i].Content + "发生");
                                        string _ip = GetIp();
                                        string _class = DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20 ? "D" : "N";
                                        string _faulttime = "0";
                                        await BigDataInsert(_ip, macid, linenum, productnum, _class, AlarmList[i].Content, AlarmList[i].Start.ToString(), _faulttime);
                                    }
                                }
                                else
                                {
                                    AlarmList[i].End = DateTime.Now;
                                    AddMessage(AlarmList[i].Code + AlarmList[i].Content + "解除");
                                    string _ip = GetIp();
                                    string _class = DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20 ? "D" : "N";
                                    string _faulttime = (AlarmList[i].End - AlarmList[i].Start).TotalMinutes.ToString("F0");
                                    if ((AlarmList[i].End - AlarmList[i].Start).TotalHours <= 0.5 && (AlarmList[i].End - AlarmList[i].Start).TotalHours > 0)
                                    {
                                        await BigDataUpdate(_ip, AlarmList[i].Content, AlarmList[i].Start.ToString(), _class, _faulttime);
                                    }
                                }
                            } 
                        }
                    }
                    #endregion
                }
                catch
                { }
                #endregion
                #region 耗材
                if (MWorksheet != null)
                {
                    Mdt.Clear();
                    for (int i = 3; i <= MWorksheet.Dimension.End.Row; i++)
                    {
                        DataRow dr = Mdt.NewRow();
                        for (int j = 1; j <= MWorksheet.Dimension.End.Column; j++)
                        {
                            dr[j - 1] = MWorksheet.Cells[i, j].Value;
                        }
                        Mdt.Rows.Add(dr);
                    }
                    //0:耗材正常
                    //1:耗材预警
                    //2:耗材异常
                    MaterialStatus = 0;
                    for (int i = 3; i <= MWorksheet.Dimension.End.Row; i++)
                    {
                        try
                        {
                            if (Convert.ToInt32(MWorksheet.Cells[i, 6].Value) > Convert.ToInt32(MWorksheet.Cells[i, 4].Value))
                            {
                                MatetialMessage.Text = (string)MWorksheet.Cells[i, 1].Value + "," + (string)MWorksheet.Cells[i, 3].Value + " 使用寿命到达上限";
                                MaterialStatus = 2;
                                break;
                            }
                            else
                            {
                                if (Convert.ToInt32(MWorksheet.Cells[i, 6].Value) > Convert.ToInt32(MWorksheet.Cells[i, 5].Value))
                                {
                                    MatetialMessage.Text = (string)MWorksheet.Cells[i, 1].Value + "," + (string)MWorksheet.Cells[i, 3].Value + " 使用寿命预警";
                                    MaterialStatus = 1;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MatetialMessage.Text = ex.Message;
                            MaterialStatus = 2;
                            AddMessage(ex.Message);
                        }

                    }
                    switch (MaterialStatus)
                    {
                        case 1:
                            MatetialTextGrid.Background = Brushes.Violet;
                            MatetialTextGrid.Visibility = Visibility.Visible;
                            break;
                        case 2:
                            MatetialTextGrid.Background = Brushes.Red;
                            MatetialTextGrid.Visibility = Visibility.Visible;
                            break;
                        default:
                            MatetialTextGrid.Visibility = Visibility.Collapsed;
                            break;
                    }
                    if (haocaisavetimes++ > 10)
                    {
                        haocaisavetimes = 0;
                        try
                        {
                            MPackage.Save();
                        }
                        catch (Exception ex)
                        {
                            AddMessage(ex.Message);
                        }
                    }
                }
                #endregion
                SWms = sw.ElapsedMilliseconds;
            }
        }
        void IORun()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(10);
                foreach (int[] item in ExIoIn)//雷赛卡的输入
                {
                    switch (item[1])
                    {
                        case 0:
                            epsonRC90.Rc90In[item[0]] = ls.Input[item[2]];
                            break;
                        case 1:
                            epsonRC90.Rc90In[item[0]] = ls.Input[item[2] + 32];
                            break;
                        case 2:
                            epsonRC90.Rc90In[item[0]] = ls.Input[item[2] + 64];
                            break;
                        default:
                            break;
                    }

                }
                foreach (int[] item in ExIoOut)
                {
                    switch (item[1])
                    {
                        case 0:
                            ls.Output[item[2]] = epsonRC90.Rc90Out[item[0]];
                            break;
                        case 1:
                            ls.Output[item[2] + 32] = epsonRC90.Rc90Out[item[0]];
                            break;
                        case 2:
                            ls.Output[item[2] + 64] = epsonRC90.Rc90Out[item[0]];
                            break;
                        default:
                            break;
                    }
                }
                foreach (int[] item in ExIoIn2)//雷赛卡的输入
                {
                    switch (item[1])
                    {
                        case 0:
                            epsonRC90_2.Rc90In[item[0]] = ls.Input[item[2]];
                            break;
                        case 1:
                            epsonRC90_2.Rc90In[item[0]] = ls.Input[item[2] + 32];
                            break;
                        case 2:
                            epsonRC90_2.Rc90In[item[0]] = ls.Input[item[2] + 64];
                            break;
                        default:
                            break;
                    }

                }
                foreach (int[] item in ExIoOut2)
                {
                    switch (item[1])
                    {
                        case 0:
                            ls.Output[item[2]] = epsonRC90_2.Rc90Out[item[0]];
                            break;
                        case 1:
                            ls.Output[item[2] + 32] = epsonRC90_2.Rc90Out[item[0]];
                            break;
                        case 2:
                            ls.Output[item[2] + 64] = epsonRC90_2.Rc90Out[item[0]];
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        string GetIp()
        {
            string ipstring = "127.0.0.1";
            string hostName = Dns.GetHostName();
            System.Net.IPAddress[] addressList = Dns.GetHostAddresses(hostName);//会返回所有地址，包括IPv4和IPv6 
            foreach (var item in addressList)
            {
                ipstring = item.ToString();
                string[] ss = ipstring.Split(new string[] { "." }, StringSplitOptions.None);
                if (ss.Length == 4 && ss[0] == "10")
                {
                    return ipstring;
                }
            }
            return "127.0.0.1";
        }
        private async Task BigDataInsert(string COMPUTERIP, string MACID, string LINEID, string PARTNUM, string CLASS, string FAULTID, string FAULTSTARTTIME, string FAULTTIME)
        {
            int result = await Task.Run<int>(() =>
            {
                try
                {
                    string _TDate;
                    if (DateTime.Now.Hour < 8)
                    {
                        _TDate = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
                    }
                    else
                    {
                        _TDate = DateTime.Now.ToString("yyyyMMdd");
                    }

                    string StrMySQL = "Server=10.89.164.62;Database=dcdb;Uid=dcu;Pwd=dcudata;pooling=false;CharSet=utf8;port=3306";
                    string stm = "insert into TED_FAULT_DATA (WORKSTATION,COMPUTERIP,MACID,LINEID,PARTNUM,TDATE,TTIME,CLASS,FAULTID,FAULTSTARTTIME,FAULTTIME,REPAIRRESULT,REPAIRER,FL01) VALUES ('JASPER','"
    + COMPUTERIP + "','" + MACID + "','" + LINEID + "','" + PARTNUM + "','" + _TDate + "','" + DateTime.Now.ToString("HHmmss") + "','"
    + CLASS + "','" + FAULTID + "','" + FAULTSTARTTIME + "','" + FAULTTIME + "','NA','NA','ON')";
                    Mysql mysql = new Mysql();
                    int res = -1;
                    if (mysql.Connect(StrMySQL))
                    {
                        res = mysql.executeQuery(stm);
                    }
                    mysql.DisConnect();
                    return res;
                }
                catch (Exception ex)
                {
                    return -999;
                }
            });
            AddMessage("上传报警" + result.ToString());
        }
        private async Task BigDataUpdate(string ip, string content, string starttime, string _class, string faulttime)
        {
            int result = await Task.Run<int>(() =>
            {
                try
                {
                    string StrMySQL = "Server=10.89.164.62;Database=dcdb;Uid=dcu;Pwd=dcudata;pooling=false;CharSet=utf8;port=3306";

                    string stm = "update TED_FAULT_DATA SET CLASS = '" + _class + "',FAULTTIME = '" + faulttime + "',FL01 = 'OFF' WHERE COMPUTERIP = '"
                    + ip + "' AND FAULTID = '" + content + "' AND FAULTSTARTTIME = '" + starttime + "'";
                    Mysql mysql = new Mysql();
                    int res = -1;
                    if (mysql.Connect(StrMySQL))
                    {
                        res = mysql.executeQuery(stm);
                    }
                    mysql.DisConnect();
                    return res;
                }
                catch (Exception ex)
                {
                    return -999;
                }
            });
            AddMessage("更新报警" + result.ToString());
        }
        private string GetBanci()
        {
            string rs = "";
            if (DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20)
            {
                rs += DateTime.Now.ToString("yyyyMMdd") + "Day";
            }
            else
            {
                if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 8)
                {
                    rs += DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + "Night";
                }
                else
                {
                    rs += DateTime.Now.ToString("yyyyMMdd") + "Night";
                }
            }
            return rs;
        }
        private void WriteMachineData()
        {
            string excelpath = @"D:\JasperMachineData.xlsx";

            try
            {
                FileInfo fileInfo = new FileInfo(excelpath);
                if (!File.Exists(excelpath))
                {
                    using (ExcelPackage package = new ExcelPackage(fileInfo))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Machine1Data");
                        worksheet.Cells[1, 1].Value = "日期时间";
                        for (int i = 0; i < 8; i++)
                        {
                            worksheet.Cells[1, 2 + i * 3].Value = "穴" + (i + 1).ToString() + "测试数";
                            worksheet.Cells[1, 3 + i * 3].Value = "穴" + (i + 1).ToString() + "良品数";
                            worksheet.Cells[1, 4 + i * 3].Value = "穴" + (i + 1).ToString() + "良率";
                        }
                        ExcelWorksheet worksheet1 = package.Workbook.Worksheets.Add("Machine2Data");
                        worksheet1.Cells[1, 1].Value = "日期时间";
                        for (int i = 0; i < 8; i++)
                        {
                            worksheet1.Cells[1, 2 + i * 3].Value = "穴" + (i + 1).ToString() + "测试数";
                            worksheet1.Cells[1, 3 + i * 3].Value = "穴" + (i + 1).ToString() + "良品数";
                            worksheet1.Cells[1, 4 + i * 3].Value = "穴" + (i + 1).ToString() + "良率";
                        }

                        package.Save();
                    }
                }


                using (ExcelPackage package = new ExcelPackage(fileInfo))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                    int newrow = worksheet.Dimension.End.Row + 1;
                    worksheet.Cells[newrow, 1].Value = System.DateTime.Now.ToString();
                    for (int i = 0; i < 8; i++)
                    {
                        worksheet.Cells[newrow, 2 + i * 3].Value = epsonRC90.jasperTester.TestCount[i];
                        worksheet.Cells[newrow, 3 + i * 3].Value = epsonRC90.jasperTester.PassCount[i];
                        worksheet.Cells[newrow, 4 + i * 3].Value = epsonRC90.jasperTester.Yield[i];
                    }
                    ExcelWorksheet worksheet1 = package.Workbook.Worksheets[2];
                    newrow = worksheet1.Dimension.End.Row + 1;
                    worksheet1.Cells[newrow, 1].Value = System.DateTime.Now.ToString();
                    for (int i = 0; i < 8; i++)
                    {
                        worksheet1.Cells[newrow, 2 + i * 3].Value = epsonRC90_2.jasperTester.TestCount[i];
                        worksheet1.Cells[newrow, 3 + i * 3].Value = epsonRC90_2.jasperTester.PassCount[i];
                        worksheet1.Cells[newrow, 4 + i * 3].Value = epsonRC90_2.jasperTester.Yield[i];
                    }
                    package.Save();
                }
                AddMessage("保存机台生产数据完成");
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }
        #endregion
        #region 事件函数
        private void ModelPrintEventProcess(string str)
        {
            AddMessage(str);
            #region 样本
            if (str.Contains("EndSample"))
            {
                if (str.Contains("第1台"))
                {
                    lastSam1 = DateTime.Now;
                    Inifile.INIWriteValue(iniParameterPath, "Sample", "LastSam1", lastSam1.ToString());
                    this.Dispatcher.Invoke(new Action(() => {
                        LastSam1.Text = lastSam1.ToString();
                    }));
                }
                if (str.Contains("第2台"))
                {
                    lastSam2 = DateTime.Now;
                    Inifile.INIWriteValue(iniParameterPath, "Sample", "LastSam2", lastSam2.ToString());
                    this.Dispatcher.Invoke(new Action(() => {
                        LastSam2.Text = lastSam2.ToString();
                    }));

                }

            }
            if (str.Contains("AskSample"))
            {
                if (str.Contains("第1台"))
                {
                    this.Dispatcher.Invoke(new Action(async () => {
                        if ((DateTime.Now - SamStartDatetime1).TotalSeconds > 0 && IsSam.IsChecked.Value)
                        {
                            await epsonRC90.TestSentNet.SendAsync("SampleTest;OK");
                            if (epsonRC90.TestSendStatus)
                            {
                                await epsonRC90.TestSentNet.SendAsync("StartSample");
                            }
                        }
                        else
                        {
                            await epsonRC90.TestSentNet.SendAsync("SampleTest;NG");
                        }
                    }));

                }
                if (str.Contains("第2台"))
                {
                    this.Dispatcher.Invoke(new Action(async () => {
                        if ((DateTime.Now - SamStartDatetime2).TotalSeconds > 0 && IsSam.IsChecked.Value)
                        {
                            await epsonRC90_2.TestSentNet.SendAsync("SampleTest;OK");
                            if (epsonRC90_2.TestSendStatus)
                            {
                                await epsonRC90_2.TestSentNet.SendAsync("StartSample");
                            }
                        }
                        else
                        {
                            await epsonRC90_2.TestSentNet.SendAsync("SampleTest;NG");
                        }
                    }));

                }
            }
            #endregion
            if (str.Contains("CheckClean"))
            {
                if (str.Contains("第1台"))
                {
                    this.Dispatcher.Invoke(new Action(async () => {
                        if ((DateTime.Now - lastClean1.AddHours(2)).TotalSeconds > 0 && IsClean.IsChecked.Value)
                        {
                            await epsonRC90.TestSentNet.SendAsync("StartClean");
                            lastClean1 = DateTime.Now;
                            LastClean1.Text = lastClean1.ToString();
                            Inifile.INIWriteValue(iniParameterPath, "Clean", "LastClean1", lastClean1.ToString());
                        }
                        else
                        {
                            await epsonRC90.TestSentNet.SendAsync("EndClean");
                        }
                    }));
                }
                if (str.Contains("第2台"))
                {
                    this.Dispatcher.Invoke(new Action(async () => {
                        if ((DateTime.Now - lastClean2.AddHours(2)).TotalSeconds > 0 && IsClean.IsChecked.Value)
                        {
                            await epsonRC90_2.TestSentNet.SendAsync("StartClean");
                            lastClean2 = DateTime.Now;
                            LastClean2.Text = lastClean2.ToString();
                            Inifile.INIWriteValue(iniParameterPath, "Clean", "LastClean2", lastClean2.ToString());
                        }
                        else
                        {
                            await epsonRC90_2.TestSentNet.SendAsync("EndClean");
                        }
                    }));
                }
            }
            if (str.Contains("CheckHaocai"))
            {
                if (str.Contains("第1台"))
                {
                    if (MaterialStatus > 1)
                    {
                        epsonRC90.TestSentNet.SendAsync("CheckInfo;0");
                    }
                    else
                    {
                        epsonRC90.TestSentNet.SendAsync("CheckInfo;1");
                    }
                }
                if (str.Contains("第2台"))
                {
                    if (MaterialStatus > 1)
                    {
                        epsonRC90_2.TestSentNet.SendAsync("CheckInfo;0");
                    }
                    else
                    {
                        epsonRC90_2.TestSentNet.SendAsync("CheckInfo;1");
                    }
                }
            }
            if (str.Contains("TestResult"))
            {
                if (str.Contains("第1台"))
                {
                    try
                    {
                        MWorksheet.Cells[3, 6].Value = Convert.ToInt32(MWorksheet.Cells[3, 6].Value) + 2;
                        MWorksheet.Cells[6, 6].Value = Convert.ToInt32(MWorksheet.Cells[6, 6].Value) + 1;
                        MWorksheet.Cells[7, 6].Value = Convert.ToInt32(MWorksheet.Cells[7, 6].Value) + 1;
                        MWorksheet.Cells[8, 6].Value = Convert.ToInt32(MWorksheet.Cells[8, 6].Value) + 1;
                        MWorksheet.Cells[9, 6].Value = Convert.ToInt32(MWorksheet.Cells[9, 6].Value) + 1;
                    }
                    catch (Exception ex)
                    {
                        AddMessage(ex.Message);
                    }

                }
                if (str.Contains("第2台"))
                {
                    try
                    {
                        MWorksheet.Cells[4, 6].Value = Convert.ToInt32(MWorksheet.Cells[4, 6].Value) + 2;
                        MWorksheet.Cells[10, 6].Value = Convert.ToInt32(MWorksheet.Cells[10, 6].Value) + 1;
                        MWorksheet.Cells[11, 6].Value = Convert.ToInt32(MWorksheet.Cells[11, 6].Value) + 1;
                        MWorksheet.Cells[12, 6].Value = Convert.ToInt32(MWorksheet.Cells[12, 6].Value) + 1;
                        MWorksheet.Cells[13, 6].Value = Convert.ToInt32(MWorksheet.Cells[13, 6].Value) + 1;
                    }
                    catch (Exception ex)
                    {
                        AddMessage(ex.Message);
                    }

                }
            }
        }
        private void EpsonStatusUpdateProcess(string str)
        {
            EpsonStatusAuto = str[2] == '1';
            EpsonStatusWarning = str[3] == '1';
            EpsonStatusSError = str[4] == '1';
            EpsonStatusSafeGuard = str[5] == '1';
            EpsonStatusEStop = str[6] == '1';
            EpsonStatusError = str[7] == '1';
            EpsonStatusPaused = str[8] == '1';
            EpsonStatusRunning = str[9] == '1';
            EpsonStatusReady = str[10] == '1';
        }
        private void EpsonStatusUpdateProcess2(string str)
        {
            EpsonStatusAuto2 = str[2] == '1';
            EpsonStatusWarning2 = str[3] == '1';
            EpsonStatusSError2 = str[4] == '1';
            EpsonStatusSafeGuard2 = str[5] == '1';
            EpsonStatusEStop2 = str[6] == '1';
            EpsonStatusError2 = str[7] == '1';
            EpsonStatusPaused2 = str[8] == '1';
            EpsonStatusRunning2 = str[9] == '1';
            EpsonStatusReady2 = str[10] == '1';
        }
        #endregion

        private void HomePageSelect(object sender, RoutedEventArgs e)
        {
            HomePage.Visibility = Visibility.Visible;
            ParameterPage.Visibility = Visibility.Collapsed;
            CameraPage.Visibility = Visibility.Collapsed;
            MatetialPage.Visibility = Visibility.Collapsed;
        }

        private void ParameterPageSelect(object sender, RoutedEventArgs e)
        {
            HomePage.Visibility = Visibility.Collapsed;
            ParameterPage.Visibility = Visibility.Visible;
            CameraPage.Visibility = Visibility.Collapsed;
            MatetialPage.Visibility = Visibility.Collapsed;
        }
        private void CameraPageSelect(object sender, RoutedEventArgs e)
        {
            HomePage.Visibility = Visibility.Collapsed;
            ParameterPage.Visibility = Visibility.Collapsed;
            CameraPage.Visibility = Visibility.Visible;
            MatetialPage.Visibility = Visibility.Collapsed;
        }

        private void SaveParameterButtonClick(object sender, RoutedEventArgs e)
        {
            Inifile.INIWriteValue(iniParameterPath, "System", "MachineID", MachineID.Text);
            Inifile.INIWriteValue(iniParameterPath, "System", "MachineID2", MachineID2.Text);
            Inifile.INIWriteValue(iniParameterPath, "System", "线体", 线体.Text);
            Inifile.INIWriteValue(iniParameterPath, "System", "测试料号", 测试料号.Text);
            try
            {
                int.Parse(SamLimitCount.Text);
            }
            catch
            {
                SamLimitCount.Text = "999";
            }
            Inifile.INIWriteValue(iniParameterPath, "System", "SamLimitCount", SamLimitCount.Text);
        }

        private void MsgTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MsgTextBox.ScrollToEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private async void EpsonStartButtonClick(object sender, RoutedEventArgs e)
        {
            if (epsonRC90.CtrlStatus && EpsonStatusReady && !EpsonStatusEStop)
            {
                await epsonRC90.CtrlNet.SendAsync("$start,0");
            }
        }

        private async void EpsonPauseButtonClick(object sender, RoutedEventArgs e)
        {
            if (epsonRC90.CtrlStatus)
            {
                await epsonRC90.CtrlNet.SendAsync("$pause");
            }
        }

        private async void EpsonContinueButtonClick(object sender, RoutedEventArgs e)
        {
            if (epsonRC90.CtrlStatus)
            {
                await epsonRC90.CtrlNet.SendAsync("$continue");
            }
        }

        private async void EpsonReStartButtonClick(object sender, RoutedEventArgs e)
        {
            bool r = System.Windows.MessageBox.Show("确定重启机械手?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
            if (epsonRC90.CtrlStatus && r)
            {
                await epsonRC90.CtrlNet.SendAsync("$stop");
                await Task.Delay(300);
                await epsonRC90.CtrlNet.SendAsync("$SetMotorOff,1");
                await Task.Delay(400);
                await epsonRC90.CtrlNet.SendAsync("$reset");
            }
        }

        private void FuncButtonClick(object sender, RoutedEventArgs e)
        {
            //epsonRC90.BottomScanGetBarCodeCallback("G5Y936600AZP2CQ1S");
            //try
            //{
            //    GlobalVars.GetImage();
            //    GlobalVars.GetBarcode();
            //}
            //catch (Exception ex)
            //{
            //    AddMessage(ex.Message);
            //}
            //GlobalVars.Scan1.GetBarCode((string str) => { AddMessage(str); });
            ////GlobalVars.Fx5u.WriteMultW("D1000", new short[3] { 1, 2, 3 });
            //var A = GlobalVars.Fx5u.ReadMultiW("D1000",2);
            //AddMessage(A[0].ToString() + " " + A[1].ToString());
            ////GlobalVars.Fx5u.WriteD("D1000",256);
            //if (!Directory.Exists("D:\\样本测试\\" + DateTime.Now.ToString("yyyyMMdd")))
            //{
            //    Directory.CreateDirectory("D:\\样本测试\\" + DateTime.Now.ToString("yyyyMMdd"));
            //} 
            //bool[] aa = new bool[] { epsonRC90.Rc90Out[49], epsonRC90_2.Rc90Out[49], epsonRC90.Rc90Out[50], epsonRC90_2.Rc90Out[50], epsonRC90.Rc90Out[51], epsonRC90_2.Rc90Out[51], epsonRC90.Rc90Out[52], epsonRC90_2.Rc90Out[52] };
            //Yield1_8.Yield = 99;
            //epsonRC90.CheckSam();
        }

        private void GrapButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GlobalVars.GetBarcode();
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }

        }

        private void DrawRec1_Click(object sender, RoutedEventArgs e)
        {
            HTuple row1, column1, row2, column2;
            GlobalVars.viewController1.viewPort.HalconWindow.SetColor("red");
            HOperatorSet.DrawRectangle1(GlobalVars.viewController1.viewPort.HalconWindow,out row1,out column1,out row2,out column2);
            HObject rectangle;
            HOperatorSet.GenRectangle1(out rectangle, row1, column1, row2, column2);
            GlobalVars.viewController1.addIconicVar(rectangle);
            GlobalVars.viewController1.repaint();
            rectangle.WriteObject(System.Environment.CurrentDirectory + "\\rectangle1.hobj");
        }

        private void DrawRec2_Click(object sender, RoutedEventArgs e)
        {
            HTuple row1, column1, row2, column2;
            GlobalVars.viewController1.viewPort.HalconWindow.SetColor("red");
            HOperatorSet.DrawRectangle1(GlobalVars.viewController1.viewPort.HalconWindow, out row1, out column1, out row2, out column2);
            HObject rectangle;
            HOperatorSet.GenRectangle1(out rectangle, row1, column1, row2, column2);
            GlobalVars.viewController1.addIconicVar(rectangle);
            GlobalVars.viewController1.repaint();
            rectangle.WriteObject(System.Environment.CurrentDirectory + "\\rectangle2.hobj");
        }

        private void GrapImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GlobalVars.GetImage();
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }

        private async void EpsonStartButton2Click(object sender, RoutedEventArgs e)
        {
            if (epsonRC90_2.CtrlStatus && EpsonStatusReady2 && !EpsonStatusEStop2)
            {
                await epsonRC90_2.CtrlNet.SendAsync("$start,0");
            }
        }

        private async void EpsonPauseButton2Click(object sender, RoutedEventArgs e)
        {
            if (epsonRC90_2.CtrlStatus)
            {
                await epsonRC90_2.CtrlNet.SendAsync("$pause");
            }
        }

        private async void EpsonContinueButton2Click(object sender, RoutedEventArgs e)
        {
            if (epsonRC90_2.CtrlStatus)
            {
                await epsonRC90_2.CtrlNet.SendAsync("$continue");
            }
        }

        private void GrapImage_Click2(object sender, RoutedEventArgs e)
        {
            try
            {
                GlobalVars.GetImage2();
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }

        private void GrapButton_Click2(object sender, RoutedEventArgs e)
        {
            try
            {
                GlobalVars.GetBarcode2();
            }
            catch (Exception ex)
            {
                AddMessage(ex.Message);
            }
        }

        private void IsReTest_Checked(object sender, RoutedEventArgs e)
        {
            EpsonRC90.IsRetestMode = true;
        }

        private void IsReTest_Unchecked(object sender, RoutedEventArgs e)
        {
            EpsonRC90.IsRetestMode = false;
        }

        private void MaterialPageSelect(object sender, RoutedEventArgs e)
        {
            HomePage.Visibility = Visibility.Collapsed;
            ParameterPage.Visibility = Visibility.Collapsed;
            CameraPage.Visibility = Visibility.Collapsed;
            MatetialPage.Visibility = Visibility.Visible;
        }

        private void MaterialButtonClick(object sender, RoutedEventArgs e)
        {
            bool r = System.Windows.MessageBox.Show("确认更换物料?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
            if (r)
            {
                try
                {
                    MWorksheet.Cells[MaterialComboxBox.SelectedIndex + 3, 9].Value = Convert.ToInt32(MWorksheet.Cells[MaterialComboxBox.SelectedIndex + 3, 9].Value) + 1;
                    MWorksheet.Cells[MaterialComboxBox.SelectedIndex + 3, 7].Value = MWorksheet.Cells[MaterialComboxBox.SelectedIndex + 3, 8].Value;
                    MWorksheet.Cells[MaterialComboxBox.SelectedIndex + 3, 8].Value = System.DateTime.Now.ToString();
                    MWorksheet.Cells[MaterialComboxBox.SelectedIndex + 3, 6].Value = 0;
                }
                catch (Exception ex)
                {
                    AddMessage(ex.Message);
                }
            }
        }

        private void SingleBarcodeModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SystemBarcodeModeCheckBox.IsChecked = false;
            Inifile.INIWriteValue(iniParameterPath, "System", "SystemBarcodeMode", "0");
            Inifile.INIWriteValue(iniParameterPath, "System", "SingleBarcodeMode", "1");
        }

        private void SingleBarcodeModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SystemBarcodeModeCheckBox.IsChecked = true;
            Inifile.INIWriteValue(iniParameterPath, "System", "SystemBarcodeMode", "1");
            Inifile.INIWriteValue(iniParameterPath, "System", "SingleBarcodeMode", "0");
        }

        private void SystemBarcodeModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SingleBarcodeModeCheckBox.IsChecked = false;
            Inifile.INIWriteValue(iniParameterPath, "System", "SystemBarcodeMode", "1");
            Inifile.INIWriteValue(iniParameterPath, "System", "SingleBarcodeMode", "0");
        }

        private void SystemBarcodeModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SingleBarcodeModeCheckBox.IsChecked = true;
            Inifile.INIWriteValue(iniParameterPath, "System", "SystemBarcodeMode", "0");
            Inifile.INIWriteValue(iniParameterPath, "System", "SingleBarcodeMode", "1");
        }

        private void ReadImage_Click2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image文件(*.bmp;*.jpg)|*.bmp;*.jpg|所有文件|*.*";
            ofd.ValidateNames = true;
            ofd.CheckPathExists = true;
            ofd.CheckFileExists = true;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string strFileName = ofd.FileName;
                GlobalVars.Camera2.ReadImage(strFileName);
                GlobalVars.viewController2.addIconicVar(GlobalVars.Camera2.CurrentImage);
                GlobalVars.viewController2.repaint();
            }
        }

        private async void StartSamClick2(object sender, RoutedEventArgs e)
        {
            bool r = System.Windows.MessageBox.Show("确定开始2样本测试?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
            if (r)
            {
                if (epsonRC90_2.TestSendStatus)
                {
                    await epsonRC90_2.TestSentNet.SendAsync("StartSample");
                    AddMessage("Robote2 StartSample");
                }
            }
        }

        private async void AlarmButton_Click(object sender, RoutedEventArgs e)
        {
            AlarmButton.IsEnabled = false;
            await Task.Run(() => {
                try
                {
                    if (!Directory.Exists("D:\\报警记录\\" + DateTime.Now.ToString("yyyyMMdd")))
                    {
                        Directory.CreateDirectory("D:\\报警记录\\" + DateTime.Now.ToString("yyyyMMdd"));
                    }
                    string path = "D:\\报警记录\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "AlarmSimple.csv";
                    Csvfile.savetocsv(path, new string[] { "Content", "Count", "Time(min)" });
                    string _class = DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20 ? "D" : "N";
                    string _ip = GetIp();
                    string _date;
                    if (DateTime.Now.Hour < 8)
                    {
                        _date = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
                    }
                    else
                    {
                        _date = DateTime.Now.ToString("yyyyMMdd");
                    }

                    int alarmcount = 0; float alarmelapsed = 0;
                    foreach (var item in AlarmList)
                    {
                        string StrMySQL = "Server=10.89.164.62;Database=dcdb;Uid=dcu;Pwd=dcudata;pooling=false;CharSet=utf8;port=3306";
                        string stm = "SELECT * FROM TED_FAULT_DATA WHERE COMPUTERIP ='" + _ip + "' AND FAULTID = '" + item.Content +
    "' AND TDATE = '" + _date + "' AND CLASS = '" + _class + "' AND FL01 = '" + "OFF'";
                        Mysql mysql = new Mysql();
                        if (mysql.Connect(StrMySQL))
                        {
                            DataSet ds = mysql.Select(stm);
                            DataTable dt = ds.Tables["table0"];
                            if (dt.Rows.Count > 0)
                            {
                                int i = 0;
                                float elapsed = 0;
                                foreach (DataRow datarow in dt.Rows)
                                {
                                    try
                                    {
                                        elapsed += float.Parse((string)datarow["FAULTTIME"]);
                                    }
                                    catch
                                    { }
                                    i++;
                                }
                                if (i > 0)
                                {
                                    alarmcount += i;
                                    alarmelapsed += elapsed;
                                    Csvfile.savetocsv(path, new string[] { item.Content, i.ToString(), elapsed.ToString("F1") });
                                }
                            }
                        }
                        mysql.DisConnect();
                    }
                    Process process1 = new Process();
                    process1.StartInfo.FileName = path;
                    process1.StartInfo.Arguments = "";
                    process1.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    process1.Start();
                }
                catch (Exception ex)
                {
                    AddMessage(ex.Message);
                }
            });
            await Task.Run(() => {
                try
                {
                    string path = "D:\\报警记录\\" + DateTime.Now.ToString("yyyyMMdd") + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "AlarmTotal.csv";
                    string _class = DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20 ? "D" : "N";
                    string _ip = GetIp();
                    string _date;
                    if (DateTime.Now.Hour < 8)
                    {
                        _date = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
                    }
                    else
                    {
                        _date = DateTime.Now.ToString("yyyyMMdd");
                    }
                    string StrMySQL = "Server=10.89.164.62;Database=dcdb;Uid=dcu;Pwd=dcudata;pooling=false;CharSet=utf8;port=3306";
                    string stm = "SELECT * FROM TED_FAULT_DATA WHERE COMPUTERIP ='" + _ip +
                            "' AND TDATE = '" + _date + "' AND CLASS = '" + _class + "' AND FL01 = '" + "OFF'";

                    Mysql mysql = new Mysql();
                    if (mysql.Connect(StrMySQL))
                    {
                        DataSet ds = mysql.Select(stm);
                        DataTable dt = ds.Tables["table0"];
                        if (dt.Rows.Count > 0)
                        {
                            string strHead = DateTime.Now.ToString("yyyyMMddHHmmss") + "AlarmTotal";
                            string strColumns = "";
                            for (int i = 0; i < dt.Columns.Count; i++)
                            {
                                strColumns += dt.Columns[i].ColumnName + ",";
                            }
                            strColumns = strColumns.Substring(0, strColumns.Length - 1);
                            Csvfile.dt2csv(dt, path, strHead, strColumns);

                            Process process1 = new Process();
                            process1.StartInfo.FileName = path;
                            process1.StartInfo.Arguments = "";
                            process1.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                            process1.Start();
                        }
                    }
                    mysql.DisConnect();
                }
                catch (Exception ex)
                {
                    AddMessage(ex.Message);
                }
            });
            AlarmButton.IsEnabled = true;
        }

        private void ReadImage_Click1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image文件(*.bmp;*.jpg)|*.bmp;*.jpg|所有文件|*.*";
            ofd.ValidateNames = true;
            ofd.CheckPathExists = true;
            ofd.CheckFileExists = true;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string strFileName = ofd.FileName;
                GlobalVars.Camera.ReadImage(strFileName);
                GlobalVars.viewController1.addIconicVar(GlobalVars.Camera.CurrentImage);
                GlobalVars.viewController1.repaint();
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "图片(*.bmp)|*.bmp";
            if (saveDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GlobalVars.Camera.SaveImage("bmp",saveDlg.FileName);
            }
        }

        private void SaveImage_Click2(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "图片(*.bmp)|*.bmp";
            if (saveDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GlobalVars.Camera2.SaveImage("bmp", saveDlg.FileName);
            }
        }

        //private void SaveSamItem(object sender, RoutedEventArgs e)
        //{
        //    Inifile.INIWriteValue(iniParameterPath, "Sample", "SamItem1", SamItem1.Text);
        //    Inifile.INIWriteValue(iniParameterPath, "Sample", "SamItem2", SamItem2.Text);
        //    Inifile.INIWriteValue(iniParameterPath, "Sample", "SamItem3", SamItem3.Text);
        //    Inifile.INIWriteValue(iniParameterPath, "Sample", "SamItem4", SamItem4.Text);
        //    Inifile.INIWriteValue(iniParameterPath, "Sample", "IsSam", IsSam.IsChecked.Value ? "1" : "0");
        //    Inifile.INIWriteValue(iniParameterPath, "Clean", "IsClean", IsClean.IsChecked.Value ? "1" : "0");
        //}

        private async void StartSamClick(object sender, RoutedEventArgs e)
        {
            bool r = System.Windows.MessageBox.Show("确定开始1样本测试?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
            if (r)
            {
                if (epsonRC90.TestSendStatus)
                {
                    await epsonRC90.TestSentNet.SendAsync("StartSample");
                    AddMessage("Robote1 StartSample");
                }
            }
        }

        private void DrawRec1_Click2(object sender, RoutedEventArgs e)
        {
            HTuple row1, column1, row2, column2;
            GlobalVars.viewController2.viewPort.HalconWindow.SetColor("red");
            HOperatorSet.DrawRectangle1(GlobalVars.viewController2.viewPort.HalconWindow, out row1, out column1, out row2, out column2);
            HObject rectangle;
            HOperatorSet.GenRectangle1(out rectangle, row1, column1, row2, column2);
            GlobalVars.viewController2.addIconicVar(rectangle);
            GlobalVars.viewController2.repaint();
            rectangle.WriteObject(System.Environment.CurrentDirectory + "\\rectangle3.hobj");
        }

        private void DrawRec2_Click2(object sender, RoutedEventArgs e)
        {
            HTuple row1, column1, row2, column2;
            GlobalVars.viewController2.viewPort.HalconWindow.SetColor("red");
            HOperatorSet.DrawRectangle1(GlobalVars.viewController2.viewPort.HalconWindow, out row1, out column1, out row2, out column2);
            HObject rectangle;
            HOperatorSet.GenRectangle1(out rectangle, row1, column1, row2, column2);
            GlobalVars.viewController2.addIconicVar(rectangle);
            GlobalVars.viewController2.repaint();
            rectangle.WriteObject(System.Environment.CurrentDirectory + "\\rectangle4.hobj");
        }

        private async void EpsonReStartButton2Click(object sender, RoutedEventArgs e)
        {
            bool r = System.Windows.MessageBox.Show("确定重启机械手?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
            if (epsonRC90_2.CtrlStatus && r)
            {
                await epsonRC90_2.CtrlNet.SendAsync("$stop");
                await Task.Delay(300);
                await epsonRC90_2.CtrlNet.SendAsync("$SetMotorOff,1");
                await Task.Delay(400);
                await epsonRC90_2.CtrlNet.SendAsync("$reset");
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {


            await Task.Delay(1);
            CameraPage.Visibility = Visibility.Visible;
            LoadedCallback();
            CameraPage.Visibility = Visibility.Collapsed;


        }
    }
    class AlarmData
    {
        public string Code { set; get; }
        public string Content { set; get; }
        public DateTime Start { set; get; }
        public DateTime End { set; get; }
        public bool State { set; get; }
    }
}
