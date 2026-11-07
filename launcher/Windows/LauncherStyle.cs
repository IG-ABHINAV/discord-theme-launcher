using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CommunityThemes;

internal static class LauncherStyle
{
    // Discord's current native dark palette, observed from its rendered UI.
    public static readonly Color Base = Color.FromArgb(26, 26, 30);
    public static readonly Color Surface = Color.FromArgb(32, 32, 36);
    public static readonly Color Card = Color.FromArgb(36, 36, 41);
    public static readonly Color Hover = Color.FromArgb(45, 45, 51);
    public static readonly Color Border = Color.FromArgb(67, 68, 77);
    public static readonly Color Text = Color.FromArgb(239, 239, 241);
    public static readonly Color Muted = Color.FromArgb(181, 182, 190);
    public static readonly Color Blurple = Color.FromArgb(88, 101, 242);
    public static readonly Color Link = Color.FromArgb(0, 168, 252);
    private static FontFamily family = FontFamily.GenericSansSerif;
    public static Font Font(float size = 10, FontStyle style = FontStyle.Regular) => new(family, size, style);

    public sealed class Typography : IDisposable
    {
        private readonly PrivateFontCollection fonts = new();
        private readonly List<IntPtr> allocations = [];
        private readonly List<IntPtr> registrations = [];
        public Typography()
        {
            using var installed = new InstalledFontCollection();
            var gg = installed.Families.FirstOrDefault(f => f.Name.Equals("gg sans", StringComparison.OrdinalIgnoreCase));
            if (gg != null) { family = new FontFamily(gg.Name); return; }
            foreach (var name in new[] { "NotoSans-Regular.ttf", "NotoSans-Bold.ttf" })
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!;
                using var bytes = new MemoryStream(); stream.CopyTo(bytes);
                var data = bytes.ToArray(); var pointer = Marshal.AllocHGlobal(data.Length); allocations.Add(pointer);
                Marshal.Copy(data, 0, pointer, data.Length);
                fonts.AddMemoryFont(pointer, data.Length);
                uint count = 0;
                var handle = AddFontMemResourceEx(pointer, (uint)data.Length, IntPtr.Zero, ref count);
                if (handle != IntPtr.Zero) registrations.Add(handle);
            }
            family = fonts.Families[0];
        }
        public void Dispose()
        {
            fonts.Dispose();
            foreach (var handle in registrations) RemoveFontMemResourceEx(handle);
            foreach (var pointer in allocations) Marshal.FreeHGlobal(pointer);
        }
        [DllImport("gdi32.dll")] private static extern IntPtr AddFontMemResourceEx(IntPtr data, uint size, IntPtr reserved, ref uint count);
        [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool RemoveFontMemResourceEx(IntPtr handle);
    }

    public static void DarkCaption(Form form)
    {
        if (!form.IsHandleCreated) return;
        int enabled = 1;
        _ = DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int));
    }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    public static void SurfaceBox(Graphics graphics, Rectangle bounds, Color fill, Color border, int radius = 6)
    {
        using var shape = new GraphicsPath();
        int diameter = radius * 2;
        shape.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        shape.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        shape.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        shape.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90); shape.CloseFigure();
        using var brush = new SolidBrush(fill); using var pen = new Pen(border);
        graphics.FillPath(brush, shape); graphics.DrawPath(pen, shape);
    }

    public static void ShowMessage(IWin32Window? owner, string message, string title, bool longText = false)
    {
        using var dialog = new Form { Text = title, BackColor = Surface, ForeColor = Text, Font = Font(), ClientSize = new Size(600, longText ? 560 : 240), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 2, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        Control copy = longText ? new TextBox { Text = message.ReplaceLineEndings(Environment.NewLine), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Base, ForeColor = Text, BorderStyle = BorderStyle.None, Dock = DockStyle.Fill } : new Label { Text = message, Dock = DockStyle.Fill, ForeColor = Text };
        var close = new DiscordButton { Text = UiText.Text("Got it"), Primary = true, AutoSize = true, Padding = new Padding(18, 8, 18, 8), Anchor = AnchorStyles.Right, DialogResult = DialogResult.OK };
        layout.Controls.Add(copy, 0, 0); layout.Controls.Add(close, 0, 1); dialog.Controls.Add(layout);
        dialog.AcceptButton = close; dialog.CancelButton = close;
        dialog.Shown += (_, _) => { DarkCaption(dialog); if (copy is TextBox textBox) textBox.Select(0, 0); close.Focus(); };
        dialog.ShowDialog(owner);
    }
}

// Keep native CheckBox/Button keyboard and accessibility behavior; own only their painting.
// Windows' default disabled painting forces dark text, regardless of ForeColor.
internal sealed class DiscordCheckBox : CheckBox
{
    private bool hover;
    public DiscordCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        ForeColor = LauncherStyle.Text; Cursor = Cursors.Hand; UseMnemonic = false;
    }
    public override Size GetPreferredSize(Size proposedSize)
    {
        int width = proposedSize.Width > 34 ? proposedSize.Width - 34 : 560;
        var text = TextRenderer.MeasureText(Text, Font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        return new Size(text.Width + 34, Math.Max(26, text.Height + 6));
    }
    protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var box = new Rectangle(1, (Height - 20) / 2, 20, 20);
        var fill = Checked ? (Enabled ? LauncherStyle.Blurple : Color.FromArgb(72, 78, 125)) : BackColor;
        LauncherStyle.SurfaceBox(e.Graphics, box, fill, Checked ? fill : hover && Enabled ? LauncherStyle.Text : LauncherStyle.Muted, 4);
        if (Checked)
        {
            using var pen = new Pen(Color.White, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.DrawLines(pen, [new PointF(box.Left + 5, box.Top + 10), new PointF(box.Left + 9, box.Top + 14), new PointF(box.Left + 16, box.Top + 6)]);
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(31, 0, Math.Max(0, Width - 33), Height), Enabled ? LauncherStyle.Text : Color.FromArgb(211, 212, 220), TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -1, -1), LauncherStyle.Text, BackColor);
    }
}

internal sealed class DiscordButton : Button
{
    [System.ComponentModel.DefaultValue(false)]
    public bool Primary { get; set; }
    private bool hover;
    public DiscordButton() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); UseMnemonic = false; Cursor = Cursors.Hand; }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? LauncherStyle.Surface); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var fill = !Enabled ? Color.FromArgb(55, 56, 70) : Primary ? hover ? Color.FromArgb(71, 82, 196) : LauncherStyle.Blurple : hover ? Color.FromArgb(70, 71, 80) : Color.FromArgb(57, 58, 66);
        LauncherStyle.SurfaceBox(e.Graphics, new Rectangle(1, 1, Width - 3, Height - 3), fill, Primary ? fill : LauncherStyle.Border);
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? Color.White : Color.FromArgb(211, 212, 220), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -5, -5), Color.White, fill);
    }
}
