using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Security.Policy;
using System.Text;
using System.Windows.Forms;

namespace Nox.Win32.Forms.Base
{
    public partial class FormDialog : Super.FormSuper
    {
        public EventHandler<EventArgs> ButtonOk;
        public EventHandler<EventArgs> ButtonCancel;
        public EventHandler<EventArgs> ButtonAbort;
        public EventHandler<EventArgs> ButtonRetry;
        public EventHandler<EventArgs> ButtonIgnore;
        public EventHandler<EventArgs> ButtonYes;
        public EventHandler<EventArgs> ButtonNo;
        public EventHandler<EventArgs> ButtonTryAgain;
        public EventHandler<EventArgs> ButtonContinue;

        private MessageBoxButtons _dialogButtons;
        private int _buttonCount = 1;

        private MessageBoxDefaultButton _defaultButton = MessageBoxDefaultButton.Button1;

        public MessageBoxButtons DialogButton
        {
            get => _dialogButtons;
            set
            {
                switch (_dialogButtons = value)
                {
                    case MessageBoxButtons.OK:
                        ButtonCount = 1;
                        ButtonText("&Ok");

                        _defaultButton = MessageBoxDefaultButton.Button1;
                        AcceptButton = CancelButton = button1;

                        break;
                    case MessageBoxButtons.OKCancel:
                        ButtonCount = 2;
                        ButtonText("&Ok", "&Cancel");

                        _defaultButton = MessageBoxDefaultButton.Button1;
                        AcceptButton = button1;
                        CancelButton = button2;

                        break;
                    case MessageBoxButtons.AbortRetryIgnore:
                        ButtonCount = 3;
                        ButtonText("&Abort", "&Retry", "&Ignore");

                        _defaultButton = MessageBoxDefaultButton.Button2;
                        AcceptButton = button2;
                        CancelButton = button1;

                        break;
                    case MessageBoxButtons.YesNoCancel:
                        ButtonCount = 3;
                        ButtonText("&Yes", "&No", "&Cancel");

                        _defaultButton = MessageBoxDefaultButton.Button1;
                        AcceptButton = button1;
                        CancelButton = button3;

                        break;
                    case MessageBoxButtons.YesNo:
                        ButtonCount = 2;
                        ButtonText("&Yes", "&No");

                        _defaultButton = MessageBoxDefaultButton.Button1;
                        AcceptButton = button1;
                        CancelButton = button2;

                        break;
                    case MessageBoxButtons.RetryCancel:
                        ButtonCount = 2;
                        ButtonText("&Retry", "&Cancel");

                        _defaultButton = MessageBoxDefaultButton.Button1;
                        AcceptButton = button1;
                        CancelButton = button2;
                        break;
                    case MessageBoxButtons.CancelTryContinue:
                        ButtonCount = 3;
                        ButtonText("&Cancel", "Try &again", "&Continue");

                        _defaultButton = MessageBoxDefaultButton.Button2;
                        AcceptButton = button2;
                        CancelButton = button1;
                        break;
                }
            }
        }

        private Button ChooseButton(MessageBoxDefaultButton button)
        {
            switch (button)
            {
                case MessageBoxDefaultButton.Button1:
                    return button1;
                case MessageBoxDefaultButton.Button2:
                    if (ButtonCount > 1) // only if 3 buttons available
                        return button2;

                    break;
                case MessageBoxDefaultButton.Button3:
                    if (ButtonCount > 2) // only if 3 buttons available
                        return button3;

                    break;
                case MessageBoxDefaultButton.Button4:
                    // ignore 
                    return null;
            }

            return null;
        }

        public MessageBoxDefaultButton DefaultButton
        {
            get => _defaultButton;
            set => AcceptButton = ChooseButton(_defaultButton = value);
        }

        private int ButtonCount
        {
            get => _buttonCount;
            set
            {
                _buttonCount = value;

                button1.Visible = button1.Enabled = _buttonCount > 0;
                button2.Visible = button2.Enabled = _buttonCount > 1;
                button3.Visible = button3.Enabled = _buttonCount > 2;
            }
        }

        
        private void ButtonText(params string[] text)
        {
            for (int i = 0; i < text.Length; i++)
                switch (i)
                {
                    case 0:
                        button1.Text = text[i];
                        break;
                    case 1:
                        button2.Text = text[i];
                        break;
                    case 2:
                        button3.Text = text[i];
                        break;
                }

            Align();
        }

        private void Align()
        {
            SuspendLayout();

            var t = (this.ClientRectangle.Height - button1.Height - DefaultMargin.Size.Height);


            button3.Location = new Point(ClientRectangle.Width - (button3.Width + DefaultMargin.Size.Width), t);

            if (ButtonCount < 3)
                button2.Location = new Point(ClientRectangle.Width - (button2.Width + DefaultMargin.Size.Width), t);
            else
                button2.Location = new Point(button3.Left - (button3.Width + DefaultMargin.Size.Width), t);

            if (ButtonCount < 2)
                button1.Location = new Point(ClientRectangle.Width - (button1.Width + DefaultMargin.Size.Width), t);
            else
                button1.Location = new Point(button2.Left - (button1.Width + DefaultMargin.Size.Width), t);

            ResumeLayout();
        }

        public FormDialog()
        {
            InitializeComponent();

            button1.Click += (object sender, EventArgs e) =>
            {
                switch (DialogButton)
                {
                    case MessageBoxButtons.OK:
                    case MessageBoxButtons.OKCancel:
                        ButtonOk?.Invoke(sender, e);
                        break;
                    case MessageBoxButtons.AbortRetryIgnore:
                        ButtonAbort?.Invoke(sender, e);
                        break;
                    case MessageBoxButtons.YesNo:
                    case MessageBoxButtons.YesNoCancel:
                        ButtonYes?.Invoke(sender, e);
                        break;
                    case MessageBoxButtons.RetryCancel:
                        ButtonRetry?.Invoke(sender, e);
                        break;
                    case MessageBoxButtons.CancelTryContinue:
                        ButtonCancel?.Invoke(sender, e);
                        break;
                }
            };
            button2.Click += (object sender, EventArgs e) =>
            {
                switch (DialogButton)
                {
                    case MessageBoxButtons.OKCancel:
                    case MessageBoxButtons.RetryCancel:
                        ButtonCancel?.Invoke(sender, e);
                        break;
                    case MessageBoxButtons.AbortRetryIgnore:
                        ButtonRetry?.Invoke(sender, e);
                        break;
                    case MessageBoxButtons.YesNo:
                    case MessageBoxButtons.YesNoCancel:
                        ButtonNo?.Invoke(sender, e);
                        break;
                    case MessageBoxButtons.CancelTryContinue:
                        ButtonTryAgain?.Invoke(sender, e);
                        break;
                }
            };
            button3.Click += (object sender, EventArgs e) =>
            {
                switch (DialogButton)
                {
                    case MessageBoxButtons.AbortRetryIgnore:
                        ButtonIgnore?.Invoke(sender, e);
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        ButtonCancel?.Invoke(sender, e);
                        break;
                    case MessageBoxButtons.CancelTryContinue:
                        ButtonContinue?.Invoke(sender, e);
                        break;
                }
            };

            Align();
        }

        private void FormDialog_Load(object sender, EventArgs e) =>
            Align();

        private void FormDialog_Resize(object sender, EventArgs e) =>
            Align();

    }
}
