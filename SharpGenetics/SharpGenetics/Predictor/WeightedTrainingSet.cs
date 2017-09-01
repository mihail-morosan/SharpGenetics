﻿using System;
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

        public WeightedTrainingSet(int HighValuesCapacity = 0, int LowValuesCapacity = 0, int MaxCapacity = 100)
        {
            this.HighValuesCapacity = HighValuesCapacity;
            this.LowValuesCapacity = LowValuesCapacity;
            this.MaxCapacity = MaxCapacity;
        }

        public override int Count()
        {
            return HighValues.Count + LowValues.Count + OtherValues.Count;
        }

        public override void AddIndividualToTrainingSet(InputOutputPair Individual)
        {
            if (HighValues.Count < HighValuesCapacity || (HighValues.Count > 0 && HighValues.Min(i => i.Outputs.Sum()) < Individual.Outputs.Sum()))
            {
                HighValues.Add(Individual);
            }

            if(LowValues.Count < LowValuesCapacity || (LowValues.Count > 0 && LowValues.Max(i=>i.Outputs.Sum()) > Individual.Outputs.Sum()))
            {
                LowValues.Add(Individual);
            }
            
            OtherValues.Add(Individual);

            if(HighValues.Count > HighValuesCapacity)
            {
                double MinVal = HighValues.Min(i => i.Outputs.Sum());
                HighValues.RemoveAll(i => i.Outputs.Sum() == MinVal);
                //HighValues.Remove(HighValues.Min(i => i.Outputs.Sum()));
            }

            if(LowValues.Count > LowValuesCapacity)
            {
                double MaxVal = LowValues.Max(i => i.Outputs.Sum());
                LowValues.RemoveAll(i => i.Outputs.Sum() == MaxVal);
                //LowValues.Remove(LowValues.Max(i => i.Outputs.Sum()));
            }

            if(OtherValues.Count > (MaxCapacity - LowValuesCapacity - HighValuesCapacity))
            {
                OtherValues.RemoveAt(0);
            }
        }

        public override List<InputOutputPair> GetAllValues()
        {
            var All = new List<InputOutputPair>(OtherValues);
            All.AddRange(HighValues);
            All.AddRange(LowValues);

            return All;
        }
    }
}
