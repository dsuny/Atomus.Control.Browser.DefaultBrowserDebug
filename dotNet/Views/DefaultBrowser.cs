using Atomus.Control.Login.Controllers;
using Atomus.Control.Login.Models;
using Atomus.Control.Menu.Controllers;
using Atomus.Control.Menu.Models;
using Atomus.Diagnostics;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Atomus.Control.Browser
{
    public partial class DefaultBrowser : Form, IAction
    {
        private IAction _UserControl;
        private IAction _ToolbarControl;
        private AtomusControlEventHandler _BeforeActionEventHandler;
        private AtomusControlEventHandler _AfterActionEventHandler;
        public bool IsDebug { get; set; } = true;

        #region Init
        public DefaultBrowser()
        {
            string _SkinName;
            Color _Color;

            InitializeComponent();

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.IsMdiContainer = false;

            this.Translator().TargetCultureName = "ko-KR";

            _SkinName = this.GetAttribute("SkinName1");

            if (_SkinName != null)
            {
                Config.Client.SetAttribute("SkinName", _SkinName);

                _Color = this.GetAttributeColor(_SkinName + ".BackColor");
                if (_Color != null)
                    this.BackColor = _Color;

                _Color = this.GetAttributeColor(_SkinName + ".ForeColor");
                if (_Color != null)
                    this.ForeColor = _Color;
            }

            //this.TransparencyKey = Color.Magenta;

            this.FormClosing += new FormClosingEventHandler(this.DefaultBrowser_FormClosing);
        }
        #endregion

        #region IO
        object IAction.ControlAction(ICore sender, AtomusControlArgs e)
        {
            try
            {
                this._BeforeActionEventHandler?.Invoke(this, e);

                switch (e.Action)
                {
                    default:
                        throw new AtomusException("'{0}'은 처리할 수 없는 Action 입니다.".Translate(e.Action));
                }
            }
            finally
            {
                this._AfterActionEventHandler?.Invoke(this, e);
            }
        }
        
        private void ToolbarControl_BeforeActionEventHandler(ICore sender, AtomusControlEventArgs e) { }
        private void ToolbarControl_AfterActionEventHandler(ICore sender, AtomusControlEventArgs e)
        {
            try
            {
                switch (e.Action)
                {
                    case "Close":
                        this.ApplicationExit();

                        break;

                    default:
                        if (!e.Action.StartsWith("Action."))
                            this._UserControl.ControlAction(sender, new AtomusControlArgs(e.Action, e.Value));
                        break;
                }
            }
            catch (Exception _Exception)
            {
                this.MessageBoxShow(this, _Exception);
            }
        }

        public void AddUserControl(DefaultLoginSearch _DefaultLoginSearch, DefaultMenuSearch _DefaultMenuSearch, IAction _UserControl)
        {
            this.Login(_DefaultLoginSearch);

            this._UserControl = _UserControl;

            this.OpenControl(_DefaultMenuSearch, this._UserControl);
        }

        private bool Login(DefaultLoginSearch _DefaultLoginSearch)
        {
            Service.IResponse _Result;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                _Result = this.Search(_DefaultLoginSearch);

                if (_Result.Status == Service.Status.OK)
                {
                    if (_Result.DataSet != null && _Result.DataSet.Tables.Count >= 1)
                        foreach (DataTable _DataTable in _Result.DataSet.Tables)
                            for (int i = 1; i < _DataTable.Columns.Count; i++)
                                foreach (DataRow _DataRow in _DataTable.Rows)
                                    Config.Client.SetAttribute(string.Format("{0}.{1}", _DataRow[0].ToString(), _DataTable.Columns[i].ColumnName), _DataRow[i]);


                    return true;
                }
                else
                {
                    this.MessageBoxShow(this, _Result.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            catch (Exception _Exception)
            {
                this.MessageBoxShow(this, _Exception);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            return false;
        }

        private async void OpenControl(DefaultMenuSearch _DefaultMenuSearch, IAction _UserControl)
        {
            Service.IResponse _Result;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                _Result = await this.SearchOpenControl(_DefaultMenuSearch);

                if (_Result.Status == Service.Status.OK)
                {
                    if (_Result.DataSet.Tables.Count == 2)
                        if (_Result.DataSet.Tables[0].Rows.Count == 1)
                        {
                            Config.Client.SetAttribute(_UserControl, "MENU_ID", _DefaultMenuSearch.MENU_ID);

                            foreach (DataRow _DataRow in _Result.DataSet.Tables[1].Rows)
                            {
                                Config.Client.SetAttribute(_UserControl, _DataRow["ATTRIBUTE_NAME"].ToString(), _DataRow["ATTRIBUTE_VALUE"]);
                            }


                            this.Controls.Add((UserControl)_UserControl);

                            ((UserControl)_UserControl).Dock = DockStyle.Fill;

                            this.ToolbarControlSetup((IAction)_UserControl);
                        }
                }
                else
                {
                    this.MessageBoxShow(this, _Result.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception _Exception)
            {
                this.MessageBoxShow(this, _Exception);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        #endregion

        #region Event
        event AtomusControlEventHandler IAction.BeforeActionEventHandler
        {
            add
            {
                this._BeforeActionEventHandler += value;
            }
            remove
            {
                this._BeforeActionEventHandler -= value;
            }
        }
        event AtomusControlEventHandler IAction.AfterActionEventHandler
        {
            add
            {
                this._AfterActionEventHandler += value;
            }
            remove
            {
                this._AfterActionEventHandler -= value;
            }
        }

        private void DefaultBrowser_Load(object sender, EventArgs e)
        {
            try
            {
#if DEBUG
                DiagnosticsTool.MyDebug(string.Format("DefaultBrowser_Load(object sender = {0}, EventArgs e = {1})", (sender != null) ? sender.ToString() : "null", (e != null) ? e.ToString() : "null"));
#endif

                this.Text = Factory.FactoryConfig.GetAttribute("Atomus", "ProjectName");
            }
            //catch (AtomusException _Exception)
            //{
            //    this.MessageBoxShow(this, _Exception);
            //    Application.Exit();
            //}
            //catch (TypeInitializationException _Exception)
            //{
            //    this.MessageBoxShow(this, _Exception);
            //    Application.Exit();
            //}
            catch (Exception _Exception)
            {
                this.MessageBoxShow(this, _Exception);
                Application.Exit();
            }

            try
            {
                this.WindowState = FormWindowState.Maximized;
                this.SetToolbar();

                _BeforeActionEventHandler.Invoke(this, null);

                //DebugUserControl.AddUserControl(this);
            }
            catch (Exception _Exception)
            {
                this.MessageBoxShow(this, _Exception);
                Application.Exit();
            }
        }

        private void ToolbarControlSetup(IAction _UserControl)
        {
            AtomusControlArgs _AtomusControlArgs;

            try
            {
                _AtomusControlArgs = new AtomusControlArgs();
                _AtomusControlArgs.Action = "Action.New";
                _AtomusControlArgs.Value = _UserControl.GetAttribute(_AtomusControlArgs.Action);

                if (_AtomusControlArgs.Value == null)
                    _AtomusControlArgs.Value = "Y";

                this._ToolbarControl.ControlAction(_UserControl, _AtomusControlArgs);


                _AtomusControlArgs.Action = "Action.Search";
                _AtomusControlArgs.Value = _UserControl.GetAttribute(_AtomusControlArgs.Action);

                if (_AtomusControlArgs.Value == null)
                    _AtomusControlArgs.Value = "Y";

                this._ToolbarControl.ControlAction(_UserControl, _AtomusControlArgs);


                _AtomusControlArgs.Action = "Action.Save";
                _AtomusControlArgs.Value = _UserControl.GetAttribute(_AtomusControlArgs.Action);

                if (_AtomusControlArgs.Value == null)
                    _AtomusControlArgs.Value = "Y";

                this._ToolbarControl.ControlAction(_UserControl, _AtomusControlArgs);


                _AtomusControlArgs.Action = "Action.Delete";
                _AtomusControlArgs.Value = _UserControl.GetAttribute(_AtomusControlArgs.Action);

                if (_AtomusControlArgs.Value == null)
                    _AtomusControlArgs.Value = "Y";

                this._ToolbarControl.ControlAction(_UserControl, _AtomusControlArgs);


                _AtomusControlArgs.Action = "Action.Print";
                _AtomusControlArgs.Value = _UserControl.GetAttribute(_AtomusControlArgs.Action);

                if (_AtomusControlArgs.Value == null)
                    _AtomusControlArgs.Value = "Y";

                this._ToolbarControl.ControlAction(_UserControl, _AtomusControlArgs);

                ((UserControl)_UserControl).BringToFront();
            }
            catch (Exception _Exception)
            {
                this.MessageBoxShow(this, _Exception);
            }
        }

        private void DefaultBrowser_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F4)
            {
                try
                {
                    this.ToolbarControl_AfterActionEventHandler(_ToolbarControl, new AtomusControlArgs("Close", null));
                    //((IAction)this).ControlAction(_ToolbarControl, ));
                }
                catch (Exception _Exception)
                {
                    this.MessageBoxShow(this, _Exception);
                }
            }

#if DEBUG
            if (e.Control && e.Shift && e.KeyCode == Keys.D)
            {
                DiagnosticsTool.ShowForm();
            }
#endif

            if (e.Control && e.Shift && e.KeyCode == Keys.T)
            {
                DiagnosticsTool.ShowForm();
            }
        }

        private void DefaultBrowser_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.MessageBoxShow(this, "종료하시겠습니까?", "Question", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                e.Cancel = true;
        }
        #endregion

        #region "ETC"
        private void SetToolbar()
        {
            UserControl _UserControl;

            try
            {
                this._ToolbarControl = (IAction)this.CreateInstance("Toolbar");
                this._ToolbarControl.BeforeActionEventHandler += ToolbarControl_BeforeActionEventHandler;
                this._ToolbarControl.AfterActionEventHandler += ToolbarControl_AfterActionEventHandler;

                _UserControl = (UserControl)this._ToolbarControl;
                _UserControl.Dock = DockStyle.Top;

                this.Controls.Add((UserControl)this._ToolbarControl);
            }
            catch (Exception _Exception)
            {
                this.MessageBoxShow(this, _Exception);
            }
        }

        private bool ApplicationExit()
        {
            if (this.MessageBoxShow(this, "종료하시겠습니까?", "Question", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                Application.Exit();
                return true;
            }
            else
                return false;
        }

        private void DebugStart()
        {
            if (!DiagnosticsTool.IsStart)
            {
                DiagnosticsTool.Mode = Mode.DebugToTextBox | Mode.DebugToFile;
                DiagnosticsTool.TextBoxBase = new RichTextBox();
                DiagnosticsTool.Start();
            }
        }

        private void TraceStart()
        {
            if (!DiagnosticsTool.IsStart)
            {
                DiagnosticsTool.Mode = Mode.TraceToTextBox | Mode.TraceToFile;
                DiagnosticsTool.TextBoxBase = new RichTextBox();
                DiagnosticsTool.Start();
            }
        }
        #endregion
    }
}