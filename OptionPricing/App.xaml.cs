﻿using System;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;

namespace OptionPricing {
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            var bootstrapper = new Bootstrapper();
            bootstrapper.Start();
        }

        
    }
}