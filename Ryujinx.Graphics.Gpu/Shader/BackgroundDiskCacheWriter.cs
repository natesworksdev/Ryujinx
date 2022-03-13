using Ryujinx.Common;
using System;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Represent a cache collection handling one shader cache.
    /// </summary>
    class BackgroundDiskCacheWriter : IDisposable
    {
        /// <summary>
        /// Possible operation to do on the <see cref="_fileWriterWorkerQueue"/>.
        /// </summary>
        private enum CacheFileOperation
        {
            AddShader
        }

        /// <summary>
        /// Represent an operation to perform on the <see cref="_fileWriterWorkerQueue"/>.
        /// </summary>
        private struct CacheFileOperationTask
        {
            /// <summary>
            /// The type of operation to perform.
            /// </summary>
            public readonly CacheFileOperation Type;

            /// <summary>
            /// The data associated to this operation or null.
            /// </summary>
            public readonly object Data;

            public CacheFileOperationTask(CacheFileOperation type, object data)
            {
                Type = type;
                Data = data;
            }
        }

        private struct AddShaderData
        {
            public readonly CachedShaderProgram Program;
            public readonly byte[] HostCode;

            public AddShaderData(CachedShaderProgram program, byte[] hostCode)
            {
                Program = program;
                HostCode = hostCode;
            }
        }

        private readonly GpuContext _context;
        private readonly DiskCacheHostStorage _hostStorage;
        private readonly AsyncWorkQueue<CacheFileOperationTask> _fileWriterWorkerQueue;

        public BackgroundDiskCacheWriter(GpuContext context, DiskCacheHostStorage hostStorage)
        {
            _context = context;
            _hostStorage = hostStorage;
            _fileWriterWorkerQueue = new AsyncWorkQueue<CacheFileOperationTask>(ProcessTask, "Gpu.BackgroundDiskCacheWriter");
        }

        private void ProcessTask(CacheFileOperationTask task)
        {
            switch (task.Type)
            {
                case CacheFileOperation.AddShader:
                    AddShaderData data = (AddShaderData)task.Data;
                    _hostStorage.AddShader(_context, data.Program, data.HostCode);
                    break;
            }
        }

        public void AddShader(CachedShaderProgram program, byte[] hostCode)
        {
            _fileWriterWorkerQueue.Add(new CacheFileOperationTask(CacheFileOperation.AddShader, new AddShaderData(program, hostCode)));
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileWriterWorkerQueue.Dispose();
            }
        }
    }
}
