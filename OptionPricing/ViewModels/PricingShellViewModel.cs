using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BaseEntities;
using OptionPricing.Annotations;
using OptionPricing.Services;
using OptionPricingModels;

namespace OptionPricing.ViewModels
{
    public class PricingShellViewModel : INotifyPropertyChanged
    {
        private OptionPosition _currentOption;
        private int _idCounter = 1;
        private ObservableCollection<OptionExerciseType> _optionExerciseTypes;
        private ObservableCollection<OptionPosition> _optionsPortfolio = new ObservableCollection<OptionPosition>();
        private ObservableCollection<OptionStyle> _optionStyles;
        private ObservableCollection<OptionType> _optionTypes;
        private double _prevRateShift;
        private double _prevVolShift;
        private ObservableCollection<OptionPricingModel> _pricingModels;
        private double _rateShift;
        private double _volShift;

        public PricingShellViewModel()
        {
            _currentOption = new OptionPosition();
            OptionStyles =
                new ObservableCollection<OptionStyle>(Enum.GetValues(typeof(OptionStyle)).Cast<OptionStyle>());
            OptionExerciseTypes =
                new ObservableCollection<OptionExerciseType>(
                    Enum.GetValues(typeof(OptionExerciseType)).Cast<OptionExerciseType>());
            OptionTypes = new ObservableCollection<OptionType>(Enum.GetValues(typeof(OptionType)).Cast<OptionType>());
            PricingModels =
                new ObservableCollection<OptionPricingModel>(
                    Enum.GetValues(typeof(OptionPricingModel)).Cast<OptionPricingModel>());
            ComputeThis();
            OptionsPortfolio.CollectionChanged += (sender, args) => ComputePortfolio();
        }

        public double VolShift
        {
            get { return _volShift; }
            set
            {
                _volShift = value;
                RaisePropertyChanged();
                foreach (var option in OptionsPortfolio)
                {
                    option.Volatility += value - _prevVolShift;
                }
                _prevVolShift = value;
                ComputePortfolio();
            }
        }

        public double RateShift
        {
            get { return _rateShift; }
            set
            {
                _rateShift = value;
                RaisePropertyChanged();
                foreach (var option in OptionsPortfolio)
                {
                    option.InterestRate += value - _prevRateShift;
                }
                _prevRateShift = value;
                ComputePortfolio();
            }
        }

        public string Underlying
        {
            get { return _currentOption.Instrument.Name; }
            set
            {
                _currentOption.Instrument.Name = value;
                RaisePropertyChanged();
            }
        }

        public OptionPosition CurrentOption
        {
            get { return _currentOption; }
            set
            {
                _currentOption = value;
                RaisePropertyChanged();
            }
        }

        public double UnderlyingPrice
        {
            get { return _currentOption.UnderlyingPrice; }
            set
            {
                _currentOption.UnderlyingPrice = value;
                _currentOption.Compute();
                RaisePropertyChanged();
                foreach (var option in OptionsPortfolio)
                {
                    option.UnderlyingPrice = value;
                }
                ComputeThis();
                ComputePortfolio();
            }
        }

        public ObservableCollection<OptionStyle> OptionStyles
        {
            get { return _optionStyles; }
            set
            {
                _optionStyles = value;
                RaisePropertyChanged();
            }
        }

        public OptionStyle SelectedStyle
        {
            get { return _currentOption.Style; }
            set
            {
                _currentOption.Style = value;
                RaisePropertyChanged();
                ComputeThis();
            }
        }

        public ObservableCollection<OptionExerciseType> OptionExerciseTypes
        {
            get { return _optionExerciseTypes; }
            set
            {
                _optionExerciseTypes = value;
                RaisePropertyChanged();
            }
        }

        public OptionExerciseType SelectedExerciseType
        {
            get { return _currentOption.ExerciseType; }
            set
            {
                _currentOption.ExerciseType = value;
                RaisePropertyChanged();
                ComputeThis();
            }
        }

        public ObservableCollection<OptionPricingModel> PricingModels
        {
            get { return _pricingModels; }
            set
            {
                _pricingModels = value;
                RaisePropertyChanged();
            }
        }

        public OptionPricingModel SelectedPricingModel
        {
            get { return _currentOption.Model; }
            set
            {
                _currentOption.Model = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsDividendCanBeUsed));
                ComputeThis();
            }
        }

        public ObservableCollection<OptionType> OptionTypes
        {
            get { return _optionTypes; }
            set
            {
                _optionTypes = value;
                RaisePropertyChanged();
            }
        }

        public OptionType SelectedType
        {
            get { return _currentOption.Type; }
            set
            {
                _currentOption.Type = value;
                RaisePropertyChanged();
                ComputeThis();
            }
        }

        public bool IsBuy
        {
            get { return _currentOption.Side == Side.Buy; }
            set
            {
                _currentOption.Side = value ? Side.Buy : Side.Sell;
                RaisePropertyChanged();
                ComputeThis();
            }
        }

        public double StrikePrice
        {
            get { return _currentOption.Strike; }
            set
            {
                _currentOption.Strike = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(StrikePricePct));
                ComputeThis();
            }
        }

        public double StrikePricePct => _currentOption.PctStrike;

        public DateTime ValuationDate
        {
            get { return _currentOption.ValuationDateTime; }
            set
            {
                _currentOption.ValuationDateTime = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TimeToExpiry));
                foreach (var option in OptionsPortfolio)
                {
                    option.ValuationDateTime = value;
                }
                ComputePortfolio();
                ComputeThis();
            }
        }

        public DateTime ExpirationDate
        {
            get { return _currentOption.ExpirationDateTime; }
            set
            {
                _currentOption.ExpirationDateTime = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TimeToExpiry));
                ComputeThis();
            }
        }

        public TimeSpan TimeToExpiry => _currentOption.TimeToExpiry;

        public double Volatility
        {
            get { return _currentOption.Volatility; }
            set
            {
                _currentOption.Volatility = value;
                RaisePropertyChanged();
                ComputeThis();
            }
        }

        public int TreeSteps
        {
            get { return _currentOption.TreeSteps; }
            set
            {
                _currentOption.TreeSteps = value;
                RaisePropertyChanged();
                ComputeThis();
            }
        }

        public double InterestRate
        {
            get { return _currentOption.InterestRate; }
            set
            {
                _currentOption.InterestRate = value;
                RaisePropertyChanged();
                ComputeThis();
            }
        }

        public bool IsDividendCanBeUsed => SelectedPricingModel == OptionPricingModel.BlackScholes || SelectedPricingModel == OptionPricingModel.Binomial;

        public double DividendRate
        {
            get { return _currentOption.DividendRate; }
            set
            {
                _currentOption.DividendRate = value;
                RaisePropertyChanged();
                ComputeThis();
            }
        }

        public ICommand AddCommand => new DelegateCommand(AddOption);
        public ICommand ShowExampleCommand => new DelegateCommand(ShowExample);

        public ObservableCollection<OptionPosition> OptionsPortfolio
        {
            get { return _optionsPortfolio; }
            set
            {
                _optionsPortfolio = value;
                RaisePropertyChanged();
            }
        }

        public double PL => OptionsPortfolio.Sum(p => p.PL);
        public double PortfolioDelta => OptionsPortfolio.Sum(p => p.PositionDelta);
        public double PortfolioCashDelta => OptionsPortfolio.Sum(p => p.CashDelta);
        public double PortfolioCashGamma => OptionsPortfolio.Sum(p => p.CashGamma);
        public double PortfolioCashVega => OptionsPortfolio.Sum(p => p.CashVega);
        public double PortfolioCashTheta => OptionsPortfolio.Sum(p => p.CashTheta);
        public double PortfolioCashRho => OptionsPortfolio.Sum(p => p.CashRho);

        public int Quantity
        {
            get { return _currentOption.Quantity; }
            set
            {
                _currentOption.Quantity = value;
                RaisePropertyChanged();
                ComputeThis();
            }
        }

        private void ShowExample(object obj)
        {
            OptionsPortfolio.Clear();

            var option1 =
                new OptionPosition
                {
                    ExpirationDateTime = DateTime.Now.AddYears(1),
                    Strike = 90,
                    Type = OptionType.Put,
                    Side = Side.Buy,
                    Id = _idCounter++,
                    Quantity = 1
                };
            option1.PropertyChanged += EvaluatePortfolio;

            var option2 =
                new OptionPosition
                {
                    Strike = 100,
                    Type = OptionType.Call,
                    Side = Side.Sell,
                    Id = _idCounter++,
                    Quantity = 1
                };
            option2.PropertyChanged += EvaluatePortfolio;

            option1.Compute();
            option2.Compute();

            option1.CostPrice = option1.PositionPrice;
            option2.CostPrice = option2.PositionPrice;

            OptionsPortfolio.Add(option1);
            OptionsPortfolio.Add(option2);
        }

        private void AddOption(object obj)
        {
            _currentOption.Id = _idCounter;
            _currentOption.CostPrice = _currentOption.PositionPrice;
            var option = ObjectCopier.Clone(_currentOption);
            option.PropertyChanged += EvaluatePortfolio;
            OptionsPortfolio.Add(option);
            _idCounter++;
            ComputePortfolio();
        }

        private void EvaluatePortfolio(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            RaisePropertyChanged(nameof(PL));
            RaisePropertyChanged(nameof(PortfolioDelta));
            RaisePropertyChanged(nameof(PortfolioCashDelta));
            RaisePropertyChanged(nameof(PortfolioCashGamma));
            RaisePropertyChanged(nameof(PortfolioCashRho));
            RaisePropertyChanged(nameof(PortfolioCashTheta));
            RaisePropertyChanged(nameof(PortfolioCashVega));
        }

        private void ComputeThis()
        {
            CurrentOption.Compute();
        }

        private void ComputePortfolio()
        {
            foreach (var option in OptionsPortfolio)
            {
                option.Compute();
            }
            EvaluatePortfolio(null, null);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}