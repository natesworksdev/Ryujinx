using Ryujinx.Horizon.Common;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ryujinx.Horizon.Sdk.Sf.Cmif
{
    class ServiceDispatchTable : ServiceDispatchTableBase
    {
        private readonly IReadOnlyDictionary<uint, CommandHandler> _entries;

        public ServiceDispatchTable(IReadOnlyDictionary<uint, CommandHandler> entries)
        {
            _entries = entries;
        }

        public override Result ProcessMessage(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData)
        {
            return ProcessMessageImpl(ref context, inRawData, _entries);
        }

        public static ServiceDispatchTableBase Create(IServiceObject instance)
        {
            if (instance is DomainServiceObject)
            {
                return new DomainServiceObjectDispatchTable();
            }

            Dictionary<uint, CommandHandler> entries = new Dictionary<uint, CommandHandler>();

            var meths = instance.GetType().GetMethods();

            foreach (MethodInfo meth in meths)
            {
                var commandAttribute = meth.GetCustomAttribute<CommandAttribute>();
                if (commandAttribute != null)
                {
                    entries.Add(commandAttribute.CommandId, new CommandHandler(meth, instance));
                }
            }

            return new ServiceDispatchTable(entries);
        }
    }
}
