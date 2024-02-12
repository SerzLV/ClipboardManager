using System.Windows.Input;

namespace Models
{
    public class TextModel
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public ICommand CopyCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
    }
}
