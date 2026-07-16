using System.Text.Json.Serialization;
using Wyvern.Domain.Entities;
using System.Collections.Generic;

namespace Wyvern.Api
{

    [JsonSerializable(typeof(IEnumerable<Usuario>))]
    [JsonSerializable(typeof(Usuario))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}