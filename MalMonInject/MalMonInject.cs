﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using EasyHook;

namespace MalMonInject
{
    public class MalMonInject : EasyHook.IEntryPoint
    {
        public MalMonitor.MonitorInterface Interface;

        public FileActivities FileApis;

        public RegActivities RegApis;

        public ProcActivities ProcApis;

        public NetActivities NetApis;

        public Stack<String> Queue = new Stack<String>();

        public MalMonInject(
            RemoteHooking.IContext InContext,
            String InChannelName)
        {
            FileApis = new FileActivities(this);
            ProcApis = new ProcActivities(this);

            // connect to host...
            Interface = RemoteHooking.IpcConnectClient<MalMonitor.MonitorInterface>(InChannelName);
            Interface.Ping();
        }

        public void Run(
            RemoteHooking.IContext InContext,
            String InChannelName)
        {
            // install hook...
            try
            {
                FileApis.InstallHook();
                ProcApis.InstallHook();
            }
            catch (Exception ExtInfo)
            {
                Interface.ReportException(ExtInfo);

                return;
            }

            Interface.IsInstalled(RemoteHooking.GetCurrentProcessId());

            RemoteHooking.WakeUpProcess();

            // wait for host process termination...
            try
            {
                while (true)
                {
                    Thread.Sleep(500);

                    // transmit newly monitored file accesses...
                    if (Queue.Count > 0)
                    {
                        String[] Package = null;

                        lock (Queue)
                        {
                            Package = Queue.ToArray();

                            Queue.Clear();
                        }

                        Interface.Output(DateTime.Now, RemoteHooking.GetCurrentProcessId(), Package);
                    }
                    else
                        Interface.Ping();
                }
            }
            catch
            {
                // Ping() will raise an exception if host is unreachable
            }
        }

        
    }
}
