using System.Text.Json.Serialization;

namespace LucysTools;

public class Config {
    [JsonInclude] public bool SomeSetting = true;
}
