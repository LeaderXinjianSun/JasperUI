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
            HTuple height, width;
            HOperatorSet.GetImageSize(Camera.CurrentImage, out width, out height);
            HOperatorSet.GenRectangle1(out Roi1, 0, 0, height / 2 - 1, width / 2 - 1);
            HOperatorSet.GenRectangle1(out Roi2, height / 2, width / 2, height, width);
            HOperatorSet.ReduceDomain(Camera.CurrentImage, Roi1, out image1);
            HOperatorSet.ReduceDomain(Camera.CurrentImage, Roi2, out image2);
            (new HImage(image1)).FindDataCode2d(DataCodeHandle, new HTuple(), new HTuple(), out ResultHandles, out DecodedDataStrings);
            res[0] = new HTuple((new HTuple(DecodedDataStrings.TupleLength())).TupleEqual(1)) == 1 ? DecodedDataStrings.TupleSelect(0).ToString() : "error";
            int[] co = GetCorinWindow(viewController1.viewPort.HalconWindow, Camera.CurrentImage, 0, 0);
            HOperatorSet.DispText(viewController1.viewPort.HalconWindow, res[0], "window", co[0], co[1], "black", "box", "true");
            (new HImage(image2)).FindDataCode2d(DataCodeHandle, new HTuple(), new HTuple(), out ResultHandles, out DecodedDataStrings);
            res[1] = new HTuple((new HTuple(DecodedDataStrings.TupleLength())).TupleEqual(1)) == 1 ? DecodedDataStrings.TupleSelect(0).ToString() : "error";
            co = GetCorinWindow(viewController1.viewPort.HalconWindow, Camera.CurrentImage, height / 2, width / 2);
            HOperatorSet.DispText(viewController1.viewPort.HalconWindow, res[1], "window", co[0], co[1], "black", "box", "true");

            image1.Dispose();
            image2.Dispose();
            Roi1.Dispose();
            Roi2.Dispose();
            DataCodeHandle.Dispose();
            return res;
        }
        public static void GetImage()
        {
            Camera.GrabImageVoid();
            viewController1.addIconicVar(GlobalVars.Camera.CurrentImage);
            viewController1.repaint();
            //GlobalVars.viewController1.viewPort.HalconWindow.SetColor
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
