using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Chip8Meta.Run
{
    public partial class FormMain : Form
    {
        private Chip8 _chip8;
        private Bitmap _display;

        public FormMain()
        {
            InitializeComponent();
            _chip8 = new Chip8();
            _display = new Bitmap(Chip8.DisplayWidth, Chip8.DisplayHeight, PixelFormat.Format32bppRgb);
            ResizeRedraw = true;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            _chip8.Load(File.ReadAllBytes("Game.ch8"));
        }

        private void FormMain_Paint(object sender, PaintEventArgs e)
        {
            var clientHeight = ClientSize.Height - menuStrip.Height;
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            int scale = Math.Min(ClientSize.Width / _display.Width, clientHeight / _display.Height);
            e.Graphics.DrawImage(_display,
                new Rectangle(0, menuStrip.Height, _display.Width * scale, _display.Height * scale));
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            try
            {
                for(int i = 0 ; i < 10; i++)
                {
                    _chip8.Step();
                }
                _chip8.Tick();
                UpdateDisplayBitmap();
                Refresh();
            }
            catch
            {
                _timer.Enabled = false;
            }
        }

        private unsafe void UpdateDisplayBitmap()
        {
            var bData = _display.LockBits(new Rectangle(0, 0, _display.Width, _display.Height), ImageLockMode.WriteOnly, _display.PixelFormat);

            byte* scan0 = (byte*)bData.Scan0.ToPointer();

            for (int y = 0; y < bData.Height; y++)
            {
                for (int x = 0; x < bData.Width; x++)
                {
                    byte* pixel = scan0 + y * bData.Stride + x * 4;

                    if (_chip8.Display[y * Chip8.DisplayWidth + x])
                    {
                        pixel[0] = 255;
                        pixel[1] = 255;
                        pixel[2] = 255;
                    }
                    else
                    {
                        pixel[0] = 0;
                        pixel[1] = 0;
                        pixel[2] = 0;
                    }
                }
            }

            _display.UnlockBits(bData);
        }

        private int? KeyToValue(Keys key)
        {
            switch (key)
            {
                case Keys.D1: return 0x1;
                case Keys.D2: return 0x2;
                case Keys.D3: return 0x3;
                case Keys.D4: return 0xC;
                case Keys.Q: return 0x4;
                case Keys.W: return 0x5;
                case Keys.E: return 0x6;
                case Keys.R: return 0xD;
                case Keys.A: return 0x7;
                case Keys.S: return 0x8;
                case Keys.D: return 0x9;
                case Keys.F: return 0xE;
                case Keys.Z: return 0xA;
                case Keys.X: return 0x0;
                case Keys.C: return 0xB;
                case Keys.V: return 0xF;
                default: return null;
            }
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            var value = KeyToValue(e.KeyCode);
            if (value.HasValue)
            {
                _chip8.Keys[value.Value] = true;
            }
        }

        private void FormMain_KeyUp(object sender, KeyEventArgs e)
        {
            var value = KeyToValue(e.KeyCode);
            if (value.HasValue)
            {
                _chip8.Keys[value.Value] = false;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _chip8.Load(File.ReadAllBytes(openFileDialog.FileName));
            }
        }
    }
}
