using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eleon.Modding;

namespace Recycle
{
    class CommonFunctions
    {

        internal static void LogFile(string FileName, string FileData)
        {
            if (!File.Exists(MyEmpyrionMod.ModPath + FileName))
            {
                try
                {
                    using (FileStream fs = File.Create(MyEmpyrionMod.ModPath + FileName)) { }
                }
                catch
                {
                    if (MyEmpyrionMod.debug) LogFile("debug.txt", "File Creation Error: " + FileName);
                }
            }
            File.AppendAllText(MyEmpyrionMod.ModPath + FileName, FileData + Environment.NewLine);
        }

        internal static void Debug(string Data)
        {
            if (MyEmpyrionMod.debug)
            {
                LogFile("Debug.txt", Data);
            }
        }

        internal static void ERROR(string Data)
        {
            LogFile("ERROR.txt", Data);
        }

        internal static void Log(string Data)
        {
            //LogFile("Logs\\Log " + MyEmpyrionMod.BootupTimestamp + ".txt", Data);
            if (MyEmpyrionMod.LogName != "unknown")
            {
                LogFile("Logs\\" + MyEmpyrionMod.LogName + ".txt", Data);
            }
        }

        internal static int SeqNrGenerator(int LastSeqNr)
        {
            bool Fail = false;
            int CurrentSeqNr = 2000;
            do
            {
                if (LastSeqNr > 65530)
                {
                    LastSeqNr = 2000;
                }
                CurrentSeqNr = LastSeqNr + 1;
                if (MyEmpyrionMod.SeqNrStorage.ContainsKey(CurrentSeqNr)) { Fail = true; }
            } while (Fail == true);
            return CurrentSeqNr;
        }

        internal static string ArrayConcatenate(int start, string[] array)
        {
            string message = "";
            for (int i = start; i < array.Length; i++)
            {
                message = message + "\r\n";
                message = message + array[i];
            }
            return message;
        }

        internal static string ArrayToString(int start, string[] array)
        {
            string message = "";
            for (int i = start; i < array.Length; i++)
            {
                message = message + " " + array[i];
            }
            return message;
        }

        public static void FileReader(ushort ThisSeqNr, string File)
        {
            //Checks for simple errors
            string[] Script1 = System.IO.File.ReadAllLines(File);
            for (int i = 0; i < Script1.Count(); ++i)
            {

            }
        }

        public static string ChatmessageHandler(string[] Chatmessage, string Selector)
        {
            List<string> Restring = new List<string>(Chatmessage);
            string Picked = "";
            if (Selector.Contains('*'))
            {
                if (Selector == "1*")
                {
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
                else if (Selector == "2*")
                {
                    Restring.Remove(Restring[1]);
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
                else if (Selector == "3*")
                {
                    Restring.Remove(Restring[2]);
                    Restring.Remove(Restring[1]);
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
                else if (Selector == "4*")
                {
                    Restring.Remove(Restring[3]);
                    Restring.Remove(Restring[2]);
                    Restring.Remove(Restring[1]);
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
                else if (Selector == "5*")
                {
                    Restring.Remove(Restring[4]);
                    Restring.Remove(Restring[3]);
                    Restring.Remove(Restring[2]);
                    Restring.Remove(Restring[1]);
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
            }
            else
            {

            }
            return Picked;
        }

        public static Dictionary<string, string[]> CSVReader1(string File)
        {
            Dictionary<string, string[]> ItemDB = new Dictionary<string, string[]> { };
            string[] Line = System.IO.File.ReadAllLines(File);
            foreach (string Item in Line)
            {
                string[] itemArray = Item.Split(',');
                ItemDB.Add(itemArray[0], itemArray);
            }
            return ItemDB;
        }

        public static ItemStack[] ReadItemStacks(string File)
        {
            string[] bagLines = System.IO.File.ReadAllLines(File);
            int itemStackSize = bagLines.Count();
            ItemStack[] itStack = new ItemStack[itemStackSize];
            for (int i = 0; i < itemStackSize; ++i)
            {
                string[] bagLinesSplit = bagLines[i].Split(',');
                itStack[i] = new ItemStack(Convert.ToInt32(bagLinesSplit[1]), Convert.ToInt32(bagLinesSplit[2]));
                itStack[i].slotIdx = Convert.ToByte(bagLinesSplit[0]);
                itStack[i].ammo = Convert.ToInt32(bagLinesSplit[3]);
                itStack[i].decay = Convert.ToInt32(bagLinesSplit[4]);
            }
            return itStack;
        }

        public static void WriteItemStacks(string File, ItemStack[] ItemStacks, bool SuperStack)
        {
            if (SuperStack)
            {
                Dictionary<int, ItemStack> Superstacker = new Dictionary<int, ItemStack> { };
                foreach (ItemStack item in ItemStacks)
                {
                    int itemid = item.id;
                    if (Superstacker.Keys.Contains(item.id))
                    {
                        ItemStack FirstStack = Superstacker[item.id];
                        int FirstCount = item.count;
                        FirstCount = FirstCount + item.count;
                        ItemStack EndStack = new ItemStack
                        {
                            slotIdx = FirstStack.slotIdx,
                            id = FirstStack.id,
                            count = FirstCount,
                            decay = 0,
                            ammo = 0
                        };
                        Superstacker[item.id] = EndStack;
                    }
                    else
                    {
                        Superstacker.Add(item.id, item);
                    }
                    /*
                    string ItemName = "fish";
                    LogFile(File, item.slotIdx + "," + item.id + "," + item.count + "," + item.decay + "," + item.ammo + "," + ItemName);
                    */
                }
                foreach (int key in Superstacker.Keys)
                {

                }
            }
            else
            {
                foreach (ItemStack item in ItemStacks)
                {
                    LogFile(File, item.slotIdx + "," + item.id + "," + item.count + "," + item.decay + "," + item.ammo);
                }
            }
        }

        public static string SplitChat2(string ChatMessage)
        {
            string[] splitted = ChatMessage.Split(new[] { ' ' }, 2);
            string message = splitted[1];
            return message;
        }

        public static string UnixTimeStamp()
        {
            string time = Convert.ToString((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
            return time;
        }

        public static string TimeStamp()
        {
            //string Timestamp = CommonFunctions.UnixTimeStamp();
            DateTime Timestamp2 = CommonFunctions.UnixTimeStampToDateTime(Convert.ToDouble(CommonFunctions.UnixTimeStamp()));
            return Timestamp2.ToString("yyyy/MM/dd HH:mm:ss ffff");
        }

        public static string TimeStampFilename()
        {
            //string Timestamp = CommonFunctions.UnixTimeStamp();
            DateTime Timestamp2 = CommonFunctions.UnixTimeStampToDateTime(Convert.ToDouble(CommonFunctions.UnixTimeStamp()));
            return Timestamp2.ToString("yyyy.MM.dd HH.mm");
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static byte[] ConvertToByteArray(string input)
        {
            Char[] CharArray = input.ToCharArray();
            List<byte> ListByte = new List<byte> { };
            foreach (char Character in CharArray)
            {
                ListByte.Add(Convert.ToByte(Character));
            }
            byte[] data = ListByte.ToArray();
            return data;
        }

        public static string ConvertByteArrayToString(byte[] input)
        {
            string output = "";
            foreach (char character in input)
            {
                output = output + character.ToString();
            }
            return output;
        }

        public static string SanitizeString(string Input)
        {
            string Output = "";
            List<char> ValidCharacters = new List<char> { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            foreach (char Character in Input)
            {
                if (!ValidCharacters.Contains(Character))
                {
                    Output = Output + '_';
                }
                else
                {
                    Output = Output + Character;
                }
            }
            return Output;
        }

    }
}

