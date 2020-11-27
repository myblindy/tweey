using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tweey.Loaders
{
    class Resource
    {
        public string Name { get; set; }
        public int StackSize { get; set; }
        public double Weight { get; set; }
    }

    record ResourceQuantity(Resource Resource, double Quantity)
    {
    }

    class ResourceTemplates : BaseTemplates<Resource>
    {
        public ResourceTemplates(ILoader loader) : base(loader, "Resources", x => x.Name)
        {
        }
    }
}
