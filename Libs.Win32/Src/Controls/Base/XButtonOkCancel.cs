using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Nox.Win32.Controls.Base
{
    public class XButtonOkCancel
        : Super.XUserControl
    {
        private readonly XButtonOkay xButtonOkay;
        private readonly XButtonCancel xButtonCancel;

        public XButtonOkCancel()
            : this(null)
        {
        }

        public XButtonOkCancel(LocaleService localeService)
            : base(localeService)
        {
            xButtonOkay = new XButtonOkay(localeService);
            xButtonCancel = new XButtonCancel(localeService);

            var Container = new Super.XWrapPanel();
            Container.Children.Add(xButtonOkay);
            Container.Children.Add(new Super.XSeperator()
            {
                Width = 5
            });
            Container.Children.Add(xButtonCancel);

            this.Content = Container;
        }
    }
}
