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
        private static long offset = new DateTime (1970, 1, 1).Ticks;

        static void Main (string[] args)
        {
            // try
            // {
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
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine ("Error: " + ex.Message);
            // }
        }

        private static List<GpxInputData> MapToGpxData (List<Item> items)
        {
            List<GpxInputData> gpxData = new List<GpxInputData> ();

            foreach (Item item in items)
            {
                foreach (MotionPathData motionPathData in item.motionPathData)
                {
                    if (motionPathData.sportType >= 2 && motionPathData.sportType <= 5)
                    {

                        gpxData.Add (new GpxInputData ()
                        {
                            filename = GetFileName (motionPathData),
                                gpxMetaData = new GpxMetadata ("author"),
                                gpxTrack = GetGpxTrack (motionPathData)
                        });
                    }
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
                    break;
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
                                currentWaypoint = currentWaypoint.WithLatitude (new GpxLatitude (d));
                            }
                            break;
                        }
                    case "lon":
                        {
                            if (currentWaypoint != null)
                            {
                                currentWaypoint = currentWaypoint.WithLongitude (new GpxLongitude (double.Parse (pair[1])));
                            }
                            break;
                        }
                    case "t":
                        {
                            if (currentWaypoint != null)
                            {
                                var d = double.Parse (pair[1]);
                                var l = Convert.ToInt64 (d);
                                if (l > 10000000000)
                                {
                                    l *= 10000; // convert to ticks
                                }
                                else
                                {
                                    l *= 10000000;
                                }

                                l += offset; // 1970-01-01
                                DateTime t = new DateTime (l);
                                t = DateTime.SpecifyKind (t, DateTimeKind.Utc);
                                currentWaypoint.WithTimestampUtc (t);
                            }
                            break;
                        }
                }
            }
            System.Collections.Immutable.ImmutableArray<GpxTrackSegment> segments = System.Collections.Immutable.ImmutableArray<GpxTrackSegment>.Empty;
            segments = segments.Add (new GpxTrackSegment ().WithWaypoints (waypoints));
            return gpxTrack.WithSegments (segments);
        }

        private static string GetFileName (MotionPathData motionPathData)
        {
            long l = motionPathData.startTime;
            l *= 10000; // Ticks
            l += offset; // 1970
            DateTime t = new DateTime (l);
            var tz = int.Parse (motionPathData.timeZone);
            tz /= 100;
            t = t.AddHours (tz);

            return t.ToString("yyyy-MM-ddTHH_mm_ssZ") + " - " + motionPathData.sportType.ToString () + ".gpx";
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