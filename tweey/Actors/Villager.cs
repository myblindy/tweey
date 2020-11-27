using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using tweey.Actors.Interfaces;

namespace tweey.Actors
{
    class Villager : IPlaceableEntity
    {
        public Vector2 Location { get; set; }
    }
}
