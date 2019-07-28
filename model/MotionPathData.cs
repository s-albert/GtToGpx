using Newtonsoft.Json;

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
            [JsonIgnore]
            [JsonProperty(Required = Required.Default)]
            public object partTimeMap;
        }