using SharpGenetics.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.BaseClasses
{
    [DataContractAttribute(IsReference=true)]
    public class RunParameters
    {
        [DataMember]
        public ObservableDictionary<string, object> _parameters { get; set; }

        public RunParameters()
        {
            _parameters = new ObservableDictionary<string, object>();
        }

        public void AddToParameters(string key, object value)
        {
            if (!_parameters.ContainsKey(key))
                _parameters.Add(key, value);
            else
                _parameters[key] = value;
        }

        public object GetParameter(string key, object DefaultValue = null)
        {
            if (_parameters.ContainsKey(key))
            {
                double t = DefaultValue == null ? 0 : (double)DefaultValue;
                if(!key.Substring(0,6).Equals("string") && Double.TryParse(""+_parameters[key], out t))
                {
                    return t;
                }
                return _parameters[key];
            } else
            {
                if (!key.Substring(0, 6).Equals("string"))
                {
                    return DefaultValue == null ? 0.0d : (double)DefaultValue;
                } else
                {
                    return DefaultValue == null ? "" : (string)DefaultValue;
                }
            }
        }

        public RunParameters Clone()
        {
            RunParameters clone = new RunParameters();
            foreach(var key in _parameters.Keys)
            {
                clone.AddToParameters(key, _parameters[key]);
            }
            return clone;
        }
    }
}
