using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public class WeightedTrainingSet
    {
        [DataMember]
        List<InputOutputPair> HighValues = new List<InputOutputPair>();

        [DataMember]
        List<InputOutputPair> LowValues = new List<InputOutputPair>();

        [DataMember]
        List<InputOutputPair> OtherValues = new List<InputOutputPair>();

        [DataMember]
        int HighValuesCapacity = 0;

        [DataMember]
        int LowValuesCapacity = 0;

        [DataMember]
        int MaxCapacity = 100;

        public WeightedTrainingSet(int HighValuesCapacity = 0, int LowValuesCapacity = 0, int MaxCapacity = 100)
        {
            this.HighValuesCapacity = HighValuesCapacity;
            this.LowValuesCapacity = LowValuesCapacity;
            this.MaxCapacity = MaxCapacity;
        }

        public int Count()
        {
            return HighValues.Count + LowValues.Count + OtherValues.Count;
        }

        public void AddIndividualToTrainingSet(InputOutputPair Individual)
        {
            if (HighValues.Count == 0 || HighValues.Min(i => i.Outputs.Sum()) < Individual.Outputs.Sum())
            {
                HighValues.Add(Individual);
            }

            if(LowValues.Count == 0 || LowValues.Max(i=>i.Outputs.Sum()) > Individual.Outputs.Sum())
            {
                LowValues.Add(Individual);
            }
            
            OtherValues.Add(Individual);

            if(HighValues.Count > HighValuesCapacity)
            {
                HighValues.Remove(HighValues.Min());
            }

            if(LowValues.Count > LowValuesCapacity)
            {
                LowValues.Remove(LowValues.Max());
            }

            if(OtherValues.Count > (MaxCapacity - LowValuesCapacity - HighValuesCapacity))
            {
                OtherValues.RemoveAt(0);
            }
        }

        public List<InputOutputPair> GetAllValues()
        {
            var All = new List<InputOutputPair>(OtherValues);
            All.AddRange(HighValues);
            All.AddRange(LowValues);

            return All;
        }
    }
}
