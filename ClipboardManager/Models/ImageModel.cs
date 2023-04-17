using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Models
{
    public class ImageModel
    {
        public string Name { get; set; }
        public BitmapSource ImageSource { get; set; }
        public ICommand CopyCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
    }
}
