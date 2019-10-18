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
    /// Tester.xaml 的交互逻辑
    /// </summary>
    public partial class Tester : UserControl
    {
        public Tester()
        {
            InitializeComponent();
        }
        public string Result
        {
            get { return (string)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Result.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register("Result", typeof(string), typeof(Tester), new PropertyMetadata((s, e) =>
            {
                Tester tester = s as Tester;
                tester.ResultText.Text = e.NewValue.ToString();
                switch (e.NewValue.ToString())
                {
                    case "P":
                        tester.ResultBackGround.Background = Brushes.Green;
                        break;
                    case "F":
                        tester.ResultBackGround.Background = Brushes.Red;
                        break;
                    default:
                        tester.ResultBackGround.Background = Brushes.Gray;
                        break;
                }
            }));

    }
}
