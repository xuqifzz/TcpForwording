using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace TcpForwarding
{
    class Program
    {
        static void Main(string[] args)
        {
            SetCurExeWorkDir();
            try
            {
                GetConfig().ForEach(line =>
                {
                    Console.WriteLine("监听端口:{0}", line.Item1.Port);
                    line.Item2.ForEach(line2 =>
                    {
                        Console.WriteLine("转发至:{0}", line2);
                    });
                    Proxy p = new Proxy(line.Item1, line.Item2);
                    p.Start();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            while (true)
            {
                Console.ReadKey();

            }

        }
        /*
        static void Main(string[] args)
        {


            GetConfig2().ForEach(line =>
            {
                Console.WriteLine("监听端口:{0}", line.Item1.Port);
                line.Item2.ForEach(line2 =>
                {
                    Console.WriteLine("转发至:{0}", line2);
                });
            });
        }
        */

        static List<Tuple<IPEndPoint, List<IPEndPoint>>> GetConfig()
        {
            var result = new List<Tuple<IPEndPoint, List<IPEndPoint>>>();
            var lines = File.ReadAllLines("forward.txt");       //从配置文件中读取所有行
            foreach (var line in lines)
            {
                string[] fields = line.Split('|');  //按|进行切割
                if (fields.Length < 2) continue;    //过滤空行

                IPEndPoint main = new IPEndPoint(IPAddress.Any, int.Parse(fields[0]));  //提取监听端口
                List<IPEndPoint> forwordingList = new List<IPEndPoint>();
                for (int i = 1; i < fields.Length; i++)     //提取转发列表
                {
                    int index = fields[i].IndexOf(":");
                    if (index != -1)
                    {
                        forwordingList.Add(new IPEndPoint(IPAddress.Parse(fields[i].Substring(0, index)), int.Parse(fields[i].Substring(index+1))));
                    }
                }
                if (forwordingList.Count > 0)   //过滤掉空列表
                {
                    result.Add(new Tuple<IPEndPoint, List<IPEndPoint>>(main, forwordingList));
                }
            }
            return result;

        }

        static List<Tuple<IPEndPoint, List<IPEndPoint>>> GetConfig1()
        {
            var result = new List<Tuple<IPEndPoint, List<IPEndPoint>>>();
            Regex r = new Regex(@"^(\d+)(\|.+:\d+)+");
            Regex r2 = new Regex(@"\G\|([\d\.]+):(\d+)");
            var lines = File.ReadAllLines("forward.txt");

            foreach (var line in lines)
            {
                if (r.IsMatch(line))
                {
                    var match = r.Match(line);
                    IPEndPoint main = new IPEndPoint(IPAddress.Any, int.Parse(match.Groups[1].Value));
                    List<IPEndPoint> forwordingList = new List<IPEndPoint>();
                    foreach (Match match2 in r2.Matches(match.Groups[2].Value))
                    {
                        forwordingList.Add(new IPEndPoint(IPAddress.Parse(match2.Groups[1].Value), int.Parse(match2.Groups[2].Value)));
                    }
                    result.Add(new Tuple<IPEndPoint, List<IPEndPoint>>(main, forwordingList));
                }
            }
            return result;

        }
        static List<Tuple<IPEndPoint, List<IPEndPoint>>> GetConfig2()
        {
            Regex r = new Regex(@"^(\d+)(\|.+:\d+)+");
            Regex r2 = new Regex(@"\G\|([\d\.]+):(\d+)");

            return File.ReadAllLines("forward.txt").ToList()
                .Where( i => r.IsMatch(i))
                .Select( i=> r.Match(i))
                .Select( i=> new  Tuple<IPEndPoint, List<IPEndPoint>>(
                    new IPEndPoint(IPAddress.Any, int.Parse(i.Groups[1].Value)),
                    (from Match j in r2.Matches(i.Groups[2].Value) select new IPEndPoint(IPAddress.Parse(j.Groups[1].Value), int.Parse(j.Groups[2].Value))).ToList()
                    )           
                ).ToList();
        }



        static void SetCurExeWorkDir()
        {
            string ddd = System.Reflection.Assembly.GetEntryAssembly().Location;
            int iidx = ddd.LastIndexOf('\\');
            if (iidx != -1 && iidx != ddd.Length - 1)
            {
                string curdir = ddd.Substring(0, iidx + 1);
                Environment.CurrentDirectory = curdir;
            }
        }
    }
}
