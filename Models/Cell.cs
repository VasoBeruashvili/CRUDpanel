using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CRUDpanel.Models
{
    public class Cell
    {
        [JsonProperty("column")]
        public Column Column { get; set; }
                
        [JsonProperty("value")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Value { get; set; }
    }
}