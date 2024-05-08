using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Nox.Win32.Controls.Base
{
    public class XButtonOkay
        : Super.XButton
    {

        public XButtonOkay()
            : base(null)
        {

        }

        public XButtonOkay(LocaleService localeService)
            : base(localeService)
        {
            this.IsDefault = true;
            Content = localeService.GetLocalizedString(LocaleKey.BUTTON_OK);
        }
    }
}
