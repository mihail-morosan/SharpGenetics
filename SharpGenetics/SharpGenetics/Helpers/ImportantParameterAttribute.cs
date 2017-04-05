using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGenetics.Helpers
{
    public class ImportantParameterAttribute : Attribute
    {
        public ImportantParameterAttribute(string ParameterName, string FriendlyName = "", double RangeMin = -1, double RangeMax = -1, double Default = -1)
        {
            this.ParameterName = ParameterName;
            this.FriendlyName = FriendlyName.Length > 0 ? FriendlyName : ParameterName;
            this.RangeMin = RangeMin;
            this.RangeMax = RangeMax;
            this.Default = Default;
        }

        private string _parameterName = "";
        public string ParameterName
        {
            get { return _parameterName; }
            set { _parameterName = value; }
        }

        //private string _friendlyName = "";
        //private double _rangeMin = -1;
        //private double _rangeMax = -1;
        //private double _default = -1;

        public string FriendlyName { get; set; }
        public double RangeMin { get; set; }
        public double RangeMax { get; set; }
        public double Default { get; set; }
    }
}
