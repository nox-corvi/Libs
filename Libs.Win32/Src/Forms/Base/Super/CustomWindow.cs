using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace Nox.Win32.Forms.Base.Super;

public partial class CustomWindow
    : System.Windows.Window
{
    #region Form Extensions
    protected List<CustomWindow> OpenForms() => Application.Current.Windows.OfType<CustomWindow>().ToList<CustomWindow>();

    protected IEnumerable<Control> FindControls(Func<Control, bool> Match)
    {
        var Result = new List<Control>();
        var Next = new Stack<Control>();

        Next.Push(this);
        while (Next.Count() > 0)
        {
            var C = Next.Pop();

            if (Match.Invoke(C))
            {
                Result.Add(C);
                //foreach (var Item in C.Controls)
                //    Next.Push((Control)Item);
            }
        }

        return Result;
    }

    //protected void SetAllMargins(Padding Margin)
    //{
    //    foreach (var Item in FindControls(f => true))
    //        Item.Margin = Margin;
    //}

    //protected void SetAllMargins(int Margin) =>
    //    SetAllMargins(new Padding(Margin));
    #endregion  


    public CustomWindow()
    {
        this.Width = 800;
        this.Height = 600;
    }
}
