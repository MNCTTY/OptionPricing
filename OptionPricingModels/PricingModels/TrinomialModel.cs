// (c) Konstantin Brownstein 2016, except implementation

using System;

namespace OptionPricingModels.PricingModels {
    internal class TrinomialModel : IOptionsPricingModel {
        public void Compute(OptionPosition option)
        {
            var t = option.TimeToExpiry.TotalDays / 365.0;
            if (Math.Abs(t) < Double.Epsilon) return;
            if (option.TreeSteps == 0) return;
            
            Func<double, double> getWithTime = x => GetPrice(option.Type == OptionType.Call, option.UnderlyingPrice, option.Strike,
                option.Volatility, option.InterestRate, option.DividendRate, x, option.TreeSteps)[0, 0];
            Func<double, double> getWithVol = x => GetPrice(option.Type == OptionType.Call, option.UnderlyingPrice, option.Strike,
                x, option.InterestRate, option.DividendRate, t, option.TreeSteps)[0, 0];
            Func<double, double> getWithR = x => GetPrice(option.Type == OptionType.Call, option.UnderlyingPrice, option.Strike,
                option.Volatility, x, option.DividendRate, t, option.TreeSteps)[0, 0];

            var deltaT = t / (double)option.TreeSteps;

            double u = Math.Exp(option.Volatility * Math.Sqrt(2 * deltaT));
            double d = Math.Exp(-option.Volatility * Math.Sqrt(2 * deltaT));

            var prices = GetPrice(option.Type == OptionType.Call, option.UnderlyingPrice, option.Strike,
                option.Volatility, option.InterestRate, option.DividendRate, t, option.TreeSteps);

            option.OptionPrice = prices[0, 0];

            option.Delta = (prices[1, 2] - 2 * prices[1, 1] + prices[1, 0])/(option.UnderlyingPrice*(u - d));

            option.Gamma = ((prices[2, 4] - 2 * prices[2, 3] + prices[2, 2])/(Math.Pow(option.UnderlyingPrice*(u*u - u),2)) -
                            (prices[2, 2] - 2 * prices[2, 1] + prices[2, 0])/(Math.Pow(option.UnderlyingPrice*(d - d*d),2)))/
                           (0.5*(option.UnderlyingPrice*(u*u - d*d)));

            option.Theta = (getWithTime(t + deltaT) - getWithTime(t - deltaT))/(2*deltaT);

            double volDelta = option.Volatility*0.01;
            option.Vega = (getWithVol(option.Volatility + volDelta) - getWithVol(option.Volatility - volDelta))/
                          (2*volDelta);

            double rDelta = option.InterestRate * 0.01;
            option.Rho = (getWithR(option.InterestRate + rDelta) - getWithR(option.InterestRate - rDelta)) /
                          (2 * rDelta);

            SideHelper.FixGreeksAccordingToSide(option);
        }

        private static double[,] GetPrice(bool isCall, double s, double k, double v, double r, double q, double t, int steps)
        {
            int treeSize = (2 * steps + 1);
            double[] prices = new double[treeSize];

            var deltaT = t / (double)steps;
            
            double u = Math.Exp(v * Math.Sqrt(2 * deltaT));
            double d = Math.Exp(-v * Math.Sqrt(2 * deltaT));

            double pU = Math.Pow(
                (Math.Exp((r - q) * deltaT / 2) -
                 Math.Exp(-v* Math.Sqrt(deltaT / 2))) /
                (Math.Exp(v * Math.Sqrt(deltaT / 2)) -
                 Math.Exp(-v * Math.Sqrt(deltaT / 2))),
                2);

            double pD = Math.Pow(
                (Math.Exp(v * Math.Sqrt(deltaT / 2)) -
                 Math.Exp((r - q) * deltaT / 2))
                / (Math.Exp(v * Math.Sqrt(deltaT / 2)) - Math.Exp(-v* Math.Sqrt(deltaT / 2))),
                2);

            double pM = 1 - pU - pD;

            prices[steps] = s;

            for (int gen = 1; gen <= steps; gen++)
            {
                for (int i = -gen; i < 0; i++)
                {
                    prices[steps + i] = prices[steps + i + 1] * d;
                }
                for (int i = gen; i > 0; i--)
                {
                    prices[steps + i] = prices[steps + i - 1] * u;
                }
            }

            for (int i = 0; i < prices.Length; i++)
            {
                prices[i] = GetPayoff(isCall, prices[i], k);
            }

            double[,] result = new double[3, 5];

            for (int n = treeSize - 3; n >= 0; n -= 2)
            {
                for (int i = 0; i <= n; i++)
                {
                    prices[i] = Math.Exp(-(r - q) * deltaT) *
                                (pU * prices[i + 2] + pM * prices[i + 1] * pD * prices[i]);

                    if (n <= 4)
                    {
                        result[n/2, i] = prices[i];
                    }
                }
            }

            return result;
        }

        private static double GetPayoff(bool isCall, double underlyingPrice, double strike)
        {
            return isCall
                ? Math.Max(underlyingPrice - strike, 0)
                : Math.Max(strike - underlyingPrice, 0);
        }
    }
}