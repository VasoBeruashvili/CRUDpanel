using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CRUDpanel.Models
{
    public class Row
    {
        [JsonProperty("cells")]
        public List<string> Cells { get; set; }
    }
}