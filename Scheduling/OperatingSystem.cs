using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
	class OperatingSystem
	{
		public Disk Disk { get; private set; }
		public CPU CPU { get; private set; }
		private Dictionary<int, ProcessTableEntry> m_dProcessTable;
		private List<ReadTokenRequest> m_lReadRequests;
		private int m_cProcesses;
		private SchedulingPolicy m_spPolicy;
		private static int IDLE_PROCESS_ID = 0;

		public OperatingSystem(CPU cpu, Disk disk, SchedulingPolicy sp)
		{
			CPU = cpu;
			Disk = disk;
			m_dProcessTable = new Dictionary<int, ProcessTableEntry>();
			m_lReadRequests = new List<ReadTokenRequest>();
			cpu.OperatingSystem = this;
			disk.OperatingSystem = this;
			m_spPolicy = sp;

			//"idle" process here
			IdleCode idleCode = new IdleCode();
			m_dProcessTable[m_cProcesses] = new ProcessTableEntry(m_cProcesses, "idle", idleCode);
			m_cProcesses++;
		}


		public void CreateProcess(string sCodeFileName)
		{
			Code code = new Code(sCodeFileName);
			m_dProcessTable[m_cProcesses] = new ProcessTableEntry(m_cProcesses, sCodeFileName, code);
			m_dProcessTable[m_cProcesses].StartTime = CPU.TickCount;
			m_spPolicy.AddProcess(m_cProcesses);
			m_cProcesses++;
		}

		public void ProcessTerminated(Exception e)
		{
			if (e != null)
				Console.WriteLine("Process " + CPU.ActiveProcess + " terminated unexpectedly. " + e);
			m_dProcessTable[CPU.ActiveProcess].Done = true;
			m_dProcessTable[CPU.ActiveProcess].Console.Close();
			m_dProcessTable[CPU.ActiveProcess].EndTime = CPU.TickCount;
			ActivateScheduler();
		}

		public void TimeoutReached()
		{
			ActivateScheduler();
		}

		public void ReadToken(string sFileName, int iTokenNumber, int iProcessId, string sParameterName)
		{
			ReadTokenRequest request = new ReadTokenRequest();
			request.ProcessId = iProcessId;
			request.TokenNumber = iTokenNumber;
			request.TargetVariable = sParameterName;
			request.Token = null;
			request.FileName = sFileName;
			m_dProcessTable[iProcessId].Blocked = true;
			if (Disk.ActiveRequest == null)
				Disk.ActiveRequest = request;
			else
				m_lReadRequests.Add(request);
			CPU.ProgramCounter = CPU.ProgramCounter + 1;
			ActivateScheduler();
		}

		public void Interrupt(ReadTokenRequest rFinishedRequest)
		{
			ProcessTableEntry curProcess = m_dProcessTable[rFinishedRequest.ProcessId];
			int NumOfProcess = rFinishedRequest.ProcessId;

			//when the token is null, EOF has been reached.
			//we write the value to the appropriate address space of the calling process.
			if (rFinishedRequest.Token == null)
			{
				curProcess.AddressSpace[rFinishedRequest.TargetVariable] = double.NaN;
			}
			else
			{
				//translate the returned token into a value (double). 
				double token = Convert.ToDouble(rFinishedRequest.Token);

				curProcess.AddressSpace[rFinishedRequest.TargetVariable] = token;
			}

			curProcess.Blocked = false; //so we will not stuck in the "idle" later on...
			curProcess.outOfBlock = CPU.TickCount; //the process isn't blocked any more... hence, its ready. so we save the time... for later calculate max starvation

			if (m_dProcessTable[NumOfProcess].Done != true) //didn't finish so i need to schedual via policy
			{
				m_spPolicy.AddProcess(NumOfProcess);
			}


			//activate the next request in queue on the disk.
			if (m_lReadRequests.Count > 0)
			{
				//need to activate next request...
				Disk.ActiveRequest = m_lReadRequests[0];
				m_lReadRequests.RemoveAt(0);

			}


			if (m_spPolicy.RescheduleAfterInterrupt())
				ActivateScheduler();
		}

		private ProcessTableEntry ContextSwitch(int iEnteringProcessId)
		{

			ProcessTableEntry newProcess = m_dProcessTable[iEnteringProcessId];

			if (CPU.ActiveProcess != -1) // there is a process in the background at the moment
			{
				ProcessTableEntry oldProcess = m_dProcessTable[CPU.ActiveProcess];

				if (oldProcess == newProcess) // we are trying to swittch between same process.. no need to waist time for updating...
				{
					if ((m_spPolicy is RoundRobin))
					{
						CPU.RemainingTime = oldProcess.Quantum; //updaating the quantom of the SAME process
					}
					return oldProcess;
				}





				if (oldProcess.Name != "idle" && oldProcess.Done == false && oldProcess.Blocked == false)
				{
					oldProcess.outOfBlock = CPU.TickCount; // this is for knowing the time that a process is ready

					m_spPolicy.AddProcess(oldProcess.ProcessId); // adding the oldProcess to the schedualing policy because we switched it with the newProcess
				}


				//updating the max starvation for the last function in "Program.cs"
				if (CPU.TickCount - oldProcess.outOfBlock > oldProcess.MaxStarvation)
				{
					oldProcess.MaxStarvation = (CPU.TickCount - oldProcess.outOfBlock);
				}

				/* this is for RoundRobin only!!! */
				object obj = m_spPolicy as RoundRobin;
				if (obj != null)
				{
					CPU.RemainingTime = newProcess.Quantum; //updating remaining time of the cpu to be the quantom of the new process
				}



				oldProcess.ProgramCounter = CPU.ProgramCounter; //update the line, which the old process stop, in the table
				CPU.ActiveProcess = iEnteringProcessId; // update the id of the new process as the running process
				CPU.ProgramCounter = newProcess.ProgramCounter; // put the PC of new process in the cpu so the new process continue from there
				CPU.ActiveAddressSpace = newProcess.AddressSpace; //update address space
				CPU.ActiveConsole = newProcess.Console; //update console situation

				return oldProcess;
			}
			else // we are the first program
			{
				CPU.ActiveProcess = iEnteringProcessId; // update the id of the new process as the running process
				CPU.ProgramCounter = newProcess.ProgramCounter; // put the PC of new process in the cpu so the new process continue from there
				CPU.ActiveAddressSpace = newProcess.AddressSpace; //update address space
				CPU.ActiveConsole = newProcess.Console; //update console situation
				return null;
				//return null because we've been asked to return the returning movie but now no movie should return.
			}
		}

		public void ActivateScheduler()
		{
			int iNextProcessId = m_spPolicy.NextProcess(m_dProcessTable);
			if (iNextProcessId == -1)
			{
				Console.WriteLine("All processes terminated or blocked.");
				CPU.Done = true;
			}
			else
			{
				bool bOnlyIdleRemains = false;
				if (iNextProcessId == IDLE_PROCESS_ID)
				{
					bOnlyIdleRemains = true;
					foreach (ProcessTableEntry e in m_dProcessTable.Values)
					{
						if (e.Name != "idle" && e.Done != true)// not idle - not done
						{
							bOnlyIdleRemains = false;
						}
					}
				}

				//we have been asked to add a code here but i don't see why...

				if (bOnlyIdleRemains)
				{
					Console.WriteLine("Only idle remains.");
					CPU.Done = true;
				}
				else
					ContextSwitch(iNextProcessId);


			}
		}

		public double AverageTurnaround()
		{
			double sum = 0;

			for (int i = 1; i < m_dProcessTable.Count; i++) // i=1 because we dont want to count the idle...
			{
				sum = sum + (m_dProcessTable[i].EndTime - m_dProcessTable[i].StartTime);
			}

			return sum / (m_dProcessTable.Count - 1); // -1 cause no idle...
		}


		public int MaximalStarvation()
		{
			int maxStarvation = 0;

			for (int i = 1; i < m_dProcessTable.Count; i++) // i=1 because we dont want to count the idle...
			{
				if (m_dProcessTable[i].MaxStarvation > maxStarvation)
				{
					/* in the ContextSwitch function up above, i calculate accurate starvation 
                     * using a new parameter i've created name "outOfBlock" so it will be
                     */
					maxStarvation = m_dProcessTable[i].MaxStarvation; // updating the max value
				}
			}
			return maxStarvation;
		}

	}
}
