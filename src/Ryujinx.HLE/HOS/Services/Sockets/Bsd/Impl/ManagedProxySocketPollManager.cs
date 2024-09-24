using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Types;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd.Impl
{
    class ManagedProxySocketPollManager : IPollManager
    {
        private static ManagedProxySocketPollManager _instance;

        public static ManagedProxySocketPollManager Instance
        {
            get
            {
                _instance ??= new ManagedProxySocketPollManager();

                return _instance;
            }
        }

        public bool IsCompatible(PollEvent evnt)
        {
            return evnt.FileDescriptor is ManagedProxySocket;
        }

        public LinuxError Poll(List<PollEvent> events, int timeoutMilliseconds, out int updatedCount)
        {
            Dictionary<SelectMode, List<ManagedProxySocket>> eventDict = new()
            {
                { SelectMode.SelectRead, [] },
                { SelectMode.SelectWrite, [] },
                { SelectMode.SelectError, [] },
            };

            updatedCount = 0;

            foreach (PollEvent evnt in events)
            {
                ManagedProxySocket socket = (ManagedProxySocket)evnt.FileDescriptor;

                bool isValidEvent = evnt.Data.InputEvents == 0;

                eventDict[SelectMode.SelectError].Add(socket);

                if ((evnt.Data.InputEvents & PollEventTypeMask.Input) != 0)
                {
                    eventDict[SelectMode.SelectRead].Add(socket);

                    isValidEvent = true;
                }

                if ((evnt.Data.InputEvents & PollEventTypeMask.UrgentInput) != 0)
                {
                    eventDict[SelectMode.SelectRead].Add(socket);

                    isValidEvent = true;
                }

                if ((evnt.Data.InputEvents & PollEventTypeMask.Output) != 0)
                {
                    eventDict[SelectMode.SelectWrite].Add(socket);

                    isValidEvent = true;
                }

                if (!isValidEvent)
                {
                    Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported Poll input event type: {evnt.Data.InputEvents}");
                    return LinuxError.EINVAL;
                }
            }

            try
            {
                int actualTimeoutMicroseconds = timeoutMilliseconds == -1 ? -1 : timeoutMilliseconds * 1000;
                int totalEvents = eventDict[SelectMode.SelectRead].Count + eventDict[SelectMode.SelectWrite].Count + eventDict[SelectMode.SelectError].Count;
                // TODO: Maybe check all events first, wait for the timeout and then check the failed ones again?
                int timeoutMicrosecondsPerEvent = actualTimeoutMicroseconds == -1 ? -1 : actualTimeoutMicroseconds / totalEvents;

                foreach ((SelectMode selectMode, List<ManagedProxySocket> eventList) in eventDict)
                {
                    List<ManagedProxySocket> newEventList = [];

                    foreach (ManagedProxySocket eventSocket in eventList)
                    {
                        if (eventSocket.Poll(timeoutMicrosecondsPerEvent, selectMode))
                        {
                            newEventList.Add(eventSocket);
                        }
                    }

                    eventDict[selectMode] = newEventList;
                }
            }
            catch (SocketException exception)
            {
                return WinSockHelper.ConvertError((WsaError)exception.ErrorCode);
            }

            foreach (PollEvent evnt in events)
            {
                ManagedProxySocket socket = ((ManagedProxySocket)evnt.FileDescriptor);

                PollEventTypeMask outputEvents = evnt.Data.OutputEvents & ~evnt.Data.InputEvents;

                if (eventDict[SelectMode.SelectError].Contains(socket))
                {
                    outputEvents |= PollEventTypeMask.Error;

                    if (!socket.ProxyClient.Connected || !socket.ProxyClient.IsBound)
                    {
                        outputEvents |= PollEventTypeMask.Disconnected;
                    }
                }

                if (eventDict[SelectMode.SelectRead].Contains(socket))
                {
                    if ((evnt.Data.InputEvents & PollEventTypeMask.Input) != 0)
                    {
                        outputEvents |= PollEventTypeMask.Input;
                    }
                }

                if (eventDict[SelectMode.SelectWrite].Contains(socket))
                {
                    outputEvents |= PollEventTypeMask.Output;
                }

                evnt.Data.OutputEvents = outputEvents;
            }

            updatedCount = eventDict[SelectMode.SelectRead].Count + eventDict[SelectMode.SelectWrite].Count + eventDict[SelectMode.SelectError].Count;

            return LinuxError.SUCCESS;
        }

        public LinuxError Select(List<PollEvent> events, int timeout, out int updatedCount)
        {
            Dictionary<SelectMode, List<ManagedProxySocket>> eventDict = new()
            {
                { SelectMode.SelectRead, [] },
                { SelectMode.SelectWrite, [] },
                { SelectMode.SelectError, [] },
            };

            updatedCount = 0;

            foreach (PollEvent pollEvent in events)
            {
                ManagedProxySocket socket = (ManagedProxySocket)pollEvent.FileDescriptor;

                if (pollEvent.Data.InputEvents.HasFlag(PollEventTypeMask.Input))
                {
                    eventDict[SelectMode.SelectRead].Add(socket);
                }

                if (pollEvent.Data.InputEvents.HasFlag(PollEventTypeMask.Output))
                {
                    eventDict[SelectMode.SelectWrite].Add(socket);
                }

                if (pollEvent.Data.InputEvents.HasFlag(PollEventTypeMask.Error))
                {
                    eventDict[SelectMode.SelectError].Add(socket);
                }
            }

            int totalEvents = eventDict[SelectMode.SelectRead].Count + eventDict[SelectMode.SelectWrite].Count + eventDict[SelectMode.SelectError].Count;
            // TODO: Maybe check all events first, wait for the timeout and then check the failed ones again?
            int timeoutMicrosecondsPerEvent = timeout == -1 ? -1 : timeout / totalEvents;

            foreach ((SelectMode selectMode, List<ManagedProxySocket> eventList) in eventDict)
            {
                List<ManagedProxySocket> newEventList = [];

                foreach (ManagedProxySocket eventSocket in eventList)
                {
                    if (eventSocket.Poll(timeoutMicrosecondsPerEvent, selectMode))
                    {
                        newEventList.Add(eventSocket);
                    }
                }

                eventDict[selectMode] = newEventList;
            }

            updatedCount = eventDict[SelectMode.SelectRead].Count + eventDict[SelectMode.SelectWrite].Count + eventDict[SelectMode.SelectError].Count;

            foreach (PollEvent pollEvent in events)
            {
                ManagedProxySocket socket = (ManagedProxySocket)pollEvent.FileDescriptor;

                if (eventDict[SelectMode.SelectRead].Contains(socket))
                {
                    pollEvent.Data.OutputEvents |= PollEventTypeMask.Input;
                }

                if (eventDict[SelectMode.SelectWrite].Contains(socket))
                {
                    pollEvent.Data.OutputEvents |= PollEventTypeMask.Output;
                }

                if (eventDict[SelectMode.SelectError].Contains(socket))
                {
                    pollEvent.Data.OutputEvents |= PollEventTypeMask.Error;
                }
            }

            return LinuxError.SUCCESS;
        }
    }
}
