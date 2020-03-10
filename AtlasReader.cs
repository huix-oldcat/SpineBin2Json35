using System;
using System.IO;
using System.Collections.Generic;

namespace SpineBin2Json35
{
    public class AtlasReader
    {
        TextReader reader;

        public Dictionary<string, int[]> size = new Dictionary<string, int[]>();

        string tupleName;
        string[] tupleValues = new string[4];

        int ReadTuple()
        {
            string line = reader.ReadLine();
            int colon = line.IndexOf(':');
            if (colon == -1) throw new System.Exception("Line dismatch: " + line);
            tupleName = line.Substring(colon);
            int count = 0, lastComma = colon+1;
            for (count = 0; count < 3; ++count)
            {
                int comma = line.IndexOf(',', lastComma);
                if (comma == -1) break;
                tupleValues[count] = line.Substring(lastComma, comma - lastComma).Trim();
                lastComma = comma + 1;
            }
            tupleValues[count] = line.Substring(lastComma).Trim();
            return count + 1;
        }

        public AtlasReader(TextReader reader)
        {
            this.reader = reader;
            Convert();
        }

        void Convert()
        {
            string line, pageFile = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim().Length == 0)
                {
                    pageFile = null;
                    continue;
                }
                if (pageFile == null)
                {
                    pageFile = line;
                    if (ReadTuple() == 2) ReadTuple();//size, format
                    ReadTuple();//filter
                    ReadTuple();//repeat
                }
                else
                {
                    ReadTuple();//rotate
                    ReadTuple();//xy
                    ReadTuple();//size
                    // Console.WriteLine(int.Parse(tupleValues[0]));
                    // Console.WriteLine(int.Parse(tupleValues[1]));
                    size[line] = new int[2] { int.Parse(tupleValues[0]), int.Parse(tupleValues[1]) };
                    if (ReadTuple() == 4)//origin size
                    {
                        //split
                        if (ReadTuple() == 4)
                        {
                            //pads
                            ReadTuple();
                        }
                    }
                    ReadTuple();//offset
                    ReadTuple();//index
                }
            }
        }
    }
}