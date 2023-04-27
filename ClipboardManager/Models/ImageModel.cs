using System.Windows.Input;
using System.Windows.Media.Imaging;
using ClipboardManager.Helper;

namespace Models
{
    public class ImageModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] ImageData { get; set; }
        public BitmapSource ImageSource { get; set; }
        public ICommand CopyCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
    }
}
