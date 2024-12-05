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
//using System.ComponentModel;
using System.Drawing;
//using System.Data;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using Advanced_Combat_Tracker;
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;
//using System.Threading;

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


            PopulateRegexArray();
			SetupEQ1EnglishEnvironment();

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
        /*
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
        */


        //#region Parsing
        char[] chrApos = new char[] { '\'', '’' };
        char[] chrSpaceApos = new char[] { ' ', '\'', '’' };
        Regex[] regexArray;
        const string logTimeStampRegexStr = @"\(\d{10}\)\[.{24}\] ";
        //DateTime lastWardTime = DateTime.MinValue;
        //long lastWardAmount = 0;
        //string lastWardedTarget = string.Empty;
        //DateTime lastInterceptTime = DateTime.MinValue;
        //long lastInterceptAmount = 0;
        //string lastInterceptTarget = string.Empty;
        //string lastIntercepter = string.Empty;
        Regex engKillSplit = new Regex("(?<mob>.+?) in .+", RegexOptions.Compiled);
        Regex petSplit = new Regex(@"(?<petName>[A-z]* ?)<(?<attacker>[A-z]+)[’'의の](?<s>s?) (?<petClass>.+)>", RegexOptions.Compiled);

        private void PopulateRegexArray()
        {

            regexArray = new Regex[14];
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Clear();
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(0, Color.Gray); //Melee hit.
            //regexArray[0] = new Regex(@"\[.*\] (?<attacker>.+) (?<attacktype>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn)(?:s|es)? (?<victim>.+) for (?<damage>[\d]+) points? of damage.", RegexOptions.Compiled);
            regexArray[0] = new Regex(@"\[.*\] (?<attacker>.+) (?<attacktype>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn|shoot)(?:s|es)? (?<victim>.+) for (?<damage>[\d]+) points of ?(?<special>.+)? damage.", RegexOptions.Compiled);
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(1, Color.Red); //Personal spell casting, haven't attempted yet.
			regexArray[1] = new Regex(@"\[.*\] (?<attacker>.+) are healed (?<victim>.+) for (?<healed>.+) points.", RegexOptions.Compiled);
			ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(2, Color.Red); //Dot1.
			regexArray[2] = new Regex(@"\[.*\] (?<victim>.+) has taken (?<damage>[\d]+) damage from your (?<DotType>.+)\. ?(?<special>.+)?", RegexOptions.Compiled);
			//regexArray[2] = new Regex(@"\[.*\] (?<victim>.+) has taken (?<damage>[\d]+) damage from your (?<DotType>.+)\.", RegexOptions.Compiled);
			ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(3, Color.Red);//Dot2.
            regexArray[3] = new Regex(@"\[.*\] (?<victim>.+) has taken (?<damage>[\d]+) damage from (?<DotType>.+) by (?<attacker>.+)\. ?(?<special>.+)?", RegexOptions.Compiled);
            //regexArray[3] = new Regex(@"\[.*\] (?<victim>.+) has taken (?<damage>[\d]+) damage from (?<DotType>.+) by (?<attacker>.+)\. ?(?<special>.+)?", RegexOptions.Compiled);
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(4, Color.DarkRed); //Zone change.
            regexArray[4] = new Regex(@"\[.*\] You have entered (?<zone>.+)\.", RegexOptions.Compiled);
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(5, Color.ForestGreen); //Killed1.
            regexArray[5] = new Regex(@"\[.*\] (?<attacker>.+) have slain (?<victim>.+)!", RegexOptions.Compiled);
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(6, Color.Black); //Killed2.
            regexArray[6] = new Regex(@"\[.*\] (?<victim>.+) has been slain by (?<attacker>.+)!", RegexOptions.Compiled);
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(7, Color.Black); //Resist.
            regexArray[7] = new Regex(@"\[.*\] (?<attacker>.+) hit (?<victim>.+) for (?<damage>[\d]+) points of (?<damageType>.+) damage by (?<skillType>.+)\. ?(?<special>.+)?", RegexOptions.Compiled);
            //regexArray[7] = new Regex(@"\[.*\] (?<attacker>.+) hit (?<victim>.+) for (?<damage>[\d]+) points of (?<damageType>.+) damage by (?<skillType>.+)\.", RegexOptions.Compiled);
            ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(8, Color.DarkOrchid); //Damageshield
            //regexArray[8] = new Regex(@"\[.*\] (?<victim>.+) is (?<attackType>.+) by (?<attacker>.+) (?<skillType>thorns) for (?<damage>[\d]+) points of (?<damageType>.+) damage.", RegexOptions.Compiled);
			regexArray[8] = new Regex(@"\[.*\] (?<victim>.+) was (?<attacktype>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn|shoot)(?:s|es)? for (?<damage>[\d]+) points of damage.", RegexOptions.Compiled);
			ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(9, Color.DodgerBlue); //Heal over time.
			//regexArray[9] = new Regex(@"\[.*\] (?<attacker>.+) healed (?<victim>.+) for (?<healed>.+) (?<overhealed>.+) hit points by (?<healType>.+)\. ?(?<special>.+)?", RegexOptions.Compiled);
			regexArray[9] = new Regex(@"\[.*\] (?<attacker>.+) healed (?<victim>.+) for (?<healed>.+) (?<overhealed>.+) hit points by (?<healType>.+)\.", RegexOptions.Compiled);

			ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(10, Color.Black); //Direct healing. //optional hit before points gets LoH here.
			regexArray[10] = new Regex(@"\[.*\] (?<attacker>.+) have healed (?<victim>.+) for (?<healed>.+) ?(?<special>.+)? points.", RegexOptions.Compiled);
			//regexArray[10] = new Regex(@"\[.*\] (?<victim>.+) resisted your (?<skillType>.+)\!", RegexOptions.Compiled);
			ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(11, Color.Black); //Lifetap healing.
			//You have been healed for X points of damage.
			regexArray[11] = new Regex(@"\[.*\] (?<attacker>.+) have been healed for (?<healed>.+) points of damage.", RegexOptions.Compiled);
			ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(12, Color.Black); //Melee avoid1.
			regexArray[12] = new Regex(@"\[.*\] (?<attacker>.+) tries to (?<attackType>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn|shoot) (?<victim>.+), but (?<why>.+)!", RegexOptions.Compiled);
			ActGlobals.oFormEncounterLogs.LogTypeToColorMapping.Add(13, Color.Black); //Melee avoid2.
			//regexArray[13] = new Regex(@"\[.*\] (?<attacker>.+) tries to (?<attackType>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn|shoot) (?<victim>.+), but (?<why>.+)!", RegexOptions.Compiled);
			regexArray[13] = new Regex(@"\[.*\] (?<attacker>.+) tries to (?<attackType>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn|shoot) (?<victim>.+), but (?<why>.+)!", RegexOptions.Compiled);
			//regexArray[13] = new Regex(@"\[.*\] (?<attacker>.+) tries to (?<attackType>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn) (?<victim>.+), but (?<why>.+)!", RegexOptions.Compiled);
		





        }
        void BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
        {
            if (NotQuickFail(logInfo))
            {
                for (int i = 0; i < regexArray.Length; i++)
                {
                    Match reMatch = regexArray[i].Match(logInfo.logLine);
                    if (reMatch.Success)
                    {
                        logInfo.detectedType = i + 1;
                        LogExeEnglish(reMatch, i + 1, logInfo.logLine, isImport);
                        break;
                    }
                }
            }
        }
        //string[] matchKeywords = new string[] { "damage", "point", ", but", "killed", "command", "entered", "hate", "dispel", "relieve", "reduces" };
        string[] matchKeywords = new string[] { "damage", "points", ", but", "slain", "entered" }; //"resisted"
        private bool NotQuickFail(LogLineEventArgs logInfo)
        {
            for (int i = 0; i < matchKeywords.Length; i++)
            {
                if (logInfo.logLine.Contains(matchKeywords[i]))
                    return true;
            }

            return false;
        }
        private void LogExeEnglish(Match reMatch, int logMatched, string logLine, bool isImport)
        {
            string attacker, attackType, victim, damage, skillType, special, special2, special3, damageType, healed, evade;
			//string critStr;
			Dnum Avoid;
            List<string> attackingTypes = new List<string>();
            List<string> damages = new List<string>();
			//SwingTypeEnum swingType;
			bool critical = false, twincast = false, lucky = false;
            string[] engNameSkillSplit;
            //List<DamageAndType> damageAndTypeArr;

            DateTime time = ActGlobals.oFormActMain.LastKnownTime;

            //Dnum failType;
            int gts = ActGlobals.oFormActMain.GlobalTimeSorter;

			switch (logMatched)
			{
				case 1: //Melee hit.
					if (reMatch.Groups[1].Value.Length > 70)
						break;

					attacker = reMatch.Groups[1].Value;
					attackType = reMatch.Groups[2].Value;
					victim = reMatch.Groups[3].Value;
					damage = reMatch.Groups[4].Value;
					special = reMatch.Groups[5].Value;
					//skillType = reMatch.Groups[6].Value;


					if (victim.ToUpper() == "YOU")
						victim = ActGlobals.charName;
					if (attacker.ToUpper() == "YOU")
						attacker = ActGlobals.charName;


					//[Sun Nov 10 04:45:40 2024] a shiverback was hit by non-melee for 25 points of damage. //DS.
					//[Sat Nov 16 06:35:42 2024] attacker hit Terror for 90 points of non - melee damage. //Spell damage/proc.
					//[Sat Nov 16 06:35:39 2024] attacker hit Terror for 175 points of non - melee damage. //Proc.
					//[Sat Nov 16 06:35:47 2024] You have healed victim for 307 points. //Healing.
					//You have been healed for 772 points of damage.






					MasterSwing ms1;

					

					if (special.Count() > 0)
					{
						//[Thu Nov 28 12:35:03 2024] You are immolated by raging energy.  You have taken 211 points of damage.
						//Example of the character parsing taking damage. Parsing this is annoying, might be doable.
						//Assigning [Thu Nov 28 12:33:55 2024] You begin casting Lifespike.
						//Doesn't appear doable due to time lag betweeen casting, and the spell landing, which could be dozens of parse lines.
						//It seems only doable if the casting is directly followed by the spell landing, always. Otherwise it could be a proc inbetween, or any number of attacks.

						//Spells and procs.
						ms1 = new MasterSwing((int)SwingTypeEnum.NonMelee, critical, Int64.Parse(damage), time, gts, "Magic", attacker, "Spell", victim);
					}
					else
					{
						//Melee.
						ms1 = new MasterSwing((int)SwingTypeEnum.Melee, critical, Int64.Parse(damage), time, gts, attackType, attacker, "Physical", victim);
					}


					if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
						ActGlobals.oFormActMain.AddCombatAction(ms1);



					break;

				case 2: //Personal spell cast linking. Not reliable. See Case1 explanation above.
					if (reMatch.Groups[1].Value.Length > 70)
						break;

					//attacker = reMatch.Groups[1].Value;
					attackType = reMatch.Groups[2].Value;
					victim = reMatch.Groups[1].Value;
					damage = reMatch.Groups[2].Value;
					damageType = "Non-melee";
					skillType = "DamageShield";
					//special = reMatch.Groups[6].Value;

					if (victim.ToUpper() == "YOU")
						victim = ActGlobals.charName;
					attacker = ActGlobals.charName;


					MasterSwing ms2 = new MasterSwing((int)SwingTypeEnum.NonMelee, critical, Int64.Parse(damage), time, gts, skillType, attacker, damageType, victim);
					//ms2.Tags["Lucky"] = lucky;
					//ms2.Tags["Twincast"] = twincast;
					if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
						ActGlobals.oFormActMain.AddCombatAction(ms2);


					break;

				case 3: //DOT, your.
					victim = reMatch.Groups[1].Value;
					damage = reMatch.Groups[2].Value;
					//why = reMatch.Groups[4].Value;
					skillType = reMatch.Groups[3].Value;
					special = reMatch.Groups[4].Value;

					attacker = ActGlobals.charName;



					MasterSwing ms3 = new MasterSwing((int)SwingTypeEnum.NonMelee, critical, Int64.Parse(damage), time, gts, skillType, attacker, "DOT", victim);
					//ms3.Tags["Lucky"] = lucky;
					//ms3.Tags["Twincast"] = twincast;
					if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
						ActGlobals.oFormActMain.AddCombatAction(ms3);

					break;
				case 4: //DOT Damage.

					victim = reMatch.Groups[1].Value;
					damage = reMatch.Groups[2].Value;
					//why = reMatch.Groups[4].Value;
					skillType = reMatch.Groups[3].Value; //Possibly starts with your, which needs to be parsed out.
					attacker = reMatch.Groups[4].Value;
					special = reMatch.Groups[5].Value;




					MasterSwing ms4 = new MasterSwing((int)SwingTypeEnum.NonMelee, critical, Int64.Parse(damage), time, gts, skillType, attacker, "DOT", victim);
					//ms4.Tags["Lucky"] = lucky;
					//ms4.Tags["Twincast"] = twincast;

					if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
						ActGlobals.oFormActMain.AddCombatAction(ms4);





					break;
				case 5://Zone change.


					if (!string.IsNullOrWhiteSpace(reMatch.Groups[1].Value.Trim()))
						ActGlobals.oFormActMain.ChangeZone(reMatch.Groups[1].Value.Trim());


					break;
				case 6://Killing1


					attacker = reMatch.Groups[1].Value;
					victim = reMatch.Groups[2].Value;


					if (victim.ToUpper() == "YOU")
						victim = ActGlobals.charName;
					if (attacker.ToUpper() == "YOU")
						attacker = ActGlobals.charName;


					//MasterSwing ms6 = new MasterSwing((int)SwingTypeEnum.NonMelee, false, Dnum.Death, time, gts, skillname, attacker, "", victim);

					//Dnum.Death needs to be rework to something else I think. I imagine I can put whatever I want in there like I did for avoidance.
					//Death is fine, killing, not sure what I want instead.

					// only log death if currently in combat.
					if (ActGlobals.oFormActMain.InCombat)
						if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
							ActGlobals.oFormActMain.AddCombatAction((int)SwingTypeEnum.NonMelee, false, "None", attacker, "Killing", Dnum.Death, time, gts, victim, "Death");
					//ActGlobals.oFormActMain.AddCombatAction(ms6);



					/*
                  
                    ActGlobals.oFormSpellTimers.RemoveTimerMods(victim);
                    ActGlobals.oFormSpellTimers.DispellTimerMods(victim);
                    if (ActGlobals.oFormActMain.InCombat)
                    {
                        ActGlobals.oFormActMain.AddCombatAction((int)swingType, false, "None", attacker, "Killing", Dnum.Death, time, gts, victim, "Death");

                        //if (cbKillEnd.Checked && ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.GetAllies().IndexOf(new CombatantData(attacker, null)) > -1)
                        //{
                        //    EndCombat(true);
                        //}
                    }
                    */
					break;
				case 7: //Killing2
					victim = reMatch.Groups[1].Value;
					attacker = reMatch.Groups[2].Value;


					if (victim.ToUpper() == "YOU")
						victim = ActGlobals.charName;
					if (attacker.ToUpper() == "YOU")
						attacker = ActGlobals.charName;


					//MasterSwing ms6 = new MasterSwing((int)SwingTypeEnum.NonMelee, false, Dnum.Death, time, gts, skillname, attacker, "", victim);

					// only log death if currently in combat.
					if (ActGlobals.oFormActMain.InCombat)
						if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
							ActGlobals.oFormActMain.AddCombatAction((int)SwingTypeEnum.NonMelee, false, "None", attacker, "Killing", Dnum.Death, time, gts, victim, "Death");
					//ActGlobals.oFormActMain.AddCombatAction(ms6);






					break;
				case 8: //Resists
					victim = reMatch.Groups[1].Value;
					//damage = reMatch.Groups[2].Value;
					//why = reMatch.Groups[4].Value;
					skillType = reMatch.Groups[2].Value; 
					special = "None";
					attacker = ActGlobals.charName;



					MasterSwing ms8 = new MasterSwing((int)SwingTypeEnum.NonMelee, false, Dnum.Miss, time, gts, skillType, attacker, "resist", victim);

					if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
					ActGlobals.oFormActMain.AddCombatAction(ms8);






					break;


				case 9: //Damage Shield.
					victim = reMatch.Groups[1].Value;
					attackType = reMatch.Groups[2].Value;
					attacker = reMatch.Groups[3].Value;
					skillType = reMatch.Groups[4].Value;
					damage = reMatch.Groups[5].Value;
					damageType = reMatch.Groups[6].Value;


					if (attacker.ToUpper() == "YOUR") //Handles YOUR thorns.
						attacker = ActGlobals.charName;


					engNameSkillSplit = attacker.Split(chrApos);
					if (engNameSkillSplit.Length > 1)
						attacker = engNameSkillSplit[0];



					MasterSwing ms9 = new MasterSwing((int)SwingTypeEnum.NonMelee, false, Int64.Parse(damage), time, gts, skillType, attacker, attackType, victim);

					if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
                    {
						//ms9.Tags["Lucky"] = lucky;
						//ms9.Tags["Twincast"] = twincast;
						ActGlobals.oFormActMain.AddCombatAction(ms9);
					}
					






					break;

				case 10: //Heal over time.
					if (!ActGlobals.oFormActMain.InCombat)
						break;
					if (reMatch.Groups[1].Value.Length > 60)
						break;
					attacker = reMatch.Groups[1].Value;
					victim = reMatch.Groups[2].Value;
					healed = reMatch.Groups[3].Value;
					//totalhealed = reMatch.Groups[4].Value;
					skillType = reMatch.Groups[4].Value;
					special = reMatch.Groups[5].Value;


					engNameSkillSplit = victim.Split(' ');
					if (engNameSkillSplit.Length > 1)
						victim = engNameSkillSplit[0];

					if (victim == "itself" | victim == "himself" | victim == "herself")
						victim = attacker;

					if (victim.ToUpper() == "YOU")
						victim = ActGlobals.charName;
					if (attacker.ToUpper() == "YOU")
						attacker = ActGlobals.charName;



					MasterSwing ms10 = new MasterSwing((int)SwingTypeEnum.Healing, critical, "None", Int64.Parse(healed), time, gts, skillType, attacker, "Hitpoints", victim);
					ms10.Tags["Lucky"] = lucky;
					ms10.Tags["Twincast"] = twincast;
					ActGlobals.oFormActMain.AddCombatAction(ms10);



					break;
				case 11: //Healing Direct
					if (!ActGlobals.oFormActMain.InCombat)
						break;
					if (reMatch.Groups[1].Value.Length > 60)
						break;
					attacker = reMatch.Groups[1].Value;
					victim = reMatch.Groups[2].Value;
					healed = reMatch.Groups[3].Value;
					//totalhealed = reMatch.Groups[4].Value;
					skillType = reMatch.Groups[4].Value;
					special = reMatch.Groups[5].Value;


					engNameSkillSplit = victim.Split(' ');
					if (engNameSkillSplit.Length > 1)
						victim = engNameSkillSplit[0];

					if (victim == "itself" | victim == "himself" | victim == "herself")
						victim = attacker;

					if (victim.ToUpper() == "YOU")
						victim = ActGlobals.charName;
					if (attacker.ToUpper() == "YOU")
						attacker = ActGlobals.charName;

					//MasterSwing ms11 = new MasterSwing((int)SwingTypeEnum.Healing, critical, "None", Int64.Parse(healed), time, gts, skillType, attacker, "Hitpoints", victim);
					//ms11.Tags["Lucky"] = lucky;
					//ms11.Tags["Twincast"] = twincast;
					//ActGlobals.oFormActMain.AddCombatAction(ms11);



					break;
				case 12: //Lifetap heal.
					if (!ActGlobals.oFormActMain.InCombat)
						break;
					if (reMatch.Groups[1].Value.Length > 60)
						break;
					attacker = reMatch.Groups[1].Value;
					victim = reMatch.Groups[2].Value;
					healed = reMatch.Groups[3].Value;
					//totalhealed = reMatch.Groups[4].Value;
					skillType = reMatch.Groups[4].Value;
					special = reMatch.Groups[5].Value;


					//engNameSkillSplit = victim.Split(' ');
					//if (engNameSkillSplit.Length > 1)
					//	victim = engNameSkillSplit[0];

					//if (victim == "itself" | victim == "himself" | victim == "herself")
					//victim = attacker;

					//if (victim.ToUpper() == "YOU")
					//	victim = ActGlobals.charName;
					if (attacker.ToUpper() == "YOU")
						attacker = ActGlobals.charName;
					victim = attacker;


					//SwingtypeEnum should determine where the parsed information is put. Healing/Melee/NonMelee.
					MasterSwing ms12 = new MasterSwing((int)SwingTypeEnum.Healing, critical, "None", Int64.Parse(healed), time, gts, skillType, attacker, "Hitpoints", victim);
					//ms12.Tags["Lucky"] = lucky;
					//ms12.Tags["Twincast"] = twincast;
					ActGlobals.oFormActMain.AddCombatAction(ms12);



					break;

				case 13: //Misses.
					if (reMatch.Groups[1].Value.Length > 60)
						break;
					attacker = reMatch.Groups[1].Value;
					attackType = reMatch.Groups[2].Value;
					victim = reMatch.Groups[3].Value;
					evade = reMatch.Groups[4].Value;
					special = reMatch.Groups[5].Value;


					if (victim.ToUpper() == "YOU")
						victim = ActGlobals.charName;
					if (attacker.ToUpper() == "YOU")
						attacker = ActGlobals.charName;

					Avoid = Dnum.NoDamage;

					engNameSkillSplit = evade.Split(' ');
					if (engNameSkillSplit.Length > 1)
					{
						int TempNum = engNameSkillSplit.Length - 1;
						if (engNameSkillSplit[1] == "dodge" || engNameSkillSplit[1] == "dodges")
							Avoid = new Dnum(-8, "Dodge");
						else if (engNameSkillSplit[1] == "parry" || engNameSkillSplit[1] == "parries")
							Avoid = new Dnum(-6, "Parry");
						else if (engNameSkillSplit[1] == "riposte" || engNameSkillSplit[1] == "ripostes")
							Avoid = new Dnum(-7, "Riposte");
						else if (engNameSkillSplit[1] == "magical")
							Avoid = new Dnum(-11, "MagicalSkin");
						else if (engNameSkillSplit[1] == "block" || engNameSkillSplit[1] == "blocks")
							Avoid = new Dnum(-12, "Block");
					}
					else
					{
						if (engNameSkillSplit[0] == "miss" || engNameSkillSplit[0] == "misses")
						{
							Avoid = Dnum.Miss;
						}
					}



					//[Mon Nov 18 05:39:42 2024] A dar ghoul knight tries to hit YOU, but YOU riposte!
					//[Mon Nov 18 05:39:42 2024] You try to crush a dar ghoul knight, but a dar ghoul knight ripostes!
					//[Mon Nov 18 05:39:42 2024] You try to crush a dar ghoul knight, but a dar ghoul knight dodges!
					//but miss!
					//But misses!



					//Note, the victim is the one avoiding.
					if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
					{ 
					ActGlobals.oFormActMain.AddCombatAction((int)SwingTypeEnum.Melee, false, "None", attacker, attackType, Avoid, time, gts, victim, "Physical");
					//MasterSwing ms13 = new MasterSwing((int)SwingTypeEnum.Melee, false, "None", attacker, attackType, Avoid, time, gts, victim, "Physical");
					//ActGlobals.oFormActMain.AddCombatAction(ms13);
					}


					break;
				case 14: //Misses.
					if (reMatch.Groups[1].Value.Length > 60)
						break;
					attacker = reMatch.Groups[1].Value;
					attackType = reMatch.Groups[2].Value;
					victim = reMatch.Groups[3].Value;
					evade = reMatch.Groups[4].Value;
					special = reMatch.Groups[5].Value;


					if (victim.ToUpper() == "YOU")
						victim = ActGlobals.charName;
					if (attacker.ToUpper() == "YOU")
						attacker = ActGlobals.charName;

					Avoid = Dnum.NoDamage;

					engNameSkillSplit = evade.Split(' ');
					if (engNameSkillSplit.Length > 1)
					{
						int TempNum = engNameSkillSplit.Length - 1;
						if (engNameSkillSplit[1] == "dodge" || engNameSkillSplit[1] == "dodges")
							Avoid = new Dnum(-8, "Dodge");
						else if (engNameSkillSplit[1] == "parry" || engNameSkillSplit[1] == "parries")
							Avoid = new Dnum(-6, "Parry");
						else if (engNameSkillSplit[1] == "riposte" || engNameSkillSplit[1] == "ripostes")
							Avoid = new Dnum(-7, "Riposte");
						else if (engNameSkillSplit[1] == "magical")
							Avoid = new Dnum(-11, "MagicalSkin");
						else if (engNameSkillSplit[1] == "block" || engNameSkillSplit[1] == "blocks")
							Avoid = new Dnum(-12, "Block");
					}
					else
					{
						if (engNameSkillSplit[0] == "miss" || engNameSkillSplit[0] == "misses")
						{
							Avoid = Dnum.Miss;
						}
					}
					


					//[Mon Nov 18 05:39:42 2024] A dar ghoul knight tries to hit YOU, but YOU riposte!
					//[Mon Nov 18 05:39:42 2024] You try to crush a dar ghoul knight, but a dar ghoul knight ripostes!
					//[Mon Nov 18 05:39:42 2024] You try to crush a dar ghoul knight, but a dar ghoul knight dodges!
					//but miss!
					//But misses!



					//Note, the victim is the one avoiding.
					if (ActGlobals.oFormActMain.SetEncounter(time, attacker, victim))
						//ActGlobals.oFormActMain.AddCombatAction(ms3);
						ActGlobals.oFormActMain.AddCombatAction((int)SwingTypeEnum.Melee, false, "None", attacker, attackType, Avoid, time, gts, victim, "Physical");


					break;


				default:
                    break;



            }//End switch (logMatched)



        }//End ParseExeEnglish. IE, Main parser function.






























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



		private string GetIntCommas()
		{
			return ActGlobals.mainTableShowCommas ? "#,0" : "0";
		}
		private string GetFloatCommas()
		{
			return ActGlobals.mainTableShowCommas ? "#,0.00" : "0.00";
		}

		private void SetupEQ1EnglishEnvironment()
		{
			CultureInfo usCulture = new CultureInfo("en-US");   // This is for SQL syntax; do not change

			EncounterData.ColumnDefs.Clear();
			//                                                                                      Do not change the SqlDataName while doing localization
			EncounterData.ColumnDefs.Add("EncId", new EncounterData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.EncId; }));
			EncounterData.ColumnDefs.Add("Title", new EncounterData.ColumnDef("Title", true, "VARCHAR(64)", "Title", (Data) => { return Data.Title; }, (Data) => { return Data.Title; }));
			EncounterData.ColumnDefs.Add("StartTime", new EncounterData.ColumnDef("StartTime", true, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : String.Format("{0} {1}", Data.StartTime.ToShortDateString(), Data.StartTime.ToLongTimeString()); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
			EncounterData.ColumnDefs.Add("EndTime", new EncounterData.ColumnDef("EndTime", true, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
			EncounterData.ColumnDefs.Add("Duration", new EncounterData.ColumnDef("Duration", true, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }));
			EncounterData.ColumnDefs.Add("Damage", new EncounterData.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }));
			EncounterData.ColumnDefs.Add("EncDPS", new EncounterData.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.DPS.ToString(GetFloatCommas()); }, (Data) => { return Data.DPS.ToString(usCulture); }));
			EncounterData.ColumnDefs.Add("Zone", new EncounterData.ColumnDef("Zone", false, "VARCHAR(64)", "Zone", (Data) => { return Data.ZoneName; }, (Data) => { return Data.ZoneName; }));
			EncounterData.ColumnDefs.Add("Kills", new EncounterData.ColumnDef("Kills", true, "INT", "Kills", (Data) => { return Data.AlliedKills.ToString(GetIntCommas()); }, (Data) => { return Data.AlliedKills.ToString(); }));
			EncounterData.ColumnDefs.Add("Deaths", new EncounterData.ColumnDef("Deaths", true, "INT", "Deaths", (Data) => { return Data.AlliedDeaths.ToString(); }, (Data) => { return Data.AlliedDeaths.ToString(); }));

			EncounterData.ExportVariables.Clear();
			EncounterData.ExportVariables.Add("n", new EncounterData.TextExportFormatter("n", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-newline"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-newline"].DisplayedText, (Data, SelectiveAllies, Extra) => { return "\n"; }));
			EncounterData.ExportVariables.Add("t", new EncounterData.TextExportFormatter("t", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-tab"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-tab"].DisplayedText, (Data, SelectiveAllies, Extra) => { return "\t"; }));
			EncounterData.ExportVariables.Add("title", new EncounterData.TextExportFormatter("title", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-title"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-title"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "title", Extra); }));
			EncounterData.ExportVariables.Add("duration", new EncounterData.TextExportFormatter("duration", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-duration"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-duration"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "duration", Extra); }));
			EncounterData.ExportVariables.Add("DURATION", new EncounterData.TextExportFormatter("DURATION", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-DURATION"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-DURATION"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DURATION", Extra); }));
			EncounterData.ExportVariables.Add("damage", new EncounterData.TextExportFormatter("damage", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damage"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damage"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damage", Extra); }));
			EncounterData.ExportVariables.Add("damage-m", new EncounterData.TextExportFormatter("damage-m", "Damage M", "Damage divided by 1,000,000 (with two decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damage-m", Extra); }));
			EncounterData.ExportVariables.Add("damage-*", new EncounterData.TextExportFormatter("damage-*", "Damage w/suffix", "Damage divided 1K/M/B/T/Q (with two decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damage-*", Extra); }));
			EncounterData.ExportVariables.Add("DAMAGE-k", new EncounterData.TextExportFormatter("DAMAGE-k", "Short Damage K", "Damage divided by 1,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-k", Extra); }));
			EncounterData.ExportVariables.Add("DAMAGE-m", new EncounterData.TextExportFormatter("DAMAGE-m", "Short Damage M", "Damage divided by 1,000,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-m", Extra); }));
			EncounterData.ExportVariables.Add("DAMAGE-b", new EncounterData.TextExportFormatter("DAMAGE-b", "Short Damage B", "Damage divided by 1,000,000,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-b", Extra); }));
			EncounterData.ExportVariables.Add("DAMAGE-*", new EncounterData.TextExportFormatter("DAMAGE-*", "Short Damage w/suffix", "Damage divided by 1K/M/B/T/Q (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-*", Extra); }));
			EncounterData.ExportVariables.Add("dps", new EncounterData.TextExportFormatter("dps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-dps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-dps"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "dps", Extra); }));
			EncounterData.ExportVariables.Add("dps-*", new EncounterData.TextExportFormatter("dps-*", "DPS w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-dps"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "dps-*", Extra); }));
			EncounterData.ExportVariables.Add("DPS", new EncounterData.TextExportFormatter("DPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-DPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-DPS"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DPS", Extra); }));
			EncounterData.ExportVariables.Add("DPS-k", new EncounterData.TextExportFormatter("DPS-k", "DPS K", "DPS divided by 1,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DPS-k", Extra); }));
			EncounterData.ExportVariables.Add("DPS-m", new EncounterData.TextExportFormatter("DPS-m", "DPS M", "DPS divided by 1,000,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DPS-m", Extra); }));
			EncounterData.ExportVariables.Add("DPS-*", new EncounterData.TextExportFormatter("DPS-*", "DPS w/suffix", "DPS divided by 1K/M/B/T/Q (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DPS-*", Extra); }));
			EncounterData.ExportVariables.Add("encdps", new EncounterData.TextExportFormatter("encdps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-extdps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-extdps"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "encdps", Extra); }));
			EncounterData.ExportVariables.Add("encdps-*", new EncounterData.TextExportFormatter("encdps-*", "Encounter DPS w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-extdps"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "encdps-*", Extra); }));
			EncounterData.ExportVariables.Add("ENCDPS", new EncounterData.TextExportFormatter("ENCDPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-EXTDPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-EXTDPS"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS", Extra); }));
			EncounterData.ExportVariables.Add("ENCDPS-k", new EncounterData.TextExportFormatter("ENCDPS-k", "Short Encounter DPS K", "ENCDPS divided by 1,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS-k", Extra); }));
			EncounterData.ExportVariables.Add("ENCDPS-m", new EncounterData.TextExportFormatter("ENCDPS-m", "Short Encounter DPS M", "ENCDPS divided by 1,000,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS-m", Extra); }));
			EncounterData.ExportVariables.Add("ENCDPS-*", new EncounterData.TextExportFormatter("ENCDPS-*", "Short Encounter DPS w/suffix", "ENCDPS divided by 1K/M/B/T/Q (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS-*", Extra); }));
			EncounterData.ExportVariables.Add("hits", new EncounterData.TextExportFormatter("hits", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-hits"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-hits"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "hits", Extra); }));
			EncounterData.ExportVariables.Add("crithits", new EncounterData.TextExportFormatter("crithits", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-crithits"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-crithits"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "crithits", Extra); }));
			EncounterData.ExportVariables.Add("crithit%", new EncounterData.TextExportFormatter("crithit%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-crithit%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-crithit%"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "crithit%", Extra); }));
			EncounterData.ExportVariables.Add("misses", new EncounterData.TextExportFormatter("misses", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-misses"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-misses"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "misses", Extra); }));
			EncounterData.ExportVariables.Add("hitfailed", new EncounterData.TextExportFormatter("hitfailed", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-hitfailed"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-hitfailed"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "hitfailed", Extra); }));
			EncounterData.ExportVariables.Add("swings", new EncounterData.TextExportFormatter("swings", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-swings"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-swings"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "swings", Extra); }));
			EncounterData.ExportVariables.Add("tohit", new EncounterData.TextExportFormatter("tohit", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-tohit"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-tohit"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "tohit", Extra); }));
			EncounterData.ExportVariables.Add("TOHIT", new EncounterData.TextExportFormatter("TOHIT", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-TOHIT"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-TOHIT"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "TOHIT", Extra); }));
			EncounterData.ExportVariables.Add("maxhit", new EncounterData.TextExportFormatter("maxhit", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhit"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhit"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhit", Extra); }));
			EncounterData.ExportVariables.Add("MAXHIT", new EncounterData.TextExportFormatter("MAXHIT", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHIT"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHIT"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHIT", Extra); }));
			EncounterData.ExportVariables.Add("maxhit-*", new EncounterData.TextExportFormatter("maxhit-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhit"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhit"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhit-*", Extra); }));
			EncounterData.ExportVariables.Add("MAXHIT-*", new EncounterData.TextExportFormatter("MAXHIT-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHIT"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHIT"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHIT-*", Extra); }));
			EncounterData.ExportVariables.Add("healed", new EncounterData.TextExportFormatter("healed", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healed"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healed"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "healed", Extra); }));
			EncounterData.ExportVariables.Add("enchps", new EncounterData.TextExportFormatter("enchps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-exthps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-exthps"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "enchps", Extra); }));
			EncounterData.ExportVariables.Add("enchps-*", new EncounterData.TextExportFormatter("enchps-*", "Encounter HPS w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-exthps"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "enchps-*", Extra); }));
			EncounterData.ExportVariables.Add("ENCHPS", new EncounterData.TextExportFormatter("ENCHPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-EXTHPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-EXTHPS"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS", Extra); }));
			EncounterData.ExportVariables.Add("ENCHPS-k", new EncounterData.TextExportFormatter("ENCHPS-k", "Short ENCHPS K", "ENCHPS divided by 1,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS-k", Extra); }));
			EncounterData.ExportVariables.Add("ENCHPS-m", new EncounterData.TextExportFormatter("ENCHPS-m", "Short ENCHPS M", "ENCHPS divided by 1,000,000 (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS-m", Extra); }));
			EncounterData.ExportVariables.Add("ENCHPS-*", new EncounterData.TextExportFormatter("ENCHPS-*", "Short ENCHPS w/suffix", "ENCHPS divided by 1/K/M/B/T/Q (with no decimal places)", (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS-*", Extra); }));
			EncounterData.ExportVariables.Add("heals", new EncounterData.TextExportFormatter("heals", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-heals"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-heals"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "heals", Extra); }));
			EncounterData.ExportVariables.Add("critheals", new EncounterData.TextExportFormatter("critheals", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-critheals"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-critheals"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "critheals", Extra); }));
			EncounterData.ExportVariables.Add("critheal%", new EncounterData.TextExportFormatter("critheal%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-critheal%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-critheal%"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "critheal%", Extra); }));
			EncounterData.ExportVariables.Add("cures", new EncounterData.TextExportFormatter("cures", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-cures"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-cures"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "cures", Extra); }));
			EncounterData.ExportVariables.Add("maxheal", new EncounterData.TextExportFormatter("maxheal", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxheal"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxheal"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxheal", Extra); }));
			EncounterData.ExportVariables.Add("MAXHEAL", new EncounterData.TextExportFormatter("MAXHEAL", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEAL"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEAL"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEAL", Extra); }));
			EncounterData.ExportVariables.Add("maxhealward", new EncounterData.TextExportFormatter("maxhealward", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhealward"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhealward"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhealward", Extra); }));
			EncounterData.ExportVariables.Add("MAXHEALWARD", new EncounterData.TextExportFormatter("MAXHEALWARD", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEALWARD"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEALWARD"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEALWARD", Extra); }));
			EncounterData.ExportVariables.Add("maxheal-*", new EncounterData.TextExportFormatter("maxheal-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxheal"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxheal"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxheal-*", Extra); }));
			EncounterData.ExportVariables.Add("MAXHEAL-*", new EncounterData.TextExportFormatter("MAXHEAL-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEAL"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEAL"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEAL-*", Extra); }));
			EncounterData.ExportVariables.Add("maxhealward-*", new EncounterData.TextExportFormatter("maxhealward-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhealward"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhealward"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhealward-*", Extra); }));
			EncounterData.ExportVariables.Add("MAXHEALWARD-*", new EncounterData.TextExportFormatter("MAXHEALWARD-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEALWARD"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEALWARD"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEALWARD-*", Extra); }));
			EncounterData.ExportVariables.Add("damagetaken", new EncounterData.TextExportFormatter("damagetaken", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damagetaken"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damagetaken"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damagetaken", Extra); }));
			EncounterData.ExportVariables.Add("damagetaken-*", new EncounterData.TextExportFormatter("damagetaken-*", "Damage Received w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damagetaken"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damagetaken-*", Extra); }));
			EncounterData.ExportVariables.Add("healstaken", new EncounterData.TextExportFormatter("healstaken", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healstaken"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healstaken"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "healstaken", Extra); }));
			EncounterData.ExportVariables.Add("healstaken-*", new EncounterData.TextExportFormatter("healstaken-*", "Healing Received w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healstaken"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "healstaken-*", Extra); }));
			EncounterData.ExportVariables.Add("powerdrain", new EncounterData.TextExportFormatter("powerdrain", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-powerdrain"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerdrain"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "powerdrain", Extra); }));
			EncounterData.ExportVariables.Add("powerdrain-*", new EncounterData.TextExportFormatter("powerdrain-*", "Power Drain w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerdrain"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "powerdrain-*", Extra); }));
			EncounterData.ExportVariables.Add("powerheal", new EncounterData.TextExportFormatter("powerheal", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-powerheal"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerheal"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "powerheal", Extra); }));
			EncounterData.ExportVariables.Add("powerheal-*", new EncounterData.TextExportFormatter("powerheal-*", "Power Replenish w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerheal"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "powerheal-*", Extra); }));
			EncounterData.ExportVariables.Add("kills", new EncounterData.TextExportFormatter("kills", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-kills"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-kills"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "kills", Extra); }));
			EncounterData.ExportVariables.Add("deaths", new EncounterData.TextExportFormatter("deaths", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-deaths"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-deaths"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "deaths", Extra); }));

			CombatantData.ColumnDefs.Clear();
			CombatantData.ColumnDefs.Add("EncId", new CombatantData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.EncId; }, (Left, Right) => { return 0; }));
			CombatantData.ColumnDefs.Add("Ally", new CombatantData.ColumnDef("Ally", false, "CHAR(1)", "Ally", (Data) => { return Data.Parent.GetAllies().Contains(Data).ToString(); }, (Data) => { return Data.Parent.GetAllies().Contains(Data) ? "T" : "F"; }, (Left, Right) => { return Left.Parent.GetAllies().Contains(Left).CompareTo(Right.Parent.GetAllies().Contains(Right)); }));
			CombatantData.ColumnDefs.Add("Name", new CombatantData.ColumnDef("Name", true, "VARCHAR(64)", "Name", (Data) => { return Data.Name; }, (Data) => { return Data.Name; }, (Left, Right) => { return Left.Name.CompareTo(Right.Name); }));
			CombatantData.ColumnDefs.Add("StartTime", new CombatantData.ColumnDef("StartTime", true, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.StartTime.CompareTo(Right.StartTime); }));
			CombatantData.ColumnDefs.Add("EndTime", new CombatantData.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.EndTime.CompareTo(Right.EndTime); }));
			CombatantData.ColumnDefs.Add("Duration", new CombatantData.ColumnDef("Duration", true, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }, (Left, Right) => { return Left.Duration.CompareTo(Right.Duration); }));
			CombatantData.ColumnDefs.Add("Damage", new CombatantData.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
			CombatantData.ColumnDefs.Add("Damage%", new CombatantData.ColumnDef("Damage%", true, "VARCHAR(4)", "DamagePerc", (Data) => { return Data.DamagePercent; }, (Data) => { return Data.DamagePercent; }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
			CombatantData.ColumnDefs.Add("Kills", new CombatantData.ColumnDef("Kills", false, "INT", "Kills", (Data) => { return Data.Kills.ToString(GetIntCommas()); }, (Data) => { return Data.Kills.ToString(); }, (Left, Right) => { return Left.Kills.CompareTo(Right.Kills); }));
			CombatantData.ColumnDefs.Add("Healed", new CombatantData.ColumnDef("Healed", false, "BIGINT", "Healed", (Data) => { return Data.Healed.ToString(GetIntCommas()); }, (Data) => { return Data.Healed.ToString(); }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
			CombatantData.ColumnDefs.Add("Healed%", new CombatantData.ColumnDef("Healed%", false, "VARCHAR(4)", "HealedPerc", (Data) => { return Data.HealedPercent; }, (Data) => { return Data.HealedPercent; }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
			CombatantData.ColumnDefs.Add("CritHeals", new CombatantData.ColumnDef("CritHeals", false, "INT", "CritHeals", (Data) => { return Data.CritHeals.ToString(GetIntCommas()); }, (Data) => { return Data.CritHeals.ToString(); }, (Left, Right) => { return Left.CritHeals.CompareTo(Right.CritHeals); }));
			CombatantData.ColumnDefs.Add("Heals", new CombatantData.ColumnDef("Heals", false, "INT", "Heals", (Data) => { return Data.Heals.ToString(GetIntCommas()); }, (Data) => { return Data.Heals.ToString(); }, (Left, Right) => { return Left.Heals.CompareTo(Right.Heals); }));
			CombatantData.ColumnDefs.Add("Cures", new CombatantData.ColumnDef("Cures", false, "INT", "CureDispels", (Data) => { return Data.CureDispels.ToString(GetIntCommas()); }, (Data) => { return Data.CureDispels.ToString(); }, (Left, Right) => { return Left.CureDispels.CompareTo(Right.CureDispels); }));
			CombatantData.ColumnDefs.Add("PowerDrain", new CombatantData.ColumnDef("PowerDrain", true, "BIGINT", "PowerDrain", (Data) => { return Data.PowerDamage.ToString(GetIntCommas()); }, (Data) => { return Data.PowerDamage.ToString(); }, (Left, Right) => { return Left.PowerDamage.CompareTo(Right.PowerDamage); }));
			CombatantData.ColumnDefs.Add("PowerReplenish", new CombatantData.ColumnDef("PowerReplenish", false, "BIGINT", "PowerReplenish", (Data) => { return Data.PowerReplenish.ToString(GetIntCommas()); }, (Data) => { return Data.PowerReplenish.ToString(); }, (Left, Right) => { return Left.PowerReplenish.CompareTo(Right.PowerReplenish); }));
			CombatantData.ColumnDefs.Add("DPS", new CombatantData.ColumnDef("DPS", false, "DOUBLE", "DPS", (Data) => { return Data.DPS.ToString(GetFloatCommas()); }, (Data) => { return Data.DPS.ToString(usCulture); }, (Left, Right) => { return Left.DPS.CompareTo(Right.DPS); }));
			CombatantData.ColumnDefs.Add("EncDPS", new CombatantData.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.EncDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.EncDPS.ToString(usCulture); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
			CombatantData.ColumnDefs.Add("EncHPS", new CombatantData.ColumnDef("EncHPS", true, "DOUBLE", "EncHPS", (Data) => { return Data.EncHPS.ToString(GetFloatCommas()); }, (Data) => { return Data.EncHPS.ToString(usCulture); }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
			CombatantData.ColumnDefs.Add("Hits", new CombatantData.ColumnDef("Hits", false, "INT", "Hits", (Data) => { return Data.Hits.ToString(GetIntCommas()); }, (Data) => { return Data.Hits.ToString(); }, (Left, Right) => { return Left.Hits.CompareTo(Right.Hits); }));
			CombatantData.ColumnDefs.Add("CritHits", new CombatantData.ColumnDef("CritHits", false, "INT", "CritHits", (Data) => { return Data.CritHits.ToString(GetIntCommas()); }, (Data) => { return Data.CritHits.ToString(); }, (Left, Right) => { return Left.CritHits.CompareTo(Right.CritHits); }));
			CombatantData.ColumnDefs.Add("Avoids", new CombatantData.ColumnDef("Avoids", false, "INT", "Blocked", (Data) => { return Data.Blocked.ToString(GetIntCommas()); }, (Data) => { return Data.Blocked.ToString(); }, (Left, Right) => { return Left.Blocked.CompareTo(Right.Blocked); }));
			CombatantData.ColumnDefs.Add("Misses", new CombatantData.ColumnDef("Misses", false, "INT", "Misses", (Data) => { return Data.Misses.ToString(GetIntCommas()); }, (Data) => { return Data.Misses.ToString(); }, (Left, Right) => { return Left.Misses.CompareTo(Right.Misses); }));
			CombatantData.ColumnDefs.Add("Swings", new CombatantData.ColumnDef("Swings", false, "INT", "Swings", (Data) => { return Data.Swings.ToString(GetIntCommas()); }, (Data) => { return Data.Swings.ToString(); }, (Left, Right) => { return Left.Swings.CompareTo(Right.Swings); }));
			CombatantData.ColumnDefs.Add("HealingTaken", new CombatantData.ColumnDef("HealingTaken", false, "BIGINT", "HealsTaken", (Data) => { return Data.HealsTaken.ToString(GetIntCommas()); }, (Data) => { return Data.HealsTaken.ToString(); }, (Left, Right) => { return Left.HealsTaken.CompareTo(Right.HealsTaken); }));
			CombatantData.ColumnDefs.Add("DamageTaken", new CombatantData.ColumnDef("DamageTaken", true, "BIGINT", "DamageTaken", (Data) => { return Data.DamageTaken.ToString(GetIntCommas()); }, (Data) => { return Data.DamageTaken.ToString(); }, (Left, Right) => { return Left.DamageTaken.CompareTo(Right.DamageTaken); }));
			CombatantData.ColumnDefs.Add("Deaths", new CombatantData.ColumnDef("Deaths", true, "INT", "Deaths", (Data) => { return Data.Deaths.ToString(GetIntCommas()); }, (Data) => { return Data.Deaths.ToString(); }, (Left, Right) => { return Left.Deaths.CompareTo(Right.Deaths); }));
			CombatantData.ColumnDefs.Add("ToHit%", new CombatantData.ColumnDef("ToHit%", false, "FLOAT", "ToHit", (Data) => { return Data.ToHit.ToString(GetFloatCommas()); }, (Data) => { return Data.ToHit.ToString(usCulture); }, (Left, Right) => { return Left.ToHit.CompareTo(Right.ToHit); }));
			CombatantData.ColumnDefs.Add("CritDam%", new CombatantData.ColumnDef("CritDam%", false, "VARCHAR(8)", "CritDamPerc", (Data) => { return Data.CritDamPerc.ToString("0'%"); }, (Data) => { return Data.CritDamPerc.ToString("0'%"); }, (Left, Right) => { return Left.CritDamPerc.CompareTo(Right.CritDamPerc); }));
			CombatantData.ColumnDefs.Add("CritHeal%", new CombatantData.ColumnDef("CritHeal%", false, "VARCHAR(8)", "CritHealPerc", (Data) => { return Data.CritHealPerc.ToString("0'%"); }, (Data) => { return Data.CritHealPerc.ToString("0'%"); }, (Left, Right) => { return Left.CritHealPerc.CompareTo(Right.CritHealPerc); }));

			CombatantData.ColumnDefs.Add("CritTypes", new CombatantData.ColumnDef("CritTypes", true, "VARCHAR(32)", "CritTypes", CombatantDataGetCritTypes, CombatantDataGetCritTypes, (Left, Right) => { return CombatantDataGetCritTypes(Left).CompareTo(CombatantDataGetCritTypes(Right)); }));

			CombatantData.ColumnDefs.Add("Threat +/-", new CombatantData.ColumnDef("Threat +/-", false, "VARCHAR(32)", "ThreatStr", (Data) => { return Data.GetThreatStr("Threat (Out)"); }, (Data) => { return Data.GetThreatStr("Threat (Out)"); }, (Left, Right) => { return Left.GetThreatDelta("Threat (Out)").CompareTo(Right.GetThreatDelta("Threat (Out)")); }));
			CombatantData.ColumnDefs.Add("ThreatDelta", new CombatantData.ColumnDef("ThreatDelta", false, "BIGINT", "ThreatDelta", (Data) => { return Data.GetThreatDelta("Threat (Out)").ToString(GetIntCommas()); }, (Data) => { return Data.GetThreatDelta("Threat (Out)").ToString(); }, (Left, Right) => { return Left.GetThreatDelta("Threat (Out)").CompareTo(Right.GetThreatDelta("Threat (Out)")); }));

			CombatantData.ColumnDefs["Damage"].GetCellForeColor = (Data) => { return Color.DarkRed; };
			CombatantData.ColumnDefs["Damage%"].GetCellForeColor = (Data) => { return Color.DarkRed; };
			CombatantData.ColumnDefs["Healed"].GetCellForeColor = (Data) => { return Color.DarkBlue; };
			CombatantData.ColumnDefs["Healed%"].GetCellForeColor = (Data) => { return Color.DarkBlue; };
			CombatantData.ColumnDefs["PowerDrain"].GetCellForeColor = (Data) => { return Color.DarkMagenta; };
			CombatantData.ColumnDefs["DPS"].GetCellForeColor = (Data) => { return Color.DarkRed; };
			CombatantData.ColumnDefs["EncDPS"].GetCellForeColor = (Data) => { return Color.DarkRed; };
			CombatantData.ColumnDefs["EncHPS"].GetCellForeColor = (Data) => { return Color.DarkBlue; };
			CombatantData.ColumnDefs["DamageTaken"].GetCellForeColor = (Data) => { return Color.DarkOrange; };

			CombatantData.OutgoingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
		{
			{"Auto-Attack (Out)", new CombatantData.DamageTypeDef("Auto-Attack (Out)", -1, Color.DarkGoldenrod)},
			{"Skill/Ability (Out)", new CombatantData.DamageTypeDef("Skill/Ability (Out)", -1, Color.DarkOrange)},
			{"Outgoing Damage", new CombatantData.DamageTypeDef("Outgoing Damage", 0, Color.Orange)},
			{"Healed (Out)", new CombatantData.DamageTypeDef("Healed (Out)", 1, Color.Blue)},
			{"Power Drain (Out)", new CombatantData.DamageTypeDef("Power Drain (Out)", -1, Color.Purple)},
			{"Power Replenish (Out)", new CombatantData.DamageTypeDef("Power Replenish (Out)", 1, Color.Violet)},
			{"Cure/Dispel (Out)", new CombatantData.DamageTypeDef("Cure/Dispel (Out)", 0, Color.Wheat)},
			{"Threat (Out)", new CombatantData.DamageTypeDef("Threat (Out)", -1, Color.Yellow)},
			{"All Outgoing (Ref)", new CombatantData.DamageTypeDef("All Outgoing (Ref)", 0, Color.Black)}
		};
			CombatantData.IncomingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
		{
			{"Incoming Damage", new CombatantData.DamageTypeDef("Incoming Damage", -1, Color.Red)},
			{"Healed (Inc)",new CombatantData.DamageTypeDef("Healed (Inc)", 1, Color.LimeGreen)},
			{"Power Drain (Inc)",new CombatantData.DamageTypeDef("Power Drain (Inc)", -1, Color.Magenta)},
			{"Power Replenish (Inc)",new CombatantData.DamageTypeDef("Power Replenish (Inc)", 1, Color.MediumPurple)},
			{"Cure/Dispel (Inc)", new CombatantData.DamageTypeDef("Cure/Dispel (Inc)", 0, Color.Wheat)},
			{"Threat (Inc)",new CombatantData.DamageTypeDef("Threat (Inc)", -1, Color.Yellow)},
			{"All Incoming (Ref)",new CombatantData.DamageTypeDef("All Incoming (Ref)", 0, Color.Black)}
		};
			CombatantData.SwingTypeToDamageTypeDataLinksOutgoing = new SortedDictionary<int, List<string>>
		{
			{1, new List<string> { "Auto-Attack (Out)", "Outgoing Damage" } },
			{2, new List<string> { "Skill/Ability (Out)", "Outgoing Damage" } },
			{3, new List<string> { "Healed (Out)" } },
			{10, new List<string> { "Power Drain (Out)" } },
			{13, new List<string> { "Power Replenish (Out)" } },
			{20, new List<string> { "Cure/Dispel (Out)" } },
			{16, new List<string> { "Threat (Out)" } }
		};
			CombatantData.SwingTypeToDamageTypeDataLinksIncoming = new SortedDictionary<int, List<string>>
		{
			{1, new List<string> { "Incoming Damage" } },
			{2, new List<string> { "Incoming Damage" } },
			{3, new List<string> { "Healed (Inc)" } },
			{10, new List<string> { "Power Drain (Inc)" } },
			{13, new List<string> { "Power Replenish (Inc)" } },
			{20, new List<string> { "Cure/Dispel (Inc)" } },
			{16, new List<string> { "Threat (Inc)" } }
		};

			CombatantData.DamageSwingTypes = new List<int> { 1, 2 };
			CombatantData.HealingSwingTypes = new List<int> { 3 };

			CombatantData.DamageTypeDataNonSkillDamage = "Auto-Attack (Out)";
			CombatantData.DamageTypeDataOutgoingDamage = "Outgoing Damage";
			CombatantData.DamageTypeDataOutgoingHealing = "Healed (Out)";
			CombatantData.DamageTypeDataIncomingDamage = "Incoming Damage";
			CombatantData.DamageTypeDataIncomingHealing = "Healed (Inc)";

			CombatantData.ExportVariables.Clear();
			CombatantData.ExportVariables.Add("n", new CombatantData.TextExportFormatter("n", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-newline"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-newline"].DisplayedText, (Data, Extra) => { return "\n"; }));
			CombatantData.ExportVariables.Add("t", new CombatantData.TextExportFormatter("t", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-tab"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-tab"].DisplayedText, (Data, Extra) => { return "\t"; }));
			CombatantData.ExportVariables.Add("name", new CombatantData.TextExportFormatter("name", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-name"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-name"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "name", Extra); }));
			CombatantData.ExportVariables.Add("NAME", new CombatantData.TextExportFormatter("NAME", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME", Extra); }));
			CombatantData.ExportVariables.Add("duration", new CombatantData.TextExportFormatter("duration", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-duration"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-duration"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "duration", Extra); }));
			CombatantData.ExportVariables.Add("DURATION", new CombatantData.TextExportFormatter("DURATION", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-DURATION"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-DURATION"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "DURATION", Extra); }));
			CombatantData.ExportVariables.Add("damage", new CombatantData.TextExportFormatter("damage", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damage"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damage"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "damage", Extra); }));
			CombatantData.ExportVariables.Add("damage-m", new CombatantData.TextExportFormatter("damage-m", "Damage M", "Damage divided by 1,000,000 (with two decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "damage-m", Extra); }));
			CombatantData.ExportVariables.Add("damage-b", new CombatantData.TextExportFormatter("damage-b", "Damage B", "Damage divided by 1,000,000,000 (with two decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "damage-b", Extra); }));
			CombatantData.ExportVariables.Add("damage-*", new CombatantData.TextExportFormatter("damage-*", "Damage w/suffix", "Damage divided by 1K/M/B/T/Q (with one decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "damage-*", Extra); }));
			CombatantData.ExportVariables.Add("DAMAGE-k", new CombatantData.TextExportFormatter("DAMAGE-k", "Short Damage K", "Damage divided by 1,000 (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "DAMAGE-k", Extra); }));
			CombatantData.ExportVariables.Add("DAMAGE-m", new CombatantData.TextExportFormatter("DAMAGE-m", "Short Damage M", "Damage divided by 1,000,000 (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "DAMAGE-m", Extra); }));
			CombatantData.ExportVariables.Add("DAMAGE-b", new CombatantData.TextExportFormatter("DAMAGE-b", "Short Damage B", "Damage divided by 1,000,000,000 (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "DAMAGE-b", Extra); }));
			CombatantData.ExportVariables.Add("DAMAGE-*", new CombatantData.TextExportFormatter("DAMAGE-*", "Short Damage w/suffix", "Damage divided by 1K/M/B/T/Q (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "DAMAGE-*", Extra); }));
			CombatantData.ExportVariables.Add("damage%", new CombatantData.TextExportFormatter("damage%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damage%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damage%"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "damage%", Extra); }));
			CombatantData.ExportVariables.Add("dps", new CombatantData.TextExportFormatter("dps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-dps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-dps"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "dps", Extra); }));
			CombatantData.ExportVariables.Add("dps-*", new CombatantData.TextExportFormatter("dps-*", "DPS w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-dps"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "dps-*", Extra); }));
			CombatantData.ExportVariables.Add("DPS", new CombatantData.TextExportFormatter("DPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-DPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-DPS"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "DPS", Extra); }));
			CombatantData.ExportVariables.Add("DPS-k", new CombatantData.TextExportFormatter("DPS-k", "Short DPS K", "Short DPS divided by 1,000 (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "DPS-k", Extra); }));
			CombatantData.ExportVariables.Add("DPS-m", new CombatantData.TextExportFormatter("DPS-m", "Short DPS M", "Short DPS divided by 1,000,000 (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "DPS-m", Extra); }));
			CombatantData.ExportVariables.Add("DPS-*", new CombatantData.TextExportFormatter("DPS-*", "Short DPS w/suffix", "Short DPS divided by 1K/M/B/T/Q (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "DPS-*", Extra); }));
			CombatantData.ExportVariables.Add("encdps", new CombatantData.TextExportFormatter("encdps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-extdps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-extdps"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "encdps", Extra); }));
			CombatantData.ExportVariables.Add("encdps-*", new CombatantData.TextExportFormatter("encdps-*", "Encounter DPS w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-extdps"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "encdps-*", Extra); }));
			CombatantData.ExportVariables.Add("ENCDPS", new CombatantData.TextExportFormatter("ENCDPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-EXTDPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-EXTDPS"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCDPS", Extra); }));
			CombatantData.ExportVariables.Add("ENCDPS-k", new CombatantData.TextExportFormatter("ENCDPS-k", "Short Encounter DPS K", "Short Encounter DPS divided by 1,000 (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCDPS-k", Extra); }));
			CombatantData.ExportVariables.Add("ENCDPS-m", new CombatantData.TextExportFormatter("ENCDPS-m", "Short Encounter DPS M", "Short Encounter DPS divided by 1,000,000 (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCDPS-m", Extra); }));
			CombatantData.ExportVariables.Add("ENCDPS-*", new CombatantData.TextExportFormatter("ENCDPS-*", "Short Encounter DPS w/suffix", "Short Encounter DPS divided by 1K/M/B/T/Q (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCDPS-*", Extra); }));
			CombatantData.ExportVariables.Add("hits", new CombatantData.TextExportFormatter("hits", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-hits"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-hits"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "hits", Extra); }));
			CombatantData.ExportVariables.Add("crithits", new CombatantData.TextExportFormatter("crithits", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-crithits"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-crithits"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "crithits", Extra); }));
			CombatantData.ExportVariables.Add("crithit%", new CombatantData.TextExportFormatter("crithit%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-crithit%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-crithit%"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "crithit%", Extra); }));
			CombatantData.ExportVariables.Add("crittypes", new CombatantData.TextExportFormatter("crittypes", "Critical Types", "Distribution of Critical Types  (Normal|Legendary|Fabled|Mythical)", (Data, Extra) => { return CombatantFormatSwitch(Data, "crittypes", Extra); }));
			CombatantData.ExportVariables.Add("misses", new CombatantData.TextExportFormatter("misses", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-misses"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-misses"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "misses", Extra); }));
			CombatantData.ExportVariables.Add("hitfailed", new CombatantData.TextExportFormatter("hitfailed", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-hitfailed"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-hitfailed"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "hitfailed", Extra); }));
			CombatantData.ExportVariables.Add("swings", new CombatantData.TextExportFormatter("swings", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-swings"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-swings"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "swings", Extra); }));
			CombatantData.ExportVariables.Add("tohit", new CombatantData.TextExportFormatter("tohit", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-tohit"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-tohit"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "tohit", Extra); }));
			CombatantData.ExportVariables.Add("TOHIT", new CombatantData.TextExportFormatter("TOHIT", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-TOHIT"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-TOHIT"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "TOHIT", Extra); }));
			CombatantData.ExportVariables.Add("maxhit", new CombatantData.TextExportFormatter("maxhit", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhit"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhit"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "maxhit", Extra); }));
			CombatantData.ExportVariables.Add("MAXHIT", new CombatantData.TextExportFormatter("MAXHIT", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHIT"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHIT"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "MAXHIT", Extra); }));
			CombatantData.ExportVariables.Add("maxhit-*", new CombatantData.TextExportFormatter("maxhit-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhit"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhit"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "maxhit-*", Extra); }));
			CombatantData.ExportVariables.Add("MAXHIT-*", new CombatantData.TextExportFormatter("MAXHIT-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHIT"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHIT"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "MAXHIT-*", Extra); }));
			CombatantData.ExportVariables.Add("healed", new CombatantData.TextExportFormatter("healed", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healed"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healed"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "healed", Extra); }));
			CombatantData.ExportVariables.Add("healed%", new CombatantData.TextExportFormatter("healed%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healed%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healed%"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "healed%", Extra); }));
			CombatantData.ExportVariables.Add("enchps", new CombatantData.TextExportFormatter("enchps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-exthps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-exthps"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "enchps", Extra); }));
			CombatantData.ExportVariables.Add("enchps-*", new CombatantData.TextExportFormatter("enchps-*", "Encounter HPS w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-exthps"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "enchps-*", Extra); }));
			CombatantData.ExportVariables.Add("ENCHPS", new CombatantData.TextExportFormatter("ENCHPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-EXTHPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-EXTHPS"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCHPS", Extra); }));
			CombatantData.ExportVariables.Add("ENCHPS-k", new CombatantData.TextExportFormatter("ENCHPS-k", "Short Encounter HPS K", "Short Encounter HPS divided by 1,000 (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCHPS-k", Extra); }));
			CombatantData.ExportVariables.Add("ENCHPS-m", new CombatantData.TextExportFormatter("ENCHPS-m", "Short Encounter HPS M", "Short Encounter HPS divided by 1,000,000 (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCHPS-m", Extra); }));
			CombatantData.ExportVariables.Add("ENCHPS-*", new CombatantData.TextExportFormatter("ENCHPS-*", "Short Encounter HPS w/suffix", "Short Encounter HPS divided by 1/K/M/B/T/Q (with no decimal places)", (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCHPS-*", Extra); }));
			CombatantData.ExportVariables.Add("critheals", new CombatantData.TextExportFormatter("critheals", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-critheals"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-critheals"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "critheals", Extra); }));
			CombatantData.ExportVariables.Add("critheal%", new CombatantData.TextExportFormatter("critheal%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-critheal%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-critheal%"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "critheal%", Extra); }));
			CombatantData.ExportVariables.Add("heals", new CombatantData.TextExportFormatter("heals", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-heals"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-heals"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "heals", Extra); }));
			CombatantData.ExportVariables.Add("cures", new CombatantData.TextExportFormatter("cures", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-cures"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-cures"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "cures", Extra); }));
			CombatantData.ExportVariables.Add("maxheal", new CombatantData.TextExportFormatter("maxheal", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxheal"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxheal"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "maxheal", Extra); }));
			CombatantData.ExportVariables.Add("MAXHEAL", new CombatantData.TextExportFormatter("MAXHEAL", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEAL"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEAL"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "MAXHEAL", Extra); }));
			CombatantData.ExportVariables.Add("maxhealward", new CombatantData.TextExportFormatter("maxhealward", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhealward"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhealward"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "maxhealward", Extra); }));
			CombatantData.ExportVariables.Add("MAXHEALWARD", new CombatantData.TextExportFormatter("MAXHEALWARD", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEALWARD"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEALWARD"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "MAXHEALWARD", Extra); }));
			CombatantData.ExportVariables.Add("maxheal-*", new CombatantData.TextExportFormatter("maxheal-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxheal"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxheal"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "maxheal-*", Extra); }));
			CombatantData.ExportVariables.Add("MAXHEAL-*", new CombatantData.TextExportFormatter("MAXHEAL-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEAL"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEAL"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "MAXHEAL-*", Extra); }));
			CombatantData.ExportVariables.Add("maxhealward-*", new CombatantData.TextExportFormatter("maxhealward-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhealward"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhealward"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "maxhealward-*", Extra); }));
			CombatantData.ExportVariables.Add("MAXHEALWARD-*", new CombatantData.TextExportFormatter("MAXHEALWARD-*", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEALWARD"].DisplayedText + " w/ suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEALWARD"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "MAXHEALWARD-*", Extra); }));
			CombatantData.ExportVariables.Add("damagetaken", new CombatantData.TextExportFormatter("damagetaken", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damagetaken"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damagetaken"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "damagetaken", Extra); }));
			CombatantData.ExportVariables.Add("damagetaken-*", new CombatantData.TextExportFormatter("damagetaken-*", "Damage Received w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damagetaken"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "damagetaken-*", Extra); }));
			CombatantData.ExportVariables.Add("healstaken", new CombatantData.TextExportFormatter("healstaken", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healstaken"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healstaken"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "healstaken", Extra); }));
			CombatantData.ExportVariables.Add("healstaken-*", new CombatantData.TextExportFormatter("healstaken-*", "Healing Received w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healstaken"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "healstaken-*", Extra); }));
			CombatantData.ExportVariables.Add("powerdrain", new CombatantData.TextExportFormatter("powerdrain", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-powerdrain"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerdrain"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "powerdrain", Extra); }));
			CombatantData.ExportVariables.Add("powerdrain-*", new CombatantData.TextExportFormatter("powerdrain-*", "Power Drain w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerdrain"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "powerdrain-*", Extra); }));
			CombatantData.ExportVariables.Add("powerheal", new CombatantData.TextExportFormatter("powerheal", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-powerheal"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerheal"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "powerheal", Extra); }));
			CombatantData.ExportVariables.Add("powerheal-*", new CombatantData.TextExportFormatter("powerheal-*", "Power Replenish w/suffix", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerheal"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "powerheal-*", Extra); }));
			CombatantData.ExportVariables.Add("kills", new CombatantData.TextExportFormatter("kills", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-kills"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-kills"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "kills", Extra); }));
			CombatantData.ExportVariables.Add("deaths", new CombatantData.TextExportFormatter("deaths", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-deaths"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-deaths"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "deaths", Extra); }));
			CombatantData.ExportVariables.Add("threatstr", new CombatantData.TextExportFormatter("threatstr", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-threatstr"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-threatstr"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "threatstr", Extra); }));
			CombatantData.ExportVariables.Add("threatdelta", new CombatantData.TextExportFormatter("threatdelta", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-threatdelta"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-threatdelta"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "threatdelta", Extra); }));
			CombatantData.ExportVariables.Add("NAME3", new CombatantData.TextExportFormatter("NAME3", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME3"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME3"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME3", Extra); }));
			CombatantData.ExportVariables.Add("NAME4", new CombatantData.TextExportFormatter("NAME4", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME4"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME4"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME4", Extra); }));
			CombatantData.ExportVariables.Add("NAME5", new CombatantData.TextExportFormatter("NAME5", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME5"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME5"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME5", Extra); }));
			CombatantData.ExportVariables.Add("NAME6", new CombatantData.TextExportFormatter("NAME6", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME6"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME6"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME6", Extra); }));
			CombatantData.ExportVariables.Add("NAME7", new CombatantData.TextExportFormatter("NAME7", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME7"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME7"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME7", Extra); }));
			CombatantData.ExportVariables.Add("NAME8", new CombatantData.TextExportFormatter("NAME8", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME8"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME8"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME8", Extra); }));
			CombatantData.ExportVariables.Add("NAME9", new CombatantData.TextExportFormatter("NAME9", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME9"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME9"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME9", Extra); }));
			CombatantData.ExportVariables.Add("NAME10", new CombatantData.TextExportFormatter("NAME10", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME10"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME10"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME10", Extra); }));
			CombatantData.ExportVariables.Add("NAME11", new CombatantData.TextExportFormatter("NAME11", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME11"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME11"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME11", Extra); }));
			CombatantData.ExportVariables.Add("NAME12", new CombatantData.TextExportFormatter("NAME12", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME12"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME12"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME12", Extra); }));
			CombatantData.ExportVariables.Add("NAME13", new CombatantData.TextExportFormatter("NAME13", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME13"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME13"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME13", Extra); }));
			CombatantData.ExportVariables.Add("NAME14", new CombatantData.TextExportFormatter("NAME14", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME14"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME14"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME14", Extra); }));
			CombatantData.ExportVariables.Add("NAME15", new CombatantData.TextExportFormatter("NAME15", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME15"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME15"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME15", Extra); }));


			DamageTypeData.ColumnDefs.Clear();
			DamageTypeData.ColumnDefs.Add("EncId", new DamageTypeData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.Parent.EncId; }));
			DamageTypeData.ColumnDefs.Add("Combatant", new DamageTypeData.ColumnDef("Combatant", false, "VARCHAR(64)", "Combatant", (Data) => { return Data.Parent.Name; }, (Data) => { return Data.Parent.Name; }));
			DamageTypeData.ColumnDefs.Add("Grouping", new DamageTypeData.ColumnDef("Grouping", false, "VARCHAR(92)", "Grouping", (Data) => { return string.Empty; }, GetDamageTypeGrouping));
			DamageTypeData.ColumnDefs.Add("Type", new DamageTypeData.ColumnDef("Type", true, "VARCHAR(64)", "Type", (Data) => { return Data.Type; }, (Data) => { return Data.Type; }));
			DamageTypeData.ColumnDefs.Add("StartTime", new DamageTypeData.ColumnDef("StartTime", false, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
			DamageTypeData.ColumnDefs.Add("EndTime", new DamageTypeData.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
			DamageTypeData.ColumnDefs.Add("Duration", new DamageTypeData.ColumnDef("Duration", false, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }));
			DamageTypeData.ColumnDefs.Add("Damage", new DamageTypeData.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }));
			DamageTypeData.ColumnDefs.Add("EncDPS", new DamageTypeData.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.EncDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.EncDPS.ToString(usCulture); }));
			DamageTypeData.ColumnDefs.Add("CharDPS", new DamageTypeData.ColumnDef("CharDPS", false, "DOUBLE", "CharDPS", (Data) => { return Data.CharDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.CharDPS.ToString(usCulture); }));
			DamageTypeData.ColumnDefs.Add("DPS", new DamageTypeData.ColumnDef("DPS", false, "DOUBLE", "DPS", (Data) => { return Data.DPS.ToString(GetFloatCommas()); }, (Data) => { return Data.DPS.ToString(usCulture); }));
			DamageTypeData.ColumnDefs.Add("Average", new DamageTypeData.ColumnDef("Average", true, "DOUBLE", "Average", (Data) => { return Data.Average.ToString(GetFloatCommas()); }, (Data) => { return Data.Average.ToString(usCulture); }));
			DamageTypeData.ColumnDefs.Add("Median", new DamageTypeData.ColumnDef("Median", false, "BIGINT", "Median", (Data) => { return Data.Median.ToString(GetIntCommas()); }, (Data) => { return Data.Median.ToString(); }));
			DamageTypeData.ColumnDefs.Add("MinHit", new DamageTypeData.ColumnDef("MinHit", true, "BIGINT", "MinHit", (Data) => { return Data.MinHit.ToString(GetIntCommas()); }, (Data) => { return Data.MinHit.ToString(); }));
			DamageTypeData.ColumnDefs.Add("MaxHit", new DamageTypeData.ColumnDef("MaxHit", true, "BIGINT", "MaxHit", (Data) => { return Data.MaxHit.ToString(GetIntCommas()); }, (Data) => { return Data.MaxHit.ToString(); }));
			DamageTypeData.ColumnDefs.Add("Hits", new DamageTypeData.ColumnDef("Hits", true, "INT", "Hits", (Data) => { return Data.Hits.ToString(GetIntCommas()); }, (Data) => { return Data.Hits.ToString(); }));
			DamageTypeData.ColumnDefs.Add("CritHits", new DamageTypeData.ColumnDef("CritHits", false, "INT", "CritHits", (Data) => { return Data.CritHits.ToString(GetIntCommas()); }, (Data) => { return Data.CritHits.ToString(); }));
			DamageTypeData.ColumnDefs.Add("Avoids", new DamageTypeData.ColumnDef("Avoids", false, "INT", "Blocked", (Data) => { return Data.Blocked.ToString(GetIntCommas()); }, (Data) => { return Data.Blocked.ToString(); }));
			DamageTypeData.ColumnDefs.Add("Misses", new DamageTypeData.ColumnDef("Misses", false, "INT", "Misses", (Data) => { return Data.Misses.ToString(GetIntCommas()); }, (Data) => { return Data.Misses.ToString(); }));
			DamageTypeData.ColumnDefs.Add("Swings", new DamageTypeData.ColumnDef("Swings", true, "INT", "Swings", (Data) => { return Data.Swings.ToString(GetIntCommas()); }, (Data) => { return Data.Swings.ToString(); }));
			DamageTypeData.ColumnDefs.Add("ToHit", new DamageTypeData.ColumnDef("ToHit", false, "FLOAT", "ToHit", (Data) => { return Data.ToHit.ToString(GetFloatCommas()); }, (Data) => { return Data.ToHit.ToString(); }));
			DamageTypeData.ColumnDefs.Add("AvgDelay", new DamageTypeData.ColumnDef("AvgDelay", false, "FLOAT", "AverageDelay", (Data) => { return Data.AverageDelay.ToString(GetFloatCommas()); }, (Data) => { return Data.AverageDelay.ToString(); }));
			DamageTypeData.ColumnDefs.Add("Crit%", new DamageTypeData.ColumnDef("Crit%", false, "VARCHAR(8)", "CritPerc", (Data) => { return Data.CritPerc.ToString("0'%"); }, (Data) => { return Data.CritPerc.ToString("0'%"); }));
			DamageTypeData.ColumnDefs.Add("CritTypes", new DamageTypeData.ColumnDef("CritTypes", true, "VARCHAR(32)", "CritTypes", DamageTypeDataGetCritTypes, DamageTypeDataGetCritTypes));


			AttackType.ColumnDefs.Clear();
			AttackType.ColumnDefs.Add("EncId", new AttackType.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.Parent.Parent.EncId; }, (Left, Right) => { return 0; }));
			AttackType.ColumnDefs.Add("Attacker", new AttackType.ColumnDef("Attacker", false, "VARCHAR(64)", "Attacker", (Data) => { return Data.Parent.Outgoing ? Data.Parent.Parent.Name : string.Empty; }, (Data) => { return Data.Parent.Outgoing ? Data.Parent.Parent.Name : string.Empty; }, (Left, Right) => { return 0; }));
			AttackType.ColumnDefs.Add("Victim", new AttackType.ColumnDef("Victim", false, "VARCHAR(64)", "Victim", (Data) => { return Data.Parent.Outgoing ? string.Empty : Data.Parent.Parent.Name; }, (Data) => { return Data.Parent.Outgoing ? string.Empty : Data.Parent.Parent.Name; }, (Left, Right) => { return 0; }));
			AttackType.ColumnDefs.Add("SwingType", new AttackType.ColumnDef("SwingType", false, "TINYINT", "SwingType", GetAttackTypeSwingType, GetAttackTypeSwingType, (Left, Right) => { return 0; }));
			AttackType.ColumnDefs.Add("Type", new AttackType.ColumnDef("Type", true, "VARCHAR(64)", "Type", (Data) => { return Data.Type; }, (Data) => { return Data.Type; }, (Left, Right) => { return Left.Type.CompareTo(Right.Type); }));
			AttackType.ColumnDefs.Add("StartTime", new AttackType.ColumnDef("StartTime", false, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.StartTime.CompareTo(Right.StartTime); }));
			AttackType.ColumnDefs.Add("EndTime", new AttackType.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.EndTime.CompareTo(Right.EndTime); }));
			AttackType.ColumnDefs.Add("Duration", new AttackType.ColumnDef("Duration", false, "INT", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }, (Left, Right) => { return Left.Duration.CompareTo(Right.Duration); }));
			AttackType.ColumnDefs.Add("Damage", new AttackType.ColumnDef("Damage", true, "BIGINT", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
			AttackType.ColumnDefs.Add("EncDPS", new AttackType.ColumnDef("EncDPS", true, "DOUBLE", "EncDPS", (Data) => { return Data.EncDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.EncDPS.ToString(usCulture); }, (Left, Right) => { return Left.EncDPS.CompareTo(Right.EncDPS); }));
			AttackType.ColumnDefs.Add("CharDPS", new AttackType.ColumnDef("CharDPS", false, "DOUBLE", "CharDPS", (Data) => { return Data.CharDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.CharDPS.ToString(usCulture); }, (Left, Right) => { return Left.CharDPS.CompareTo(Right.CharDPS); }));
			AttackType.ColumnDefs.Add("DPS", new AttackType.ColumnDef("DPS", false, "DOUBLE", "DPS", (Data) => { return Data.DPS.ToString(GetFloatCommas()); }, (Data) => { return Data.DPS.ToString(usCulture); }, (Left, Right) => { return Left.DPS.CompareTo(Right.DPS); }));
			AttackType.ColumnDefs.Add("Average", new AttackType.ColumnDef("Average", true, "DOUBLE", "Average", (Data) => { return Data.Average.ToString(GetFloatCommas()); }, (Data) => { return Data.Average.ToString(usCulture); }, (Left, Right) => { return Left.Average.CompareTo(Right.Average); }));
			AttackType.ColumnDefs.Add("Median", new AttackType.ColumnDef("Median", true, "BIGINT", "Median", (Data) => { return Data.Median.ToString(GetIntCommas()); }, (Data) => { return Data.Median.ToString(); }, (Left, Right) => { return Left.Median.CompareTo(Right.Median); }));
			AttackType.ColumnDefs.Add("MinHit", new AttackType.ColumnDef("MinHit", true, "BIGINT", "MinHit", (Data) => { return Data.MinHit.ToString(GetIntCommas()); }, (Data) => { return Data.MinHit.ToString(); }, (Left, Right) => { return Left.MinHit.CompareTo(Right.MinHit); }));
			AttackType.ColumnDefs.Add("MaxHit", new AttackType.ColumnDef("MaxHit", true, "BIGINT", "MaxHit", (Data) => { return Data.MaxHit.ToString(GetIntCommas()); }, (Data) => { return Data.MaxHit.ToString(); }, (Left, Right) => { return Left.MaxHit.CompareTo(Right.MaxHit); }));
			AttackType.ColumnDefs.Add("Resist", new AttackType.ColumnDef("Resist", true, "VARCHAR(64)", "Resist", (Data) => { return Data.Resist; }, (Data) => { return Data.Resist; }, (Left, Right) => { return Left.Resist.CompareTo(Right.Resist); }));
			AttackType.ColumnDefs.Add("Hits", new AttackType.ColumnDef("Hits", true, "INT", "Hits", (Data) => { return Data.Hits.ToString(GetIntCommas()); }, (Data) => { return Data.Hits.ToString(); }, (Left, Right) => { return Left.Hits.CompareTo(Right.Hits); }));
			AttackType.ColumnDefs.Add("CritHits", new AttackType.ColumnDef("CritHits", false, "INT", "CritHits", (Data) => { return Data.CritHits.ToString(GetIntCommas()); }, (Data) => { return Data.CritHits.ToString(); }, (Left, Right) => { return Left.CritHits.CompareTo(Right.CritHits); }));
			AttackType.ColumnDefs.Add("Avoids", new AttackType.ColumnDef("Avoids", false, "INT", "Blocked", (Data) => { return Data.Blocked.ToString(GetIntCommas()); }, (Data) => { return Data.Blocked.ToString(); }, (Left, Right) => { return Left.Blocked.CompareTo(Right.Blocked); }));
			AttackType.ColumnDefs.Add("Misses", new AttackType.ColumnDef("Misses", false, "INT", "Misses", (Data) => { return Data.Misses.ToString(GetIntCommas()); }, (Data) => { return Data.Misses.ToString(); }, (Left, Right) => { return Left.Misses.CompareTo(Right.Misses); }));
			AttackType.ColumnDefs.Add("Swings", new AttackType.ColumnDef("Swings", true, "INT", "Swings", (Data) => { return Data.Swings.ToString(GetIntCommas()); }, (Data) => { return Data.Swings.ToString(); }, (Left, Right) => { return Left.Swings.CompareTo(Right.Swings); }));
			AttackType.ColumnDefs.Add("ToHit", new AttackType.ColumnDef("ToHit", true, "FLOAT", "ToHit", (Data) => { return Data.ToHit.ToString(GetFloatCommas()); }, (Data) => { return Data.ToHit.ToString(usCulture); }, (Left, Right) => { return Left.ToHit.CompareTo(Right.ToHit); }));
			AttackType.ColumnDefs.Add("AvgDelay", new AttackType.ColumnDef("AvgDelay", false, "FLOAT", "AverageDelay", (Data) => { return Data.AverageDelay.ToString(GetFloatCommas()); }, (Data) => { return Data.AverageDelay.ToString(usCulture); }, (Left, Right) => { return Left.AverageDelay.CompareTo(Right.AverageDelay); }));
			AttackType.ColumnDefs.Add("Crit%", new AttackType.ColumnDef("Crit%", true, "VARCHAR(8)", "CritPerc", (Data) => { return Data.CritPerc.ToString("0'%"); }, (Data) => { return Data.CritPerc.ToString("0'%"); }, (Left, Right) => { return Left.CritPerc.CompareTo(Right.CritPerc); }));
			AttackType.ColumnDefs.Add("CritTypes", new AttackType.ColumnDef("CritTypes", true, "VARCHAR(32)", "CritTypes", AttackTypeGetCritTypes, AttackTypeGetCritTypes, (Left, Right) => { return AttackTypeGetCritTypes(Left).CompareTo(AttackTypeGetCritTypes(Right)); }));


			MasterSwing.ColumnDefs.Clear();
			MasterSwing.ColumnDefs.Add("EncId", new MasterSwing.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.ParentEncounter.EncId; }, (Left, Right) => { return 0; }));
			MasterSwing.ColumnDefs.Add("Time", new MasterSwing.ColumnDef("Time", true, "TIMESTAMP", "STime", (Data) => { return Data.Time.ToString("T"); }, (Data) => { return Data.Time.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.Time.CompareTo(Right.Time); }));
			MasterSwing.ColumnDefs.Add("Attacker", new MasterSwing.ColumnDef("Attacker", true, "VARCHAR(64)", "Attacker", (Data) => { return Data.Attacker; }, (Data) => { return Data.Attacker; }, (Left, Right) => { return Left.Attacker.CompareTo(Right.Attacker); }));
			MasterSwing.ColumnDefs.Add("SwingType", new MasterSwing.ColumnDef("SwingType", false, "TINYINT", "SwingType", (Data) => { return Data.SwingType.ToString(); }, (Data) => { return Data.SwingType.ToString(); }, (Left, Right) => { return Left.SwingType.CompareTo(Right.SwingType); }));
			MasterSwing.ColumnDefs.Add("AttackType", new MasterSwing.ColumnDef("AttackType", true, "VARCHAR(64)", "AttackType", (Data) => { return Data.AttackType; }, (Data) => { return Data.AttackType; }, (Left, Right) => { return Left.AttackType.CompareTo(Right.AttackType); }));
			MasterSwing.ColumnDefs.Add("DamageType", new MasterSwing.ColumnDef("DamageType", true, "VARCHAR(64)", "DamageType", (Data) => { return Data.DamageType; }, (Data) => { return Data.DamageType; }, (Left, Right) => { return Left.DamageType.CompareTo(Right.DamageType); }));
			MasterSwing.ColumnDefs.Add("Victim", new MasterSwing.ColumnDef("Victim", true, "VARCHAR(64)", "Victim", (Data) => { return Data.Victim; }, (Data) => { return Data.Victim; }, (Left, Right) => { return Left.Victim.CompareTo(Right.Victim); }));
			MasterSwing.ColumnDefs.Add("DamageNum", new MasterSwing.ColumnDef("DamageNum", false, "BIGINT", "Damage", (Data) => { return ((long)Data.Damage).ToString(); }, (Data) => { return ((long)Data.Damage).ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
			MasterSwing.ColumnDefs.Add("Damage", new MasterSwing.ColumnDef("Damage", true, "VARCHAR(128)", "DamageString", (Data) => { return Data.Damage.ToString(); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
			MasterSwing.ColumnDefs.Add("Critical", new MasterSwing.ColumnDef("Critical", false, "CHAR(1)", "Critical", (Data) => { return Data.Critical.ToString(); }, (Data) => { return Data.Critical.ToString(usCulture)[0].ToString(); }, (Left, Right) => { return Left.Critical.CompareTo(Right.Critical); }));

			MasterSwing.ColumnDefs.Add("Lucky", new MasterSwing.ColumnDef("Lucky", false, "CHAR(1)", "Lucky", MasterSwingGetLucky, MasterSwingGetLucky, (Left, Right) => { return MasterSwingGetLucky(Left).CompareTo(MasterSwingGetLucky(Right)); }));
			MasterSwing.ColumnDefs.Add("Twincast", new MasterSwing.ColumnDef("Twincast", false, "CHAR(1)", "Twincast", MasterSwingGetTwincast, MasterSwingGetTwincast, (Left, Right) => { return MasterSwingGetTwincast(Left).CompareTo(MasterSwingGetTwincast(Right)); }));

			//MasterSwing.ColumnDefs.Add("Critical", new MasterSwing.ColumnDef("Critical", false, "CHAR(1)", "Critical", (Data) => { return Data.Critical.ToString(); }, (Data) => { return Data.Critical.ToString(usCulture)[0].ToString(); }, (Left, Right) => { return Left.Critical.CompareTo(Right.Critical); }));


			MasterSwing.ColumnDefs.Add("CriticalStr", new MasterSwing.ColumnDef("CriticalStr", true, "VARCHAR(32)", "CriticalStr", (Data) =>
			{
				if (Data.Tags.ContainsKey("CriticalStr"))
					return (string)Data.Tags["CriticalStr"];
				else
					return "None";
			}, (Data) =>
			{
				if (Data.Tags.ContainsKey("CriticalStr"))
					return (string)Data.Tags["CriticalStr"];
				else
					return "None";
			}, (Left, Right) =>
			{
				string left = Left.Tags.ContainsKey("CriticalStr") ? (string)Left.Tags["CriticalStr"] : "None";
				string right = Right.Tags.ContainsKey("CriticalStr") ? (string)Right.Tags["CriticalStr"] : "None";
				return left.CompareTo(right);
			}));
			MasterSwing.ColumnDefs.Add("Special", new MasterSwing.ColumnDef("Special", true, "VARCHAR(64)", "Special", (Data) => { return Data.Special; }, (Data) => { return Data.Special; }, (Left, Right) => { return Left.Special.CompareTo(Right.Special); }));

			foreach (KeyValuePair<string, MasterSwing.ColumnDef> pair in MasterSwing.ColumnDefs)
				pair.Value.GetCellForeColor = (Data) => { return GetSwingTypeColor(Data.SwingType); };

			ActGlobals.oFormActMain.ValidateLists();
			ActGlobals.oFormActMain.ValidateTableSetup();
		}

		
		private string MasterSwingGetLucky(MasterSwing Data)
		{
			//if (Data.Tags.ContainsKey("Lucky"))
			return Data.Tags["Lucky"].ToString();
		}
		private string MasterSwingGetTwincast(MasterSwing Data)
		{
			//if (Data.Tags.ContainsKey("Lucky"))
			return Data.Tags["Twincast"].ToString();
		}
		




		private string CombatantDataGetCritTypes(CombatantData Data)
		{
			AttackType at;
			if (Data.AllOut.TryGetValue(ActGlobals.ActLocalization.LocalizationStrings["attackTypeTerm-all"].DisplayedText, out at))
			{
				return AttackTypeGetCritTypes(at);
			}
			else
				return "-";
		}
		private string DamageTypeDataGetCritTypes(DamageTypeData Data)
		{
			AttackType at;
			if (Data.Items.TryGetValue(ActGlobals.ActLocalization.LocalizationStrings["attackTypeTerm-all"].DisplayedText, out at))
			{
				return AttackTypeGetCritTypes(at);
			}
			else
				return "-";
		}
		private string AttackTypeGetCritTypes(AttackType Data)
		{
			int crit = 0;
			int lCrit = 0;
			int fCrit = 0;
			int mCrit = 0;
			for (int i = 0; i < Data.Items.Count; i++)
			{
				MasterSwing ms = Data.Items[i];
				if (ms.Critical)
				{
					crit++;
					if (!ms.Tags.ContainsKey("CriticalStr"))
						continue;
					if (((string)ms.Tags["CriticalStr"]).Contains("Legendary"))
					{
						lCrit++;
						continue;
					}
					if (((string)ms.Tags["CriticalStr"]).Contains("Fabled"))
					{
						fCrit++;
						continue;
					}
					if (((string)ms.Tags["CriticalStr"]).Contains("Mythical"))
					{
						mCrit++;
						continue;
					}
				}
			}
			float lCritPerc = ((float)lCrit / (float)crit) * 100f;
			float fCritPerc = ((float)fCrit / (float)crit) * 100f;
			float mCritPerc = ((float)mCrit / (float)crit) * 100f;
			if (crit == 0)
				return "-";
			return String.Format("{0:0.0}%L - {1:0.0}%F - {2:0.0}%M", lCritPerc, fCritPerc, mCritPerc);
		}
		private Color GetSwingTypeColor(int SwingType)
		{
			switch (SwingType)
			{
				case 1:
				case 2:
					return Color.Crimson;
				case 3:
					return Color.Blue;
				case 4:
					return Color.DarkRed;
				case 5:
					return Color.DarkOrange;
				case 8:
					return Color.DarkOrchid;
				case 9:
					return Color.DodgerBlue;
				default:
					return Color.Black;
			}
		}
		string EncounterFormatSwitch(EncounterData Data, List<CombatantData> SelectiveAllies, string VarName, string Extra)
		{
			long damage = 0;
			long healed = 0;
			int swings = 0;
			int hits = 0;
			int crits = 0;
			int heals = 0;
			int critheals = 0;
			int cures = 0;
			int misses = 0;
			int hitfail = 0;
			float tohit = 0;
			double dps = 0;
			double hps = 0;
			long healstaken = 0;
			long damagetaken = 0;
			long powerdrain = 0;
			long powerheal = 0;
			int kills = 0;
			int deaths = 0;

			switch (VarName)
			{
				case "maxheal":
					return Data.GetMaxHeal(true, false, false);
				case "MAXHEAL":
					return Data.GetMaxHeal(false, false, false);
				case "maxheal-*":
					return Data.GetMaxHeal(true, false, true);
				case "MAXHEAL-*":
					return Data.GetMaxHeal(false, false, true);
				case "maxhealward":
					return Data.GetMaxHeal(true, true, false);
				case "MAXHEALWARD":
					return Data.GetMaxHeal(false, true, false);
				case "maxhealward-*":
					return Data.GetMaxHeal(true, true, true);
				case "MAXHEALWARD-*":
					return Data.GetMaxHeal(false, true, true);
				case "maxhit":
					return Data.GetMaxHit(true, false);
				case "MAXHIT":
					return Data.GetMaxHit(false, false);
				case "maxhit-*":
					return Data.GetMaxHit(true, true);
				case "MAXHIT-*":
					return Data.GetMaxHit(false, true);
				case "duration":
					return Data.DurationS;
				case "DURATION":
					return Data.Duration.TotalSeconds.ToString("0");
				case "damage":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					return damage.ToString();
				case "damage-m":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					return (damage / 1000000.0).ToString("0.00");
				case "damage-b":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					return (damage / 1000000000.0).ToString("0.00");
				case "damage-*":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					return ActGlobals.oFormActMain.CreateDamageString(damage, true, true);
				case "DAMAGE-k":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					return (damage / 1000.0).ToString("0");
				case "DAMAGE-m":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					return (damage / 1000000.0).ToString("0");
				case "DAMAGE-b":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					return (damage / 1000000000.0).ToString("0");
				case "DAMAGE-*":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					return ActGlobals.oFormActMain.CreateDamageString(damage, true, false);
				case "healed":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					return healed.ToString();
				case "healed-*":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					return ActGlobals.oFormActMain.CreateDamageString(healed, true, true);
				case "swings":
					foreach (CombatantData cd in SelectiveAllies)
						swings += cd.Swings;
					return swings.ToString();
				case "hits":
					foreach (CombatantData cd in SelectiveAllies)
						hits += cd.Hits;
					return hits.ToString();
				case "crithits":
					foreach (CombatantData cd in SelectiveAllies)
						crits += cd.CritHits;
					return crits.ToString();
				case "crithit%":
					foreach (CombatantData cd in SelectiveAllies)
						crits += cd.CritHits;
					foreach (CombatantData cd in SelectiveAllies)
						hits += cd.Hits;
					float crithitperc = (float)crits / (float)hits;
					return crithitperc.ToString("0'%");
				case "heals":
					foreach (CombatantData cd in SelectiveAllies)
						heals += cd.Heals;
					return heals.ToString();
				case "critheals":
					foreach (CombatantData cd in SelectiveAllies)
						critheals += cd.CritHits;
					return critheals.ToString();
				case "critheal%":
					foreach (CombatantData cd in SelectiveAllies)
						critheals += cd.CritHeals;
					foreach (CombatantData cd in SelectiveAllies)
						heals += cd.Heals;
					float crithealperc = (float)critheals / (float)heals;
					return crithealperc.ToString("0'%");
				case "cures":
					foreach (CombatantData cd in SelectiveAllies)
						cures += cd.CureDispels;
					return cures.ToString();
				case "misses":
					foreach (CombatantData cd in SelectiveAllies)
						misses += cd.Misses;
					return misses.ToString();
				case "hitfailed":
					foreach (CombatantData cd in SelectiveAllies)
						hitfail += cd.Blocked;
					return hitfail.ToString();
				case "TOHIT":
					foreach (CombatantData cd in SelectiveAllies)
						tohit += cd.ToHit;
					tohit /= SelectiveAllies.Count;
					return tohit.ToString("0");
				case "DPS":
				case "ENCDPS":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					dps = damage / Data.Duration.TotalSeconds;
					return dps.ToString("0");
				case "DPS-*":
				case "ENCDPS-*":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					dps = damage / Data.Duration.TotalSeconds;
					return ActGlobals.oFormActMain.CreateDamageString((long)dps, true, false);
				case "DPS-k":
				case "ENCDPS-k":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					dps = damage / Data.Duration.TotalSeconds;
					return (dps / 1000.0).ToString("0");
				case "ENCDPS-m":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					dps = damage / Data.Duration.TotalSeconds;
					return (dps / 1000000.0).ToString("0");
				case "ENCHPS":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					hps = healed / Data.Duration.TotalSeconds;
					return hps.ToString("0");
				case "ENCHPS-k":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					hps = healed / Data.Duration.TotalSeconds;
					return (hps / 1000.0).ToString("0");
				case "ENCHPS-m":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					hps = healed / Data.Duration.TotalSeconds;
					return (hps / 1000000.0).ToString("0");
				case "ENCHPS-*":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					hps = healed / Data.Duration.TotalSeconds;
					return ActGlobals.oFormActMain.CreateDamageString((long)hps, true, false);
				case "tohit":
					foreach (CombatantData cd in SelectiveAllies)
						tohit += cd.ToHit;
					tohit /= SelectiveAllies.Count;
					return tohit.ToString("F");
				case "dps":
				case "encdps":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					dps = damage / Data.Duration.TotalSeconds;
					return dps.ToString("F");
				case "dps-k":
				case "encdps-k":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					dps = damage / Data.Duration.TotalSeconds;
					return (dps / 1000.0).ToString("F");
				case "encdps-m":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					dps = damage / Data.Duration.TotalSeconds;
					return (dps / 1000000.0).ToString("F");
				case "encdps-*":
					foreach (CombatantData cd in SelectiveAllies)
						damage += cd.Damage;
					dps = damage / Data.Duration.TotalSeconds;
					return ActGlobals.oFormActMain.CreateDamageString((long)dps, true, true);
				case "enchps":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					hps = healed / Data.Duration.TotalSeconds;
					return hps.ToString("F");
				case "enchps-k":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					hps = healed / Data.Duration.TotalSeconds;
					return (hps / 1000.0).ToString("F");
				case "enchps-m":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					hps = healed / Data.Duration.TotalSeconds;
					return (hps / 1000000.0).ToString("F");
				case "enchps-*":
					foreach (CombatantData cd in SelectiveAllies)
						healed += cd.Healed;
					hps = healed / Data.Duration.TotalSeconds;
					return ActGlobals.oFormActMain.CreateDamageString((long)hps, true, true);
				case "healstaken":
					foreach (CombatantData cd in SelectiveAllies)
						healstaken += cd.HealsTaken;
					return healstaken.ToString();
				case "healstaken-*":
					foreach (CombatantData cd in SelectiveAllies)
						healstaken += cd.HealsTaken;
					return ActGlobals.oFormActMain.CreateDamageString(healstaken, true, true);
				case "damagetaken":
					foreach (CombatantData cd in SelectiveAllies)
						damagetaken += cd.DamageTaken;
					return damagetaken.ToString();
				case "damagetaken-*":
					foreach (CombatantData cd in SelectiveAllies)
						damagetaken += cd.DamageTaken;
					return ActGlobals.oFormActMain.CreateDamageString(damagetaken, true, true);
				case "powerdrain":
					foreach (CombatantData cd in SelectiveAllies)
						powerdrain += cd.PowerDamage;
					return powerdrain.ToString();
				case "powerdrain-*":
					foreach (CombatantData cd in SelectiveAllies)
						powerdrain += cd.PowerDamage;
					return ActGlobals.oFormActMain.CreateDamageString(powerdrain, true, true);
				case "powerheal":
					foreach (CombatantData cd in SelectiveAllies)
						powerheal += cd.PowerReplenish;
					return powerheal.ToString();
				case "powerheal-*":
					foreach (CombatantData cd in SelectiveAllies)
						powerheal += cd.PowerReplenish;
					return ActGlobals.oFormActMain.CreateDamageString(powerheal, true, true);
				case "kills":
					foreach (CombatantData cd in SelectiveAllies)
						kills += cd.Kills;
					return kills.ToString();
				case "deaths":
					foreach (CombatantData cd in SelectiveAllies)
						deaths += cd.Deaths;
					return deaths.ToString();
				case "title":
					return Data.Title;

				default:
					return VarName;
			}
		}
		private string CombatantFormatSwitch(CombatantData Data, string VarName, string Extra)
		{
			int len = 0;
			switch (VarName)
			{
				case "name":
					return Data.Name;
				case "NAME":
					len = Int32.Parse(Extra);
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME3":
					len = 3;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME4":
					len = 4;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME5":
					len = 5;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME6":
					len = 6;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME7":
					len = 7;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME8":
					len = 8;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME9":
					len = 9;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME10":
					len = 10;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME11":
					len = 11;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME12":
					len = 12;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME13":
					len = 13;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME14":
					len = 14;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "NAME15":
					len = 15;
					return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
				case "DURATION":
					return Data.Duration.TotalSeconds.ToString("0");
				case "duration":
					return Data.DurationS;
				case "maxhit":
					return Data.GetMaxHit(true, false);
				case "MAXHIT":
					return Data.GetMaxHit(false, false);
				case "maxhit-*":
					return Data.GetMaxHit(true, true);
				case "MAXHIT-*":
					return Data.GetMaxHit(false, true);
				case "maxheal":
					return Data.GetMaxHeal(true, false, false);
				case "MAXHEAL":
					return Data.GetMaxHeal(false, false, false);
				case "maxheal-*":
					return Data.GetMaxHeal(true, false, true);
				case "MAXHEAL-*":
					return Data.GetMaxHeal(false, false, true);
				case "maxhealward":
					return Data.GetMaxHeal(true, true, false);
				case "MAXHEALWARD":
					return Data.GetMaxHeal(false, true, false);
				case "maxhealward-*":
					return Data.GetMaxHeal(true, true, true);
				case "MAXHEALWARD-*":
					return Data.GetMaxHeal(false, true, true);
				case "damage":
					return Data.Damage.ToString();
				case "damage-k":
					return (Data.Damage / 1000.0).ToString("0.00");
				case "damage-m":
					return (Data.Damage / 1000000.0).ToString("0.00");
				case "damage-b":
					return (Data.Damage / 1000000000.0).ToString("0.00");
				case "damage-*":
					return ActGlobals.oFormActMain.CreateDamageString(Data.Damage, true, true);
				case "DAMAGE-k":
					return (Data.Damage / 1000.0).ToString("0");
				case "DAMAGE-m":
					return (Data.Damage / 1000000.0).ToString("0");
				case "DAMAGE-b":
					return (Data.Damage / 1000000000.0).ToString("0");
				case "DAMAGE-*":
					return ActGlobals.oFormActMain.CreateDamageString(Data.Damage, true, false);
				case "healed":
					return Data.Healed.ToString();
				case "healed-*":
					return ActGlobals.oFormActMain.CreateDamageString(Data.Healed, true, true);
				case "swings":
					return Data.Swings.ToString();
				case "hits":
					return Data.Hits.ToString();
				case "crithits":
					return Data.CritHits.ToString();
				case "critheals":
					return Data.CritHeals.ToString();
				case "crittypes":
					return CombatantDataGetCritTypes(Data);
				case "crithit%":
					return Data.CritDamPerc.ToString("0'%");
				case "critheal%":
					return Data.CritHealPerc.ToString("0'%");
				case "heals":
					return Data.Heals.ToString();
				case "cures":
					return Data.CureDispels.ToString();
				case "misses":
					return Data.Misses.ToString();
				case "hitfailed":
					return Data.Blocked.ToString();
				case "TOHIT":
					return Data.ToHit.ToString("0");
				case "DPS":
					return Data.DPS.ToString("0");
				case "DPS-k":
					return (Data.DPS / 1000.0).ToString("0");
				case "DPS-m":
					return (Data.DPS / 1000000.0).ToString("0");
				case "DPS-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.DPS, true, false);
				case "ENCDPS":
					return Data.EncDPS.ToString("0");
				case "ENCDPS-k":
					return (Data.EncDPS / 1000.0).ToString("0");
				case "ENCDPS-m":
					return (Data.EncDPS / 1000000.0).ToString("0");
				case "ENCDPS-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.EncDPS, true, false);
				case "ENCHPS":
					return Data.EncHPS.ToString("0");
				case "ENCHPS-k":
					return (Data.EncHPS / 1000.0).ToString("0");
				case "ENCHPS-m":
					return (Data.EncHPS / 1000000.0).ToString("0");
				case "ENCHPS-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.EncHPS, true, false);
				case "tohit":
					return Data.ToHit.ToString("F");
				case "dps":
					return Data.DPS.ToString("F");
				case "dps-k":
					return (Data.DPS / 1000.0).ToString("F");
				case "dps-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.DPS, true, true);
				case "encdps":
					return Data.EncDPS.ToString("F");
				case "encdps-k":
					return (Data.EncDPS / 1000.0).ToString("F");
				case "encdps-m":
					return (Data.EncDPS / 1000000.0).ToString("F");
				case "encdps-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.EncDPS, true, true);
				case "enchps":
					return Data.EncHPS.ToString("F");
				case "enchps-k":
					return (Data.EncHPS / 1000.0).ToString("F");
				case "enchps-m":
					return (Data.EncHPS / 1000000.0).ToString("F");
				case "enchps-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.EncHPS, true, true);
				case "healstaken":
					return Data.HealsTaken.ToString();
				case "healstaken-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.HealsTaken, true, true);
				case "damagetaken":
					return Data.DamageTaken.ToString();
				case "damagetaken-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.DamageTaken, true, true);
				case "powerdrain":
					return Data.PowerDamage.ToString();
				case "powerdrain-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.PowerDamage, true, true);
				case "powerheal":
					return Data.PowerReplenish.ToString();
				case "powerheal-*":
					return ActGlobals.oFormActMain.CreateDamageString((long)Data.PowerReplenish, true, true);
				case "kills":
					return Data.Kills.ToString();
				case "deaths":
					return Data.Deaths.ToString();
				case "damage%":
					return Data.DamagePercent;
				case "healed%":
					return Data.HealedPercent;
				case "threatstr":
					return Data.GetThreatStr("Threat (Out)");
				case "threatdelta":
					return Data.GetThreatDelta("Threat (Out)").ToString();
				case "n":
					return "\n";
				case "t":
					return "\t";

				default:
					return VarName;
			}
		}
		private string GetAttackTypeSwingType(AttackType Data)
		{
			int swingType = 100;
			List<int> swingTypes = new List<int>();
			List<MasterSwing> cachedItems = new List<MasterSwing>(Data.Items);
			for (int i = 0; i < cachedItems.Count; i++)
			{
				MasterSwing s = cachedItems[i];
				if (swingTypes.Contains(s.SwingType) == false)
					swingTypes.Add(s.SwingType);
			}
			if (swingTypes.Count == 1)
				swingType = swingTypes[0];

			return swingType.ToString();
		}
		private string GetDamageTypeGrouping(DamageTypeData Data)
		{
			string grouping = string.Empty;

			int swingTypeIndex = 0;
			if (Data.Outgoing)
			{
				grouping += "attacker=" + Data.Parent.Name;
				foreach (KeyValuePair<int, List<string>> links in CombatantData.SwingTypeToDamageTypeDataLinksOutgoing)
				{
					foreach (string damageTypeLabel in links.Value)
					{
						if (Data.Type == damageTypeLabel)
						{
							grouping += String.Format("&swingtype{0}={1}", swingTypeIndex++ == 0 ? string.Empty : swingTypeIndex.ToString(), links.Key);
						}
					}
				}
			}
			else
			{
				grouping += "victim=" + Data.Parent.Name;
				foreach (KeyValuePair<int, List<string>> links in CombatantData.SwingTypeToDamageTypeDataLinksIncoming)
				{
					foreach (string damageTypeLabel in links.Value)
					{
						if (Data.Type == damageTypeLabel)
						{
							grouping += String.Format("&swingtype{0}={1}", swingTypeIndex++ == 0 ? string.Empty : swingTypeIndex.ToString(), links.Key);
						}
					}
				}
			}

			return grouping;
		}

	}
}
