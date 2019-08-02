using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
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
            if (args.Length == 0)
            { // Hilfe
                Console.WriteLine ("Usage: GttoGpx data.json [outputpath]");
                Console.WriteLine ($"Version: " +
                    $"{Assembly.GetEntryAssembly().GetName().Version}");
            }
            else
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
                
                string path = null;
                if (args.Length > 1) {
                    path = args[1];
                }

                int i = 0;
                foreach (GpxInputData gpxData in gpxDataList)
                {
                    WriteToGpx (gpxData, path);
                    i++;
                }
                Console.WriteLine(i.ToString() + " gpx files created.");
            }
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine ("Error: " + ex.Message);
            // }
        }

        public string GetAssemblyVersion ()
        {
            return GetType ().Assembly.GetName ().Version.ToString ();
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
                            filename = GetName (motionPathData) + ".gpx",
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
            var attributes = motionPathData.attribute.Split (";");
            int i = 0;

            Point p = null;
            List<GpxWaypoint> waypoints = new List<GpxWaypoint> ();
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
                            AddWaypoint(p, waypoints);
                            p = new Point();

                            break;
                        }
                    case "lat":
                        {
                            if (p != null)
                            {
                                double d = double.Parse (pair[1]);
                                p.Lat = d;
                            }

                            break;
                        }
                    case "lon":
                        {
                            if (p != null)
                            {
                                double d = double.Parse (pair[1]);
                                p.Long = d;
                            }
                            break;
                        }
                    case "alt":
                        {
                            if (p != null)
                            {
                                double d = double.Parse (pair[1]);
                                p.Elev = d;
                            }
                            break;
                        }
                    case "t":
                        {
                            if (p != null)
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
                                p.T = t;
                            }
                            break;
                        }
                }
                AddWaypoint(p, waypoints);
            }
            System.Collections.Immutable.ImmutableArray<GpxTrackSegment> segments = System.Collections.Immutable.ImmutableArray<GpxTrackSegment>.Empty;
            segments = segments.Add (new GpxTrackSegment (new ImmutableGpxWaypointTable(waypoints), null));

            var gpxTrack = new GpxTrack (GetName (motionPathData), 
            "Steps: " + motionPathData.totalSteps, 
            "Cals: " + motionPathData.totalCalories, 
            "", 
            System.Collections.Immutable.ImmutableArray<GpxWebLink>.Empty,
            null,
            null,
            motionPathData.sportType,
            segments
            );
            return gpxTrack;
        }

        private static string ConvertCategory(int i) {
            switch(i)
            {
                case 2: return "Wandern"; 
                case 4: return "Laufen";
                case 3: return "Radfahren";
                case 5: return "Gehen";
                default: return "Aktivitaet";
            }
        }

        private static void AddWaypoint(Point p, List<GpxWaypoint> waypoints)
        {
            if (p != null && p.T != DateTime.MinValue && p.Lat != 0 && p.Long != 0 && p.Lat != 90 && p.Long != 90)
            {
                p.T = DateTime.SpecifyKind(p.T, DateTimeKind.Utc);
                waypoints.Add(new GpxWaypoint(new GpxLongitude(p.Long), new GpxLatitude(p.Lat), p.Elev, p.T, null, null, null, null, null, null, System.Collections.Immutable.ImmutableArray<GpxWebLink>.Empty, null, null, null, null, null, null, null, null, null, null));
            }
        }

        private static string GetName (MotionPathData motionPathData)
        {
            long l = motionPathData.startTime;
            l *= 10000; // Ticks
            l += offset; // 1970
            DateTime t = new DateTime (l);
            var tz = int.Parse (motionPathData.timeZone);
            tz /= 100;
            t = t.AddHours (tz);

            return t.ToString ("yyyy-MM-ddTHH_mm_ssZ") + " - " + ConvertCategory(motionPathData.sportType);
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
                Console.WriteLine(file + " read...");
                return items;
            }
        }

        private static void HandleDeserializationError (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            var currentError = errorArgs.ErrorContext.Error.Message;
            errorArgs.ErrorContext.Handled = true;
            Console.WriteLine ("Error: " + currentError);
        }

        private static int WriteToGpx (GpxInputData gpxData, string path)
        {
            var f = new GpxFile ();
            f.Tracks.Add (gpxData.gpxTrack);

            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8 };
            if (path != null) {
                path = Path.Combine(path, gpxData.filename);
            } else {
                path = gpxData.filename;
            }
            using (var wr = XmlWriter.Create (path, writerSettings))
            {
                f.WriteTo (wr, new GpxWriterSettings ());
                Console.WriteLine(path + " written...");
            }

            // byte[] expected = File.ReadAllBytes (path);

            // note that this is not a guarantee in the general case.  the inputs here have all been
            // slightly tweaked such that it should succeed for our purposes.
            //  Assert.False (diff.HasDifferences (), string.Join (Environment.NewLine, diff.Differences));

            return 0;
        }
    }
}