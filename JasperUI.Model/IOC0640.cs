using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace csIOC0640
{
    public class IOC0640
    {
        [DllImport("IOC0640.dll", EntryPoint = "ioc_board_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int ioc_board_init();
        [DllImport("IOC0640.dll", EntryPoint = "ioc_board_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void ioc_board_close();

        [DllImport("IOC0640.dll", EntryPoint = "ioc_read_inbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int ioc_read_inbit(ushort cardno, ushort bitno);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_read_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int ioc_read_outbit(ushort cardno, ushort bitno);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_write_outbit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_write_outbit(ushort cardno, ushort bitno, int on_off);

        [DllImport("IOC0640.dll", EntryPoint = "ioc_read_inport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int ioc_read_inport(ushort cardno, ushort m_PortNo);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_read_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int ioc_read_outport(ushort cardno, ushort m_PortNo);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_write_outport", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_write_outport(ushort cardno, ushort m_PortNo, uint port_value);

        public delegate uint IOC0640_OPERATE(IntPtr operate_data);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_int_enable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_int_enable(ushort cardno, IOC0640_OPERATE funcIntHandler, IntPtr operate_data);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_int_disable", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_int_disable(ushort cardno);

        [DllImport("IOC0640.dll", EntryPoint = "ioc_config_intbitmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_config_intbitmode(ushort cardno, ushort bitno, ushort enable,ushort logic);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_config_intbitmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_config_intbitmode(ushort cardno, ushort bitno, ushort[] enable,ushort[] logic);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_read_intbitstatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int ioc_read_intbitstatus(ushort cardno, ushort bitno);

        [DllImport("IOC0640.dll", EntryPoint = "ioc_config_intporten", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_config_intporten(ushort cardno, ushort m_PortNo, uint port_en);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_config_intportlogic", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_config_intportlogic(ushort cardno, ushort m_PortNo, uint port_logic);
        
        [DllImport("IOC0640.dll", EntryPoint = "ioc_read_intportmode", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_read_intportmode(ushort cardno, ushort m_PortNo, uint[] enable, uint[] logic);
        [DllImport("IOC0640.dll", EntryPoint = "ioc_read_intportstatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int ioc_read_intportstatus(ushort cardno, ushort m_PortNo);

        [DllImport("IOC0640.dll", EntryPoint = "ioc_set_filter", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ioc_set_filter(ushort cardno, double filter);
 
    }
}
