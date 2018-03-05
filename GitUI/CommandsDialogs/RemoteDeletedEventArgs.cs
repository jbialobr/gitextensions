namespace GitUI.CommandsDialogs
{
    public class RemoteDeletedEventArgs : RemoteChangedEventArgsBase
    {
        public RemoteDeletedEventArgs(string remoteName) : base(remoteName)
        {
        }
    }
}