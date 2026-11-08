using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

namespace NeonRift
{
    // Tiny standalone tool. It writes a standards-compliant PNG-backed ICO, no shell/icon library needed.
    public static class IconBuilder
    {
        public static void Main(string[] args)
        {
            string dir = args.Length > 0 ? args[0] : ".";
            Directory.CreateDirectory(dir);
            int[] sizes = new int[] { 16, 32, 48, 64, 128, 256 };
            List<byte[]> pngs = new List<byte[]>();
            for (int i = 0; i < sizes.Length; i++)
            {
                using (Bitmap b = new Bitmap(sizes[i], sizes[i], PixelFormat.Format32bppArgb))
                using (Graphics g = Graphics.FromImage(b))
                using (MemoryStream ms = new MemoryStream())
                {
                    g.Clear(Color.FromArgb(7, 11, 26));
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    Art.Portrait(g, new Rectangle(0, 0, sizes[i], sizes[i]), 0);
                    b.Save(ms, ImageFormat.Png);
                    pngs.Add(ms.ToArray());
                    if (sizes[i] == 256) File.WriteAllBytes(Path.Combine(dir, "NeonRift-Portrait.png"), pngs[pngs.Count - 1]);
                }
            }
            using (FileStream fs = File.Create(Path.Combine(dir, "NeonRift.ico")))
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                w.Write((short)0); w.Write((short)1); w.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    int s = sizes[i];
                    w.Write((byte)(s == 256 ? 0 : s)); w.Write((byte)(s == 256 ? 0 : s));
                    w.Write((byte)0); w.Write((byte)0); w.Write((short)1); w.Write((short)32);
                    w.Write(pngs[i].Length); w.Write(offset); offset += pngs[i].Length;
                }
                for (int i = 0; i < pngs.Count; i++) w.Write(pngs[i]);
            }
            Art.DisposeResources();
        }
    }
}
