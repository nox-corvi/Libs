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
    public class XButtonCancel
        : Super.XButton
    {
        public XButtonCancel()
            : base()
        {
            this.IsCancel = true;
            this.Content = "&Cancel";
        }
    }
}
