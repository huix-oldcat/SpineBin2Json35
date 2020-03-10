/**************************************************************************
    This file is part of SpineBin2Json.

    SpineBin2Json is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    SpineBin2Json is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with SpineBin2Json.  If not, see <https://www.gnu.org/licenses/>.
**************************************************************************/
using System;
using System.IO;

namespace SpineBin2Json35
{
    class Program
    {
        static void Main(string[] args)
        {
            string atlasPath = null, skelPath = null, outputPath = null;
            bool ok = false;

            if (args.Length >= 2)
                for (int i = 0; i < 2; ++i)
                {
                    if (args[i].EndsWith(".skel"))
                        skelPath = args[i];
                    else if (args[i].EndsWith(".atlas"))
                        atlasPath = args[i];
                }

            if (atlasPath != null && skelPath != null)
            {
                if (File.Exists(atlasPath) && File.Exists(skelPath) && atlasPath.Substring(0, atlasPath.Length - 6) == skelPath.Substring(0, skelPath.Length - 5))
                {
                    ok = true;
                    outputPath = atlasPath.Substring(0, atlasPath.Length - 6);
                }
            }

            PrintHeader();
            if (ok == false)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Drag and drop two files(*.skel & *.atlas) onto this program. (Two * should be the same)\n将两个文件(*.skel和*.atlas)拖拽到本程序上(两个*需要一样)");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("I will ");
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.Write("OVERRIDE");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(" *.json(result) and *.txt(features not supported by Dragonbones)");
                Console.Write("两个文件将会被");
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.Write("覆盖");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine(":*.json(输出文件)和*.txt(龙骨不支持的特性)");
                End();
            }

            using (var fs = File.OpenRead(atlasPath))
            using (var tr = new StreamReader(fs))
            using (var input = File.OpenRead(skelPath))
            using (var warningOut = File.OpenWrite(outputPath + ".txt"))
            using (var warning = new StreamWriter(warningOut))
            {
                var atlasReader = new AtlasReader(tr);
                var reader = new BinaryReader(input, atlasReader, warning);
                var obj = reader.Convert();

                using (var output = File.OpenWrite(outputPath+".json"))
                using (var sw = new StreamWriter(output))
                    sw.Write(Newtonsoft.Json.JsonConvert.SerializeObject(obj));

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(string.Format("File length: {0}\nRead length: {1}", input.Length, input.Position));
                Console.ForegroundColor = ConsoleColor.White;
                if (input.Length == input.Position)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("It seems to have succeeded.\n看起来转换成功了");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("We did not read the entire file in its entirety.\n好像文件没有读取完整...");
                    Console.WriteLine("Please report the issue to https://github.com/huix-oldcat/SpineBin2Json35 \n请去上面的网址或QQ群1021330668反馈");
                    Console.WriteLine("You may provide this binary file. It helps a lot.\n如果您愿意把转换前文件提供给我们那更好!");
                }
            }

            End();
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("SpineBin2Json version 1-351");
            Console.Write("Spine binary version: ");
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("3.5.x");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("By huix-oldcat@github");
            Console.WriteLine("Project: https://github.com/huix-oldcat/SpineBin2Json35");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("SpineBin2Json is free software: you can redistribute it and/or modify\nit under the terms of the GNU General Public License as published by\nthe Free Software Foundation, either version 3 of the License, or\n(at your option) any later version.\n\nSpineBin2Json is distributed in the hope that it will be useful,\nbut WITHOUT ANY WARRANTY; without even the implied warranty of\nMERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the\nGNU General Public License for more details.\n\nYou should have received a copy of the GNU General Public License\nalong with SpineBin2Json.  If not, see <https://www.gnu.org/licenses/>.");
            Console.WriteLine("---------------------------------------");
        }

        private static void End()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("按回车退出");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }
}
