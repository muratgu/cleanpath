using System;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace cleanpath
{
    class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetShortPathName(
   [MarshalAs(UnmanagedType.LPTStr)]
   string lpszLongPath,
   [MarshalAs(UnmanagedType.LPTStr)]
   StringBuilder lpszShortPath,
   uint cchBuffer);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetShortPathName(string lpszLongPath, char[] lpszShortPath, int cchBuffer);

        static void log(string s)
        {
            Console.WriteLine(s);
        }

        static string GetShortPathName(string longFileName)
        {
            int sz = (int) GetShortPathName(longFileName, null, 0);
            if (sz == 0)
                throw new Win32Exception();
            var sb = new StringBuilder(sz + 1);
            sz = (int) GetShortPathName(longFileName, sb, (uint) sb.Capacity);
            if (sz == 0)
                throw new Win32Exception();
            return sb.ToString();
        }

        static void CleanPathFor(EnvironmentVariableTarget target)
        {
            var delPathList = new List<string>();
            var modPathList = new List<string>();
            var curPath = Environment.GetEnvironmentVariable("PATH", target);
            var curPathArr = curPath.Split(";");
            log("");
            log($"Current # of {target} paths: {curPathArr.Length}, length: {curPath.Length}");
            var newPath = "";
            var newPathList = new List<string>();
            foreach (var p in curPathArr)
            {
                if (p.Trim().Length == 0) continue;
                var fullPath = Path.GetFullPath(p);
                if (!Directory.Exists(fullPath))
                {
                    delPathList.Add(fullPath);
                    continue;
                }
                if (newPathList.Any(x => x.Equals(fullPath))) continue;
                newPathList.Add(fullPath);
                var shortPath = GetShortPathName(fullPath);
                if (!string.Equals(p, shortPath))
                {
                    modPathList.Add($"{p} -> {shortPath}");
                }
                newPath += shortPath + ";";
            }
            var newPathArr = newPath.Split(";");
            log($"New # of {target} paths: {newPathArr.Length}, length: {newPath.Length}");
            if (delPathList.Count > 0)
            {
                log("");
                log("Following paths are obsolete and will be REMOVED");
                log(string.Join(Environment.NewLine, delPathList));
            }
            if (modPathList.Count > 0)
            {
                log("");
                log("Following paths will be SHORTENED");
                log(string.Join(Environment.NewLine, modPathList));
            }
            if (newPathArr.Length == curPathArr.Length)
            {
                log("New path is equal to current path; will not update.");
            }
            else if (newPathArr.Length > curPathArr.Length)
            {
                log("New path is longer (!) than current path; will not update.");
            }
            else if (newPathArr.Length < 10)
            {
                log("New path is too short; will not update.");
            }
            else
            {
                log("");
                log($"Please type Y and press ENTER to update {target} path");
                var res = Console.Read();
                if (res == (int)ConsoleKey.Y)                
                {
                    log($"Updating {target} path...");
                    try
                    {
                        Environment.SetEnvironmentVariable("PATH", newPath, target);
                        log($"{target} path update successful");
                    }
                    catch (SecurityException)
                    {
                        log($"{target} path update failed. Try running it with administrator rights.");
                        Environment.Exit(-1);
                    }
                    catch (Exception ex)
                    {
                        log($"{target} path update failed");
                        throw ex;
                    }
                }
                else
                {
                    log($"Current {target} path preserved");
                }
            }
        }

        static void Main(string[] args)
        {
            log("CleanPath © muratgu 2021");
            CleanPathFor(EnvironmentVariableTarget.User);
            CleanPathFor(EnvironmentVariableTarget.Machine);
        }
    }
}
