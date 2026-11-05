using System.Drawing.Imaging;

namespace CommunityThemes;

internal static class AssetImport
{
    public static ThemeAsset Image(string path)
    {
        ThemeStore.CheckPath(path);
        if (new FileInfo(path).Length > 8 * 1024 * 1024) throw new InvalidDataException("Images must be at most 8 MiB.");
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg")) throw new InvalidDataException("Choose a PNG or JPEG image.");
        using var stream = File.OpenRead(path);
        using var image = System.Drawing.Image.FromStream(stream, false, true);
        if (image.RawFormat.Guid != ImageFormat.Png.Guid && image.RawFormat.Guid != ImageFormat.Jpeg.Guid) throw new InvalidDataException("The file is not a PNG or JPEG image.");
        CheckDimensions(image);
        // Flatten to a new static PNG, dropping metadata, animation and original container content.
        using var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap)) graphics.DrawImage(image, 0, 0, image.Width, image.Height);
        using var output = new MemoryStream(); bitmap.Save(output, ImageFormat.Png);
        var asset = ThemeAsset.Create(Path.GetFileNameWithoutExtension(path) + ".png", "image/png", output.ToArray());
        ThemeConfiguration.ValidateAsset(asset, false); return asset;
    }

    private static void CheckDimensions(System.Drawing.Image image)
    {
        if (image.Width is < 1 or > 4096 || image.Height is < 1 or > 4096 || (long)image.Width * image.Height > 16_000_000)
            throw new InvalidDataException("Images must be at most 4096 × 4096 and 16 million pixels.");
    }

    public static void ValidateImages(ThemeConfiguration configuration)
    {
        foreach (var asset in new[] { configuration.CustomBanner, configuration.CustomBackground }.OfType<ThemeAsset>())
        {
            using var stream = new MemoryStream(asset.Data);
            using var image = System.Drawing.Image.FromStream(stream, false, true); CheckDimensions(image);
            using var bitmap = new Bitmap(image); _ = bitmap.GetPixel(0, 0);
        }
    }

    public static ThemeAsset? Font(IWin32Window owner)
    {
        using var file = new OpenFileDialog { Title = "Import a static font licensed for embedding and sharing", Filter = "Static fonts (*.ttf;*.otf)|*.ttf;*.otf", CheckFileExists = true };
        if (file.ShowDialog(owner) != DialogResult.OK) return null;
        ThemeStore.CheckPath(file.FileName);
        if (new FileInfo(file.FileName).Length > 2 * 1024 * 1024) throw new InvalidDataException("Fonts must be at most 2 MiB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".ttf" or ".otf")) throw new InvalidDataException("Choose a static TTF or OTF font.");
        using var license = new OpenFileDialog { Title = "Select this font's redistribution license (TXT)", Filter = "License text (*.txt)|*.txt", CheckFileExists = true };
        if (license.ShowDialog(owner) != DialogResult.OK) return null;
        ThemeStore.CheckPath(license.FileName);
        if (new FileInfo(license.FileName).Length > 64000) throw new InvalidDataException("The font license must be at most 64,000 bytes.");
        var asset = ThemeAsset.Create(Path.GetFileName(file.FileName), extension == ".ttf" ? "font/ttf" : "font/otf", File.ReadAllBytes(file.FileName), File.ReadAllText(license.FileName));
        ThemeConfiguration.ValidateAsset(asset, true);
        using var dialog = new Form { Text = "Font license", BackColor = LauncherStyle.Surface, ForeColor = LauncherStyle.Text, Font = LauncherStyle.Font(), ClientSize = new Size(640, 500), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var text = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Text = asset.License.ReplaceLineEndings(Environment.NewLine), BackColor = LauncherStyle.Base, ForeColor = LauncherStyle.Text };
        var consent = new DiscordCheckBox { Text = "I have permission to embed and redistribute this font\nand its license with shared theme configurations.", Dock = DockStyle.Fill };
        var accept = new DiscordButton { Text = "Import font", Primary = true, Dock = DockStyle.Right, Width = 140, Enabled = false, DialogResult = DialogResult.OK };
        consent.CheckedChanged += (_, _) => accept.Enabled = consent.Checked;
        layout.Controls.Add(text, 0, 0); layout.Controls.Add(consent, 0, 1); layout.Controls.Add(accept, 0, 2); dialog.Controls.Add(layout);
        dialog.Shown += (_, _) => LauncherStyle.DarkCaption(dialog);
        return dialog.ShowDialog(owner) == DialogResult.OK ? asset : null;
    }
}
