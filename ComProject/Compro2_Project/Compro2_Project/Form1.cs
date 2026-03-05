using System.Reflection.Emit;
using System.IO;

namespace Compro2_Project
{
    public partial class Form1 : Form
    {
        // --- ตัวแปรระบบเลือดและคะแนน ---
        int playerHP = 5;
        int score = 0;

        // --- ตัวแปรจำนวนมอนที่ตีตาย ---
        int slimeKills = 0; // นับจำนวนสไลม์ที่ตีตาย
        int batKills = 0;   // นับจำนวนค้างคาวที่ตีตาย

        // ช่วงเวลาที่อนุโลมให้กดได้ (ยิ่งค่าน้อย เกมยิ่งเล่นยาก ต้องกดเป๊ะขึ้น)
        float hitTolerance = 150f; // 150 มิลลิวินาที

        Image imgBackground; // ตัวแปรเก็บฉากหลัง

        // --- ตัวแปรสำหรับรูปภาพ Player ---
        Image imgPlayerIdle;     // ภาพท่ายืนปกติ
        Image imgPlayerHitUp;    // ภาพท่าตีเลนบน (ค้างคาว)
        Image imgPlayerHitDown;  // ภาพท่าตีเลนล่าง (สไลม์)
        Image currentPlayerImg;  // ภาพที่กำลังแสดงผลอยู่ ณ ปัจจุบัน

        // นาฬิกาจับเวลาที่แม่นยำระดับมิลลิวินาที (หัวใจของ Rhythm Game)
        System.Diagnostics.Stopwatch audioStopwatch = new System.Diagnostics.Stopwatch();

        // กระเป๋าเก็บมอนสเตอร์ทั้งหมดในฉาก
        List<Monster> activeMonsters = new List<Monster>();

        // ตัวแปรเก็บรูปมอนสเตอร์
        Image imgSlime;
        Image currentSlimeImg;
        Image imgBat;
        Image currentBatImg;

        // ความเร็วของมอนสเตอร์ (0.5 พิกเซล ต่อ 1 มิลลิวินาที)
        float noteSpeed = 0.5f;
        int hitZoneX = 150; // พิกัดแกน X ที่ผู้เล่นยืนอยู่

        // Boss variables (ถ้าคุณอยากเพิ่มบอสในอนาคต)
        int bossHP = 50; // เลือดบอส
        Image imgBossIdle;  // รูปบอสยืนปกติ
        Image imgBossThrow; // รูปบอสตอนโยนของ
        Image currentBossImg;

        // ------ ระบบเสียงแบบใหม่ (เวอร์ชันนักสืบ หา Error) ------
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern long mciSendString(string command, System.Text.StringBuilder returnValue, int returnLength, IntPtr winHandle);

        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern bool mciGetErrorString(long mciError, System.Text.StringBuilder errorText, int errorTextSize);

        private void PlayAudio(string alias, string filepath, int volume = 1000, bool loop = false)
        {
            if (!System.IO.File.Exists(filepath))
            {
                MessageBox.Show($"หาไฟล์เสียงไม่เจอ: {filepath}\nกรุณาเช็คว่ากด Copy to Output Directory หรือยัง", "Error 1: File Not Found");
                return;
            }

            string fullPath = System.IO.Path.GetFullPath(filepath);
            mciSendString($"close {alias}", null, 0, IntPtr.Zero);

            long resultOpen = mciSendString($"open \"{fullPath}\" type mpegvideo alias {alias}", null, 0, IntPtr.Zero);

            if (resultOpen != 0)
            {
                System.Text.StringBuilder errorMsg = new System.Text.StringBuilder(256);
                mciGetErrorString(resultOpen, errorMsg, 256);
                MessageBox.Show($"เปิดไฟล์ไม่ได้: {filepath}\nสาเหตุที่ Windows แจ้ง: {errorMsg}", "Error 2: MCI Open Failed");
                return;
            }

            mciSendString($"setaudio {alias} volume to {volume}", null, 0, IntPtr.Zero);

            string playCommand = loop ? $"play {alias} repeat" : $"play {alias} from 0";
            long resultPlay = mciSendString(playCommand, null, 0, IntPtr.Zero);

            if (resultPlay != 0)
            {
                System.Text.StringBuilder errorMsg = new System.Text.StringBuilder(256);
                mciGetErrorString(resultPlay, errorMsg, 256);
                MessageBox.Show($"เล่นเสียงไม่ได้: {filepath}\nสาเหตุที่ Windows แจ้ง: {errorMsg}", "Error 3: MCI Play Failed");
            }
        }

        // ฟังก์ชันสำหรับดึงความยาวไฟล์เพลง (หน่วยเป็นมิลลิวินาที)
        private float GetAudioLength(string alias)
        {
            System.Text.StringBuilder lengthBuf = new System.Text.StringBuilder(256);
            // สั่งให้ Windows คายความยาวของเสียงที่ชื่อ alias ออกมาเก็บใน lengthBuf
            mciSendString($"status {alias} length", lengthBuf, lengthBuf.Capacity, IntPtr.Zero);

            float length = 0;
            float.TryParse(lengthBuf.ToString(), out length);
            return length;
        }
        // -----------------------------------------------

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // ดึงเวลาปัจจุบันจากนาฬิกา
            float currentTime = audioStopwatch.ElapsedMilliseconds;

            if (e.KeyCode == Keys.F) // เลนบน (ค้างคาว)
            {
                currentPlayerImg = imgPlayerHitUp;
                CheckHit(0, currentTime); // ส่งค่า 0 เพื่อบอกว่าเช็คเลนบน
            }
            else if (e.KeyCode == Keys.J) // เลนล่าง (สไลม์)
            {
                currentPlayerImg = imgPlayerHitDown;
                CheckHit(1, currentTime); // ส่งค่า 1 เพื่อบอกว่าเช็คเลนล่าง
            }

            pbCanvas.Invalidate();
        }

        // เมื่อผู้เล่น "ปล่อย" ปุ่ม
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            // ไม่ว่าจะปล่อยปุ่มไหน ก็ให้กลับมายืนท่าปกติ
            if (e.KeyCode == Keys.F || e.KeyCode == Keys.J)
            {
                currentPlayerImg = imgPlayerIdle;
                pbCanvas.Invalidate(); // สั่งวาดหน้าจอใหม่
            }
        }

        private void TakeDamage()
        {
            // ถ้าเลือดหมดไปแล้ว (Game Over แล้ว) ให้ข้ามไปเลย ป้องกันการทำงานซ้ำซ้อน
            if (playerHP <= 0) return;

            playerHP--; // ลดเลือดลงทีละ 1
            lblHP.Text = "HP: " + playerHP; // อัปเดตตัวเลขบนจอ

            // เล่นเสียงตอนโดนตี (ถ้ามีไฟล์เสียง)
            //PlayAudio("missSound", "hurt.wav", 1000);

            // เช็คเงื่อนไขตาย
            if (playerHP <= 0)
            {
                // 1. หยุดการทำงานของ Loop เกมทั้งหมดทันที!
                gameTimer.Stop();
                audioStopwatch.Stop();

                // 2. หยุดเล่นเพลง BGM
                mciSendString("stop bgm", null, 0, IntPtr.Zero);

                // 3. แสดงหน้าต่างแจ้งเตือน (พอมันหยุดทุกอย่างแล้ว ค่อยเด้งขึ้นมา จะไม่ Error ครับ)
                MessageBox.Show("Game Over! คุณเสียเลือดจนหมด", "จบเกม");

                // 4. (แถมให้) เมื่อกด OK ปิดหน้าต่าง Game Over แล้ว ให้เริ่มเกมใหม่ทั้งหมด
                // Application.Restart(); // ถ้าอยากให้เริ่มเกมใหม่ทันที เอาเครื่องหมาย // ออกได้เลยครับ
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // 1. วาดฉากหลังก่อนเพื่อนเลย
            if (imgBackground != null)
            {
                e.Graphics.DrawImage(imgBackground, 0, 0, 1280, 720);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.DarkSlateGray, 0, 0, 1280, 720);
            }

            // 2. วาดเส้นแบ่งเลน
            e.Graphics.DrawLine(Pens.Gray, 0, 250, 1280, 250);
            e.Graphics.DrawLine(Pens.Gray, 0, 450, 1280, 450);

            // 3. วาด Player ที่พิกัด X=150, Y=350
            if (currentPlayerImg != null)
            {
                e.Graphics.DrawImage(currentPlayerImg, 150, 274, 192, 192);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.Blue, 150, 350, 64, 64);
            }

            // 4. วาดบอสที่ฝั่งขวาของจอ
            if (currentBossImg != null)
            {
                e.Graphics.DrawImage(currentBossImg, 1000, 250, 150, 150);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.DarkRed, 1000, 300, 150, 150);
            }

            // 5. วาดมอนสเตอร์และของบอส (รวบลูปมาไว้ที่เดียวกัน)
            foreach (var mon in activeMonsters)
            {
                // วาดเฉพาะตัวที่ยังไม่โดนตี (หรือกำลังสะท้อนกลับ) และยังไม่หลุดขอบจอซ้าย
                if ((!mon.IsHit || mon.IsReflected) && mon.X > -100)
                {
                    if (mon.Type == "Bat")
                    {
                        // **ข้อควรระวัง:** ตรวจสอบให้แน่ใจว่าคุณมีตัวแปร imgBat และโหลดรูปมาแล้ว
                        if (currentBatImg != null)
                        {
                            e.Graphics.DrawImage(currentBatImg, mon.X, mon.Y, 100, 100); // ใช้ mon.X, mon.Y เสมอ
                        }
                        else
                        {
                            e.Graphics.FillRectangle(Brushes.Purple, mon.X, mon.Y, 80, 80);
                        }
                    }
                    else if (mon.Type == "Slime")
                    {
                        if (currentSlimeImg != null)
                        {
                            e.Graphics.DrawImage(currentSlimeImg, mon.X, mon.Y, 100, 100);
                        }
                        else
                        {
                            e.Graphics.FillRectangle(Brushes.Lime, mon.X, mon.Y, 80, 80);
                        }
                    }
                    else if (mon.Type == "BossItem")
                    {
                        // ของบอสวาดเป็นวงกลมสีส้ม
                        e.Graphics.FillEllipse(Brushes.Orange, mon.X, mon.Y, 80, 80);
                    }
                }
            }
        }

        private void gameTimer_Tick(object sender, EventArgs e)
        {
            float currentTime = audioStopwatch.ElapsedMilliseconds;

            foreach (var mon in activeMonsters)
            {
                // 1. ตรวจสอบว่าวัตถุอยู่ในสถานะ "สะท้อนกลับ" หรือไม่
                if (mon.IsReflected)
                {
                    // วิธีทำ: เปลี่ยนสมการเคลื่อนที่ ให้แกน X เพิ่มขึ้นอย่างรวดเร็ว (พุ่งไปทางขวา)
                    mon.X += 150f; // ความเร็ว 15 พิกเซลต่อเฟรม

                    // ตรวจสอบว่าชนบอสหรือยัง (บอสอยู่พิกัด X = 1000)
                    if (mon.X >= 1000)
                    {
                        mon.IsReflected = false; // หยุดทำงาน
                        mon.X = -500; // ซ่อนของไว้รอการลบทิ้ง
                        BossTakeDamage(); // เรียกฟังก์ชันลดเลือดบอส
                    }
                }
                else // 2. ถ้าไม่ได้สะท้อนกลับ ให้ใช้สมการวิ่งตามจังหวะเพลงเหมือนเดิม
                {
                    mon.X = hitZoneX + (noteSpeed * (mon.HitTime - currentTime));
                }

                // ระบบเสียเลือดเมื่อปล่อยหลุด (โค้ดเดิมของคุณ)
                if (!mon.IsHit && !mon.IsReflected && currentTime > mon.HitTime + hitTolerance)
                {
                    mon.IsHit = true;
                    TakeDamage();
                }
            }

            // 👉 เช็คว่าเวลาปัจจุบัน เดินมาถึงเวลาจบด่าน (ความยาวของไฟล์เพลง) หรือยัง
            // บวกเวลาเผื่อไปอีกนิดหน่อย (เช่น 500ms) เผื่อเสียงดนตรีเฟดลง (Fade out)
            if (gameEndTime > 0 && currentTime >= (gameEndTime + 500))
            {
                gameTimer.Stop();       // หยุดเกม
                audioStopwatch.Stop();  // หยุดนาฬิกา
                mciSendString("stop bgm", null, 0, IntPtr.Zero); // สั่งหยุดเพลงให้ชัวร์

                // สรุปผลคะแนนเมื่อเพลงจบ
                MessageBox.Show($"เพลงจบแล้ว! เคลียร์ด่านสำเร็จ!\n\nคะแนนรวม: {score}\nสไลม์ที่กำจัด: {slimeKills}\nค้างคาวที่กำจัด: {batKills}", "Stage Clear!");
            }

            pbCanvas.Invalidate();
        }

        private void CheckHit(int targetLane, float currentTime)
        {
            // 1. หามอนสเตอร์ตัวที่ยังไม่ตาย และอยู่ในเลนที่เรากด (0=บน, 1=ล่าง)
            // โดยเลือกตัวที่เวลา HitTime ใกล้จะถึงมากที่สุด (ตัวหน้าสุด)
            var target = activeMonsters.OrderBy(m => m.HitTime)
                                       .FirstOrDefault(m => m.Lane == targetLane && !m.IsHit);

            // ถ้าเจอมอนสเตอร์
            if (target != null)
            {
                float timeDifference = Math.Abs(target.HitTime - currentTime);

                if (timeDifference <= hitTolerance)
                {
                    target.IsHit = true;

                    //PlayAudio("hitSound", "hit.wav", 800);

                    // เพิ่มการตรวจสอบประเภทตรงนี้ครับ:
                    if (target.Type == "BossItem")
                    {
                        // ถ้าเป็นของที่บอสโยนมา ให้ตั้งค่า IsReflected เป็น true
                        // มันจะถูกนำไปคำนวณให้พุ่งกลับไปขวาใน gameTimer_Tick
                        target.IsReflected = true;
                    }
                    else
                    {
                        // ถ้าเป็นมอนสเตอร์ปกติ ก็ตายตามปกติ
                        score += 10;
                        if (target.Type == "Bat") batKills++;
                        else if (target.Type == "Slime") slimeKills++;
                    }

                    lblScore.Text = "Score: " + score;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // ใช้ฟังก์ชันตัวช่วยโหลดรูปทั้งหมดในรวดเดียว (สั้นและสะอาดมาก!)
            imgBossIdle = LoadImageSafely("boss_idle.png");
            imgBossThrow = LoadImageSafely("boss_throw.png"); // สมมติว่ามีภาพตอนโยนของ
            imgPlayerIdle = LoadImageSafely("idle.png");
            imgPlayerHitUp = LoadImageSafely("hit_up.png");
            imgPlayerHitDown = LoadImageSafely("hit_down.png");
            imgBat = LoadImageSafely("bat.png");
            imgSlime = LoadImageSafely("slime.png");
            imgBackground = LoadImageSafely("bg.png");

            // โหลดรูปภาพ
            currentPlayerImg = imgPlayerIdle;
            currentBossImg = imgBossIdle;
            currentSlimeImg = imgSlime;
            currentBatImg = imgBat;

            // ตั้งค่า Label ให้อยู่บน PictureBox
            lblHP.Parent = pbCanvas;
            lblScore.Parent = pbCanvas;
            lblHP.BackColor = Color.Transparent;
            lblHP.Location = new Point(10, 10);

            // 👉 โหลดภาพฉากหลัง
            if (System.IO.File.Exists("bg.png"))
            {
                imgBackground = Image.FromFile("bg.png");
            }

            // 1. สร้างมอนสเตอร์จาก Beatmap
            LoadBeatmap();

            // 1. เริ่มเล่นเพลง BGM 
            // **ข้อสำคัญ:** ต้องเปลี่ยนค่าตัวสุดท้ายจาก true เป็น false เพื่อไม่ให้เพลงวนลูป (เพราะเราต้องการให้มันจบ)
            PlayAudio("bgm", "MEIKO.wav", 20, false);

            // 2. ดึงความยาวของเพลง "bgm" มากำหนดเป็นเวลาจบเกมโดยอัตโนมัติ!
            gameEndTime = GetAudioLength("bgm");

            // 3. เริ่มเกม
            audioStopwatch.Start();
            gameTimer.Start();
        }

        private async void BossTakeDamage()
        {
            bossHP -= 10; // เลือดบอสลดทีละ 5

            // --- เพิ่ม Effect บอสโดนตี ---
            // 1. เล่นเสียงบอสโดนตี (อย่าลืมเตรียมไฟล์ boss_hurt.wav ไว้ในโฟลเดอร์)
            //PlayAudio("bossHurt", "boss_hurt.wav", 1000);

            // 2. เปลี่ยนรูปบอสเป็นตอนโดนตี (สมมติว่าคุณโหลด imgBossHit ไว้ตอนเริ่มเกมแล้ว)
            // currentBossImg = imgBossHit; // ถ้ามีรูปให้ปลดคอมเมนต์บรรทัดนี้
            pbCanvas.Invalidate(); // สั่งวาดหน้าจอทันทีเพื่อให้เห็นบอสเปลี่ยนรูป

            // 3. หน่วงเวลา 200 มิลลิวินาที (0.2 วินาที)
            await Task.Delay(200);

            // 4. เปลี่ยนรูปบอสกลับเป็นท่ายืนปกติ
            currentBossImg = imgBossIdle;
            pbCanvas.Invalidate(); // สั่งวาดหน้าจออีกรอบ
                                   // ----------------------------

            // ตรวจสอบเงื่อนไขบอสหลอดเลือดหมด
            if (bossHP <= 0)
            {
                gameTimer.Stop(); // หยุดการอัปเดตเกม

                // ใช้คำสั่งหยุดเพลงของระบบใหม่ อ้างอิงจากชื่อ "bgm" ที่เราตั้งไว้ตอนกด Play
                mciSendString("stop bgm", null, 0, IntPtr.Zero);

                MessageBox.Show("ยินดีด้วย! คุณปราบลอสและเคลียร์ดันเจี้ยนสำเร็จ!", "ชนะแล้ว");
            }
        }

        private void LoadBeatmap()
        {
            activeMonsters.Clear();
            gameEndTime = 0; // รีเซ็ตเวลาจบด่านใหม่ทุกครั้งที่โหลดด่าน
            string filePath = "map.txt";

            if (System.IO.File.Exists(filePath))
            {
                string[] mapData = System.IO.File.ReadAllLines(filePath);
                foreach (string data in mapData)
                {
                    if (string.IsNullOrWhiteSpace(data)) continue;

                    string[] parts = data.Split(',');
                    if (parts.Length == 3)
                    {
                        float time = float.Parse(parts[0]);
                        string type = parts[1];
                        int lane = int.Parse(parts[2]);
                        float startY = (lane == 0) ? 200f : 370f;

                        activeMonsters.Add(new Monster(type, lane, time, startY));

                        // 👉 เพิ่มตรงนี้: อัปเดตเวลาจบด่านให้เท่ากับมอนสเตอร์ตัวที่ออกช้าที่สุด
                        if (time > gameEndTime)
                        {
                            gameEndTime = time;
                        }
                    }
                }

                // 👉 เมื่อรู้เวลามอนสเตอร์ตัวสุดท้ายแล้ว บวกเผื่อเวลาให้มันวิ่งทะลุจอไปอีก 4 วินาที (4000 ms) ค่อยจบเกม
                gameEndTime += 4000;
            }
        }

        // ฟังก์ชันนี้จะรับชื่อไฟล์เข้าไป แล้วคาย Image กลับออกมาให้
        private Image LoadImageSafely(string fileName)
        {
            // เช็คว่ามีไฟล์รูปนี้อยู่จริงไหม
            if (System.IO.File.Exists(fileName))
            {
                return Image.FromFile(fileName); // ถ้ามี ให้ส่งรูปกลับไป
            }
            else
            {
                // ถ้าไม่มี ให้แจ้งเตือนพร้อมระบุชื่อไฟล์ที่หายไป
                MessageBox.Show($"หารูป {fileName} ไม่เจอ! โปรแกรมจะใช้กล่องสีแทนชั่วคราว", "Image Error");
                return null; // ส่งค่าความว่างเปล่ากลับไป (เพื่อที่ตอนวาด e.Graphics จะได้วาดกล่องสีแทน)
            }
        }
    }
}
