using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewROI;
using BingLibrary.HVision;
using HalconDotNet;

namespace JasperUI.Model
{
    public static class GlobalVars
    {
        public static Scan UpScan;

        public static HWndCtrl viewController1;
        public static ROIController roiController1;
        public static CameraOperate Camera = new CameraOperate();

        static HTuple ResultHandles = new HTuple();
        static HTuple DecodedDataStrings = new HTuple();
        static HDataCode2D DataCodeHandle = new HDataCode2D();
        public static string[] GetBarcode()
        {
            string[] res = new string[2];
            DataCodeHandle = new HDataCode2D("Data Matrix ECC 200", new HTuple(), new HTuple());

            HObject image1, image2;
            HObject Roi1, Roi2;
            HObject symbolXLD1, symbolXLD2;
            HOperatorSet.ReadObject(out Roi1, System.Environment.CurrentDirectory + "\\rectangle1.hobj");
            HOperatorSet.ReadObject(out Roi2, System.Environment.CurrentDirectory + "\\rectangle2.hobj");
            HOperatorSet.ReduceDomain(Camera.CurrentImage, Roi1, out image1);
            HOperatorSet.ReduceDomain(Camera.CurrentImage, Roi2, out image2);
            HOperatorSet.FindDataCode2d(new HImage(image1), out symbolXLD1, DataCodeHandle, new HTuple(), new HTuple(), out ResultHandles, out DecodedDataStrings);
            res[0] = new HTuple((new HTuple(DecodedDataStrings.TupleLength())).TupleEqual(1)) == 1 ? DecodedDataStrings.TupleSelect(0).ToString().Replace("\"", "") : "error";
            HOperatorSet.FindDataCode2d(new HImage(image2), out symbolXLD2, DataCodeHandle, new HTuple(), new HTuple(), out ResultHandles, out DecodedDataStrings);
            res[1] = new HTuple((new HTuple(DecodedDataStrings.TupleLength())).TupleEqual(1)) == 1 ? DecodedDataStrings.TupleSelect(0).ToString().Replace("\"", "") : "error";

            viewController1.addIconicVar(symbolXLD1);
            viewController1.addIconicVar(symbolXLD2);
            viewController1.addIconicVar(Roi1);
            viewController1.addIconicVar(Roi2);

            viewController1.viewPort.HalconWindow.SetColor("green");
            viewController1.repaint();
            HTuple area, rows, columns;

            HOperatorSet.AreaCenter(Roi1, out area, out rows, out columns);
            int[] co = GetCorinWindow(viewController1.viewPort.HalconWindow, Camera.CurrentImage, (int)rows.D, (int)columns.D);
            HOperatorSet.DispText(viewController1.viewPort.HalconWindow, "① " + res[0], "window", co[0], co[1], "black", "box", "true");
            HOperatorSet.AreaCenter(Roi2, out area, out rows, out columns);
            co = GetCorinWindow(viewController1.viewPort.HalconWindow, Camera.CurrentImage, (int)rows.D, (int)columns.D);
            HOperatorSet.DispText(viewController1.viewPort.HalconWindow, "② " + res[1], "window", co[0], co[1], "black", "box", "true");

            image1.Dispose();
            image2.Dispose();

            DataCodeHandle.Dispose();
            return res;
        }
        public static void GetImage()
        {
            Camera.GrabImageVoid();
            viewController1.addIconicVar(GlobalVars.Camera.CurrentImage);
            viewController1.repaint();
        }
        private static int[] GetCorinWindow(HWindow window, HObject image, int r, int c)
        {
            int[] cor = new int[2];
            double H1, W1;
            HTuple width, height;
            try
            {
                HOperatorSet.GetImageSize(image, out width, out height);
                int r1,c1,h1, w1;
                window.GetWindowExtents(out r1,out c1,out w1,out h1);
                int h = h1;
                int w = w1;
                if (w / h > width.D / height.D)
                {
                    H1 = h;
                    W1 = (double)h / height * width;
                }
                else
                {
                    H1 = (double)w / width * height;
                    W1 = w;
                }
                int delta_r = r - height / 2;
                int delat_c = c - width / 2;
                int Delat_R = (int)(delta_r * H1 / height.D);//delta_r * height / (w / width * height)
                int Delta_C = (int)(delat_c * W1 / width.D);//delat_c * width / w
                cor[0] = Delat_R + h / 2 >= 0 ? Delat_R + h / 2 : 0;
                cor[1] = Delta_C + w / 2 >= 0 ? Delta_C + w / 2 : 0;
            }
            catch
            {


            }
            return cor;
        }
    }
}
