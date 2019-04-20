using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseEntities;

namespace OptionPricingModels.PricingModels
{
    internal static class SideHelper
    {
        public static OptionPosition FixGreeksAccordingToSide(OptionPosition option)
        {
            var sign = option.Side == Side.Buy ? 1 : -1;
            option.Delta *= sign;
            option.Gamma *= sign;
            option.Theta *= sign;
            option.Vega *= sign;
            option.Rho *= sign;

            return option;
        }
    }
}
