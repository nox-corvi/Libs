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
    public class XButton
        : Button
    {
        private readonly LocaleService _localeService;

        public XButton() 
            : this(null)
        {

        }

        public XButton(LocaleService localeService)
            : base()
        {
            _localeService = localeService;
            Content = this.Name;
        }
    }
}
