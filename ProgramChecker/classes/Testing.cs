﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProgramChecker.Languages;

namespace ProgramChecker.classes
{
    public class Testing
    {
        private Test test;
        private int checkId;
        private bool isForceKill;
        private bool isMemoryLimit;
        private int peakMemory;
        private int timeOut;
        private long spentTime;
        private int spentMemory;
        private int isParseDec;
   

        public Testing(Test test, int checkId, int peakMemory, int timeOut, int isParseDec)
        {
            this.test = test;
            this.checkId = checkId;
            this.peakMemory = peakMemory;
            this.timeOut = timeOut * 1000;
            this.isParseDec = isParseDec;
        }

        public Result testing()
        {
            string outtext = "";

            string pathSrc = Program.globalConfig["paths"]["src"] + $@"check_{checkId}\";
            string testSrc = pathSrc + $@"test_{test.id}\";


            if (!Directory.Exists(testSrc))
            {
                Directory.CreateDirectory(testSrc);
            }


            Language language = Compiller.language;
            string file = language.prepareTesting(test);

            using (FileStream fs = File.Create(testSrc + "/input.txt"))
            {
                Byte[] input = new UTF8Encoding(true).GetBytes(test.input);
                fs.Write(input, 0, input.Length);
            }

            runTestFile(file, testSrc);
            int st = 0;

            if (!isMemoryLimit && !isForceKill)
            {
                using (StreamReader sr = new StreamReader(testSrc + "/output.txt"))
                {
                    outtext = clearString(sr.ReadToEnd(), isParseDec);

                }

                String testValue = clearString(test.output, isParseDec);
                if (outtext.Equals(testValue))
                {
                    st = Check.PASS_SUCCESS;
                }
                else
                {
                    st = Check.PASS_FAIL;
                }
            }
            else if (isMemoryLimit)
            {
                st = Check.PASS_MEMORY_LIMIT;
                outtext = "Memory Limit";
            }
            else
            {
                st = Check.PASS_TIMEOUT;
                outtext = "Timeout";
            }

            return new Result()
            {
                test_id = test.id,
                status = st,
                outtext = outtext,
                memory = spentMemory,
                time = spentTime
            };
        }
        private String clearString(String s, int parseDec)
        {
            String[] splited = s.Replace("\r", "").Trim().Split('\n');

            for (int i = 0; i < splited.Length; i++)
            {
                splited[i] = splited[i].Trim();
                if (isParseDec == 1)
                {
                    splited[i] = splited[i].Replace(',', '.');
                }
            }
            String outStr = "";
            foreach (String p in splited)
            {
                outStr += p + "\n";
            }
            return outStr.Trim();
        }

        private void runTestFile(string testExe, string testSrc)
        {
            string pathScript = Program.globalConfig["paths"]["scripts"];
            isForceKill = false;
            isMemoryLimit = false;
            Thread.Sleep(300);// timeout for hold  and clear processes
            Task runTesTask = new Task(() =>
            {
                Language language = Compiller.language;
                var compile = language.createTestProcess(testExe, testSrc);
                spentTime = 0;
                Stopwatch w = new Stopwatch();
                compile.Start();
                w.Start();
                do
                {
                    if(compile.HasExited) break;
                    if (compile.PeakPagedMemorySize64 / 1024.0 > peakMemory) isMemoryLimit = true;
                    if (!compile.WaitForExit(timeOut))
                    {
                        isForceKill = true;
                        compile.Kill();
                    }

                } while (!compile.WaitForExit(timeOut));
                w.Stop();
                spentTime = w.ElapsedMilliseconds;
                spentMemory = (int)(compile.PeakPagedMemorySize64 / 1024.0);
            });


            runTesTask.Start();

            Task.WaitAll(runTesTask);
        }


        public bool getTimeout()
        {
            return isForceKill;
        }

        public bool getMemeryLimit()
        {
            return isMemoryLimit;
        }
    }
}