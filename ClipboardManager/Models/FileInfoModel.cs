using System.Windows.Input;

namespace Models
{
    public class FileInfoModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public ICommand CopyCommand { get; set; }
        public ICommand OpenFileCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
    }
}
