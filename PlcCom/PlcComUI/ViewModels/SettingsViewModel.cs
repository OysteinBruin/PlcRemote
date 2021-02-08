﻿using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlcComUI.ViewModels
{
    public class SettingsViewModel : Screen
    {
        public SettingsViewModel()
        {
            this.PaletteSelectorViewModel = new PaletteSelectorViewModel();
        }
        public PaletteSelectorViewModel PaletteSelectorViewModel { get; set; }
    }
}
