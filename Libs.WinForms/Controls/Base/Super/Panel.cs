using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Libs.WinForms.Controls.Base.Super
{
    public enum AlignDirectionEnum
    { 
        LeftToRight = 0,
        TopToBottom = 1,
    }

    public enum OrderPropertyEnum
    {
        TabStop,
        Location, 
    }

    public class Panel : System.Windows.Forms.Panel
    {
        public bool AutoAlignControls { get; set; } = false;
        public AlignDirectionEnum AutoAlignDirection { get; set; } = AlignDirectionEnum.LeftToRight;

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
        }


        private void InitializeComponents()
        {

        }


        public Panel() 
            :base ()
        {
            InitializeComponents();
        }
    }
}
