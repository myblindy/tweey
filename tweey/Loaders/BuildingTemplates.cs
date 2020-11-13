using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace tweey.Loaders
{
    enum BuildingType { WorkPlace, Storage }

    class BuildingTemplate
    {
        public string Name { get; set; }
        public BuildingType Type { get; set; }
    }

    class BuildingTemplates : BaseTemplates<BuildingTemplate>
    {
        public BuildingTemplates(ILoader loader) : base(loader, "Buildings", x => x.Name)
        {
        }
    }
}
