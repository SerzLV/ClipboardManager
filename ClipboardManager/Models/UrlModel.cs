using System.Windows.Input;

namespace Models
{
    public class UrlModel
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public ICommand CopyCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand OpenLinkCommand { get; set; }
    }
}
