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

        public void AddGeneration(double AvgFitness, double BestFitness)
        {
            AverageFitnesses.Add(AvgFitness);
            BestFitnesses.Add(BestFitness);
        }
    }
}
