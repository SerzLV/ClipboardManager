using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Models
{
    public class TextModel
    {
        public string Text { get; set; }
        public ICommand CopyCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
    }
}
