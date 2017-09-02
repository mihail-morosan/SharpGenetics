using SharpGenetics.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [KnownType(typeof(List<double>))]
    [KnownType(typeof(List<int>))]
    [DataContractAttribute(IsReference=true)]
    public class RunParameters
    {   
        [DataMember]
        public string JsonParameters = "";

        public dynamic JsonParams = null;

        public RunParameters()
        {
        }

        public T GetParameter<T>(string key, T DefaultValue)
        {
            if(JsonParams.gaparams[key] != null)
            {
                return (T)JsonParams.gaparams[key];
            }
            else
            {
                return DefaultValue;
            }
        }

        public RunParameters Clone()
        {
            RunParameters clone = new RunParameters();
            clone.JsonParameters = JsonParameters;
            clone.JsonParams = JsonParams;
            return clone;
        }
    }
}
