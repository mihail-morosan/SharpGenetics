using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Helpers
{
    public class ImportantParameterAttribute : Attribute
    {
        public ImportantParameterAttribute(string ParameterName)
        {
            this.ParameterName = ParameterName;
        }

        private string _parameterName = "";
        public string ParameterName
        {
            get { return _parameterName; }
            set { _parameterName = value; }
        }
    }
}
