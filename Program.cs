using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace GtToGpx
{
    class Program
    {
        static void Main (string[] args)
        {
            Console.WriteLine ("File to parse: " + args[0]);
            var items = ReadJsonFile (args[0]);
            var gpxDataList = MapToGpxData (items);

            foreach (GpxInputData gpxData in gpxDataList)
            {
                WriteToGpx (gpxData);
            }
        }

        private static List<GpxInputData> MapToGpxData (List<Item> items)
        {
            List<GpxInputData> gpxData = new List<GpxInputData> ();

            foreach (Item item in items)
            {
                foreach (MotionPathData motionPathData in item.motionPathData)
                {
                    gpxData.Add (new GpxInputData () { filename = "", gpxMetaData = new GpxMetadata ("author") });
                }
            }
            return gpxData;
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

        static int WriteToGpx (GpxInputData gpxData)
        {
            using (var ms = new MemoryStream ())
            {
                var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = false };
                using (var wr = XmlWriter.Create (ms, writerSettings))
                {
                    GpxWriter.Write (wr, null, gpxData.gpxMetaData, null, null);
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