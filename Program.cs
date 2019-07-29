using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NetTopologySuite.Features;
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
                    gpxData.Add (new GpxInputData () { filename = GetFileName (item.recordDay, motionPathData), gpxMetaData = GetGpxMetadata(motionPathData) });
                }
            }
            return gpxData;
        }

        private static GpxMetadata GetGpxMetadata(MotionPathData motionPathData) {
            var metadata =  new GpxMetadata ("author");
            var attributes = motionPathData.attribute.Split(";");
            int i = 1;
            while(i < attributes.Length) {
                var a = attributes[i];
                var aValue = a.Split("=");
                if (aValue[0] == "k") {
                }
            }
            return metadata;
        }

        private static string GetFileName (int recordDay, MotionPathData motionPathData)
        {
            return recordDay.ToString () + "-" + motionPathData.startTime.ToString ();
        }

        private static List<Item> ReadJsonFile (string file)
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

        private static void HandleDeserializationError (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            var currentError = errorArgs.ErrorContext.Error.Message;
            errorArgs.ErrorContext.Handled = true;
            Console.WriteLine ("Error: " + currentError);
        }

        private static int WriteToGpx (GpxInputData gpxData)
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