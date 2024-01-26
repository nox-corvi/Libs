using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Nox.Win32.Forms.Base;

public partial class MdiContainer 
    : Super.FormSuper
{

    #region Form Extensions
    public void BindForm(MdiChild form)
    {
        form.BindToBase();
        form.Show();
    }
    #endregion

    public MdiContainer()
    {
        InitializeComponent();
    }
}
