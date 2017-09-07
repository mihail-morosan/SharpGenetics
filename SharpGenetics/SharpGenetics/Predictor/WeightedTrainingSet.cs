using Accord.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public class WeightedTrainingSet : TrainingSet
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

        [DataMember]
        int MinCapacity = 100;

        public WeightedTrainingSet(int HighValuesCapacity = 0, int LowValuesCapacity = 0, int MinCapacity = 100, int MaxCapacity = 100)
        {
            this.HighValuesCapacity = HighValuesCapacity;
            this.LowValuesCapacity = LowValuesCapacity;
            this.MaxCapacity = MaxCapacity;
            this.MinCapacity = MinCapacity;
        }

        public override int Count()
        {
            return HighValues.Count + LowValues.Count + OtherValues.Count;
        }

        public override void AddIndividualToTrainingSet(InputOutputPair Individual)
        {
            bool alreadyAdded = false;

            if (LowValues.Count < LowValuesCapacity || (LowValues.Count > 0 && LowValues.Max(i => i.Outputs.Sum()) > Individual.Outputs.Sum()))
            {
                LowValues.Add(Individual);
                alreadyAdded = true;
            }

            if (HighValues.Count < HighValuesCapacity || (HighValues.Count > 0 && HighValues.Min(i => i.Outputs.Sum()) < Individual.Outputs.Sum()))
            {
                if (!alreadyAdded)
                {
                    HighValues.Add(Individual);
                    alreadyAdded = true;
                }
            }

            if (!alreadyAdded)
            {
                OtherValues.Add(Individual);
            }

            if (HighValues.Count > HighValuesCapacity)
            {
                double MinVal = HighValues.Min(i => i.Outputs.Sum());
                HighValues.Remove(HighValues.Find(t => t.Outputs.Sum() == MinVal));
                //HighValues.RemoveAll(i => i.Outputs.Sum() == MinVal);
                //HighValues.Remove(HighValues.Min(i => i.Outputs.Sum()));
            }

            if (LowValues.Count > LowValuesCapacity)
            {
                double MaxVal = LowValues.Max(i => i.Outputs.Sum());
                LowValues.Remove(LowValues.Find(t => t.Outputs.Sum() == MaxVal));
                //LowValues.RemoveAll(i => i.Outputs.Sum() == MaxVal);
                //LowValues.Remove(LowValues.Max(i => i.Outputs.Sum()));
            }

            if (OtherValues.Count > (MaxCapacity - LowValuesCapacity - HighValuesCapacity))
            {
                OtherValues.RemoveAt(0);
            }
        }

        public override List<InputOutputPair> GetAllValues(bool Shuffle = false)
        {
            var All = new List<InputOutputPair>(HighValues);
            All.AddRange(LowValues);
            All.AddRange(OtherValues);

            if (Shuffle)
            {
                All.Shuffle();
            }

            return All;
        }
    }
}
