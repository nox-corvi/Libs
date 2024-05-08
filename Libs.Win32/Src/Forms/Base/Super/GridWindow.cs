using Nox.Win32.Controls.Base.Super;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;


namespace Nox.Win32.Forms.Base.Super;

public partial class GridWindow(LocaleService LocaleService = null)
    : Dooper.CustomWindow(LocaleService)
{
    private int _RowCount = 3;
    private int _ColCount = 3;

    private XGrid _grid;

    #region Properties
    public int RowCount
    {
        get => _RowCount;
        set
        {
            if (_RowCount != value)
            {
                _RowCount = value;
            }
        }
    }

    public int ColCount
    {
        get => _ColCount;
        set
        {
            if (_ColCount != value)
            {
                _ColCount = value;
            }
        }
    }

    public XGrid Grid { get => _grid; }
    #endregion

    public void SetRowColCount(int RowCount, int ColCount)
    {
        _RowCount = RowCount;
        _ColCount = ColCount;

        UpdateGrid();
        AssignControls();
    }

    public void SetItem(int Row, int Col, Control control)
    {
        XGrid.SetRow(control, Row); // Setzt die Reihe des Textblocks
        XGrid.SetColumn(control, Col); // Setzt die Spalte des Textblocks

        _grid.Children.Add(control);
    }

    /// <summary>
    /// Wird aufgerufen, nachdem das Grid neu strukturiert wurde, 
    /// damit die bestehenden Controls dem Grid neu zugeordnet werden können
    /// </summary>
    public virtual void AssignControls()
    {

    }

    public virtual void UpdateGrid()
    {
        _grid.ColumnDefinitions.Clear();
        _grid.RowDefinitions.Clear();

        for (int i = 0; i < _ColCount; i++)
            _grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < _RowCount; i++)
            _grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
    }

    protected override void Initialize()
    {
        base.Initialize();

        _grid = new XGrid();

        UpdateGrid();
        AssignControls();

        this.Content = _grid;
    }
}
