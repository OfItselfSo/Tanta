using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TantaCaptureToFileViaReaderWriter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        // SUPER IMPORTANT NOTE: You MUST use [MTAThread] here. If you use [STAThread] you will get the following
        // error when attempting to call ReadSample on the SourceReader object
        //    Unable to cast COM object of type 'System.__ComObject' to interface type 'MediaFoundation.Alt.IMFSourceReaderAsync'. 
        //    This operation failed because the QueryInterface call on the COM component for the interface with IID '{70AE66F2-C809-4E4F-8915-BDCB406B7993}' 
        //    failed due to the following error: No such interface supported (Exception from HRESULT: 0x80004002 (E_NOINTERFACE)).
        [MTAThread]
        //[STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }
    }
}
