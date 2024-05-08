using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;

namespace Nox.Win32.Forms.Base;

public class MdiContainer 
    : Super.Dooper.CustomWindow
{

    #region Form Extensions
    public void BindForm(MdiChild form)
    {
        //form.BindToBase();
        form.Show();
    }
    #endregion

    public MdiContainer()
    {
        //InitializeComponent();
    }
}
