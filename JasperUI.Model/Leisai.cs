using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using csIOC0640;

namespace JasperUI.Model
{
    public class Leisai
    {
        public bool[] Input = new bool[96];
        public bool[] Output = new bool[96];
        public int Connect()
        {
            return IOC0640.ioc_board_init();
        }
        public void Run()
        {
            while (true)
            {
                long Input1 = IOC0640.ioc_read_inport(0, 0);
                for (int i = 0; i < 32; i++)
                {
                    Input[i] = (Input1 >> i & 1) == 0;
                }
                long Input2 = IOC0640.ioc_read_inport(1, 0);
                for (int i = 0; i < 32; i++)
                {
                    Input[i + 32] = (Input2 >> i & 1) == 0;
                }
                long Input3 = IOC0640.ioc_read_inport(2, 0);
                for (int i = 0; i < 32; i++)
                {
                    Input[i + 64] = (Input3 >> i & 1) == 0;
                }
                uint Output1 = 0;
                for (int i = 0; i < 32; i++)
                {
                    Output1 += (uint)(Output[i] ? 0 : Math.Pow(2, i));
                }
                IOC0640.ioc_write_outport(0, 0, Output1);
                uint Output2 = 0;
                for (int i = 0; i < 32; i++)
                {
                    Output2 += (uint)(Output[i + 32] ? 0 : Math.Pow(2, i));
                }
                IOC0640.ioc_write_outport(1, 0, Output2);
                uint Output3 = 0;
                for (int i = 0; i < 32; i++)
                {
                    Output3 += (uint)(Output[i + 64] ? 0 : Math.Pow(2, i));
                }
                IOC0640.ioc_write_outport(2, 0, Output3);
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
