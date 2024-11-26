// EQ_ACT_Plugin ~ RegexCache.cs
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

namespace EQ_ACT_Plugin
{
    public static class RegexCache
    {
        /// <summary>
        /// Zone Change regex
        /// Example: [Sat Nov 04 15:29:08 2017] You have entered Greater Faydark.
        /// </summary>
        public static Regex ZoneChange = new Regex(@"\[.*\] You have entered (?<zone>.+)\.", RegexOptions.Compiled);

        /// <summary>
        /// Character name from Filename regex
        /// Example: eqlog_Ravahn_agnarr.txt
        /// </summary>
        public static Regex CharNameFromFilename = new Regex(@".*\\eqlog_(?<charname>.+)_.*.txt", RegexOptions.Compiled);

        /// <summary>
        /// Damage regex
        /// Example: You pierce a widow hatchling for 2 points of damage.
        /// Example: You pierce a widow hatchling for 2 points of damage.
        /// Example:
        /// </summary>
        public static Regex Damage = new Regex(@"\[.*\] (?<actorname>.+) (?<swingtype>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn)(?:s|es)? (?<targetname>.+) for (?<amount>[\d]+) points? of (?<type>.+\ )?damage.", RegexOptions.Compiled);

        /// <summary>
        /// Damage regex
        /// Example: Orc centurion is pierced by Seedling's thorns for 1 point of non-melee damage.
        /// Example:
        /// </summary>
        public static Regex DamagePassive = new Regex(@"\[.*\] (?<targetname>.+) is (?<swingtype>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn)(ed by|d by) (?<actorname>.+)'s (?<type>.+) for (?<amount>[\d]+) points? of (?<type2>.+\ )?damage.", RegexOptions.Compiled);

        //You have taken 1 damage from Feeble Poison
        public static Regex DamageDoTTick = new Regex(@"\[.*\] (?<targetname>.+) (have taken|has taken|takes) (?<amount>[\d]+) damage (from (?<actorname>.+)'s corpse by (?<type>.+)|from (?<actorname2>.+) by (?<type2>.+)|from (?<type3>.+)|by (?<type4>.+))", RegexOptions.Compiled);

        /// <summary>
        /// Miss regex
        /// Example: You try to pierce a widow hatchling, but miss!
        /// Example: A widow hatchling tries to bite YOU, but misses!
        /// Example: You try to pierce a giant wasp warrior, but a giant wasp warrior dodges!
        /// Example:
        /// </summary>
        public static Regex Miss = new Regex(@"\[.*\] (?<actorname>.+) (try|tries) to (?<swingtype>slash|hit|kick|pierce|bash|punch|crush|bite|maul|backstab|claw|strike|sting|burn) (?<targetname>.+), but ((?<miss>miss(es)?)|(.+) (?<dodge>dodge(s)?)|(.+) (?<parry>parr(ies)?)|(.+) (?<riposte>riposte(s)?)|)!", RegexOptions.Compiled);

        /// <summary>
        /// Slay / Death regex
        /// Example: You have slain orc pawn!
        /// Example:
        /// </summary>
        public static Regex Death = new Regex(@"\[.*\] (?<actorname>.+) (have|has) slain (by )?(?<targetname>.+)!", RegexOptions.Compiled);

        /// <summary>
        /// Slay / Death regex
        /// Example: Orc centurion has been slain by Renfail!
        /// Example:
        /// </summary>
        public static Regex Death2 = new Regex(@"\[.*\] (?<targetname>.+) (has been) slain by (?<actorname>.+)!", RegexOptions.Compiled);

        /// <summary>
        /// Non-melee swing type detection
        /// Example: You begin casting Blast of Cold.
        /// </summary>
        public static Regex NonMeleeType = new Regex(@"\[.*\] (?<actorname>.+) begins? (casting|to cast) (?<skillname>.+)", RegexOptions.Compiled);
        //Ravahn hit orc pawn for 12 points of non-melee damage.
        //Orc pawn's skin goes numb.


        public static Regex ChatText = new Regex(@"\[.*\] (?<actorname>.+) (tells|says|shouts|auctions)(\ )?(?<channelname>.+)?, '(?<text>.+)'", RegexOptions.Compiled);

        public static Regex IgnoreLine = new Regex(@"\[.*\] (" +
            "You must first click on the being you wish to attack!|" +
            "Right click on the NPC to consider (it|her)|" +
            "Stand close to and right click on the (Player|NPC) to|" +
            "You are (no longer|now) A.F.K. \\(Away From Keyboard\\)|" +
            "(.+?) has completed achievement:|" +
            "LOADING, PLEASE WAIT\\.\\.\\.|" +
            "Targeted \\((NPC|Player|Corpse|Merchant)\\):|" +
            "Auto attack is (on|off)|" +
            "Logging to|" +
            "(.+?) regards you indifferently|" +
            "(.+) scowls at you, ready to attack|" +
            "(.+) glares at you threateningly|" +
            "You have become better at|" +
            "You gain experience!|" +
            "--You have looted|" +
            "--You have decided to not loot|" +
            "You cannot see your target|" +
            "You no longer have a target|" +
            "Your target is too far away, get closer!|" +
            "You can't use that command right now|" +
            "Your faction standing with |" +
            "<SYSTEMWIDE_MESSAGE>:|" +
            "You haven't recovered yet|" +
            "You receive (.+) from (.+) for|" +
            "You give (.+) to (.+)\\.|" +
            "It will take (.+) to prepare your camp|" +
            "You are inspecting|" +
            "Insufficient Mana to cast this spell!|" +
            "You cannot loot this item|" +
            "(.+) won the (.+) roll on|" +
            "You receive (.+) from the corpse." +
            ")", RegexOptions.Compiled);
    }
}
