using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using GitCommands;
using GitCommands.Settings;

namespace GitUI.Script
{
    public static class ScriptManager
    {
        public static BindingList<ScriptInfo> GetScripts( GitCommands.GitModule gitModule, RepoDistSettings repoDistSettings = null, bool merge = true )
        {
            BindingList< ScriptInfo > scripts = null;
            
            if (repoDistSettings == null)
                repoDistSettings = RepoDistSettings.CreateEffective(gitModule);

            Func<BindingList<ScriptInfo>, BindingList<ScriptInfo>, BindingList<ScriptInfo>> mergeFnc;
            if (merge)
                mergeFnc = MergeSettings;
            else
                mergeFnc = null;

            // POSSIBLE BUG: In the case that mergeFnc is null, we really need to be attempting
            //               to get our scripts list value from the desired RepoDistSettings object,
            //               even if no such settings exist in it (at that priority level).
            //               This "GetValue" function does not necessarily do a "GetValueHere", and
            //               so we may actually be grabbing the wrong list.
            //scripts = repoDistSettings.GetValue<BindingList<ScriptInfo>>("ScriptManagerXML",
            //                    new BindingList<ScriptInfo>(), DeserializeFromXml, mergeFnc);

            repoDistSettings.GetValueHereWithMerge< BindingList< ScriptInfo > >( "ScriptManagerXML",
                               new BindingList< ScriptInfo >(), DeserializeFromXml, mergeFnc, out scripts );

            return scripts;
        }

        public static void SetScripts( RepoDistSettings repoDistSettings, BindingList< ScriptInfo > scripts )
        {
            Debug.Assert(repoDistSettings != null);

            // POSSIBLE BUG: I'm fairly certain that we really need to be setting
            //               our scripts list value on the desired RepoDistSettings object.
            //               This "SetValue" function does not necessarily do a "SetValueHere", and
            //               so we may actually be setting our list on the wrong priority level.
            //repoDistSettings.SetValue("ScriptManagerXML", scripts, SerializeIntoXml);

            repoDistSettings.SetValueHere< BindingList< ScriptInfo > >( "ScriptManagerXML", scripts, SerializeIntoXml );
        }

        public static ScriptInfo GetScript(string key, GitCommands.GitModule gitModule )
        {
            return GetScript( key, GetScripts( gitModule ) );
        }

        private static ScriptInfo GetScript( string key, BindingList< ScriptInfo > scripts, Dictionary< string, int > dictionary = null )
        {
            if( dictionary == null )
            {
                // Perform a linear search.
                foreach( ScriptInfo script in scripts )
                    if (script.Name.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                        return script;
            }
            else
            {
                key = key.ToLower();
                if( dictionary.ContainsKey( key ) )
                {
                    int index = dictionary[ key ];
                    return scripts[ index ];
                }
            }

            return null;
        }

        private static void BuildDictionary( BindingList< ScriptInfo > scripts, out Dictionary< string, int > dictionary )
        {
            dictionary = new Dictionary< string, int >();
            for( int index = 0; index < scripts.Count; index++ )
            {
                ScriptInfo script = scripts[ index ];
                string key = script.Name.ToLower();
                dictionary.Add( key, index );
            }
        }

        public static void RunEventScripts(GitModuleForm form, ScriptEvent scriptEvent, BindingList< ScriptInfo > scripts = null)
        {
            if( scripts == null )
                scripts = GetScripts( form.Module );
            foreach (ScriptInfo scriptInfo in scripts)
            {
                if (scriptInfo.Enabled && scriptInfo.OnEvent == scriptEvent)
                {
                    if (scriptInfo.AskConfirmation)
                        if (MessageBox.Show(form, String.Format("Do you want to execute '{0}'?", scriptInfo.Name), "Script", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                            continue;

                    ScriptRunner.RunScript(form, form.Module, scriptInfo.Name, null);
                }
            }
        }

        public static BindingList<ScriptInfo> MergeSettings(BindingList<ScriptInfo> lowerPrioritySettings, BindingList<ScriptInfo> higherPrioritySettings)
        {
            BindingList<ScriptInfo> finalList = new BindingList<ScriptInfo>();
            BindingList<ScriptInfo> additionalList = new BindingList<ScriptInfo>();

            finalList.AddAll(higherPrioritySettings);

            // In the case that we don't build a dictionary, we're O(m*n).
            // In the case that we do, we should be O(m log n).
            // For small n, the O(m*n) algorithm may be faster than the O(m log n) version.
            Dictionary< string, int > dictionary = null;
            const int threshold = 30;
            if( finalList.Count > threshold )
                BuildDictionary( finalList, out dictionary );

            foreach (ScriptInfo scriptInfo in lowerPrioritySettings)
                if( null == GetScript( scriptInfo.Name, finalList, dictionary ) )
                    additionalList.Add( scriptInfo );

            finalList.AddAll( additionalList );
            return finalList;
        }

        private static string SerializeIntoXml( BindingList< ScriptInfo > scripts )
        {
            try
            {
                var sw = new StringWriter();
                var serializer = new XmlSerializer(typeof(BindingList<ScriptInfo>));
                serializer.Serialize(sw, scripts);
                return sw.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static BindingList<ScriptInfo> DeserializeFromXml(string xml)
        {
            BindingList< ScriptInfo > result;
            DeserializeFromXml( xml, out result );
            return result;
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
                DeserializeFromOldFormat(xml, out result);
                Trace.WriteLine(ex.Message);                
            }

            if( result == null )
                return false;
            return true;
        }

        // TODO: When/where should we call this?
        private static void AddDefaultScripts( BindingList< ScriptInfo > scripts )
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
            scripts.Add(fetchAfterCommitScript);

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
            scripts.Add(updateSubmodulesAfterPullScript);

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
            scripts.Add(userMenuScript);

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
            scripts.Add(openHashOnGitHub);
        }

        private static void DeserializeFromOldFormat(string inputString, out BindingList< ScriptInfo > scripts)
        {
            const string paramSeparator = "<_PARAM_SEPARATOR_>";
            const string scriptSeparator = "<_SCRIPT_SEPARATOR_>";

            scripts = new BindingList< ScriptInfo >();

            if (inputString.Contains(paramSeparator) || inputString.Contains(scriptSeparator))
            {
                string[] scriptsArray = inputString.Split(new[] { scriptSeparator }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < scriptsArray.Length; i++)
                {
                    string[] parameters = scriptsArray[i].Split(new[] { paramSeparator }, StringSplitOptions.None);

                    ScriptInfo scriptInfo = new ScriptInfo();
                    scriptInfo.Name = parameters[0];
                    scriptInfo.Command = parameters[1];
                    scriptInfo.Arguments = parameters[2];
                    scriptInfo.AddToRevisionGridContextMenu = parameters[3].Equals("yes");
                    scriptInfo.Enabled = true;
                    scriptInfo.ShowProgress = true;

                    scripts.Add(scriptInfo);
                }
            }
        }
    }
}
