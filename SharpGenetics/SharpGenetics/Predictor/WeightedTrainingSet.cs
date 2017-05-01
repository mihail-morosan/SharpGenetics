using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    public class WeightedTrainingSet
    {
        SortedList<double, InputOutputPair> HighValues = new SortedList<double, InputOutputPair>();
        SortedList<double, InputOutputPair> LowValues = new SortedList<double, InputOutputPair>();
        List<InputOutputPair> OtherValues = new List<InputOutputPair>();

        int HighValuesCapacity = 0;
        int LowValuesCapacity = 0;
        int MaxCapacity = 100;

        public WeightedTrainingSet(int HighValuesCapacity = 0, int LowValuesCapacity = 0, int MaxCapacity = 100)
        {
            this.HighValuesCapacity = HighValuesCapacity;
            this.LowValuesCapacity = LowValuesCapacity;
            this.MaxCapacity = MaxCapacity;
        }

        public void AddIndividualToTrainingSet(InputOutputPair Individual)
        {
            HighValues.Add(Individual.Outputs.Sum(), Individual);
            LowValues.Add(Individual.Outputs.Sum(), Individual);
            OtherValues.Add(Individual);

            if(HighValues.Count > HighValuesCapacity)
            {
                HighValues.RemoveAt(0);
            }

            if(LowValues.Count > LowValuesCapacity)
            {
                LowValues.RemoveAt(LowValues.Count - 1);
            }

            if(OtherValues.Count > (MaxCapacity - LowValuesCapacity - HighValuesCapacity))
            {
                OtherValues.RemoveAt(0);
            }
        }

        public List<InputOutputPair> GetAllValues()
        {
            var All = new List<InputOutputPair>(OtherValues);
            All.AddRange(HighValues.Values);
            All.AddRange(LowValues.Values);

            return All;
        }
    }
}
