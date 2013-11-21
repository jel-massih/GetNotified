using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;

namespace GetNotified.NotifyControls
{
    public partial class MediumTileFront : UserControl
    {
        private string iconUrl;
        public void SetProperties(string imageSource, string temperature)
        {
            this.DataContext = this;
            this.iconUrl = imageSource;
            this.temperature.Text = temperature;
        }

        public MediumTileFront()
        {
            InitializeComponent();
        }
    }
}