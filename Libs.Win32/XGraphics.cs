using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Nox.Win32
{
    public class Vector2
    {
        public int X, Y;      

        public void RotateX(float Angle)
        {

        }
    }

    //public struct Vector3 : IEquatable<Vector3>, IFormattable
    //{
    //    public float X, Y, Z;


    //    public static readonly int SizeInBytes = Utilities.SizeOf<Vector3>();




    //    public float Length =>
    //        (float) Math.Sqrt((X* X) + (Y* Y) + (Z* Z));

    //    public Vector3(float X, float Y, float Z)
    //    {
    //        this.X = X;
    //        this.Y = Y;
    //        this.Z = Z;
    //    }
    //}

    public class Vector4
    {

    }

    public class Leyout
    {
        //public Color Back = Color.CornflowerBlue;

        //public Color FrameLight = Color.Azure;
        //public Color FrameDark = Color.DarkSlateBlue;

        ////public Color FixedBorder = Color.SkyBlue;

        //public Color Grid = Color.White;
        //public Color Box = Color.Black;

        public Color Back1 = Color.FromArgb(int.Parse("ff0b597d", System.Globalization.NumberStyles.HexNumber));
        public Color Back2 = Color.FromArgb(int.Parse("ff10151b", System.Globalization.NumberStyles.HexNumber));

        public Color FrameLight = Color.FromArgb(int.Parse("ff1880b7", System.Globalization.NumberStyles.HexNumber));
        public Color FrameDark = Color.FromArgb(int.Parse("ff242b33", System.Globalization.NumberStyles.HexNumber));

        public Color FixedBorder = Color.FromArgb(int.Parse("ff0899b4", System.Globalization.NumberStyles.HexNumber));

        public Color Grid = Color.FromArgb(int.Parse("ffd7edf8", System.Globalization.NumberStyles.HexNumber));
        public Color Box = Color.Black;
    }

    public class LeyoutWinFormExtender
        : Leyout
    {

    }

    public class XGraphics : IDisposable
    {
        private Leyout _Leyout = new Leyout();

        private Graphics _graphics;


        #region Properties
        public Leyout Leyout { get => _Leyout; set => _Leyout = value; }

        public Graphics Graphics { get => _graphics; }
        #endregion

        public void DrawBorderBox(Rectangle f, int BorderWidth, Color BorderColor)
        {
            var P = new Pen(BorderColor);
            for (int i = 0; i < BorderWidth; i++)
                _graphics.DrawRectangle(P, new Rectangle(f.X + i, f.Y + i, f.Width - (i << 1), f.Height - (i << 1)));
        }

        public void Fill(Rectangle f, Color FillColor) =>
            _graphics.FillRectangle(new SolidBrush(FillColor), f);

        public void DrawFrame(Rectangle f, int BorderWidth, Color Light, Color Dark)
        {
            var L = new Pen(Light);
            var D = new Pen(Dark);

            int BW = BorderWidth, BW2 = BorderWidth << 1;
            for (int i = 0; i < BorderWidth; i++)
            {
                _graphics.DrawLine(D, f.Left + i, f.Top, f.Left + i, f.Bottom - 1); // left
                _graphics.DrawLine(D, f.Left, f.Top + i, f.Right - 1, f.Top + i); // top
                _graphics.DrawLine(L, f.Left + BW, f.Bottom - i - 1, f.Right - 1, f.Bottom - i - 1);
                _graphics.DrawLine(L, f.Right - i - 1, f.Top + BW, f.Right - i - 1, f.Bottom - i - 1);

                _graphics.DrawLine(L, f.Left + BW + i, f.Top + BW, f.Left + BW + i, f.Bottom - BW - 1); // left
                _graphics.DrawLine(L, f.Left + BW, f.Top + BW + i, f.Right - BW - 1, f.Top + BW + i); // top
                _graphics.DrawLine(D, f.Left + BW2, f.Bottom - BW - i - 1, f.Right - BW - 1, f.Bottom - BW - i - 1);
                _graphics.DrawLine(D, f.Right - BW - i - 1, f.Top + BW2, f.Right - BW - i - 1, f.Bottom - BW - i - 1);
            }
        }

        public void DrawFrame(Rectangle f, int BorderWidth) =>
            DrawFrame(f, BorderWidth, Leyout.FrameLight, Leyout.FrameDark);

        public void DrawFilledBorderBox(Rectangle f, int BorderWidth, Color FillColor, Color BorderColor)
        {
            // fill 
            _graphics.FillRectangle(new SolidBrush(FillColor), f);

            DrawBorderBox(f, BorderWidth, BorderColor);
        }

        // Given three colinear points p, q, r,
        // the function checks if point q lies
        // on line segment 'pr'
        private bool onSegment(Point p, Point q, Point r) =>
            q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
            q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);

        // To find orientation of ordered triplet (p, q, r).
        // The function returns following values
        // -1 --> Counterclockwise
        //  0 --> p, q and r are colinear
        //  1 --> Clockwise
        private int orientation(Point p, Point q, Point r) =>
            (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);

        // The function that returns true if
        // line segment 'p1q1' and 'p2q2' intersect.
        private bool LineIntersect(Point p1, Point q1, Point p2, Point q2)
        {
            // Find the four orientations needed for
            // general and special cases
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases
            // p1, q1 and p2 are colinear and
            // p2 lies on segment p1q1
            if (o1 == 0 && onSegment(p1, p2, q1))
                return true;

            // p1, q1 and p2 are colinear and
            // q2 lies on segment p1q1
            if (o2 == 0 && onSegment(p1, q2, q1))
                return true;

            // p2, q2 and p1 are colinear and
            // p1 lies on segment p2q2
            if (o3 == 0 && onSegment(p2, p1, q2))
                return true;

            // p2, q2 and q1 are colinear and
            // q1 lies on segment p2q2
            if (o4 == 0 && onSegment(p2, q1, q2))
                return true;

            // Doesn't fall in any of the above cases
            return false;
        }

        // Returns true if the point p lies
        // inside the polygon[] with n vertices
        private bool isPointInsidePolygon(int n, Point p, params Point[] points)
        {
            int INF = 0x10000;

            // There must be at least 3 vertices in polygon[]
            if (n < 3)
                return false;

            // Create a point for line segment from p to infinite
            Point extreme = new Point(INF, p.Y);

            // Count intersections of the above line
            // with sides of polygon
            int count = 0, i = 0;
            do
            {
                int next = (i + 1) % n;

                // Check if the line segment from 'p' to
                // 'extreme' intersects with the line
                // segment from 'polygon[i]' to 'polygon[next]'
                if (LineIntersect(points[i], points[next], p, extreme))
                {
                    // If the point 'p' is colinear with line
                    // segment 'i-next', then check if it lies
                    // on segment. If it lies, return true, otherwise false
                    if (orientation(points[i], p, points[next]) == 0)
                        return onSegment(points[i], p, points[next]);

                    count++;
                }
                i = next;
            } while (i != 0);

            // Return true if count is odd, false otherwise
            return (count % 2 == 1); // Same as (count%2 == 1)
        }

        public void DrawArea(Point[] Points, int BorderWidth, Color FillColor, Color BoundaryColor)
        {
            //Point Left, Top, Right, Bottom;

            //Left = Top = Right = Bottom = Points[0];
            //for (int i = 1; i < Points.Length; i++)
            //{
            //    // find most left point
            //    if (Points[i].X <= Left.X) 
            //        Left = Points[i];
            //    else
            //        // find most right point
            //        if (Points[i].X >= Right.X) 
            //            Right = Points[i];

            //    // find most highest point  
            //    if (Points[i].Y <= Top.X) 
            //        Top = Points[i];
            //    else
            //        // find the lowest point
            //        if (Points[i].Y >= Bottom.X) 
            //            Bottom = Points[i];
            //}


            // fill 
            var F = new SolidBrush(FillColor);
            _graphics.FillPolygon(F, Points);

            // border
            var B = new Pen(BoundaryColor, BorderWidth);
            _graphics.DrawPolygon(B, Points);
        }

        public void DrawArea(Point[] Points, int BorderWidth) =>
            DrawArea(Points, BorderWidth, Leyout.Back2, Leyout.FixedBorder);

        private Rectangle BoundaryFromPoints(Point[] P)
        {
            int x1 = int.MaxValue, y1 = int.MaxValue, x2 = int.MinValue, y2 = int.MinValue;
            foreach (var item in P)
            {
                if (item.X < x1) x1 = item.X;
                if (item.Y < y1) y1 = item.Y;

                if (item.X > x1) x2 = item.X;
                if (item.Y > y1) y2 = item.Y;
            }

            return new Rectangle(x1, y1, x2, y2);
        }

        //public void DrawArea(Point[] Points, int BorderWidth, Color FillColor1, Color FillColor2, Color BoundaryColor)
        //{
        //    // fill 
        //    var r = BoundaryFromPoints(Points);

        //    LinearGradientBrush lgb =
        //       new LinearGradientBrush(r, FillColor1, FillColor2, (float)Math.PI / 4, true);

        //    _graphics.FillPolygon(lgb, Points);

        //    // border
        //    var B = new Pen(BoundaryColor, BorderWidth);
        //    _graphics.DrawPolygon(B, Points);
        //}

        public void DrawGrid(Point Origin, Size RectSize, int BorderWidth, int Space, Color GridColor)
        {
            var Z = new Point((RectSize.Width % Space) >> 1, (RectSize.Height % Space) >> 1);
            var r = new Rectangle(Origin, RectSize);

            var P = new Pen(GridColor);

            int f = r.Left + (Z.X - (BorderWidth >> 1));
            while (f < r.Right)
            {
                for (int i = 0; i < BorderWidth; i++)
                    _graphics.DrawLine(P, new Point(f + i, r.Top), new Point(f + i, r.Bottom));

                f += Space;
            }

            var g = r.Top + (Z.Y - (BorderWidth >> 1));
            while (g < r.Bottom)
            {
                for (int i = 0; i < BorderWidth; i++)
                    _graphics.DrawLine(P, new Point(r.Left, g + i), new Point(r.Right, g + i));

                g += Space;
            }
        }
        
        public void DrawGrid(Point Origin, Size RectSize, int BorderWidth, int Space) =>
            DrawGrid(Origin, RectSize, BorderWidth, Space, _Leyout.Grid);


        public void Dispose()
        {
            
        }

        public XGraphics(Graphics graphics)
            : base() =>
            _graphics = graphics;
    }
}
