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
    public partial class DefaultBrowserDebug : Form, IAction
    {
        private IAction toolbarControl;
        private TabControl tabControl;
        private UserControl browserViewer;
        private AtomusControlEventHandler beforeActionEventHandler;
        private AtomusControlEventHandler afterActionEventHandler;

        private string menuDatabaseName;
        private string menuProcedureID;

        #region Init
        public DefaultBrowserDebug()
        {
            string skinName;
            Color color;

            InitializeComponent();

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.IsMdiContainer = false;

            this.Translator().TargetCultureName = "ko-KR";

            skinName = this.GetAttribute("SkinName");

            if (skinName != null)
            {
                Config.Client.SetAttribute("SkinName", skinName);

                color = this.GetAttributeColor(skinName + ".BackColor");
                if (color != null)
                    this.BackColor = color;

                color = this.GetAttributeColor(skinName + ".ForeColor");
                if (color != null)
                    this.ForeColor = color;
            }

            //this.TransparencyKey = Color.Magenta;

            this.FormClosing += new FormClosingEventHandler(this.DefaultBrowser_FormClosing);
        }
        #endregion

        #region Dictionary
        #endregion

        #region Spread
        #endregion

        #region IO
        object IAction.ControlAction(ICore sender, AtomusControlArgs e)
        {
            IAction userControl;
            object[] objects;

            try
            {
                if (e.Action != "AddUserControl" && e.Action != "Login")
                    this.beforeActionEventHandler?.Invoke(this, e);

                switch (e.Action)
                {
                    case "Login":
                        objects = (object[])e.Value;

                        this.Login(new DefaultLoginSearchModel()
                        {
                            DatabaseName = (string)objects[0],
                            ProcedureID = (string)objects[1],
                            EMAIL = (string)objects[2],
                            ACCESS_NUMBER = (string)objects[3]
                        });
                        return true;

                    case "AddUserControl":
                        objects = (object[])e.Value;

                        this.menuDatabaseName = (string)objects[4];
                        this.menuProcedureID = (string)objects[5];

                        userControl = (IAction)objects[9];

                       this.OpenControl(
                             (string)objects[4],
                            (string)objects[5],
                            //MENU_NAME = (string)_object[6],
                            (decimal)objects[7],
                            (decimal)objects[8]
                        , userControl, null, null, true);

                        return true;

                    default:
                        throw new AtomusException("'{0}'은 처리할 수 없는 Action 입니다.".Translate(e.Action));
                }
            }
            finally
            {
                if (e.Action != "AddUserControl" && e.Action != "Login")
                    this.afterActionEventHandler?.Invoke(this, e);
            }
        }

        private void ToolbarControl_BeforeActionEventHandler(ICore sender, AtomusControlEventArgs e) { }
        private void ToolbarControl_AfterActionEventHandler(ICore sender, AtomusControlEventArgs e)
        {
            IAction action;
            TabPage tabPage;
            UserControl userControl;

            try
            {
                action = (IAction)this.tabControl.Tag;

                switch (e.Action)
                {
                    case "Close":
                        if (this.tabControl.TabPages.Count > 0)
                        {
                            tabPage = this.tabControl.SelectedTab;
                            this.tabControl.DeselectTab(tabPage);
                            userControl = (UserControl)action;
                            this.tabControl.TabPages.Remove(tabPage);
                            this.browserViewer.Controls.Remove(userControl);
                            userControl.Dispose();
                        }
                        else
                            this.ApplicationExit();

                        break;

                    default:
                        if (!e.Action.StartsWith("Action."))
                            action.ControlAction(sender, e.Action, e.Value);

                        //this.ControlActionHome(sender, e);
                        break;
                }
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
            }
        }

        private void UserControl_BeforeActionEventHandler(ICore sender, AtomusControlEventArgs e) { }
        private void UserControl_AfterActionEventHandler(ICore sender, AtomusControlEventArgs e)
        {
            object[] objects;

            try
            {
                switch (e.Action)
                {
                    case "UserControl.OpenControl" :
                        objects = (object[])e.Value;

                        //                             _MENU_ID, _ASSEMBLY_ID, sender, AtomusControlArgs
                        //_DatabaseName, _ProcedureID, _MENU_ID, _ASSEMBLY_ID, _Core, sender, AtomusControlArgs
                        this.OpenControl(this.menuDatabaseName, this.menuProcedureID,  (decimal)objects[0], (decimal)objects[1], null, sender, (objects[2] == null) ? null : (AtomusControlEventArgs)objects[2], true);

                        break;

                    case "UserControl.GetControl" :
                        objects = (object[])e.Value;//_MENU_ID, _ASSEMBLY_ID, sender, AtomusControlArgs

                        e.Value = this.OpenControl(this.menuDatabaseName, this.menuProcedureID, (decimal)objects[0], (decimal)objects[1], null, sender, (objects[2] == null) ? null : (AtomusControlEventArgs)objects[2], false);
                        break;

                        //default:
                        //    throw new AtomusException("'{0}'은 처리할 수 없는 Action 입니다.".Translate(e.Action));
                }
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
            }
        }

        private bool Login(DefaultLoginSearchModel defaultLoginSearch)
        {
            Service.IResponse result;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                result = this.Search(defaultLoginSearch);

                if (result.Status == Service.Status.OK)
                {
                    if (result.DataSet != null && result.DataSet.Tables.Count >= 1)
                        foreach (DataTable _DataTable in result.DataSet.Tables)
                            for (int i = 1; i < _DataTable.Columns.Count; i++)
                                foreach (DataRow _DataRow in _DataTable.Rows)
                                    Config.Client.SetAttribute(string.Format("{0}.{1}", _DataRow[0].ToString(), _DataTable.Columns[i].ColumnName), _DataRow[i]);


                    return true;
                }
                else
                {
                    this.MessageBoxShow(this, result.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            return false;
        }

        private ICore OpenControl(string databaseName, string procedureID, decimal MENU_ID, decimal ASSEMBLY_ID, IAction core, ICore sender, AtomusControlEventArgs atomusControlEventArgs, bool addTabControl)
        {
            Service.IResponse result;

            try
            {
                result = this.SearchOpenControl(new DefaultMenuSearchModel()
                {
                    DatabaseName = databaseName,
                    ProcedureID = procedureID,
                    MENU_ID = MENU_ID,
                    ASSEMBLY_ID = ASSEMBLY_ID
                });

                if (result.Status == Service.Status.OK)
                {
                    if (result.DataSet.Tables.Count == 2)
                        if (result.DataSet.Tables[0].Rows.Count == 1)
                        {
                            if (core == null)
                            {
                                if (result.DataSet.Tables[0].Columns.Contains("FILE_TEXT") && result.DataSet.Tables[0].Rows[0]["FILE_TEXT"] != DBNull.Value)
                                    core = (IAction)Factory.CreateInstance(Convert.FromBase64String((string)result.DataSet.Tables[0].Rows[0]["FILE_TEXT"]), result.DataSet.Tables[0].Rows[0]["NAMESPACE"].ToString(), false, false);
                                else
                                    core = (IAction)Factory.CreateInstance((byte[])result.DataSet.Tables[0].Rows[0]["FILE"], result.DataSet.Tables[0].Rows[0]["NAMESPACE"].ToString(), false, false);

                            }

                            core.BeforeActionEventHandler += UserControl_BeforeActionEventHandler;
                            core.AfterActionEventHandler += UserControl_AfterActionEventHandler;

                            core.SetAttribute("MENU_ID", MENU_ID.ToString());
                            core.SetAttribute("ASSEMBLY_ID", ASSEMBLY_ID.ToString());

                            foreach (DataRow _DataRow in result.DataSet.Tables[1].Rows)
                            {
                                core.SetAttribute(_DataRow["ATTRIBUTE_NAME"].ToString(), _DataRow["ATTRIBUTE_VALUE"].ToString());
                            }

                            if (addTabControl)
                                this.OpenControl((string)result.DataSet.Tables[0].Rows[0]["NAME"], string.Format("{0} {1}", result.DataSet.Tables[0].Rows[0]["DESCRIPTION"], core.GetType().Assembly.GetName().Version.ToString()), (UserControl)core);

                            if (atomusControlEventArgs != null)
                                core.ControlAction(sender, atomusControlEventArgs.Action, atomusControlEventArgs.Value);

                        }

                    return core;
                }
                else
                {
                    this.MessageBoxShow(this, result.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
                return null;
            }
            finally
            {
            }
        }
        private void OpenControl(string name, string DESCRIPTION, UserControl userControl)
        {
            TabPage _TabPage;

            try
            {
                userControl.Dock = DockStyle.Fill;

                _TabPage = new TabPage
                {
                    BackColor = Color.Transparent,
                    Text = name,
                    ToolTipText = DESCRIPTION,
                    Tag = userControl
                };

                this.tabControl.TabPages.Add(_TabPage);
                this.tabControl.Tag = userControl;
                this.tabControl.SelectedTab = _TabPage;

                this.TabControl_Selected(this.tabControl, null);
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
            }
        }
        #endregion

        #region Event
        event AtomusControlEventHandler IAction.BeforeActionEventHandler
        {
            add
            {
                this.beforeActionEventHandler += value;
            }
            remove
            {
                this.beforeActionEventHandler -= value;
            }
        }
        event AtomusControlEventHandler IAction.AfterActionEventHandler
        {
            add
            {
                this.afterActionEventHandler += value;
            }
            remove
            {
                this.afterActionEventHandler -= value;
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
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
                Application.Exit();
            }

            try
            {
                this.WindowState = FormWindowState.Maximized;

                beforeActionEventHandler.Invoke(this, new AtomusControlEventArgs() { Action = "Login" });

                this.SetBrowserViewer();

                this.SetTabControl();

                this.SetToolbar();

                beforeActionEventHandler.Invoke(this, new AtomusControlEventArgs() { Action = "AddUserControl" });
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
                Application.Exit();
            }
        }

        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {
            TabControl tabControl;
            UserControl userControl;
            ICore core;

            try
            {
                tabControl = (TabControl)sender;
                tabControl.Tag = tabControl.SelectedTab.Tag;
                userControl = (UserControl)tabControl.Tag;

                if (!this.browserViewer.Controls.Contains(userControl))
                    this.browserViewer.Controls.Add(userControl);

                core = (ICore)userControl;

                object _Value;

                _Value = core.GetAttribute("Action.New");
                this.toolbarControl.ControlAction(core, "Action.New", _Value ?? "Y");

                _Value = core.GetAttribute("Action.Search");
                this.toolbarControl.ControlAction(core, "Action.Search", _Value ?? "Y");

                _Value = core.GetAttribute("Action.Save");
                this.toolbarControl.ControlAction(core, "Action.Save", _Value ?? "Y");

                _Value = core.GetAttribute("Action.Delete");
                this.toolbarControl.ControlAction(core, "Action.Delete", _Value ?? "Y");

                _Value = core.GetAttribute("Action.Print");
                this.toolbarControl.ControlAction(core, "Action.Print", _Value ?? "Y");

                this.toolbarControl.ControlAction(core, "UserToolbarButton.Add", null);

                userControl.BringToFront();
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
            }
        }

        private void DefaultBrowser_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F4)
            {
                try
                {
                    this.ToolbarControl_AfterActionEventHandler(toolbarControl, new AtomusControlArgs("Close", null));
                    //((IAction)this).ControlAction(_ToolbarControl, ));
                }
                catch (Exception exception)
                {
                    this.MessageBoxShow(this, exception);
                }
            }

            if (e.Control && e.KeyCode == Keys.Tab && ActiveControl != this.tabControl)
            {
                if (this.tabControl.SelectedIndex + 1 == this.tabControl.TabCount)
                    this.tabControl.SelectedIndex = 0;
                else
                    this.tabControl.SelectedIndex += 1;
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
        private void SetBrowserViewer()
        {
            try
            {
                this.browserViewer = new UserControl
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                };
                this.Controls.Add(this.browserViewer);
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
            }
        }

        private void SetToolbar()
        {
            UserControl userControl;

            try
            {
                this.toolbarControl = (IAction)this.CreateInstance("Toolbar");
                //this.toolbarControl = new Toolbar.DefaultToolbar();
                this.toolbarControl.BeforeActionEventHandler += ToolbarControl_BeforeActionEventHandler;
                this.toolbarControl.AfterActionEventHandler += ToolbarControl_AfterActionEventHandler;

                userControl = (UserControl)this.toolbarControl;
                userControl.Dock = DockStyle.Top;

                this.Controls.Add((UserControl)this.toolbarControl);
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
            }
        }

        private void SetTabControl()
        {
            try
            {
                this.tabControl = new TabControl();
                this.tabControl.DoubleBuffered(true);
                this.tabControl.ShowToolTips = true;
                this.tabControl.BackColor = this.BackColor;
                this.tabControl.TabPages.Clear();
                this.tabControl.Dock = DockStyle.Top;
                this.tabControl.Height = 21;
                this.tabControl.HotTrack = true;
                this.tabControl.Selected += this.TabControl_Selected;
                this.Controls.Add(this.tabControl);
            }
            catch (Exception exception)
            {
                this.MessageBoxShow(this, exception);
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