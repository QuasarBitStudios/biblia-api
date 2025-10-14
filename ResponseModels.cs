using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Text.Json.Serialization;

namespace BibliaAPI
{
    public class ResponseModels
    {
        public class Retorno
        {
            public string? texto { get; set; }
            public string? tipo { get; set; }
            public string? livro { get; set; }
            public int? capitulo { get; set; }
            public string? versiculo { get; set; }
            public string? audio_url { get; set; }
        }
        
        public class CadastroRequest
        {
            [JsonPropertyName("nome")]
            public string Nome { get; set; } = null!;

            [JsonPropertyName("email")]
            public string Email { get; set; } = null!;

            [JsonPropertyName("uso")]
            public string Uso { get; set; } = null!;

            [JsonPropertyName("perfil")]
            public string Perfil { get; set; } = null!;
        }

        
        public class CadastroResponse
        {
            [JsonPropertyName("uuid")]
            public string Uuid { get; set; } = null!;
        }
        [Table("Usuarios")]
        public class Usuarios : BaseModel
        {
            [Column("user_id")]
            [PrimaryKey("user_id")]
            public Guid? Id { get; set; }

            [Column("nome")]
            public string Nome { get; set; } = null!;

            [Column("email")]
            public string Email { get; set; } = null!;

            [Column("uso")]
            public string Uso { get; set; } = null!;

            [Column("perfil")]
            public string Perfil { get; set; } = null!;

        }
    }
}
