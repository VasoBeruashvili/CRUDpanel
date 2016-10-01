using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CRUDpanel.Models
{
    public class Table
    {
        [JsonProperty("schema")]
        public string Schema { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("primaryKeyColumn")]
        public string PrimaryKeyColumn { get; set; }

        [JsonProperty("identityColumn")]
        public string IdentityColumn { get; set; }

        [JsonProperty("referencingColumn")]
        public string ReferencingColumn { get; set; }

        [JsonProperty("dependantColumns")]
        public List<Column> DependantColumns { get; set; }

        [JsonProperty("dependantRows")]
        public List<Row> DependantRows { get; set; }

        [JsonProperty("referencedColumn")]
        public string ReferencedColumn { get; set; }
    }
}