using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using UnityEngine.InputSystem;

namespace GazeMetrics
{
    public class CalcMetrics : MonoBehaviour
    {

        [SerializeField]
        private string experimentDataRawPath;
        [SerializeField]
        private float excludeAngleThreshold = 7.5f;



        private Thread threadProc;
        private bool isProcessing = false;


        // Start is called before the first frame update
        void Start()
        {
            if (!File.Exists(experimentDataRawPath))
            {
                Debug.LogError($"File [{experimentDataRawPath}] does not exists!");
                UnityEditor.EditorApplication.isPlaying = false;
            }

            if (experimentDataRawPath.IndexOf("experiment_data_raw") < 0)
            {
                Debug.LogError($"[{experimentDataRawPath}] is not a valid experiment data path!");
                UnityEditor.EditorApplication.isPlaying = false;
            }

        }


        // Update is called once per frame
        void Update()
        {


            if (Keyboard.current.spaceKey.isPressed)
            {
                // Spacebar was pressed 
                Debug.Log("Spacebar was pressed !");

                if (!isProcessing)
                {
                    isProcessing = true;
                    threadProc = new Thread(ProcMetrics);
                    threadProc.Start();
                }
            }

        }


        void ProcMetrics()
        {
            List<SampleData> sampleDataList = new List<SampleData>();
            List<TargetMetrics> targetMetricsList = new List<TargetMetrics>();

            // load sample data
            //using (StreamReader sr = new StreamReader(new FileStream(experimentDataRawPath, FileMode.Open, FileAccess.Read)))
            using (StreamReader sr = new StreamReader(experimentDataRawPath))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("timeStamp"))  // header line
                        continue;

                    // decode sampledata of each line
                    SampleData sampleData = new SampleData();
                    if (StringLineToSampleData(line, ref sampleData))
                    {
                        sampleDataList.Add(sampleData);
                    }
                    else
                    {
                        Debug.LogError("line to SampleData error!");
                        sr.Close();
                        sr.Dispose();
                        return;
                    }
                }
            }

            // calc metricsvar
            var targetMetricsQuery = from sampledata in sampleDataList
                                     group sampledata by sampledata.targetId into sampleGroup
                                     select new TargetMetrics
                                     {
                                         targetId = sampleGroup.Key,
                                         localMarkerPosition = sampleGroup.First().localMarkerPosition,
                                         worldMarkerPosition = sampleGroup.First().worldMarkerPosition,
                                         AverageAccuracy = (from sm in sampleGroup where sm.exclude == false && sm.isValid == true select sm).Average(p => p.OffsetAngle),
                                         RmsPrecision = (from sm in sampleGroup where sm.exclude == false && sm.isValid == true select sm).RootMeanSquare(),
                                         SdPrecision = (from sm in sampleGroup where sm.exclude == false && sm.isValid == true select sm).StandardDeviation(),
                                         SampleCount = sampleGroup.Count(),
                                         ValidSamples = (from sm in sampleGroup where sm.isValid == true select sm).Count(),
                                         ExcludedSamples = (from sm in sampleGroup where sm.exclude == true select sm).Count()
                                     };
            targetMetricsList = targetMetricsQuery.ToList();

            // out to file
            string dirpath = Path.GetDirectoryName(experimentDataRawPath);
            string expID = Path.GetFileNameWithoutExtension(experimentDataRawPath).Replace("_experiment_data_raw", "");
            DataExporter.MetricsToCsv(dirpath, expID, targetMetricsList);

            // reset flag
            Debug.Log("process end");
            isProcessing = false;
        }



        bool StringLineToSampleData(string line, ref SampleData data)
        {
            string[] headers = SampleData.ToCSVHeader().Split(',');
            string[] items = line.Split(',');
            if (headers.Length != items.Length)
                return false;

            // trim
            for (int i = 0; i < items.Length; i++)
                items[i] = items[i].Trim();

            // update SampleData
            int idx = 0;
            data.timeStamp = ConvertTo<float>(items, ref idx);
            data.isValid = items[idx] == "Valid"; idx++;
            data.exclude = items[idx] == "Excluded"; idx++;
            data.targetId = ConvertTo<float>(items, ref idx);
            data.cameraPosition = ConvertTo<Vector3>(items, ref idx);
            data.localGazeOrigin = ConvertTo<Vector3>(items, ref idx);
            data.localGazeDirection = ConvertTo<Vector3>(items, ref idx);
            data.worldGazeOrigin = ConvertTo<Vector3>(items, ref idx);
            data.worldGazeDirection = ConvertTo<Vector3>(items, ref idx);
            data.worldGazeDistance = ConvertTo<float>(items, ref idx);
            data.localMarkerPosition = ConvertTo<Vector3>(items, ref idx);
            data.worldMarkerPosition = ConvertTo<Vector3>(items, ref idx);
            data.OffsetAngle = ConvertTo<float>(items, ref idx);
            data.interSampleAngle = ConvertTo<float>(items, ref idx);

            Ray r = new Ray(data.worldGazeOrigin, data.worldGazeDirection);
            data.wolrdGazePoint = r.GetPoint(data.worldGazeDistance);

            // threshhold for excluding
            if (data.OffsetAngle > excludeAngleThreshold)
                data.exclude = true;

            return true;
        }


        T ConvertTo<T>(string[] ss, ref int currIdx)
        {
            object rst = null;
            if (typeof(T) == typeof(float))
            {
                rst = float.Parse(ss[currIdx]);
                currIdx++;
            }
            else if (typeof(T) == typeof(bool))
            {
                rst = bool.Parse(ss[currIdx]);
                currIdx++;
            }
            else if (typeof(T) == typeof(Vector3))
            {
                float x = float.Parse(ss[currIdx]);
                float y = float.Parse(ss[currIdx + 1]);
                float z = float.Parse(ss[currIdx + 2]);
                rst = new Vector3(x, y, z);
                currIdx += 3;
            }

            return (T)rst;
        }


    }
}