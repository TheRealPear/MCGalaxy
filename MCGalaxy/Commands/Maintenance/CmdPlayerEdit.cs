/*
    Copyright 2015 MCGalaxy team
    
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
using MCGalaxy.SQL;

namespace MCGalaxy.Commands {    
    public sealed class CmdPlayerEdit : Command {        
        public override string name { get { return "playeredit"; } }
        public override string shortcut { get { return "pe"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override CommandAlias[] Aliases {
            get { return new [] { new CommandAlias("setinfo") }; }
        }
        public CmdPlayerEdit() { }

        public override void Use(Player p, string message) {
            if (message == "") { Help(p); return; }
            string[] args = message.SplitSpaces(3);
            args[0] = PlayerInfo.FindMatchesPreferOnline(p, args[0]);
            
            if (args[0] == null) return;
            Player who = PlayerInfo.FindExact(args[0]);
            if (args.Length == 1) {
                Player.Message(p, Colors.red + "You must specify a type to modify.");
                MessageValidTypes(p); return;
            }
            
            switch (args[1].ToLower()) {
                case "firstlogin":
                    SetDate(p, args, "FirstLogin", who, v => who.firstLogin = v); break;
                case "lastlogin":
                    SetDate(p, args, "LastLogin", who, v => who.timeLogged = v); break;
                case "totallogin":
                case "totallogins":
                    SetInteger(p, args, "totalLogin", 1000000000, who, v => who.totalLogins = v); break;
                case "title":
                    if (args.Length < 3) {
                        Player.Message(p, "Title can be up to 20 characters. Use \"null\" to remove the title"); return;
                    }
                    if (args[2].Length >= 20) { Player.Message(p, "Title must be under 20 characters"); return; }
                    if (args[2] == "null") args[2] = "";
                    
                    if (who != null) {
                        who.title = args[2];
                        who.SetPrefix();
                    }
                    UpdateDB(args[0], args[2], "Title");
                    MessageDataChanged(p, args[0], args[1], args[2]); break;
                case "totaldeaths":
                    SetInteger(p, args, "TotalDeaths", 1000000, who, v => who.overallDeath = v); break;
                case "money":
                    SetInteger(p, args, "Money", 100000000, who, v => who.money = v); break;
                case "totalblocks":
                    SetInteger(p, args, "totalBlocks", int.MaxValue, who, v => who.overallBlocks = v); break;
                case "totalcuboided":
                case "totalcuboid":
                    SetInteger(p, args, "totalCuboided", int.MaxValue, who, v => who.TotalDrawn = v); break;
                case "totalkicked":
                    SetInteger(p, args, "totalKicked", 1000000000, who, v => who.totalKicked = v); break;
                case "timespent":
                    SetTimespan(p, args, "TimeSpent", who, v => who.time = v.ParseDBTime()); break;
                case "color":
                    SetColor(p, args, "color", who, v => who.color = (v == "" ? who.group.color : v)); break;
                case "titlecolor":
                    SetColor(p, args, "title_color", who, v => who.titlecolor = (v == "" ? "" : v)); break;
                default:
                    Player.Message(p, Colors.red + "Invalid type.");
                    MessageValidTypes(p); break;
            }
        }
        
        static void SetColor(Player p, string[] args, string column, Player who, Action<string> setter) {
            if (args.Length < 3) {
                Player.Message(p, "Color format: color name, or \"null\" to reset to default color."); return;
            }
            
            string col = args[2] == "null" ? "" : Colors.Parse(args[2]);
            if (col == "" && args[2] != "null") {
                Player.Message(p, "There is no color \"" + args[2] + "\"."); return;
            }
            
            if (who != null) {
                setter(col);
                who.SetPrefix();
                args[0] = who.name;
            }
            UpdateDB(args[0], col, column);
            MessageDataChanged(p, args[0], args[1], args[2]);
        }
        
        const string dateFormat = "yyyy-MM-dd HH:mm:ss";
        static void SetDate(Player p, string[] args, string column, Player who, Action<DateTime> setter) {
            if (args.Length < 3) {
                Player.Message(p, "Dates must be in the format: yyyy-mm-dd hh:mm:ss");
                return;
            }
            
            DateTime date;
            if (!DateTime.TryParseExact(args[2], dateFormat, null, 0, out date)) {
                Player.Message(p, "Invalid date. (must be in format: yyyy-mm-dd hh:mm:ss");
                return;
            }
            
            if (who != null)
                setter(date);
            UpdateDB(args[0], args[2], column);
            MessageDataChanged(p, args[0], args[1], args[2]);
        }
        
        static void SetTimespan(Player p, string[] args, string column, Player who, Action<string> setter) {
            if (args.Length < 3) {
                Player.Message(p, "Timespan must be in the format: <number><quantifier>..");
                Player.Message(p, CommandParser.TimespanHelp, "set time spent to");
                return;
            }
            
            TimeSpan timeFrame = TimeSpan.Zero;
            if (!CommandParser.GetTimespan(p, args[2], ref timeFrame, "set time spent to", 'm')) return;
            
            string time = timeFrame.ToDBTime();
            if (who != null) {
                setter(time);
            } else {
                UpdateDB(args[0], time, column);
            }
            MessageDataChanged(p, args[0], args[1], timeFrame.Shorten(true));
        }
        
        static void SetInteger(Player p, string[] args, string column, int max, Player who, Action<int> setter) {
            if (args.Length < 3) {
                max--;
                int digits = 1; max /= 10;
                while (max > 0) {
                    digits++; max /= 10;
                }
                Player.Message(p, "You must specify a number, which can be " + digits + " digits at most."); return;
            }
            
            int value = 0;
            if (!CommandParser.GetInt(p, args[2], "Amount", ref value, 0, max)) return;
            
            if (who != null)
                setter(value);
            else
                UpdateDB(args[0], args[2], column);
            MessageDataChanged(p, args[0], args[1], args[2]);
        }
        
        static void UpdateDB(string name, string value, string column) {
            Database.Backend.UpdateRows("Players", column + " = @1", "WHERE Name = @0", name, value.UnicodeToCp437());
        }
        
        static void MessageDataChanged(Player p, string name, string type, string value) {
            name = PlayerInfo.GetColoredName(p, name);
            string msg = value == "" ? String.Format("The {1} data for &b{0} %Shas been reset.", name, type)
                : String.Format("The {1} data for &b{0} %Shas been updated to &a{2}%S.", name, type, value);
            Player.Message(p, msg);
        }

        static void MessageValidTypes(Player p) {
            Player.Message(p, "%HValid types: %SFirstLogin, LastLogin, TotalLogins, Title, TotalDeaths, Money, " +
                               "TotalBlocks, TotalCuboid, TotalKicked, TimeSpent, Color, TitleColor ");
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/pe [username] [type] <value>");
            Player.Message(p, "%HEdits an online or offline player's information. Use with caution!");
            MessageValidTypes(p);
            Player.Message(p, "%HTo see value format for a specific type, leave <value> blank.");
        }
    }
}
