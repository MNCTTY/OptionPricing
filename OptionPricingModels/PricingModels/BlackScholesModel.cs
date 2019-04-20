// (c) Konstantin Brownstein 2016, except implementation

using System;
using BaseEntities;
using OptionPricingModels.Services;

namespace OptionPricingModels.PricingModels {
    internal class BlackScholesModel : IOptionsPricingModel {
        public void Compute(OptionPosition option) {
            var t = option.TimeToExpiry.TotalDays/365.0;
            if (Math.Abs(t) < Double.Epsilon) return;

            var d1 = Math.Log(option.UnderlyingPrice / option.Strike) + (option.InterestRate - option.DividendRate + Math.Pow(option.Volatility, 2)) / t;
            var d2 = d1 - option.Volatility*Math.Sqrt(t);

            // when calculating option greeks keep in mind that we calculate greek for position, 
            // and that's why BS formulas for greeks must be multiplied by -1 in case we're selling options
            // so formulas for greeks are [sign * BSFormula]

            if (option.Type == OptionType.Call)
            {
                option.OptionPrice = option.UnderlyingPrice*NormalDistribution.Phi(d1) -
                                     option.Strike*Math.Exp(-option.InterestRate*t)*NormalDistribution.Phi(d2);

                option.Delta = Math.Exp(-option.DividendRate*t)*NormalDistribution.Phi(d1);

                option.Theta = -Math.Exp(-option.DividendRate*t)*option.UnderlyingPrice*NormalDistribution.Density(d1)*
                               option.Volatility/(2*Math.Sqrt(t))
                               +
                               option.DividendRate*Math.Exp(-option.DividendRate*t)*option.UnderlyingPrice*
                               NormalDistribution.Phi(d1)
                               -
                               option.InterestRate*option.Strike*Math.Exp(-option.InterestRate*t)*
                               NormalDistribution.Phi(d2);

                option.Rho = t*option.Strike*Math.Exp(-option.InterestRate*t)*NormalDistribution.Phi(d2);
            }
            else
            {
                option.OptionPrice = -option.UnderlyingPrice * NormalDistribution.Phi(-d1) +
                                     option.Strike * Math.Exp(-option.InterestRate * t) * NormalDistribution.Phi(-d2);

                option.Delta = Math.Exp(-option.DividendRate * t) * (NormalDistribution.Phi(d1) - 1);

                option.Theta = -Math.Exp(-option.DividendRate * t) * option.UnderlyingPrice * NormalDistribution.Density(d1) *
                               option.Volatility / (2 * Math.Sqrt(t))
                               -
                               option.DividendRate * Math.Exp(-option.DividendRate * t) * option.UnderlyingPrice *
                               NormalDistribution.Phi(-d1)
                               +
                               option.InterestRate * option.Strike * Math.Exp(-option.InterestRate * t) *
                               NormalDistribution.Phi(-d2);

                option.Rho = -t * option.Strike * Math.Exp(-option.InterestRate * t) * NormalDistribution.Phi(-d2);
            }

            option.Gamma = Math.Exp(-option.DividendRate*t)*NormalDistribution.Density(d1)/
                           (option.UnderlyingPrice*option.Volatility*Math.Sqrt(t));
            option.Vega = Math.Exp(-option.DividendRate*t)*option.UnderlyingPrice*NormalDistribution.Density(d1)*
                          Math.Sqrt(t);

            SideHelper.FixGreeksAccordingToSide(option);
        }
    }
}