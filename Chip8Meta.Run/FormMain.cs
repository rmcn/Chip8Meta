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
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            int scale = Math.Min(ClientSize.Width / _display.Width, ClientSize.Height / _display.Height);
            e.Graphics.DrawImage(_display,
                new Rectangle(0, 0, _display.Width * scale, _display.Height * scale));
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
    }
}
