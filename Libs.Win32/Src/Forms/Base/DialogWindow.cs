using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Nox.Win32.Controls;

namespace Nox.Win32.Forms.Base;

public class DialogWindow 
    : Super.CustomWindow
{
    private Controls.Base.Super.XButton button1 = new();
    private Controls.Base.Super.XButton button2 = new();
    private Controls.Base.Super.XButton button3 = new();

    public EventHandler<EventArgs> ButtonOk;
    public EventHandler<EventArgs> ButtonCancel;
    public EventHandler<EventArgs> ButtonYes;
    public EventHandler<EventArgs> ButtonNo;

    private MessageBoxButton _dialogButtons;
    private int _buttonCount = 1;
    private int _defaultButton = 1;

    private void SetDefaultButton(int index)
    {
        button1.IsDefault = (index == 1);
        button2.IsDefault = (index == 2);
        button3.IsDefault = (index == 3);
    }
    private void SetCancelButton(int index)
    {
        button1.IsCancel = (index == 1);
        button2.IsCancel = (index == 2);
        button3.IsCancel = (index == 3);
    }

    public MessageBoxButton DialogButton
    {
        get => _dialogButtons;
        set
        {
            switch (_dialogButtons = value)
            {
                case MessageBoxButton.OK:
                    ButtonCount = 1;
                    ButtonText("&Ok");

                    //_defaultButton = MessageBoxDefaultButton.Button1;
                    button1.IsDefault = true;

                    break;
                case MessageBoxButton.OKCancel:
                    ButtonCount = 2;
                    ButtonText("&Ok", "&Cancel");

                    //_defaultButton = MessageBoxDefaultButton.Button1;
                    SetDefaultButton(1);
                    SetCancelButton(2);

                    break;
                case MessageBoxButton.YesNoCancel:
                    ButtonCount = 3;
                    ButtonText("&Yes", "&No", "&Cancel");

                    SetDefaultButton(1);
                    SetCancelButton(3);

                    break;
                case MessageBoxButton.YesNo:
                    ButtonCount = 2;
                    ButtonText("&Yes", "&No");

                    SetDefaultButton(1);
                    SetCancelButton(2);

                    break;
            }
        }
    }

    private Controls.Base.Super.XButton ChooseButton(int DefaultButton)
    {
        switch (DefaultButton)
        {
            case 1:
                return button1;
            case 2:
                if (ButtonCount > 1) // only if 3 buttons available
                    return button2;

                break;
            case 3:
                if (ButtonCount > 2) // only if 3 buttons available
                    return button3;

                break;
        }

        return null;
    }

    public int DefaultButton
    {
        get => _defaultButton;
        set => SetDefaultButton(_defaultButton = value);
    }

    private int ButtonCount
    {
        get => _buttonCount;
        set
        {
            _buttonCount = value;

            button1.Visibility = (button1.IsEnabled = _buttonCount > 0) ? Visibility.Visible : Visibility.Hidden;
            button2.Visibility = (button2.IsEnabled = _buttonCount > 1) ? Visibility.Visible : Visibility.Hidden;
            button3.Visibility = (button3.IsEnabled = _buttonCount > 2) ? Visibility.Visible : Visibility.Hidden;
        }
    }


    private void ButtonText(params string[] text)
    {
        for (int i = 0; i < text.Length; i++)
            switch (i)
            {
                case 0:
                    button1.Content = text[i];
                    break;
                case 1:
                    button2.Content = text[i];
                    break;
                case 2:
                    button3.Content = text[i];
                    break;
            }

        Align();
    }

    private void Align()
    {
        //var t = (Height - button1.Height - (Margin.Bottom - Margin.Top));

        //button3. Location = new Point(ClientRectangle.Width - (button3.Width + DefaultMargin.Size.Width), t);

        //if (ButtonCount < 3)
        //    button2.Location = new Point(ClientRectangle.Width - (button2.Width + DefaultMargin.Size.Width), t);
        //else
        //    button2.Location = new Point(button3.Left - (button3.Width + DefaultMargin.Size.Width), t);

        //if (ButtonCount < 2)
        //    button1.Location = new Point(ClientRectangle.Width - (button1.Width + DefaultMargin.Size.Width), t);
        //else
        //    button1.Location = new Point(button2.Left - (button1.Width + DefaultMargin.Size.Width), t);

    }

    public DialogWindow()
        : base()
    {
        button1.Click += (object sender, RoutedEventArgs e) =>
        {
            switch (DialogButton)
            {
                case MessageBoxButton.OK:
                case MessageBoxButton.OKCancel:
                    ButtonOk?.Invoke(sender, e);
                    break;
                case MessageBoxButton.YesNo:
                case MessageBoxButton.YesNoCancel:
                    ButtonYes?.Invoke(sender, e);
                    break;
            }
        };
        button2.Click += (object sender, RoutedEventArgs e) =>
        {
            switch (DialogButton)
            {
                case MessageBoxButton.OKCancel:
                    ButtonCancel?.Invoke(sender, e);
                    break;
                case MessageBoxButton.YesNo:
                case MessageBoxButton.YesNoCancel:
                    ButtonNo?.Invoke(sender, e);
                    break;
            }
        };
        button3.Click += (object sender, RoutedEventArgs e) =>
        {
            switch (DialogButton)
            {
                case MessageBoxButton.YesNoCancel:
                    ButtonCancel?.Invoke(sender, e);
                    break;
            }
        };

        StackPanel panel = new StackPanel();
        panel.Children.Add(button1);
        panel.Children.Add(button2);
        panel.Children.Add(button3);

        this.Content = panel;

        DialogButton = MessageBoxButton.OKCancel;

        Align();
    }

    private void FormDialog_Load(object sender, EventArgs e) =>
        Align();

    private void FormDialog_Resize(object sender, EventArgs e) =>
        Align();
}
