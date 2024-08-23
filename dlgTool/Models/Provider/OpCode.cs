using Newtonsoft.Json;

namespace dlgTool.Models.Provider
{
    class OpCode
    {
        [JsonIgnore]
        public int? Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("count")]
        public int ArgumentCount { get; set; }
    }
}
