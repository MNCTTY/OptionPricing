// (c) Konstantin Brownstein 2016, except implementation

using System;

namespace OptionPricingModels.PricingModels
{
    internal class BinomialModel : IOptionsPricingModel
    {
        public void Compute(OptionPosition option)
        {
            int steps = option.TreeSteps;
            double s = option.UnderlyingPrice;

            var t = option.TimeToExpiry.TotalDays / 365.0;
            if (Math.Abs(t) < Double.Epsilon) return;
            if (option.TreeSteps == 0) return;

            var deltaT = t/(double)steps;

            if (deltaT >= Math.Pow(option.Volatility, 2)/Math.Pow(option.InterestRate - option.DividendRate, 2))
            {
                return;
            }

            double u = Math.Exp(option.Volatility*Math.Sqrt(t));
            double d = 1 / u;

            var prices = GetPrices(option.Type == OptionType.Call, option.UnderlyingPrice, t, steps, option.Volatility,
                option.Strike, option.InterestRate, option.DividendRate);
            // x x x
            //   x x
            //     x


            option.OptionPrice = prices[0, 0];
            option.Delta = (prices[1, 1] - prices[1, 0])/(s*u - s*d);
            option.Gamma = ((prices[2, 2] - prices[2, 1])/(s*u*u - s*u*d) -
                            (prices[2, 1] - prices[2, 0])/(s*u*d - s*d*d))/(0.5*(s*u*u - s*d*d));
            option.Theta = (prices[2, 1] - prices[0, 0])/(2*deltaT);

            option.Vega =
                (GetPrices(option.Type == OptionType.Call, option.UnderlyingPrice, t, steps, option.Volatility*1.01,
                    option.Strike, option.InterestRate, option.DividendRate)[0, 0]
                 - option.OptionPrice)/(option.Volatility*0.01);

            option.Rho =
                (GetPrices(option.Type == OptionType.Call, option.UnderlyingPrice, t, steps, option.Volatility,
                    option.Strike, option.InterestRate*1.01, option.DividendRate)[0, 0]
                 - option.OptionPrice)/(option.InterestRate*0.01);

            SideHelper.FixGreeksAccordingToSide(option);
        }

        private static double[,] GetPrices(bool isCall, double s, double t, int steps, double v, double k, double r, double q)
        {
            var deltaT = t / (double)steps;

            double u = Math.Exp(v * Math.Sqrt(t));
            double d = 1 / u;

            double[] prices = new double[steps + 1];

            for (int i = 0; i <= steps; i++)
            {
                prices[i] = GetPayoff(isCall, GetStockFinalPrice(s, u, d, i, steps), k);
            }

            double[,] pGreeks = new double[3, 3];

            for (int n = steps - 1; n >= 0; n--)
            {
                for (int i = 0; i <= n; i++)
                {
                    double p = GetProbability(r, q, deltaT, u, d);
                    prices[i] = Math.Exp(-(r - q) * deltaT) *
                                (p * prices[i + 1] + (1 - p) * prices[i]);

                    if (n < 3)
                    {
                        pGreeks[n, i] = prices[i];
                    }
                }
            }

            return pGreeks;
        }

        private static double GetProbability(double interestRate, double dividendRate, double deltaT, double u, double d)
        {
            return (Math.Exp((interestRate - dividendRate)*deltaT) - d)/(u - d);
        }

        private static double GetPayoff(bool isCall, double underlyingPrice, double strike)
        {
            return isCall
                ? Math.Max(underlyingPrice - strike, 0)
                : Math.Max(strike - underlyingPrice, 0);
        }

        public static double GetStockFinalPrice(double initialPrice, double u, double d, int upSteps, int totalSteps)
        {
            int downSteps = totalSteps - upSteps;

            return initialPrice*Math.Pow(u, upSteps - downSteps);
        }
    }
}