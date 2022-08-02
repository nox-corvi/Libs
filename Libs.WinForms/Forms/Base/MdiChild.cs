using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Nox.WinForms.Forms.Base
{
    public partial class MdiChild : Super.FormSuper
    {
        #region Form Extensions
        public void BindToBase()
        {
            MdiContainer C = base.FindMdiParent();

            if (C != null)
                this.MdiParent = C;
            else
                throw new Exception("mdi container not found");
        }


        #endregion

        public MdiChild()
        {
            InitializeComponent();
        }
    }
}
