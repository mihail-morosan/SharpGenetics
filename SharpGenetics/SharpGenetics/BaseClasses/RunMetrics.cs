using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [DataContract]
    public class RunMetrics
    {
        [DataMember]
        public List<double> AverageFitnesses = new List<double>();
        [DataMember]
        public List<double> BestFitnesses = new List<double>();
        [DataMember]
        public List<double> ThirdQuartileOfFitnesses = new List<double>();
        [DataMember]
        public List<double> MedianOfFitnesses = new List<double>();

        public void AddGeneration(List<double> Values)
        {
            if (Values.Count == 0)
                return;
            var Sorted = GetSortedList(Values);
            AverageFitnesses.Add(Sorted.Average());
            BestFitnesses.Add(Sorted[0]);
            ThirdQuartileOfFitnesses.Add(GetThirdQuartile(Sorted));
            MedianOfFitnesses.Add(GetMedian(Sorted));
        }

        public static List<double> GetSortedList(List<double> Values)
        {
            var Sorted = new List<double>(Values);
            Sorted.Sort();
            return Sorted;
        }

        public static double GetThirdQuartile(List<double> Values)
        {
            Accord.DoubleRange range;
            Accord.Statistics.Measures.Quartiles(Values.ToArray(), out range, true);

            return range.Max;
        }

        public static double GetMedian(List<double> Values)
        {
            return Accord.Statistics.Measures.Median(Values.ToArray());
        }
    }
}
