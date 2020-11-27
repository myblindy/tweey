using System.Numerics;
using tweey.Actors;
using tweey.Actors.Interfaces;
using tweey.Loaders;

namespace tweey.Actors
{
    record ResourceBucket(params ResourceQuantity[] Resources) : IPlaceableEntity
    {
        public Vector2 Location { get; set; }
    }
}
