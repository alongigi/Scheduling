using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
    class RoundRobin : SchedulingPolicy
    {

        public int quantom;
       Queue<int> readyProcessList = new Queue<int>();


        public RoundRobin(int iQuantum)
        {
            quantom = iQuantum;
        }

        public override int NextProcess(Dictionary<int, ProcessTableEntry> dProcessTable)
        {
            if (readyProcessList.Count == 0)
            {
                return -1;
            }

            int pid = readyProcessList.Dequeue();
            dProcessTable[pid].Quantum = quantom;
           // readyProcessList.RemoveAt(0); //removing the oldest process from the start of the list
            return pid;  
        }

        public override void AddProcess(int iProcessId)
        {
            readyProcessList.Enqueue(iProcessId); //entering the newest process into the end of the list
        }

        public override bool RescheduleAfterInterrupt()
        {
            return true;
        }
    }
}
