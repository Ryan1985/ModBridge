using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Modbus.Device;

namespace ModBridge
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private volatile bool runFlag = true;
        private long readByteCnt = 0;
        private string[] sourceParams;
        private string[] targetParams;
        private const int MAX_PACKAGE_LENGTH = 123;


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    while (runFlag)
                    {
                        try
                        {
                            var sourceClient = new TcpClient();
                            sourceClient.Connect(IPAddress.Parse(sourceParams[0]),
                                int.Parse(sourceParams[1]));

                            var sourceMaster =
                                ModbusIpMaster.CreateIp(sourceClient
                                    );

                            var targetClient = new TcpClient();
                            targetClient.Connect(IPAddress.Parse(targetParams[0]), int.Parse(targetParams[1]));


                            var targetMaster =
                                ModbusIpMaster.CreateIp(targetClient);

                            while (runFlag)
                            {
                                var totalLength = ushort.Parse(sourceParams[3]);
                                ushort startLengthFloat = 0;
                                do
                                {
                                    ushort byteCntToSend = totalLength;
                                    if (totalLength > MAX_PACKAGE_LENGTH)
                                    {
                                        byteCntToSend = MAX_PACKAGE_LENGTH;
                                    }

                                    var readResult =
                                        sourceMaster.ReadHoldingRegisters(
                                            (ushort) (ushort.Parse(sourceParams[2]) + startLengthFloat),
                                            byteCntToSend);
                                    targetMaster.WriteMultipleRegisters(
                                        (ushort) (ushort.Parse(targetParams[2]) + startLengthFloat), readResult);
                                    readByteCnt += readResult.Length;
                                    startLengthFloat += (ushort) readResult.Length;
                                    totalLength -= (ushort) readResult.Length;

                                    try
                                    {
                                        Invoke(new Action<long>(l =>
                                        {
                                            try
                                            {
                                                if (l == long.MaxValue)
                                                {
                                                    l = 0;
                                                }
                                                label1.Text = l.ToString();
                                                lblInfo.Text = string.Empty;
                                            }
                                            catch
                                            {

                                            }
                                        }), readByteCnt);
                                    }
                                    catch
                                    {
                                    }
                                } while (totalLength > 0);
                                Thread.Sleep(100);
                            }
                        }
                        catch (Exception ex)
                        {
                            lblInfo.Text = ex.Message;
                        }
                        Thread.Sleep(30000);
                    }
                });
                button1.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            runFlag = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            sourceParams = ConfigurationManager.AppSettings["SourceSlave"].Split(',');
            targetParams = ConfigurationManager.AppSettings["TargetSlave"].Split(',');

            label4.Text = string.Format(@"读取 {0}:{1}，起始地址：{2}，长度：{3}", sourceParams[0], sourceParams[1], sourceParams[2],
                sourceParams[3]);
            label7.Text = string.Format(@"读取 {0}:{1}，起始地址：{2}，长度：{3}", targetParams[0], targetParams[1], targetParams[2],
                targetParams[3]);


        }
    }
}
