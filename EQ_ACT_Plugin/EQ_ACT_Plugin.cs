// EQ_ACT_Plugin ~ EQ_ACT_Plugin.cs
// 
// Copyright © 2017 Ravahn - All Rights Reserved
// 
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using Advanced_Combat_Tracker;
using System.Xml;
using System.Text.RegularExpressions;

namespace EQ_ACT_Plugin
{
    public partial class EQ_ACT_Plugin : UserControl, IActPluginV1
    {
        Label lblStatus;    // The status label that appears in ACT's Plugin tab
        string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\EQ_ACT_Plugin.config.xml");
        SettingsSerializer xmlSettings;

        public EQ_ACT_Plugin()
        {
            InitializeComponent();
        }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            lblStatus = pluginStatusText;   // Hand the status label's reference to our local var
            pluginScreenSpace.Controls.Add(this);   // Add this UserControl to the tab ACT provides
            this.Dock = DockStyle.Fill; // Expand the UserControl to fill the tab's client space
            xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
            LoadSettings();

            ActGlobals.oFormActMain.LogPathHasCharName = true;
            ActGlobals.oFormActMain.LogFileFilter = "eqlog_*.txt"; // used by history db
            ActGlobals.oFormActMain.CharacterFileNameRegex = RegexCache.CharNameFromFilename;
            ActGlobals.oFormActMain.ZoneChangeRegex = RegexCache.ZoneChange;
            ActGlobals.oFormActMain.GetDateTimeFromLog = new FormActMain.DateTimeLogParser(LogParse.ParseLogDateTime);
            //ActGlobals.oFormActMain.LogFileChanged += OnLogFileChanged;
            ActGlobals.oFormActMain.BeforeLogLineRead += BeforeLogLineRead;

            ActGlobals.oFormActMain.TimeStampLen = "[DAY MON XX HH:MM:SS YYYY] ".Length;


            // Create some sort of parsing event handler.  After the "+=" hit TAB twice and the code will be generated for you.
            //ActGlobals.oFormActMain.AfterCombatAction += new CombatActionDelegate(oFormActMain_AfterCombatAction);

            lblStatus.Text = "Plugin Started";
        }

        public void DeInitPlugin()
        {
            // Unsubscribe from any events you listen to when exiting!
            ActGlobals.oFormActMain.BeforeLogLineRead -= BeforeLogLineRead;

            SaveSettings();
            lblStatus.Text = "Plugin Exited";
        }

        public void BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (LogParse.ParseDamage(logInfo))
                return;
            if (LogParse.ParseDoTTick(logInfo))
                return;
            if (LogParse.ParseMiss(logInfo))
                return;
            if (LogParse.ParseDeath(logInfo))
                return;
            if (LogParse.ParseNonMeleeType(logInfo))
                return;
            if (LogParse.CheckForRegexMatch(logInfo, RegexCache.ChatText))
                return;
            if (LogParse.ParseZone(logInfo))
                return;
            if (LogParse.CheckForRegexMatch(logInfo, RegexCache.IgnoreLine))
                return;

            lstMessages.UIThread(() => lstMessages.Items.Add(logInfo.logLine));
        }


        void LoadSettings()
        {
            //xmlSettings.AddControlSetting(textBox1.Name, textBox1);

            if (File.Exists(settingsFile))
            {
                FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                XmlTextReader xReader = new XmlTextReader(fs);

                try
                {
                    while (xReader.Read())
                    {
                        if (xReader.NodeType == XmlNodeType.Element)
                        {
                            if (xReader.LocalName == "SettingsSerializer")
                            {
                                xmlSettings.ImportFromXml(xReader);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Error loading settings: " + ex.Message;
                }
                xReader.Close();
            }
        }
        void SaveSettings()
        {
            FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8);
            xWriter.Formatting = Formatting.Indented;
            xWriter.Indentation = 1;
            xWriter.IndentChar = '\t';
            xWriter.WriteStartDocument(true);
            xWriter.WriteStartElement("Config");    // <Config>
            xWriter.WriteStartElement("SettingsSerializer");    // <Config><SettingsSerializer>
            xmlSettings.ExportToXml(xWriter);   // Fill the SettingsSerializer XML
            xWriter.WriteEndElement();  // </SettingsSerializer>
            xWriter.WriteEndElement();  // </Config>
            xWriter.WriteEndDocument(); // Tie up loose ends (shouldn't be any)
            xWriter.Flush();    // Flush the file buffer to disk
            xWriter.Close();
        }

        private void cmdClearMessages_Click(object sender, EventArgs e)
        {
            lstMessages.Items.Clear();
        }

        private void cmdCopyProblematic_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (object itm in lstMessages.Items)
                sb.AppendLine((itm ?? "").ToString());

            if (sb.Length > 0)
                System.Windows.Forms.Clipboard.SetText(sb.ToString());
        }
    }
}
