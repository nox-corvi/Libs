using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls; 
using System.Windows.Media; 

namespace Nox.Win32.Controls.Base.Super
{
    public class XUserControl 
        : UserControl
    {
        private readonly LocaleService _LocaleService;

        public XUserControl(LocaleService LocaleService)
            : base()
        {
            _LocaleService = LocaleService;
        }
    }
}
