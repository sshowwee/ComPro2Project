using System;
using System.Collections.Generic;
using System.Text;

namespace Compro2_Project
{
    public class Monster
    {
        public string Type { get; set; } // "Slime" หรือ "Bat"
        public int Lane { get; set; }    // 0 = เลนบน, 1 = เลนล่าง
        public float HitTime { get; set; } // เวลาที่ต้องถูกตีให้ตรงจังหวะ (มิลลิวินาที)
        public float X { get; set; }     // พิกัดแกน X
        public float Y { get; set; }     // พิกัดแกน Y
        public bool IsHit { get; set; } = false; // โดนผู้เล่นตีตายไปหรือยัง?
        public bool IsReflected { get; set; } = false; // เพิ่มตัวแปรนี้เพื่อเช็คว่าของชิ้นนี้กำลังลอยกลับไปหาบอสหรือไม่

        // Constructor กำหนดค่าตอนสร้างมอนสเตอร์
        public Monster(string type, int lane, float hitTime, float startY)
        {
            Type = type;
            Lane = lane;
            HitTime = hitTime;
            Y = startY;
            X = 1280; // ให้มอนสเตอร์ทุกตัวเริ่มต้นที่ขอบจอฝั่งขวาสุดเสมอ
        }
    }
}
