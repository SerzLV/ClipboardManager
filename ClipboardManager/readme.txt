Read Me
This code is a MainWindowViewModel class which handles the logic of the main window of an application. 
The purpose of this class is to monitor changes made in the clipboard and copy them to the application's UI, 
categorizing them into different types: files, texts, URLs, and images.

imageName: A string which stores the name of the copied image.
trigger: An integer which is used to indicate whether the copied image is a duplicate.
Public Commands
ClearCommand: A RelayCommand which clears all the copied items and the corresponding UI elements.
CopyCommand: A RelayCommand which copies the selected item to the clipboard.
DeleteCommand: A RelayCommand which deletes the selected item from the list and the corresponding UI element.
OpenLinkCommand: A RelayCommand which opens the selected URL in a new tab of the default browser.

ClipboardContextChange: This method is called when there is a change in the clipboard. 
It processes the dropped files, text, and image from the clipboard and categorizes them into different types.
