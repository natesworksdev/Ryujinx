using Ryujinx.HLE.HOS.Services.Mii.Types;
using Ryujinx.HLE.HOS.Services.Settings;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Mii.StaticService
{
    class DatabaseServiceImpl : IDatabaseService
    {
        private DatabaseImpl            _database;
        private DatabaseSessionMetadata _metadata;
        private bool                    _isSystem;

        public DatabaseServiceImpl(DatabaseImpl database, bool isSystem, SpecialMiiKeyCode miiKeyCode)
        {
            _database = database;
            _metadata = _database.CreateSessionMetadata(miiKeyCode);
            _isSystem = isSystem;
        }

        public bool IsDatabaseTestModeEnabled()
        {
            if (NxSettings.Settings.TryGetValue("mii!is_db_test_mode_enabled", out object isDatabaseTestModeEnabled))
            {
                return (bool)isDatabaseTestModeEnabled;
            }

            return false;
        }

        public override bool IsUpdated(SourceFlag flag)
        {
            return _database.IsUpdated(_metadata, flag);
        }

        public override bool IsFullDatabase()
        {
            return _database.IsFullDatabase();
        }

        public override uint GetCount(SourceFlag flag)
        {
            return _database.GetCount(_metadata, flag);
        }

        public override ResultCode Get(SourceFlag flag, out int count, Span<CharInfoElement> elements)
        {
            return _database.Get(_metadata, flag, out count, elements);
        }

        public override ResultCode Get1(SourceFlag flag, out int count, Span<CharInfo> elements)
        {
            return _database.Get(_metadata, flag, out count, elements);
        }

        public override ResultCode UpdateLatest(CharInfo oldCharInfo, SourceFlag flag, out CharInfo newCharInfo)
        {
            newCharInfo = new CharInfo();

            return _database.UpdateLatest(_metadata, oldCharInfo, flag, newCharInfo);
        }

        public override ResultCode BuildRandom(Age age, Gender gender, Race race, out CharInfo charInfo)
        {
            if (age > Age.All || gender > Gender.All || race > Race.All)
            {
                charInfo = new CharInfo();

                return ResultCode.InvalidArgument;
            }

            _database.BuildRandom(age, gender, race, out charInfo);

            return ResultCode.Success;
         }

        public override ResultCode BuildDefault(uint index, out CharInfo charInfo)
        {
            if (index >= DefaultMii.TableLength)
            {
                charInfo = new CharInfo();

                return ResultCode.InvalidArgument;
            }

            _database.BuildDefault(index, out charInfo);

            return ResultCode.Success;
        }

        public override ResultCode Get2(SourceFlag flag, out int count, Span<StoreDataElement> elements)
        {
            if (!_isSystem)
            {
                count = -1;

                return ResultCode.PermissionDenied;
            }

            return _database.Get(_metadata, flag, out count, elements);
        }

        public override ResultCode Get3(SourceFlag flag, out int count, Span<StoreData> elements)
        {
            if (!_isSystem)
            {
                count = -1;

                return ResultCode.PermissionDenied;
            }

            return _database.Get(_metadata, flag, out count, elements);
        }

        public override ResultCode UpdateLatest1(StoreData oldStoreData, SourceFlag flag, out StoreData newstoreData)
        {
            newstoreData = new StoreData();

            if (!_isSystem)
            {
                return ResultCode.PermissionDenied;
            }

            return _database.UpdateLatest(_metadata, oldStoreData, flag, newstoreData);
        }

        public override ResultCode FindIndex(CreateId createId, bool isSpecial, out int index)
        {
            if (!_isSystem)
            {
                index = -1;

                return ResultCode.PermissionDenied;
            }

            index = _database.FindIndex(_metadata, createId, isSpecial);

            return ResultCode.Success;
        }

        public override ResultCode Move(CreateId createId, int newIndex)
        {
            if (!_isSystem)
            {
                return ResultCode.PermissionDenied;
            }

            if (newIndex > 0 && _database.GetCount(_metadata, SourceFlag.Database) > newIndex)
            {
                return _database.Move(_metadata, newIndex, createId);
            }

            return ResultCode.InvalidArgument;
        }

        public override ResultCode AddOrReplace(StoreData storeData)
        {
            if (!_isSystem)
            {
                return ResultCode.PermissionDenied;
            }

            return _database.AddOrReplace(_metadata, storeData);
        }

        public override ResultCode Delete(CreateId createId)
        {
            if (!_isSystem)
            {
                return ResultCode.PermissionDenied;
            }

            return _database.Delete(_metadata, createId);
        }

        public override ResultCode DestroyFile()
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            return _database.DestroyFile(_metadata);
        }

        public override ResultCode DeleteFile()
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            return _database.DeleteFile();
        }

        public override ResultCode Format()
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            _database.Format(_metadata);

            return ResultCode.Success;
        }

        public override ResultCode Import(ReadOnlySpan<byte> data)
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            throw new NotImplementedException();
        }

        public override ResultCode Export(Span<byte> data)
        {
            if (!IsDatabaseTestModeEnabled())
            {
                return ResultCode.TestModeNotEnabled;
            }

            throw new NotImplementedException();
        }

        public override ResultCode IsBrokenDatabaseWithClearFlag(out bool isBrokenDatabase)
        {
            if (!_isSystem)
            {
                isBrokenDatabase = false;

                return ResultCode.PermissionDenied;
            }

            isBrokenDatabase = _database.IsBrokenDatabaseWithClearFlag();

            return ResultCode.Success;
        }

        public override ResultCode GetIndex(CharInfo charInfo, out int index)
        {
            return _database.GetIndex(_metadata, charInfo, out index);
        }

        public override void SetInterfaceVersion(uint interfaceVersion)
        {
            _database.SetInterfaceVersion(_metadata, interfaceVersion);
        }

        public override ResultCode Convert(Ver3StoreData ver3StoreData, out CharInfo charInfo)
        {
            throw new NotImplementedException();
        }

        public override ResultCode ConvertCoreDataToCharInfo(CoreData coreData, out CharInfo charInfo)
        {
            return _database.ConvertCoreDataToCharInfo(coreData, out charInfo);
        }

        public override ResultCode ConvertCharInfoToCoreData(CharInfo charInfo, out CoreData coreData)
        {
            return _database.ConvertCharInfoToCoreData(charInfo, out coreData);
        }
    }
}
