using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EmbeddedSelenium
{
    public partial class Form1 : Form
    {
        private const int TCM_SETMINTABWIDTH = 0x1300 + 49;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        public HashSet<int> listChrome = new HashSet<int>();

        public Form1()
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            SendMessage(mainTabControl.Handle, TCM_SETMINTABWIDTH, IntPtr.Zero, (IntPtr)16);
        }

        private void mainTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            var lastIndex = mainTabControl.TabCount - 1;
            if (mainTabControl.GetTabRect(lastIndex).Contains(e.Location))
            {
                new Thread(() =>
                {
                    var guid = Guid.NewGuid().ToString();
                    ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;
                    ChromeDriver driver = new ChromeDriver(service);
                    driver.ExecuteScript($"document.title = '{guid}';");

                    var driverHandle = IntPtr.Zero;
                    do
                    {
                        foreach(var item in Process.GetProcessesByName("chrome").Where(x => !string.IsNullOrEmpty(x.MainWindowTitle)).ToList())
                        {
                            if (item.MainWindowTitle.Contains(guid))
                            {
                                driverHandle = item.MainWindowHandle;
                                listChrome.Add(item.Id);
                            }
                        }
                        Thread.Sleep(10);
                    }
                    while (driverHandle == IntPtr.Zero);

                    mainTabControl.Invoke(new Action(() =>
                    {
                        mainTabControl.TabPages.Insert(lastIndex, $"New Tab {lastIndex}");
                        mainTabControl.SelectedIndex = lastIndex;
                    }));


                    TabPage lastTab = mainTabControl.TabPages[lastIndex];
                    lastTab.Invoke(new Action(() =>
                    {
                        lastTab.Text = $"Zalo {listChrome.Count}";
                        SetParent(driverHandle, lastTab.Handle);
                        MoveWindow(driverHandle, 0, 0, lastTab.Width, lastTab.Height, true);
                    }));
                    driver.Navigate().GoToUrl("https://chat.zalo.me");

                })
                { IsBackground = true }.Start();
            }


        }

        private void mainTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPageIndex == mainTabControl.TabCount - 1)
                e.Cancel = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            listChrome.ToList().ForEach(x => Process.GetProcessById(x)?.Kill());
        }
    }
}
