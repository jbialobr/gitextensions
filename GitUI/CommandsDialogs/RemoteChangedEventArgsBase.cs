namespace GitUI.CommandsDialogs
{
    public class RemoteChangedEventArgsBase
    {
        public RemoteChangedEventArgsBase(string remoteName)
        {
            RemoteName = remoteName;
        }

        public string RemoteName { get; }
    }
}