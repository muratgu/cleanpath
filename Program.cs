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
using CommandLine;

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

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
        static extern uint GetLongPathName(string lpszShortPath, [Out] StringBuilder lpszLongPath, uint cchBuffer);

        static void log(string s)
        {
            Console.WriteLine(s);
        }

        static string RemoveLastSlash(string s)
        {
            if (String.IsNullOrEmpty(s)) 
                return s;
            if (s.EndsWith("/") || s.EndsWith("\\"))
                return s.Substring(0, s.Length - 1);
            return s;
        }

        static string GetShortPathName(string longFileName)
        {
            int sz = (int)GetShortPathName(longFileName, null, 0);
            if (sz == 0)
                throw new Win32Exception();
            var sb = new StringBuilder(sz + 1);
            sz = (int)GetShortPathName(longFileName, sb, (uint)sb.Capacity);
            if (sz == 0)
                throw new Win32Exception();
            return RemoveLastSlash(sb.ToString());
        }

        static string GetLongPathName(string shortPath)
        {
            StringBuilder builder = new StringBuilder(255);
            int result = (int) GetLongPathName(shortPath, builder, (uint) builder.Capacity);
            if (result > 0 && result < builder.Capacity) {
                return RemoveLastSlash(builder.ToString(0, result));
            }     
            if (result > 0) {
                builder = new StringBuilder(result);
                result = (int) GetLongPathName(shortPath, builder, (uint) builder.Capacity);
                return RemoveLastSlash(builder.ToString(0, result));
            }
            return RemoveLastSlash(shortPath); // not found
        }

        static void CleanPathFor(EnvironmentVariableTarget target, bool change = false, bool confirmed = false, bool list = false, bool listFullPath = false)
        {
            var newPathList = new List<string>(); // the new path list (full)
            var newPath = ""; // the new path string (short)

            var curPath = Environment.GetEnvironmentVariable("PATH", target);
            var curPathArr = curPath.Split(";", StringSplitOptions.RemoveEmptyEntries);
            log($"{curPathArr.Length} {target} paths ({curPath.Length} chars)");
            foreach (var p in curPathArr)
            {
                var fullPath = GetLongPathName(p);
                
                if (list) log(listFullPath ? fullPath : p);

                if (!Directory.Exists(fullPath))
                {
                    log($"OBSOLETE {fullPath}");
                    continue;
                }
                if (newPathList.Any(x => x.Equals(fullPath)))
                {
                    log($"DUPLICATE {fullPath}");
                    continue;
                }
                var shortPath = GetShortPathName(fullPath);
                if (!string.Equals(p, shortPath))
                {
                    log($"LONG {fullPath}");
                }
                newPath += shortPath + ";";
                newPathList.Add(fullPath);               
            }
            var newPathArr = newPath.Split(";", StringSplitOptions.RemoveEmptyEntries);

            var diffLength = curPath.Length - newPath.Length;

            if (newPathArr.Length == curPathArr.Length && diffLength == 0)
            {
                if (change) log("New path is equal to current path; will NOT update");
                else log("The path is clean");
            }
            else if (newPathArr.Length > curPathArr.Length || diffLength < 0)
            {
                if (change) log("New path is longer (!) than current path; will NOT update");
                else log("The path may become longer if modified");
            }
            else if (newPathArr.Length < 10)
            {
                if (change) log("New path is too short; will NOT update");
                else log("The path may become too short if modified");
            }
            else
            {
                log($"The {target} path can be shortened to {newPath.Length} chars ({diffLength} less)");
                if (change)
                {
                    if (!confirmed)
                    {
                        log($"Do you really want to update the {target} path? [Y]");
                        var res = Console.Read();
                        confirmed = (res == (int)ConsoleKey.Y); 
                    }                    
                    if (confirmed)
                    {
                        try
                        {
                            Environment.SetEnvironmentVariable("PATH", newPath, target);
                            log($"Successfully updated the {target} path");
                        }
                        catch (SecurityException)
                        {
                            log($"Failed to updated the {target} path. Try with administrator rights");
                            Environment.Exit(-1);
                        }
                        catch (Exception)
                        {
                            log($"Failed to update the {target} path");
                            throw;
                        }
                    }
                    else
                    {
                        log($"The {target} path is NOT updated");
                    }
                }                
            }
        }

        class Options
        {            
            [Option('m', "machine", Default = false, HelpText = "Target machine path")]
            public bool TargetMachinePath { get; set; }

            [Option('c', "change", Default = false, HelpText = "Change path")]
            public bool ChangePath { get; set; }

            [Option('l', "list", Default = false, HelpText = "List path")]
            public bool ListPath { get; set; }

            [Option('f', "full", Default = false, HelpText = "List full path")]
            public bool ListFullPath { get; set; }

            [Option('y', "yes", Default = false, HelpText = "Respond yes to confirmation")]
            public bool RespondYes { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       var target = o.TargetMachinePath ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;

                       if (o.ChangePath)
                       {
                           CleanPathFor(target, change: o.ChangePath, confirmed: o.RespondYes, list: o.ListPath, listFullPath: o.ListFullPath);
                       }
                       else
                       {
                           CleanPathFor(target, list: o.ListPath, listFullPath: o.ListFullPath);
                       }
                   });
        }
    }
}
    
