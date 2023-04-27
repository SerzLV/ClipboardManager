using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClipboardManager.Helper
{
    public static class OpenFile
    {
        public static void FileOpen(string filePath)
        {
            string target = filePath;
            try
            {
                if (File.Exists(target))
                {
                    Process.Start(new ProcessStartInfo() { FileName = target, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
