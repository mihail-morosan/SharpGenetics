using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Predictor
{
    [DataContractAttribute]
    public class InputOutputPair
    {
        public InputOutputPair()
        {
            Inputs = new List<double>();
            Outputs = new List<double>();
        }

        public InputOutputPair(List<double> In, List<double> Out)
        {
            Inputs = new List<double>(In);
            Outputs = new List<double>(Out);
        }

        [DataMember]
        public List<double> Inputs;

        [DataMember]
        public List<double> Outputs;

        public static List<double> Normalise(List<double> Values, List<double> Min, List<double> Max)
        {
            List<double> Ret = new List<double>(Values);
            for (int i = 0; i < Values.Count; i++)
            {
                Ret[i] = Math.Min(1, (Ret[i] - Min[i]) / (Max[i] - Min[i]));
            }
            return Ret;
        }
    }

}
