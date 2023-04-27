using System.Diagnostics;
using System.Windows;

namespace ClipboardManager.Helper
{
    public static class OpenLink
    {
        public static void LinkOpenInDefaultBrowser(string url)
        {
            string target = url;
            //Use no more than one assignment when you test this code.
            //string target = "ftp://ftp.microsoft.com";
            //string target = "C:\\Program Files\\Microsoft Visual Studio\\INSTALL.HTM";
            try
            {
                Process.Start(new ProcessStartInfo() { FileName = target, UseShellExecute = true });
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }
    }
}
