namespace GitUI.CommandsDialogs.SettingsDialog.Pages
{
    public partial class HotkeysSettingsPage : SettingsPageWithHeader
    {
        public HotkeysSettingsPage()
        {
            InitializeComponent();
            Text = "Hotkeys";
            Translate();
        }

        private GitCommands.GitModule gitModule;

        protected override void Init(ISettingsPageHost aPageHost)
        {
            base.Init(aPageHost);

            FormSettings formSettings = aPageHost as FormSettings;
            if( formSettings != null )
                gitModule = formSettings.Module;

            this.controlHotkeys.AssignModule( gitModule );
        }

        protected override void SettingsToPage()
        {
            controlHotkeys.ReloadSettings();
        }

        protected override void PageToSettings()
        {
            controlHotkeys.SaveSettings();
        }
    }
}
