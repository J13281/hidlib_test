using HidLibrary;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    class Program
    {
        HidDevice ds4;
        SerialPort gimx;

        static void Main(string[] args)
        {
            new Program().m1();
        }

        void m1()
        {
            gimxInit();
            Console.WriteLine("done serialInit");
            ds4Init();
            Console.WriteLine("done hardwareInit");
            communicationInit();
            Console.WriteLine("done communicationInit");

            var next = Environment.TickCount;
            var wait = 1000 / 60;

            while (true)
            {
                if (next < Environment.TickCount)
                {
                    next += wait;
                    //gimxTask();
                    ds4Task();
                }
            }
        }

        void gimxInit()
        {
            gimx = new SerialPort
            {
                PortName = "COM10",
                BaudRate = 500000,
                DtrEnable = true
            };
            gimx.DataReceived += Gimx_DataReceived;
            gimx.Open();
            while (!gimx.IsOpen) ;
        }

        void ds4Init()
        {
            var VendorId = 0x054c;
            var ProductId = 0x09cc;
            var devices = HidDevices.Enumerate(VendorId, ProductId);
            if (devices == null)
            {
                throw new Exception();
            }

            ds4 = devices.First();
        }

        void communicationInit()
        {
            var resetBytes = new byte[] { 0x55, 0x00 };
            gimx.Write(resetBytes, 0, resetBytes.Length);

            Thread.Sleep(2000);

            var startBytes = new byte[] { 0x33, 0x00 };
            gimx.Write(startBytes, 0, startBytes.Length);
        }

        void gimxTask()
        {

        }

        void Gimx_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var type = 0;
            while ((type = gimx.ReadByte()) < 0) ;
            var len = 0;
            while ((len = gimx.ReadByte()) < 0) ;

            var report = new byte[len];
            var offset = 0;
            while (offset < len)
                offset += gimx.Read(report, offset, len - offset);

            handleRead(type, len, report);
        }

        void handleRead(int type, int len, byte[] report)
        {
            Console.WriteLine($"type=0x{type:X2}, len=0x{len:X2}");
            showBytes(report, len, "gimx");

            if (type == 0x44 && report[2] == 0xF0)
            {
                report = report.Skip(8).ToArray();
                Console.WriteLine(ds4.WriteFeatureData(report));
            }
            else if (type == 0x44 && report[2] == 0xF1)
            {
                if (ds4.ReadFeatureData(out var data, 0xF1))
                {
                    showBytes(data, data.Length, "F1");
                    var cdata = new byte[] { 0x44, (byte)data.Length };
                    gimx.Write(cdata, 0, cdata.Length);
                    gimx.Write(data, 0, data.Length);
                }
            }
            else if (type == 0x44 && report[2] == 0xF2)
            {
                if (ds4.ReadFeatureData(out var data, 0xF2))
                {
                    showBytes(data, data.Length, "F2");
                    var cdata = new byte[] { 0x44, (byte)data.Length };
                    gimx.Write(cdata, 0, cdata.Length);
                    gimx.Write(data, 0, data.Length);
                }
            }
            else if (type == 0xEE)
            {
                Console.WriteLine(ds4.Write(report));
            }
        }

        void ds4Task()
        {
            var read = ds4.Read();
            if (read.Status != HidDeviceData.ReadStatus.Success) return;

            if (read.Data[0] != 0x01)
                showBytes(read.Data, read.Data.Length, "readData");

            var cdata = new byte[] { 0xFF, (byte)read.Data.Length };
            gimx.Write(cdata, 0, cdata.Length);
            gimx.Write(read.Data, 0, read.Data.Length);
        }

        void showBytes(byte[] bytes, int len, string name = null)
        {
            if (name != null)
            {
                Console.Write($"{name}->");
            }

            for (int i = 0; i < len; i++)
            {
                Console.Write($"0x{bytes[i]:X2} ");
            }

            Console.WriteLine();
        }
    }
}
