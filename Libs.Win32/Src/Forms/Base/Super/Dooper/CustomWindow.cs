using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;


namespace Nox.Win32.Forms.Base.Super.Dooper;

public partial class CustomWindow
    : Window
{
    private readonly LocaleService _localeService;

    public LocaleService LocaleService { get => _localeService; }

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

    #region Xaml Helpers
    public static T LoadFromXamlFile<T>(string xamlFilePath)
        where T : Window
        => LoadFromXaml<T>(new FileStream(xamlFilePath, FileMode.Open));

    public static T LoadFromXamlString<T>(string xaml)
        where T : Window
        => LoadFromXaml<T>(xaml.ToStream());

    public static T LoadFromXaml<T>(Stream stream)
        where T : Window
    {
        var xmlReader = XmlReader.Create(stream);
        return (T)XamlReader.Load(xmlReader);
    }
    #endregion

    protected virtual void Initialize()
    {
        Title = Name;

        Width = 800;
        Height = 600;
    }

    public CustomWindow()
    {
        Initialize();
    }

    public CustomWindow(LocaleService localeService)
    {
        _localeService = localeService;
        Initialize();
    }

}
