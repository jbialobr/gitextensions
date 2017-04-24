using System;
using System.Collections.Specialized;

namespace GitUI.CommitInfo
{
    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs(string command, string aPath, NameValueCollection aParams)
        {
            this.Command = command;
            this.Path = aPath;
            this.Params = aParams ?? new NameValueCollection();
        }

        public readonly string Command;
        public readonly string Path;
        public readonly NameValueCollection Params;
    }
}
