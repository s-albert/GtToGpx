using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using NetTopologySuite.IO;

namespace GtToGpx
{
    class Program
    {
        static void Main (string[] args)
        {
            Console.WriteLine ("Hello World!");

            var json = ReadJsonFile (args[0]);

            WriteToGpx (json);
        }

        static string ReadJsonFile (string file)
        {

            using (StreamReader r = new StreamReader (file))
            {
                string json = r.ReadToEnd ();
                List<Item> items = JsonConvert.DeserializeObject<List<Item>> (json);
            }
            return "";
        }

        public class Item
        {
            public int millis;
            public string stamp;
            public DateTime datetime;
            public string light;
            public float temp;
            public float vcc;
        }

        static int WriteToGpx (string json)
        {

            using (var ms = new MemoryStream ())
            {
                var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = false };
                using (var wr = XmlWriter.Create (ms, writerSettings))
                {
                    GpxWriter.Write (wr, null, null, null, null);
                }

                ms.Position = 0;
               // byte[] expected = File.ReadAllBytes (path);

                // note that this is not a guarantee in the general case.  the inputs here have all been
                // slightly tweaked such that it should succeed for our purposes.
              //  Assert.False (diff.HasDifferences (), string.Join (Environment.NewLine, diff.Differences));
            }
                return 0;
        }
    }
}