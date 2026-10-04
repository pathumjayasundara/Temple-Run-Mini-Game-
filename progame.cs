
        System.Windows.Forms.Timer timer;
        BufferedGraphicsContext bgCtx;
        BufferedGraphics buffer;

        public Form1()
        {
            ClientSize = new Size(FormW, FormH);
            Text = "Temple Run 3D – mini_project";
            KeyPreview = true;
            DoubleBuffered = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            KeyDown += OnKeyDown;
            Paint += OnPaint;
            Resize += (_, _) => InitBuffer();
            Load += (_, _) => InitBuffer();

            GenerateBackground();

            timer = new System.Windows.Forms.Timer { Interval = 16 }; // 60 FPS
            timer.Tick += GameLoop;
            timer.Start();
        }

        void InitBuffer()
        {
            bgCtx ??= BufferedGraphicsManager.Current;
            buffer?.Dispose();
            buffer = bgCtx.Allocate(CreateGraphics(), DisplayRectangle);
        }

        void GenerateBackground()
        {
            // Mountains
            for (int i = 0; i < 12; i++)
                bgElements.Add(new BgElem { Type = 0, Z = rnd.Next(200, 500), X = rnd.Next(-200, FormW + 200), W = rnd.Next(80, 200), H = rnd.Next(40, 100) });
            // Trees
            for (int i = 0; i < 15; i++)
                bgElements.Add(new BgElem { Type = 1, Z = rnd.Next(120, 350), X = rnd.Next(-100, FormW + 100), W = rnd.Next(20, 40), H = rnd.Next(50, 100) });
        }

        // ========== Projection helpers ==========
        static float ZtoScreenY(float z) => VanishY + (PlayerScreenY - VanishY) * Math.Clamp((z - RoadTopZ) / (PlayerZ - RoadTopZ), 0, 1);
        static float ZtoScale(float z) => 0.1f + 0.9f * Math.Clamp((z - RoadTopZ) / (PlayerZ - RoadTopZ), 0, 1);

        static float ScreenXForLane(int lane, float scale)
        {
            float roadW = FormW * scale;
            float left = (FormW - roadW) / 2;
            float laneW = roadW / LaneCount;
            return left + lane * laneW + laneW / 2;
        }

        Rectangle GetObjectRect(GameObj obj)
        {
            float s = ZtoScale(obj.Z), sy = ZtoScreenY(obj.Z);
            float w = obj.W * s, h = obj.H * s;
            float x = ScreenXForLane(obj.Lane, s);
            return new Rectangle((int)(x - w / 2), (int)(sy - h / 2), (int)w, (int)h);
        }

        Rectangle PlayerRect()
        {
            float w = 40, h = isSliding ? 25 : 60;
            float x = ScreenXForLane(playerLane, 1f);
            float y = PlayerScreenY - jumpHeight;
            return new Rectangle((int)(x - w / 2), (int)(y - h), (int)w, (int)h);
        }

        // ========== Game Loop ==========
        void GameLoop(object sender, EventArgs e)
        {
            if (!gameStarted) { Invalidate(); return; }
            if (gameOver) { Invalidate(); return; }

            // Jump physics
            if (isJumping)
            {
                jumpHeight += jumpVel;
                jumpVel -= 0.8f;
                if (jumpHeight <= 0) { jumpHeight = 0; isJumping = false; jumpVel = 0; }
            }
            // Slide timer
            if (isSliding && --slideTimer <= 0) isSliding = false;

            // Move objects (Z decreases = approaching player)
            foreach (var o in obstacles) o.Z -= gameSpeed;
            foreach (var c in coins) c.Z -= gameSpeed;

            // Remove passed objects
            obstacles.RemoveAll(o => o.Z <= PlayerZ);
            coins.RemoveAll(c => c.Z <= PlayerZ);

            // Spawn obstacles (random lane, random type)
            if (rnd.Next(60) < 2)
            {
                obstacles.Add(new GameObj
                {
                    Z = MaxZ,
                    Lane = rnd.Next(LaneCount),
                    Type = (ObjType)rnd.Next(3), // 0:Tall,1:Short,2:Medium
                    W = 30,
                    H = 40
                });
            }
            // Spawn coins
            if (rnd.Next(90) < 2)
            {
                coins.Add(new GameObj { Z = MaxZ, Lane = rnd.Next(LaneCount), Type = ObjType.Coin, W = 20, H = 20 });
            }

            // Collision detection
            Rectangle pRect = PlayerRect();

            // Coins collection
            coins.RemoveAll(c =>
            {
                if (c.Lane == playerLane && GetObjectRect(c).IntersectsWith(pRect))
                {
                    coinScore += 50;
                    score += 50;
                    Console.Beep(1500, 80); // coin sound (optional)
                    return true;
                }
                return false;
            });

            // Obstacle collision
            foreach (var obs in obstacles)
            {
                if (obs.Lane == playerLane && GetObjectRect(obs).IntersectsWith(pRect))
                {
                    gameOver = true;
                    timer.Stop();
                    if (score > highScore) highScore = score;
                    Console.Beep(200, 200); // crash sound (optional)
                    break;
                }
            }

            // Player animation
            if (++animCnt >= 5) { animCnt = 0; animFrame = (animFrame + 1) % 4; }

            // Speed increase
            if (gameSpeed < 12f) gameSpeed += 0.002f;
            score += (int)(gameSpeed * 0.3f);

            Invalidate();
        }

        // ========== Drawing ==========
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
            // Sun
            g.FillEllipse(Brushes.Yellow, FormW - 80, 30, 60, 60);
            // Clouds
            g.FillEllipse(Brushes.White, 100, 40, 120, 40);
            g.FillEllipse(Brushes.White, 300, 60, 80, 30);

            // Mountains
            foreach (var bg in bgElements.Where(b => b.Type == 0))
            {
                float s = ZtoScale(bg.Z), sy = ZtoScreenY(bg.Z);
                float w = bg.W * s, h = bg.H * s, x = bg.X * s + FormW / 2 * (1 - s);
                Point[] pts = { new((int)(x - w/2), (int)sy), new((int)(x + w/2), (int)sy), new((int)x, (int)(sy - h)) };
                g.FillPolygon(Brushes.ForestGreen, pts);
            }
            // Trees
            foreach (var bg in bgElements.Where(b => b.Type == 1))
            {
                float s = ZtoScale(bg.Z), sy = ZtoScreenY(bg.Z);
                float w = bg.W * s, h = bg.H * s, x = bg.X * s + FormW / 2 * (1 - s);
                g.FillRectangle(Brushes.SaddleBrown, x - w/6, sy - h*0.7f, w/3, h*0.7f); // trunk
                g.FillEllipse(Brushes.Green, x - w/2, sy - h, w, h*0.8f);               // leaves
            }
        }

        void DrawRoad(Graphics g)
        {
            float topS = ZtoScale(RoadTopZ);
            float roadTopW = FormW * topS;
            float topLeft = (FormW - roadTopW) / 2;
            PointF[] road = { new(topLeft, VanishY), new(topLeft + roadTopW, VanishY), new(FormW, FormH), new(0, FormH) };
            g.FillPolygon(new SolidBrush(Color.FromArgb(120, 100, 70)), road);
            // Road edges
            g.DrawLine(Pens.White, topLeft, VanishY, 0, FormH);
            g.DrawLine(Pens.White, topLeft + roadTopW, VanishY, FormW, FormH);
            // Lane dividers (dashed)
            for (int i = 1; i < LaneCount; i++)
            {
                float topX = topLeft + roadTopW / LaneCount * i;
                float bottomX = FormW / LaneCount * i;
                using Pen p = new(Color.White, 2) { DashStyle = DashStyle.Dash };
                g.DrawLine(p, topX, VanishY, bottomX, FormH);
            }
        }

        void DrawObjects(Graphics g)
        {
            // Combine and sort far to near (Z descending)
            var all = obstacles.Concat(coins).OrderByDescending(o => o.Z);
            foreach (var obj in all)
            {
                float s = ZtoScale(obj.Z), sy = ZtoScreenY(obj.Z);
                float w = obj.W * s, h = obj.H * s, x = ScreenXForLane(obj.Lane, s);
                var rect = new RectangleF(x - w/2, sy - h/2, w, h);
                int alpha = (int)(255 * Math.Min(s * 1.5f, 1f));
                alpha = Math.Max(50, alpha);

                if (obj.Type == ObjType.Coin)
                {
                    using Brush br = new SolidBrush(Color.FromArgb(alpha, 255, 215, 0));
                    g.FillEllipse(br, rect);
                    g.DrawEllipse(Pens.DarkGoldenrod, rect.X, rect.Y, rect.Width, rect.Height);
                    if (s > 0.5f) // near enough to show symbol
                        g.DrawString("$", new Font("Arial", h * 0.6f, FontStyle.Bold), Brushes.White, rect.X + w * 0.2f, rect.Y + 2);
                }
                else
                {
                    Color c = obj.Type switch
                    {
                        ObjType.Tall => Color.FromArgb(alpha, 139, 69, 19),
                        ObjType.Short => Color.FromArgb(alpha, 200, 50, 50),
                        _ => Color.FromArgb(alpha, 205, 133, 63)
                    };
                    using Brush br = new SolidBrush(c);
                    g.FillRectangle(br, rect);
                    g.DrawRectangle(Pens.Black, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        void DrawPlayer(Graphics g)
        {
            float w = 40, h = isSliding ? 25 : 60;
            float x = ScreenXForLane(playerLane, 1f);
            float y = PlayerScreenY - jumpHeight;

            // Shadow
            g.FillEllipse(Brushes.Black, x - w/2 + 5, y - 2, w - 10, 6);

            if (isSliding)
            {
                g.FillRectangle(Brushes.DodgerBlue, x - w/2, y - h, w, h);
                // Simple face
                g.FillEllipse(Brushes.PeachPuff, x - 8, y - h - 10, 16, 16);
                return;
            }

            // Normal/Jumping stance
            int legOff = animFrame % 2 == 0 ? 8 : -8;
            // Body
            g.FillRectangle(Brushes.DodgerBlue, x - 15, y - 50, 30, 30);
            // Head
            g.FillEllipse(Brushes.PeachPuff, x - 12, y - 70, 24, 24);
            g.FillEllipse(Brushes.White, x - 5, y - 64, 6, 6);
            g.FillEllipse(Brushes.Black, x - 3, y - 62, 2, 2);
            // Arms
            g.FillRectangle(Brushes.PeachPuff, x - 25, y - 45, 8, 20);
            g.FillRectangle(Brushes.PeachPuff, x + 17, y - 45, 8, 20);
            // Legs
            g.FillRectangle(Brushes.Black, x - 12, y - 20, 10, 25 + legOff);
            g.FillRectangle(Brushes.Black, x + 2, y - 20, 10, 25 - legOff);
        }

        void DrawUI(Graphics g)
        {
            g.DrawString($"Score: {score}", new Font("Arial", 14, FontStyle.Bold), Brushes.White, 10, 10);
            g.DrawString($"Coins: {coinScore / 50}", new Font("Arial", 14), Brushes.Yellow, 10, 30);
            g.DrawString($"Hi-Score: {highScore}", new Font("Arial", 10), Brushes.LightGray, 10, 50);
        }

        static void DrawStartScreen(Graphics g)
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), 0, 0, FormW, FormH);
            g.DrawString("TEMPLE RUN 3D", new Font("Arial", 30, FontStyle.Bold), Brushes.Gold, 80, 200);
            g.DrawString("Press SPACE to Start", new Font("Arial", 16), Brushes.White, 140, 300);
            g.DrawString("A/D or Arrows : Move Lane", new Font("Arial", 12), Brushes.White, 120, 350);
            g.DrawString("SPACE : Jump, S/Down : Slide", new Font("Arial", 12), Brushes.White, 100, 380);
        }

        void DrawGameOverScreen(Graphics g)
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), 0, 0, FormW, FormH);
            g.DrawString("GAME OVER", new Font("Arial", 30, FontStyle.Bold), Brushes.Red, 120, 250);
            g.DrawString($"Score: {score}   Hi-Score: {highScore}", new Font("Arial", 16), Brushes.White, 100, 320);
            g.DrawString("Press R to Restart", new Font("Arial", 14), Brushes.White, 150, 370);
        }

        // ========== Keyboard ==========
        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!gameStarted && e.KeyCode == Keys.Space) { StartNewGame(); return; }
            if (gameOver && e.KeyCode == Keys.R) { StartNewGame(); return; }
            if (gameOver || !gameStarted) return;

            switch (e.KeyCode)
            {
                case Keys.A: case Keys.Left:
                    if (playerLane > 0) playerLane--;
                    break;
                case Keys.D: case Keys.Right:
                    if (playerLane < LaneCount - 1) playerLane++;
                    break;
                case Keys.Space:
                    if (!isJumping && jumpHeight == 0 && !isSliding)
                    {
                        isJumping = true;
                        jumpVel = 12f;
                        Console.Beep(800, 100);
                    }
                    break;
                case Keys.S: case Keys.Down:
                    if (!isSliding && jumpHeight == 0)
                    {
                        isSliding = true;
                        slideTimer = 20;
                        Console.Beep(600, 50);
                    }
                    break;
            }
        }

        void StartNewGame()
        {
            gameStarted = true;
            gameOver = false;
            obstacles.Clear();
            coins.Clear();
            score = 0;
            coinScore = 0;
            playerLane = 1;
            jumpHeight = 0;
            isJumping = false;
            isSliding = false;
            gameSpeed = 5.0f;
            timer.Start();
        }
    }

    // ========== Helper Types ==========
    public enum ObjType { Tall, Short, Medium, Coin = 10 }

    public class GameObj
    {
        public float Z { get; set; }
        public int Lane { get; set; }
        public ObjType Type { get; set; }
        public float W { get; set; } = 30;
        public float H { get; set; } = 40;
    }

    public class BgElem
    {
        public int Type { get; set; } // 0=mountain,1=tree
        public float Z { get; set; }
        public int X { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }
}
