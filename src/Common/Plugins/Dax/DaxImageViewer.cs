using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GoldBoxExplorer.Lib.Plugins.Dax
{
    public class DaxImageViewer : IGoldBoxViewer
    {
        public event EventHandler<ChangeFileEventArgs> ChangeSelectedFile;
        private readonly IList<Bitmap> _bitmaps;
        private readonly IList<int> _bitmapIds;
        private readonly bool _display35ImagesPerRow;
        private readonly bool _displayBorder;
        private readonly PictureBox _pictureBox;
        private readonly Dictionary<RectangleF, int> _rectanglesToIndex;
        private readonly DaxImageFile _file;

        public DaxImageViewer(IList<Bitmap> bitmaps, float zoom, int containerWidth, bool display35ImagesPerRow, bool displayBorder, IList<int> bitmapIds, DaxImageFile file)
        {
            Zoom = zoom;
            ContainerWidth = containerWidth;
            _bitmaps = bitmaps;
            _bitmapIds = bitmapIds;
            _display35ImagesPerRow = display35ImagesPerRow;
            _displayBorder = displayBorder;
            _rectanglesToIndex = new Dictionary<RectangleF, int>();
            _pictureBox = new PictureBox();
            _pictureBox.Paint += PictureBoxPaint;
            _pictureBox.DoubleClick += PictureBoxDoubleClick;
            _file = file;
        }

        public float Zoom { get; set; }

        public int ContainerWidth { get; set; }

        public Control GetControl()
        {
            return _pictureBox;
        }

        private void PictureBoxPaint(object sender, PaintEventArgs e)
        {
            var x = 0;
            var y = 0;
            var bitmapCount = _bitmaps.Count;
            var lastImageHeight = 0;
            var pen = new Pen(Color.Fuchsia);
            int fontSize = (int) (14  * Zoom);
            var font = new Font("Courier New", fontSize);
            var brush = new SolidBrush(Color.FromArgb(85, 85, 85));
            int padding = fontSize * 3;

            _rectanglesToIndex.Clear();

            for (int i = 0; i < bitmapCount; i++)
            {
                var currentImage = _bitmaps[i];
                var currentId = _bitmapIds[i];
                lastImageHeight = (int) Math.Max(lastImageHeight, currentImage.Height*Zoom);

                var newRow = false;

                if (_display35ImagesPerRow)
                {
                    if (i > 0 && i%35 == 0)
                        newRow = true;
                }
                else
                {
                    if (x + ((padding + currentImage.Width)*Zoom) > ContainerWidth)
                        newRow = true;
                }

                if(newRow)
                {
                    x = 0;
                    var ypad = (int) (padding*Zoom/2);
                    y += lastImageHeight + ypad;
                    if (i < bitmapCount - 1) lastImageHeight = 0;
                }
                RectangleF imageRect = new RectangleF(x, y, currentImage.Width * Zoom, currentImage.Height * Zoom);
                _rectanglesToIndex.Add(imageRect, i);
                e.Graphics.DrawImage(currentImage, imageRect);
                e.Graphics.DrawString(currentId.ToString(), font, brush, x + (currentImage.Width*Zoom),y);
                if (_displayBorder)
                {
                    e.Graphics.DrawRectangle(pen, x, y, currentImage.Width*Zoom, currentImage.Height*Zoom);
                }

                x += (int) ((currentImage.Width + padding)*Zoom);
            }

            _pictureBox.Width = ContainerWidth;
            _pictureBox.Height = (int) (fontSize + y + (lastImageHeight*Zoom));
        }

        private void PictureBoxDoubleClick(object sender, EventArgs e)
        {
            int id = GetID(new Point(((MouseEventArgs)e).X, ((MouseEventArgs)e).Y));
            if (id < 0)
            {
                return;
            }

            Bitmap origBitmap = _bitmaps[id];

            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            string dimensions = origBitmap.Width + " x " + origBitmap.Height;
            fileDialog.Filter = "Bitmap images (*.bmp)|*.bmp";
            fileDialog.Title = "Please choose a bitmap file with the following dimensions: " + dimensions;
            DialogResult result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                System.IO.Stream strm = fileDialog.OpenFile();
                Bitmap b = new Bitmap(strm);
                
                if (b.Width != origBitmap.Width || b.Height != origBitmap.Height) { 
                    string fileDims = b.Width + " x " + b.Height;
                    throw new ArgumentException("Size of loaded image (" + fileDims + ") does not match original (" + dimensions + ")");
                }

                // Modify the block with the new images
                _file.SetBitmap(b, id);
                _pictureBox.Refresh();
                _file.save();
            }

        }

        private int GetID(Point point)
        {
            foreach (RectangleF r in _rectanglesToIndex.Keys)
            {
                if (r.Contains(point))
                {
                    return _rectanglesToIndex[r];
                }
            }
            return -1;
        }
    }
}