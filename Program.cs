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
            var delPathList = new List<string>(); // obsolete paths found to delete
            var modPathList = new List<string>(); // shorter paths found to modify
            var dupPathList = new List<string>(); // duplicate paths found to skip
            var newPathList = new List<string>(); // the new path list (full)
            var newPath = ""; // the new path string (short)

            var curPath = Environment.GetEnvironmentVariable("PATH", target);
            var curPathArr = curPath.Split(";", StringSplitOptions.RemoveEmptyEntries);
            log("");
            log($"Current # of {target} paths: {curPathArr.Length}, length: {curPath.Length}");
            foreach (var p in curPathArr)
            {
                var fullPath = Path.GetFullPath(p);
                if (!Directory.Exists(fullPath))
                {
                    delPathList.Add(fullPath);
                    continue;
                }
                if (newPathList.Any(x => x.Equals(fullPath)))
                {
                    dupPathList.Add(fullPath);
                    continue;
                }
                var shortPath = GetShortPathName(fullPath);
                if (!string.Equals(p, shortPath))
                {
                    modPathList.Add($"{p} -> {shortPath}");
                }
                newPath += shortPath + ";";
                newPathList.Add(fullPath);
            }
            var newPathArr = newPath.Split(";", StringSplitOptions.RemoveEmptyEntries );
            log($"New # of {target} paths: {newPathArr.Length}, length: {newPath.Length}");
            if (dupPathList.Count > 0)
            {
                log("");
                log("Following paths are redundant and will be DE-DUPLICATED");
                log(string.Join(Environment.NewLine, dupPathList));
            }
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
                log("New path is equal to current path; will NOT update.");
            }
            else if (newPathArr.Length > curPathArr.Length)
            {
                log("New path is longer (!) than current path; will NOT update.");
            }
            else if (newPathArr.Length < 10)
            {
                log("New path is too short; will NOT update.");
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
                    catch (Exception)
                    {
                        log($"{target} path update failed");
                        throw;
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
            log("CleanPath © 2021");
            CleanPathFor(EnvironmentVariableTarget.User);
            CleanPathFor(EnvironmentVariableTarget.Machine);
        }
    }
}
