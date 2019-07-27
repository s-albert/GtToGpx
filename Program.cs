using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GtToGpx
{
    class Program
    {
        static void Main (string[] args)
        {
            Console.WriteLine ("File to parse: " + args[0]);

            var json = ReadJsonFile (args[0]);

            Console.WriteLine ("Result: " + json);

            // WriteToGpx (json);
        }

        static List<Item> ReadJsonFile (string file)
        {

            using (StreamReader r = new StreamReader (file))
            {
                string json = r.ReadToEnd ();
                var items = JsonConvert.DeserializeObject<List<Item>> (json, new JsonSerializerSettings
                {
                    Error = Program.HandleDeserializationError
                });
                return items;
            }
        }

        public static void HandleDeserializationError (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            var currentError = errorArgs.ErrorContext.Error.Message;
            errorArgs.ErrorContext.Handled = true;
            Console.WriteLine ("Error: " + currentError);
        }

        public class Item
        {
            public List<MotionPathData> motionPathData;
            public int recordDay;
        }

        public class MotionPathData
        {
            public long startTime;
            public long endTime;
            public int totalDistance;
            public string attribute;
            public int totalSteps;
            public int totalTime;
            public int sportType;
            public int totalCalories;
            public string timeZone;
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