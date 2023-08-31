using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Pctl.Types;
using Ryujinx.Horizon.Sdk.Pctl;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;
using Result = Ryujinx.Horizon.Common.Result;
using TimeSpan = Ryujinx.Horizon.Pctl.Types.TimeSpan;

namespace Ryujinx.Horizon.Pctl.Ipc
{
    partial class PctlService : IPctlService
    {
        private readonly ulong _pid;
        private readonly int _permissionFlag;
        private PctlFlagValue _parentalControlFlag;

        // TODO: Find where they are set.
        private readonly bool _restrictionEnabled = false;
        private readonly bool _featuresRestriction = false;
        private readonly bool _stereoVisionRestrictionConfigurable = true;
        private bool _stereoVisionRestriction = false;

        public PctlService(ulong pid, bool withInitialize, int permissionFlag)
        {
            _pid = pid;
            _permissionFlag = permissionFlag;

            if (withInitialize)
            {
                Initialize();
            }
        }

        [CmifCommand(1)] // 4.0.0+
        public Result Initialize()
        {
            if ((_permissionFlag & 0x8001) == 0)
            {
                return PctlResult.PermissionDenied;
            }

            Result result = PctlResult.InvalidPid;

            if (_pid != 0)
            {
                if ((_permissionFlag & 0x40) == 0)
                {
                    // TODO: Get actual control flag from Application
                    _parentalControlFlag = PctlFlagValue.FreeCommunication;
                }

                if ((_permissionFlag & 0x8040) == 0)
                {
                    // TODO: Services store TitleId and FreeCommunicationEnabled in a static object.
                    // When initialisation is complete, signal an event on that static object.
                    Logger.Stub?.PrintStub(LogClass.ServicePctl);
                }

                result = Result.Success;
            }

            return result;
        }

        [CmifCommand(1001)]
        public Result CheckFreeCommunicationPermission()
        {
            if (_parentalControlFlag == PctlFlagValue.FreeCommunication && _restrictionEnabled)
            {
                // TODO: Checks if an entry exists in the FreeCommunicationApplicationList using the TitleId.
                // Returns FreeCommunicationDisabled if entry doesn't exist.

                return PctlResult.FreeCommunicationDisabled;
            }

            Logger.Stub?.PrintStub(LogClass.ServicePctl);

            return Result.Success;
        }

        [CmifCommand(1002)]
        public Result ConfirmLaunchApplicationPermission(ApplicationId applicationId, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg1, bool arg2)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1003)]
        public Result ConfirmResumeApplicationPermission(ApplicationId applicationId, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg1, bool arg2)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1004)]
        public Result ConfirmSnsPostPermission()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1005)]
        public Result ConfirmSystemSettingsPermission()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1006)]
        public Result IsRestrictionTemporaryUnlocked(out bool arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1007)]
        public Result RevertRestrictionTemporaryUnlocked()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1008)]
        public Result EnterRestrictedSystemSettings()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1009)]
        public Result LeaveRestrictedSystemSettings()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1010)]
        public Result IsRestrictedSystemSettingsEntered(out bool arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1011)]
        public Result RevertRestrictedSystemSettingsEntered()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1012)]
        public Result GetRestrictedFeatures(out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1013)] // 4.0.0+
        public Result ConfirmStereoVisionPermission()
        {
            return IsStereoVisionPermittedImpl();
        }

        [CmifCommand(1014)] // 5.0.0+
        public Result ConfirmPlayableApplicationVideoOld([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1015)] // 5.0.0+
        public Result ConfirmPlayableApplicationVideo(ApplicationId arg0, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg1)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1016)] // 6.0.0+
        public Result ConfirmShowNewsPermission([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1017)] // 10.0.0+
        public Result EndFreeCommunication()
        {
            return Result.Success;
        }

        [CmifCommand(1018)] // 10.0.0+
        public Result IsFreeCommunicationAvailable()
        {
            if (_parentalControlFlag == PctlFlagValue.FreeCommunication && _restrictionEnabled)
            {
                // TODO: Checks if an entry exists in the FreeCommunicationApplicationList using the TitleId.
                // Returns FreeCommunicationDisabled if entry doesn't exist.

                return PctlResult.FreeCommunicationDisabled;
            }

            Logger.Stub?.PrintStub(LogClass.ServicePctl);

            return Result.Success;
        }

        [CmifCommand(1031)]
        public Result IsRestrictionEnabled(out bool restrictionEnabled)
        {
            if ((_permissionFlag & 0x140) == 0)
            {
                restrictionEnabled = default;
                return PctlResult.PermissionDenied;
            }

            restrictionEnabled = _restrictionEnabled;

            return Result.Success;
        }

        [CmifCommand(1032)]
        public Result GetSafetyLevel(out int arg0)
        {
            arg0 = default;

            return Result.Success;
        }

        [CmifCommand(1033)]
        public Result SetSafetyLevel(int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1034)]
        public Result GetSafetyLevelSettings(out RestrictionSettings restrictionSettings, int arg1)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                restrictionSettings = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1035)]
        public Result GetCurrentSettings(out RestrictionSettings restrictionSettings)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                restrictionSettings = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1036)]
        public Result SetCustomSafetyLevelSettings(RestrictionSettings restrictionSettings)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1037)]
        public Result GetDefaultRatingOrganization(out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1038)]
        public Result SetDefaultRatingOrganization(int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1039)]
        public Result GetFreeCommunicationApplicationListCount(out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1042)]
        public Result AddToFreeCommunicationApplicationList(ApplicationId arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1043)]
        public Result DeleteSettings()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1044)]
        public Result GetFreeCommunicationApplicationList(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<FreeCommunicationApplicationInfo> arg1, int arg2)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1045)]
        public Result UpdateFreeCommunicationApplicationList([Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias)] ReadOnlySpan<FreeCommunicationApplicationInfo> arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1046)]
        public Result DisableFeaturesForReset()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1047)] // 3.0.0+
        public Result NotifyApplicationDownloadStarted(ApplicationId applicationId)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1048)] // 6.0.0+
        public Result NotifyNetworkProfileCreated()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1049)] // 11.0.0+
        public Result ResetFreeCommunicationApplicationList()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1061)] // 4.0.0+
        public Result ConfirmStereoVisionRestrictionConfigurable()
        {
            if ((_permissionFlag & 2) == 0)
            {
                return PctlResult.PermissionDenied;
            }

            if (_stereoVisionRestrictionConfigurable)
            {
                return Result.Success;
            }
            else
            {
                return PctlResult.StereoVisionRestrictionConfigurableDisabled;
            }
        }

        [CmifCommand(1062)] // 4.0.0+
        public Result GetStereoVisionRestriction(out bool stereoVisionRestriction)
        {
            if ((_permissionFlag & 0x200) == 0)
            {
                stereoVisionRestriction = default;
                return PctlResult.PermissionDenied;
            }

            stereoVisionRestriction = _stereoVisionRestrictionConfigurable && _stereoVisionRestriction;

            return Result.Success;
        }

        [CmifCommand(1063)] // 4.0.0+
        public Result SetStereoVisionRestriction(bool stereoVisionRestriction)
        {
            if ((_permissionFlag & 0x200) == 0)
            {
                return PctlResult.PermissionDenied;
            }

            if (!_featuresRestriction)
            {
                if (_stereoVisionRestrictionConfigurable)
                {
                    _stereoVisionRestriction = stereoVisionRestriction;

                    // TODO: It signals an internal event of the service. We have to determine where this event is used.
                }
            }

            return Result.Success;
        }

        [CmifCommand(1064)] // 5.0.0+
        public Result ResetConfirmedStereoVisionPermission()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1065)] // 5.0.0+
        public Result IsStereoVisionPermitted(out bool isStereoVisionPermitted)
        {
            Result result = IsStereoVisionPermittedImpl();

            isStereoVisionPermitted = result == Result.Success;

            return result;
        }

        private Result IsStereoVisionPermittedImpl()
        {
            // TODO: Application Exemptions are read from file "appExemptions.dat" in the service savedata.
            // Since we don't support the pctl savedata for now, this can be implemented later.

            if (_stereoVisionRestrictionConfigurable && _stereoVisionRestriction)
            {
                return PctlResult.StereoVisionDenied;
            }
            else
            {
                return Result.Success;
            }
        }

        [CmifCommand(1201)]
        public Result UnlockRestrictionTemporarily([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1202)]
        public Result UnlockSystemSettingsRestriction([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1203)]
        public Result SetPinCode([Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1204)]
        public Result GenerateInquiryCode(out InquiryCode inquiryCode)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                inquiryCode = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1205)]
        public Result CheckMasterKey(out bool arg0, InquiryCode inquiryCode, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg2)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1206)]
        public Result GetPinCodeLength(out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1207)]
        public Result GetPinCodeChangedEvent([CopyHandle] out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1208)] // 4.0.0+
        public Result GetPinCode(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<sbyte> arg1)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1403)]
        public Result IsPairingActive(out bool arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1406)]
        public Result GetSettingsLastUpdated(out PosixTime posixTime)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                posixTime = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1411)]
        public Result GetPairingAccountInfo(out PairingAccountInfoBase pairingAccountInfo, PairingInfoBase pairingInfo)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                pairingAccountInfo = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1421)]
        public Result GetAccountNickname(out uint arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<sbyte> arg1, PairingAccountInfoBase arg2)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1424)]
        public Result GetAccountState(out int arg0, PairingAccountInfoBase pairingAccountInfo)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1425)] // 6.0.0+
        public Result RequestPostEvents(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<EventData> arg1)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1426)] // 11.0.0+
        public Result GetPostEventInterval(out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1427)] // 11.0.0+
        public Result SetPostEventInterval(int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1432)]
        public Result GetSynchronizationEvent([CopyHandle] out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1451)]
        public Result StartPlayTimer()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1452)]
        public Result StopPlayTimer()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1453)]
        public Result IsPlayTimerEnabled(out bool arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1454)]
        public Result GetPlayTimerRemainingTime(out TimeSpan timeSpan)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                timeSpan = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1455)]
        public Result IsRestrictedByPlayTimer(out bool isPlayTimeRestricted)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                isPlayTimeRestricted = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1456)]
        public Result GetPlayTimerSettings(out PlayTimerSettings playTimerSettings)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                playTimerSettings = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1457)]
        public Result GetPlayTimerEventToRequestSuspension([CopyHandle] out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1458)] // 4.0.0+
        public Result IsPlayTimerAlarmDisabled(out bool isPlayTimerAlarmDisabled)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                isPlayTimerAlarmDisabled = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1471)]
        public Result NotifyWrongPinCodeInputManyTimes()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1472)]
        public Result CancelNetworkRequest()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1473)]
        public Result GetUnlinkedEvent([CopyHandle] out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1474)]
        public Result ClearUnlinkedEvent()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1601)]
        public Result DisableAllFeatures(out bool arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1602)]
        public Result PostEnableAllFeatures(out bool arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1603)]
        public Result IsAllFeaturesDisabled(out bool arg0, out bool arg1)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                arg1 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1901)]
        public Result DeleteFromFreeCommunicationApplicationListForDebug(ApplicationId applicationId)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1902)]
        public Result ClearFreeCommunicationApplicationListForDebug()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1903)] // 5.0.0+
        public Result GetExemptApplicationListCountForDebug(out int arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1904)] // 5.0.0+
        public Result GetExemptApplicationListForDebug(out int arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<ExemptApplicationInfo> arg1, int arg2)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1905)] // 5.0.0+
        public Result UpdateExemptApplicationListForDebug([Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias)] ReadOnlySpan<ExemptApplicationInfo> arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1906)] // 5.0.0+
        public Result AddToExemptApplicationListForDebug(ApplicationId applicationId)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1907)] // 5.0.0+
        public Result DeleteFromExemptApplicationListForDebug(ApplicationId applicationId)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1908)] // 5.0.0+
        public Result ClearExemptApplicationListForDebug()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1941)]
        public Result DeletePairing()
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1951)]
        public Result SetPlayTimerSettingsForDebug(PlayTimerSettings playTimerSettings)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1952)]
        public Result GetPlayTimerSpentTimeForTest(out TimeSpan timeSpan)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                timeSpan = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(1953)] // 4.0.0+
        public Result SetPlayTimerAlarmDisabledForDebug(bool arg0)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2001)]
        public Result RequestPairingAsync(out AsyncData asyncData, [CopyHandle] out int arg1, [Buffer(HipcBufferFlags.In | HipcBufferFlags.Pointer)] ReadOnlySpan<sbyte> arg2)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                asyncData = default;
                arg1 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2002)]
        public Result FinishRequestPairing(out PairingInfoBase pairingInfo, AsyncData asyncData)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                pairingInfo = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2003)]
        public Result AuthorizePairingAsync(out AsyncData asyncData, [CopyHandle] out int arg1, PairingInfoBase pairingInfo)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                asyncData = default;
                arg1 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2004)]
        public Result FinishAuthorizePairing(out PairingInfoBase pairingInfo, AsyncData asyncData)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                pairingInfo = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2005)]
        public Result RetrievePairingInfoAsync(out AsyncData asyncData, [CopyHandle] out int arg1)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                asyncData = default;
                arg1 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2006)]
        public Result FinishRetrievePairingInfo(out PairingInfoBase pairingInfo, AsyncData asyncData)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                pairingInfo = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2007)]
        public Result UnlinkPairingAsync(out AsyncData asyncData, [CopyHandle] out int arg1, bool arg2)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                asyncData = default;
                arg1 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2008)]
        public Result FinishUnlinkPairing(AsyncData asyncData, bool arg1)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2009)]
        public Result GetAccountMiiImageAsync(out AsyncData asyncData, [CopyHandle] out int arg1, out uint arg2, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<byte> arg3, PairingAccountInfoBase pairingAccountInfo)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                asyncData = default;
                arg1 = default;
                arg2 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2010)]
        public Result FinishGetAccountMiiImage(out uint arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<byte> arg1, AsyncData asyncData)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2011)]
        public Result GetAccountMiiImageContentTypeAsync(out AsyncData asyncData, [CopyHandle] out int arg1, out uint arg2, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<sbyte> arg3, PairingAccountInfoBase pairingAccountInfo)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                asyncData = default;
                arg1 = default;
                arg2 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2012)]
        public Result FinishGetAccountMiiImageContentType(out uint arg0, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.Pointer)] Span<sbyte> arg1, AsyncData asyncData)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                arg0 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2013)]
        public Result SynchronizeParentalControlSettingsAsync(out AsyncData asyncData, [CopyHandle] out int arg1)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                asyncData = default;
                arg1 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2014)]
        public Result FinishSynchronizeParentalControlSettings(AsyncData asyncData)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2015)]
        public Result FinishSynchronizeParentalControlSettingsWithLastUpdated(out PosixTime posixTime, AsyncData asyncData)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                posixTime = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }

        [CmifCommand(2016)] // 5.0.0+
        public Result RequestUpdateExemptionListAsync(out AsyncData asyncData, [CopyHandle] out int arg1, ApplicationId applicationId, bool arg3)
        {
            if (HorizonStatic.Options.IgnoreMissingServices)
            {
                asyncData = default;
                arg1 = default;
                return Result.Success;
            }

            throw new NotImplementedException();
        }
    }
}
