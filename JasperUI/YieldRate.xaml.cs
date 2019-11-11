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

namespace JasperUI
{
    /// <summary>
    /// YieldRate.xaml 的交互逻辑
    /// </summary>
    public partial class YieldRate : UserControl
    {
        public YieldRate()
        {
            InitializeComponent();
        }
        public int PassCount
        {
            get { return (int)GetValue(PassCountProperty); }
            set { SetValue(PassCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Result.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PassCountProperty =
            DependencyProperty.Register("PassCount", typeof(int), typeof(YieldRate), new PropertyMetadata((s, e) =>
            {
                YieldRate yieldRate = s as YieldRate;
                yieldRate.PassCountTextBlock.Text = e.NewValue.ToString();
            }));
        public double Yield
        {
            get { return (double)GetValue(YieldProperty); }
            set { SetValue(YieldProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Result.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YieldProperty =
            DependencyProperty.Register("Yield", typeof(double), typeof(YieldRate), new PropertyMetadata((s, e) =>
            {
                YieldRate yieldRate = s as YieldRate;
                double yield = 0;
                try
                {
                    yield = (double)e.NewValue;
                }
                catch
                { }
                yieldRate.YieldTextBlock.Text = yield.ToString("F1") + "%" ;
                if (yield >= 98)
                {
                    yieldRate.YieldGrid.Background = Brushes.LightBlue;
                }
                else
                {
                    if (yield >= 95)
                    {
                        yieldRate.YieldGrid.Background = Brushes.Green;
                    }
                    else
                    {
                        if (yield == 0)
                        {
                            yieldRate.YieldGrid.Background = Brushes.Transparent;
                        }
                        else
                        {
                            yieldRate.YieldGrid.Background = Brushes.Red;
                        }
                    }
                }
            }));
    }
}
