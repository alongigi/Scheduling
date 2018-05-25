using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
	class FirstComeFirstServedPolicy : SchedulingPolicy
	{

		Queue<int> readyProcessList = new Queue<int>();

		public override int NextProcess(Dictionary<int, ProcessTableEntry> dProcessTable)
		{
			if (readyProcessList.Count == 0) //if the queue is empty so we want to return the first item from the table otherwise we will get error
			{
				return dProcessTable[0].ProcessId;
			}

			int pid = readyProcessList.Dequeue();
			//readyProcessList.RemoveAt(0); //removing the oldest process from the start of the list
			return pid;
		}

		public override void AddProcess(int iProcessId)
		{
			readyProcessList.Enqueue(iProcessId); //entering the newest process into the end of the list
		}

		public override bool RescheduleAfterInterrupt()
		{
			return false;
		}
	}
}
