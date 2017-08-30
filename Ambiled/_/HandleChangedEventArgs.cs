using System;

namespace Ambiled.Core
{
    public class HandleChangedEventArgs : EventArgs
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }

        public HandleChangedEventArgs(IntPtr handle, string title)
        {
            this.Handle = handle;
            this.Title = title;
        }
    }

}
