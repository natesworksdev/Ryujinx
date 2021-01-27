namespace Ryujinx.HLE.HOS.Tamper.Operations
{
    internal class OpProcCtrl : IOperation
    {
        ITamperedProcess _process;
        bool _pause;

        public OpProcCtrl(bool pause)
        {
            _pause = pause;
        }

        public void Execute()
        {
            if (_pause)
            {
                _process.PauseProcess();
            }
            else
            {
                _process.ResumeProcess();
            }
        }
    }
}
