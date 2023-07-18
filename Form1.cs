using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using LibreHardwareMonitor;
using LibreHardwareMonitor.Hardware;

namespace HardMon
{
    public partial class Form1 : Form
    {
        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        private Computer computer;
        private string key = string.Empty;

        public Form1()
        {
            InitializeComponent();
        }

        private void GetStaticHardwareInfo(string key, ListView list)
        {
            list.Items.Clear();

            ManagementObjectSearcher searcher = 
                new ManagementObjectSearcher("SELECT * FROM " + key);

            try
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    ListViewGroup listViewGroup;

                    try
                    {
                        listViewGroup = list.Groups.Add(obj["Name"].ToString(),
                                                        obj["Name"].ToString());
                    }
                    catch (Exception ex)
                    {
                        listViewGroup = list.Groups.Add(obj.ToString(), obj.ToString());
                    }

                    if (obj.Properties.Count == 0)
                    {
                        MessageBox.Show("Не удалось получить информацию",
                                        "Ошибка",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        return;
                    }

                    foreach (PropertyData data in obj.Properties)
                    {
                        ListViewItem item = new ListViewItem(listViewGroup);

                        if (list.Items.Count % 2 != 0)
                            item.BackColor = Color.White;
                        else item.BackColor = Color.WhiteSmoke;

                        item.Text = data.Name;

                        if (data.Value != null && !string.IsNullOrEmpty(data.Value.ToString()))
                        {
                            switch (data.Value.GetType().ToString()) {
                                case "System.String[]":
                                    string[] stringData = data.Value as string[];
                                    string resStr = string.Empty;
                                    foreach (string s in stringData)
                                        resStr += $"{s} ";
                                    item.SubItems.Add(resStr);
                                    break;
                                case "System.UInt16[]":
                                    ushort[] ushortData = data.Value as ushort[];
                                    string resStr2 = string.Empty;
                                    foreach (ushort u in ushortData)
                                        resStr2 += $"{Convert.ToString(u)} ";
                                    item.SubItems.Add(resStr2);
                                    break;
                                default:
                                    item.SubItems.Add(data.Value.ToString());
                                    break;
                            }
                            list.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message,
                                "Ошибка",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private void GetDynamicHardwareInfo(string key)
        {
            List<HardwareType> hwType = new List<HardwareType>();
            string info = string.Empty;
            switch (key)
            {
                case "Win32_Processor":
                    hwType.Add(HardwareType.Cpu);
                    break;
                case "Win32_VideoController":
                    hwType.Add(HardwareType.GpuIntel);
                    hwType.Add(HardwareType.GpuNvidia);
                    hwType.Add(HardwareType.GpuAmd);
                    break;
                case "Win32_PhysicalMemory":
                    hwType.Add(HardwareType.Memory);
                break;
                case "Win32_DiskDrive":
                    hwType.Add(HardwareType.Storage);
                    break;
                case "Win32_NetworkAdapter":
                    hwType.Add(HardwareType.Network);
                    break;
                default:
                    return;
            }
            foreach (HardwareType hwtype in hwType) {
                foreach (IHardware hardware in computer.Hardware)
                {
                    if (hardware.HardwareType != hwtype)
                        continue;
                    info += "\t" + hardware.Name + "\n";
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        info += $"{sensor.Name}    {sensor.SensorType}: {sensor.Value}\n";
                    }
                    info += "\n";
                }
            }
            richTextBox1.Text = info;
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            key = string.Empty;
            switch (toolStripComboBox1.SelectedItem.ToString())
            {
                case "CPU":
                    key = "Win32_Processor";
                    break;
                case "GPU":
                    key = "Win32_VideoController";
                    break;
                case "Battery":
                    key = "Win32_Battery";
                    break;
                case "RAM":
                    key = "Win32_PhysicalMemory";
                    break;
                case "Storage":
                    key = "Win32_DiskDrive";
                    break;
                case "Cache":
                    key = "Win32_CacheMemory";
                    break;
                case "Network adapter":
                    key = "Win32_NetworkAdapter";
                    break;
                default:
                    return;
            }
            GetDynamicHardwareInfo(key);
            GetStaticHardwareInfo(key, listView1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //toolStripComboBox1.SelectedIndex = 0;
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true,
                IsNetworkEnabled = true,
                IsStorageEnabled = true
            };
            computer.Open();
            backgroundWorker1.RunWorkerAsync();
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetDynamicHardwareInfo(key);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                computer.Accept(new UpdateVisitor());
                Thread.Sleep(1000);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            computer.Close();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                timer1.Stop();
                toolStripButton1.Checked = true;
            }
            else
            {
                timer1.Start();
                toolStripButton1.Checked = false;
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (key == string.Empty) return;
            GetStaticHardwareInfo(key, listView1);
        }
    }
}
