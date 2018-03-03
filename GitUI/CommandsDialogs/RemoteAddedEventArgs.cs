namespace GitUI.CommandsDialogs
{
    public class RemoteAddedEventArgs : RemoteChangedEventArgsBase
    {
        public RemoteAddedEventArgs(string remoteName) : base(remoteName)
        {
        }
    }
}