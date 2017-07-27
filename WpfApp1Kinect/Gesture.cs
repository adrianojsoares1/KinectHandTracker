using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1Kinect
{
    class Gesture
    {
        public float startX { get; set; }
        public float endX { get; set; }
        public float startY { get; set; }
        public float endY { get; set; }
        public float velocityX { get; set; }
        public float velocityY { get; set; }
        public string direction { get; set; }
     

        public Gesture()
        {
            startX = 0.0f;
            endX = 0.0f;
            startY = 0.0f;
            endY = 0.0f;
            velocityX = 0.0f;
            velocityY = 0.0f;
            direction = "None";
        }
        public Gesture(float sX, float eX, float sY, float eY, int time)
        {
            startX = sX;
            startY = sY;
            endX = eX;
            endY = eY;

            Tuple<float, float, string> stats = calculateVelocity(time);
            velocityX = stats.Item1;
            velocityY = stats.Item2;
            direction = stats.Item3;         
        }
        public Tuple<float, float, string> calculateVelocity(float timeMS)
        {
            float dirX = (startX - endX) / timeMS;
            float dirY = (startY - endY) / timeMS;
            string direction = dirX > 0 ? "Swipe Left" : "Swipe Right";
            return new Tuple<float, float, string>(dirX, dirY, direction);
        }

    }   
}
