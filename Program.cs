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
                    gpxData.Add (new GpxInputData () { filename = GetFileName (item.recordDay, motionPathData), gpxMetaData = new GpxMetadata("author"), gpxTrack = GetGpxMetadata (motionPathData) });
                }
            }
            return gpxData;
        }

        private static GpxTrack GetGpxTrack (MotionPathData motionPathData)
        {
            var gpxTrack = new GpxTrack ();

            var attributes = motionPathData.attribute.Split (";");
            int i = 1;

            List<GpxWaypoint> waypoints = new List<GpxWaypoint> ();
            GpxWaypoint currentWaypoint = null;
            while (i < attributes.Length)
            {
                var a = attributes[i];
                var aValue = a.Split ("=");
                switch (aValue[0])
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
                                currentWaypoint.WithLatitude (new GpxLatitude (double.Parse (aValue[1])));
                            }
                            break;
                        }
                    case "lon":
                        {
                            if (currentWaypoint != null)
                            {
                                currentWaypoint.WithLongitude (new GpxLongitude (double.Parse (aValue[1])));
                            }
                            break;
                        }
                    case "t":
                        {
                            if (currentWaypoint != null)
                            {
                                currentWaypoint.WithTimestampUtc (DateTime.Parse (aValue[1]));
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
            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8 };
            using (var wr = XmlWriter.Create (gpxData.filename, writerSettings))
            {
                GpxWriter.Write (wr, null, gpxData.gpxMetaData, null, null);
            }

            IFeature

            // byte[] expected = File.ReadAllBytes (path);

            // note that this is not a guarantee in the general case.  the inputs here have all been
            // slightly tweaked such that it should succeed for our purposes.
            //  Assert.False (diff.HasDifferences (), string.Join (Environment.NewLine, diff.Differences));

            return 0;
        }
    }
}