#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace mini_project
{
    public class Form1 : Form
    {
        const int FormW = 500, FormH = 700;
        const int LaneCount = 3;
        const float VanishY = 150f;
        const float PlayerScreenY = 600f;
        const float PlayerZ = 100f;
        const float FarZ = 0f;
        const float RoadFarZ = 0f;

        int playerLane = 1;
        float jumpHeight = 0, jumpVel = 0;
        bool isJumping = false;
        bool isSliding = false;
        int slideTimer = 0;
        int animFrame = 0, animCnt = 0;
        float gameSpeed = 1.5f;
        int score = 0, coinCount = 0;
        bool gameOver = false, gameStarted = false;
        int highScore = 0;
        bool godMode = false;              // 👉 God Mode flag
        Random rnd = new Random();

        List<GameObj> obstacles = new List<GameObj>();
        List<GameObj> coins = new List<GameObj>();
        List<BgElem> bgElements = new List<BgElem>();

        System.Windows.Forms.Timer timer;
        BufferedGraphicsContext bgCtx;
        BufferedGraphics buffer;

        public Form1()
        {
            ClientSize = new Size(FormW, FormH);
            Text = "Temple Run 3D - mini_project (F1: God Mode)";
            KeyPreview = true;
            DoubleBuffered = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            KeyDown += OnKeyDown;
            Paint += OnPaint;
            Resize += (_, _) => InitBuffer();
            Load += (_, _) => InitBuffer();

            for (int i = 0; i < 15; i++)
                bgElements.Add(new BgElem { Type = 0, Z = rnd.Next(0, 80), X = rnd.Next(-200, FormW + 200), W = rnd.Next(80, 180), H = rnd.Next(40, 90) });
            for (int i = 0; i < 20; i++)
                bgElements.Add(new BgElem { Type = 1, Z = rnd.Next(0, 70), X = rnd.Next(-100, FormW + 100), W = rnd.Next(20, 40), H = rnd.Next(60, 110) });

            timer = new System.Windows.Forms.Timer { Interval = 16 };
            timer.Tick += GameLoop;
            timer.Start();
        }

        void InitBuffer()
        {
            bgCtx ??= BufferedGraphicsManager.Current;
            buffer?.Dispose();
            buffer = bgCtx.Allocate(CreateGraphics(), DisplayRectangle);
        }

        float ZtoScreenY(float z) => VanishY + (PlayerScreenY - VanishY) * Math.Clamp(z / PlayerZ, 0, 1);
        float ZtoScale(float z) => 0.1f + 0.9f * Math.Clamp(z / PlayerZ, 0, 1);

        float ScreenXForLane(int lane, float scale)
        {
            float roadW = FormW * scale;
            float left = (FormW - roadW) / 2f;
            float laneW = roadW / LaneCount;
            return left + lane * laneW + laneW / 2f;
        }

        Rectangle GetObjectRect(GameObj obj)
        {
            float s = ZtoScale(obj.Z), sy = ZtoScreenY(obj.Z);
            float w = obj.W * s;
            float x = ScreenXForLane(obj.Lane, s);

            if (obj.Type == ObjType.Coin)
            {
                float h = obj.H * s;
                return new Rectangle((int)(x - w / 2), (int)(sy - h / 2), (int)w, (int)h);
            }
            else if (obj.Type == ObjType.Tall)
            {
                float h = obj.H * s * 0.7f;
                return new Rectangle((int)(x - w / 2), (int)(sy - obj.H * s), (int)w, (int)h);
            }
            else if (obj.Type == ObjType.Short)
            {
                float h = obj.H * s * 0.5f;
                return new Rectangle((int)(x - w / 2), (int)(sy - h), (int)w, (int)h);
            }
            else
            {
                float h = obj.H * s;
                return new Rectangle((int)(x - w / 2), (int)(sy - h), (int)w, (int)h);
            }
        }

        Rectangle PlayerCollisionRect()
        {
            float w = 40;
            float h = isSliding ? 25 : 60;
            float x = ScreenXForLane(playerLane, 1f);
            float y = PlayerScreenY - jumpHeight;
            return new Rectangle((int)(x - w / 2), (int)(y - h), (int)w, (int)h);
        }

        void GameLoop(object sender, EventArgs e)
        {
            if (!gameStarted) { Invalidate(); return; }
            if (gameOver) { Invalidate(); return; }

            if (isJumping)
            {
                jumpHeight += jumpVel;
                jumpVel -= 0.8f;
                if (jumpHeight <= 0) { jumpHeight = 0; isJumping = false; jumpVel = 0; }
            }
            if (isSliding && --slideTimer <= 0) isSliding = false;

            foreach (var o in obstacles) o.Z += gameSpeed;
            foreach (var c in coins) c.Z += gameSpeed;

            obstacles.RemoveAll(o => o.Z > PlayerZ + 20);
            coins.RemoveAll(c => c.Z > PlayerZ + 20);

            if (rnd.Next(60) < 1)
                obstacles.Add(new GameObj { Z = FarZ, Lane = rnd.Next(LaneCount), Type = (ObjType)rnd.Next(3), W = 45, H = 65 });
            if (rnd.Next(80) < 1)
                coins.Add(new GameObj { Z = FarZ, Lane = rnd.Next(LaneCount), Type = ObjType.Coin, W = 28, H = 28 });

            Rectangle playerRect = PlayerCollisionRect();

            coins.RemoveAll(c =>
            {
                if (c.Lane == playerLane && GetObjectRect(c).IntersectsWith(playerRect))
                {
                    coinCount++;
                    score += 50;
                    Console.Beep(1200, 70);
                    return true;
                }
                return false;
            });

            foreach (var obs in obstacles)
            {
                if (obs.Lane == playerLane && GetObjectRect(obs).IntersectsWith(playerRect))
                {
                    if (!godMode)    // 👉 God Mode එක on නම් game over නෑ
                    {
                        gameOver = true;
                        timer.Stop();
                        if (score > highScore) highScore = score;
                        Console.Beep(200, 300);
                        break;
                    }
                }
            }

            if (++animCnt >= 5) { animCnt = 0; animFrame = (animFrame + 1) % 4; }
            if (gameSpeed < 5.5f) gameSpeed += 0.001f;
            score += (int)(gameSpeed * 0.2f);

            Invalidate();
        }

        void OnPaint(object sender, PaintEventArgs e)
        {
            if (buffer == null) InitBuffer();
            Graphics g = buffer.Graphics;
            g.Clear(Color.SkyBlue);

            DrawBackground(g);
            DrawRoad(g);
            DrawObjects(g);
            DrawPlayer(g);
            DrawUI(g);

            if (!gameStarted) DrawStartScreen(g);
            else if (gameOver) DrawGameOverScreen(g);

            buffer.Render(e.Graphics);
        }

        void DrawBackground(Graphics g)
        {
            g.FillEllipse(Brushes.Yellow, FormW - 80, 30, 60, 60);
            g.FillEllipse(Brushes.White, 100, 40, 130, 40);
            g.FillEllipse(Brushes.White, 320, 60, 90, 30);

            foreach (var bg in bgElements.Where(b => b.Type == 0))
            {
                float s = ZtoScale(bg.Z), sy = ZtoScreenY(bg.Z);
                float w = bg.W * s, h = bg.H * s, x = bg.X * s + FormW / 2 * (1 - s);
                Point[] pts = { new((int)(x - w/2), (int)sy), new((int)(x + w/2), (int)sy), new((int)x, (int)(sy - h)) };
                g.FillPolygon(Brushes.ForestGreen, pts);
            }
            foreach (var bg in bgElements.Where(b => b.Type == 1))
            {
                float s = ZtoScale(bg.Z), sy = ZtoScreenY(bg.Z);
                float w = bg.W * s, h = bg.H * s, x = bg.X * s + FormW / 2 * (1 - s);
                g.FillRectangle(Brushes.SaddleBrown, x - w/6, sy - h*0.65f, w/3, h*0.65f);
                g.FillEllipse(Brushes.Green, x - w/2, sy - h, w, h*0.7f);
            }
        }

        void DrawRoad(Graphics g)
        {
            float roadTopS = ZtoScale(RoadFarZ);
            float roadTopW = FormW * roadTopS;
            float topLeft = (FormW - roadTopW) / 2f;
            PointF[] road = { new(topLeft, VanishY), new(topLeft + roadTopW, VanishY), new(FormW, FormH), new(0, FormH) };
            g.FillPolygon(new SolidBrush(Color.FromArgb(110, 90, 60)), road);
            g.DrawLine(Pens.White, topLeft, VanishY, 0, FormH);
            g.DrawLine(Pens.White, topLeft + roadTopW, VanishY, FormW, FormH);

            for (int i = 1; i < LaneCount; i++)
            {
                float topX = topLeft + roadTopW / LaneCount * i;
                float bottomX = FormW / LaneCount * i;
                using Pen p = new Pen(Color.White, 2) { DashStyle = DashStyle.Dash };
                g.DrawLine(p, topX, VanishY, bottomX, FormH);
            }
        }

        void DrawObjects(Graphics g)
        {
            var all = obstacles.Concat(coins).OrderBy(o => o.Z);
            foreach (var obj in all)
            {
                float s = ZtoScale(obj.Z), sy = ZtoScreenY(obj.Z);
                float w = obj.W * s, h = obj.H * s, x = ScreenXForLane(obj.Lane, s);
                var rect = new RectangleF(x - w/2, sy - h, w, h);
                int alpha = (int)(255 * Math.Min(s * 1.2f, 1f));
                alpha = Math.Max(100, alpha);

                if (obj.Type == ObjType.Coin)
                {
                    using Brush br = new SolidBrush(Color.FromArgb(alpha, 255, 215, 0));
                    g.FillEllipse(br, rect);
                    using Pen p = new Pen(Color.DarkGoldenrod, 2);
                    g.DrawEllipse(p, rect);
                    if (s > 0.4f)
                        g.DrawString("★", new Font("Arial", h * 0.55f, FontStyle.Bold), Brushes.White, rect.X + w * 0.15f, rect.Y + h * 0.1f);
                }
                else if (obj.Type == ObjType.Tall)
                {
                    using Brush br = new SolidBrush(Color.FromArgb(alpha, 220, 50, 0));
                    g.FillRectangle(br, rect);
                    using Pen p = new Pen(Color.Black, 3);
                    g.DrawRectangle(p, rect.X, rect.Y, rect.Width, rect.Height);
                    using Pen hl = new Pen(Color.White, 1);
                    g.DrawLine(hl, rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
                    g.DrawLine