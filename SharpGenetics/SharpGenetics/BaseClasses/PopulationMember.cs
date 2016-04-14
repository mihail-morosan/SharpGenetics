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

    public interface PopulationMember
    {
        double CalculateFitness<T,Y>(params GenericTest<T,Y>[] values);

        T Crossover<T>(T b) where T : PopulationMember;

        T Mutate<T>() where T : PopulationMember;

        CRandom GetRandomGenerator();

        void SetRandomGenerator(CRandom rand);

        PopulationMember Clone();
    }
}
