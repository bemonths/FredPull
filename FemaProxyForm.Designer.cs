namespace FredPull;

partial class FemaProxyForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _lblInfo = new Label();
        _lblAddress = new Label();
        _txtAddress = new TextBox();
        _lblUser = new Label();
        _txtUser = new TextBox();
        _lblPassword = new Label();
        _txtPassword = new TextBox();
        _btnTest = new Button();
        _txtResult = new TextBox();
        _btnSave = new Button();
        _btnCancel = new Button();
        SuspendLayout();
        //
        // _lblInfo
        //
        _lblInfo.Location = new Point(12, 9);
        _lblInfo.Name = "_lblInfo";
        _lblInfo.Size = new Size(536, 48);
        _lblInfo.TabIndex = 0;
        _lblInfo.Text = "Proxy yalnızca FEMA'nın resmî sel haritası servisine (hazards.fema.gov) giden isteklerde kullanılır; Census, CMS, OpenStreetMap ve Redfin doğrudan bağlanır. Değerler yalnızca exe'nin yanındaki fredpull_settings.json'da durur; parola Windows hesabınla şifrelenir, günlüğe yazılmaz.";
        //
        // _lblAddress
        //
        _lblAddress.AutoSize = true;
        _lblAddress.Location = new Point(12, 68);
        _lblAddress.Name = "_lblAddress";
        _lblAddress.Size = new Size(46, 15);
        _lblAddress.TabIndex = 1;
        _lblAddress.Text = "Adres:";
        //
        // _txtAddress
        //
        _txtAddress.Location = new Point(130, 65);
        _txtAddress.Name = "_txtAddress";
        _txtAddress.PlaceholderText = "http://host:port ya da socks5://host:port (boş = proxy yok)";
        _txtAddress.Size = new Size(418, 23);
        _txtAddress.TabIndex = 2;
        //
        // _lblUser
        //
        _lblUser.AutoSize = true;
        _lblUser.Location = new Point(12, 97);
        _lblUser.Name = "_lblUser";
        _lblUser.Size = new Size(76, 15);
        _lblUser.TabIndex = 3;
        _lblUser.Text = "Kullanıcı adı:";
        //
        // _txtUser
        //
        _txtUser.Location = new Point(130, 94);
        _txtUser.Name = "_txtUser";
        _txtUser.PlaceholderText = "isteğe bağlı";
        _txtUser.Size = new Size(200, 23);
        _txtUser.TabIndex = 4;
        //
        // _lblPassword
        //
        _lblPassword.AutoSize = true;
        _lblPassword.Location = new Point(12, 126);
        _lblPassword.Name = "_lblPassword";
        _lblPassword.Size = new Size(41, 15);
        _lblPassword.TabIndex = 5;
        _lblPassword.Text = "Parola:";
        //
        // _txtPassword
        //
        _txtPassword.Location = new Point(130, 123);
        _txtPassword.Name = "_txtPassword";
        _txtPassword.PlaceholderText = "isteğe bağlı";
        _txtPassword.Size = new Size(200, 23);
        _txtPassword.TabIndex = 6;
        _txtPassword.UseSystemPasswordChar = true;
        //
        // _btnTest
        //
        _btnTest.AutoSize = true;
        _btnTest.Location = new Point(12, 158);
        _btnTest.Name = "_btnTest";
        _btnTest.Size = new Size(110, 25);
        _btnTest.TabIndex = 7;
        _btnTest.Text = "Proxy'yi dene";
        _btnTest.UseVisualStyleBackColor = true;
        _btnTest.Click += BtnTest_Click;
        //
        // _txtResult
        //
        _txtResult.Location = new Point(12, 190);
        _txtResult.Multiline = true;
        _txtResult.Name = "_txtResult";
        _txtResult.ReadOnly = true;
        _txtResult.ScrollBars = ScrollBars.Vertical;
        _txtResult.Size = new Size(536, 110);
        _txtResult.TabIndex = 8;
        //
        // _btnSave
        //
        _btnSave.Location = new Point(362, 312);
        _btnSave.Name = "_btnSave";
        _btnSave.Size = new Size(90, 27);
        _btnSave.TabIndex = 9;
        _btnSave.Text = "Kaydet";
        _btnSave.UseVisualStyleBackColor = true;
        _btnSave.Click += BtnSave_Click;
        //
        // _btnCancel
        //
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Location = new Point(458, 312);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new Size(90, 27);
        _btnCancel.TabIndex = 10;
        _btnCancel.Text = "İptal";
        _btnCancel.UseVisualStyleBackColor = true;
        //
        // FemaProxyForm
        //
        AcceptButton = _btnSave;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = _btnCancel;
        ClientSize = new Size(560, 351);
        Controls.Add(_lblInfo);
        Controls.Add(_lblAddress);
        Controls.Add(_txtAddress);
        Controls.Add(_lblUser);
        Controls.Add(_txtUser);
        Controls.Add(_lblPassword);
        Controls.Add(_txtPassword);
        Controls.Add(_btnTest);
        Controls.Add(_txtResult);
        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "FemaProxyForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "FEMA proxy";
        FormClosing += FemaProxyForm_FormClosing;
        ResumeLayout(false);
        PerformLayout();
    }

    private Label _lblInfo;
    private Label _lblAddress;
    private TextBox _txtAddress;
    private Label _lblUser;
    private TextBox _txtUser;
    private Label _lblPassword;
    private TextBox _txtPassword;
    private Button _btnTest;
    private TextBox _txtResult;
    private Button _btnSave;
    private Button _btnCancel;
}
