using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tweey.Support
{
    public static class Extensions
    {
        public static Vector2 ToNumericsVector2(this Vector2i v) => new(v.X, v.Y);
    }
}
