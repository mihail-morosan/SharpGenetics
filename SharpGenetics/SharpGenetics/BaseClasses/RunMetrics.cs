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

        public void AddGeneration(double AvgFitness, double BestFitness, double ThirdQrtl)
        {
            AverageFitnesses.Add(AvgFitness);
            BestFitnesses.Add(BestFitness);
            ThirdQuartileOfFitnesses.Add(ThirdQrtl);
        }

        public static double GetThirdQuartile(List<double> Values)
        {
            if (Values.Count == 0)
                return 0;
            List<double> SortedFitnesses = new List<double>(Values);
            SortedFitnesses.Sort();

            Accord.DoubleRange range;
            Accord.Statistics.Measures.Quartiles(SortedFitnesses.ToArray(), out range, true);

            return range.Max;
        }
    }
}
