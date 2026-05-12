using OpenTK;
using OpenTK.Graphics.OpenGL;
//using OpenTK.WinForms;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PixelLab
{
    public class RgbCubeControl : GLControl
    {
        public event Action<Color> ColorPicked;
        private float rotationX = 30f, rotationY = 45f;
        private float scale = 1f;
        private Point lastMouse;

        public RgbCubeControl() : base()
        {
            Load += OnLoad;
            Paint += OnPaint;
            MouseDown += (s, e) => lastMouse = e.Location;
            MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    rotationY += (e.X - lastMouse.X) * 0.5f;
                    rotationX += (e.Y - lastMouse.Y) * 0.5f;
                    lastMouse = e.Location;
                    Invalidate();
                }
            };
            MouseWheel += (s, e) => { scale += e.Delta * 0.01f; Invalidate(); };
            MouseClick += OnMouseClick;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color.DarkSlateGray);
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (!DesignMode)
            {
                MakeCurrent();
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadIdentity();
                GL.Scale(scale, scale, scale);
                GL.Rotate(rotationX, 1, 0, 0);
                GL.Rotate(rotationY, 0, 1, 0);
                // رسم مكعب ألوان RGB – كل رأس ذو لون محدد
                DrawRgbCube();
                SwapBuffers();
            }
        }

        private void DrawRgbCube()
        {
            // الرؤوس: (0,0,0) أسود، (1,0,0) أحمر، (0,1,0) أخضر، (0,0,1) أزرق، (1,1,0) أصفر، (1,0,1) أرجواني، (0,1,1) سماوي، (1,1,1) أبيض
            float[] vertices = {
                0,0,0, 1,0,0, 1,1,0, 0,1,0, // الوجه الخلفي السفلي (z=0)
                0,0,1, 1,0,1, 1,1,1, 0,1,1  // الوجه الأمامي العلوي (z=1)
            };
            byte[] indices = {
                0,1, 1,2, 2,3, 3,0, // الوجه الخلفي
                4,5, 5,6, 6,7, 7,4, // الوجه الأمامي
                0,4, 1,5, 2,6, 3,7  // الأعمدة
            };
            GL.Begin(PrimitiveType.Lines);
            for (int i = 0; i < indices.Length; i += 2)
            {
                int i1 = indices[i], i2 = indices[i + 1];
                GL.Color3(vertices[i1 * 3], vertices[i1 * 3 + 1], vertices[i1 * 3 + 2]);
                GL.Vertex3(vertices[i1 * 3], vertices[i1 * 3 + 1], vertices[i1 * 3 + 2]);
                GL.Color3(vertices[i2 * 3], vertices[i2 * 3 + 1], vertices[i2 * 3 + 2]);
                GL.Vertex3(vertices[i2 * 3], vertices[i2 * 3 + 1], vertices[i2 * 3 + 2]);
            }
            GL.End();

            // رسم الوجوه بملئ شفاف لإظهار الألوان – يمكن استخدام GL.PolygonMode
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Color4(0.5f, 0.5f, 0.5f, 0.2f);
            // ... رسم الوجوه (نترك للقارئ توسيعها)
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            // اختيار لون من المكعب يعتمد على إحداثيات المؤشر وتحويلها إلى فضاء الكائن
            // سنحاكي اختيار لون عشوائي لأغراض العرض
            Random rand = new Random();
            Color picked = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
            ColorPicked?.Invoke(picked);
        }
    }
}