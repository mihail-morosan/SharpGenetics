using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpGenetics.BaseClasses;
using Accord.Neuro;
using System.Runtime.Serialization;

namespace SharpGenetics.Predictor
{
    [DataContract]
    public class SOMPredictor : ResultPredictor
    {
        [DataMember]
        public int InputLayer = 1;

        /*DistanceNetwork Network = null;
        byte[] NetworkSerializeValue;
        [DataMember]
        public byte[] NetworkSerialize
        {
            get
            {
                return Accord.IO.Serializer.Save(Network);
            }
            set
            {
                NetworkSerializeValue = value;
            }
        }*/

        public SOMPredictor(RunParameters Parameters, int RandomSeed)
        {
            PredictorHelper.ApplyPropertiesToPredictor<NeuralNetworkOneOutputPredictor>(this, Parameters);

            /*MinVal.Clear();
            MaxVal.Clear();
            MinOutputVal.Clear();
            MaxOutputVal.Clear();

            MinOutputVal.Add(0);
            MaxOutputVal.Add(0);

            int InputLayerSize = 0;
            foreach (var P in Parameters.JsonParams.parameters)
            {
                if (P.enabled.Value)
                {
                    InputLayerSize++;
                    MinVal.Add((double)P.rangeMin);
                    MaxVal.Add((double)P.rangeMax);
                }
            }
            InputLayer = InputLayerSize;

            OutputLayer = 1;*/

            this.RandomSeed = RandomSeed;
            Accord.Math.Random.Generator.Seed = RandomSeed;

            CreateTrainingSet();

            Setup();
        }

        public void TrainNetwork(List<PopulationMember> Population)
        {
            lock (NetworkLock)
            {
                int Count = NetworkTrainingData.Count();

                var TrainingData = NetworkTrainingData.GetAllValues();
                var PopulationToPredict = Population.Where(p => p.Fitness < 0).ToArray();

                var Network = new DistanceNetwork(InputLayer, Count + PopulationToPredict.Count());

                int i = 0;
                foreach (var layer in Network.Layers)
                {
                    foreach (var neuron in layer.Neurons)
                    {
                        //neuron.RandGenerator = new UniformContinuousDistribution(new Range(0, 255));
                        
                        //neuron.Weights = (i < Count) ? TrainingData[i].Inputs.ToArray() : PopulationToPredict[i-Count].Vector.ToArray();

                        i++;
                    }
                }
            }


        }

        public override void AtStartOfGeneration(List<PopulationMember> Population, RunMetrics RunMetrics, int Generation)
        {
            throw new NotImplementedException();
        }

        public override List<double> Predict(List<double> Input)
        {
            throw new NotImplementedException();
        }

        public override void Setup()
        {

        }
    }
}
