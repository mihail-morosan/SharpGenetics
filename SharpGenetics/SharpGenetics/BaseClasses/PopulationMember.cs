using SharpGenetics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [DataContractAttribute]
    public class GenericTest<InputT,OutputT>
    {
        [DataMember]
        public Dictionary<String, InputT> Inputs { get; set; }
        [DataMember]
        public List<OutputT> Outputs { get; set; }

        public GenericTest()
        {
            Inputs = new Dictionary<string, InputT>();
            Outputs = new List<OutputT>();
        }

        public void AddInput(String key, InputT value)
        {
            Inputs.Add(key, value);
        }

        public void AddOutput(OutputT value)
        {
            Outputs.Add(value);
        }

        public override string ToString()
        {
            string res = "Test( ";
            foreach(var input in Inputs.Keys)
            {
                res += input + "=" + Inputs[input] + " ";
            }
            res += ") = ( ";
            foreach(var output in Outputs)
            {
                res += output;
            }
            res += " )";

            return res;
        }
    }

    [DataContractAttribute]
    public abstract class PopulationMember
    {
        public abstract void ReloadParameters<T,I,O>(PopulationManager<T, I, O> Manager) where T:PopulationMember;

        public abstract double CalculateFitness<T, Y>(int CurrentGeneration, params GenericTest<T, Y>[] values);

        public abstract double GetFitness();

        public abstract T Crossover<T>(T b) where T : PopulationMember;

        public abstract T Mutate<T>() where T : PopulationMember;

        public abstract PopulationMember Clone();

        [DataMember]
        public double Fitness = -1;

        [DataMember]
        public List<double> ObjectivesFitness = new List<double>();

        [DataMember]
        public List<double> Vector = new List<double>();

        [DataMember]
        public CRandom rand;

        [DataMember]
        public int CreatedAtGeneration = 0;

        [DataMember]
        public int UpdatedAtGeneration = 0;

        [DataMember]
        public bool Predicted = false;
        
        public abstract PopulationManager<T, I, O> GetParentManager<T,I,O>() where T: PopulationMember;

        public CRandom GetRandomGenerator()
        {
            return rand;
        }

        public void SetRandomGenerator(CRandom rand)
        {
            this.rand = rand;
        }
    }
}
