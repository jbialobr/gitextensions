using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using GitCommands;
using GitCommands.Settings;

namespace GitUI.Script
{
    public static class ScriptManager
    {
        public class Merger : SettingsContainer< RepoDistSettings >.Merger
        {
            public override bool MergeValues< T >( T higherPriorityValue, T lowerPriorityValue, out T mergedValue, Func<string, T> decode )
            {
                mergedValue = higherPriorityValue;

                if( typeof(T) == typeof( string ) )
                {
                    string higherPriorityString = higherPriorityValue as string;
                    string lowerPriorityString = lowerPriorityValue as string;
                    string mergedString = mergedValue as string;

                    if( !ScriptManager.MergeSettings( higherPriorityString, lowerPriorityString, out mergedString ) )
                        return false;

                    mergedValue = decode( mergedString );
                    return true;
                }

                return false;
            }
        }

        private static BindingList<ScriptInfo> Scripts { get; set; }

        public static void InvalidateScripts()
        {
            Scripts = null;
        }

        public static BindingList<ScriptInfo> GetScripts( GitCommands.GitModule gitModule, RepoDistSettings repoDistSettings = null, bool merge = true )
        {
            if (Scripts == null)
            {
                string xml = null;
                if( repoDistSettings == null )
                    repoDistSettings = RepoDistSettings.CreateEffective( gitModule );
                
                if( merge == true )
                    repoDistSettings.GetValueWithMerger< string >( "ScriptManagerXML", "", s => s, out xml, new Merger() );
                else
                    repoDistSettings.GetValueHere< string >( "ScriptManagerXML", "", s => s, out xml );
                
                DeserializeFromXml( xml );
            }

            return Scripts;
        }

        public static void SetScripts( RepoDistSettings repoDistSettings )
        {
            string xml = ScriptManager.SerializeIntoXml();
            if( repoDistSettings != null )
                repoDistSettings.SetStringHere( "ScriptManagerXML", xml );
        }

        public static ScriptInfo GetScript(string key, GitCommands.GitModule gitModule )
        {
            return GetScript( key, GetScripts( gitModule ) );
        }

        private static ScriptInfo GetScript( string key, BindingList< ScriptInfo > bindingList )
        {
            foreach( ScriptInfo script in bindingList )
                if (script.Name.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                    return script;

            return null;
        }

        public static void RunEventScripts(GitModuleForm form, ScriptEvent scriptEvent)
        {
            foreach (ScriptInfo scriptInfo in GetScripts( form.Module ))
                if (scriptInfo.Enabled && scriptInfo.OnEvent == scriptEvent)
                {
                    if (scriptInfo.AskConfirmation)
                        if (MessageBox.Show(form, String.Format("Do you want to execute '{0}'?", scriptInfo.Name), "Script", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                            continue;

                    ScriptRunner.RunScript(form, form.Module, scriptInfo.Name, null);
                }
        }

        public static bool MergeSettings( string lowerPrioritySettings, string higherPrioritySettings, out string mergedSettings )
        {
            mergedSettings = "";

            BindingList< ScriptInfo > finalList;
            if( !DeserializeFromXml( higherPrioritySettings, out finalList ) )
                return false;

            BindingList< ScriptInfo > additionalList;
            if( !DeserializeFromXml( lowerPrioritySettings, out additionalList ) )
                return false;

            foreach( ScriptInfo scriptInfo in additionalList )
                if( null == GetScript( scriptInfo.Name, finalList ) )
                    finalList.Add( scriptInfo );

            mergedSettings = SerializeIntoXml( finalList );
            return true;
        }

        public static string SerializeIntoXml()
        {
            return SerializeIntoXml( Scripts );
        }

        private static string SerializeIntoXml( BindingList< ScriptInfo > bindingList )
        {
            try
            {
                var sw = new StringWriter();
                var serializer = new XmlSerializer(typeof(BindingList<ScriptInfo>));
                serializer.Serialize(sw, bindingList);
                return sw.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static void DeserializeFromXml(string xml)
        {
            BindingList< ScriptInfo > result;
            DeserializeFromXml( xml, out result );
            Scripts = result;
            if( string.IsNullOrEmpty( xml ) )   //When there is nothing to deserialize, add default scripts
                AddDefaultScripts();
        }

        private static bool DeserializeFromXml( string xml, out BindingList< ScriptInfo > result )
        {
            result = null;

            if (string.IsNullOrEmpty(xml))
            {
                result = new BindingList<ScriptInfo>();
                return true;
            }

            try
            {
                var serializer = new XmlSerializer(typeof(BindingList<ScriptInfo>));
                using (var stringReader = new StringReader(xml))
                {
                    var xmlReader = new XmlTextReader(stringReader);
                    result = serializer.Deserialize(xmlReader) as BindingList<ScriptInfo>;
                }
            }
            catch (Exception ex)
            {
                result = new BindingList<ScriptInfo>();
                DeserializeFromOldFormat(xml);
                
                Trace.WriteLine(ex.Message);                
            }

            if( result == null )
                return false;
            return true;
        }

        private static void AddDefaultScripts()
        {
            ScriptInfo fetchAfterCommitScript = new ScriptInfo();
            fetchAfterCommitScript.HotkeyCommandIdentifier = 9000;
            fetchAfterCommitScript.Name = "Fetch changes after commit";
            fetchAfterCommitScript.Command = "git";
            fetchAfterCommitScript.Arguments = "fetch";
            fetchAfterCommitScript.ShowProgress = true;
            fetchAfterCommitScript.AskConfirmation = true;
            fetchAfterCommitScript.OnEvent = ScriptEvent.AfterCommit;
            fetchAfterCommitScript.AddToRevisionGridContextMenu = false;
            fetchAfterCommitScript.Enabled = false;
            Scripts.Add(fetchAfterCommitScript);

            ScriptInfo updateSubmodulesAfterPullScript = new ScriptInfo();
            updateSubmodulesAfterPullScript.HotkeyCommandIdentifier = 9001;
            updateSubmodulesAfterPullScript.Name = "Update submodules after pull";
            updateSubmodulesAfterPullScript.Command = "git";
            updateSubmodulesAfterPullScript.Arguments = "submodule update --init --recursive";
            updateSubmodulesAfterPullScript.ShowProgress = true;
            updateSubmodulesAfterPullScript.AskConfirmation = true;
            updateSubmodulesAfterPullScript.OnEvent = ScriptEvent.AfterPull;
            updateSubmodulesAfterPullScript.AddToRevisionGridContextMenu = false;
            updateSubmodulesAfterPullScript.Enabled = false;
            Scripts.Add(updateSubmodulesAfterPullScript);

            ScriptInfo userMenuScript = new ScriptInfo();
            userMenuScript.HotkeyCommandIdentifier = 9002;
            userMenuScript.Name = "Example";
            userMenuScript.Command = "c:\\windows\\system32\\calc.exe";
            userMenuScript.Arguments = "";
            userMenuScript.ShowProgress = false;
            userMenuScript.AskConfirmation = false;
            userMenuScript.OnEvent = ScriptEvent.ShowInUserMenuBar;
            userMenuScript.AddToRevisionGridContextMenu = false;
            userMenuScript.Enabled = false;
            Scripts.Add(userMenuScript);

            ScriptInfo openHashOnGitHub = new ScriptInfo();
            openHashOnGitHub.HotkeyCommandIdentifier = 9003;
            openHashOnGitHub.Name = "Open on GitHub";
            openHashOnGitHub.Command = "{openurl}";
            openHashOnGitHub.Arguments = "https://github.com{cDefaultRemotePathFromUrl}/commit/{sHash}";
            openHashOnGitHub.ShowProgress = false;
            openHashOnGitHub.AskConfirmation = false;
            openHashOnGitHub.OnEvent = 0;
            openHashOnGitHub.AddToRevisionGridContextMenu = true;
            openHashOnGitHub.Enabled = false;
            Scripts.Add(openHashOnGitHub);
        }

        private static void DeserializeFromOldFormat(string inputString)
        {
            const string paramSeparator = "<_PARAM_SEPARATOR_>";
            const string scriptSeparator = "<_SCRIPT_SEPARATOR_>";

            if (inputString.Contains(paramSeparator) || inputString.Contains(scriptSeparator))
            {
                Scripts = new BindingList<ScriptInfo>();

                string[] scripts = inputString.Split(new[] { scriptSeparator }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < scripts.Length; i++)
                {
                    string[] parameters = scripts[i].Split(new[] { paramSeparator }, StringSplitOptions.None);

                    ScriptInfo scriptInfo = new ScriptInfo();
                    scriptInfo.Name = parameters[0];
                    scriptInfo.Command = parameters[1];
                    scriptInfo.Arguments = parameters[2];
                    scriptInfo.AddToRevisionGridContextMenu = parameters[3].Equals("yes");
                    scriptInfo.Enabled = true;
                    scriptInfo.ShowProgress = true;

                    Scripts.Add(scriptInfo);
                }
            }
        }
    }
}
