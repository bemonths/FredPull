using System.Diagnostics;
using System.Globalization;

namespace FredPull;

/// Bir ilçenin ev kartı: adaylar, seçili adayın fiyat geçmişi ve grafiği, referans fotoğraflar ve dışa aktarımlar.
/// Arayüz HouseDetailForm.Designer.cs'te. Modal değil; farklı ilçeler için birden fazla açılabilir.
public partial class HouseDetailForm : Form
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    const double SqFtToM2 = 0.09290304;

    readonly MainForm _owner;
    readonly List<Image> _thumbImages = new();
    string? _selectedPhoto;

    public string Fips { get; }
    public string State { get; }

    public HouseDetailForm(MainForm owner, string fips)
    {
        InitializeComponent();
        _owner = owner;
        Fips = fips;
        State = owner.CardsState;
        FillAll(null);
    }

    /// Kart ana formdaki sözlükten her seferinde okunur (toplama sürerken güncellenmiş olabilir).
    HouseCard? Card => _owner.CardsState == State && _owner.Cards.TryGetValue(Fips, out var c) ? c : null;
    HouseCandidate? Current => _gridCands.CurrentRow?.Tag as HouseCandidate;

    // ---------- doldurma ----------

    void FillAll(string? keepUrl)
    {
        var card = Card;
        if (card == null)
        {
            _lblCounty.Text = "Ev kartı bulunamadı";
            _txtCard.Text = "Ana formda eyalet değişmiş ya da kart silinmiş olabilir; formu kapatıp yeniden aç.";
            return;
        }
        Text = $"Ev detayları — {card.County}";
        _lblCounty.Text = $"{card.County} — {card.Mode}, bant {card.MinPrice / 1000}k-{card.MaxPrice / 1000}k, " +
                          $"{card.CreatedAt:dd.MM.yyyy HH:mm}, listede {card.ListCount:N0} ilan";
        _txtCard.Text = card.Chosen != null ? $"{card.CardText}\n{card.Chosen.Url}" : card.Note;

        _filling = true;
        _gridCands.Rows.Clear();
        foreach (var a in card.Candidates)
        {
            RedfinListingPicker.FillPriceSteps(a);
            bool chosen = a.Url == card.Chosen?.Url;
            int i = _gridCands.Rows.Add(chosen ? "✓" : "", a.Street, a.City, a.Price, a.OriginalPrice, a.CurrentPrice,
                a.HasHistory ? a.Cuts.Count : null, a.HasHistory ? a.TotalCut : null, a.HasHistory ? a.Days : a.DaysOnRedfin,
                a.YearBuilt, a.SqFt is int s ? Math.Round(s * SqFtToM2) : null, a.Beds,
                a.LastSaleDate?.ToString("yyyy-MM-dd", Inv), a.LastSalePrice, a.PreviouslyWithdrawn ? "evet" : "", a.Lat, a.Lng);
            var row = _gridCands.Rows[i];
            row.Tag = a;
            if (chosen) row.DefaultCellStyle.Font = new Font(_gridCands.Font, FontStyle.Bold);
            if (a.Error != null) { row.DefaultCellStyle.ForeColor = Color.Gray; row.Cells[_cStreet.Index].ToolTipText = a.Error; }
        }

        var target = _gridCands.Rows.Cast<DataGridViewRow>()
            .FirstOrDefault(r => ((HouseCandidate)r.Tag!).Url == (keepUrl ?? card.Chosen?.Url)) ?? _gridCands.Rows.Cast<DataGridViewRow>().FirstOrDefault();
        if (target != null) _gridCands.CurrentCell = target.Cells[_cStreet.Index];
        _filling = false;
        ShowCandidate(Current);
    }

    bool _filling;      // tablo doldurulurken seçim olayı fotoğrafları boşuna yüklemesin

    void GridCands_CurrentCellChanged(object? sender, EventArgs e)
    {
        if (!_filling) ShowCandidate(Current);
    }

    void ShowCandidate(HouseCandidate? a)
    {
        var card = Card;
        _btnOpenRedfin.Enabled = a != null;
        _btnOpenMap.Enabled = a?.Lat != null && a.Lng != null;
        _btnChoose.Enabled = a != null && a.HasHistory && a.OriginalPrice.HasValue && a.CurrentPrice.HasValue && a.Url != card?.Chosen?.Url;
        _btnCopyCard.Enabled = card?.Chosen != null;
        _btnPhotos.Enabled = a != null;
        if (a == null) return;

        ShowHistory(a);
        ShowChart(a);
        ShowPhotos(a);
        _status.Text = a.Error != null ? $"{a.Street}: {a.Error}" : $"{a.Street}, {a.City} {a.Zip}";
    }

    void ShowHistory(HouseCandidate a)
    {
        _gridHistory.Rows.Clear();
        var rows = RedfinListingPicker.HistoryRows(a);

        // Fark: kira ve satış dışındaki fiyatlı kayıtlarda bir önceki (daha eski) istenen fiyata göre
        var diff = new Dictionary<int, int>();
        int? prev = null;
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            var r = rows[i];
            if (r.Rental || r.Sale || r.Price is not int p || p < 25_000) continue;
            if (prev.HasValue && r.Description != "Listed") diff[i] = p - prev.Value;
            prev = p;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            int k = _gridHistory.Rows.Add(r.Date.ToString("yyyy-MM-dd", Inv), r.Rental ? r.Description + " (kira)" : r.Description,
                r.Price, diff.TryGetValue(i, out var d) ? d : null);
            var row = _gridHistory.Rows[k];
            if (r.Rental) row.DefaultCellStyle.ForeColor = Color.Gray;
            else if (a.ListedDate.HasValue && r.Date >= a.ListedDate.Value) row.DefaultCellStyle.Font = new Font(_gridHistory.Font, FontStyle.Bold);
            if (diff.TryGetValue(i, out var dd) && dd < 0) row.Cells[_hDiff.Index].Style.ForeColor = Color.FromArgb(180, 20, 20);
        }
    }

    void ShowChart(HouseCandidate a)
    {
        var plt = _plot.Plot;
        plt.Clear();
        if (a.PriceSteps.Count > 0)
        {
            // Merdiven çizgi: her fiyat bir sonraki değişikliğe (sonuncusu bugüne) kadar sürer
            var xs = new List<double>();
            var ys = new List<double>();
            for (int i = 0; i < a.PriceSteps.Count; i++)
            {
                var s = a.PriceSteps[i];
                var until = i + 1 < a.PriceSteps.Count ? a.PriceSteps[i + 1].Date : DateOnly.FromDateTime(DateTime.Today);
                xs.Add(OA(s.Date)); ys.Add(s.Price);
                xs.Add(OA(until)); ys.Add(s.Price);
            }
            var sc = plt.Add.Scatter(xs.ToArray(), ys.ToArray());
            sc.LegendText = "İstenen fiyat";
            sc.LineWidth = 2.5f;
            sc.MarkerSize = 0;
            if (a.LastSalePrice is int buy)
            {
                var hl = plt.Add.HorizontalLine(buy);
                hl.LegendText = $"Alış {a.LastSaleDate?.ToString("yyyy-MM", Inv)} ({RedfinListingPicker.Usd(buy)} $)";
                hl.LinePattern = ScottPlot.LinePattern.Dashed;
            }
            plt.Axes.DateTimeTicksBottom();
            plt.Title($"{a.Street} — {a.Cuts.Count} indirim, {a.Days} gün");
            plt.YLabel("$");
            plt.ShowLegend(ScottPlot.Alignment.UpperRight);
        }
        else plt.Title("Fiyat geçmişi yok");
        _plot.Refresh();
    }

    static double OA(DateOnly d) => d.ToDateTime(TimeOnly.MinValue).ToOADate();

    // ---------- fotoğraflar ----------

    void ShowPhotos(HouseCandidate a)
    {
        _thumbs.SuspendLayout();
        foreach (var c in _thumbs.Controls.Cast<Control>().ToList()) c.Dispose();
        _thumbs.Controls.Clear();
        foreach (var img in _thumbImages) img.Dispose();
        _thumbImages.Clear();
        SetPreview(null);

        foreach (var rel in a.PhotoPaths)
        {
            var path = Path.Combine(_owner.OutDir, rel);
            if (!File.Exists(path) || LoadImage(path, 160, 120) is not { } thumb) continue;
            _thumbImages.Add(thumb);
            var pb = new PictureBox
            {
                Width = 160, Height = 120, SizeMode = PictureBoxSizeMode.Zoom, Image = thumb, Tag = path,
                Cursor = Cursors.Hand, Margin = new Padding(3), BackColor = SystemColors.ControlLight,
            };
            pb.Click += Thumb_Click;
            _thumbs.Controls.Add(pb);
        }
        _thumbs.ResumeLayout();

        int n = _thumbs.Controls.Count;
        _lblPhotos.Text = n > 0
            ? $"Referans fotoğraflar ({n}) — emlakçı/MLS telifli, videoda kullanılmaz"
            : a.PhotoUrls.Count > 0 ? $"{a.PhotoUrls.Count} fotoğraf adresi hazır — \"Fotoğrafları indir (referans)\""
                                    : "Fotoğraf yok — \"Fotoğrafları indir (referans)\" ilan sayfasından bulur";
        _btnShowFolder.Enabled = n > 0;
        if (n > 0) Thumb_Click(_thumbs.Controls[0], EventArgs.Empty);
    }

    void Thumb_Click(object? sender, EventArgs e)
    {
        if (sender is not PictureBox pb || pb.Tag is not string path) return;
        foreach (var c in _thumbs.Controls.OfType<PictureBox>()) c.BorderStyle = BorderStyle.None;
        pb.BorderStyle = BorderStyle.Fixed3D;
        _selectedPhoto = path;
        SetPreview(LoadImage(path, 1600, 1200));
    }

    void SetPreview(Image? img)
    {
        var old = _preview.Image;
        _preview.Image = img;
        old?.Dispose();
        if (img == null) _selectedPhoto = null;
    }

    /// Dosyayı kilitlemeden, en çok maxW x maxH boyutunda kopya olarak yükler.
    static Image? LoadImage(string path, int maxW, int maxH)
    {
        try
        {
            using var ms = new MemoryStream(File.ReadAllBytes(path));
            using var img = Image.FromStream(ms);
            double k = Math.Min(1.0, Math.Min((double)maxW / img.Width, (double)maxH / img.Height));
            return new Bitmap(img, Math.Max(1, (int)(img.Width * k)), Math.Max(1, (int)(img.Height * k)));
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException) { return null; }
    }

    // ---------- düğmeler ----------

    void BtnOpenRedfin_Click(object? sender, EventArgs e)
    {
        if (Current is { } a) Process.Start(new ProcessStartInfo(a.Url) { UseShellExecute = true });
    }

    void BtnOpenMap_Click(object? sender, EventArgs e)
    {
        if (Current is { Lat: double lat, Lng: double lng })
            Process.Start(new ProcessStartInfo($"https://www.google.com/maps/search/?api=1&query={lat.ToString("0.######", Inv)},{lng.ToString("0.######", Inv)}") { UseShellExecute = true });
    }

    void BtnChoose_Click(object? sender, EventArgs e)
    {
        if (Card is not { } card || Current is not { } a) return;
        if (!a.HasHistory || !a.OriginalPrice.HasValue || !a.CurrentPrice.HasValue)
        {
            MessageBox.Show("Bu adayın fiyat geçmişi okunamamış; kart metni üretilemez.", "Bu evi seç");
            return;
        }
        card.Chosen = a;
        card.CardText = RedfinListingPicker.CardText(a);
        card.Note = "";
        _owner.SaveCards();
        _owner.LogListings($"{card.County}: elle seçildi — {card.CardText}");
        FillAll(a.Url);
        _status.Text = $"Seçildi, kaydedildi: {card.CardText}";
    }

    void BtnCopyCard_Click(object? sender, EventArgs e)
    {
        if (Card is { Chosen: not null } card)
        {
            Clipboard.SetText(card.CardText);
            _status.Text = "Kart metni panoya kopyalandı.";
        }
    }

    async void BtnPhotos_Click(object? sender, EventArgs e)
    {
        if (Card is not { } card || Current is not { } a) return;
        if (ListingExports.HasPhotos(_owner.OutDir, a))
        {
            ShowPhotos(a);
            _status.Text = "Fotoğraflar daha önce indirilmiş; klasörden gösteriliyor.";
            return;
        }
        if (_owner.Busy)
        {
            MessageBox.Show("Ev kartı toplama ya da başka bir indirme sürüyor; bitince tekrar dene.", "Fotoğraflar");
            return;
        }

        var ct = _owner.BeginWork();
        SetButtons(false);
        var status = new Progress<string>(s => _status.Text = s);
        try
        {
            if (a.PhotoUrls.Count == 0)
            {
                // Adresler toplama sırasında alınmamış (eski kart): ilan sayfası aynı profil ve engel yönetimiyle açılır
                _status.Text = "İlan sayfası açılıyor...";
                await using var picker = await Task.Run(() =>
                    RedfinListingPicker.StartAsync(_owner.BaseDir, _owner.OutDir, _owner.UseOpenChrome, _owner.LogListings, status), ct);
                await Task.Run(() => picker.EnrichAsync(a, status, ct), ct);
            }
            if (a.PhotoUrls.Count == 0) throw new InvalidOperationException("İlan sayfasında fotoğraf bulunamadı.");

            int n = await ListingExports.DownloadPhotosAsync(_owner.OutDir, State, Fips, a, status, ct);
            _owner.SaveCards();
            _owner.LogListings($"{card.County}: {a.Street} — {n} referans fotoğraf indirildi");
            FillAll(a.Url);
            _status.Text = $"{n} fotoğraf indirildi: {ListingExports.PhotoDir(_owner.OutDir, State, Fips, a)}";
        }
        catch (OperationCanceledException) { _status.Text = "İptal edildi."; }
        catch (RedfinListingPicker.ChromeNotReachableException ex) { MessageBox.Show(ex.Message, "Açık Chrome'a bağlan"); }
        catch (Exception ex)
        {
            _owner.LogListings($"{card.County}: {a.Street} fotoğraf indirilemedi — {ex.Message}");
            MessageBox.Show(ex.Message, "Fotoğraflar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _status.Text = "Hata: " + ex.Message;
        }
        finally
        {
            _owner.EndWork();
            SetButtons(true);
        }
    }

    void BtnShowFolder_Click(object? sender, EventArgs e)
    {
        if (_selectedPhoto != null && File.Exists(_selectedPhoto))
            Process.Start("explorer.exe", $"/select,\"{_selectedPhoto}\"");
        else if (Current is { } a && Directory.Exists(ListingExports.PhotoDir(_owner.OutDir, State, Fips, a)))
            Process.Start("explorer.exe", ListingExports.PhotoDir(_owner.OutDir, State, Fips, a));
    }

    async void BtnKml_Click(object? sender, EventArgs e)
    {
        if (Card == null) return;
        var cards = _owner.Cards.Values.Where(c => c.Chosen != null).ToList();
        var missing = cards.Where(c => c.Chosen!.Lat == null || c.Chosen.Lng == null).ToList();

        // Konum alanından önce toplanmış kartlar: ilan sayfalarından tamamlamayı öner
        if (missing.Count > 0 && !_owner.Busy &&
            MessageBox.Show($"{missing.Count} seçilen evin koordinatı yok ({string.Join(", ", missing.Select(c => c.County))}).\n" +
                            $"İlan sayfaları açılıp tamamlansın mı? (ev başına ~10 sn)\nHayır: bu evler KML'e girmez.",
                            "KML dışa aktar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            var ct = _owner.BeginWork();
            SetButtons(false);
            var status = new Progress<string>(s => _status.Text = s);
            try
            {
                await using var picker = await Task.Run(() =>
                    RedfinListingPicker.StartAsync(_owner.BaseDir, _owner.OutDir, _owner.UseOpenChrome, _owner.LogListings, status), ct);
                foreach (var c in missing)
                {
                    ct.ThrowIfCancellationRequested();
                    try { await Task.Run(() => picker.EnrichAsync(c.Chosen!, status, ct), ct); }
                    catch (Exception ex) when (ex is not OperationCanceledException && !picker.Closed)
                    {
                        _owner.LogListings($"{c.County}: konum tamamlanamadı — {ex.Message}");
                    }
                }
                _owner.SaveCards();
            }
            catch (OperationCanceledException) { _status.Text = "İptal edildi; eldekilerle devam."; }
            catch (Exception ex) { MessageBox.Show(ex.Message, "KML dışa aktar", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally
            {
                _owner.EndWork();
                SetButtons(true);
            }
        }

        var (path, written, skipped) = ListingExports.WriteKml(_owner.OutDir, State, cards);
        foreach (var s in skipped) _owner.LogListings($"KML: koordinat yok, atlandı — {s}");
        _owner.LogListings($"KML: {written} ev yazıldı — {path}");
        FillAll(Current?.Url);
        _status.Text = $"KML: {written} ev{(skipped.Count > 0 ? $", {skipped.Count} ev koordinatsız atlandı" : "")} — {path}";
        MessageBox.Show($"{written} ev yazıldı{(skipped.Count > 0 ? $"; koordinatı olmayan {skipped.Count} ev atlandı:\n{string.Join("\n", skipped)}" : ".")}\n\n{path}",
            "KML dışa aktar");
    }

    void BtnAnim_Click(object? sender, EventArgs e)
    {
        if (Card == null) return;
        var (dir, written, skipped) = ListingExports.WriteAnimCsv(_owner.OutDir, State, _owner.Cards.Values);
        foreach (var s in skipped) _owner.LogListings($"Animasyon CSV: fiyat geçmişi yok, atlandı — {s}");
        _status.Text = $"Animasyon CSV: {written} ev — {dir}";
        MessageBox.Show($"{written} ev için date, price, cut dosyası yazıldı{(skipped.Count > 0 ? $"; {skipped.Count} ev atlandı" : "")}.\n\n{dir}",
            "Animasyon CSV");
    }

    void SetButtons(bool enabled)
    {
        foreach (var b in _buttons.Controls.OfType<Button>()) b.Enabled = enabled;
        if (enabled) ShowCandidate(Current);
    }

    void HouseDetailForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        SetPreview(null);
        foreach (var img in _thumbImages) img.Dispose();
        _thumbImages.Clear();
    }
}
