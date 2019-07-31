using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
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
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                // Change current culture
                CultureInfo culture;
                if (Thread.CurrentThread.CurrentCulture.Name != "en-US")
                {
                    culture = CultureInfo.CreateSpecificCulture ("en-US");

                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                }

                Console.WriteLine ("File to parse: " + args[0]);
                var items = ReadJsonFile (args[0]);
                if (items == null)
                {
                    throw new Exception ("Json output is null.");
                }

                var gpxDataList = MapToGpxData (items);

                foreach (GpxInputData gpxData in gpxDataList)
                {
                    WriteToGpx (gpxData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine ("Error: " + ex.Message);
            }
        }

        private static List<GpxInputData> MapToGpxData (List<Item> items)
        {
            List<GpxInputData> gpxData = new List<GpxInputData> ();

            foreach (Item item in items)
            {
                foreach (MotionPathData motionPathData in item.motionPathData)
                {
                    gpxData.Add (new GpxInputData ()
                    {
                        filename = GetFileName (item.recordDay, motionPathData),
                            gpxMetaData = new GpxMetadata ("author"),
                            gpxTrack = GetGpxTrack (motionPathData)
                    });
                }
            }
            return gpxData;
        }

        private static GpxTrack GetGpxTrack (MotionPathData motionPathData)
        {
            var gpxTrack = new GpxTrack ();

            var attributes = motionPathData.attribute.Split (";");
            int i = 0;

            List<GpxWaypoint> waypoints = new List<GpxWaypoint> ();
            GpxWaypoint currentWaypoint = null;
            while (i < attributes.Length)
            {
                i++;
                var a = attributes[i];
                var pair = a.Split ("=");
                if (pair.Length != 2)
                {
                    throw new Exception ("Ungültiges Key-Value Pair: " + a);
                }
                switch (pair[0])
                {
                    case "k":
                        {
                            currentWaypoint = new GpxWaypoint (GpxLongitude.MinValue, GpxLatitude.MinValue);
                            waypoints.Add (currentWaypoint);
                            break;
                        }
                    case "lat":
                        {
                            if (currentWaypoint != null)
                            {
                                double d = double.Parse (pair[1]);
                                currentWaypoint.WithLatitude (new GpxLatitude (d));
                            }
                            break;
                        }
                    case "lon":
                        {
                            if (currentWaypoint != null)
                            {
                                currentWaypoint.WithLongitude (new GpxLongitude (double.Parse (pair[1])));
                            }
                            break;
                        }
                    case "t":
                        {
                            if (currentWaypoint != null)
                            {
                                var d = double.Parse(pair[1]);
                                var l = Convert.ToInt64(d);
                                DateTime t = new DateTime(636905317316732000);
                                currentWaypoint.WithTimestampUtc (t);
                            }
                            break;
                        }
                }
            }
            System.Collections.Immutable.ImmutableArray<GpxTrackSegment> segments = new System.Collections.Immutable.ImmutableArray<GpxTrackSegment> ();
            segments.Add (new GpxTrackSegment ().WithWaypoints (waypoints));
            gpxTrack.WithSegments (segments);
            return gpxTrack;
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
            var f = new GpxFile ();
            f.Tracks.Add (gpxData.gpxTrack);

            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8 };
            using (var wr = XmlWriter.Create (gpxData.filename, writerSettings))
            {
                f.WriteTo (wr, new GpxWriterSettings ());
                // GpxWriter.Write (wr, null, gpxData.gpxMetaData, null, null);
            }

            // byte[] expected = File.ReadAllBytes (path);

            // note that this is not a guarantee in the general case.  the inputs here have all been
            // slightly tweaked such that it should succeed for our purposes.
            //  Assert.False (diff.HasDifferences (), string.Join (Environment.NewLine, diff.Differences));

            return 0;
        }
    }
}