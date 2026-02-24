using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using ShortcutNest.Config;
using ShortcutNest.Models;

namespace ShortcutNest
{
    public class LauncherPopup : Form
    {
        // UI Constants for theming and maintainability
        private readonly Color _popupBg = Color.FromArgb(14, 18, 28);
        private readonly Color _slotBg = Color.FromArgb(25, 30, 42);
        private readonly Color _slotHover = Color.FromArgb(36, 49, 74);
        private readonly Color _slotSelected = Color.FromArgb(49, 78, 138);
        private readonly Color _slotBorder = Color.FromArgb(55, 63, 86);
        private readonly Color _slotSelectedBorder = Color.FromArgb(112, 164, 255);
        private const int SlotBorderRadius = 14;
        private const int FormBorderRadius = 18;
        private const int GridSize = 3; // 3x3

        private readonly TableLayoutPanel _grid;
        private LauncherConfig _config = LauncherConfig.Load();
        private readonly List<Panel> _slotPanels = [];
        private int _selectedIndex = 0;

        public bool HideOnCloseRequested { get; set; } = false;

        public LauncherPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            DoubleBuffered = true;

            BackColor = _popupBg;
            ForeColor = Color.White;
            Padding = new Padding(14);
            Width = 620;
            Height = 410;

            _grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = GridSize,
                RowCount = GridSize,
                BackColor = Color.Transparent,
                Padding = new Padding(4)
            };

            for (int i = 0; i < GridSize; i++)
            {
                _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / GridSize));
                _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / GridSize));
            }

            Controls.Add(_grid);

            Resize += (_, __) => ApplyRoundedRegionToForm();
            RebuildButtons();

            // Esta linha é a versão antiga que fechava ao clicar fora/perder foco
            // Deactivate += (_, __) => HidePopup();

            KeyDown += LauncherPopup_KeyDown;

            Shown += (_, __) =>
            {
                ApplyRoundedRegionToForm();
                Focus();
                SelectSlot(0);

                // Force the popup to the top by toggling TopMost
                this.TopMost = false;
                this.TopMost = true;
            };
        }

        public void ReloadConfig()
        {
            _config = LauncherConfig.Load();
            RebuildButtons();
        }

        public void ShowCentered()
        {
            ReloadConfig();

            var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(
                screen.Left + (screen.Width - Width) / 2,
                screen.Top + (screen.Height - Height) / 2
            );

            Show();
            Activate();
            Focus();

            SelectSlot(Math.Clamp(_selectedIndex, 0, GridSize * GridSize - 1));
        }

        public void HidePopup()
        {
            if (HideOnCloseRequested)
                Hide();
            else
                Close();
        }

        private void RebuildButtons()
        {
            _grid.Controls.Clear();
            _slotPanels.Clear();

            int totalSlots = GridSize * GridSize;
            for (int i = 0; i < totalSlots; i++)
            {
                var slot = i < _config.Slots.Count ? _config.Slots[i] : null;
                var panel = CreateSlotPanel(i, slot);
                _slotPanels.Add(panel);
                _grid.Controls.Add(panel, i % GridSize, i / GridSize);
            }

            _selectedIndex = 0;
            UpdateSlotVisualStates();
        }

        private Panel CreateSlotPanel(int index, LauncherSlot? slot)
        {
            var panel = new Panel
            {
                Margin = new Padding(8),
                BackColor = _slotBg,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                Tag = index,
                TabStop = false
            };

            panel.Paint += (s, e) => PaintSlotPanel(s, e);
            panel.Resize += (s, e) => ResizeSlotPanel(s, e);

            var layout = CreateSlotLayout(slot, index);
            panel.Controls.Add(layout);

            SetupSlotEvents(panel, layout, index);

            return panel;
        }

        private void PaintSlotPanel(object? sender, PaintEventArgs e)
        {
            var panel = (Panel)sender!;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int panelIndex = panel.Tag is int i ? i : -1;
            bool isSelected = panelIndex == _selectedIndex;
            Color borderColor = isSelected ? _slotSelectedBorder : _slotBorder;

            using var borderPen = new Pen(borderColor, isSelected ? 2f : 1f);
            var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            using var path = CreateRoundedRectPath(rect, SlotBorderRadius);
            e.Graphics.DrawPath(borderPen, path);
        }

        private void ResizeSlotPanel(object? sender, EventArgs e)
        {
            var panel = (Panel)sender!;
            using var path = CreateRoundedRectPath(new Rectangle(0, 0, panel.Width, panel.Height), SlotBorderRadius);
            panel.Region = new Region(path);
            panel.Invalidate();
        }

        private TableLayoutPanel CreateSlotLayout(LauncherSlot? slot, int index)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(10),
                Margin = new Padding(0),
                TabStop = false
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));

            var iconBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2),
                TabStop = false
            };
            TryLoadIconInto(iconBox, slot);

            var title = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.TopCenter,
                Text = slot?.Title is { Length: > 0 } ? $"{index + 1}. {slot.Title}" : $"{index + 1}. (empty)",
                Margin = new Padding(0, 4, 0, 0),
                TabStop = false
            };

            layout.Controls.Add(iconBox, 0, 0);
            layout.Controls.Add(title, 0, 1);
            return layout;
        }

        private void SetupSlotEvents(Panel panel, TableLayoutPanel layout, int index)
        {
            void clickHandler(object? s, EventArgs e) => ExecuteSlot(index);

            panel.Click += clickHandler;
            layout.Click += clickHandler;

            foreach (Control ctrl in layout.Controls)
                ctrl.Click += clickHandler;

            panel.MouseEnter += (_, __) =>
            {
                _selectedIndex = index;
                panel.BackColor = _slotHover;
                UpdateSlotVisualStates();
            };

            panel.MouseLeave += (_, __) => UpdateSlotVisualStates();

            void setHoverSelection()
            {
                _selectedIndex = index;
                UpdateSlotVisualStates();
            }

            foreach (Control ctrl in layout.Controls)
                ctrl.MouseEnter += (_, __) => setHoverSelection();
        }

        private void TryLoadIconInto(PictureBox iconBox, LauncherSlot? slot)
        {
            if (slot == null)
                return;

            if (!string.IsNullOrWhiteSpace(slot.IconPath))
            {
                try
                {
                    var fullPath = ResolvePath(slot.IconPath);
                    if (File.Exists(fullPath))
                    {
                        iconBox.Image?.Dispose();
                        iconBox.Image = Image.FromFile(fullPath);
                        return;
                    }
                }
                catch
                {
                    // Ignore icon load errors and fallback to generated placeholder
                }
            }

            iconBox.Paint += (s, e) => PaintFallbackIcon(s, e, slot);
        }

        private void PaintFallbackIcon(object? sender, PaintEventArgs e, LauncherSlot? slot)
        {
            var pb = (PictureBox)sender!;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var r = new Rectangle(8, 8, Math.Max(24, pb.Width - 16), Math.Max(24, pb.Height - 16));
            int size = Math.Min(r.Width, r.Height);
            r = new Rectangle(pb.Width / 2 - size / 2, pb.Height / 2 - size / 2, size, size);

            using var bg = new SolidBrush(Color.FromArgb(34, 40, 58));
            using var pen = new Pen(Color.FromArgb(70, 92, 140), 1.5f);
            using var txtBrush = new SolidBrush(Color.FromArgb(160, 182, 230));
            using var path = CreateRoundedRectPath(r, 10);

            e.Graphics.FillPath(bg, path);
            e.Graphics.DrawPath(pen, path);

            string letter = "?";
            if (!string.IsNullOrWhiteSpace(slot?.Title))
                letter = slot.Title.Trim()[0].ToString().ToUpperInvariant();

            using var font = new Font("Segoe UI", Math.Max(10f, size * 0.28f), FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(letter, font, txtBrush, r, sf);
        }

        private void ExecuteSlot(int index)
        {
            if (index < 0 || index >= _config.Slots.Count)
                return;

            var slot = _config.Slots[index];
            if (slot == null)
                return;

            try
            {
                switch ((slot.Type ?? "").ToLowerInvariant())
                {
                    case "app":
                    case "url":
                    case "folder":
                        if (!string.IsNullOrWhiteSpace(slot.Target))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = ResolvePathIfNeeded(slot.Target, slot.Type!),
                                UseShellExecute = true
                            });
                        }
                        break;

                    case "command":
                        if (!string.IsNullOrWhiteSpace(slot.Target))
                        {
                            // Check if the command appears to be PowerShell
                            bool isPowerShell = slot.Target.Contains('$') || slot.Target.Contains('[') || slot.Target.Contains('{');

                            Process.Start(new ProcessStartInfo
                            {
                                FileName = isPowerShell ? "powershell.exe" : "cmd.exe",
                                Arguments = isPowerShell
                                    ? $"-NoProfile -NoExit -Command \"{slot.Target}\""
                                    : $"/C \"{slot.Target}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing slot {index + 1}:\n{ex.Message}",
                    "ShortcutNest", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HidePopup();
            }
        }

        private string ResolvePathIfNeeded(string target, string type)
        {
            if (type.Equals("url", StringComparison.OrdinalIgnoreCase))
                return target;

            try
            {
                if (!Path.IsPathRooted(target))
                {
                    var combined = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, target);
                    if (File.Exists(combined) || Directory.Exists(combined))
                        return combined;
                }
            }
            catch { }

            return target;
        }

        private string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        private void ApplyRoundedRegionToForm()
        {
            if (Width <= 0 || Height <= 0) return;

            using var path = CreateRoundedRectPath(new Rectangle(0, 0, Width, Height), FormBorderRadius);
            Region = new Region(path);
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            int d = Math.Max(2, radius * 2);
            int x = rect.X;
            int y = rect.Y;
            int w = Math.Max(1, rect.Width);
            int h = Math.Max(1, rect.Height);

            var path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void LauncherPopup_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                HidePopup();
                e.Handled = true;
                return;
            }

            int? numericIndex = e.KeyCode switch
            {
                Keys.D1 or Keys.NumPad1 => 0,
                Keys.D2 or Keys.NumPad2 => 1,
                Keys.D3 or Keys.NumPad3 => 2,
                Keys.D4 or Keys.NumPad4 => 3,
                Keys.D5 or Keys.NumPad5 => 4,
                Keys.D6 or Keys.NumPad6 => 5,
                Keys.D7 or Keys.NumPad7 => 6,
                Keys.D8 or Keys.NumPad8 => 7,
                Keys.D9 or Keys.NumPad9 => 8,
                _ => null
            };

            if (numericIndex.HasValue)
            {
                SelectSlot(numericIndex.Value);
                ExecuteSlot(numericIndex.Value);
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                ExecuteSlot(_selectedIndex);
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Tab)
            {
                int delta = e.Shift ? -1 : 1;
                MoveSelectionLinear(delta);
                e.Handled = true;
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Left:
                    MoveSelectionGrid(-1, 0);
                    e.Handled = true;
                    return;
                case Keys.Right:
                    MoveSelectionGrid(1, 0);
                    e.Handled = true;
                    return;
                case Keys.Up:
                    MoveSelectionGrid(0, -1);
                    e.Handled = true;
                    return;
                case Keys.Down:
                    MoveSelectionGrid(0, 1);
                    e.Handled = true;
                    return;
            }
        }

        private void MoveSelectionLinear(int delta)
        {
            int newIndex = (_selectedIndex + delta) % (GridSize * GridSize);
            if (newIndex < 0) newIndex += (GridSize * GridSize);
            SelectSlot(newIndex);
        }

        private void MoveSelectionGrid(int dx, int dy)
        {
            int row = _selectedIndex / GridSize;
            int col = _selectedIndex % GridSize;

            row = (row + dy + GridSize) % GridSize;
            col = (col + dx + GridSize) % GridSize;

            int newIndex = row * GridSize + col;
            SelectSlot(newIndex);
        }

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= _slotPanels.Count)
                return;

            _selectedIndex = index;
            UpdateSlotVisualStates();
        }

        private void UpdateSlotVisualStates()
        {
            for (int i = 0; i < _slotPanels.Count; i++)
            {
                var panel = _slotPanels[i];
                bool isSelected = i == _selectedIndex;

                panel.BackColor = isSelected ? _slotSelected : _slotBg;
                panel.Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var panel in _slotPanels)
                {
                    foreach (Control ctrl in panel.Controls)
                    {
                        if (ctrl is TableLayoutPanel layout)
                        {
                            foreach (Control subCtrl in layout.Controls)
                            {
                                if (subCtrl is PictureBox pb && pb.Image != null)
                                {
                                    pb.Image.Dispose();
                                }
                            }
                        }
                    }
                }
            }
            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_ACTIVATE = 0x0006;
            if (m.Msg == WM_ACTIVATE && m.WParam == IntPtr.Zero)
            {
                // Deactivated
                HidePopup();
            }
            base.WndProc(ref m);
        }
    }
}