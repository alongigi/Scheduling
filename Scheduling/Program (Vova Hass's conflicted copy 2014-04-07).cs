using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;

namespace Scheduling
{
    class Program
    {
        static void Example1(OperatingSystem os)
        {
            for (int i = 0; i < 3; i++)
            {
                os.CreateProcess("a.code");
                os.CreateProcess("b.code");
            }
        }
        static void Example2(OperatingSystem os)
        {
            for (int i = 0; i < 3; i++)
            {
                os.CreateProcess("ReadFile1.code");
                os.CreateProcess("ReadFile2.code");
            }
        }
        static void Example3(OperatingSystem os)
        {
            for (int i = 0; i < 3; i++)
            {
                os.CreateProcess("c.code");
                os.CreateProcess("d.code");
            }
        }
        static void Main(string[] args)
        {
            Disk disk = new Disk();
            CPU cpu = new CPU(disk);
            cpu.Debug = true;
            OperatingSystem os = new OperatingSystem(cpu, disk, new RoundRobin(1));
            //  Example1(os);
             Example2(os);
            //Example3(os);
            os.ActivateScheduler();
            cpu.Execute();
            Thread.Sleep(1000);
            Console.WriteLine("Average turnaround " + os.AverageTurnaround());
            Console.WriteLine("Maximal starvation " + os.MaximalStarvation());
        }
    }
}
