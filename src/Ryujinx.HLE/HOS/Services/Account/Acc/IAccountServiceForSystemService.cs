using Ryujinx.Common;
using Ryujinx.HLE.HOS.Services.Account.Acc.AccountService;

namespace Ryujinx.HLE.HOS.Services.Account.Acc
{
    [Service("acc:u1", AccountServiceFlag.SystemService)] // Max Sessions: 16
    class IAccountServiceForSystemService : IpcService
    {
        private readonly ApplicationServiceServer _applicationServiceServer;

#pragma warning disable IDE0060
        public IAccountServiceForSystemService(ServiceCtx context, AccountServiceFlag serviceFlag)
        {
            _applicationServiceServer = new ApplicationServiceServer(serviceFlag);
        }
#pragma warning restore IDE0060

        [CommandCmif(0)]
        // GetUserCount() -> i32
        public static ResultCode GetUserCount(ServiceCtx context)
        {
            return ApplicationServiceServer.GetUserCountImpl(context);
        }

        [CommandCmif(1)]
        // GetUserExistence(nn::account::Uid) -> bool
        public static ResultCode GetUserExistence(ServiceCtx context)
        {
            return ApplicationServiceServer.GetUserExistenceImpl(context);
        }

        [CommandCmif(2)]
        // ListAllUsers() -> array<nn::account::Uid, 0xa>
        public static ResultCode ListAllUsers(ServiceCtx context)
        {
            return ApplicationServiceServer.ListAllUsers(context);
        }

        [CommandCmif(3)]
        // ListOpenUsers() -> array<nn::account::Uid, 0xa>
        public static ResultCode ListOpenUsers(ServiceCtx context)
        {
            return ApplicationServiceServer.ListOpenUsers(context);
        }

        [CommandCmif(4)]
        // GetLastOpenedUser() -> nn::account::Uid
        public static ResultCode GetLastOpenedUser(ServiceCtx context)
        {
            return ApplicationServiceServer.GetLastOpenedUser(context);
        }

        [CommandCmif(5)]
        // GetProfile(nn::account::Uid) -> object<nn::account::profile::IProfile>
        public ResultCode GetProfile(ServiceCtx context)
        {
            ResultCode resultCode = ApplicationServiceServer.GetProfile(context, out IProfile iProfile);

            if (resultCode == ResultCode.Success)
            {
                MakeObject(context, iProfile);
            }

            return resultCode;
        }

        [CommandCmif(50)]
        // IsUserRegistrationRequestPermitted(pid) -> bool
        public ResultCode IsUserRegistrationRequestPermitted(ServiceCtx context)
        {
            // NOTE: pid is unused.

            return _applicationServiceServer.IsUserRegistrationRequestPermitted(context);
        }

        [CommandCmif(51)]
        // TrySelectUserWithoutInteraction(bool) -> nn::account::Uid
        public static ResultCode TrySelectUserWithoutInteraction(ServiceCtx context)
        {
            return ApplicationServiceServer.TrySelectUserWithoutInteraction(context);
        }

        [CommandCmif(102)]
        // GetBaasAccountManagerForSystemService(nn::account::Uid) -> object<nn::account::baas::IManagerForApplication>
        public ResultCode GetBaasAccountManagerForSystemService(ServiceCtx context)
        {
            UserId userId = context.RequestData.ReadStruct<UserId>();

            if (userId.IsNull)
            {
                return ResultCode.NullArgument;
            }

            MakeObject(context, new IManagerForSystemService(userId));

            // Doesn't occur in our case.
            // return ResultCode.NullObject;

            return ResultCode.Success;
        }

        [CommandCmif(140)] // 6.0.0+
        // ListQualifiedUsers() -> array<nn::account::Uid, 0xa>
        public static ResultCode ListQualifiedUsers(ServiceCtx context)
        {
            return ApplicationServiceServer.ListQualifiedUsers(context);
        }
    }
}