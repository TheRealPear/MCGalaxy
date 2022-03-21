/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using System.IO;
using MCGalaxy.Blocks;
using MCGalaxy.Commands;
using MCGalaxy.Config;
using MCGalaxy.Events.GroupEvents;

namespace MCGalaxy {
    /// <summary> This is the group object, where ranks and their data are stored </summary>
    public sealed partial class Group {

        public static Group BannedRank { get { return Find(LevelPermission.Banned); } }
        public static Group GuestRank  { get { return Find(LevelPermission.Guest); } }
        public static Group NobodyRank { get { return Find(LevelPermission.Nobody); } }
        public static Group DefaultRank;
        public static List<Group> GroupList = new List<Group>();
        static bool reloading;
        
        const int mapGenLimitAdmin = 225 * 1000 * 1000;
        const int mapGenLimit = 30 * 1000 * 1000;
        
        public string Name;
        [ConfigPerm("Permission", null, LevelPermission.Null)]
        public LevelPermission Permission = LevelPermission.Null;
        [ConfigColor("Color", null, "&f")]
        public string Color;
        public string ColoredName { get { return Color + Name; } }
        
        [ConfigInt("Limit", null, 0, 0)]
        public int DrawLimit;
        [ConfigTimespan("MaxUndo", null, 0, false)]
        public TimeSpan MaxUndo;
        [ConfigString("MOTD", null, "", true)]
        public string MOTD = "";
        [ConfigInt("GenVolume", null, mapGenLimit)]
        public int GenVolume = mapGenLimit;
        [ConfigInt("OSMaps", null, 3, 0)]
        public int OverseerMaps = 3;
        [ConfigBool("AfkKicked", null, true)]
        public bool AfkKicked = true;
        [ConfigTimespan("AfkKickMinutes", null, 45, true)]
        public TimeSpan AfkKickTime = TimeSpan.FromMinutes(45);
        [ConfigString("Prefix", null, "", true)]
        public string Prefix = "";
        [ConfigInt("CopySlots", null, 0, 0)]
        public int CopySlots = 1;
        [ConfigString("Filename", null, "", true, ".,_-+=")]
        internal string filename;
        
        public PlayerList Players;
        public List<Command> Commands;
        public bool[] Blocks = new bool[Block.SUPPORTED_COUNT];
        
        
        public void SetUsableCommands() {
            List<Command> commands = new List<Command>();
            foreach (CommandPerms perms in CommandPerms.List) 
            {
                if (!perms.UsableBy(Permission)) continue;
                
                Command cmd = Command.Find(perms.CmdName);
                if (cmd != null) commands.Add(cmd);
            }
            Commands = commands;
        }
        
        public void SetUsableBlocks() {
            foreach (BlockPerms perms in BlockPerms.List) 
            {
                Blocks[perms.ID] = perms.UsableBy(Permission);
            }
        }

        /// <summary> Creates a copy of this group, except for members list and usable commands and blocks. </summary>
        public Group CopyConfig() {
            Group copy = new Group();
            copy.Name = Name; copy.Color = Color; copy.Permission = Permission;
            copy.DrawLimit = DrawLimit; copy.MaxUndo = MaxUndo; copy.MOTD = MOTD;
            copy.GenVolume = GenVolume; copy.OverseerMaps = OverseerMaps;
            copy.AfkKicked = AfkKicked; copy.AfkKickTime = AfkKickTime;
            copy.Prefix = Prefix; copy.CopySlots = CopySlots; copy.filename = filename;
            return copy;
        }
        
        
        public static Group Find(string name) {
            MapName(ref name);
            foreach (Group grp in GroupList) {
                if (grp.Name.CaselessEq(name)) return grp;
            }
            return null;
        }
        
        internal static void MapName(ref string name) {
            if (name.CaselessEq("op")) name = "operator";
        }

        public static Group Find(LevelPermission perm) {
            return GroupList.Find(grp => grp.Permission == perm);
        }

        public static Group GroupIn(string playerName) {
            foreach (Group grp in GroupList) {
                if (grp.Players.Contains(playerName)) return grp;
            }
            return DefaultRank;
        }
        
        public static string GetColoredName(LevelPermission perm) {
            Group grp = Find(perm);
            if (grp != null) return grp.ColoredName;
            return "&f" + ((int)perm);
        }
        
        public static string GetColoredName(string rankName) {
            Group grp = Find(rankName);
            if (grp != null) return grp.ColoredName;
            return "&f" + rankName;
        }
        
        /// <summary> Returns the color of the group with the given permission level </summary>
        /// <remarks> Returns white if no such group exists </remarks>
        public static string GetColor(LevelPermission perm) {
            Group grp = Find(perm);
            if (grp != null) return grp.Color;
            return "&f";
        }
        
        public static LevelPermission ParsePermOrName(string value, LevelPermission defPerm) {
            if (value == null) return defPerm;
            
            sbyte perm;
            if (sbyte.TryParse(value, out perm))
                return (LevelPermission)perm;
            
            Group grp = Find(value);
            return grp != null ? grp.Permission : defPerm;
        }
        
        static string GetPlural(string name) {
            if (name.Length < 2) return name;
            
            string last2 = name.Substring(name.Length - 2).ToLower();
            if ((last2 != "ed" || name.Length <= 3) && last2[1] != 's')
                return name + "s";
            return name;
        }
        
        public string GetFormattedName() { return Color + GetPlural(Name); }
        
        
        static void Add(LevelPermission perm, int drawLimit, int undoMins, string name, char colCode, int realms) {
            Group grp   = new Group();
            int afkMins = perm <= LevelPermission.AdvBuilder ? 45 : 60;

            grp.Permission   = perm;
            grp.DrawLimit    = drawLimit;
            grp.MaxUndo      = TimeSpan.FromMinutes(undoMins);
            grp.Name         = name;
            grp.Color        = "&" + colCode;
            grp.GenVolume    = perm < LevelPermission.Admin ? mapGenLimit : mapGenLimitAdmin;
            grp.AfkKickTime  = TimeSpan.FromMinutes(afkMins);
            grp.OverseerMaps = realms;
            Register(grp);
        }

        public static void Register(Group grp) {
            GroupList.Add(grp);
            grp.LoadPlayers();
            
            if (reloading) {
                grp.SetUsableBlocks();
                grp.SetUsableCommands();
            }
            OnGroupLoadedEvent.Call(grp);
        }
        
        public static void LoadAll() {
            GroupList = new List<Group>();
            if (File.Exists(Paths.RankPropsFile)) {
                LoadFromDisc();
            } else {
                // Add some default ranks
                Add(LevelPermission.Builder,      4096,        5, "Builder",    '2',  3); // 16^3 draw volume
                Add(LevelPermission.AdvBuilder, 262144,       15, "AdvBuilder", '3',  5); // 64^3
                Add(LevelPermission.Operator,  2097152,       90, "Operator",   'c',  8); // 128^3
                Add(LevelPermission.Admin,    16777216, 21024000, "Admin",      'e', 12); // 256^3
            }

            if (BannedRank == null)
                Add(LevelPermission.Banned,         1,        0, "Banned", '8',  0);
            if (GuestRank == null)
                Add(LevelPermission.Guest,          1,        2, "Guest",  '7',  3);
            if (NobodyRank == null)
                Add(LevelPermission.Nobody, 134217728, 21024000, "Owner",  '0', 16); // 512^3
            
            GroupList.Sort((a, b) => a.Permission.CompareTo(b.Permission));
            DefaultRank = Find(Server.Config.DefaultRankName);
            if (DefaultRank == null) DefaultRank = GuestRank;

            OnGroupLoadEvent.Call();
            reloading = true;
            SaveAll(GroupList);
            
            Player.Console.group = NobodyRank;
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players) {
                UpdateGroup(p);
            }
        }
        
        static void UpdateGroup(Player p) {
            Group grp = Group.Find(p.group.Permission);
            if (grp == null) grp = DefaultRank;
            p.group = grp;
            
            p.UpdateColor(PlayerInfo.DefaultColor(p));
        }

        static readonly object saveLock = new object();
        public static void SaveAll(List<Group> givenList) {
            lock (saveLock) {
                SaveGroups(givenList);
            }
            OnGroupSaveEvent.Call();
        }
        
        
        void LoadPlayers() {
            string desired = (int)Permission + "_rank";
            // Try to use the auto filename format
            if (filename == null || !filename.StartsWith(desired))
                MoveToDesired(desired);
            
            Players = PlayerList.Load("ranks/" + filename);
        }
        
        void MoveToDesired(string desired) {
            // rank doesn't exist to begin with
            if (filename == null || !File.Exists("ranks/" + filename)) {
                filename = desired + ".txt";
                // TODO: should start backwards from z to a
            } else if (MoveToFile(desired + ".txt")) {
            } else {
                // try appending a and z if duplicate file
                for (char c = 'a'; c <= 'z'; c++) {
                    string newFile = desired + c + ".txt";
                    if (MoveToFile(newFile)) return;
                }
            }
        }
        
        bool MoveToFile(string newFile) {
            if (File.Exists("ranks/" + newFile)) return false;
            
            try {
                File.Move("ranks/" + filename, "ranks/" + newFile);
                filename = newFile;
                return true;
            } catch (Exception ex) {
                Logger.LogError(ex);
                return false;
            }
        }


        static ConfigElement[] cfg;
        static void LoadFromDisc() {
            Group temp = null;
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(Group));
            
            PropertiesFile.Read(Paths.RankPropsFile, ref temp, ParseProperty, '=', false);
            if (temp != null) AddGroup(temp);
        }
        
        static void ParseProperty(string key, string value, ref Group temp) {
            if (key.CaselessEq("RankName")) {
                if (temp != null) AddGroup(temp);
                temp  = null;
                value = value.Replace(" ", "");

                if (value.CaselessEq("op")) {
                    Logger.Log(LogType.Warning, "Cannot have a rank named \"{0}\", this rank is hard-coded.", value);
                } else if (Group.Find(value) == null) {
                    temp = new Group();
                    temp.Name = value;
                } else {
                    Logger.Log(LogType.Warning, "Cannot add the rank {0} twice", value);
                }
            } else {
                if (temp == null) return;
                // for prefix we need to keep space at end
                if (!key.CaselessEq("Prefix")) {
                    value = value.Trim();
                } else {
                    value = value.TrimStart();
                }
                
                ConfigElement.Parse(cfg, temp, key, value);
            }
        }
        
        static void AddGroup(Group temp) {
            if (Find(temp.Permission) != null) {
                Logger.Log(LogType.Warning, "Cannot have 2 ranks set at permission level " + (int)temp.Permission);
            } else if (temp.Permission == LevelPermission.Null) {
                Logger.Log(LogType.Warning, "Invalid permission level for rank " + temp.Name);
            } else {
                Register(temp);
            }
        }
        
        static void SaveGroups(List<Group> givenList) {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(Group));
            
            using (StreamWriter w = new StreamWriter(Paths.RankPropsFile)) {
                w.WriteLine("#Version 3");
                w.WriteLine("#RankName = string");
                w.WriteLine("#\tThe name of the rank (e.g. Guest)");
                w.WriteLine("#Permission = num");
                w.WriteLine("#\tThe \"permission\" of the rank. It's a number.");
                w.WriteLine("#\tThere are pre-defined permissions already set. (for the old ranks)");
                w.WriteLine("#\t\tBanned = -20, Guest = 0, Builder = 30, AdvBuilder = 50, Operator = 80");
                w.WriteLine("#\t\tSuperOP = 100, Nobody = 120");
                w.WriteLine("#\tMust be greater than -50 and less than 120");
                w.WriteLine("#\tThe higher the number, the more commands do (such as undo allowing more seconds)");
                w.WriteLine("#Limit = num");
                w.WriteLine("#\tThe draw command limit for the rank (can be changed in-game with /limit)");
                w.WriteLine("#\tMust be greater than 0");
                w.WriteLine("#MaxUndo = num");
                w.WriteLine("#\tThe undo limit for the rank, only applies when undoing others.");
                w.WriteLine("#\tMust be greater than 0");
                w.WriteLine("#Color = color");
                w.WriteLine("#\tThe default color shown in tab and chat for players of the rank (e.g. &f)");
                w.WriteLine("#MOTD = string");
                w.WriteLine("#\tAlternate MOTD players of the rank will see when joining the server.");
                w.WriteLine("#\tLeave blank to use the server MOTD.");
                w.WriteLine("#OSMaps = num");
                w.WriteLine("#\tThe number of maps the players will have in /os");
                w.WriteLine("#Prefix = string");
                w.WriteLine("#\tCharacters that always appear before names of players of the rank in chat.");
                w.WriteLine("#\tLeave blank to have no characters before the names of players.");
                w.WriteLine("#GenVolume = num");
                w.WriteLine("#\tThe maximum volume of a map that can be generated by players of the rank.");
                w.WriteLine("#AfkKickMinutes = num");
                w.WriteLine("#\tNumber of minutes a player of the rank can be AFK for before they are automatically AFK kicked.");
                w.WriteLine();
                w.WriteLine();
                
                foreach (Group group in givenList) 
                {
                    w.WriteLine("RankName = " + group.Name);
                    ConfigElement.SerialiseElements(cfg, w, group);
                    w.WriteLine();
                }
            }
        }
    }
}