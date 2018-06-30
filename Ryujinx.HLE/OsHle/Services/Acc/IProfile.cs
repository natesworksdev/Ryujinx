using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Ipc;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.OsHle.Services.Acc
{
    class IProfile : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IProfile()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 1, GetBase }
            };
        }

        public long GetBase(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAcc, "Stubbed.");

            //UserID
            Context.ResponseData.Write(1L);

            //Padding?
            Context.ResponseData.Write(0);
            Context.ResponseData.Write(0);

            //Timestamp
            Context.ResponseData.Write(0L);

            //Username
            Context.ResponseData.Write(GetUsernameBytes("Ryujinx"));

            return 0;
        }

        private byte[] GetUsernameBytes(string Username)
        {
            char[] CharArr = Username.ToCharArray();
            byte[] ByteArr = new byte[0x20];

            for (int Index = 0; Index < ByteArr.Length; Index++)
            {
                if (Index > CharArr.Length)
                {
                    ByteArr[Index] = 0x0;
                }
                else
                {
                    ByteArr[Index] = Convert.ToByte(CharArr[Index]);
                }
            }

            return ByteArr;
        }
    }
}