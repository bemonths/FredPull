namespace FredPull;

/// FEMA proxy ayarı: adres, isteğe bağlı kullanıcı adı ve parola, "Proxy'yi dene". Kaydedilen değerleri ana form
/// fredpull_settings.json'a yazar (parola DPAPI ile şifreli). Adres boşsa proxy kullanılmaz.
public partial class FemaProxyForm : Form
{
    CancellationTokenSource? _cts;

    public FemaProxyForm()
    {
        InitializeComponent();
    }

    public FemaProxyForm(string? address, string? user, string? password) : this()
    {
        _txtAddress.Text = address ?? "";
        _txtUser.Text = user ?? "";
        _txtPassword.Text = password ?? "";
    }

    public string Address => _txtAddress.Text.Trim();
    public string User => _txtUser.Text.Trim();
    public string Password => _txtPassword.Text;

    /// Kutulardaki değerlerle proxy ayarı; adres boşsa null.
    public ExtraData.ProxySettings? Current =>
        Address.Length == 0 ? null : new ExtraData.ProxySettings(Address, User.Length > 0 ? User : null, Password.Length > 0 ? Password : null);

    async void BtnTest_Click(object? sender, EventArgs e)
    {
        if (Current is not { } p) { _txtResult.Text = "Önce proxy adresini yaz."; return; }
        if (ExtraData.ProxySettings.Validate(p.Address) is { } err) { _txtResult.Text = err; return; }
        _btnTest.Enabled = false;
        _txtResult.Text = "Deneniyor…";
        _cts = new CancellationTokenSource();
        try
        {
            var r = await ExtraData.TestFemaProxyAsync(p, _cts.Token);
            _txtResult.Text = r.Text;
        }
        catch (OperationCanceledException) { }
        finally
        {
            _btnTest.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    void BtnSave_Click(object? sender, EventArgs e)
    {
        if (Address.Length > 0 && ExtraData.ProxySettings.Validate(Address) is { } err)
        {
            MessageBox.Show(err, "FEMA proxy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }

    void FemaProxyForm_FormClosing(object? sender, FormClosingEventArgs e) => _cts?.Cancel();
}
